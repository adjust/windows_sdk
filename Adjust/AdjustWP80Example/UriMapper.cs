using AdjustSdk;
using System;
using System.Windows.Navigation;

namespace AdjustWP80Example
{
    public class UriMapper : UriMapperBase
    {
        private string tempUri;

        public override Uri MapUri(Uri uri)
        {
            Adjust.AppWillOpenUrl(uri);

            // Map everything to the main page. 
            return new Uri("/MainPage.xaml", UriKind.Relative);
        }
    }
}
