using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdjustSdk.Pcl
{
    public static class AdjustFactory
    {
        private static ILogger InternalLogger;

        public static ILogger Logger
        {
            get
            {
                if (InternalLogger == null)
                    InternalLogger = new Logger();
                return InternalLogger;
            }

            set { InternalLogger = value; }
        }
    }
}