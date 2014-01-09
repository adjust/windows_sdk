using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using PCLStorage;
using PCLStorage.Exceptions;
using System.IO;

namespace adeven.AdjustIo.PCL
{
    internal static class Util
    {
        //internal const string BaseUrl = "https://app.adjust.io";
        internal const string BaseUrl = "https://stage.adjust.io"; // todo remove

        internal static string GetStringEncodedParameters(Dictionary<string, string> parameters)
        {
            if (parameters.Count == 0) return "";
            var firstPair = parameters.First();

            var stringBuilder = new StringBuilder(EncodedQueryParameter(firstPair, isFirstParameter: true));

            foreach (var pair in parameters.Skip(1))//skips the first &
            {
                stringBuilder.Append(EncodedQueryParameter(pair));
            }

            return stringBuilder.ToString();
        }

        internal static void DeleteFile(string filename)
        {
            try
            {
                var localStorage = FileSystem.Current.LocalStorage;
                var activityStateFile = localStorage.GetFileAsync(filename).Result;
                if (activityStateFile != null)
                {
                    activityStateFile.DeleteAsync().Wait();
                    AILogger.Verbose("File {0} deleted", filename);
                }
                else
                {
                    AILogger.Verbose("File {0} doesn't exist to delete", filename);
                }
            }
            catch (PCLStorage.Exceptions.FileNotFoundException)
            {
                AILogger.Verbose("File {0} doesn't exist to delete", filename);
            }
            catch (Exception)
            {
                AILogger.Debug("Error deleting {0} file", filename);
            }
        }

        internal static bool TryDeserializeFromFile<T>(string fileName, Func<Stream,T> ObjectReader, out T output)
            where T : class
        {
            output = null;
            try 
            {
                var localStorage = FileSystem.Current.LocalStorage;
                var activityStateFile = localStorage.GetFileAsync(fileName).Result;

                if (activityStateFile == null)
                {
                    throw new PCLStorage.Exceptions.FileNotFoundException(fileName);
                }

                using (var stream = activityStateFile.OpenAsync(FileAccess.Read).Result)
                {
                    output = ObjectReader(stream);
                }
                AILogger.Verbose("Restored from file {0}", fileName);

                //successful read
                return true;
            }
            catch(PCLStorage.Exceptions.FileNotFoundException)
            {
                AILogger.Error("Restore not possible. File {0} not found", fileName);
            }
            catch(Exception ex)
            {
                AILogger.Error("Failed to restore from file {0} exception {1}", fileName, ex.Message);
            }
            return false;
        }

        internal static void SerializeToFile<T>(string filename, Action<Stream, T> ObjectWriter, T input)
            where T : class
        {
            try
            {
                var localStorage = FileSystem.Current.LocalStorage;
                var newActivityStateFile = localStorage.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting).Result;

                using (var stream = newActivityStateFile.OpenAsync(FileAccess.ReadAndWrite).Result)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    ObjectWriter(stream, input);
                }
                AILogger.Verbose("Object wrote to file {0}", filename);
            }
            catch (Exception ex)
            {
                AILogger.Error("Failed to write object from file {0}, with error {1}", filename, ex.Message);
            }
        }

        private static string EncodedQueryParameter(KeyValuePair<string, string> pair, bool isFirstParameter = false)
        {
            if (isFirstParameter)
                return String.Format("{0}={1}", Uri.EscapeDataString(pair.Key), Uri.EscapeDataString(pair.Value));
            else
                return String.Format("&{0}={1}", Uri.EscapeDataString(pair.Key), Uri.EscapeDataString(pair.Value));
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
                return  timeSpan.Value.TotalSeconds;
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
        #endregion

    }
}
