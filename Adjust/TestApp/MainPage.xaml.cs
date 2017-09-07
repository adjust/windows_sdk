using System.Collections.Generic;
using System.Linq;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using AdjustSdk.Pcl;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestApp
{
    public sealed partial class MainPage : Page
    {
        private TestLibrary.TestLibrary _testLibrary { get; }
        public static readonly string TAG = "TestApp";

        public MainPage()
        {
            InitializeComponent();
            
            //var baseUrl = "http://192.168.8.223:8080";
            var baseUrl = "http://localhost:8080";

            //TODO: SSL setup
            //AdjustFactory.SetTestingMode(baseUrl);

            var localIp = GetLocalIp();
            var commandListener = new CommandListener();
            AdjustFactory.BaseUrl = baseUrl;
            _testLibrary = new TestLibrary.TestLibrary(baseUrl, commandListener, localIp);

            string testNames = GetTestNames();
            _testLibrary.SetTests(testNames);

            _testLibrary.ExitAppEvent += (sender, args) => { Exit(); };
            commandListener.SetTestLibrary(_testLibrary);

            StartTestSession();
        }

        private string GetTestNames()
        {
            string testsDir = "current/Test_";
            string testDirWin = "current/windows/Test_";
            var testNamesList = new List<string>
            {
                // PASSING ////////////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////////////////////
                testsDir + "Event_Count",
                testsDir + "Event_Revenue",
                testsDir + "DefaultTracker",
                testsDir + "Event_EventToken_Malformed",
                testsDir + "Event_Params",
                testsDir + "SessionParams",
                testsDir + "SessionCount",
                testsDir + "SubsessionCount",
                testsDir + "SessionEventCallbacks",
                testsDir + "Event_OrderId",
                testsDir + "AttributionCallback",
                testsDir + "Init_Malformed",
                testDirWin + "SdkPrefix",
                testDirWin + "UserAgent",
                testDirWin + "Deeplink",
                testDirWin + "DelayStart",
                testDirWin + "SendInBackground",

                // NOT PASSING ////////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////////////////////


                // NOT PASSING - 4.12.0 ///////////////////////////////////////////////
                ///////////////////////////////////////////////////////////////////////
                //testsDir + "OfflineMode",
                //testsDir + "Disable_Enable"
                //testsDir + "EventBuffering"
                //testsDir + "AppSecret",
                //testDirWin + "SdkInfo",

                // EXPELED     ////////////////////////////////////////////////////////
                ///////////////////////////////////////////////////////////////////////
                //testsDir + "ExternalDeviceId"
            };

            return string.Join(";", testNamesList);
        }

        private void StartTestSession()
        {
            _testLibrary.InitTestSession("wuap4.11.4");
        }

        private string GetLocalIp()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return null;

            var hostname = NetworkInformation.GetHostNames()
                .SingleOrDefault(hn =>
                    hn.IPInformation?.NetworkAdapter != null && hn.IPInformation.NetworkAdapter.NetworkAdapterId
                    == icp.NetworkAdapter.NetworkAdapterId);

            // the ip address
            return hostname?.CanonicalName;
        }

        public void Exit()
        {
            Application.Current.Exit();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            StartTestSession();
        }
    }
}