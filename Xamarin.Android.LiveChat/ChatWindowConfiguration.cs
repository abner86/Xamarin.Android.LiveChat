using Android.Text;
using Java.Lang;
using System.Collections.Generic;

namespace Xamarin.Android.LiveChat
{
    public class ChatWindowConfiguration
    {
        public const string KEY_LICENSE_NUMBER = "KEY_LICENCE_NUMBER_FRAGMENT";
        public const string KEY_GROUP_ID = "KEY_GROUP_ID_FRAGMENT";
        public const string KEY_VISITOR_NAME = "KEY_VISITOR_NAME_FRAGMENT";
        public const string KEY_VISITOR_EMAIL = "KEY_VISITOR_EMAIL_FRAGMENT";
        private const string DEFAULT_GROUP_ID = "-1";
        public const string CUSTOM_PARAM_PREFIX = "#LCcustomParam_";

        private readonly string _licenseNumber;
        private readonly string _groupId;
        private readonly string _visitorName;
        private readonly string _visitorEmail;
        private readonly Dictionary<string, string> _customVariables;

        public ChatWindowConfiguration(
                string licenseNumber,
                string groupId,
                string visitorName,
                string visitorEmail,
                Dictionary<string, string> customVariables)
        {
            _licenseNumber = licenseNumber;
            _groupId = groupId;
            _visitorName = visitorName;
            _visitorEmail = visitorEmail;
            _customVariables = customVariables;
        }

        internal Dictionary<string, string> GetParams()
        {
            var dicParams = new Dictionary<string, string>
            {
                { KEY_LICENSE_NUMBER, _licenseNumber },
                { KEY_GROUP_ID, _groupId ?? DEFAULT_GROUP_ID }
            };
            if (!TextUtils.IsEmpty(_visitorName))
                dicParams.Add(KEY_VISITOR_NAME, _visitorName);
            if (!TextUtils.IsEmpty(_visitorEmail))
                dicParams.Add(KEY_VISITOR_EMAIL, _visitorEmail);
            if (_customVariables != null)
            {
                foreach (var item in _customVariables)
                {
                    dicParams.Add(CUSTOM_PARAM_PREFIX + item, item.Key);
                }
            }
            return dicParams;
        }

        public class Builder
        {
            private string _licenseNumber;
            private string _groupId;
            private string _visitorName;
            private string _visitorEmail;
            private Dictionary<string, string> _customParams;

            public ChatWindowConfiguration Build()
            {
                if (TextUtils.IsEmpty(_licenseNumber))
                {
                    throw new IllegalStateException("License Number cannot be null");
                }
                return new ChatWindowConfiguration(_licenseNumber, _groupId, _visitorName, _visitorEmail,
                    _customParams);
            }

            internal Builder SetLicenseNumber(string licenseNumber)
            {
                _licenseNumber = licenseNumber;
                return this;
            }
            internal Builder SetGroupId(string groupId)
            {
                _groupId = groupId;
                return this;
            }
            internal Builder SetVisitorName(string visitorName)
            {
                _visitorName = visitorName;
                return this;
            }
            internal Builder SetVisitorEmail(string visitorEmail)
            {
                _visitorEmail = visitorEmail;
                return this;
            }
            internal Builder SetCustomParams(Dictionary<string, string> customParams)
            {
                _customParams = customParams;
                return this;
            }
        }
    }
}