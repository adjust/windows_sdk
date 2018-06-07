using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using AdjustSdk.Pcl.FileSystem;
using static AdjustSdk.Pcl.Constants;

namespace AdjustSdk.Pcl
{
    public static class Util
    {
        private static ILogger Logger => AdjustFactory.Logger;
        private static readonly NullFormat NullFormat = new NullFormat();
        internal static string UserAgent { get; set; }

        private static HttpClient _httpClient;

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
        
        public static string F(string message, params object[] parameters)
        {
            return string.Format(NullFormat, message, parameters);
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
                if (file == null)
                {
                    Logger.Verbose("{0} file not found", objectName);
                    return defaultReturn();
                }

                T output;
                using (var stream = await file.OpenAsync())
                {
                    output = objectReader(stream);
                }

                Logger.Debug("Read {0}: {1}", objectName, output);

                // successful read
                return output;
            }
            catch (Exception ex)
            {
                if (ex.IsFileNotFound())
                {
                    Logger.Verbose("{0} file not found", objectName);
                }
                else
                {
                    Logger.Error("Failed to read file {0} ({1})", objectName, ExtractExceptionMessage(ex));
                }
            }

            // fresh start
            return defaultReturn();
        }
        
        private static string EncodedQueryParameter(KeyValuePair<string, string> pair, bool isFirstParameter = false)
        {
            if (isFirstParameter)
            {
                return F("{0}={1}", Uri.EscapeDataString(pair.Key), Uri.EscapeDataString(pair.Value));
            }

            return F("&{0}={1}", Uri.EscapeDataString(pair.Key), Uri.EscapeDataString(pair.Value));
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
            if (timeSpan == null) { return -1; }

            return timeSpan.Value.TotalSeconds;
        }

        internal static string Quote(this string input)
        {
            if (input == null || !input.Contains(" ")) { return input; }

            return F("'{0}'", input);
        }

        internal static string DateFormat(DateTime value)
        {
            var timeZone = value.ToString("zzz");
            var rfc822TimeZone = timeZone.Remove(3, 1);
            var sDTwOutTimeZone = value.ToString("yyyy-MM-ddTHH:mm:ss");
            var sDateTime = F("{0}Z{1}", sDTwOutTimeZone, rfc822TimeZone);

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
                return BuildJsonDict(sResponse: sResponse, isSuccessStatusCode: false);
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

        internal static Dictionary<string, string> BuildJsonDict(string sResponse, bool isSuccessStatusCode)
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
                Logger.Error("Failed to parse json response ({0})", ExtractExceptionMessage(e));
            }

            if (jsonDicObj == null) { return null; }

            // convert to a string,string dictionary
            Dictionary<string, string> jsonDic = jsonDicObj.Where(kvp => kvp.Value != null).
                ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());

            return jsonDic;
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
            return $"{timeSpan.TotalSeconds:0.0}";
        }

        public static void MarkGdprForgotten(IDeviceUtil deviceUtil)
        {
            deviceUtil.PersistSimpleValue(GDPR_USER_FORGOTTEN, "true");
        }

        public static bool IsMarkedGdprForgotten(IDeviceUtil deviceUtil)
        {
            string isGdprForgottenStr;
            if (deviceUtil.TryTakeSimpleValue(GDPR_USER_FORGOTTEN, out isGdprForgottenStr))
            {
                return !string.IsNullOrEmpty(isGdprForgottenStr) && isGdprForgottenStr == "true";
            }
            return false;
        }

        public static void ClearGdprForgotten(IDeviceUtil deviceUtil)
        {
            deviceUtil.ClearSimpleValue(GDPR_USER_FORGOTTEN);
        }

        public static void ClearDeeplinkCache(IDeviceUtil deviceUtil)
        {
            deviceUtil.ClearSimpleValue(DEEPLINK_URL);
            deviceUtil.ClearSimpleValue(DEEPLINK_CLICK_TIME);
        }

        public static bool GetDeeplinkCacheValues(IDeviceUtil deviceUtil, out Uri deeplinkUri, out DateTime clickTime)
        {
            deeplinkUri = null;
            clickTime = new DateTime();

            string deeplinkUriStr;
            deviceUtil.TryTakeSimpleValue(DEEPLINK_URL, out deeplinkUriStr);
            string deeplinkUrlClickTimeStr;
            deviceUtil.TryTakeSimpleValue(DEEPLINK_CLICK_TIME, out deeplinkUrlClickTimeStr);

            if (deeplinkUriStr == null || deeplinkUrlClickTimeStr == null)
            {
                return false;
            }

            long deeplinkUrlClickTimeTicks;
            if (!long.TryParse(deeplinkUrlClickTimeStr, out deeplinkUrlClickTimeTicks))
            {
                return false;
            }

            deeplinkUri = new Uri(deeplinkUriStr);
            clickTime = new DateTime(deeplinkUrlClickTimeTicks);

            return true;
        }

        public static HttpResponseMessage SendPostRequest(ActivityPackage activityPackage, string basePath, int queueSize)
        {
            string baseUrl = activityPackage.ActivityKind != ActivityKind.Gdpr
                ? AdjustFactory.BaseUrl
                : AdjustFactory.GdprUrl;

            string url = basePath != null
                ? baseUrl + basePath + activityPackage.Path
                : baseUrl + activityPackage.Path;

            var sNow = DateFormat(DateTime.Now);
            activityPackage.Parameters[SENT_AT] = sNow;

            string secretId = ExtractSecretId(activityPackage.Parameters);
            string appSecret = ExtractAppSecret(activityPackage.Parameters);

            string activityKind = Enum.GetName(typeof(ActivityKind), activityPackage.ActivityKind);
            string authorizationHeader =
                BuildAuthorizationHeader(activityPackage.Parameters, appSecret, secretId, activityKind);

            SetUserAgent();
            SetAuthorizationParameter(authorizationHeader);

            Dictionary<string, string> postParamsMap = 
                new Dictionary<string, string>(activityPackage.Parameters);
            if(queueSize > 0)
                postParamsMap.Add(QUEUE_SIZE, queueSize.ToString());

            using (var postParams = new FormUrlEncodedContent(postParamsMap))
            {
                return _httpClient.PostAsync(url, postParams).Result;
            }
        }

        public static HttpResponseMessage SendGetRequest(ActivityPackage activityPackage, string basePath, string queryParameters)
        {
            var finalQuery = F("{0}&{1}={2}", queryParameters, SENT_AT, DateFormat(DateTime.Now));

            string secretId = ExtractSecretId(activityPackage.Parameters);
            string appSecret = ExtractAppSecret(activityPackage.Parameters);

            string activityKind = Enum.GetName(typeof(ActivityKind), activityPackage.ActivityKind);
            string authorizationHeader =
                BuildAuthorizationHeader(activityPackage.Parameters, appSecret, secretId, activityKind);

            string path = basePath != null
                ? basePath + activityPackage.Path
                : activityPackage.Path;

            var uriBuilder = new UriBuilder(AdjustFactory.BaseUrl);
            uriBuilder.Path = path;
            uriBuilder.Query = finalQuery;

            SetUserAgent();
            SetAuthorizationParameter(authorizationHeader);

            return _httpClient.GetAsync(uriBuilder.Uri).Result;
        }

        private static string ExtractAppSecret(Dictionary<string, string> parameters)
        {
            string appSecret;
            if (parameters.TryGetValue(APP_SECRET, out appSecret))
            {
                parameters.Remove(APP_SECRET);
            }
            return appSecret;
        }

        private static string ExtractSecretId(Dictionary<string, string> parameters)
        {
            string secretId;
            if (parameters.TryGetValue(SECRET_ID, out secretId))
            {
                parameters.Remove(SECRET_ID);
            }
            return secretId;
        }

        private static string BuildAuthorizationHeader(IReadOnlyDictionary<string, string> parameters,
            string appSecret, string secretId, string activityKind)
        {
            // check if the secret exists and it's not empty
            if (string.IsNullOrEmpty(appSecret) || parameters == null)
                return null;

            var signatureDetails = GetSignature(parameters, activityKind, appSecret);
            
            string algorithm = ALG_SHA256;
            string signature = AdjustConfig.String2Sha256Func(signatureDetails[CLEAR_SIGNATURE]);
            signature = signature.ToLower();
            string fields = signatureDetails[FIELDS];

            string secretIdHeader = $"{SECRET_ID}=\"{secretId}\"";
            string signatureHeader = $"{SIGNATURE}=\"{signature}\"";
            string algorithmHeader = $"{ALGORITHM}=\"{algorithm}\"";
            string fieldsHeader = $"{HEADERS}=\"{fields}\"";

            string authorizationHeader = $"Signature {secretIdHeader},{signatureHeader},{algorithmHeader},{fieldsHeader}";

            Logger.Verbose($"authorizationHeader: {authorizationHeader}");

            return authorizationHeader;
        }

        private static Dictionary<string, string> GetSignature(
            IReadOnlyDictionary<string, string> parameters,
            string activityKind, string appSecret)
        {
            string createdAt = parameters[CREATED_AT];
            string deviceIdentifier;
            string deviceIdentifierName = GetValidIdentifier(parameters, out deviceIdentifier);

            var signatureParams = new Dictionary<string, string>
            {
                {APP_SECRET, appSecret},
                {CREATED_AT, createdAt},
                {ACTIVITY_KIND, activityKind.ToLower()},
                {deviceIdentifierName, deviceIdentifier}
            };

            string fields = string.Empty;
            string clearSignature = string.Empty;

            foreach (var paramKvp in signatureParams)
            {
                if (paramKvp.Value == null)
                    continue;

                fields += paramKvp.Key + " ";
                clearSignature += paramKvp.Value;
            }

            fields = fields.Substring(0, fields.Length - 1);

            var signature = new Dictionary<string, string>
            {
                {CLEAR_SIGNATURE, clearSignature},
                {FIELDS, fields}
            };

            return signature;
        }

        private static string GetValidIdentifier(IReadOnlyDictionary<string, string> parameters, 
            out string foundValue)
        {
            if (parameters.TryGetValue(WIN_ADID, out foundValue))
                return WIN_ADID;

            if (parameters.TryGetValue(WIN_HWID, out foundValue))
                return WIN_HWID;

            if (parameters.TryGetValue(WIN_NAID, out foundValue))
                return WIN_NAID;

            if (parameters.TryGetValue(WIN_UDID, out foundValue))
                return WIN_UDID;

            foundValue = null;
            return null;
        }
        
        private static void SetUserAgent()
        {
            _httpClient.DefaultRequestHeaders.Remove(USER_AGENT);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(USER_AGENT, UserAgent);
        }

        private static void SetAuthorizationParameter(string authHeader)
        {
            _httpClient.DefaultRequestHeaders.Remove(AUTHORIZATION_PARAM);
            if(!string.IsNullOrEmpty(authHeader))
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(AUTHORIZATION_PARAM, authHeader);
        }

        public static ResponseData ProcessResponse(HttpWebResponse httpWebResponse, ActivityPackage activityPackage)
        {
            var jsonDic = ParseJsonResponse(httpWebResponse);
            return ProcessResponse(jsonDic, (int?)httpWebResponse?.StatusCode, activityPackage);
        }

        public static ResponseData ProcessResponse(HttpResponseMessage httpResponseMessage, ActivityPackage activityPackage)
        {
            var jsonDic = ParseJsonResponse(httpResponseMessage);
            return ProcessResponse(jsonDic, (int?)httpResponseMessage?.StatusCode, activityPackage);
        }

        private static ResponseData ProcessResponse(Dictionary<string, string> jsonResponse, 
            int? statusCode, 
            ActivityPackage activityPackage)
        {
            var responseData = ResponseData.BuildResponseData(activityPackage);
            responseData.JsonResponse = jsonResponse;
            responseData.StatusCode = statusCode;
            responseData.Success = false; // false by default, set to true later

            // for scenario: too frequent request for GDPR forget user
            if (statusCode == 429)
            {
                responseData.WillRetry = true;
                return responseData;
            }

            if (jsonResponse == null)
            {
                responseData.WillRetry = true;
                return responseData;
            }

            responseData.Message = GetDictionaryString(jsonResponse, "message");
            responseData.Timestamp = GetDictionaryString(jsonResponse, "timestamp");
            responseData.Adid = GetDictionaryString(jsonResponse, "adid");

            string trackingState = GetDictionaryString(jsonResponse, "tracking_state");
            if(!string.IsNullOrEmpty(trackingState) && trackingState == "opted_out")
            {
                responseData.TrackingState = TrackingState.OPTED_OUT;
            }

            string message = responseData.Message;
            if (message == null)
            {
                message = "No message found";
            }
            
            if (statusCode.HasValue && statusCode.Value == 200)
            {
                Logger.Info("{0}", message);
                responseData.Success = true;
            }
            else
            {
                Logger.Error("{0}", message);
                responseData.Success = false;
            }

            if (!statusCode.HasValue)
            {
                responseData.WillRetry = true;
            }

            return responseData;
        }

        internal static bool CheckParameter(string attribute, string attributeType, string parameterName)
        {
            if (attribute == null)
            {
                Logger.Error("{0} parameter {1} is missing", parameterName, attributeType);
                return false;
            }

            if (attribute.Length == 0)
            {
                Logger.Error("{0} parameter {1} is empty", parameterName, attributeType);
                return false;
            }

            return true;
        }

        internal static Dictionary<string, string> MergeParameters(
            Dictionary<string, string> target,
            Dictionary<string, string> source,
            string parametersName)
        {
            if (target == null) { return source; }
            if (source == null) { return target; }

            var mergedParameters = new Dictionary<string, string>(target);
            foreach (var kvp in source)
            {
                string oldValue;
                if (mergedParameters.TryGetValue(kvp.Key, out oldValue))
                {
                    Logger.Warn("Key {0} with value {1} from {2} parameter was replaced by value {3}",
                        kvp.Key,
                        oldValue,
                        parametersName,
                        kvp.Value);
                }
                mergedParameters.AddSafe(kvp.Key, kvp.Value);
            }
            return mergedParameters;
        }

        internal static void AddSafe<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (key == null || value == null) { return; }
            dict.Remove(key);
            dict.Add(key, value);
        }

        public static void Teardown()
        {
            _httpClient?.Dispose();
            _httpClient = null;
        }

        public static void RecreateHttpClient(string clientSdk)
        {
            if (_httpClient != null)
                return;

            _httpClient = new HttpClient { Timeout = new TimeSpan(0, 1, 0) };
            if (clientSdk != null)
                _httpClient.DefaultRequestHeaders.Add(CLIENT_SDK, clientSdk);
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

            return null;
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