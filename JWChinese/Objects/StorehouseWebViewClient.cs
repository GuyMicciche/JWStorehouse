using System;

using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Webkit;

using System.Net;
using System.IO;
using System.Text.RegularExpressions;

namespace JWChinese
{
    /// <summary>
    /// Custom JW Chinese WebViewClient
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
                if (App.FUNCTIONS.ConnectedToNetwork(App.STATE.Activity))
                {
                    Console.WriteLine(url);
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Timeout = 80000;

                    try
                    {
                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        {
                            // If image
                            if (url.Contains("/mp/"))
                            {
                                App.FUNCTIONS.ShowPresentationDialog(App.STATE.Activity, "Watchtower ONLINE Library", url, external);
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
                                                    <script src='js/init.js'></script>
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
//                                    HttpWebRequest request2 = (HttpWebRequest)WebRequest.Create("http://mandarinspot.com/annotate?text=" + App.FUNCTIONS.RemoveHTMLTags(html2));
//                                    request2.Timeout = 80000;
//                                    try
//                                    {
//                                        using (HttpWebResponse response2 = (HttpWebResponse)request2.GetResponse())
//                                        {

//                                            Stream stream2 = response2.GetResponseStream();
//                                            StreamReader reader2 = new StreamReader(stream2);
//                                            html2 = reader2.ReadToEnd();

//                                            string js = string.Empty;
//                                            foreach (Match m in Regex.Matches(html2, @"(<script type=""text/javascript"">)(.*?)(</script>)", RegexOptions.Singleline))
//                                            {
//                                                js += m.Value.Replace("#da0", "#dd0033");
//                                            }

//                                            html2 = html2.Replace("<br />            <br />	    <br />		        <br />				        ", "");
//                                            html2 = html2.Replace("<br />    <br />        <br />            <br />	    <br />				        ", "");
//                                            html2 = html2.Replace("<br />				    <br />    <br />						", "<br />");
//                                            html2 = html2.Replace("<br />				    <br /><br />    <br /><br />	    <br />				        ", "<p>");
//                                            //html2 = html2.Replace("<br />", "");

//                                            string pattern2 = @"(<div id=""annotated"">)(.*?)(<div class=""mid"">)";
//                                            foreach (Match match in Regex.Matches(html2, pattern2, RegexOptions.Singleline))
//                                            {
//                                                html2 = @"<html>
//                                                            <head>
//                                                                <meta name='viewport' content='width=320' />
//                                                                <meta http-equiv='Content-Type' content='text/html; charset=utf-8' />
//                                                                <style type='text/css'>
//                                                                    #tip{border:1px solid #332f2f;background-color:#ccc8c8;padding:4px;font:normal 80% sans-serif,arial;z-index:10001;visibility:hidden;position:absolute;_width:15ex;}
//                                                                    .ann{cursor:default;z-index:99;}
//                                                                    .iann{color:#535353;text-align:center;white-space:nowrap;display:-moz-inline-box;display:inline-table;display:inline-block;vertical-align:bottom;}
//                                                                    .sann{margin:0 0.3ex;}
//                                                                    .nann{vertical-align:bottom;}
//                                                                    .py{font-size:80%;color:#4477a1;display:table-row;}
//                                                                    .zy{font-size:70%;color:#4477a1;display:table-row;}
//                                                                    .zh{display:table-row;}
//                                                                </style>" + js + 
//                                                            @"</head>
//                                                            <body>
//                                                                <div id='content'>
//                                                                <div id='tip' style='text-align: center'></div>
//                                                                <div id='annotated'>" + match.Groups[2].Value + @"</div>
//                                                            </body>
//                                                        </html>";
//                                            }

//                                            html = html2;
//                                        }
//                                    }
//                                    catch (Exception ex)
//                                    {
//                                        Toast.MakeText(App.STATE.Activity, "Sorry, there was an error loading the content. " + ex.Message, ToastLength.Long).Show();
//                                    }

                                    external = true;
                                }

                                // References
                                if (!App.STATE.Preferences.GetBoolean("references", true))
                                {
                                    html = html.Replace("class='fn'", "class='fn' style='display: none;'").Replace("class='mr'", "class='mr' style='display: none;'");
                                }

                                // Load in dialog
                                App.FUNCTIONS.ShowPresentationDialog(App.STATE.Activity, title, html, external);
                            }                            
                        }
                    }
                    catch (Exception ex)
                    {
                        Toast.MakeText(App.STATE.Activity, "Sorry, there was an error loading the content. " + ex.Message, ToastLength.Long).Show();
                    }
                }
                else
                {
                    Toast.MakeText(App.STATE.Activity, "Cannot connect to Watchtower ONLINE Library. Consider device connection.", ToastLength.Long).Show();
                }

                external = false;

                return true;
            }
            else
            {
                Intent browserIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(url));
                App.STATE.Activity.StartActivity(browserIntent);

                external = false;

                return base.ShouldOverrideUrlLoading(view, url);
            }
        }

        public override void OnPageFinished(WebView view, string url)
        {
            //string js = "javascript:KingJavaScriptInterface.ShowAlert();";
            //view.LoadUrl(js);

            if (App.STATE.Preferences.GetBoolean("pinyinWol", false) || external)
            {
                view.LoadUrl(@"javascript:
                var leaveTags = ['SCRIPT', 'STYLE', 'TITLE', 'TEXTAREA', 'OPTION'], stripTags = ['WBR'];           
                function annotPopAll(e) {
	                function f(c) {
		                var i = 0,
		                r = '',
		                cn = c.childNodes;
		                for (; i < cn.length; i++)
			                r += (cn[i].firstChild ? f(cn[i]) : (cn[i].nodeValue ? cn[i].nodeValue : ''));
		                return r;
	                }
	                KingJavaScriptInterface.Alert(f(e.firstChild) + ' ' + f(e.firstChild.nextSibling), e.title)
                };
                function HTMLSizeChanged(callback) {
	                var getLen = function (w) {
		                var r = 0;
		                if (w.frames && w.frames.length) {
			                var i;
			                for (i = 0; i < w.frames.length; i++)
				                r += getLen(w.frames[i])
		                }
		                if (w.document && w.document.body && w.document.body.innerHTML)
			                r += w.document.body.innerHTML.length;
		                return r
	                };
	                var curLen = getLen(window),
	                stFunc = function () {
		                window.setTimeout(tFunc, 300)
	                },
	                tFunc = function () {
		                if (getLen(window) == curLen)
			                stFunc();
		                else
			                callback()
	                };
	                stFunc()
                }
                function all_frames_docs(c) {
	                var f = function (w) {
		                if (w.frames && w.frames.length) {
			                var i;
			                for (i = 0; i < w.frames.length; i++)
				                f(w.frames[i])
		                }
		                c(w.document)
	                };
	                f(window)
                }
                function tw0() {
	                all_frames_docs(function (d) {
		                walk(d, d, false)
	                })
                }
                function annotScan() {
	                tw0();
	                all_frames_docs(function (d) {
		                if (d.rubyScriptAdded == 1 || !d.body)
			                return;
		                var e = d.createElement('span');
                        e.innerHTML = '<style>ruby{display:inline-table;}ruby *{display: inline;line-height:1.0;text-indent:0;text-align:center;white-space:nowrap;}rb{display:table-row-group;font-size: 100%;}rt{display:table-header-group;font-size:100%;line-height:1.1; }</style>';
		                d.body.insertBefore(e, d.body.firstChild);
		                var wk = navigator.userAgent.indexOf('WebKit/');
		                if (wk > -1 && navigator.userAgent.slice(wk + 7, wk + 12) > 534) {
			                var rbs = document.getElementsByTagName('rb');
			                for (var i = 0; i < rbs.length; i++)
				                rbs[i].innerHTML = '&#8203;' + rbs[i].innerHTML + '&#8203;'
		                }
		                d.rubyScriptAdded = 1
	                });
	                HTMLSizeChanged(annotScan)
                }
                function walk(n, document, inLink) {
	                var c = n.firstChild;
	                while (c) {
		                var cNext = c.nextSibling;
		                if (c.nodeType == 1 && stripTags.indexOf(c.nodeName) != -1) {
			                var ps = c.previousSibling;
			                while (c.firstChild) {
				                var tmp = c.firstChild;
				                c.removeChild(tmp);
				                n.insertBefore(tmp, c);
			                }
			                n.removeChild(c);
			                if (ps && ps.nodeType == 3 && ps.nextSibling && ps.nextSibling.nodeType == 3) {
				                ps.nodeValue += ps.nextSibling.nodeValue;
				                n.removeChild(ps.nextSibling)
			                }
			                if (cNext && cNext.nodeType == 3 && cNext.previousSibling && cNext.previousSibling.nodeType == 3) {
				                cNext.previousSibling.nodeValue += cNext.nodeValue;
				                var tmp = cNext;
				                cNext = cNext.previousSibling;
				                n.removeChild(tmp)
			                }
		                }
		                c = cNext;
	                }
	                c = n.firstChild;
	                while (c) {
		                var cNext = c.nextSibling;
		                switch (c.nodeType) {
		                case 1:
			                if (leaveTags.indexOf(c.nodeName) == -1 && c.className != '_adjust0')
				                walk(c, document, inLink || (c.nodeName == 'A' && c.href));
			                break;
		                case 3: {
				                var nv = KingJavaScriptInterface.Annotate(c.nodeValue, inLink);
				                if (nv != c.nodeValue) {
					                var newNode = document.createElement('span');
					                newNode.className = '_adjust0';
					                n.replaceChild(newNode, c);
					                newNode.innerHTML = nv;
				                }
			                }
		                }
		                c = cNext
	                }
                }
                annotScan()");
            }

            external = false;
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