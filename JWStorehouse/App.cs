using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Preferences;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Android.Database;
using Android.Graphics;
using Android.Text;
using Java.Lang.Reflect;

namespace JWStorehouse
{
    [Application]
    public class App : Application
    {
        //////////////////////////////////////////////////////////////////////////
        // EVENTS Section
        //////////////////////////////////////////////////////////////////////////

        /************************************************************************/
        /* Language listener                                                    */
        /************************************************************************/
        public delegate void LanguageChangedEventHandler(object sender, LanguageChangedArgs e);
        public class LanguageChangedArgs : EventArgs
        {
            private string language;

            public LanguageChangedArgs(string language)
            {
                this.language = language;
            }

            public string Language
            {
                get
                {
                    return language;
                }
            }
        }

        public event LanguageChangedEventHandler LanguageChanged;
        protected virtual void OnLanguageChanged(LanguageChangedArgs e)
        {
            LanguageChanged(this, e);
        }

        private string language;
        public string Language
        {
            get
            {
                return this.language;
            }
            set
            {
                this.language = value;

                LanguageChangedArgs args = new LanguageChangedArgs(value);
                this.OnLanguageChanged(args);
            }
        }

        /************************************************************************/
        /* Article Navigation listener                                          */
        /************************************************************************/
        public delegate void ArticleChangedEventHandler(object sender, ArticleChangedArgs e);
        public class ArticleChangedArgs : EventArgs
        {
            private NavStruct selectedArticle;

            public ArticleChangedArgs(NavStruct selectedArticle)
            {
                this.selectedArticle = selectedArticle;
            }

            public NavStruct SelectedArticle
            {
                get
                {
                    return selectedArticle;
                }
            }
        }

        public event ArticleChangedEventHandler ArticleChanged;
        protected virtual void OnArticleChanged(ArticleChangedArgs e)
        {
            ArticleChanged(this, e);
        }

        private NavStruct selectedArticle;
        public NavStruct SelectedArticle
        {
            get
            {
                return this.selectedArticle;
            }
            set
            {
                this.selectedArticle = value;

                ArticleChangedArgs args = new ArticleChangedArgs(value);
                this.OnArticleChanged(args);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        // PRE INITIALIZATION Section
        //////////////////////////////////////////////////////////////////////////

        static readonly App State = new App();
        public static App STATE
        {
            get
            {
                return App.State;
            }
        }

        static readonly Functions Functions = new Functions();
        public static Functions FUNCTIONS
        {
            get
            {
                return App.Functions;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        // PROPERTIES Section
        //////////////////////////////////////////////////////////////////////////

        public Context Context;
        public Activity Activity;

        public bool Swapped = false;

        public List<Library> Libraries;

        public Stack<string> PrimaryWebViewStack;
        public Stack<string> SecondaryWebViewStack;

        public Library CurrentLibrary = Library.None;

        public ScreenMode CurrentScreenMode = ScreenMode.Duel;

        public int CurrentArticleGroup;

        public int SeekBarTextSize = 0;
        public int TextSizeMultiplier = 3;
        public int TextSizeBase = 16;

        public float[] WebviewWeights = new float[2] { 1, 1 };

        public ISharedPreferences Preferences;

        public Language PrimaryLanguage;
        public Language SecondaryLanguage;

        public List<BibleBook> PrimaryBibleBooks;
        public List<BibleBook> SecondaryBibleBooks;

        public string[] PrimaryInsightGroups;
        public string[] SecondaryInsightGroups;
        public List<InsightArticle> PrimaryInsightArticles;
        public List<InsightArticle> SecondaryInsightArticles;

        public List<WOLArticle> PrimaryBooks;
        public List<WOLArticle> SecondaryBooks;

        public Dictionary<string, NavStruct> ArticleNavigation = new Dictionary<string, NavStruct>();

        public const string ApiKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAotFWO8AoSe+sNF+4WPaulCE6sCU0r+5Zm/Ir8CTNGE5o8ZhhBFIDz4GP3+sv8sKyI2V5ANUMpaLvA138AcGX83WVeC89RAKf7JwXKpI1jMd5oenhrw159cEkJe66Vl0HXzXfa6ReSiWPSNChVRRVk7tg9HZ6COhJXgWJ25xek43ozqSzi66wdBuA+WdnOKsC+uEMUxOOIwrIAyGurmAh7FCp0DlRhabObtPEol/XES9Kia0ZmkfC2algos3ColNGcCC/BDQPF4JqpoxBkw5WkXvV9h0VhORlE+2+rQL1+La58Wve5oRG5WaSs1YRM2l8CvFUvRd5h4KNJQ40rlsHqQIDAQAB";

        public static string EnglishDatabasePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "english.db");
        public static string ChineseDatabasePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "chinese.db");

        public App ()
        {
            // If book, chapter, or verse is changed, store it in the navigation dictionary
            this.ArticleChanged += App_ArticleChanged;
            this.LanguageChanged += App_LanguageChanged;

            SelectedArticle = new NavStruct();

            PrimaryWebViewStack = new Stack<string>();
            SecondaryWebViewStack = new Stack<string>();
        }

        public App(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
            
        }

        public override void OnCreate()
        {
            base.OnCreate();

            FontsOverride.SetDefaultFont(this, "MONOSPACE", "fonts/Roboto-Light.ttf");
        }

        private void App_ArticleChanged(object sender, ArticleChangedArgs e)
        {
            string nav = string.Empty;

            try
            {
                nav = CurrentLibrary.ToString() + e.SelectedArticle.Book.ToString();

                UpdateArticleNavigation(nav, e.SelectedArticle);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        private void App_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {
            // NEEDED FOR INITIALIZTION
        }

        public void AddToWebViewStack(string htmlPrimary, string htmlSecondary)
        {
            PrimaryWebViewStack.Push(htmlPrimary);
            SecondaryWebViewStack.Push(htmlSecondary);
        }

        public void UpdateArticleNavigation(string key, NavStruct value)
        {
            if (ArticleNavigation.ContainsKey(key))
            {
                ArticleNavigation[key] = value;
            }
            else
            {
                ArticleNavigation.Add(key, value);
            }
        }

        public void SwapLanguage()
        {
            if (CanTranslate())
            {
                Swapped = !Swapped;

                PrimaryBibleBooks = PrimaryBibleBooks.SwapWith(ref SecondaryBibleBooks);
                PrimaryInsightGroups = PrimaryInsightGroups.SwapWith(ref SecondaryInsightGroups);
                PrimaryInsightArticles = PrimaryInsightArticles.SwapWith(ref SecondaryInsightArticles);
                PrimaryBooks = PrimaryBooks.SwapWith(ref SecondaryBooks);

                PrimaryLanguage = PrimaryLanguage.SwapWith(ref SecondaryLanguage);

                // ALWAYS THE FINAL ACTION
                Language = PrimaryLanguage.EnglishName;
            }            
        }

        public void ResetStorehouse()
        {
            JwStore database = new JwStore(Storehouse.Primary);
            database.Open();
            database.DeleteAllTables();
            database.Close();

            database = new JwStore(Storehouse.Secondary);
            database.Open();
            database.DeleteAllTables();
            database.Close();

            PrimaryBibleBooks = null;
            SecondaryBibleBooks = null;

            PrimaryInsightGroups = null;
            SecondaryInsightGroups = null;

            PrimaryInsightArticles = null;
            SecondaryInsightArticles = null;

            PrimaryBooks = null;
            SecondaryBooks = null;

            PrimaryWebViewStack = null;
            SecondaryWebViewStack = null;

            SelectedArticle = new NavStruct();
            Libraries = null;
            CurrentLibrary = Library.None;
            CurrentScreenMode = ScreenMode.Duel;
            Swapped = false;

            ArticleNavigation = new Dictionary<string, NavStruct>();

            App.STATE.PrimaryLanguage = null;
            App.STATE.SecondaryLanguage = null;
            App.STATE.Language = "";

            // Clear all preferences
            var prefs = PreferenceManager.GetDefaultSharedPreferences(Context);
            prefs.Edit().Clear().Commit();           
        }

        public bool CanTranslate()
        {
            if (PrimaryLanguage != null && SecondaryLanguage != null && !string.IsNullOrEmpty(Language))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetActionBarTitle(Xamarin.ActionbarSherlockBinding.App.ActionBar actionbar, string title, string subtitle = "")
        {
            title = title.Replace("\n", "|");
            title = title.Split('|')[0];

            subtitle = subtitle.Replace("\n", "|");
            subtitle = subtitle.Split('|')[0];

            Typeface face = Typeface.CreateFromAsset(Activity.Assets, "fonts/Roboto-Regular.ttf");
            SpannableString t = new SpannableString(title);
            t.SetSpan(new CustomTypefaceSpan("", face), 0, t.Length(), SpanTypes.ExclusiveExclusive);
            actionbar.TitleFormatted = t;

            if (!string.IsNullOrEmpty(subtitle))
            {
                face = Typeface.CreateFromAsset(Activity.Assets, "fonts/Roboto-Light.ttf");
                SpannableString s = new SpannableString(subtitle);
                s.SetSpan(new CustomTypefaceSpan("", face), 0, s.Length(), SpanTypes.ExclusiveExclusive);
                actionbar.SubtitleFormatted = s;
            }

            actionbar.Show();
        }

        public void SaveUserPreferences()
        {
            if(string.IsNullOrEmpty(Language))
            {
                return;
            }

            Preferences = PreferenceManager.GetDefaultSharedPreferences(Activity);
            
            //////////////////////////////////////////////////////////////////////////
            // APP OBJECTS
            //////////////////////////////////////////////////////////////////////////
            string json = JsonConvert.SerializeObject(ArticleNavigation);
            Preferences.Edit().PutString("ArticleNavigation", json).Commit();

            json = JsonConvert.SerializeObject(Libraries);
            Preferences.Edit().PutString("Libraries", json).Commit();

            json = JsonConvert.SerializeObject(PrimaryBibleBooks);
            Preferences.Edit().PutString("PrimaryBibleBooks", json).Commit();

            json = JsonConvert.SerializeObject(SecondaryBibleBooks);
            Preferences.Edit().PutString("SecondaryBibleBooks", json).Commit();

            json = JsonConvert.SerializeObject(PrimaryInsightGroups);
            Preferences.Edit().PutString("PrimaryInsightGroups", json).Commit();

            json = JsonConvert.SerializeObject(SecondaryInsightGroups);
            Preferences.Edit().PutString("SecondaryInsightGroups", json).Commit();

            json = JsonConvert.SerializeObject(PrimaryInsightArticles);
            Preferences.Edit().PutString("PrimaryInsightArticles", json).Commit();

            json = JsonConvert.SerializeObject(SecondaryInsightArticles);
            Preferences.Edit().PutString("SecondaryInsightArticles", json).Commit();

            json = JsonConvert.SerializeObject(PrimaryBooks);
            Preferences.Edit().PutString("PrimaryBooks", json).Commit();

            json = JsonConvert.SerializeObject(SecondaryBooks);
            Preferences.Edit().PutString("SecondaryBooks", json).Commit();

            json = JsonConvert.SerializeObject(WebviewWeights);
            Preferences.Edit().PutString("WebviewWeights", json).Commit();

            Preferences.Edit().PutInt("CurrentScreenMode", (int)CurrentScreenMode).Commit();

            //////////////////////////////////////////////////////////////////////////
            // VARIABLES
            //////////////////////////////////////////////////////////////////////////
            Preferences.Edit().PutInt("WebViewBaseFontSize", SeekBarTextSize).Commit();

            //////////////////////////////////////////////////////////////////////////
            // LANGUAGE
            //////////////////////////////////////////////////////////////////////////
            Preferences.Edit().PutString("Language", Language).Commit();
            Preferences.Edit().PutString("PrimaryLanguage", PrimaryLanguage.EnglishName).Commit();
            Preferences.Edit().PutString("SecondaryLanguage", SecondaryLanguage.EnglishName).Commit();
            Preferences.Edit().PutBoolean("Swapped", Swapped).Commit();
        }

        public void LoadUserPreferences()
        {
            Preferences = PreferenceManager.GetDefaultSharedPreferences(Activity);

            if (Preferences.Contains("Language") && !string.IsNullOrEmpty(Preferences.GetString("Language", "")))
            {
                //////////////////////////////////////////////////////////////////////////
                // APP OBJECTS
                //////////////////////////////////////////////////////////////////////////
                string json = Preferences.GetString("ArticleNavigation", "");
                Dictionary<string, NavStruct> articleNavigation = JsonConvert.DeserializeObject<Dictionary<string, NavStruct>>(json);

                json = Preferences.GetString("PublicationNavigation", "");
                Dictionary<string, int> publicationNavigation = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);

                json = Preferences.GetString("Libraries", "");
                List<Library> libraries = JsonConvert.DeserializeObject<List<Library>>(json);

                json = Preferences.GetString("PrimaryBibleBooks", "");
                List<BibleBook> primaryBibleBooks = JsonConvert.DeserializeObject<List<BibleBook>>(json);

                json = Preferences.GetString("SecondaryBibleBooks", "");
                List<BibleBook> secondaryBibleBooks = JsonConvert.DeserializeObject<List<BibleBook>>(json);

                json = Preferences.GetString("PrimaryInsightGroups", "");
                string[] primaryInsightGroups = JsonConvert.DeserializeObject<string[]>(json);

                json = Preferences.GetString("SecondaryInsightGroups", "");
                string[] secondaryInsightGroups = JsonConvert.DeserializeObject<string[]>(json);

                json = Preferences.GetString("PrimaryInsightArticles", "");
                List<InsightArticle> primaryInsightArticles = JsonConvert.DeserializeObject<List<InsightArticle>>(json);

                json = Preferences.GetString("SecondaryInsightArticles", "");
                List<InsightArticle> secondaryInsightArticles = JsonConvert.DeserializeObject<List<InsightArticle>>(json);

                json = Preferences.GetString("PrimaryBooks", "");
                List<WOLArticle> primaryBooks = JsonConvert.DeserializeObject<List<WOLArticle>>(json);

                json = Preferences.GetString("SecondaryBooks", "");
                List<WOLArticle> secondaryBooks = JsonConvert.DeserializeObject<List<WOLArticle>>(json);

                json = Preferences.GetString("WebviewWeights", "");
                float[] webviewWeights = JsonConvert.DeserializeObject<float[]>(json);

                ArticleNavigation = articleNavigation;

                Libraries = libraries;
                CurrentLibrary = Libraries.FirstOrDefault();

                PrimaryBibleBooks = primaryBibleBooks;
                SecondaryBibleBooks = secondaryBibleBooks;

                PrimaryInsightGroups = primaryInsightGroups;
                SecondaryInsightGroups = secondaryInsightGroups;

                PrimaryInsightArticles = primaryInsightArticles;
                SecondaryInsightArticles = secondaryInsightArticles;

                PrimaryBooks = primaryBooks;
                SecondaryBooks = secondaryBooks;

                CurrentScreenMode = (ScreenMode)Preferences.GetInt("CurrentScreenMode", 0);

                WebviewWeights = webviewWeights;

                //////////////////////////////////////////////////////////////////////////
                // VARIABLES
                //////////////////////////////////////////////////////////////////////////
                SeekBarTextSize = Preferences.GetInt("WebViewBaseFontSize", 16);

                //////////////////////////////////////////////////////////////////////////
                // LANGUAGE
                //////////////////////////////////////////////////////////////////////////
                Swapped = Preferences.GetBoolean("Swapped", false);
                PrimaryLanguage = App.FUNCTIONS.GetAvailableLanguages().Single(l => l.EnglishName == Preferences.GetString("PrimaryLanguage", ""));
                SecondaryLanguage = App.FUNCTIONS.GetAvailableLanguages().Single(l => l.EnglishName == Preferences.GetString("SecondaryLanguage", "")); ;
                Language = Preferences.GetString("Language", "");
            }
        }
    }

    public class FontsOverride
    {
        public static void SetDefaultFont(Context context, string staticTypefaceFieldName, string fontAssetName)
        {
            Typeface regular = Typeface.CreateFromAsset(context.Assets, fontAssetName);
            ReplaceFont(staticTypefaceFieldName, regular);
        }

        protected static void ReplaceFont(string staticTypefaceFieldName, Typeface newTypeface)
        {
            try
            {
                Field staticField = ((Java.Lang.Object)(newTypeface)).Class.GetDeclaredField(staticTypefaceFieldName);
                staticField.Accessible = true;
                staticField.Set(null, newTypeface);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}