using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestLibrary.Networking;

namespace TestLibrary
{
    public class ControlChannel
    {
        private static readonly string CONTROL_START_PATH = "/control_start";
        private static readonly string CONTROL_CONTINUE_PATH = "/control_continue";

        private readonly TestLibrary _testLibrary;
        private bool _isClosed;

        public ControlChannel(TestLibrary testLibrary)
        {
            _testLibrary = testLibrary;
            SendControlRequest(CONTROL_START_PATH);
        }

        public void Teardown()
        {
            _isClosed = true;
        }

        private void SendControlRequest(string controlPath)
        {
            Task.Run(() =>
            {
                var ticksBefore = DateTime.Now.Ticks;
                DebugLog("time (ticks) before wait: {0}", ticksBefore);

                var response = UtilsNetworking
                    .SendPostI(_testLibrary.CurrentBasePath + controlPath, _testLibrary.LocalIp).Result;               

                var ticksAfter = DateTime.Now.Ticks;
                var elapsedMillisenconds = TimeSpan.FromTicks(ticksAfter - ticksBefore).TotalMilliseconds;
                DebugLog("time (ticks) after wait: {0}", ticksAfter);
                DebugLog("time elapsed waiting in milliseconds: {0}", elapsedMillisenconds);

                ReadControlHeaders(response);
            });
        }

        private void ReadControlHeaders(HttpResponse httpResponse)
        {
            if (_isClosed)
            {
                DebugLog("control channel already closed");
                return;
            }
            if (httpResponse.HeaderFields.ContainsKey(Constants.TEST_CANCELTEST_HEADER))
            {
                DebugLog("Test canceled due to {0}", httpResponse.HeaderFields[Constants.TEST_CANCELTEST_HEADER][0]);
                _testLibrary.ResetTestLibrary();
                _testLibrary.ReadHeaders(httpResponse);
            }
            if (httpResponse.HeaderFields.ContainsKey(Constants.TEST_ENDWAIT_HEADER))
            {
                var waitEndReason = httpResponse.HeaderFields[Constants.TEST_ENDWAIT_HEADER][0];
                SendControlRequest(CONTROL_CONTINUE_PATH);
                EndWait(waitEndReason);
            }
        }

        private void EndWait(string waitEndReason)
        {
            DebugLog("End wait from control channel due to {0}", waitEndReason);

            var waitReasonAdded = _testLibrary.WaitControlQueue.TryAdd(waitEndReason);
            if (!waitReasonAdded)
                DebugLog("ControlChannel.EndWait - failed to add waitEndReason = [{0}] to queue", waitEndReason);

            DebugLog("Wait ended from control channel due to {0}", waitEndReason);
        }

        private void DebugLog(string message, params object[] parameters)
        {
            Log.Debug(nameof(ControlChannel), message, parameters);
        }

        private void ErrorLog(string message, params object[] parameters)
        {
            Log.Error(nameof(ControlChannel), message, parameters);
        }
    }
}