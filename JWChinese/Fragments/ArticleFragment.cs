using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Support.V4.Widget;
using Android.Text;
using Android.Views;
using Android.Webkit;
using Android.Widget;

using MaterialDialogs;

using SQLite;

using Storehouse.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Fragment = Android.Support.V4.App.Fragment;

namespace JWChinese
{
    public class ArticleFragment : Fragment, IObservableOnScrollChangedCallback
    {
        public View view;

        public ObservableWebView primaryWebview;
        public ObservableWebView secondaryWebview;
        public ObservableWebView gridViewFooterWebview;
        public ViewFlipper flipper;
        public HeaderFooterGridView gridView;
        public TextView gridViewTitle;

        public ScreenMode mode = App.STATE.CurrentScreenMode;

        private List<ISpanned> primaryChapters = new List<ISpanned>();
        private List<ISpanned> secondaryChapters = new List<ISpanned>();
        private List<WOLArticle> primaryArticles = new List<WOLArticle>();
        private List<WOLArticle> secondaryArticles = new List<WOLArticle>();

        private List<string> HtmlContents = new List<string>();
        private string OutlineContents = string.Empty;

        public NavStruct SelectedArticle;

        private Library library;

        private IMenu optionsMenu;

        private int oldYPos = 0;
        private int Selection = 0;
        private bool ChapterPrompt = false;

        private bool refreshOnResume = false;

        public ArticleFragment(NavStruct article, Library library)
        {
            this.SelectedArticle = article;
            this.library = library;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Console.WriteLine("ArticleFragment -> Create " + library.ToString());

            ChapterPrompt = PreferenceManager.GetDefaultSharedPreferences(Activity.ApplicationContext).GetBoolean("chapterSelectionFirst", false);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (view != null)
            {
                ((ViewGroup)view.Parent).RemoveView(view);

                return view;
            }

            Console.WriteLine("ArticleFragment -> CreateView " + library.ToString());

            HasOptionsMenu = true;
            SetMenuVisibility(true);

            view = inflater.Inflate(Resource.Layout.ArticleFragment, container, false);

            InitializeLayoutParadigm(view);
            
            DisplayArticles();

            AddEventHandlers();

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

            //AddEventHandlers();

            if(App.STATE.RefreshWebViews == true)
            {
                try
                {
                    HtmlContents[0] = HandleInjection(HtmlContents[0]);
                    HtmlContents[1] = HandleInjection(HtmlContents[1]);
                    RefreshWebViews();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                App.STATE.RefreshWebViews = false;
            }

            if(refreshOnResume)
            {
                refreshOnResume = false;

                primaryChapters = new List<ISpanned>();
                secondaryChapters = new List<ISpanned>();

                primaryArticles = new List<WOLArticle>();
                secondaryArticles = new List<WOLArticle>();

                DisplayArticles();
            }

            RefreshActionBar();
            HandleTopNav();
            HandleRightNav();
        }

        public override void OnPause()
        {
            base.OnPause();

            Console.WriteLine("ArticleFragment -> Pause " + library.ToString());

            //DestroyEventHandlers();

            // Clear navigation
            //var nav = (Activity as MainLibraryActivity).nav;
            //nav.Adapter = null;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            Console.WriteLine("ArticleFragment -> Destroy " + library.ToString());

            DestroyEventHandlers();

            var activity = ((MainLibraryActivity)Activity);
            int index = activity.list.CheckedItemPosition;
            string subtitle = ((NavArrayAdapter<string>)activity.list.Adapter).objects[index];
            App.STATE.SetActionBarTitle(activity.SupportActionBar, "JW Chinese", subtitle);

            optionsMenu.RemoveItem(CHAPTERS_MENU);
            optionsMenu.RemoveItem(TEXTSIZE_MENU);

            try
            {
                ((MainLibraryActivity)Activity).stacks[(int)library].Pop();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void AddEventHandlers()
        {
            App.STATE.PinyinChanged += STATE_PinyinChanged;
            App.STATE.LanguageChanged += STATE_LanguageChanged;

            ListView nav = (Activity as MainLibraryActivity).nav;
            nav.ItemClick += SelectChapter;
        }

        private void DestroyEventHandlers()
        {
            App.STATE.PinyinChanged -= STATE_PinyinChanged;
            App.STATE.LanguageChanged -= STATE_LanguageChanged;

            ListView nav = (Activity as MainLibraryActivity).nav;
            nav.ItemClick -= SelectChapter;
        }

        void STATE_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {
            Console.WriteLine("LANGUAGED CHANGED IN ARTICLE FRAGMENT!!");

            if (!IsResumed)
            {
                refreshOnResume = true;

                return;
            }

            primaryArticles = primaryArticles.SwapWith(ref secondaryArticles);
            primaryChapters = primaryChapters.SwapWith(ref secondaryChapters);

            DisplayArticles();
        }

        private void STATE_PinyinChanged(object sender, App.PinyinChangedArgs e)
        {
            if (!IsResumed)
            {
                refreshOnResume = true;

                return;
            }

            primaryChapters = new List<ISpanned>();
            secondaryChapters = new List<ISpanned>();

            primaryArticles = new List<WOLArticle>();
            secondaryArticles = new List<WOLArticle>();
        }

        private void RefreshActionBar()
        {
            // Keep this try.catch UNTIL YOU GET THE PINYIN FOR INSIGHT!!!!!
            try
            {
                WOLArticle article = GetFirstArticle("primary");
                string publicationName = string.Empty;
               
                // Set Publication title
                int width = (int)(App.STATE.Activity.Resources.GetDimension(Resource.Dimension.bible_nav_bible_book_grid_width) / App.STATE.Activity.Resources.DisplayMetrics.Density);
                if (library == Library.Bible)
                {
                    publicationName = article.PublicationName;

                    string bibleBook = App.FUNCTIONS.GetAllBibleBooks(App.STATE.PrimaryLanguage.EnglishName).Single(b => b.Number.Equals((NavStruct.Parse(article.ArticleNumber).Book - 1).ToString())).Name;
                    gridViewTitle.SetText(Html.FromHtml("<center>" + bibleBook.Replace("\n", "<br/>") + "</center>"), TextView.BufferType.Normal);
                }
                else
                {
                    if (width > 100)
                    {
                        publicationName = App.FUNCTIONS.GetPublicationName(App.STATE.Language, article.PublicationCode);
                    }
                    else
                    {
                        publicationName = App.FUNCTIONS.GetPublicationName(App.STATE.Language, article.PublicationCode, true);
                    }
                    gridViewTitle.SetText(Html.FromHtml("<center>" + publicationName.Replace("\n", "<br/>") + "</center>"), TextView.BufferType.Normal);
                }

                // Set ActionBar type
                App.STATE.SetActionBarTitle(((MainLibraryActivity)Activity).SupportActionBar, article.ArticleTitle, publicationName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void InitializeLayoutParadigm(View view)
        {
            // Set views
            primaryWebview = view.FindViewById<ObservableWebView>(Resource.Id.primaryWebView);
            secondaryWebview = view.FindViewById<ObservableWebView>(Resource.Id.secondaryWebView);
            flipper = view.FindViewById<ViewFlipper>(Resource.Id.view_flipper);
            gridViewTitle = view.FindViewById<TextView>(Resource.Id.chapterTitle);
            gridView = view.FindViewById<HeaderFooterGridView>(Resource.Id.chapterGridView);

            LayoutInflater layoutInflater = LayoutInflater.From(Activity);
            View footerView = layoutInflater.Inflate(Resource.Layout.FooterWebView, null);
            gridViewFooterWebview = footerView.FindViewById<ObservableWebView>(Resource.Id.footerWebView);

            // ViewFlipper animations
            flipper.SetInAnimation(Activity, Resource.Animation.push_down_in_no_alpha);
            flipper.SetOutAnimation(Activity, Resource.Animation.push_down_out_no_alpha);

            // Style views
            Typeface face = Typeface.CreateFromAsset(Activity.Assets, "fonts/Roboto-Regular.ttf");
            gridViewTitle.SetTypeface(face, TypefaceStyle.Normal);

            // WebView setup
            InitializeWebView(primaryWebview);
            InitializeWebView(secondaryWebview);
            InitializeWebView(gridViewFooterWebview);

            primaryWebview.Tag = "primary";
            secondaryWebview.Tag = "secondary";

            ((LinearLayout)primaryWebview.Parent).LayoutParameters = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent, 
                LinearLayout.LayoutParams.MatchParent, 
                App.STATE.WebviewWeights[0]);
            ((LinearLayout)secondaryWebview.Parent).LayoutParameters = new LinearLayout.LayoutParams(
                LinearLayout.LayoutParams.MatchParent, 
                LinearLayout.LayoutParams.MatchParent, 
                App.STATE.WebviewWeights[1]);

            if (App.STATE.WebviewWeights[0] == 0)
            {
                primaryWebview.IsDeflated = true;
            }
            if (App.STATE.WebviewWeights[1] == 0)
            {
                secondaryWebview.IsDeflated = true;
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                gridView.ChoiceMode = ChoiceMode.Single;
            }

            gridView.ItemClick += SelectChapter;

            if (library == Library.Bible)
            {
                gridView.DoSetHeight = false;

                gridViewTitle.Click += text_Click;
                gridViewTitle.SetCompoundDrawablesWithIntrinsicBounds(Resource.Drawable.ic_outline, 0, 0, 0);
                
                // Set Bible book outline
                NavStruct outline = new NavStruct()
                {
                    Book = 0,
                    Chapter = SelectedArticle.Book,
                    Verse = 0
                };
                OutlineContents = JwStore.QueryArticle("outline", outline, LibraryStorehouse.English).ArticleContent;
                gridView.AddFooterView(footerView);
                gridViewFooterWebview.LoadDataWithBaseURL("file:///android_asset/", OutlineContents, "text/html", "utf-8", null);
            }     
        }

        void text_Click(object sender, EventArgs e)
        {
            try
            {
                //int yPos = (int)gridView.GetChildAt(gridView.ChildCount - 1).GetY();
                //Console.WriteLine("Outline position before scroll -> " + yPos.ToString());
                //if (yPos <= 0)
                //{
                //    //gridView.SmoothScrollBy(oldYPos, 1000);
                //    //gridView.SmoothScrollToPosition(0);
                //    gridView.SetSelection(0);
                //}
                //else
                //{
                //    gridView.SmoothScrollToPositionFromTop(gridView.Adapter.Count, -gridView.PaddingTop);
                //    //gridView.SetSelection(gridView.Adapter.Count);
                //}

                if (Selection == gridView.Adapter.Count)
                {
                    gridView.SetSelection(0);
                    Selection = 0;
                    return;
                }
                if (Selection == 0 || Selection != gridView.Adapter.Count)
                {
                    gridView.SetSelection(gridView.Adapter.Count);
                    Selection = gridView.Adapter.Count;
                    return;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void InitializeWebView(ObservableWebView view)
        {
            StorehouseWebViewClient client = new StorehouseWebViewClient();
            view.SetWebViewClient(client);
            view.Settings.JavaScriptEnabled = true;
            view.Settings.BuiltInZoomControls = false;
            view.VerticalScrollBarEnabled = false;
            view.Settings.SetRenderPriority(WebSettings.RenderPriority.High);
            view.Settings.CacheMode = CacheModes.NoCache;
            view.AddJavascriptInterface(new KingJavaScriptInterface(Activity, view), "KingJavaScriptInterface"); 

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

            if (primaryWebview.IsDeflated || secondaryWebview.IsDeflated)
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

        private const int CHAPTERS_MENU = 1;
        private const int TEXTSIZE_MENU = 7;

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);

            try
            {
                //menu.RemoveItem(CHAPTERS_MENU);
                menu.RemoveItem(TEXTSIZE_MENU);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //var chaptersMenu = menu.Add(0, CHAPTERS_MENU, CHAPTERS_MENU, "Chapters");
            //chaptersMenu.SetIcon(Resource.Drawable.chapters);
            //chaptersMenu.SetShowAsAction(ShowAsAction.IfRoom);

            var fontIncreaseMenu = menu.Add(0, TEXTSIZE_MENU, TEXTSIZE_MENU, "Text Size");
            fontIncreaseMenu.SetIcon(Resource.Drawable.fontsize);
            fontIncreaseMenu.SetShowAsAction(ShowAsAction.Never | ShowAsAction.WithText);

            optionsMenu = menu;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
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
                case (TEXTSIZE_MENU):
                    ShowFontSizeDialog();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ChangeScreen()
        {
            if (mode == ScreenMode.Duel)
            {
                mode = ScreenMode.Single;
            }
            else if (mode == ScreenMode.Single)
            {
                mode = ScreenMode.Duel;
            }

            App.STATE.CurrentScreenMode = mode;
            ThreadPool.QueueUserWorkItem((o) => App.STATE.SaveUserPreferences());

            FlipToCurrentScreenMode();
        }

        private void FlipToCurrentScreenMode()
        {
            //if (ChapterPrompt && primaryChapters.Count > 1 && library != Library.DailyText)
            //{
            //    flipper.DisplayedChild = (int)ScreenMode.Navigation;
            //    ChapterPrompt = false;
            //}
            //else
            //{
            //    mode = App.STATE.CurrentScreenMode;

            //    if (flipper.DisplayedChild != (int)mode)
            //    {
            //        flipper.DisplayedChild = (int)mode;
            //    }
            //}

            mode = App.STATE.CurrentScreenMode;

            if (flipper.DisplayedChild != (int)mode)
            {
                flipper.DisplayedChild = (int)mode;
            }
        }

        private void SelectChapter(object sender, AdapterView.ItemClickEventArgs e)
        {
            if(library == App.STATE.CurrentLibrary)
            {
                NavStruct selected = NavStruct.Parse(primaryArticles[e.Position].ArticleNumber);

                SelectedArticle = selected;

                DisplayArticles();
            }
        }

        public void ChangeArticle(int offset)
        {
            Console.WriteLine(Enum.GetName(typeof(Library), library));

            int currentIndex = Array.IndexOf(primaryArticles.Select(a => a.ArticleNumber).ToArray(), SelectedArticle.ToString());
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

            NavStruct nav = NavStruct.Parse(primaryArticles[index].ArticleNumber);

            SelectedArticle = nav;

            DisplayArticles();
        }

        private string GetArticle(string database)
        {
            string title = string.Empty;
            string html = string.Empty;

            //////////////////////////////////////////////////////////////////////////
            // TRY TO GET ARTICLE, IF NOT, DISPLAY NOTHING
            //////////////////////////////////////////////////////////////////////////
            try
            {
                // Retrieve Article
                WOLArticle article = GetFirstArticle(database);

                // Set Article content html
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

                html = HandleInjection(html);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            return html;
        }

        private void LoadArticlesFromStorehouses()
        {
            string primaryDatabase = LibraryStorehouse.English;
            string secondaryDatabase = LibraryStorehouse.Chinese;

            // Major important!
            if (!App.STATE.Swapped)
            {
                if (App.STATE.PinyinToggle)
                {
                    secondaryDatabase = LibraryStorehouse.Pinyin;
                }
            }
            else
            {
                primaryDatabase = LibraryStorehouse.Chinese;
                secondaryDatabase = LibraryStorehouse.English;
                if (App.STATE.PinyinToggle)
                {
                    primaryDatabase = LibraryStorehouse.Pinyin;
                }
            }

            //////////////////////////////////////////////////////////////////////////
            // BIBLE
            //////////////////////////////////////////////////////////////////////////
            if (library == Library.Bible)
            {
                Console.WriteLine("Getting primary . . .");
                primaryArticles = JwStore.QueryArticlesByBibleBook(SelectedArticle, primaryDatabase);
                foreach (var a in primaryArticles)
                {
                    primaryChapters.Add(Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>")));
                }

                Console.WriteLine("Getting secondary . . .");
                secondaryArticles = JwStore.QueryMatchByMEPS(PublicationType.Bible, primaryArticles, secondaryDatabase);
                foreach (var a in secondaryArticles)
                {
                    secondaryChapters.Add(Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>")));
                }

                Console.WriteLine("Getting COMPLETE!");
            }
            //////////////////////////////////////////////////////////////////////////
            // DAILY TEXT
            //////////////////////////////////////////////////////////////////////////
            else if (library == Library.DailyText)
            {
                primaryArticles = JwStore.QueryArticlesByPublication(PublicationType.DailyText, primaryDatabase);
                foreach (var a in primaryArticles)
                {
                    primaryChapters.Add(Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>")));
                }

                secondaryArticles = JwStore.QueryMatchByMEPS(PublicationType.DailyText, primaryArticles, secondaryDatabase);
                foreach (var a in secondaryArticles)
                {
                    secondaryChapters.Add(Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>")));
                }
            }
            //////////////////////////////////////////////////////////////////////////
            // INSIGHT VOLUMES
            //////////////////////////////////////////////////////////////////////////
            else if (library == Library.Insight)
            {
                string group = "it-" + App.STATE.CurrentArticleGroup.ToString();

                primaryArticles = JwStore.QueryInsightsByGroup(group, primaryDatabase);
                foreach (var a in primaryArticles)
                {
                    primaryChapters.Add(Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>") + "<br/><i>" + a.ArticleLocation + "</i>"));
                }

                secondaryArticles = JwStore.QueryMatchByChapters(PublicationType.Insight, primaryArticles, secondaryDatabase);
                foreach (var a in secondaryArticles)
                {
                    secondaryChapters.Add(Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>") + "<br/><i>" + a.ArticleLocation + "</i>"));
                }
            }
            //////////////////////////////////////////////////////////////////////////
            // BOOKS & PUBLICATIONS
            //////////////////////////////////////////////////////////////////////////
            else if (library == Library.Publications)
            {
                string code = App.FUNCTIONS.GetPublicationCode(SelectedArticle.Book.ToString());

                primaryArticles = JwStore.QueryArticlesByPublication(code, primaryDatabase);
                foreach (var a in primaryArticles)
                {
                    primaryChapters.Add(Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>") + "<br/><i>" + a.ArticleLocation + "</i>"));
                }

                secondaryArticles = JwStore.QueryMatchByChapters(code, primaryArticles, secondaryDatabase);
                foreach (var a in secondaryArticles)
                {
                    secondaryChapters.Add(Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>") + "<br/><i>" + a.ArticleLocation + "</i>"));
                }
            }
        }

        private void DisplayArticles()
        {
            MaterialDialog dialog = null;
            MaterialDialog.Builder progress = new MaterialDialog.Builder(Activity);
            progress.SetContent("Loading articles");
            progress.SetCancelable(false);
            progress.SetProgress(true, 0);

            ThreadPool.QueueUserWorkItem((o) =>
                {
                    Activity.RunOnUiThread(() => dialog = progress.Show());

                    if (primaryArticles.Count <= 0 || secondaryArticles.Count <= 0)
                    {
                        LoadArticlesFromStorehouses();
                    }

                    List<string> content = new List<string>();

                    content.Add(GetArticle("primary"));
                    content.Add(GetArticle("secondary"));

                    LoadWebViews(content);

                    Activity.RunOnUiThread(() => dialog.Dismiss());
                });

            //Task.Factory.StartNew<List<string>>(() =>
            //{
            //    List<string> content = new List<string>();

            //    content.Add(GetArticle("primary"));
            //    content.Add(GetArticle("secondary"));

            //    return content;
            //}).ContinueWith(antecendent => LoadWebViews(antecendent.Result));

            ThreadPool.QueueUserWorkItem((o) => App.STATE.SelectedArticle = SelectedArticle);
            ThreadPool.QueueUserWorkItem((o) => App.STATE.SaveUserPreferences());
        }

        private void LoadWebViews(List<string> contents)
        {
            HtmlContents = contents;

            //////////////////////////////////////////////////////////////////////////
            // LOAD ARTICLE INTO WEBVIEWS
            //////////////////////////////////////////////////////////////////////////
            Activity.RunOnUiThread(() =>
                {
                    primaryWebview.LoadDataWithBaseURL("file:///android_asset/", contents.First(), "text/html", "utf-8", null);
                    secondaryWebview.LoadDataWithBaseURL("file:///android_asset/", contents.Last(), "text/html", "utf-8", null);

                    HandleTopNav();
                    HandleRightNav();                                       

                    FlipToCurrentScreenMode();

                    RefreshActionBar();
                });
        }

        private void RefreshWebViews()
        {
            primaryWebview.LoadDataWithBaseURL("file:///android_asset/", HtmlContents.First(), "text/html", "utf-8", null);
            secondaryWebview.LoadDataWithBaseURL("file:///android_asset/", HtmlContents.Last(), "text/html", "utf-8", null);

            FlipToCurrentScreenMode();
        }

        private string HandleInjection(string html)
        {
            // References
            if (!App.STATE.Preferences.GetBoolean("references", true))
            {
                html = html.Replace("class='fn'", "class='fn' style='display: none;'").Replace("class='mr'", "class='mr' style='display: none;'");
            }
            else
            {
                html = html.Replace("class='fn' style='display: none;'", "class='fn'").Replace("class='mr' style='display: none;'", "class='mr'");
            }

            // Check if JavaScript file is there. If not, add it.
            if(!html.Contains(@"<script src=""js/init.js""></script>"))
            {
                html = html.Replace(@"<link href=""css/wol.css"" type=""text/css"" rel=""stylesheet""/>", @"<link href=""css/wol.css"" type=""text/css"" rel=""stylesheet""/><script src=""js/init.js""></script>");
            }
            
            // UNINSTALL THREE-LINE & LEARNING PARADIGM IF ANY
            html = Regex.Replace(html, @"(<rt style)(.*?)(</rt><ruby>)(.*?)(</ruby>)", delegate(Match match)
            {
                return match.Groups[4].Value;
            });
            html = html.Replace("onclick='DisplayLearningDialog(this)' ", "");

            // Learning Paradigm
            if (App.STATE.Preferences.GetBoolean("learningParadigm", true))
            {
                SQLiteConnection db = new SQLiteConnection(App.LearningCharactersPath);
                db.CreateTable<LearningCharacter>();

                var table = db.Table<LearningCharacter>();

                // INSTALL LEARNING PARADIGM
                html = Regex.Replace(html, @"(<ruby title=')(.*?)('>)(<rt>)(.*?)(</rt>)(<rb>)(.*?)(</rb></ruby>)", delegate(Match match)
                {
                    string english = match.Groups[2].Value;
                    string pinyin = match.Groups[5].Value;
                    string chinese = match.Groups[8].Value;
                    string remainder = "<rt>" + pinyin + "</rt><rb>" + chinese + "</rb></ruby>";

                    bool exists = table.Any(x => x.Chinese.ToLower().Equals(chinese.ToLower()) && x.Pinyin.ToLower().Equals(pinyin.ToLower()));
                    if (exists)
                    {
                        //Console.WriteLine("<ruby onclick='DisplayLearningDialog(this) title='" + english + "'><rt style='color:gray;font-size:0.7em'>" + english + "</rt><ruby>" + remainder + "</ruby>");
                        return "<ruby onclick='DisplayLearningDialog(this)' title=\"" + english + "\"><rt style='color:gray;font-size:0.7em;display:compact;'>" + english + "</rt><ruby>" + remainder + "</ruby>";
                    }
                    else
                    {
                        return "<ruby onclick='DisplayLearningDialog(this)' title=\"" + match.Groups[2].Value + "\">" + remainder;
                    }
                });
            }

            // INSTALL THREE-LINE
            if (App.STATE.Preferences.GetBoolean("threeLine", false))
            {
                html = Regex.Replace(html, @"(<ruby title=')(.*?)('>)(.*?)(</rb></ruby>)", delegate(Match match)
                {
                    return match.Groups[1].Value + match.Groups[2].Value + match.Groups[3].Value + "<rt style='color:gray;font-size:0.7em;display:compact;'>" + match.Groups[2].Value + "</rt><ruby>" + match.Groups[4].Value + "</rb></ruby></ruby>";
                });                
            }

            return html;
        }

        private void HandleTopNav()
        {
            List<ISpanned> articles;
            if (library == Library.Bible)
            {
                // Chapter numbers only
                articles = primaryChapters.Select(a => Html.FromHtml(a.ToString().Split(new[] { ' ' }).Last())).ToList();
            }
            else
            {
                // Article titles
                articles = primaryChapters;
            }

            int width = (int)(App.STATE.Activity.Resources.GetDimension(Resource.Dimension.bible_nav_bible_book_grid_width) / App.STATE.Activity.Resources.DisplayMetrics.Density);
            if (library != Library.Bible)
            {
                if (width > 100)
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
            var nav = (Activity as MainLibraryActivity).nav;
            nav.Adapter = new NavArrayAdapter<ISpanned>(Activity, Resource.Layout.DrawerListItemRight, Resource.Id.right_nav_item, primaryChapters, "Roboto-Light", Android.Graphics.TypefaceStyle.Normal);

            int index = GetNavigationIndex();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                nav.SetItemChecked(index, true);
            }
            nav.SetSelection(index);

            // Enable right nav drawer & close drawer
            var drawer = (Activity as MainLibraryActivity).drawer;
            drawer.SetDrawerLockMode(DrawerLayout.LockModeUnlocked, nav);
            drawer.CloseDrawer(nav); 
        }

        private int GetNavigationIndex()
        {
            int index = 0;
            if(library == Library.DailyText)
            {
                index = Array.IndexOf(primaryArticles.Select(a => a.ArticleNumber).ToArray(), SelectedArticle.ToString());
            }
            else
            {
                index = Array.IndexOf(primaryArticles.Select(a => NavStruct.Parse(a.ArticleNumber).Chapter.ToString()).ToArray(), SelectedArticle.Chapter.ToString());
            }

            return index;
        }

        private WOLArticle GetFirstArticle(string database)
        {
            WOLArticle article;

            if(database == "primary")
            {
                if (library == Library.DailyText)
                {
                    article = primaryArticles.First(a => a.ArticleNumber == SelectedArticle.ToString());
                }
                else
                {
                    article = primaryArticles.First(a => NavStruct.Parse(a.ArticleNumber).Chapter == SelectedArticle.Chapter);
                }
            }
            else
            {
                if (library == Library.DailyText)
                {
                    article = secondaryArticles.First(a => a.ArticleNumber == SelectedArticle.ToString());
                }
                else
                {
                    article = secondaryArticles.First(a => NavStruct.Parse(a.ArticleNumber).Chapter == SelectedArticle.Chapter);
                }
            }

            return article;
        }

        public void SetWebViewTextSize(int size)
        {
            // Set size in webview
            primaryWebview.Settings.DefaultFontSize = size;
            secondaryWebview.Settings.DefaultFontSize = size;
            gridViewFooterWebview.Settings.DefaultFontSize = size;
        }       

        public void ShowFontSizeDialog()
        {
            SeekBar seek = new SeekBar(Activity);
            seek.Max = 10;
            seek.Progress = App.STATE.SeekBarTextSize;
            seek.ProgressChanged += (o, e) =>
                {
                    int size = App.FUNCTIONS.GetWebViewTextSize(e.Progress);

                    SetWebViewTextSize(size);

                    // Set font size globally
                    App.STATE.SeekBarTextSize = e.Progress;

                    // Save font size to preferences
                    var prefs = PreferenceManager.GetDefaultSharedPreferences(Activity);
                    prefs.Edit().PutInt("WebViewBaseFontSize", e.Progress).Commit();
                };

            MaterialDialog dialog = null;
            MaterialDialog.Builder popup = new MaterialDialog.Builder(Activity);
            popup.SetCustomView(seek, false);
            popup.SetTitle("Text Size");
            popup.SetPositiveText("X");

            dialog = popup.Show();
        }

        public void ShowChapterPrompt()
        {
            string title = gridViewTitle.Text;

            LayoutInflater inflater = (LayoutInflater)Activity.GetSystemService(Context.LayoutInflaterService);
            View view = inflater.Inflate(Resource.Layout.DialogChapterSelect, null);
            HeaderFooterGridView grid = view.FindViewById<HeaderFooterGridView>(Resource.Id.chapterSelectGridView);
            grid.SetSelector(Android.Resource.Color.Transparent);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                grid.ChoiceMode = ChoiceMode.Single;
            }

            List<ISpanned> articles;
            if (library == Library.Bible)
            {
                // Chapter numbers only
                articles = primaryChapters.Select(a => Html.FromHtml(a.ToString().Split(new[] { ' ' }).Last())).ToList();
                grid.StretchMode = StretchMode.NoStretch;
                grid.NumColumns = -1;
            }
            else if (library == Library.Insight)
            {
                // Article titles
                articles = primaryChapters;
                grid.StretchMode = StretchMode.StretchColumnWidth;
                grid.NumColumns = 2;
            }
            else
            {
                // Article titles
                articles = primaryChapters;
                grid.StretchMode = StretchMode.StretchColumnWidth;
                grid.NumColumns = 1;
            }

            // If one article, do nothing
            if(articles.Count == 1)
            {
                return;
            }

            MaterialDialog dialog = null;

            grid.Adapter = new ArticleButtonAdapter(Activity, articles.ToArray());
            grid.ItemClick += SelectChapter;
            grid.ItemClick += delegate
            {
                dialog.Dismiss();
            };

            int index = GetNavigationIndex();
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                grid.SetItemChecked(index, true);
            }
            grid.SetSelection(index);

            MaterialDialog.Builder popup = new MaterialDialog.Builder(Activity);
            popup.SetCustomView(view, false);
            popup.SetTitle(title.Replace("\n", "<br/>").Split('<')[0]);
            popup.SetNegativeText("X");

            dialog = popup.Show();
        }
    }
}