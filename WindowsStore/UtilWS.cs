using adeven.AdjustIo.PCL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.System.Profile;

namespace adeven.AdjustIo
{
    internal class UtilWS : DeviceUtil
    {
        public string EnvironmentSandbox { get { return "sandbox"; } }

        public string EnvironmentProduction { get { return "production"; } }

        public string ClientSdk { get { return "winstore1.0"; } }

        public string GetDeviceId()
        {
            return GetDeviceIdHardware();
            //return GetDeviceIdNetwork();
        }

        private string GetDeviceIdHardware()
        {
            var token = HardwareIdentification.GetPackageSpecificToken(null);
            var hardwareId = token.Id;
            var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

            byte[] bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            return BitConverter.ToString(bytes);
        }

        private string GetDeviceIdNetwork()
        {
            var profiles = Windows.Networking.Connectivity.NetworkInformation.GetConnectionProfiles();
            var iter = profiles.GetEnumerator();
            iter.MoveNext();
            var adapter = iter.Current.NetworkAdapter;
            string adapterId = adapter.NetworkAdapterId.ToString();
            return adapterId;
        }

        public string GetMd5Hash(string input)
        {
            var alg = HashAlgorithmProvider.OpenAlgorithm("MD5");
            IBuffer buff = CryptographicBuffer.ConvertStringToBinary(input, BinaryStringEncoding.Utf8);
            var hashed = alg.HashData(buff);
            var res = CryptographicBuffer.EncodeToHexString(hashed);
            return res;
        }

        // TODO delete if PCL Storage works in WP & WS
        public async Task<T> DeserializeFromFileAsync<T>(string fileName, Func<System.IO.Stream, T> ObjectReader, Func<T> defaultReturn)
            where T : class
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var localFile = await localFolder.GetFileAsync(fileName);

                if (localFile == null)
                    return defaultReturn();

                T output;
                using (var stream = await localFile.OpenStreamForReadAsync())
                {
                    output = ObjectReader(stream);
                }
                Logger.Verbose("Read from file {0}", fileName);
                return output;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to read file {0} ({1})", fileName, ex);
            }

            return defaultReturn();
        }

        // TODO delete if PCL Storage works in WP & WS
        public async Task SerializeToFileAsync<T>(string fileName, Action<System.IO.Stream, T> ObjectWriter, T input)
            where T : class
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                try
                {
                    var localFile = await localFolder.GetFileAsync(fileName);

                    if (localFile != null)
                        await localFile.DeleteAsync();
                }
                catch (FileNotFoundException) { }

                var newFile = await localFolder.CreateFileAsync(fileName);

                //using (var randomAccessStream = newFile.OpenAsync(FileAccessMode.ReadWrite).GetResults())
                //using (var outputStram = randomAccessStream.GetOutputStreamAt(0))
                //using (var dataWriter = new DataWriter(outputStram))
                //{
                //    dataWriter.write
                //}

                using (var ras = await newFile.OpenAsync(FileAccessMode.ReadWrite))
                using (var stream = ras.AsStreamForWrite())
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    ObjectWriter(stream, input);
                }
                Logger.Verbose("Wrote to file {0}", fileName);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to write to file {0} ({1})", fileName, ex.Message);
            }
        }

        public string GetUserAgent()
        {
            return String.Join(" ",
                getAppName(),
                getAppVersion(),
                getAppPublisher(),
                getDeviceType(),
                getDeviceName(),
                getArchitecture(),
                getOsName(),
                getOsVersion(),
                getLanguage(),
                getCountry());
        }

        #region User Agent

        private string getAppDisplayName()
        {
            string namespaceName = "http://schemas.microsoft.com/appx/2010/manifest";
            XElement element = XDocument.Load("appxmanifest.xml").Root;
            element = element.Element(XName.Get("Properties", namespaceName));
            element = element.Element(XName.Get("DisplayName", namespaceName));
            string displayName = element.Value;
            string sanitized = sanitizeString(displayName);
            return sanitized;
        }

        private string getDeviceName()
        {
            return "unknown";
        }

        private string getDeviceType()
        {
            return "unknown";
        }

        private string getArchitecture()
        {
            PackageId package = getPackage();
            ProcessorArchitecture architecture = package.Architecture;
            switch (architecture)
            {
                case ProcessorArchitecture.Arm: return "arm";
                case ProcessorArchitecture.X86: return "x86";
                case ProcessorArchitecture.X64: return "x64";
                case ProcessorArchitecture.Neutral: return "neutral";
                case ProcessorArchitecture.Unknown: return "unknown";
                default: return "unknown";
            }
        }

        private string getLanguage()
        {
            CultureInfo currentCulture = CultureInfo.CurrentUICulture;
            string cultureName = currentCulture.Name;
            if (cultureName.Length < 2)
            {
                return "zz";
            }

            string language = cultureName.Substring(0, 2);
            string sanitized = sanitizeString(language, "zz");
            return sanitized;
        }

        private string getCountry()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            string cultureName = currentCulture.Name;
            int length = cultureName.Length;
            if (length < 2)
            {
                return "zz";
            }

            string substring = cultureName.Substring(length - 2, 2);
            string country = substring.ToLower();
            string sanitized = sanitizeString(country, "zz");
            return sanitized;
        }

        private string getOsName()
        {
            return "windows";
        }

        private string getOsVersion()
        {
            return "8.0";
        }

        private PackageId getPackage()
        {
            Package package = Package.Current;
            PackageId packageId = package.Id;
            return packageId;
        }

        private string getAppName()
        {
            PackageId package = getPackage();
            string packageName = package.Name;
            string sanitized = sanitizeString(packageName);
            return sanitized;
        }

        private string getAppFamilyName()
        {
            PackageId package = getPackage();
            string packageName = package.FamilyName;
            string sanitized = sanitizeString(packageName);
            return sanitized;
        }

        private string getAppFullName()
        {
            PackageId package = getPackage();
            string fullName = package.FullName;
            string sanitized = sanitizeString(fullName);
            return sanitized;
        }

        private string getAppVersion()
        {
            PackageId package = getPackage();
            PackageVersion pv = package.Version;
            string version = string.Format("{0}.{1}", pv.Major, pv.Minor);
            string sanitized = sanitizeString(version);
            return sanitized;
        }

        private string getAppPublisher()
        {
            PackageId package = getPackage();
            string publisher = package.Publisher;
            string sanitized = sanitizeString(publisher);
            return sanitized;
        }

        private string sanitizeString(string s, string defaultString = "unknown")
        {
            if (s == null)
            {
                return defaultString;
            }

            s = s.Replace('=', '_');
            string result = Regex.Replace(s, @"\s+", "");
            if (result.Length == 0)
            {
                return defaultString;
            }

            return result;
        }

        #endregion User Agent
    }
}