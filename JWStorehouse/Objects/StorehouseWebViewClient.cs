using System;

using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Webkit;

using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace JWStorehouse
{
    /// <summary>
    /// Custom JW Storehouse WebViewClient
    /// </summary>
    public class StorehouseWebViewClient : WebViewClient
    {
        private bool external;

        public StorehouseWebViewClient(bool external = false)
        {
            this.external = external;
        }

        public override bool ShouldOverrideUrlLoading(WebView view, string url)
        {
            if (!external)
            {
                if (App.FUNCTIONS.ConnectedToNetwork(App.STATE.Context))
                {
                    Console.WriteLine(url);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = 80000;

                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            Dialog dialog = new Dialog(App.STATE.Activity);

                            // If image
                            if (url.Contains("/mp/"))
                            {
                                dialog = App.FUNCTIONS.PresentationUrlDialog(App.STATE.Activity, url);
                            }
                            // If html
                            else
                            {
                                string html = string.Empty;
                                string html2 = string.Empty;
                                string title = string.Empty;
                                string pattern = "(<div id=\"main\">)(.*?)(<div id=\"contentFooter\">)";

                                Stream stream = response.GetResponseStream();
                                StreamReader reader = new StreamReader(stream);
                                html = reader.ReadToEnd();

                                //////////////////////////////////////////////////////////////////////////
                                // ARTICLE TITLE
                                //////////////////////////////////////////////////////////////////////////
                                foreach (Match match in Regex.Matches(html, "(<title>)(.*?)(</title>)", RegexOptions.Singleline))
                                {
                                    title = match.Groups[2].Value.Replace("&mdash;", "—");
                                }

                                //////////////////////////////////////////////////////////////////////////
                                // ARTICLE CONTENT
                                //////////////////////////////////////////////////////////////////////////
                                foreach (Match match in Regex.Matches(html, pattern, RegexOptions.Singleline))
                                {
                                    html = match.Groups[2].Value;
                                    html = html.Replace("/en", "http://m.wol.jw.org/en");

                                    html2 = html;

                                    html = @"<html>
                                                <head>
                                                    <meta name='viewport' content='width=320' />
                                                    <meta http-equiv='Content-Type' content='text/html; charset=utf-8' />
                                                    <link href='css/wol.css' type='text/css' rel='stylesheet' />
                                                </head>
                                                <body class='calibre'>
                                                    <div class='body' id='content'>" + html + @"</div>
                                                    <script src='http://wol.jw.org/js/jquery.min.js'></script>
                                                    <script src='http://wol.jw.org/js/underscore-min.js'></script>
                                                    <script src='http://wol.jw.org/js/wol.modernizr.min.js'></script>
                                                    <script src='http://wol.jw.org/js/startup.js'></script>
                                                    <script src='http://wol.jw.org/js/mediaelement-and-player.min.js'></script>
                                                    <script src='http://wol.jw.org/js/spin.min.js'></script>
                                                    <script src='http://wol.jw.org/js/wol.mobile.min.js'></script>
                                                    <script src='http://wol.jw.org/js/home.js'></script>
                                                </body>
                                            </html>";
                                }

                                //////////////////////////////////////////////////////////////////////////
                                // GET ANNOTATION IF ACTIVATED IN PREFERENCES
                                //////////////////////////////////////////////////////////////////////////
                                if (App.STATE.Preferences.GetBoolean("pinyinReferences", false) && url.Contains("chs"))
                                {
                                    HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create("http://mandarinspot.com/annotate?text=" + App.FUNCTIONS.RemoveHTMLTags(html2));
                                    request2.Timeout = 80000;
                                    try
                                    {
                                        using (HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse())
                                        {

                                            Stream stream2 = response2.GetResponseStream();
                                            StreamReader reader2 = new StreamReader(stream2);
                                            html2 = reader2.ReadToEnd();

                                            string js = string.Empty;
                                            foreach (Match m in Regex.Matches(html2, @"(<script type=""text/javascript"">)(.*?)(</script>)", RegexOptions.Singleline))
                                            {
                                                js += m.Value.Replace("#da0", "#dd0033");
                                            }

                                            html2 = html2.Replace("<br />            <br />	    <br />		        <br />				        ", "");
                                            html2 = html2.Replace("<br />    <br />        <br />            <br />	    <br />				        ", "");
                                            html2 = html2.Replace("<br />				    <br />    <br />						", "<br />");
                                            html2 = html2.Replace("<br />				    <br /><br />    <br /><br />	    <br />				        ", "<p>");
                                            //html2 = html2.Replace("<br />", "");

                                            string pattern2 = @"(<div id=""annotated"">)(.*?)(<div class=""mid"">)";
                                            foreach (Match match in Regex.Matches(html2, pattern2, RegexOptions.Singleline))
                                            {
                                                html2 = @"<html>
                                                            <head>
                                                                <meta name='viewport' content='width=320' />
                                                                <meta http-equiv='Content-Type' content='text/html; charset=utf-8' />
                                                                <style type='text/css'>
                                                                    #tip{border:1px solid #332f2f;background-color:#ccc8c8;padding:4px;font:normal 80% sans-serif,arial;z-index:10001;visibility:hidden;position:absolute;_width:15ex;}
                                                                    .ann{cursor:default;z-index:99;}
                                                                    .iann{color:#535353;text-align:center;white-space:nowrap;display:-moz-inline-box;display:inline-table;display:inline-block;vertical-align:bottom;}
                                                                    .sann{margin:0 0.3ex;}
                                                                    .nann{vertical-align:bottom;}
                                                                    .py{font-size:80%;color:#4477a1;display:table-row;}
                                                                    .zy{font-size:70%;color:#4477a1;display:table-row;}
                                                                    .zh{display:table-row;}
                                                                </style>" + js + 
                                                            @"</head>
                                                            <body>
                                                                <div id='content'>
                                                                <div id='tip' style='text-align: center'></div>
                                                                <div id='annotated'>" + match.Groups[2].Value + @"</div>
                                                            </body>
                                                        </html>";
                                            }

                                            html = html2;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Toast.MakeText(App.STATE.Context, "Sorry, there was an error loading the content. " + ex.Message, ToastLength.Long).Show();
                                    }
                                }

                                // References
                                if (!App.STATE.Preferences.GetBoolean("references", true))
                                {
                                    html = html.Replace("class='fn'", "class='fn' style='display: none;'").Replace("class='mr'", "class='mr' style='display: none;'");
                                }

                                // Load in webview
                                //view.LoadDataWithBaseURL("file:///android_asset/", html, "text/html", "utf-8", null);

                                // Load in dialog
                                dialog = App.FUNCTIONS.PresentationDialog(App.STATE.Activity, title, html);
                            }

                            dialog.Show();

                            // Set dialog width to width of screen
                            WindowManagerLayoutParams layoutParams = new WindowManagerLayoutParams();
                            layoutParams.CopyFrom(dialog.Window.Attributes);
                            layoutParams.Width = WindowManagerLayoutParams.MatchParent;
                            dialog.Window.Attributes = layoutParams;
                        }
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(App.STATE.Context, "Sorry, there was an error loading the content. " + ex.Message, ToastLength.Long).Show();
                    }
                }
                else
                {
                    Toast.MakeText(App.STATE.Context, "Cannot connect to Watchtower ONLINE Library. Consider device connection.", ToastLength.Long).Show();
                }

                return true;
            }
            else
            {
                Intent browserIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(url));
                App.STATE.Activity.StartActivity(browserIntent);

                return base.ShouldOverrideUrlLoading(view, url);
            }
        }

        public override void OnPageFinished(WebView view, string url)
        {
            base.OnPageFinished(view, url);

            //string js = "javascript:KingJavaScriptInterface.ShowAlert();";
            //view.LoadUrl(js);
        }

        public void RunJs(WebView webview, string js)
        {
            webview.LoadUrl("javascript:" + js);
        }

        public string EscapeJsString(string s)
        {
            if (s == null)
            {
                return "";
            }

            return s.Replace("'", "\\'").Replace("\"", "\\\"");
        }

        public void RefreshView(WebView webview)
        {
            //TODO
        }
    }
}