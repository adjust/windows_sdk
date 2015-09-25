namespace AdjustSdk.Pcl
{
    public class DeviceInfo
    {
        public string ClientSdk { get; set; }

        public string AdvertisingId { get; set; }

        public string DeviceUniqueId { get; set; }

        public string HardwareId { get; set; }

        public string NetworkAdapterId { get; set; }

        public string AppDisplayName { get; set; }

        public string AppName { get; set; }

        public string AppVersion { get; set; }

        public string AppPublisher { get; set; }

        public string AppAuthor { get; set; }

        public string DeviceType { get; set; }

        public string DeviceName { get; set; }

        public string DeviceManufacturer { get; set; }

        public string Architecture { get; set; }

        public string OsName { get; set; }

        public string OsVersion { get; set; }

        public string Language { get; set; }

        public string Country { get; set; }

        public string EasFriendlyName { get; set; }

        public string EasId { get; set; }

        public string EasOperatingSystem { get; set; }

        public string EasSystemFirmwareVersion { get; set; }

        public string EasSystemHardwareVersion { get; set; }

        public string EasSystemManufacturer { get; set; }

        public string EasSystemProductName { get; set; }

        public string EasSystemSku { get; set; }
        
        public string SdkPrefix
        {
            set
            {
                if (value == null) { return; }
                ClientSdk = Util.f("{0}@{1}", value, ClientSdk);
            }
        }
    }
}