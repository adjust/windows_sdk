using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestApp
{
    public sealed partial class MainPage : Page
    {
        private TestLibrary.TestLibrary _testLibrary { get; }
        public static readonly string TAG = "TestApp";

        public readonly static string BaseUrl = "http://localhost:8080";
        public readonly static string GdprUrl = "http://localhost:8080";

        public MainPage()
        {
            InitializeComponent();

            var localIp = GetLocalIp();
            var commandListener = new CommandListener();
            _testLibrary = new TestLibrary.TestLibrary(BaseUrl, commandListener, localIp);

            //_testLibrary.AddTest("current/gdpr/Test_GdprForgetMe_after_install");
            _testLibrary.AddTestDirectory("current/gdpr");

            _testLibrary.ExitAppEvent += (sender, args) => { Exit(); };
            commandListener.SetTestLibrary(_testLibrary);

            StartTestSession();
        }        
        
        private void StartTestSession()
        {
            _testLibrary.StartTestSession(clientSdk: "wuap4.15.0");
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
