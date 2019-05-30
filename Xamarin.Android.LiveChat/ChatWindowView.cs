using System.Text.RegularExpressions;
using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Java.IO;
using Java.Lang;
using Xamarin.Android.LiveChat.Interfaces;
using Xamarin.Android.LiveChat.Model;
using static Android.Views.View;
using AndroidResources = Android;

namespace Xamarin.Android.LiveChat
{
    public class ChatWindowView : FrameLayout, IChatWindowView, IOnClickListener
    {
        internal static WebView webView;
        internal static WebView webViewPopup;
        internal static Context instance;
        internal static LoadWebViewContentTask loadWebViewContentTask;
        internal static TextView statusText;
        internal static Button reloadButton;
        internal static ProgressBar progressBar;
        internal IChatWindowEventsListener chatWindowListener;
        internal static readonly int REQUEST_CODE_FILE_UPLOAD = 21354;
        internal static readonly string TARGET_URL_PREFIX = "secure.livechatinc.com";

        private IValueCallBack<Uri> mUriUploadCallback;
        private IValueCallBack<Uri[]> mUriArrayUploadCallback;
        private ChatWindowConfiguration config;
        private bool initialized;
        private static Activity _activity;

        public static ChatWindowView CreateAndAttachedChatWindowInstance(Activity activity)
        {
            _activity = activity;
            activity.Window.SetSoftInputMode(SoftInput.AdjustResize | SoftInput.StateHidden); 
            ViewGroup contentView = (ViewGroup)activity.Window.DecorView.FindViewById(AndroidResources.Resource.Id.Content);
            var chatWindow = (ChatWindowView)LayoutInflater.From(activity).Inflate(Resource.Layout.view_chat_window, contentView, false);
            contentView.AddView(chatWindow, ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            return chatWindow;
        }

        public ChatWindowView(Context context) : base(context)
        {
            InitView(context);
        }

        public ChatWindowView(Context context, IAttributeSet attribute) : base(context, attribute)
        {
            InitView(context);
        }

        public ChatWindowView(Context context, IAttributeSet attribute, int defStyle) : base(context, attribute, defStyle)
        {
            InitView(context);
        }

        private void InitView(Context context)
        {
            instance = context;
            SetFitsSystemWindows(true);
            Visibility = ViewStates.Gone;
            var inflater = (LayoutInflater)instance.GetSystemService(Context.LayoutInflaterService);
            var view = (ChatWindowView)inflater.Inflate(Resource.Layout.view_chat_window_internal, this, true);
            webView = FindViewById<WebView>(Resource.Id.chat_window_web_view);
            webViewPopup = FindViewById<WebView>(Resource.Id.chat_window_web_view_popup);
            statusText = FindViewById<TextView>(Resource.Id.chat_window_status_text);
            progressBar = FindViewById<ProgressBar>(Resource.Id.chat_window_progress);
            reloadButton = FindViewById<Button>(Resource.Id.chat_window_button);
            reloadButton.SetOnClickListener(this);

            CookieManager cookieManager = CookieManager.Instance;
            cookieManager.SetAcceptCookie(true);
            cookieManager.SetAcceptThirdPartyCookies(webView, true);

            //webView.SetFocusable(ViewFocusability.Focusable);
            webView.Focusable = true;
            WebSettings webSettings = webView.Settings;
            webSettings.JavaScriptEnabled = true;
            webSettings.SetAppCacheEnabled(true);
            webSettings.JavaScriptCanOpenWindowsAutomatically = true;
            webSettings.SetSupportMultipleWindows(true);

            webView.SetWebViewClient(new LCWebViewClient());
            webView.SetWebChromeClient(new LCWebChromeClient());
            webView.RequestFocus(FocusSearchDirection.Down);
            webView.Visibility = ViewStates.Gone;

            webView.SetOnTouchListener(new OnTouchListener());
            webView.AddJavascriptInterface(new ChatWindowJsInterface(this), ChatWindowJsInterface.BRIDGE_OBJECT_NAME);
        }

        internal void OnUiReady()
        {
            Post(() => 
            {
                HideProgressBar();
            });
        }

        private void HideProgressBar()
        {
            progressBar.Visibility = ViewStates.Gone;
        }

        public void SetUpWindow(ChatWindowConfiguration configuration)
        {
            config = configuration;
        }

        public void OnNewMessageReceived(NewMessage @object)
        {
            if (chatWindowListener != null)
            {
                Post(() =>
                {
                    chatWindowListener.OnNewMessage(@object, IsShown);
                });
            }
        }

        public virtual void ShowChatWindow()
        {
            Visibility = ViewStates.Visible;
            if (chatWindowListener != null)
            {
                Post(() =>
                {
                    chatWindowListener.OnChatWindowVisibilityChanged(true);
                });
            }
        }

        public virtual void HideChatWindow()
        {
            Visibility = ViewStates.Gone;
            if (chatWindowListener != null)
            {
                Post(() =>
                {
                    chatWindowListener.OnChatWindowVisibilityChanged(false);
                });
            }
        }

        public virtual bool OnBackPressed()
        {
            if (IsShown)
            {
                HideChatWindow();
                return true;
            }
            return false;
        }

        public virtual bool OnActivityResult(int requestCode, Result resultCode, Intent intent)
        {
            if (requestCode == REQUEST_CODE_FILE_UPLOAD)
            {
                if (resultCode == Result.Ok)
                {
                    ReceiveUploadedUriArray(intent);
                }
                else
                {
                    ResetAllUploadCallbacks();
                }
                return true;
            }
            return false;
        }

        private void ResetAllUploadCallbacks()
        {
            ResetUriUploadCallback();
            ResetUriArrayUploadCallback();
        }

        private void ResetUriArrayUploadCallback()
        {
            if (mUriArrayUploadCallback != null)
            {
                mUriArrayUploadCallback.OnReceiveValue(null);
                mUriArrayUploadCallback = null;
            }
        }

        private void ResetUriUploadCallback()
        {
            if (mUriUploadCallback != null)
            {
                mUriUploadCallback.OnReceiveValue(null);
                mUriUploadCallback = null;
            }
        }

        private void ReceiveUploadedUriArray(Intent data)
        {
            Uri[] uploadedUris;
            try
            {
                uploadedUris = new Uri[] { Uri.Parse(data.DataString) };
            }
            catch (System.Exception e)
            {

                uploadedUris = null;
            }
            mUriArrayUploadCallback.OnReceiveValue(uploadedUris);
            mUriArrayUploadCallback = null;
        }

        private void ReceivedUploadedData(Intent intent)
        {
            if (IsUriArrayUpload())
            {
                ReceiveUploadedUriArray(intent);
            }
            else if (IsVersionPreHoneycomb())
            {
                ReceiveUploadedUriPreHoneyComb(intent);
            }
            else
            {
                ReceiveUploadedUri(intent);
            }
        }

        private void ReceiveUploadedUri(Intent intent)
        {
            Uri uploadedFileUri;
            try
            {
                string uploadedUriFilePath = UriUtils.GetFilePathFromUri(instance, intent.Data);
                File upload = new File(uploadedUriFilePath);
                uploadedFileUri = Uri.FromFile(upload);
            }
            catch (System.Exception)
            {

                uploadedFileUri = null;
            }

            mUriUploadCallback.OnReceiveValue(uploadedFileUri);
            mUriUploadCallback = null;
        }

        private void ReceiveUploadedUriPreHoneyComb(Intent intent)
        {
            Uri upload = intent.Data;
            mUriUploadCallback.OnReceiveValue(upload);
            mUriUploadCallback = null;
        }

        private void ChooseUriToUpload(IValueCallBack<Uri> uriValueCallback)
        {
            ResetAllUploadCallbacks();
            mUriUploadCallback = uriValueCallback;
            StartFileChooserActivity();
        }

        private void ChooseUriArrayToUpload(IValueCallBack<Uri[]> uriArrayValueCallback)
        {
            ResetAllUploadCallbacks();
            mUriArrayUploadCallback = uriArrayValueCallback;
            StartFileChooserActivity();
        }

        private void StartFileChooserActivity()
        {
            if (chatWindowListener != null)
            {
                Intent intent = new Intent(Intent.ActionGetContent);
                intent.AddCategory(Intent.CategoryOpenable);
                intent.SetType("*/*");
                chatWindowListener.OnStartFilePickerActivity(intent, REQUEST_CODE_FILE_UPLOAD);
            }
            else
            {
                Log.Error("ChatWindowView", "You must provide a listener to handle file sharing");
                Toast.MakeText(instance, Resource.String.cant_share_files, ToastLength.Short).Show();
            }
        }

        private bool IsUriArrayUpload()
        {
            return mUriArrayUploadCallback != null;
        }

        private bool IsVersionPreHoneycomb()
        {
            return Build.VERSION.SdkInt < BuildVersionCodes.Honeycomb;
        }

        public virtual bool IsInitialized()
        {
            return initialized;
        }

        public void SetUpListener(IChatWindowEventsListener eventsListener) => chatWindowListener = eventsListener;

        public void OnClick(View v)
        {
            loadWebViewContentTask.Cancel(true);
            webView.Visibility = ViewStates.Gone;
            progressBar.Visibility = ViewStates.Gone;
            statusText.Visibility = ViewStates.Gone;
            reloadButton.Visibility = ViewStates.Gone;

            initialized = false;
            Initialize();
        }

        public void Initialize()
        {
            CheckConfiguration();
            initialized = true;
            loadWebViewContentTask = new LoadWebViewContentTask(webView, progressBar, statusText, reloadButton);
            loadWebViewContentTask.Execute(config.GetParams());
        }

        private void CheckConfiguration()
        {
            if (config == null)
            {
                throw new IllegalStateException("Config must be provided before initialization");
            }
            if (initialized)
            {
                throw new IllegalStateException("Chat Window already initialized");
            }
        }

        protected class LCWebChromeClient : WebChromeClient
        {
            public override bool OnCreateWindow(WebView view, bool isDialog, bool isUserGesture, Message resultMsg)
            {
                webViewPopup = new WebView(instance)
                {
                    VerticalScrollBarEnabled = false,
                    HorizontalScrollBarEnabled = false
                };
                webViewPopup.SetWebViewClient(new LCWebViewClient());
                webViewPopup.Settings.JavaScriptEnabled = true;
                webViewPopup.LayoutParameters = new LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                if (view != null)
                {
                    view.AddView(webViewPopup);
                }
                WebView.WebViewTransport transport = (WebView.WebViewTransport)resultMsg.Obj;
                transport.WebView = webViewPopup;
                resultMsg.SendToTarget();

                return true;
            }
        }

        public interface IChatWindowEventsListener
        {
            void OnChatWindowVisibilityChanged(bool visible);
            void OnNewMessage(NewMessage newMessage, bool windowVisible);
            void OnStartFilePickerActivity(Intent intent, int requestCode);
            bool HandleUri(Uri uri);
        }

        protected class LCWebViewClient : WebViewClient
        {
            private const string Pattern = "https://secure.livechatinc.com(/(licence/)([a-zA-Z0-9_.-]*)(/v2/open_chat.cgi\\?groups=)(-?\\d*\\.{0,1}\\d+)&webview_widget=1&native_platform=android&name=([a-zA-Z]+))";
            private const string facebookPattern = "https://.+facebook.+(/dialog/oauth\\?|/login\\.php\\?|/dialog/return/arbiter\\?).+";

            public override void OnPageFinished(WebView view, string url)
            {
                if (url.StartsWith("https://www.facebook.com/dialog/return/arbiter"))
                {
                    if (webViewPopup != null)
                    {
                        webViewPopup.Visibility = ViewStates.Gone;
                        view.RemoveView(webViewPopup);
                        webViewPopup = null;
                    }
                }
                base.OnPageFinished(view, url);
            }

            public override void OnReceivedError(WebView view, IWebResourceRequest request, WebResourceError error)
            {
                view.Post(() =>
                {
                    progressBar.Visibility = ViewStates.Gone;
                    webView.Visibility = ViewStates.Gone;
                    statusText.Visibility = ViewStates.Visible;
                });
                base.OnReceivedError(view, request, error);
                Log.Error("ChatWindow Widget", $"onReceiveError: {error} request: {request}");
            }

            public override bool ShouldOverrideUrlLoading(WebView view, IWebResourceRequest request)
            {
                Uri uri = request.Url;
                return HandleUri(view, uri);
            }

            private bool HandleUri(WebView view, Uri uri)
            {
                string uriString = uri.ToString();
                string host = uri.Host;
                bool facebookLogin = Regex.IsMatch(uriString, facebookPattern);
                bool liveChat = Regex.IsMatch(uriString, Pattern);
                if (host.Equals(TARGET_URL_PREFIX) || liveChat)
                {
                    if (webViewPopup != null)
                    {
                        webViewPopup.Visibility = ViewStates.Gone;
                        view.RemoveView(webViewPopup);
                        webViewPopup = null;
                    }
                    return false;
                }
                if (facebookLogin) { return false; };
                Intent intent = new Intent(Intent.ActionView);
                instance.StartActivity(intent);
                return true;
            }
        }
    }

    public interface IValueCallBack<T>
    {
        void OnReceiveValue(T value);
    }

    public class OnTouchListener : Java.Lang.Object, IOnTouchListener
    {
        public bool OnTouch(View v, MotionEvent e)
        {
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    break;
                case MotionEventActions.Up:
                    if (!v.HasFocus)
                    {
                        v.RequestFocus(FocusSearchDirection.Down);
                    }
                    break;
            }
            return false;
        }
    }
}