using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TestLibrary.Networking;

namespace TestLibrary
{
    public class TestLibrary
    {
        private const string DATE_TIME_FORMAT = @"MM\/dd\/yyyy HH:mm";
        internal static string BaseUrl;

        public string LocalIp { get; set; }
        internal ICommandListener CommandListener;
        internal ControlChannel ControlChannel;
        internal string CurrentBasePath;
        internal string CurrentTest;
        internal bool ExitAfterEnd = true;
        internal Dictionary<string, string> InfoToServer;

        internal string TestNames;

        //https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
        internal BlockingCollection<string> WaitControlQueue;

        public TestLibrary(string baseUrl, ICommandListener commandListener, string localIp)
        {
            BaseUrl = baseUrl;
            LocalIp = localIp;
            DebugLog("base url: {0}", baseUrl);
            CommandListener = commandListener;
        }

        public event EventHandler ExitAppEvent;

        // resets test library to initial state
        public void ResetTestLibrary()
        {
            Teardown(true);

            WaitControlQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
        }

        // clears test library
        private void Teardown(bool shutdownNow)
        {
            // shutdown executor not needed in win sdk version

            ClearTest();
        }

        // clear for each test
        private void ClearTest()
        {
            WaitControlQueue?.Clear();
            WaitControlQueue = null;

            ControlChannel?.Teardown();
            ControlChannel = null;

            InfoToServer = null;
        }

        // reset for each test
        private void ResetTest()
        {
            ClearTest();

            WaitControlQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
            ControlChannel = new ControlChannel(this);
        }

        public void SetTests(string testNames)
        {
            TestNames = testNames;
        }

        public void DoNotExitAfterEnd()
        {
            ExitAfterEnd = false;
        }

        public void InitTestSession(string clientSdk)
        {
            ResetTestLibrary();
            
            Task.Run(() => { SendTestSessionI(clientSdk); });
        }

        public void AddInfoToSend(string key, string value)
        {
            if (InfoToServer == null)
                InfoToServer = new Dictionary<string, string>();

            InfoToServer.Add(key, value);
        }

        public void SendInfoToServer()
        {
            Task.Run(() => { SendInfoToServerI(); });
        }

        internal void ReadHeaders(HttpResponse httpResponse)
        {
            Task.Run(() => { ReadHeadersI(httpResponse); });
        }

        private void SendTestSessionI(string clientSdk)
        {
            var httpResponse = UtilsNetworking
                .SendPostI("/init_session", clientSdk, LocalIp, TestNames).Result;
            if (httpResponse == null)
                return;

            ReadHeadersI(httpResponse);
        }

        private void SendInfoToServerI()
        {
            DebugLog("sendInfoToServerI called");
            var httpResponse = UtilsNetworking
                .SendPostI(CurrentBasePath + "/test_info", null, LocalIp, InfoToServer).Result;
            InfoToServer = null;
            if (httpResponse == null)
                return;

            ReadHeadersI(httpResponse);
        }

        public void ReadHeadersI(HttpResponse httpResponse)
        {
            if (httpResponse.HeaderFields.ContainsKey(Constants.TEST_SESSION_END_HEADER))
            {
                Teardown(false);
                DebugLog("TestSessionEnd received");
                if (ExitAfterEnd)
                    Exit();

                return;
            }

            if (httpResponse.HeaderFields.ContainsKey(Constants.BASE_PATH_HEADER))
                CurrentBasePath = httpResponse.HeaderFields[Constants.BASE_PATH_HEADER][0];

            if (httpResponse.HeaderFields.ContainsKey(Constants.TEST_SCRIPT_HEADER))
            {
                CurrentTest = httpResponse.HeaderFields[Constants.TEST_SCRIPT_HEADER][0];
                ResetTest();

                var testCommandsArray = JsonConvert.DeserializeObject<TestCommand[]>(httpResponse.Response);
                var testCommands = testCommandsArray.ToList();

                try
                {
                    ExecTestCommandsI(testCommands);
                }
                catch (Exception e)
                {
                    Log.Error("Error while executing test commands: {0}", e.ToString());
                    throw;
                }
            }
        }

        private void Exit()
        {
            ExitAppEvent?.Invoke(this, null);
        }

        private void ExecTestCommandsI(List<TestCommand> testCommands)
        {
            DebugLog("testCommands: {0}", testCommands);

            var stopwatch = new Stopwatch();
            foreach (var testCommand in testCommands)
            {
                stopwatch.Restart();
                
                DebugLog("ClassName: {0}, FunctionName: {1}", testCommand.ClassName, testCommand.FunctionName);
                if (testCommand.Params != null && testCommand.Params.Count > 0)
                {
                    DebugLog("Params:");
                    foreach (var entry in testCommand.Params)
                        DebugLog("\t{0}: {1}", entry.Key, string.Join(", ", entry.Value));
                }

                DebugLog("time before {0} {1}: {2}", testCommand.ClassName, testCommand.FunctionName,
                    DateTime.Now.ToString(DATE_TIME_FORMAT));

                if (Constants.TEST_LIBRARY_CLASSNAME == testCommand.ClassName)
                {
                    ExecuteTestLibraryCommandI(testCommand);
                    DebugLog("time after {0} {1}: {2}", testCommand.ClassName, testCommand.FunctionName,
                        DateTime.Now.ToString(DATE_TIME_FORMAT));
                    DebugLog("time elapsed {0} {1} in milli seconds: {2}", testCommand.ClassName,
                        testCommand.FunctionName, stopwatch.ElapsedMilliseconds);

                    continue;
                }

                if (CommandListener != null)
                    CommandListener.ExecuteCommand(testCommand.ClassName, testCommand.FunctionName,
                        testCommand.Params);
                //else if (commandJsonListener != null)
                //{
                //    commandJsonListener.executeCommand(testCommand.ClassName, testCommand.FunctionName, gson.toJson(testCommand.Params));
                //}
                //else if (commandRawJsonListener != null)
                //{
                //    commandRawJsonListener.executeCommand(gson.toJson(testCommand));
                //}

                DebugLog("time after {0}.{1}: {2}", testCommand.ClassName, testCommand.FunctionName,
                    DateTime.Now.ToString(DATE_TIME_FORMAT));
                DebugLog("time elapsed {0}.{1} in milli seconds: {2}", testCommand.ClassName, testCommand.FunctionName,
                    stopwatch.ElapsedMilliseconds);
            }
        }

        private void ExecuteTestLibraryCommandI(TestCommand testCommand)
        {
            switch (testCommand.FunctionName)
            {
                case "end_test":
                    EndTestI();
                    break;
                case "wait":
                    WaitI(testCommand.Params);
                    break;
                case "exit":
                    Exit();
                    break;
            }
        }

        private void EndTestI()
        {
            var httpResponse = UtilsNetworking.SendPostI(CurrentBasePath + "/end_test", LocalIp).Result;
            if (httpResponse == null)
            {
                if (ExitAfterEnd)
                    Exit();

                return;
            }

            ReadHeadersI(httpResponse);
        }

        private void WaitI(Dictionary<string, List<string>> parameters)
        {
            if (parameters.ContainsKey(Constants.WAIT_FOR_CONTROL))
            {
                var waitExpectedReason = parameters[Constants.WAIT_FOR_CONTROL][0];
                DebugLog("wait for {0}", waitExpectedReason);

                //A call to Take may block until an item is available to be removed.
                //https://msdn.microsoft.com/en-us/library/dd287085(v=vs.110).aspx
                var endReason = WaitControlQueue.Take();
                DebugLog("wait ended due to {0}", endReason);
            }

            if (parameters.ContainsKey(Constants.WAIT_FOR_SLEEP))
            {
                var millisToSleep = long.Parse(parameters[Constants.WAIT_FOR_SLEEP][0]);
                DebugLog("sleep for {0}", millisToSleep);

                Task.Delay(TimeSpan.FromMilliseconds(millisToSleep)).Wait();
                DebugLog("sleep ended");
            }
        }

        private void DebugLog(string message, params object[] parameters)
        {
            Log.Debug(nameof(TestLibrary), message, parameters);
        }
    }
}