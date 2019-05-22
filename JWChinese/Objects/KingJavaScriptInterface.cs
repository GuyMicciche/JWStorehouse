using Android.Annotation;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Webkit;

using Java.Interop;

using Storehouse.Core;

using System;
using System.Drawing;

namespace JWChinese
{
    public class KingJavaScriptInterface : Java.Lang.Object
    {
        private Context context;
        private WebView webview;

        public KingJavaScriptInterface(Context context, WebView webview)
        {
            this.context = context;
            this.webview = webview;
        }

        public KingJavaScriptInterface(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {

        }

        [JavascriptInterface]
        [Export("Annotate")]
        public string Annotate(Java.Lang.String text, bool inLink)
        {

            string result = new Annotator((string)text).Result();
            if (!inLink)
            {
                result = result.Replace("<ruby title=\"", "<ruby onclick=\"annotPopAll(this)\" title=\"");
            }
            return result;
        }

        [JavascriptInterface]
        [Export("Alert")]
        public void Alert(Java.Lang.String text, Java.Lang.String annotation)
        {
            //ContextThemeWrapper wrapper = new ContextThemeWrapper(context, App.FUNCTIONS.GetDialogTheme());
            //StorehouseSuperDialog alert = new StorehouseSuperDialog(wrapper);

            //alert.SetTitleTextColor(Android.Graphics.Color.White);
            //alert.SetTitleBackgroundColor(Android.Resource.Color.Transparent);
            //alert.SetTitle((string)text);
            //alert.SetMessage((string)annotation);
            //alert.Show();

            string english = (string)annotation;
            string chinese = ((string)text).Split(' ')[0];
            string pinyin = (string)text.Replace(chinese + " ", "");

            LearningCharacter character = new LearningCharacter()
            {
                Chinese = chinese,
                Pinyin = pinyin,
                English = english
            };

            App.FUNCTIONS.ShowCharacterLearningDialog(context, character, webview);
        }

        [JavascriptInterface]
        [Export("GetClipboard")]
        public string GetClipboard()
        {
            return ReadClipboard();
        }

        [JavascriptInterface]
        [Export("DisplayLearningDialog")]
        public void DisplayLearningDialog(Java.Lang.String chinese, Java.Lang.String pinyin, Java.Lang.String english)
        {
            LearningCharacter character = new LearningCharacter()
            {
                Chinese = (string)chinese,
                Pinyin = (string)pinyin,
                English = (string)english
            };

            App.FUNCTIONS.ShowCharacterLearningDialog(context, character, webview);
        }

        [JavascriptInterface]
        [Export("StoreHighlight")]
        public void StoreHighlight(Java.Lang.String selectedText, int begin, int end)
        {
            Highlight character = new Highlight()
            {
                SelectedText = (string)selectedText,
                Begin = begin,
                End = end,
                ArticleNumber = App.STATE.SelectedArticle.ToString()
            };

            Console.WriteLine(character.SelectedText + " -> " + character.Begin + " -> " + character.End);
        }

        [TargetApi(Value = 11)]
        public string ReadClipboard()
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.Honeycomb)
            {
                return ((ClipboardManager)context.GetSystemService(Context.ClipboardService)).Text.ToString();
            }

            ClipData clip = ((ClipboardManager)context.GetSystemService(Context.ClipboardService)).PrimaryClip;
            if (clip != null && clip.ItemCount > 0)
            {
                return clip.GetItemAt(0).CoerceToText(context).ToString();
            }

            return "";
        }
    }
}