using System;
using System.Collections.Generic;

namespace AdjustSdk.Pcl
{
    public class SessionParameters : VersionedSerializable
    {
        internal Dictionary<string, string> CallbackParameters { get; set; }
        internal Dictionary<string, string> PartnerParameters { get; set; }

        internal SessionParameters Clone()
        {
            var copy = new SessionParameters();

            if (CallbackParameters != null)
            {
                copy.CallbackParameters = new Dictionary<string, string>(CallbackParameters);
            }

            if (PartnerParameters != null)
            {
                copy.PartnerParameters = new Dictionary<string, string>(PartnerParameters);
            }
            return copy;
        }
        
        #region Serialization
        internal override Dictionary<string, Tuple<SerializableType, object>> GetSerializableFields()
        {
            var serializableFields = new Dictionary<string, Tuple<SerializableType, object>>(2);

            AddField(serializableFields, "CallbackParameters", CallbackParameters);
            AddField(serializableFields, "PartnerParameters", PartnerParameters);

            return serializableFields;
        }

        internal override void InitWithSerializedFields(int version, Dictionary<string, object> serializedFields)
        {
            CallbackParameters = GetFieldValueDictionaryString(serializedFields, "CallbackParameters");
            PartnerParameters = GetFieldValueDictionaryString(serializedFields, "PartnerParameters");
        }
        #endregion Serialization
    }
}
