using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Preferences;
using Android.Runtime;
using Android.Text;
using Android.Widget;
using Java.Lang.Reflect;
using Mindscape.Raygun4Net;
using Newtonsoft.Json;
using Parse;
using SQLite;
using Storehouse.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using ActionBar = Android.Support.V7.App.ActionBar;

namespace JWChinese
{
    [Application]
    public class App : Application
    {

        #region EVENTS Section

        ////////////////////////////////////////////////////////////////////////
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

        /************************************************************************/
        /* Pinyin Toggle listener                                               */
        /************************************************************************/
        public delegate void PinyinChangedEventHandler(object sender, PinyinChangedArgs e);
        public class PinyinChangedArgs : EventArgs
        {
            private bool pinyinToggle;

            public PinyinChangedArgs(bool pinyinToggle)
            {
                this.pinyinToggle = pinyinToggle;
            }

            public bool PinyinToggle
            {
                get
                {
                    return pinyinToggle;
                }
            }
        }

        public event PinyinChangedEventHandler PinyinChanged;
        protected virtual void OnPinyinChanged(PinyinChangedArgs e)
        {
            PinyinChanged(this, e);
        }

        private bool pinyinToggle;
        public bool PinyinToggle
        {
            get
            {
                return this.pinyinToggle;
            }
            set
            {
                this.pinyinToggle = value;

                PinyinChangedArgs args = new PinyinChangedArgs(value);
                this.OnPinyinChanged(args);
            }
        }

        #endregion EVENTS Section

        #region Pre Initialization Section
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

        #endregion Pre Initialization Section

        #region PROPERTIES Section

        //////////////////////////////////////////////////////////////////////////
        // PROPERTIES Section
        //////////////////////////////////////////////////////////////////////////

        public Activity Activity;

        public bool Swapped = false;

        public List<Library> Libraries;

        public Library CurrentLibrary = Library.Bible;

        public ScreenMode CurrentScreenMode = ScreenMode.Duel;

        public int CurrentArticleGroup;

        public int SeekBarTextSize = 0;
        public int TextSizeMultiplier = 3;
        public int TextSizeBase = 16;

        public float[] WebviewWeights = new float[2] { 1, 1 };

        public Language PrimaryLanguage;
        public Language SecondaryLanguage;
        public Language PinyinLanguage;

        public ISharedPreferences Preferences;

        public bool RefreshWebViews = false;

        public string WebViewOriginalHTML = string.Empty;
        public string WebViewNewHTML = string.Empty;

        public Dictionary<string, NavStruct> ArticleNavigation = new Dictionary<string, NavStruct>();

        public string ApiKey = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAotFWO8AoSe+sNF+4WPaulCE6sCU0r+5Zm/Ir8CTNGE5o8ZhhBFIDz4GP3+sv8sKyI2V5ANUMpaLvA138AcGX83WVeC89RAKf7JwXKpI1jMd5oenhrw159cEkJe66Vl0HXzXfa6ReSiWPSNChVRRVk7tg9HZ6COhJXgWJ25xek43ozqSzi66wdBuA+WdnOKsC+uEMUxOOIwrIAyGurmAh7FCp0DlRhabObtPEol/XES9Kia0ZmkfC2algos3ColNGcCC/BDQPF4JqpoxBkw5WkXvV9h0VhORlE+2+rQL1+La58Wve5oRG5WaSs1YRM2l8CvFUvRd5h4KNJQ40rlsHqQIDAQAB";

        public static string EnglishDatabasePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "english.db");
        public static string ChineseDatabasePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "chinese.db");
        public static string PinyinDatabasePath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "pinyin.db");

        public static string LearningCharactersPath = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "learning.db");

        #endregion

        public App()
        {
            // If book, chapter, or verse is changed, store it in the navigation dictionary
            this.ArticleChanged += App_ArticleChanged;
            this.LanguageChanged += App_LanguageChanged;
            this.PinyinChanged += App_PinyinChanged;

            SelectedArticle = new NavStruct();
        }

        public App(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {

        }

        public override void OnCreate()
        {
            base.OnCreate();

            InitializeGlobalUI();

            // Initialize the Parse client
            ParseClient.Initialize("NGbz5QXIyeZJUrUZQSDnmG8DOJZdnYIlL8p9Onnp", "YgiZsvbO7BcKiCWurBInplOfeeMX2qaWtxS8thUT");

            // Attach Raygun.io
            //RaygunClient.Attach("UyIbXQAauQY5Rup6Sk6tyA==");
        }

        private void InitializeGlobalUI()
        {
            // Set global font
            UIOverride.SetDefaultFont(this, "MONOSPACE", "fonts/Roboto-Regular.ttf");

            // Set scroller overflow color
            UIOverride.SetScrollerOverflowColor(this, Resource.Color.storehouse_king_purple);
        }

        private void App_PinyinChanged(object sender, App.PinyinChangedArgs e)
        {
            // NEEDED FOR INITIALIZTION
        }

        private void App_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {
            // NEEDED FOR INITIALIZTION
        }

        private void App_ArticleChanged(object sender, ArticleChangedArgs e)
        {
            string nav = string.Empty;

            try
            {
                if(CurrentLibrary == Library.Insight)
                {
                    string storehouse = string.Empty;
                    string tag = string.Empty;

                    if (Language.Contains("English"))
                    {
                        storehouse = LibraryStorehouse.English;
                        tag = "en-";
                    }
                    else if (Language.Contains("Simplified"))
                    {
                        storehouse = LibraryStorehouse.Chinese;
                        tag = "ch-";
                    }
                    else if (Language.Contains("Pinyin"))
                    {
                        storehouse = LibraryStorehouse.Pinyin;
                        tag = "ch-";
                    }
                    Console.WriteLine("SELECTED ARTICLE -> " + e.SelectedArticle);

                    WOLArticle article = new SQLiteConnection(storehouse).Query<WOLArticle>("select ArticleGroup from WOLArticle where ArticleNumber like ? limit 1", "%" + e.SelectedArticle + "%").Single();

                    nav = tag + article.ArticleGroup;

                    UpdateArticleNavigation(nav, e.SelectedArticle);

                    Console.WriteLine("FROM APP NAV -> " + nav);
                }
                else
                {
                    nav = CurrentLibrary.ToString() + e.SelectedArticle.Book.ToString();

                    UpdateArticleNavigation(nav, e.SelectedArticle);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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

        public void SetActionBarTitle(ActionBar actionbar, string title, string subtitle = "")
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

        public void PinyinToggleParadigm(bool fromPrefs = false)
        {
            if (App.STATE.Preferences.GetBoolean("pinyinWol", false) && !fromPrefs)
            {
                Toast.MakeText(Activity, "Nothing happened because Pinyin WOL mode is turned on in application settings.", ToastLength.Long).Show();
                return;
            }

            PinyinToggle = !PinyinToggle;

            // English on left, just swap secondary
            if (!Swapped)
            {
                SecondaryLanguage = SecondaryLanguage.SwapWith(ref PinyinLanguage);
            }
            // Chinese on left, swap primary with pinyin and change language
            else
            {
                PrimaryLanguage = PrimaryLanguage.SwapWith(ref PinyinLanguage);
            }

            Language = App.STATE.PrimaryLanguage.EnglishName;

            if (!fromPrefs)
            {
                string mode = (PinyinToggle) ? "ON." : "OFF.";
                Toast.MakeText(Activity, "Pinyin mode " + mode, ToastLength.Long).Show();
            }
        }

        public void SwapLanguage()
        {
            Swapped = !Swapped;

            PrimaryLanguage = PrimaryLanguage.SwapWith(ref SecondaryLanguage);
            Language = PrimaryLanguage.EnglishName;

            Toast.MakeText(Activity, "Primary language: " + Language.ToUpper(), ToastLength.Long).Show();
        }

        public void SaveUserPreferences()
        {
            try
            {
                Preferences = PreferenceManager.GetDefaultSharedPreferences(Activity);

                //////////////////////////////////////////////////////////////////////////
                // APP OBJECTS
                //////////////////////////////////////////////////////////////////////////
                Preferences.Edit().PutString("ArticleNavigation", JsonConvert.SerializeObject(ArticleNavigation)).Commit();
                Preferences.Edit().PutString("Libraries", JsonConvert.SerializeObject(Libraries)).Commit();
   
                //////////////////////////////////////////////////////////////////////////
                // VARIABLES
                //////////////////////////////////////////////////////////////////////////
                Preferences.Edit().PutString("WebviewWeights", JsonConvert.SerializeObject(WebviewWeights)).Commit();
                Preferences.Edit().PutInt("WebViewBaseFontSize", SeekBarTextSize).Commit();
                Preferences.Edit().PutInt("CurrentScreenMode", (int)CurrentScreenMode).Commit();

                //////////////////////////////////////////////////////////////////////////
                // LANGUAGE
                //////////////////////////////////////////////////////////////////////////
                Preferences.Edit().PutBoolean("Swapped", Swapped).Commit();
                Preferences.Edit().PutBoolean("PinyinToggle", PinyinToggle).Commit();
                Preferences.Edit().PutString("PrimaryLanguage", PrimaryLanguage.EnglishName).Commit();
                Preferences.Edit().PutString("SecondaryLanguage", SecondaryLanguage.EnglishName).Commit();
                Preferences.Edit().PutString("PinyinLanguage", PinyinLanguage.EnglishName).Commit();
                Preferences.Edit().PutString("Language", Language).Commit();

                Console.WriteLine("Awesome, preferences are saved!");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void LoadUserPreferences()
        {
            try
            {
                Preferences = PreferenceManager.GetDefaultSharedPreferences(Activity);

                //////////////////////////////////////////////////////////////////////////
                // APP OBJECTS
                //////////////////////////////////////////////////////////////////////////
                ArticleNavigation = JsonConvert.DeserializeObject<Dictionary<string, NavStruct>>(Preferences.GetString("ArticleNavigation", ""));
                Libraries = JsonConvert.DeserializeObject<List<Library>>(Preferences.GetString("Libraries", ""));
                CurrentLibrary = Libraries.FirstOrDefault();

                //////////////////////////////////////////////////////////////////////////
                // VARIABLES
                //////////////////////////////////////////////////////////////////////////
                WebviewWeights = JsonConvert.DeserializeObject<float[]>(Preferences.GetString("WebviewWeights", ""));
                SeekBarTextSize = Preferences.GetInt("WebViewBaseFontSize", 16);
                CurrentScreenMode = (ScreenMode)Preferences.GetInt("CurrentScreenMode", 0);

                //////////////////////////////////////////////////////////////////////////
                // LANGUAGE
                //////////////////////////////////////////////////////////////////////////
                Swapped = Preferences.GetBoolean("Swapped", false);
                PinyinToggle = Preferences.GetBoolean("PinyinToggle", false);
                PrimaryLanguage = App.FUNCTIONS.GetAvailableLanguages(Activity).Where(l => l.EnglishName == Preferences.GetString("PrimaryLanguage", "")).First();
                SecondaryLanguage = App.FUNCTIONS.GetAvailableLanguages(Activity).Where(l => l.EnglishName == Preferences.GetString("SecondaryLanguage", "")).First();
                PinyinLanguage = App.FUNCTIONS.GetAvailableLanguages(Activity).Where(l => l.EnglishName == Preferences.GetString("PinyinLanguage", "")).First();
                Language = Preferences.GetString("Language", Language);

                Console.WriteLine("Awesome, preferences are loaded!");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }

    public class UIOverride
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

        public static void SetScrollerOverflowColor(Context context, int resourceID)
        {
            try
            {
                // Set global scroller overflow glow color
                int glowDrawableId = context.Resources.GetIdentifier("overscroll_glow", "drawable", "android");
                Drawable androidGlow = context.Resources.GetDrawable(glowDrawableId);
                androidGlow.SetColorFilter(context.Resources.GetColor(resourceID), PorterDuff.Mode.SrcIn);

                // Set global scroller overflow edge color
                int edgeDrawableId = context.Resources.GetIdentifier("overscroll_edge", "drawable", "android");
                Drawable androidEdge = context.Resources.GetDrawable(edgeDrawableId);
                androidEdge.SetColorFilter(context.Resources.GetColor(resourceID), PorterDuff.Mode.SrcIn);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}