using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace AdjustWP80Example
{
    public class UriMapper : UriMapperBase
    {
        private string tempUri;

        public override Uri MapUri(Uri uri)
        {
            // Map everything to the main page. 
            return new Uri("/MainPage.xaml", UriKind.Relative);
        }
    }
}
