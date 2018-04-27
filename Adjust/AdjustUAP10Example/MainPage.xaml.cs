using AdjustSdk;
using System;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace AdjustUAP10Example
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void btnSimpleEvent_Click(object sender, RoutedEventArgs e)
        {
            var simpleEvent = new AdjustEvent("g3mfiw");
            Adjust.TrackEvent(simpleEvent);
        }

        private void btnRevenueEvent_Click(object sender, RoutedEventArgs e)
        {
            var revenueEvent = new AdjustEvent("a4fd35");
            revenueEvent.SetRevenue(0.01, "EUR");
            Adjust.TrackEvent(revenueEvent);
        }

        private void btnCallbakEvent_Click(object sender, RoutedEventArgs e)
        {
            var callbackEvent = new AdjustEvent("34vgg9");
            callbackEvent.AddPartnerParameter("key", "value");
            Adjust.TrackEvent(callbackEvent);
        }

        private void btnPartnerEvent_Click(object sender, RoutedEventArgs e)
        {
            var partnerEvent = new AdjustEvent("w788qs");
            partnerEvent.AddPartnerParameter("foo", "bar");
            Adjust.TrackEvent(partnerEvent);
        }

        private void btnGetAdid_Click(object sender, RoutedEventArgs e)
        {
            var adid = Adjust.GetAdid();
            tblReceivedAdid.Text = adid != null ? adid : "received null";
        }

        private void btnGetAttribution_Click(object sender, RoutedEventArgs e)
        {
            var attribution = Adjust.GetAttributon();
            ShowSimpleMessage(
                title: "Get Attribution Result",
                message: attribution != null ? attribution.ToString() : "Received null!");
        }

        private async void ShowSimpleMessage(string title, string message)
        {
            var contentDialog = new ContentDialog
            {
                Title = title,
                Content = message,
                PrimaryButtonText = "OK"
            };

            await contentDialog.ShowAsync();
        }
    }
}
