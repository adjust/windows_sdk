using System.Collections.Generic;
using System.Diagnostics;
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

            //string baseUrl = "https://10.0.2.2:8443";
            var baseUrl = "http://192.168.8.209:8080";

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
            string testsDir = "current";
            var testNamesList = new List<string>
            {
                testsDir + "/Test_Event_Count",
                testsDir + "/Test_Event_Revenue",
                //testsDir + "/Test_DefaultTracker",
                //testsDir + "/Test_DelayStart",
                //testsDir + "/Test_Deeplink",
                //testsDir + "/Test_AppSecret",
                //testsDir + "/Test_AttributionCallback",
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