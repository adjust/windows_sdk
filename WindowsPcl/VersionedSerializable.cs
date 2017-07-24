using System;
using System.Collections.Generic;
using System.IO;

namespace AdjustSdk.Pcl
{
    public abstract class VersionedSerializable
    {
        internal enum SerializableType
        {
            Boolean=0,
            Byte,
            Bytes,
            Char,
            Chars,
            Decimal,
            Double,
            Int16,
            Int32,
            Int64,
            SByte,
            Single,
            String,
            UInt16,
            UInt32,
            UInt64,
            DictionaryStringString,
        }
        
        private static readonly Dictionary<SerializableType, Func<BinaryReader, object>> FieldReaders= new Dictionary<SerializableType, Func<BinaryReader, object>>
        {
            { SerializableType.Boolean, (reader) => reader.ReadBoolean() },
            { SerializableType.Byte, (reader) => reader.ReadByte() },
            { SerializableType.Bytes, (reader) => {
                var lenght = reader.ReadInt32();
                return reader.ReadBytes(lenght);
            } },
            { SerializableType.Char, (reader) => reader.ReadChar() },
            { SerializableType.Chars, (reader) => {
                var lenght = reader.ReadInt32();
                return reader.ReadChars(lenght);
            } },
            { SerializableType.Decimal, (reader) => reader.ReadDecimal() },
            { SerializableType.Double, (reader) => reader.ReadDouble() },
            { SerializableType.Int16, (reader) => reader.ReadInt16() },
            { SerializableType.Int32, (reader) => reader.ReadInt32() },
            { SerializableType.Int64, (reader) => reader.ReadInt64() },
            { SerializableType.SByte, (reader) => reader.ReadSByte() },
            { SerializableType.Single, (reader) => reader.ReadSingle() },
            { SerializableType.String, (reader) => reader.ReadString() },
            { SerializableType.UInt16, (reader) => reader.ReadUInt16() },
            { SerializableType.UInt32, (reader) => reader.ReadUInt32() },
            { SerializableType.UInt64, (reader) => reader.ReadUInt64() },
            { SerializableType.DictionaryStringString, (reader) => {
                var count = reader.ReadInt32();
                var dictionary = new Dictionary<string, string>(count);
                for (int i = 0; i < count; i++)
                {
                    var key = reader.ReadString();
                    var value = reader.ReadString();
                    dictionary.Add(key, value);
                }
                return dictionary;
            } },
        };

        public static int Version = 4100;

        internal abstract void InitWithSerializedFields(int version, Dictionary<string, object> serializedFields);

        // does not close stream received. Caller is responsible to close if it wants it
        internal static T DeserializeFromStream<T>(Stream stream) where T : VersionedSerializable, new()
        {
            return DeserializeFromStream<T>(new BinaryReader(stream));
        }
        internal static T DeserializeFromStream<T>(BinaryReader reader) where T : VersionedSerializable, new()
        {
            T result = new T();
            // read version of the object read
            var version = reader.ReadInt32();
            // read the number of fields of the object
            var fieldCount = reader.ReadInt32();

            var fields = new Dictionary<string, object>(fieldCount);

            for (int i = 0; i < fieldCount; i++)
            {
                string fieldName = reader.ReadString();
                SerializableType fieldType = (SerializableType) reader.ReadInt16();
                Func<BinaryReader, object> fieldReader;
                if (FieldReaders.TryGetValue(fieldType, out fieldReader))
                {
                    var fieldValue = fieldReader(reader);
                    fields.Add(fieldName, fieldValue);
                }
                else
                {
                    AdjustFactory.Logger.Error("Could not read field {0} of type {1}", fieldName, fieldType);
                    break;
                }
            }

            result.InitWithSerializedFields(version, fields);

            return result;
        }

        internal static void AddField(Dictionary<string, Tuple<SerializableType, object>> serializableFields, 
            string fieldName, string value, 
            string defaultValue = null)
        {
            if (value == defaultValue) { return; }
            serializableFields.Add(fieldName,
                new Tuple<SerializableType, object>(
                    SerializableType.String, value
                )
            );
        }

        internal static void AddField(Dictionary<string, Tuple<SerializableType, object>> serializableFields, 
            string fieldName, Dictionary<string, string> value,
            Dictionary<string, string> defaultValue = null)
        {
            if (value == defaultValue) { return; }
            serializableFields.Add(fieldName,
                new Tuple<SerializableType, object>(
                    SerializableType.DictionaryStringString, value
                )
            );
        }

        internal static void AddField(Dictionary<string, Tuple<SerializableType, object>> serializableFields, 
            string fieldName, int value,
            int defaultValue = default(int))
        {
            if (value == defaultValue) { return; }
            serializableFields.Add(fieldName,
                new Tuple<SerializableType, object>(
                    SerializableType.Int32, value
                )
            );
        }

        internal static void AddField(Dictionary<string, Tuple<SerializableType, object>> serializableFields, 
            string fieldName, TimeSpan? value,
            TimeSpan? defaultValue = null)
        {
            if (!value.HasValue || value == defaultValue) { return; }
            serializableFields.Add(fieldName,
                new Tuple<SerializableType, object>(
                    SerializableType.Int64, value.Value.Ticks
                )
            );
            
        }

        internal static void AddField(Dictionary<string, Tuple<SerializableType, object>> serializableFields,
            string fieldName, DateTime? value,
            DateTime? defaultValue = null)
        {
            if (!value.HasValue || value == defaultValue) { return; }
            serializableFields.Add(fieldName,
                new Tuple<SerializableType, object>(
                    SerializableType.Int64, value.Value.ToBinary()
                )
            );
        }

        internal static void AddField(Dictionary<string, Tuple<SerializableType, object>> serializableFields,
            string fieldName, bool value)
        {
            serializableFields.Add(fieldName,
                new Tuple<SerializableType, object>(
                    SerializableType.Boolean, value
                )
            );
        }

        protected string GetFieldValueString(Dictionary<string, object> dict, string key, string defaultValue = null)
        {
            return GetFieldValue(dict, key, defaultValue);
        }

        protected int GetFieldValueInt(Dictionary<string, object> dict, string key, int defaultValue = default(int))
        {
            return GetFieldValue(dict, key, defaultValue);
        }

        protected bool GetFieldValueBool(Dictionary<string, object> dict, string key, bool defaultValue = default(bool))
        {
            return GetFieldValue(dict, key, defaultValue);
        }

        protected Dictionary<string, string> GetFieldValueDictionaryString(Dictionary<string, object> dict, string key, Dictionary<string, string>  defaultValue = null)
        {
            return GetFieldValue(dict, key, defaultValue);
        }

        protected TimeSpan? GetFieldValueTimeSpan(Dictionary<string, object> dict, string key, TimeSpan? defaultValue = null)
        {
            var longValue = GetFieldValue<long?>(dict, key);
            if (longValue.HasValue)
            {
                return TimeSpan.FromTicks(longValue.Value);
            }
            return defaultValue;
        }

        protected DateTime? GetFieldValueDateTime(Dictionary<string, object> dict, string key, DateTime? defaultValue = null)
        {
            var longValue = GetFieldValue<long?>(dict, key);
            if (longValue.HasValue)
            {
                return DateTime.FromBinary(longValue.Value);
            }
            return defaultValue;
        }

        private T GetFieldValue<T>(Dictionary<string, object> dict, string key, T defaultValue = default(T))
        {
            if (dict == null) { return defaultValue; }
            object value;
            if (dict.TryGetValue(key, out value))
            {
                if (value is T)
                {
                    return (T)value;
                }
            }
            return defaultValue;
        }
    }
}
