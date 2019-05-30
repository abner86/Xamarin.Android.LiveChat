using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Net;
using Xamarin.Android.LiveChat.Model;
using static Xamarin.Android.LiveChat.ChatWindowView;

namespace Xamarin.Android.LiveChat
{
    public sealed class ChatWindowFragment : Fragment, IChatWindowEventsListener
    {
        public static readonly string KEY_LICENCE_NUMBER = "KEY_LICENCE_NUMBER_FRAGMENT";
        public static readonly string KEY_GROUP_ID = "KEY_GROUP_ID_FRAGMENT";
        public static readonly string KEY_VISITOR_NAME = "KEY_VISITOR_NAME_FRAGMENT";
        public static readonly string KEY_VISITOR_EMAIL = "KEY_VISITOR_EMAIL_FRAGMENT";
        public static readonly string CUSTOM_PARAM_PREFIX = "#LCcustomParam_";

        private ChatWindowConfiguration configuration;
        private ChatWindowView chatWindow;

        public static ChatWindowFragment NewInstance(object licenceNumber, object groupId)
        {
            return NewInstance(licenceNumber, groupId, null, null, null);
        }

        private static ChatWindowFragment NewInstance(object licenceNumber, object groupId, 
            object visitorName, object visitorEmail)
        {
            return NewInstance(licenceNumber, groupId, visitorName, visitorEmail, null);
        }

        private static ChatWindowFragment NewInstance(object licenceNumber, object groupId, object visitorName,
            object visitorEmail, Dictionary<string, string> customVariables)
        {
            Bundle arguments = new Bundle();
            arguments.PutString(KEY_LICENCE_NUMBER, licenceNumber.ToString() ?? string.Empty);
            arguments.PutString(KEY_GROUP_ID, groupId.ToString() ?? string.Empty);
            if(visitorName != null)
            {
                arguments.PutString(KEY_VISITOR_NAME, visitorName.ToString());
            }
            if (visitorEmail != null)
            {
                arguments.PutString(KEY_VISITOR_EMAIL, visitorEmail.ToString());
            }
            if (customVariables != null)
            {
                foreach (var item in customVariables)
                {
                    arguments.PutString(CUSTOM_PARAM_PREFIX + item, item.Key);
                }
            }

            return new ChatWindowFragment
            {
                Arguments = arguments
            };
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ChatWindowConfiguration.Builder builder = new ChatWindowConfiguration.Builder();
            Dictionary<string, string> customParams = new Dictionary<string, string>();
            if (Arguments != null)
            {
                foreach (var item in Arguments.KeySet())
                {
                    if(KEY_LICENCE_NUMBER.Equals(item))
                    {
                        builder.SetLicenseNumber(Arguments.GetString(KEY_LICENCE_NUMBER));
                    }
                    else if (KEY_GROUP_ID.Equals(item))
                    {
                        builder.SetGroupId(Arguments.GetString(KEY_GROUP_ID));
                    }
                    else if (KEY_VISITOR_NAME.Equals(item))
                    {
                        builder.SetVisitorName(Arguments.GetString(KEY_VISITOR_NAME));
                    }
                    else if (KEY_VISITOR_EMAIL.Equals(item))
                    {
                        builder.SetVisitorEmail(Arguments.GetString(KEY_VISITOR_EMAIL));
                    }
                    else
                    {
                        customParams.Add(item, Arguments.Get(item).ToString());
                    }
                }
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            chatWindow = (ChatWindowView)inflater.Inflate(Resource.Layout.view_chat_window, container, false);
            chatWindow.SetUpWindow(configuration);
            chatWindow.SetUpListener(this);
            chatWindow.Initialize();
            chatWindow.ShowChatWindow();
            return chatWindow;
        }

        public override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            chatWindow.OnActivityResult(requestCode, resultCode, data);
        }

        public void OnChatWindowVisibilityChanged(bool visible)
        {
            if (!visible)
            {
                Activity.OnBackPressed();
            }
        }

        public void OnNewMessage(NewMessage newMessage, bool windowVisible)
        {
            
        }

        public void OnStartFilePickerActivity(Intent intent, int requestCode)
        {
            StartActivityForResult(intent, requestCode);
        }

        public bool HandleUri(Uri uri) => false;
    }
}