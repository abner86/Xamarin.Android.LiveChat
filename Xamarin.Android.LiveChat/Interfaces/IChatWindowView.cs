
using Android.App;
using Android.Content;

namespace Xamarin.Android.LiveChat.Interfaces
{
    interface IChatWindowView
    {
        void ShowChatWindow();
        void HideChatWindow();
        bool OnBackPressed();
        bool OnActivityResult(int requestCode, Result resultCode, Intent intent);
        bool IsInitialized();
    }
}