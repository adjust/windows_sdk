namespace AdjustSdk.Pcl
{
    internal static class Constants
    {
        internal const string ALG_SHA256 = "sha256";
        internal const string ALG_SHA512 = "sha512";
        internal const string ALG_MD5 = "md5";

        internal const string BASE_URL = "https://app.adjust.com";
        internal const string GDPR_URL = "https://gdpr.adjust.com";

        internal const string ACTIVITY_KIND = "activity_kind";
        internal const string CREATED_AT = "created_at";
        internal const string SECRET_ID = "secret_id";
        internal const string SIGNATURE = "signature";
        internal const string ALGORITHM = "algorithm";
        internal const string HEADERS = "headers";
        internal const string APP_SECRET = "app_secret";
        internal const string CLEAR_SIGNATURE = "clear_signature";
        internal const string FIELDS = "fields";
        internal const string AUTHORIZATION_PARAM = "Authorization";
        internal const string INITIATED_BY = "initiated_by";

        internal const string SESSION = "session";
        internal const string EVENT = "event";
        internal const string CLICK = "click";
        internal const string ATTRIBUTION = "attribution";
        internal const string INFO = "info";
        internal const string SDK_INFO = "sdk_info";
        internal const string GDPR = "gdpr";

        internal const string SESSION_PATH = "/session";
        internal const string EVENT_PATH = "/event";
        internal const string SDK_CLICK_PATH = "/sdk_click";
        internal const string ATTRIBUTION_PATH = "/attribution";
        internal const string SDK_INFO_PATH = "/sdk_info";
        internal const string GDPR_PATH = "/gdpr_forget_device";

        internal const string ATTRIBUTION_DEEPLINK = "attribution_deeplink";
        internal const string EVENT_TOKEN = "event_token";
        internal const string EVENT_COUNT = "event_count";
        internal const string REVENUE = "revenue";
        internal const string CURRENCY = "currency";
        internal const string TRACKER = "tracker";
        internal const string CAMPAIGN = "campaign";
        internal const string ADGROUP = "adgroup";
        internal const string CREATIVE = "creative";
        internal const string DEEPLINK = "deeplink";
        internal const string SOURCE = "source";
        internal const string DEEPLINK_URL = "deeplink_url";
        internal const string DEEPLINK_CLICK_TIME = "deeplink_click_time";
        internal const string ADJUST_PUSH_TOKEN = "adj_push_token";
        internal const string GDPR_USER_FORGOTTEN = "adj_gdpr_user_forgotten";
        internal const string PUSH_TOKEN = "push_token";
        internal const string CLIENT_SDK = "Client-SDK";
        internal const string WIN_ADID = "win_adid";
        internal const string WIN_HWID = "win_hwid";
        internal const string WIN_NAID = "win_naid";
        internal const string WIN_UDID = "win_udid";
        internal const string WIN_UUID = "win_uuid";
        internal const string EAS_ID = "eas_id";
        internal const string CONNECTIVITY_TYPE = "connectivity_type";
        internal const string NETWORK_TYPE = "network_type";
        internal const string USER_AGENT = "user-agent";
        internal const string QUEUE_SIZE = "queue_size";
        internal const string SENT_AT = "sent_at";
        internal const string EVENT_CALLBACK_ID = "event_callback_id";

        internal const string FB_AUTH_REGEX = "^(fb|vk)[0-9]{5,}[^:]*://authorize.*access_token=.*";
    }
}
