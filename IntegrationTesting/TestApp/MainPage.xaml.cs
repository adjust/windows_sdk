using System.Collections.Generic;
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
            
            var localIp = GetLocalIp();
            var commandListener = new CommandListener();
            AdjustFactory.BaseUrl = baseUrl;
            _testLibrary = new TestLibrary.TestLibrary(baseUrl, commandListener, localIp);

            //string testNames = GetTestNames();
            //_testLibrary.SetTests(testNames);

            _testLibrary.ExitAppEvent += (sender, args) => { Exit(); };
            commandListener.SetTestLibrary(_testLibrary);

            StartTestSession();
        }        

        private string GetTestNames()
        {
            string testsDir = "current/";
            var testNamesList = new List<string>
            {
                //testsDir + "event/Test_Event_Params",
                //testsDir + "event/Test_Event_Count_6events",
                //testsDir + "event/Test_Event_EventToken_Malformed",

                //testsDir + "sdkPrefix/Test_SdkPrefix_with_value",

                //testsDir + "appSecret/Test_AppSecret_no_secret",
                //testsDir + "appSecret/Test_AppSecret_with_secret",

                //testsDir + "eventBuffering/Test_EventBuffering_sensitive_packets",

                //testsDir + "subsessionCount/Test_SubsessionCount",

                testsDir + "disableEnable/Disable_restart_track",
                //testsDir + "event/Test_Event_OrderId",
                //testsDir + "event/Test_Event_Params",
                //testsDir + "offlineMode/Test_OfflineMode",
            };
            
            return string.Join(";", testNamesList);
        }

        private void StartTestSession()
        {
            _testLibrary.StartTestSession(clientSdk: "wuap4.12.0");
        }

        private string GetLocalIp()
        {
            var icp = NetworkInformation.GetInternetConnectionProfile();

            if (icp?.NetworkAdapter == null) return null;

            var hostNames = NetworkInformation.GetHostNames();
            foreach (var hn in hostNames)
            {
                if(hn.IPInformation?.NetworkAdapter == null)
                    continue;

                if (hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId)
                    return hn.CanonicalName;
            }

            return string.Empty;
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