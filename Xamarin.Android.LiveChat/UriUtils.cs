using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Net;

namespace Xamarin.Android.LiveChat
{
    public class UriUtils
    {
        public static string GetFilePathFromUri(Context context, Uri uri)
        {
            if (IsVersionKitKat() && IsDocumentUri(context, uri))
            {
                return GetFilePathFromDocumentUriKitKat(context, uri);
            }
            else if (IsContentUri(uri))
            {
                return GetDataColumnContentUri(context, uri, null, null);
            }
            else
            {
                return uri.Path;
            }
        }

        public static bool IsExternalStorageDocument(Uri uri) => "com.android.externalstorage.documents".Equals(uri.Authority);
        public static bool IsDownloadsDocument(Uri uri) => "com.android.providers.downloads.documents".Equals(uri.Authority);
        public static bool IsMediaDocument(Uri uri) => "com.android.providers.media.documents".Equals(uri.Authority);

        private static string GetFilePathFromDocumentUriKitKat(Context context, Uri uri)
        {
            if (IsExternalStorageDocument(uri))
            {
                return GetFilePathForExternalStorageDocumentUri(uri);
            }
            else if (IsDownloadsDocument(uri))
            {
                return GetFilePathForDownloadDocumentUri(context, uri);
            }
            else if (IsMediaDocument(uri))
            {
                return GetFilePathFromMediaDocumentUri(context, uri);
            }
            else
            {
                return uri.Path;
            }
        }

        private static string GetFilePathForExternalStorageDocumentUri(Uri uri)
        {
            string documentId = DocumentsContract.GetDocumentId(uri);
            string[] split = documentId.Split(':');
            string uriContentType = split[0];
            string uriId = split[1];
            if ("primary".Equals(uriContentType, System.StringComparison.CurrentCultureIgnoreCase))
            {
                return Environment.ExternalStorageDirectory + "/" + uriId;
            }
            else
            {
                return uri.Path;
            }
        }

        private static string GetDataColumnContentUri(Context context, Uri uri,
            string selection, string[] selectionArgs)
        {
            string column = "_data";
            string[] projection = { column };

            ICursor cursor = null;
            try
            {
                cursor = context.ContentResolver.Query(uri, projection, selection, selectionArgs, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    int columnIndex = cursor.GetColumnIndexOrThrow(column);
                    return cursor.GetString(columnIndex);
                }
            }
            finally
            {
                if (cursor != null)
                {
                    cursor.Close();
                }
            }
            return null;
        }

        private static string GetFilePathForDownloadDocumentUri(Context context, Uri uri)
        {
            string documentId = DocumentsContract.GetDocumentId(uri);
            Uri downloadsContentUri = ContentUris.WithAppendedId(Uri.Parse("content://downloads/public_downloads"),
                System.Convert.ToInt64(documentId));
            return GetDataColumnContentUri(context, downloadsContentUri, null, null);
        }

        private static string GetFilePathFromMediaDocumentUri(Context context, Uri uri)
        {
            string documentId = DocumentsContract.GetDocumentId(uri);
            string[] split = documentId.Split(':');
            string uriContentType = split[0];
            string uriId = split[1];
            Uri contentUri = GetUriForContentType(uriContentType);
            string selection = "_id=?";
            string[] selectionArgs = new string[] { uriId };
            return GetDataColumnContentUri(context, contentUri, selection, selectionArgs);
        }

        private static Uri GetUriForContentType(string uriContentType)
        {
            switch (uriContentType)
            {
                case "image":
                    return MediaStore.Images.Media.ExternalContentUri;
                case "video":
                    return MediaStore.Video.Media.ExternalContentUri;
                case "audio":
                    return MediaStore.Audio.Media.ExternalContentUri;
                default:
                    return null;
            }
        }

        private static bool IsVersionKitKat() => Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat;
        private static bool IsDocumentUri(Context context, Uri uri) => DocumentsContract.IsDocumentUri(context, uri);
        private static bool IsContentUri(Uri uri) => "content".Equals(uri.Scheme, System.StringComparison.CurrentCultureIgnoreCase);
    }
}