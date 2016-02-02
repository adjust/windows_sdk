using Newtonsoft.Json;
using PCLStorage;
//using PCLStorage;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AdjustSdk.Pcl
{
    public static class Util
    {
        public const string BaseUrl = "https://app.adjust.com";
        private static ILogger Logger { get { return AdjustFactory.Logger; } }
        private static NullFormat NullFormat = new NullFormat();

        internal static string GetStringEncodedParameters(Dictionary<string, string> parameters)
        {
            if (parameters.Count == 0) return "";
            var firstPair = parameters.First();

            var stringBuilder = new StringBuilder(EncodedQueryParameter(firstPair, isFirstParameter: true));

            foreach (var pair in parameters.Skip(1))// skips the first "&"
            {
                stringBuilder.Append(EncodedQueryParameter(pair));
            }

            return stringBuilder.ToString();
        }

        public static bool DeleteFile(string filename)
        {
            try
            {
                var localStorage = FileSystem.Current.LocalStorage;
                var activityStateFile = localStorage.GetFileAsync(filename).Result;
                if (activityStateFile != null)
                {
                    activityStateFile.DeleteAsync().Wait();

                    //Logger.Debug("File {0} deleted", filename);
                    return true;
                }
                else
                {
                    //Logger.Debug("File {0} doesn't exist to delete", filename);
                    return false;
                }
            }
            catch (PCLStorage.Exceptions.FileNotFoundException)
            {
                //Logger.Debug("File {0} doesn't exist to delete", filename);
                return false;
            }
            catch (Exception)
            {
                //Logger.Error("Error deleting {0} file", filename);
                return false;
            }
        }

        public static string f(string message, params object[] parameters)
        {
            return String.Format(NullFormat, message, parameters);
        }

        internal static bool IsFileNotFound(this Exception ex)
        {
            // check if the exception type is File Not Found (FNF)
            if (ex is PCLStorage.Exceptions.FileNotFoundException
                || ex is FileNotFoundException)
                return true;

            // if the exception is an aggregate of exceptions
            // we'll check each of them recursively if they are FNF
            var agg = ex as AggregateException;
            if (agg != null)
            {
                foreach (var innerEx in agg.InnerExceptions)
                {
                    if (innerEx.IsFileNotFound())
                        return true;
                }
            }

            // if the exception it's not FNF and doesn't have an inner exception
            // then it's not a FNF
            if (ex.InnerException == null)
                return false;

            // check recursively if the inner exception is FNF
            if (ex.InnerException.IsFileNotFound())
                return true;

            // if all checks fails, the exception must not be a FNF
            return false;
        }

        internal static async Task<T> DeserializeFromFileAsync<T>(string fileName,
            Func<Stream, T> ObjectReader,
            Func<T> defaultReturn,
            string objectName,
            Func<string, byte[]> fileReader
            )
            where T : class
        {
            try
            {
                if (fileReader == null)
                {
                    // Regular reading scenario.
                    var localStorage = FileSystem.Current.LocalStorage;

                    var file = await localStorage.GetFileAsync(fileName);

                    if (file == null)
                    {
                        throw new PCLStorage.Exceptions.FileNotFoundException(fileName);
                    }

                    T output;

                    using (var stream = await file.OpenAsync(FileAccess.Read))
                    {
                        output = ObjectReader(stream);
                    }

                    Logger.Debug("Read {0}: {1}", objectName, output);

                    // successful read
                    return output;
                }
                else
                {
                    // Cocos2d-x reading scenario.
                    byte[] readContent = fileReader(fileName);

                    if (readContent == null)
                    {
                        return defaultReturn();
                    }

                    using (MemoryStream memoryStream = new MemoryStream(readContent))
                    {
                        using (XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(memoryStream, XmlDictionaryReaderQuotas.Max))
                        {
                            DataContractSerializer dcs = new DataContractSerializer(typeof(T));

                            return (T)dcs.ReadObject(reader);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex.IsFileNotFound())
                {
                    Logger.Verbose("{0} file not found", objectName);
                }
                else
                {
                    Logger.Error("Failed to read file {0} ({1})", objectName, Util.ExtractExceptionMessage(ex));
                }
            }

            // fresh start
            return defaultReturn();
        }

        internal static async Task SerializeToFileAsync<T>(Action<string, byte[]> fileWriter, string fileName, Action<Stream, T> objectWriter, T input, Func<string> sucessMessage)
            where T : class
        {
            try
            {
                if (fileWriter == null)
                {
                    // Regular writing scenario.
                    var localStorage = FileSystem.Current.LocalStorage;
                    var newActivityStateFile = await localStorage.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                    using (var stream = await newActivityStateFile.OpenAsync(FileAccess.ReadAndWrite))
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                        objectWriter(stream, input);
                    }

                    Logger.Debug(sucessMessage());
                }
                else
                {
                    // Cocos2d-x writing scenario.
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(ms))
                        {
                            DataContractSerializer dcs = new DataContractSerializer(typeof(T));
                            dcs.WriteObject(writer, input);
                            writer.Flush();
                            fileWriter(fileName, ms.ToArray());
                        }
                    }

                    Logger.Debug(sucessMessage());
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to write to file {0} ({1})", fileName, Util.ExtractExceptionMessage(ex));
            }

        }

        internal static async Task SerializeToFileAsync<T>(Action<string, byte[]> fileWriter, string fileName, Action<Stream, T> objectWriter, T input, string objectName)
            where T : class
        {
            await SerializeToFileAsync(
                fileWriter: fileWriter,
                fileName: fileName,
                objectWriter: objectWriter,
                input: input,
                sucessMessage: () => Util.f("Wrote {0}: {1}", objectName, input));
        }

        private static string EncodedQueryParameter(KeyValuePair<string, string> pair, bool isFirstParameter = false)
        {
            if (isFirstParameter)
                return Util.f("{0}={1}", Uri.EscapeDataString(pair.Key), Uri.EscapeDataString(pair.Value));
            else
                return Util.f("&{0}={1}", Uri.EscapeDataString(pair.Key), Uri.EscapeDataString(pair.Value));
        }

        internal static double SecondsFormat(this DateTime? date)
        {
            if (date == null)
                return -1;

            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date.Value.ToUniversalTime() - origin;
            return diff.TotalSeconds;
        }

        internal static double SecondsFormat(this TimeSpan? timeSpan)
        {
            if (timeSpan == null)
                return -1;
            else
                return timeSpan.Value.TotalSeconds;
        }

        internal static string Quote(this string input)
        {
            if (input == null || !input.Contains(" "))
                return input;

            return Util.f("'{0}'", input);
        }

        internal static string DateFormat(DateTime value)
        {
            var timeZone = value.ToString("zzz");
            var rfc822TimeZone = timeZone.Remove(3, 1);
            var sDTwOutTimeZone = value.ToString("yyyy-MM-ddTHH:mm:ss");
            var sDateTime = Util.f("{0}Z{1}", sDTwOutTimeZone, rfc822TimeZone);

            return sDateTime;
        }

        #region Serialization

        internal static Int64 SerializeTimeSpanToLong(TimeSpan? timeSpan)
        {
            if (timeSpan.HasValue)
                return timeSpan.Value.Ticks;
            else
                return -1;
        }

        internal static Int64 SerializeDatetimeToLong(DateTime? dateTime)
        {
            if (dateTime.HasValue)
                return dateTime.Value.Ticks;
            else
                return -1;
        }

        internal static TimeSpan? DeserializeTimeSpanFromLong(Int64 ticks)
        {
            if (ticks == -1)
                return null;
            else
                return new TimeSpan(ticks);
        }

        internal static DateTime? DeserializeDateTimeFromLong(Int64 ticks)
        {
            if (ticks == -1)
                return null;
            else
                return new DateTime(ticks);
        }

        internal static T TryRead<T>(Func<T> readStream, Func<T> defaultValue)
        {
            T result;
            try
            {
                result = readStream();
            }
            catch (EndOfStreamException)
            {
                result = defaultValue();
            }

            return result;
        }

        #endregion Serialization

        internal static Dictionary<string, string> ParseJsonResponse(HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage == null) { return null; }

            using (var content = httpResponseMessage.Content)
            {
                var sResponse = content.ReadAsStringAsync().Result;
                return BuildJsonDict(sResponse, httpResponseMessage.IsSuccessStatusCode);
            }
        }

        internal static Dictionary<string, string> ParseJsonExceptionResponse(HttpWebResponse httpWebResponse)
        {
            if (httpWebResponse == null) { return null; }

            using (var streamResponse = httpWebResponse.GetResponseStream())
            using (var streamReader = new StreamReader(streamResponse))
            {
                var sResponse = streamReader.ReadToEnd();
                return BuildJsonDict(sResponse, false);
            }
        }

        internal static string GetDictionaryValue(Dictionary<string, string> dic, string key)
        {
            string value;
            if (!dic.TryGetValue(key, out value))
            {
                return null;
            }
            return value;
        }

        internal static Dictionary<string, string> BuildJsonDict(string sResponse, bool IsSuccessStatusCode)
        {
            Logger.Verbose("Response: {0}", sResponse);

            if (sResponse == null) { return null; }
            
            Dictionary<string, object> jsonDicObj = null;
            try
            {
                jsonDicObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(sResponse);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to parse json response ({0})", Util.ExtractExceptionMessage(e));
            }

            if (jsonDicObj == null) { return null; }

            var jsonDic = jsonDicObj.Where(kvp => kvp.Value != null).
                ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

            string message;
            if (!jsonDic.TryGetValue("message", out message))
            {
                message = "No message found";
            }

            if (IsSuccessStatusCode)
            {
                Logger.Info("{0}", message);
            }
            else
            {
                Logger.Error("{0}", message);
            }

            return jsonDic;
        }

        internal static HttpClient BuildHttpClient(string clientSdk)
        {
            var httpClient = new HttpClient(AdjustFactory.GetHttpMessageHandler());

            httpClient.Timeout = new TimeSpan(0, 1, 0);
            httpClient.DefaultRequestHeaders.Add("Client-SDK", clientSdk);

            return httpClient;
        }

        internal static string ExtractExceptionMessage(Exception e)
        {
            if (e == null)
            {
                return "";
            }
            return e.Message + ExtractExceptionMessage(e.InnerException);
        }
    }

    // http://stackoverflow.com/a/7689257
    public class NullFormat : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type service)
        {
            if (service == typeof(ICustomFormatter))
            {
                return this;
            }
            else
            {
                return null;
            }
        }

        public string Format(string format, object arg, IFormatProvider provider)
        {
            if (arg == null)
            {
                return "Null";
            }
            IFormattable formattable = arg as IFormattable;
            if (formattable != null)
            {
                return formattable.ToString(format, provider);
            }
            return arg.ToString();
        }
    }
}