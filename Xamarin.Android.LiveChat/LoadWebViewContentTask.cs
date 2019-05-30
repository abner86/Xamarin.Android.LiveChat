using System.Collections.Generic;
using Android.OS;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Android.Net;
using Java.IO;
using Java.Lang;
using Java.Net;
using Newtonsoft.Json.Linq;
using Org.Json;

namespace Xamarin.Android.LiveChat
{
    public class LoadWebViewContentTask : AsyncTask<Dictionary<string, string>, Java.Lang.Object, string>
    {
        private static readonly string URL_STRING = "https://cdn.livechatinc.com/app/mobile/urls.json";
        private static readonly string JSON_CHAT_URL = "chat_url";
        private static readonly string PLACEHOLDER_LICENCE = "{%license%}";
        private static readonly string PLACEHOLDER_GROUP = "{%group%}";
        private readonly WebView mWebView;
        private readonly ProgressBar mProgressBar;
        private readonly TextView mTextView;
        private readonly Button mReloadButton;

        public LoadWebViewContentTask(WebView webView, ProgressBar progressBar, TextView textView, Button reloadButton)
        {
            mWebView = webView;
            mProgressBar = progressBar;
            mTextView = textView;
            mReloadButton = reloadButton;
        }

        protected override void OnPreExecute() => mProgressBar.Visibility = ViewStates.Visible;
        protected override void OnPostExecute(string result)
        {
            if (result != null)
            {
                mWebView.LoadUrl(result);
                mWebView.Visibility = ViewStates.Visible;
                mProgressBar.Visibility = ViewStates.Gone;
            }
            else
            {
                mProgressBar.Visibility = ViewStates.Gone;
                mTextView.Visibility = ViewStates.Visible;
                mReloadButton.Visibility = ViewStates.Visible;
            }
        }
        protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] native_parms)
        {
            return base.DoInBackground(native_parms);
        }
        protected override string RunInBackground(params Dictionary<string, string>[] @params)
        {
            HttpURLConnection uRLConnection = null;
            try
            {
                URL uRL = new URL(URL_STRING);
                uRLConnection = (HttpURLConnection)uRL.OpenConnection();
                uRLConnection.ConnectTimeout = 15000;
                uRLConnection.ReadTimeout = 15000;

                var responseCode = uRLConnection.ResponseCode;
                if (responseCode == HttpStatus.Ok)
                {
                    Java.Lang.StringBuilder stringBuilder = new Java.Lang.StringBuilder();
                    using (BufferedReader bufferedReader = new BufferedReader(new InputStreamReader(uRLConnection.InputStream)))
                    {
                        string line;
                        while ((line = bufferedReader.ReadLine()) != null)
                        {
                            stringBuilder.Append(line);
                        }
                    }
                    JObject jsonResponse = JObject.Parse(stringBuilder.ToString());
                    string chatUrl = jsonResponse[JSON_CHAT_URL].ToString();
                    chatUrl = chatUrl.Replace(PLACEHOLDER_LICENCE, @params[0].GetValueOrDefault(ChatWindowFragment.KEY_LICENCE_NUMBER));
                    chatUrl = chatUrl.Replace(PLACEHOLDER_GROUP, @params[0].GetValueOrDefault(ChatWindowFragment.KEY_GROUP_ID));
                    chatUrl += "&native_platform=android";

                    if (@params[0].GetValueOrDefault(ChatWindowFragment.KEY_VISITOR_NAME) != null)
                    {
                        chatUrl += $"&name={URLEncoder.Encode(@params[0].GetValueOrDefault(ChatWindowFragment.KEY_VISITOR_NAME), "UTF-8").Replace("+", "%20")}";
                    }
                    if (@params[0].GetValueOrDefault(ChatWindowFragment.KEY_VISITOR_EMAIL) != null)
                    {
                        chatUrl += $"&email={URLEncoder.Encode(@params[0].GetValueOrDefault(ChatWindowFragment.KEY_VISITOR_EMAIL), "UTF-8")}";
                    }

                    string customParams = EscapeCustomParams(@params[0], chatUrl);
                    if (!TextUtils.IsEmpty(customParams))
                    {
                        chatUrl += $"&params={customParams}";
                    }
                    if (!chatUrl.StartsWith("http"))
                    {
                        chatUrl = $"https://{chatUrl}";
                    }

                    return chatUrl;
                }
            }
            catch (MalformedURLException e)
            {

                e.PrintStackTrace();
            }
            catch (IOException e)
            {

                e.PrintStackTrace();
            }
            catch (JSONException e)
            {

                e.PrintStackTrace();
            }
            catch (SecurityException e)
            {
                Log.Error("LiveChat Widget", "Missing internet permission!");
                e.PrintStackTrace();
            }
            finally
            {
                if (uRLConnection != null)
                {
                    try
                    {
                        uRLConnection.Disconnect();
                    }
                    catch (Java.Lang.Exception ex)
                    {

                        ex.PrintStackTrace();
                    }
                }
            }

            return null;
        }

        private string EscapeCustomParams(Dictionary<string, string> dictionary, string chatUrl)
        {
            string parameters = string.Empty;
            foreach (string item in dictionary.Keys)
            {
                if (item.StartsWith(ChatWindowFragment.CUSTOM_PARAM_PREFIX))
                {
                    string encodedKey = Uri.Encode(item.Replace(ChatWindowFragment.CUSTOM_PARAM_PREFIX, string.Empty));
                    string encodedValue = Uri.Encode(dictionary.GetValueOrDefault(item));

                    if (!TextUtils.IsEmpty(parameters))
                    {
                        parameters += $"{encodedKey}={encodedValue}";
                    }
                }
            }

            return Uri.Encode(parameters);
        }
    }
}