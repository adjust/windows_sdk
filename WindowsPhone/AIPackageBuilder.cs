using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace adeven.AdjustIo
{
    class AIPackageBuilder
    {
        //general
        internal string AppToken { get; set; }
        internal string MacSha1 { get; set; }
        internal string MacShortMD5 { get; set; }
        internal string IdForAdvertisers { get; set; }
        internal string FbAttributionId { get; set; }
        internal string Environment { get; set; }
        internal string UserAgent { get; set; }
        internal string ClientSdk { get; set; }
        internal bool IsTrackingEnable { get; set; }

        //session
        internal int SessionCount { get; set; }
        internal int SubSessionCount { get; set; }
        internal DateTime CreatedAt { get; set; }
        internal double SessionLength { get; set; }
        internal double TimeSpent { get; set; }
        internal double LastInterval { get; set; }

        //events
        internal int EventCount { get; set; }
        internal string EventToken { get; set; }
        internal Dictionary<string, string> CallBackParameters { get; set; }
        internal double AmountInCents { get; set; }

        //defaults
        private AIActivityPackage activityPackage { get; set; }

        //TODO change ToString() to serializer, possible ServiceStack.Text

        internal void FillDefaults()
        {
            activityPackage = new AIActivityPackage
            {
                UserAgent = this.UserAgent,
                ClientSdk = this.ClientSdk,
                Parameters = new Dictionary<string, string>
                {
                    //general
                    {"created_at"   , CreatedAt.ToString()  },
                    {"app_token"    , AppToken              },
                    {"mac_sha1"     , MacSha1               },
                    {"mac_md5"      , MacShortMD5           },
                    {"idfa"         , IdForAdvertisers      },
                    {"fb_id"        , FbAttributionId       },
                    {"environment"  , Environment           },
                    {"tracking_enable"  , IsTrackingEnable.ToString()   },
                    //session related (used for events as well)
                    {"session_count"    , SessionCount.ToString()       },
                    {"subsession_count" , SubSessionCount.ToString()    },
                    {"session_length"   , SessionLength.ToString()      },
                    {"time_spent"   , TimeSpent.ToString()  },
                }
            };
        }

        internal AIActivityPackage BuildSessionPackage()
        {
            activityPackage.Parameters.Add("last_interval", LastInterval.ToString());

            activityPackage.Path = @"/startup";
            activityPackage.Kind = "session start";
            activityPackage.Suffix = "";

            return activityPackage;
        }

        internal AIActivityPackage BuildEventPackage()
        {
            activityPackage.Parameters.Add("event_count", EventCount.ToString());
            activityPackage.Parameters.Add("event_token", EventToken);
            activityPackage.Parameters.Add("params", CallBackParameters.ToString());

            activityPackage.Path = @"/event";
            activityPackage.Kind = "event";
            
            activityPackage.Suffix = this.EventSuffix();

            return activityPackage;
        }

        //internal AIActivityPackage BuildRevenuePackage()
        //{
//}

        private string EventSuffix()
        {
            return String.Format(" '{0}'", EventToken);
        }

    }
}
