using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AdjustSdk.Pcl.FileSystem;

namespace AdjustSdk.Pcl
{
    public static class Util
    {
        public const string BaseUrl = "https://app.adjust.com";
        private static ILogger _Logger { get { return AdjustFactory.Logger; } }
        private static NullFormat NullFormat = new NullFormat();
        private static HttpClient _HttpClient = new HttpClient(AdjustFactory.GetHttpMessageHandler());

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
        
        public static string f(string message, params object[] parameters)
        {
            return String.Format(NullFormat, message, parameters);
        }

        internal static bool IsFileNotFound(this Exception ex)
        {
            // check if the exception type is File Not Found (FNF)
            if (ex is FileNotFoundException)
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

        internal static async Task<T> DeserializeFromFileAsync<T>(IFile file,
            Func<Stream, T> objectReader,
            Func<T> defaultReturn,
            string objectName)
            where T : class
        {
            try
            {
                //IFolder localStorage = FileSystem.Current.LocalStorage;

                //IFile file = await localStorage.GetFileAsync(fileName);

                if (file == null)
                {
                    //throw new PCLStorage.Exceptions.FileNotFoundException(fileName);
                    _Logger.Verbose("{0} file not found", objectName);
                    return defaultReturn();
                }

                T output;
                using (var stream = await file.OpenAsync())
                {
                    output = objectReader(stream);
                }

                _Logger.Debug("Read {0}: {1}", objectName, output);

                // successful read
                return output;
            }
            catch (Exception ex)
            {
                if (ex.IsFileNotFound())
                {
                    _Logger.Verbose("{0} file not found", objectName);
                }
                else
                {
                    _Logger.Error("Failed to read file {0} ({1})", objectName, ExtractExceptionMessage(ex));
                }
            }

            // fresh start
            return defaultReturn();
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

        internal static Dictionary<string, string> ParseJsonResponse(HttpWebResponse httpWebResponse)
        {
            if (httpWebResponse == null) { return null; }

            using (var streamResponse = httpWebResponse.GetResponseStream())
            using (var streamReader = new StreamReader(streamResponse))
            {
                var sResponse = streamReader.ReadToEnd();
                return BuildJsonDict(sResponse: sResponse, IsSuccessStatusCode: false);
            }
        }

        internal static string GetDictionaryString(Dictionary<string, string> dict, string key)
        {
            string value = null;
            dict?.TryGetValue(key, out value);
            return value;
        }
        
        internal static int? GetDictionaryInt(Dictionary<string, string> dict, string key)
        {
            var stringValue = GetDictionaryString(dict, key);
            int intValue;
            if (int.TryParse(stringValue, out intValue))
            {
                return intValue;
            }
            return null;
        }

        internal static Dictionary<string, string> BuildJsonDict(string sResponse, bool IsSuccessStatusCode)
        {
            _Logger.Verbose("Response: {0}", sResponse);

            if (sResponse == null) { return null; }
            
            Dictionary<string, object> jsonDicObj = null;
            try
            {
                jsonDicObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(sResponse);
            }
            catch (Exception e)
            {
                _Logger.Error("Failed to parse json response ({0})", Util.ExtractExceptionMessage(e));
            }

            if (jsonDicObj == null) { return null; }

            // convert to a string,string dictionary
            Dictionary<string, string> jsonDic = jsonDicObj.Where(kvp => kvp.Value != null).
                ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

            return jsonDic;
        }

        internal static void ConfigureHttpClient(string clientSdk)
        {
            _HttpClient.Timeout = new TimeSpan(0, 1, 0);
            _HttpClient.DefaultRequestHeaders.Add("Client-SDK", clientSdk);
        }

        internal static string ExtractExceptionMessage(Exception e)
        {
            if (e == null)
            {
                return "";
            }
            return e.Message + ExtractExceptionMessage(e.InnerException);
        }

        internal static TimeSpan WaitingTime(int retries, BackoffStrategy backoffStrategy)
        {
            if (retries < backoffStrategy.MinRetries)
            {
                return TimeSpan.Zero;
            }

            // Start with base 0
            int baseValue = retries - backoffStrategy.MinRetries;

            // Get the exponential Time from the base: 1, 2, 4, 8, 16, ... * times the multiplier
            long exponentialTimeTicks = (long)Math.Pow(2, baseValue) * backoffStrategy.TicksMultiplier;

            // Limit the maximum allowed time to wait
            long ceilingTimeTicks = Math.Min(exponentialTimeTicks, backoffStrategy.MaxWaitTicks);

            // get the random range
            double randomRange = GetRandomNumber(backoffStrategy.MinRange, backoffStrategy.MaxRange);

            // Apply jitter factor
            double waitingTimeTicks = ceilingTimeTicks * randomRange;

            return TimeSpan.FromTicks((long)exponentialTimeTicks);
        }

        private static double GetRandomNumber(double minRange, double maxRange)
        {
            Random random = new Random();
            double range = maxRange - minRange;
            double scaled = random.NextDouble() * range;
            double shifted = scaled + minRange;
            return shifted;
        }

        public static string SecondDisplayFormat(TimeSpan timeSpan)
        {
            return String.Format("{0:0.0}", timeSpan.TotalSeconds);
        }

        public static HttpResponseMessage SendPostRequest(ActivityPackage activityPackage)
        {
            var url = Util.BaseUrl + activityPackage.Path;

            var sNow = Util.DateFormat(DateTime.Now);
            activityPackage.Parameters["sent_at"] = sNow;

            using (var parameters = new FormUrlEncodedContent(activityPackage.Parameters))
            {
                return _HttpClient.PostAsync(url, parameters).Result;
            }
        }

        public static HttpResponseMessage SendGetRequest(ActivityPackage activityPackage, string queryParameters)
        {
            var finalQuery = Util.f("{0}&send_at={1}", queryParameters, Util.DateFormat(DateTime.Now));

            var uriBuilder = new UriBuilder(Util.BaseUrl);
            uriBuilder.Path = activityPackage.Path;
            uriBuilder.Query = finalQuery;

            return _HttpClient.GetAsync(uriBuilder.Uri).Result;
        }

        public static ResponseData ProcessResponse(HttpWebResponse httpWebResponse, ActivityPackage activityPackage)
        {
            var jsonDic = Util.ParseJsonResponse(httpWebResponse);
            return Util.ProcessResponse(jsonDic, (int?)httpWebResponse?.StatusCode, activityPackage);
        }

        public static ResponseData ProcessResponse(HttpResponseMessage httpResponseMessage, ActivityPackage activityPackage)
        {
            var jsonDic = Util.ParseJsonResponse(httpResponseMessage);
            return Util.ProcessResponse(jsonDic, (int?)httpResponseMessage?.StatusCode, activityPackage);
        }

        private static ResponseData ProcessResponse(Dictionary<string, string> jsonResponse, 
            int? statusCode, 
            ActivityPackage activityPackage)
        {
            var responseData = ResponseData.BuildResponseData(activityPackage);
            responseData.JsonResponse = jsonResponse;
            responseData.StatusCode = statusCode;
            responseData.Success = false; // false by default, set to true later

            if (jsonResponse == null)
            {
                responseData.WillRetry = true;
                return responseData;
            }

            responseData.Message = Util.GetDictionaryString(jsonResponse, "message");
            responseData.Timestamp = Util.GetDictionaryString(jsonResponse, "timestamp");
            responseData.Adid = Util.GetDictionaryString(jsonResponse, "adid");

            string message = responseData.Message;
            if (message == null)
            {
                message = "No message found";
            }
            
            if (statusCode.HasValue && statusCode.Value == 200)
            {
                _Logger.Info("{0}", message);
                responseData.Success = true;
            }
            else
            {
                _Logger.Error("{0}", message);
                responseData.Success = false;
            }

            if (!statusCode.HasValue)
            {
                responseData.WillRetry = true;
            }
            else if (statusCode == 500 || statusCode == 501)
            {
                responseData.WillRetry = false;
            }
            else if (statusCode != 200)
            {
                responseData.WillRetry = true;
            }

            return responseData;
        }

        internal static bool CheckParameter(string attribute, string attributeType, string parameterName)
        {
            if (attribute == null)
            {
                _Logger.Error("{0} parameter {1} is missing", parameterName, attributeType);
                return false;
            }

            if (attribute.Length == 0)
            {
                _Logger.Error("{0} parameter {1} is empty", parameterName, attributeType);
                return false;
            }

            return true;
        }

        internal static Dictionary<string, string> MergeParameters(
            Dictionary<string, string> target,
            Dictionary<string, string> source,
            string parametersName)
        {
            if (target == null)
            {
                return source;
            }
            if (source == null)
            {
                return target;
            }

            var mergedParameters = new Dictionary<string, string>(target);
            foreach (var kvp in source)
            {
                var key = kvp.Key;
                var value = kvp.Value;
                string oldValue = null;
                if (mergedParameters.TryGetValue(key, out oldValue))
                {
                    _Logger.Warn("Key {0} with value {1} from {2} parameter was replaced by value {3}",
                        key,
                        oldValue,
                        parametersName,
                        value);
                }
                mergedParameters.AddSafe(kvp.Key, kvp.Value);
            }
            return mergedParameters;
        }

        internal static void AddSafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (key == null || value == null) {
                return;
            }
            dict.Remove(key);
            dict.Add(key, value);
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