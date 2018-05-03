using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TestLibrary.Networking;
using System.Text;

namespace TestLibrary
{
    public class TestLibrary
    {
        private const string DATE_TIME_FORMAT = @"MM\/dd\/yyyy HH:mm";
        internal static string BaseUrl;

        public string LocalIp { get; set; }
        internal ICommandListener CommandListener;
        internal ICommandJsonListener CommandJsonListener;
        internal ICommandRawJsonListener CommandRawJsonListener;
        internal ControlChannel ControlChannel;
        internal string CurrentBasePath;
        internal string CurrentTestName;
        internal bool ExitAfterEnd = true;
        internal Dictionary<string, string> InfoToServer;

        internal StringBuilder TestNames;

        //https://docs.microsoft.com/en-us/dotnet/standard/collections/thread-safe/blockingcollection-overview
        internal BlockingCollection<string> WaitControlQueue;

        public TestLibrary(string baseUrl, ICommandListener commandListener, string localIp, Action<string> logDelegate = null)
            : this(baseUrl, localIp, logDelegate)
        {
            CommandListener = commandListener;
        }

        public TestLibrary(string baseUrl, ICommandJsonListener commandJsonListener, string localIp, Action<string> logDelegate = null)
            : this(baseUrl, localIp, logDelegate)
        {
            CommandJsonListener = commandJsonListener;
        }

        public TestLibrary(string baseUrl, ICommandRawJsonListener commandRawJsonListener, string localIp, Action<string> logDelegate = null)
            : this(baseUrl, localIp, logDelegate)
        {
            CommandRawJsonListener = commandRawJsonListener;
        }

        private TestLibrary(string baseUrl, string localIp, Action<string> logDelegate = null)
        {
            Log.InjectLogDelegate(logDelegate);
            BaseUrl = baseUrl;
            LocalIp = localIp;
            TestNames = new StringBuilder();
        }

        public event EventHandler ExitAppEvent;

        // resets test library to initial state
        public void ResetTestLibrary()
        {
            Teardown();

            WaitControlQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
        }

        // clears test library
        private void Teardown()
        {
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
        private void ResetForNextTest()
        {
            ClearTest();

            WaitControlQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
            ControlChannel = new ControlChannel(this);
        }

        public void AddTest(string testName)
        {
            TestNames.Append(testName);

            if (!testName.EndsWith(";"))
            {
                TestNames.Append(";");
            }
        }
        
        public void AddTestDirectory(string testDir)
        {
            TestNames.Append(testDir);

            if (!testDir.EndsWith("/") || !testDir.EndsWith("/;"))
            {
                TestNames.Append("/");
            }

            if (!testDir.EndsWith(";"))
            {
                TestNames.Append(";");
            }
        }

        public void DoNotExitAfterEnd()
        {
            ExitAfterEnd = false;
        }

        public void StartTestSession(string clientSdk)
        {
            DebugLog("StartTestSession(" + clientSdk + ")");
            ResetTestLibrary();
            Task.Run(() => { SendTestSessionI(clientSdk); });
        }

        public void AddInfoToSend(string key, string value)
        {
            if (InfoToServer == null)
                InfoToServer = new Dictionary<string, string>();

            InfoToServer.Add(key, value);
        }

        public void SendInfoToServer(string basePath)
        {
            Task.Run(() => { SendInfoToServerI(basePath); });
        }

        internal void ReadResponse(HttpResponse httpResponse)
        {
            Task.Run(() => { ReadResponseI(httpResponse); });
        }

        private void SendTestSessionI(string clientSdk)
        {
            DebugLog("SendTestSessionI");
            var httpResponse = UtilsNetworking
                .SendPostI("/init_session", clientSdk, LocalIp, TestNames.ToString()).Result;
            
            ReadResponseI(httpResponse);
        }

        private void SendInfoToServerI(string basePath)
        {
            DebugLog("sendInfoToServerI called");
            var httpResponse = UtilsNetworking
                .SendPostI(basePath + "/test_info", null, LocalIp, InfoToServer).Result;
            InfoToServer = null;

            ReadResponseI(httpResponse);
        }

        public void ReadResponseI(HttpResponse httpResponse)
        {
            if (httpResponse == null)
            {
                DebugLog("Cannot read Response. httpResponse is null");
                return;
            }

            // test server can return an empty response in a form of open and closed bracket "{}"
            // and JsonConvert cannot deserialize it (throws an exception), hence this check is needed
            if (httpResponse.Response == "{}")
            {
                DebugLog("ReadResponseI - httpResponse is empty, skipping");
                return;
            }

            DebugLog("Reading test commands...");
            var testCommandsArray = JsonConvert.DeserializeObject<TestCommand[]>(httpResponse.Response);
            var testCommands = testCommandsArray.ToList();
            DebugLog("Received " + testCommands.Count + " test commands");
            try
            {
                ExecTestCommandsI(testCommands);
            }
            catch (Exception e)
            {
                DebugLog("Error while executing test commands: {0}", e.ToString());
            }
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
                {
                    CommandListener.ExecuteCommand(testCommand.ClassName, testCommand.FunctionName,
                        testCommand.Params);
                }
                else if (CommandJsonListener != null)
                {
                    CommandJsonListener.ExecuteCommand(testCommand.ClassName, testCommand.FunctionName, JsonConvert.SerializeObject(testCommand.Params));
                }
                else if (CommandRawJsonListener != null)
                {
                    CommandRawJsonListener.ExecuteCommand(JsonConvert.SerializeObject(testCommand));
                }

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
                case "resetTest": ResetTestI(testCommand.Params); break;
                case "endTestReadNext": EndTestReadNext(); break;
                case "endTestSession": EndTestSessionI(); break;
                case "wait": WaitI(testCommand.Params); break;
                case "exit": Exit(); break;
            }
        }

        private void ResetTestI(Dictionary<string, List<string>> paramsMap)
        {
            if (paramsMap.ContainsKey("basePath"))
            {
                CurrentBasePath = paramsMap["basePath"][0];
                DebugLog($"current base path {CurrentBasePath}");
            }

            if (paramsMap.ContainsKey("testName"))
            {
                CurrentTestName = paramsMap["testName"][0];
                DebugLog($"current test name {CurrentTestName}");
            }

            ResetForNextTest();
        }

        private void EndTestSessionI()
        {
            Teardown();
            if (ExitAfterEnd)
            {
                Exit();
            }
        }

        private void EndTestReadNext()
        {
            // send end test request
            var httpResponse = UtilsNetworking.SendPostI(CurrentBasePath + 
                "/end_test_read_next", LocalIp).Result;
            // and process the next in the response
            ReadResponseI(httpResponse);
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

        private void Exit()
        {
            ExitAppEvent?.Invoke(this, null);
        }

        private void DebugLog(string message, params object[] parameters)
        {
            Log.Debug(nameof(TestLibrary), message, parameters);
        }
    }
}
