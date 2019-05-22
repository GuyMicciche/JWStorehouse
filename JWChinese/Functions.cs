using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Webkit;
using Android.Widget;

using MaterialDialogs;

using Parse;

using SQLite;

using Storehouse.Core;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression.Zip;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace JWChinese
{
    public class Functions : Application
    {
        public Functions()
        {

        }

        #region LANGUAGE Section

        //////////////////////////////////////////////////////////////////////////
        // LANGUAGE Section
        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get available languages from languages.xml
        /// </summary>
        /// <param name="activity">Current Activity</param>
        /// <returns>List of available languages</returns>
        public List<Language> GetAvailableLanguages(Activity activity)
        {
            string xml = string.Empty;
            List<Language> languages = new List<Language>();

            Stream stream = activity.Assets.Open("languages.xml");
            using (StreamReader reader = new StreamReader(stream))
            {
                xml = reader.ReadToEnd();
            }

            XDocument doc = XDocument.Parse(xml);
            foreach (XElement element in doc.Descendants("l"))
            {
                languages.Add(new Language
                {
                    R = element.Attribute("r").Value,
                    Lp = element.Attribute("c").Value,
                    EnglishName = element.Attribute("n").Value,
                    LanguageName = element.Attribute("ln").Value,
                    Name = element.Value
                });
            }

            return languages;
        }

        /// <summary>
        /// Get all Bible books by language
        /// </summary>
        /// <param name="language">Language to get Bible books from</param>
        /// <returns>List of Bible books</returns>
        public List<BibleBook> GetAllBibleBooks(string language)
        {
            List<BibleBook> books = new List<BibleBook>();

            string lang = string.Empty;
            if (language.Contains("English"))
            {
                lang = "english";
            }
            else if (language.Contains("Simplified"))
            {
                lang = "chinese";
            }
            else if (language.Contains("Pinyin"))
            {
                lang = "pinyin";
            }

            string xml = string.Empty;
            Stream stream = App.STATE.Activity.Assets.Open("names.xml");
            using (StreamReader reader = new StreamReader(stream))
            {
                xml = reader.ReadToEnd();
            }

            XDocument doc = XDocument.Parse(xml);
            foreach (XElement element in doc.Descendants("book"))
            {
                books.Add(new BibleBook
                {
                    Number = element.ElementsBeforeSelf().Count().ToString(),
                    Name = (lang == "pinyin") ? (string)element.Element("pinyin") + "\n" + (string)element.Element("chinese") : (string)element.Element(lang),
                    ShortName = (lang == "pinyin") ? (string)element.Element("pinyin").Attribute("short") + "\n" + (string)element.Element("chinese").Attribute("short") : (string)element.Element(lang).Attribute("short"),
                    Abbreviation = (lang == "pinyin") ? (string)element.Element("pinyin").Attribute("abbr") + "\n" + (string)element.Element("chinese").Attribute("abbr") : (string)element.Element(lang).Attribute("abbr"),
                });
            }

            return books;
        }

        /// <summary>
        /// Get all Insight groups by language
        /// </summary>
        /// <param name="language">Language to get Insight groups from</param>
        /// <returns>List of Insight groups</returns>
        public List<string> GetAllInsightGroups(string language)
        {
            List<string> groups = new List<string>();

            string[] english = { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "Y", "Z", "Supplement" };
            string[] chinese = { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "W", "X", "Y", "Z", "补充资料" };

            if (language.Contains("English"))
            {
                groups = english.ToList();
            }
            else if (language.Contains("Simplified"))
            {
                groups = chinese.ToList();
            }
            else if (language.Contains("Pinyin"))
            {
                groups = chinese.ToList();
            }

            return groups;
        }
        
        /// <summary>
        /// Get all available publications
        /// </summary>
        /// <param name="pubsOnly">If true, ignores Bible, Insight, and Daily Text</param>
        /// <returns>Available publications</returns>
        public List<WOLPublication> GetAllWOLPublications(bool pubsOnly = false)
        {
            List<WOLPublication> pubs = new List<WOLPublication>();

            string xml = string.Empty;
            Stream stream = App.STATE.Activity.Assets.Open("names.xml");
            using (StreamReader reader = new StreamReader(stream))
            {
                xml = reader.ReadToEnd();
            }

            XDocument doc = XDocument.Parse(xml);

            IEnumerable<XElement> elements;
            
            // Ignore Insight and Daily Text
            if(pubsOnly)
            {
                elements = doc.Descendants("pub").Where(e => !e.Attribute("code").Value.Equals("it") && !e.Attribute("code").Value.Equals("es"));
            }
            // All publications
            else
            {
                elements = doc.Descendants("pub");
            }

            foreach (XElement element in elements)
            {
                pubs.Add(new WOLPublication
                {
                    Code = element.Attribute("code").Value,
                    Number = element.Attribute("number").Value,
                    Image = element.Attribute("img").Value,
                    EnglishNameShort = element.Element("english").Attribute("short").Value,
                    EnglishName = element.Element("english").Value,
                    ChineseNameShort = element.Element("chinese").Attribute("short").Value,
                    ChineseName = element.Element("chinese").Value,
                    PinyinNameShort = element.Element("pinyin").Attribute("short").Value,
                    PinyinName = element.Element("pinyin").Value,
                    PinyinChineseNameShort = element.Element("pinyin").Attribute("short").Value + "\n" + element.Element("chinese").Attribute("short").Value,
                    PinyinChineseName = element.Element("pinyin").Value + "\n" + element.Element("chinese").Value,
                });
            }

            return pubs;
        }

        /// <summary>
        /// Get all available publication names
        /// </summary>
        /// <param name="language">Language to get publication names from</param>
        /// <param name="isShort">If true, return short names</param>
        /// <param name="byCode">If true, return publication codes</param>
        /// <returns>List of publication names</returns>
        public List<string> GetAllPublicationNames(string language, bool isShort = false, bool byCode = false)
        {
            List<string> pubs = new List<string>();

            if (byCode)
            {
                pubs = GetAllWOLPublications(true).Select(p => p.Code).ToList();
            }
            else
            {
                if (language.Contains("English"))
                {

                    if (isShort)
                    {
                        pubs = GetAllWOLPublications(true).Select(p => p.EnglishNameShort).ToList();
                    }
                    else
                    {
                        pubs = GetAllWOLPublications(true).Select(p => p.EnglishName).ToList();
                    }
                }
                else if (language.Contains("Simplified"))
                {
                    if (isShort)
                    {
                        pubs = GetAllWOLPublications(true).Select(p => p.ChineseNameShort).ToList();
                    }
                    else
                    {
                        pubs = GetAllWOLPublications(true).Select(p => p.ChineseName).ToList();
                    }
                }
                else if (language.Contains("Pinyin"))
                {
                    if (isShort)
                    {
                        pubs = GetAllWOLPublications(true).Select(p => p.PinyinChineseNameShort).ToList();
                    }
                    else
                    {
                        pubs = GetAllWOLPublications(true).Select(p => p.PinyinChineseName).ToList();
                    }
                }
            }

            return pubs;
        }

        /// <summary>
        /// Get the full name of a publication by code
        /// </summary>
        /// <param name="language">Language to get publication name from</param>
        /// <param name="code">Code to get publication name from</param>
        /// <param name="isShort">If true, returns short name</param>
        /// <returns></returns>
        public string GetPublicationName(string language, string code, bool isShort = false)
        {
            string name = string.Empty;

            if (language.Contains("English"))
            {
                if(isShort)
                {
                    name = GetAllWOLPublications().Single(p => p.Code.Equals(code)).EnglishNameShort;
                }
                else
                {
                    name = GetAllWOLPublications().Single(p => p.Code.Equals(code)).EnglishName;
                }
            }
            else if (language.Contains("Simplified"))
            {
                if (isShort)
                {
                    name = GetAllWOLPublications().Single(p => p.Code.Equals(code)).ChineseNameShort;
                }
                else
                {
                    name = GetAllWOLPublications().Single(p => p.Code.Equals(code)).ChineseName;
                }
            }
            else if (language.Contains("Pinyin"))
            {
                if(isShort)
                {
                    name = GetAllWOLPublications().Single(p => p.Code.Equals(code)).PinyinNameShort;
                    name += "\n" + GetAllWOLPublications().Single(p => p.Code.Equals(code)).ChineseNameShort;
                }
                else
                {
                    name = GetAllWOLPublications().Single(p => p.Code.Equals(code)).PinyinName;
                    name += "\n" + GetAllWOLPublications().Single(p => p.Code.Equals(code)).ChineseName;
                }
            }

            return name;
        }
                
        /// <summary>
        /// Get publication code from number
        /// </summary>
        /// <param name="number">Publication number to get code from</param>
        /// <returns>Publication code</returns>
        public string GetPublicationCode(string number)
        {
            string xml = string.Empty;

            Stream stream = App.STATE.Activity.Assets.Open("names.xml");
            using (StreamReader reader = new StreamReader(stream))
            {
                xml = reader.ReadToEnd();
            }

            XDocument doc = XDocument.Parse(xml);

            return doc.Descendants("pub").Single(p => p.Attribute("number").Value.Equals(number)).Attribute("code").Value;
        }

        #endregion

        #region DIALOG Section

        //////////////////////////////////////////////////////////////////////////
        // DIALOG Section
        //////////////////////////////////////////////////////////////////////////
        public void ShowPresentationDialog(Context context, string title, string content, bool external = false)
        {
            LayoutInflater inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            View view = inflater.Inflate(Resource.Layout.DialogWebView, null);
            WebView presentationWebView = view.FindViewById<WebView>(Resource.Id.dialogWebView);

            StorehouseWebViewClient client = new StorehouseWebViewClient(external);
            presentationWebView.SetWebViewClient(client);
            presentationWebView.Settings.JavaScriptEnabled = true;
            presentationWebView.Settings.BuiltInZoomControls = true;
            presentationWebView.VerticalScrollBarEnabled = false;
            presentationWebView.Settings.DefaultFontSize = GetWebViewTextSize(App.STATE.SeekBarTextSize);

            if(content.StartsWith("http"))
            {
                presentationWebView.LoadUrl(content);
            }
            else
            {
                presentationWebView.LoadDataWithBaseURL("file:///android_asset/", content, "text/html", "utf-8", null);
            }

            MaterialDialog dialog = null;
            MaterialDialog.Builder popup = new MaterialDialog.Builder(context);
            popup.SetCustomView(view, false);
            popup.SetNegativeText("X", (o, args) =>
            {
                // Close dialog
            });

            App.STATE.Activity.RunOnUiThread(() =>
            {
                dialog = popup.Show();

                // Set dialog width to width of screen
                WindowManagerLayoutParams layoutParams = new WindowManagerLayoutParams();
                layoutParams.CopyFrom(dialog.Window.Attributes);
                layoutParams.Width = WindowManagerLayoutParams.MatchParent;
                dialog.Window.Attributes = layoutParams;
            });
        }

        public void ShowCharacterLearningDialog(Context context, LearningCharacter character, WebView webview)
        {
            SQLiteConnection database = new SQLiteConnection(App.LearningCharactersPath);
            database.CreateTable<LearningCharacter>();

            var table = database.Table<LearningCharacter>();
            bool exists = table.Any(x => x.Chinese.Equals(character.Chinese) && x.Pinyin.Equals(character.Pinyin));
            
            string buttonTitle = (exists) ? "REMOVE" : "ADD";

            MaterialDialog.Builder popup = new MaterialDialog.Builder(context);
            popup.SetTitle(character.Chinese + " " + character.Pinyin);
            popup.SetContent(character.English);
            popup.SetPositiveText(buttonTitle, (o, e) =>
            {
                if (!exists)
                {
                    database.Insert(character);
                }
                if (exists)
                {
                    var query = (from c in table
                                 where (c.Chinese.ToLower().Equals(character.Chinese.ToLower()) && c.Pinyin.ToLower().Equals(character.Pinyin.ToLower()))
                                 select c).ToList().FirstOrDefault();

                    database.Delete(query);
                }
                string js = "javascript:ToggleEnglish();";
                webview.LoadUrl(js);
            });

            App.STATE.Activity.RunOnUiThread(() =>
            {
                popup.Show();
            });
        }

        public AlertDialog UserSyncDialog(Context context)
        {
            ContextThemeWrapper wrapper = new ContextThemeWrapper(context, GetDialogTheme());
            StorehouseSuperDialog dialog = new StorehouseSuperDialog(wrapper);

            LayoutInflater inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            View layout = inflater.Inflate(Resource.Layout.DialogUserSyncView, null);
            TextView username = layout.FindViewById<TextView>(Resource.Id.parseUserName);
            TextView password = layout.FindViewById<TextView>(Resource.Id.parsePassword);

            dialog.SetTitleTextColor(Color.White);
            dialog.SetTitleBackgroundColor(Android.Resource.Color.Transparent);
            dialog.SetTitle("User Sync");
            dialog.SetMessageTextSize(22);

            if (ParseUser.CurrentUser != null)
            {
                dialog.SetView(null);
                dialog.SetNeutralButton("Log Out", (o, e) =>
                {
                    ParseUser.LogOut();
                });
            }
            else
            {
                dialog.SetView(layout);
            }

            dialog.SetPositiveButton("Sync Up", (o, e) =>
            {
                ParseUserSync(username.Text, password.Text, "up");
            });

            dialog.SetNegativeButton("Sync Down", (o, e) =>
            {
                ParseUserSync(username.Text, password.Text, "down");
            });

            return dialog.Show();
        }

        private void ParseUserSync(string username, string password, string syncType)
        {
            Console.WriteLine("Take 1");

            UserSignUp(username, password);

            Console.WriteLine("Take 2");

            UserLogIn(username, password);

            Console.WriteLine("Take 3");
        }

        private async void UserLogIn(string username, string password)
        {
            try
            {
                await ParseUser.LogInAsync(username, password);
            }
            catch (Exception e)
            {
                MessageBox(App.STATE.Activity, "Log In Error", e.Message);
            }
        }

        private async void UserSignUp(string username, string password)
        {
            ParseUser user = new ParseUser()
            {
                Username = username,
                Password = password
            };

            try
            {
                await user.SignUpAsync();
            }
            catch (Exception e)
            {
                MessageBox(App.STATE.Activity, "Sign Up Error", e.Message);
            }
        }

        public AlertDialog MessageBox(Context context, string title, string message, EventHandler<DialogClickEventArgs> ok = null)
        {
            ContextThemeWrapper wrapper = new ContextThemeWrapper(context, GetDialogTheme());
            StorehouseSuperDialog dialog = new StorehouseSuperDialog(wrapper);

            dialog.SetTitleTextColor(Color.White);
            dialog.SetTitleBackgroundColor(Android.Resource.Color.Transparent);
            dialog.SetIcon(Resource.Drawable.Icon);
            dialog.SetTitle(title);
            dialog.SetMessage(message);
            dialog.SetPositiveButton("Ok", ok);

            return dialog.Show();
        }

        #endregion

        #region OTHER Section

        //////////////////////////////////////////////////////////////////////////
        // OTHER Section
        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Extracts a database *.db file from Apk Expansion File
        /// </summary>
        /// <param name="database">Database to extract</param>
        /// <param name="context">Context</param>
        /// <param name="mainExpansionFileVersion">Apk Expansion File version. If 0, will return current Version Code</param>
        public void ExtractDatabase(string database, Context context, int mainExpansionFileVersion = 0)
        {
            int version = 0;            
            if(mainExpansionFileVersion == 0)
            {
                version = ((PackageInfo)context.PackageManager.GetPackageInfo(context.PackageName, 0)).VersionCode;
            }
            else
            {
                version = mainExpansionFileVersion;
            }

            // Database file
            ExpansionZipFile expansion = ApkExpansionSupport.GetApkExpansionZipFile(context, version, version);
            ZipFileEntry entry = expansion.GetEntry(database);
            ZipFile zip = new ZipFile(entry.ZipFileName);

            // Extract database from stupid obb file
            string dbPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), database);
            using (BinaryReader br = new BinaryReader(zip.ReadFile(entry)))
            {
                using (BinaryWriter bw = new BinaryWriter(new FileStream(dbPath, FileMode.Create)))
                {
                    byte[] buffer = new byte[2048];
                    int len = 0;

                    while ((len = br.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        bw.Write(buffer, 0, len);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the global WebView size
        /// </summary>
        /// <param name="multiplier">Value to multiply by</param>
        /// <returns></returns>
        public int GetWebViewTextSize(int multiplier)
        {
            int size = (multiplier * App.STATE.TextSizeMultiplier) + App.STATE.TextSizeBase;

            return size;
        }

        /// <summary>
        /// Get Dialog theme style from Style Resource
        /// </summary>
        /// <returns>Theme from Style Resource</returns>
        public int GetDialogTheme()
        {
            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.IceCreamSandwich)
            {
                return Resource.Style.Theme_Storehouse_AlertDialogStyle;
            }
            else if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Honeycomb)
            {
                return Resource.Style.Theme_Storehouse_AlertDialogStyle;
                //return Android.Resource.Style.ThemeHoloLightDialog;
            }
            else
            {
                return Resource.Style.Theme_Storehouse_AlertDialogStyle;
                //return Android.Resource.Style.ThemeDialog;
            }
        }

        public void SetActionBarDrawable(ActionBarActivity activity)
        {
            // Set background of NavigationDrawer
            //Color color = activity.Resources.GetColor(Resource.Color.storehouse_king_purple);
            //Drawable background = activity.Resources.GetDrawable(Resource.Drawable.actionbar_background);
            //background.SetColorFilter(color, PorterDuff.Mode.Multiply);
            //activity.SupportActionBar.SetBackgroundDrawable(background);
        }

        /// <summary>
        /// Formats DateTime to yyyy.M.d
        /// </summary>
        /// <param name="date">DateTime to format</param>
        /// <returns></returns>
        public string FormatDateTime(DateTime date)
        {
            return date.ToString(@"yyyy.M.d");
        }

        /// <summary>
        /// Check if connected to network
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public bool ConnectedToNetwork(Context context)
        {
            bool connected = false;

            ConnectivityManager connectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
            if (connectivityManager != null)
            {
                bool mobileNetwork = false;
                bool wifiNetwork = false;
                bool wimaxNetwork = false;

                bool mobileNetworkConnected = false;
                bool wifiNetworkConnected = false;
                bool wimaxNetworkConnected = false;

                NetworkInfo mobileInfo = connectivityManager.GetNetworkInfo(ConnectivityType.Mobile);
                NetworkInfo wifiInfo = connectivityManager.GetNetworkInfo(ConnectivityType.Wifi);
                NetworkInfo wimaxInfo = connectivityManager.GetNetworkInfo(ConnectivityType.Wimax);

                if (mobileInfo != null)
                {
                    mobileNetwork = mobileInfo.IsAvailable;
                    //Console.WriteLine("Is mobile available?  " + mobileNetwork);
                }

                if (wifiInfo != null)
                {
                    wifiNetwork = wifiInfo.IsAvailable;
                    //Console.WriteLine("Is WiFi available?  " + wifiNetwork);
                }

                if (wimaxInfo != null)
                {
                    wimaxNetwork = wimaxInfo.IsAvailable;
                    //Console.WriteLine("Is WiMAX available?  " + wimaxNetwork);
                }

                if (wifiNetwork || mobileNetwork || wimaxNetwork)
                {
                    mobileNetworkConnected = (mobileInfo != null) ? mobileInfo.IsConnectedOrConnecting : false;
                    wifiNetworkConnected = (wifiInfo != null) ? wifiInfo.IsConnectedOrConnecting : false;
                    wimaxNetworkConnected = (wimaxInfo != null) ? wimaxInfo.IsConnectedOrConnecting : false;
                }

                connected = (mobileNetworkConnected || wifiNetworkConnected || wimaxNetworkConnected);

                //Console.WriteLine("Is mobile connected?  " + mobileNetworkConnected);
                //Console.WriteLine("Is WiFi connected?  " + wifiNetworkConnected);
                //Console.WriteLine("Is WiMAX connected?  " + wimaxNetworkConnected);
            }

            Console.WriteLine("Is this device connected?  " + connected);

            return connected;
        }

        /// <summary>
        /// Remove digits from a string
        /// </summary>
        /// <param name="text">String to remove digits from</param>
        /// <returns></returns>
        public string RemoveDigits(string text)
        {
            return Regex.Replace(text, @"\d", "");
        }

        /// <summary>
        /// Remove HTML tags from string
        /// </summary>
        /// <param name="source">HTML text to remove tags from</param>
        /// <returns></returns>
        public string RemoveHTMLTags(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex).Replace("&nbsp;", " ");
        }

        #endregion
    }
}