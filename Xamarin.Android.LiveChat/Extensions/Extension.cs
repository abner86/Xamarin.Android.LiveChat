using Xamarin.Android.LiveChat.Model;

namespace Xamarin.Android.LiveChat.Extensions
{
    public static class Extension
    {
        public static T CastToNewMessage<T>(this Java.Lang.Object obj) where T : NewMessage
        {
            var propInfo = obj.GetType().GetProperty("Instance");
            return propInfo?.GetValue(obj, null) as T;
        }
    }
}