using Android.App;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Support.V4.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Xamarin.ActionbarSherlockBinding.App;
using Xamarin.ActionbarSherlockBinding.Views;

namespace JWStorehouse
{
    public class ArticleFragment : SherlockFragment, IObservableOnScrollChangedCallback
    {
        public View view;

        public ObservableWebView primaryWebview;
        public ObservableWebView secondaryWebview;
        public ViewFlipper flipper;
        public HeaderFooterGridView gridView;
        public TextView text;

        public int max = 0;

        public ScreenMode mode = App.STATE.CurrentScreenMode;

        private DateTime today = DateTime.Now;

        private List<ISpanned> primaryChapters = new List<ISpanned>();
        private List<ISpanned> secondaryChapters = new List<ISpanned>();
        private List<WOLArticle> primaryArticles;
        private List<WOLArticle> secondaryArticles;

        private List<string> HtmlContents = new List<string>();

        public NavStruct SelectedArticle;

        private Library library;

        private string[] insightMEPS = null;

        private bool ChapterPrompt = false;

        public ArticleFragment(NavStruct article, Library library)
        {
            this.SelectedArticle = article;
            this.library = library;

            if (library == Library.Insight)
            {
                insightMEPS = App.FUNCTIONS.GetInsightArticlesByGroup(App.STATE.CurrentArticleGroup).Select(a => a.MEPSID).ToArray();
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (view != null)
            {
                ((ViewGroup)view.Parent).RemoveView(view);

                return view;
            }

            SetHasOptionsMenu(true);
            SetMenuVisibility(true);

            view = inflater.Inflate(Resource.Layout.ArticleFragment, container, false);
            
            InitializeLayoutParadigm(view);

            DisplayArticles();

            return view;
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            this.view = null;

            // Recreate the view
            ViewGroup container = (ViewGroup)View;
            container.RemoveAllViewsInLayout();
            View view = OnCreateView(Activity.LayoutInflater, container, null);
            container.AddView(view);
        }

        public override void OnResume()
        {
            base.OnResume();

            Console.WriteLine("ArticleFragment -> Resume " + library.ToString());

            AddEventHandlers();
        }

        public override void OnPause()
        {
            base.OnPause();

            Console.WriteLine("ArticleFragment -> Pause " + library.ToString());

            DestroyEventHandlers();

            // Clear navigation
            var nav = (Activity as MainLibraryActivity).nav;
            nav.Adapter = null;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Console.WriteLine("ArticleFragment -> Destroy " + library.ToString());

            DestroyEventHandlers();
        }

        private void AddEventHandlers()
        {
            App.STATE.LanguageChanged += STATE_LanguageChanged;

            ListView nav = (Activity as MainLibraryActivity).nav;
            nav.ItemClick += SelectChapter;
        }

        private void DestroyEventHandlers()
        {
            App.STATE.LanguageChanged -= STATE_LanguageChanged;

            ListView nav = (Activity as MainLibraryActivity).nav;
            nav.ItemClick -= SelectChapter;
        }

        void STATE_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {
            if (App.STATE.CanTranslate())
            {
                if(library == Library.Insight)
                {
                    insightMEPS = App.FUNCTIONS.GetInsightArticlesByGroup(App.STATE.CurrentArticleGroup).Select(a => a.MEPSID).ToArray();
                }

                DisplayArticles();
            }
        }  

        public void InitializeLayoutParadigm(View view)
        {
            primaryWebview = view.FindViewById<ObservableWebView>(Resource.Id.primaryWebView);
            secondaryWebview = view.FindViewById<ObservableWebView>(Resource.Id.secondaryWebView);
            flipper = view.FindViewById<ViewFlipper>(Resource.Id.view_flipper);
            text = view.FindViewById<TextView>(Resource.Id.chapterTitle);
            gridView = view.FindViewById<HeaderFooterGridView>(Resource.Id.chapterGridView);

            flipper.SetInAnimation(Activity, Resource.Animation.push_down_in_no_alpha);
            flipper.SetOutAnimation(Activity, Resource.Animation.push_down_out_no_alpha);

            // Style views
            Typeface face = Typeface.CreateFromAsset(Activity.Assets, "fonts/Roboto-Regular.ttf");
            text.SetTypeface(face, TypefaceStyle.Normal);

            // WebView setup
            InitializeWebView(primaryWebview);
            InitializeWebView(secondaryWebview);

            primaryWebview.Tag = "primary";
            secondaryWebview.Tag = "secondary";

            ((LinearLayout)primaryWebview.Parent).LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent, App.STATE.WebviewWeights[0]);
            ((LinearLayout)secondaryWebview.Parent).LayoutParameters = new LinearLayout.LayoutParams(LinearLayout.LayoutParams.MatchParent, LinearLayout.LayoutParams.MatchParent, App.STATE.WebviewWeights[1]);

            if (App.STATE.WebviewWeights[0] == 0)
            {
                primaryWebview.IsDeflated = true;
            }
            if (App.STATE.WebviewWeights[1] == 0)
            {
                secondaryWebview.IsDeflated = true;
            }

            // GridView setup
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                gridView.ChoiceMode = ChoiceMode.Single;
            }

            gridView.ItemClick += SelectChapter;
        }

        private void InitializeWebView(ObservableWebView view)
        {
            StorehouseWebViewClient client = new StorehouseWebViewClient();
            view.SetWebViewClient(client);
            view.Settings.JavaScriptEnabled = true;
            view.Settings.BuiltInZoomControls = false;
            view.VerticalScrollBarEnabled = false;

            int size = App.FUNCTIONS.GetWebViewTextSize(App.STATE.SeekBarTextSize);
            view.Settings.DefaultFontSize = size;

            view.ScrollChangedCallback = this;
            view.ParentFragment = this;
        }

        public void OnScroll(ObservableWebView view, int x, int y)
        {
            bool autoScroll = PreferenceManager.GetDefaultSharedPreferences(Activity.ApplicationContext).GetBoolean("autoScroll", true);
            if (!autoScroll)
            {
                return;
            }

            float viewHeight = view.Height;
            float primaryHeight = primaryWebview.ContentHeight * primaryWebview.Scale;
            float secondaryHeight = secondaryWebview.ContentHeight * secondaryWebview.Scale;

            // Scroll primary
            if (view == primaryWebview)
            {
                float primaryYPos = Java.Lang.Math.Round(view.ScrollY * (secondaryHeight - viewHeight) / (primaryHeight - viewHeight));
                secondaryWebview.ScrollTo(x, (int)primaryYPos);
            }

            // Scroll secondary
            if (view == secondaryWebview)
            {
                float secondaryYPos = Java.Lang.Math.Round(view.ScrollY * (primaryHeight - viewHeight) / (secondaryHeight - viewHeight));
                primaryWebview.ScrollTo(x, (int)secondaryYPos);
            }
        }

        private const int CHAPTERS_MENU = 3;
        private const int SCREENMODE_MENU = 7;
        private const int TEXTSIZE_MENU = 8;

        public override void OnCreateOptionsMenu(Xamarin.ActionbarSherlockBinding.Views.IMenu menu, Xamarin.ActionbarSherlockBinding.Views.MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);

            try
            {
                menu.RemoveItem(CHAPTERS_MENU);
                menu.RemoveItem(TEXTSIZE_MENU);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
            
            var chaptersMenu = menu.Add(0, CHAPTERS_MENU, CHAPTERS_MENU, "Chapters");
            chaptersMenu.SetIcon(Resource.Drawable.chapters);
            chaptersMenu.SetShowAsAction((int)ShowAsAction.IfRoom);

            //var screenModeMenu = menu.Add(0, SCREENMODE_MENU, SCREENMODE_MENU, "Mode");
            ////screenModeMenu.SetIcon(Resource.Drawable.expand);
            //screenModeMenu.SetShowAsAction((int)ShowAsAction.IfRoom);

            var fontIncreaseMenu = menu.Add(0, TEXTSIZE_MENU, TEXTSIZE_MENU, "Text Size");
            //fontIncreaseMenu.SetIcon(Resource.Drawable.fontsize);
            fontIncreaseMenu.SetShowAsAction((int)ShowAsAction.Never | (int)ShowAsAction.WithText);
        }

        public override bool OnOptionsItemSelected(Xamarin.ActionbarSherlockBinding.Views.IMenuItem item)
        {
            DrawerLayout drawer = (Activity as MainLibraryActivity).drawer;

            switch (item.ItemId)
            {
                case (CHAPTERS_MENU):
                    if (flipper.DisplayedChild == (int)ScreenMode.Navigation)
                    {
                        flipper.DisplayedChild = (int)mode;
                    }
                    else
                    {
                        flipper.DisplayedChild = (int)ScreenMode.Navigation;
                    }
                    return true;
                //case (SCREENMODE_MENU):
                //    ChangeScreen();
                //    return true;
                case (TEXTSIZE_MENU):
                    FontSizeDialog(Activity).Show();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        //public void ChangeScreen()
        //{
        //    if (mode == ScreenMode.Duel)
        //    {
        //        mode = ScreenMode.Single;
        //    }
        //    else if (mode == ScreenMode.Single)
        //    {
        //        mode = ScreenMode.Duel;
        //    }

        //    App.STATE.CurrentScreenMode = mode;
        //    ThreadPool.QueueUserWorkItem((o) => App.STATE.SaveUserPreferences());

        //    FlipToCurrentScreenMode();
        //}

        private void FlipToCurrentScreenMode()
        {
            if (ChapterPrompt && primaryChapters.Count > 1 && library != Library.DailyText)
            {
                flipper.DisplayedChild = (int)ScreenMode.Navigation;
                ChapterPrompt = false;
            }
            else
            {
                mode = App.STATE.CurrentScreenMode;

                if (flipper.DisplayedChild != (int)mode)
                {
                    flipper.DisplayedChild = (int)mode;
                }
            }
        }

        private void SelectChapter(object sender, AdapterView.ItemClickEventArgs e)
        {
            NavStruct selected = NavStruct.Parse(primaryArticles[e.Position].ArticleMEPSID);

            SelectedArticle = selected;

            DisplayArticles();
        }

        public void ChangeArticle(int offset)
        {
            Console.WriteLine(Enum.GetName(typeof(Library), library));

            int currentIndex = Array.IndexOf(primaryArticles.Select(a => a.ArticleMEPSID).ToArray(), SelectedArticle.ToString());
            int index = currentIndex + offset;
            int last = primaryArticles.Count - 1;

            if (index > last)
            {
                index = 0;
            }
            else if (index < 0)
            {
                index = last;
            }

            NavStruct nav = NavStruct.Parse(primaryArticles[index].ArticleMEPSID);
            SelectedArticle = nav;

            DisplayArticles();
        }

        private string GetArticle(string storehouse)
        {
            if (primaryArticles == null || secondaryArticles == null)
            {
                return GetArticleFromStorehouse(storehouse);
            }
            
            Library library = App.STATE.CurrentLibrary;
            WOLArticle article = new WOLArticle();
            string title = string.Empty;
            string html = string.Empty;

            //////////////////////////////////////////////////////////////////////////
            // TRY TO GET ARTICLE, IF NOT, DISPLAY NOTHING
            //////////////////////////////////////////////////////////////////////////
            try
            {
                string meps = (library == Library.Insight) ? SelectedArticle.Chapter.ToString() : SelectedArticle.ToString();

                // Get article
                if (storehouse == Storehouse.Primary)
                {
                    article = primaryArticles.Single(a => a.ArticleMEPSID.Contains(meps));
                }
                else if (storehouse == Storehouse.Secondary)
                {
                    article = secondaryArticles.Single(a => a.ArticleMEPSID.Contains(meps));
                }

                // Set Publication title and content
                if (library == Library.Bible)
                {
                    title = article.ArticleTitle.Replace("\n", "<br/>");
                    html = "<center><h3>" + title + "</h3></center>" + article.ArticleContent;
                }
                else
                {
                    title = article.PublicationName.Replace("\n", "<br/>");
                    html = article.ArticleContent;
                }

                if (storehouse == ((App.STATE.Swapped == false) ? Storehouse.Primary : Storehouse.Secondary))
                {
                    text.SetText(Html.FromHtml("<center>" + App.FUNCTIONS.RemoveDigits(title) + "</center>"), TextView.BufferType.Normal);
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return html;
        }

        private string GetArticleFromStorehouse(string storehouse)
        {
            Library library = App.STATE.CurrentLibrary;
            WOLArticle article = new WOLArticle();
            string title = string.Empty;
            string html = string.Empty;

            JwStore database = new JwStore(storehouse);
            database.Open();

            //////////////////////////////////////////////////////////////////////////
            // TRY TO GET ARTICLE, IF NOT, DISPLAY NOTHING
            //////////////////////////////////////////////////////////////////////////
            //try
            //{
                //////////////////////////////////////////////////////////////////////////
                // BIBLE
                //////////////////////////////////////////////////////////////////////////
                if (library == Library.Bible)
                {
                    article = database.QueryBible(SelectedArticle).ToArticle();
                    title = article.ArticleTitle.Replace("\n", "<br/>");

                    if (storehouse == Storehouse.Primary)
                    {
                        primaryArticles = database.QueryAllArticlesByBibleChapter(title.Split('<')[0]).ToArticleList();

                        foreach (var a in primaryArticles)
                        {
                            primaryChapters.Add(Html.FromHtml(a.ArticleLocation));
                        }
                    }
                    else if (storehouse == Storehouse.Secondary)
                    {
                        secondaryArticles = database.QueryAllArticlesByBibleChapter(title.Split('<')[0]).ToArticleList();

                        foreach (var a in secondaryArticles)
                        {
                            secondaryChapters.Add(Html.FromHtml(a.ArticleLocation));
                        }
                    }
                }
                //////////////////////////////////////////////////////////////////////////
                // DAILY TEXT
                //////////////////////////////////////////////////////////////////////////
                else if (library == Library.DailyText)
                {
                    article = database.QueryDailyText(SelectedArticle.ToString()).ToArticle();

                    if (storehouse == Storehouse.Primary)
                    {
                        primaryArticles = database.QueryArticles(article.PublicationCode).ToArticleList();

                        foreach (var a in primaryArticles)
                        {
                            primaryChapters.Add(Html.FromHtml(a.ArticleTitle));
                        }
                    }
                    else if (storehouse == Storehouse.Secondary)
                    {
                        secondaryArticles = database.QueryArticles(article.PublicationCode).ToArticleList();

                        foreach (var a in secondaryArticles)
                        {
                            secondaryChapters.Add(Html.FromHtml(a.ArticleTitle));
                        }
                    }

                    title = article.PublicationName.Replace("\n", "<br/>");
                }
                //////////////////////////////////////////////////////////////////////////
                // INSIGHT VOLUMES
                //////////////////////////////////////////////////////////////////////////
                else if (library == Library.Insight)
                {
                    article = database.QueryInsight(SelectedArticle).ToArticle();

                    if (storehouse == Storehouse.Primary)
                    {
                        //primaryArticles = database.QueryArticles("it").ToArticleList();
                        primaryArticles = database.QueryMatchingArticles(insightMEPS.ToList()).ToArticleList();

                        foreach (var a in primaryArticles)
                        {
                            primaryChapters.Add(Html.FromHtml(a.ArticleTitle + "<br/><i>" + a.ArticleLocation + "</i>"));
                        }
                    }
                    else if (storehouse == Storehouse.Secondary)
                    {
                        //secondaryArticles = database.QueryArticles("it").ToArticleList();
                        secondaryArticles = database.QueryMatchingArticles(insightMEPS.ToList()).ToArticleList();

                        foreach (var a in secondaryArticles)
                        {
                            secondaryChapters.Add(Html.FromHtml(a.ArticleTitle + "<br/><i>" + a.ArticleLocation + "</i>"));
                        }
                    }

                    title = article.PublicationName.Replace("\n", "<br/>");
                }
                //////////////////////////////////////////////////////////////////////////
                // BOOKS & PUBLICATIONS
                //////////////////////////////////////////////////////////////////////////
                else if (library == Library.Books)
                {
                    article = database.QueryPublication(SelectedArticle).ToArticle();

                    if (storehouse == Storehouse.Primary)
                    {
                        primaryArticles = database.QueryArticles(article.PublicationCode).ToArticleList();

                        foreach (var a in primaryArticles)
                        {
                            primaryChapters.Add(Html.FromHtml(a.ArticleTitle + "<br/><i>" + a.ArticleLocation + "</i>"));
                        }
                    }
                    else if (storehouse == Storehouse.Secondary)
                    {
                        secondaryArticles = database.QueryArticles(article.PublicationCode).ToArticleList();

                        foreach (var a in secondaryArticles)
                        {
                            secondaryChapters.Add(Html.FromHtml(a.ArticleTitle + "<br/><i>" + a.ArticleLocation + "</i>"));
                        }
                    }

                    title = article.PublicationName.Replace("\n", "<br/>");
                }
                //////////////////////////////////////////////////////////////////////////
                // ALL OTHER
                //////////////////////////////////////////////////////////////////////////
                else
                {
                    if (storehouse == Storehouse.Primary)
                    {

                    }
                    else if (storehouse == Storehouse.Secondary)
                    {

                    }
                }
            //}
            //catch(Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}

            // Close database
            database.Close();

            // Set Publication title
            if (storehouse == ((App.STATE.Swapped == false) ? Storehouse.Primary : Storehouse.Secondary))
            {
                text.SetText(Html.FromHtml("<center>" + App.FUNCTIONS.RemoveDigits(title) + "</center>"), TextView.BufferType.Normal);
            }

            // Set Article content html
            if (library == Library.Bible)
            {
                html = "<center><h3>" + title + "</h3></center>" + article.ArticleContent;
            }
            else
            {
                html = article.ArticleContent;
            }

            return html;
        }

        private void DisplayArticles()
        {
            //App.STATE.AddToWebViewStack(primaryContent, secondaryContent);

            Activity.RunOnUiThread(() =>
            {
                string primaryContent = GetArticle((App.STATE.Swapped == false) ? Storehouse.Primary : Storehouse.Secondary);
                string secondaryContent = GetArticle((App.STATE.Swapped == false) ? Storehouse.Secondary : Storehouse.Primary);

                primaryWebview.LoadDataWithBaseURL("file:///android_asset/", primaryContent, "text/html", "utf-8", null);
                secondaryWebview.LoadDataWithBaseURL("file:///android_asset/", secondaryContent, "text/html", "utf-8", null);

                HandleTopNav();
                HandleRightNav();

                FlipToCurrentScreenMode();

                max = primaryArticles.Count();
            });

            ThreadPool.QueueUserWorkItem((o) => App.STATE.SelectedArticle = SelectedArticle);
            ThreadPool.QueueUserWorkItem((o) => App.STATE.SaveUserPreferences());
        }

        private void RefreshWebViews()
        {
            primaryWebview.LoadDataWithBaseURL("file:///android_asset/", HtmlContents.First(), "text/html", "utf-8", null);
            secondaryWebview.LoadDataWithBaseURL("file:///android_asset/", HtmlContents.Last(), "text/html", "utf-8", null);

            FlipToCurrentScreenMode();
        }

        private void HandleTopNav()
        {
            List<ISpanned> articles = (App.STATE.Swapped == false) ? primaryChapters : secondaryChapters;
            if (library == Library.Bible)
            {
                // Chapter numbers only
                articles = articles.Select(a => Html.FromHtml(a.ToString().Split(new[] { ' ' }).Last())).ToList();
            }

            int width = (int)(App.STATE.Context.Resources.GetDimension(Resource.Dimension.bible_nav_bible_book_grid_width) / App.STATE.Context.Resources.DisplayMetrics.Density);
            if (library != Library.Bible)
            {
                if (width > 60)
                {
                    gridView.NumColumns = 3;
                }
                else
                {
                    gridView.NumColumns = 1;
                }
                gridView.StretchMode = StretchMode.StretchColumnWidth;
            }
            else
            {
                gridView.NumColumns = -1;
                gridView.StretchMode = StretchMode.NoStretch;
            }

            gridView.Adapter = new ArticleButtonAdapter(Activity, articles.ToArray());

            int index = GetNavigationIndex();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                gridView.SetItemChecked(index, true);
            }
            gridView.SetSelection(index);
        }

        private void HandleRightNav()
        {
            List<ISpanned> articles = (App.STATE.Swapped == false) ? primaryChapters : secondaryChapters;
            Console.WriteLine("THIS CONTAINS -> " + articles.Count);

            var nav = (Activity as MainLibraryActivity).nav;
            nav.Adapter = new NavArrayAdapter<ISpanned>(Activity, Resource.Layout.DrawerListItemRight, Resource.Id.right_nav_item, articles, "Roboto-Light", TypefaceStyle.Normal);

            int index = GetNavigationIndex();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                nav.SetItemChecked(index, true);
            }
            nav.SetSelection(index);

            var drawer = (Activity as MainLibraryActivity).drawer;
            drawer.CloseDrawer(nav);
        }

        private int GetNavigationIndex()
        {
            List<WOLArticle> articles = (App.STATE.Swapped == false) ? primaryArticles : secondaryArticles;

            int index = 0;
            if (library == Library.DailyText)
            {
                index = Array.IndexOf(articles.Select(a => a.ArticleMEPSID).ToArray(), SelectedArticle.ToString());
            }
            else
            {
                index = Array.IndexOf(articles.Select(a => NavStruct.Parse(a.ArticleMEPSID).Chapter.ToString()).ToArray(), SelectedArticle.Chapter.ToString());
            }

            return index;
        }

        public void SetWebViewTextSize(int size)
        {
            // Set size in webview
            primaryWebview.Settings.DefaultFontSize = size;
            secondaryWebview.Settings.DefaultFontSize = size;
        }  

        public Dialog FontSizeDialog(Activity activity)
        {
            ContextThemeWrapper context = new ContextThemeWrapper(activity, App.FUNCTIONS.GetDialogTheme());
            AlertDialog.Builder dialog = new AlertDialog.Builder(context);

            SeekBar seek = new SeekBar(activity);
            seek.Max = 10;
            seek.Progress = App.STATE.SeekBarTextSize;
            seek.SetOnSeekBarChangeListener(new SeekBarListener(this));

            int size = App.FUNCTIONS.GetWebViewTextSize(App.STATE.SeekBarTextSize);
            dialog.SetIcon(Resource.Drawable.Icon);
            dialog.SetMessage("Adjust article text size:");
            dialog.SetTitle("Text Size");
            dialog.SetNeutralButton("Close",
                (o, args) =>
                {
                    // Close dialog
                });
            dialog.SetView(seek);

            return dialog.Create();
        }
    }

    public class SeekBarListener : Java.Lang.Object, SeekBar.IOnSeekBarChangeListener
    {
        private ArticleFragment fragment;

        public SeekBarListener(ArticleFragment fragment)
        {
            this.fragment = fragment;
        }

        public void OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
        {
            int size = App.FUNCTIONS.GetWebViewTextSize(progress);
            fragment.SetWebViewTextSize(size);

            // Set font size globally
            App.STATE.SeekBarTextSize = progress;

            // Save font size to preferences
            var prefs = PreferenceManager.GetDefaultSharedPreferences(fragment.Activity);
            prefs.Edit().PutInt("WebViewBaseFontSize", progress).Commit();
        }

        public void OnStartTrackingTouch(SeekBar seekBar)
        {

        }

        public void OnStopTrackingTouch(SeekBar seekBar)
        {

        }
    }
}