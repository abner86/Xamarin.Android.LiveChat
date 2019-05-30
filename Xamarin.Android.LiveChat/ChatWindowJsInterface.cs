using Android.Util;
using Android.Webkit;
using GoogleGson;
using Newtonsoft.Json;
using Org.Json;
using Xamarin.Android.LiveChat.Extensions;
using Xamarin.Android.LiveChat.Model;

namespace Xamarin.Android.LiveChat
{
    public class ChatWindowJsInterface : Java.Lang.Object
    {
        private readonly ChatWindowView view;
        public static readonly string BRIDGE_OBJECT_NAME = "androidMobileWidget";
        private static readonly string KEY_MESSAGE_TYPE = "messageType";
        private static readonly string TYPE_UI_READY = "uiReady";
        private static readonly string TYPE_HIDE_CHAT_WINDOW = "hideChatWindow";
        private static readonly string TYPE_NEW_MESSAGE = "newMessage";
        public ChatWindowJsInterface(ChatWindowView view) => this.view = view;
        [JavascriptInterface]
        public void PostMessage(string messageJson)
        {
            Log.Info("Interface", $"postMessage: {messageJson}");
            try
            {
                JSONObject jsonObject = new JSONObject(messageJson);
                if (jsonObject != null && jsonObject.Has(KEY_MESSAGE_TYPE))
                {
                    DispatchMessage(jsonObject.GetString(KEY_MESSAGE_TYPE), messageJson);
                }
            }
            catch (JsonException e)
            {

                e.StackTrace.ToString();
            }
        }

        private void DispatchMessage(string messageType, string json)
        {
            if (messageType == TYPE_HIDE_CHAT_WINDOW)
            {
                view.HideChatWindow();
            }
            else if (messageType == TYPE_UI_READY)
            {
                view.OnUiReady();
            }
            else if (messageType == TYPE_NEW_MESSAGE)
            {
                var gson = new GsonBuilder().Create();
                var classType = Java.Lang.Class.FromType(typeof(NewMessage));
                var newMessage = gson.FromJson(json, classType);
                view.OnNewMessageReceived(newMessage.CastToNewMessage<NewMessage>());
            }
        }
    }
}