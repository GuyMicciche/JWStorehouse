using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Support.V4.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;

using MaterialDialogs;

using SQLite;

using Storehouse.Core;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace JWChinese
{
    public class LibraryFragment : Fragment
    {
        public View view;

        private TextView primaryLibraryTextView;
        private TextView secondaryLibraryTextView;

        private LibraryGridView primaryLibraryGridViewTop;
        private LibraryGridView primaryLibraryGridViewBottom;
        private LibraryGridView secondaryLibraryGridViewTop;
        private LibraryGridView secondaryLibraryGridViewBottom;

        private Library LibraryMode;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            LibraryMode = App.STATE.CurrentLibrary;

            Console.WriteLine("LibraryFragment -> Create " + LibraryMode.ToString());
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            if (view != null)
            {
                ((ViewGroup)view.Parent).RemoveView(view);

                return view;
            }

            Console.WriteLine("LibraryFragment -> CreateView " + LibraryMode.ToString());

            view = inflater.Inflate(Resource.Layout.LibraryFragment, container, false);

            AddEventHandlers();
            
            InitializeLayoutParadigm(view);

            InitializeLibraryParadigm(LibraryMode);


            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            // Disable right nav drawer
            var activity = ((MainLibraryActivity)Activity);
            ListView nav = activity.nav;
            DrawerLayout drawer = activity.drawer;
            drawer.SetDrawerLockMode(DrawerLayout.LockModeLockedClosed, nav);

            AttachStackedFragments();

            UpdateUI();

            Console.WriteLine("LibraryFragment -> Resume " + LibraryMode.ToString());
        }

        public override void OnPause()
        {
            base.OnPause();

            DetachStackedFragments();

            Console.WriteLine("LibraryFragment -> Pause " + LibraryMode.ToString());
        }
        
        public override void OnDestroy()
        {
            base.OnPause();

            DestroyEventHandlers();

            Console.WriteLine("LibraryFragment -> Destroy " + LibraryMode.ToString());
        }

        private void AddEventHandlers()
        {
            App.STATE.LanguageChanged += STATE_LanguageChanged;
        }

        private void DestroyEventHandlers()
        {
            App.STATE.LanguageChanged -= STATE_LanguageChanged;
        }

        private void AttachStackedFragments()
        {
            try
            {
                Stack<Fragment> stack = ((MainLibraryActivity)Activity).stacks[(int)LibraryMode];

                FragmentManager manager = Activity.SupportFragmentManager;
                FragmentTransaction transaction = manager.BeginTransaction();
                transaction.SetTransition((int)FragmentTransit.FragmentFade);

                if (stack != null)
                {
                    foreach (Fragment f in stack)
                    {
                        transaction.Attach(f);
                    }
                }

                transaction.Commit();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void DetachStackedFragments()
        {
            try
            {
                Stack<Fragment> stack = ((MainLibraryActivity)Activity).stacks[(int)LibraryMode];

                FragmentManager manager = Activity.SupportFragmentManager;
                FragmentTransaction transaction = manager.BeginTransaction();
                transaction.SetTransition((int)FragmentTransit.FragmentFade);

                if (stack != null)
                {
                    foreach (Fragment f in stack)
                    {
                        transaction.Detach(f);
                    }
                }

                transaction.Commit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void InitializeLayoutParadigm(View view)
        {
            primaryLibraryGridViewTop = view.FindViewById<LibraryGridView>(Resource.Id.primaryLibraryGridView1);
            primaryLibraryGridViewBottom = view.FindViewById<LibraryGridView>(Resource.Id.primaryLibraryGridView2);
            secondaryLibraryGridViewTop = view.FindViewById<LibraryGridView>(Resource.Id.secondaryLibraryGridView1);
            secondaryLibraryGridViewBottom = view.FindViewById<LibraryGridView>(Resource.Id.secondaryLibraryGridView2);

            primaryLibraryTextView = view.FindViewById<TextView>(Resource.Id.primaryLibraryTitle);
            secondaryLibraryTextView = view.FindViewById<TextView>(Resource.Id.secondaryLibraryTitle);

            Typeface face = Typeface.CreateFromAsset(Activity.Assets, "fonts/Roboto-Light.ttf");
            primaryLibraryTextView.SetTypeface(face, TypefaceStyle.Normal);
            secondaryLibraryTextView.SetTypeface(face, TypefaceStyle.Normal);

            ViewGroup primaryContainer = (ViewGroup)primaryLibraryGridViewBottom.Parent;
            ViewGroup secondaryContainer = (ViewGroup)secondaryLibraryGridViewBottom.Parent;

            // Remove or keep views
            if (LibraryMode != Library.Bible)
            {
                primaryContainer.RemoveView(primaryLibraryGridViewBottom);
                secondaryContainer.RemoveView(secondaryLibraryGridViewBottom);

                if (LibraryMode == Library.Insight)
                {
                    secondaryContainer.RemoveView(secondaryLibraryGridViewTop);
                    secondaryContainer.RemoveView(secondaryLibraryGridViewBottom);
                    secondaryContainer.RemoveView(view.FindViewById<TextView>(Resource.Id.secondaryLibraryTitle));
                }
            }

            // Remove unwanted views
            primaryContainer.RemoveView(primaryLibraryTextView);
            secondaryContainer.RemoveView(secondaryLibraryTextView);

            secondaryContainer.RemoveView(secondaryLibraryGridViewTop);
            secondaryContainer.RemoveView(secondaryLibraryGridViewBottom);
            secondaryContainer.RemoveView(view.FindViewById<TextView>(Resource.Id.secondaryLibraryTitle));

            // Add click listener to gridviews if none
            primaryLibraryGridViewTop.ItemClick += LibraryGridView_Click;
            primaryLibraryGridViewBottom.ItemClick += LibraryGridView_Click;
            secondaryLibraryGridViewTop.ItemClick += LibraryGridView_Click;
            secondaryLibraryGridViewBottom.ItemClick += LibraryGridView_Click;

            UpdateUI();
        }

        public void UpdateUI()
        {
            try
            {
                var activity = ((MainLibraryActivity)Activity);

                int index = activity.list.CheckedItemPosition;
                string subtitle = ((NavArrayAdapter<string>)activity.list.Adapter).objects[index];
                App.STATE.SetActionBarTitle(activity.SupportActionBar, "JW Chinese", subtitle);

                primaryLibraryTextView.Text = App.STATE.PrimaryLanguage.LanguageName.ToUpper();
                secondaryLibraryTextView.Text = App.STATE.SecondaryLanguage.LanguageName.ToUpper();

                // Update tag information for click listener
                if (LibraryMode == Library.Bible)
                {
                    primaryLibraryGridViewTop.Tag = "hebrew " + App.STATE.PrimaryLanguage.EnglishName;
                    primaryLibraryGridViewBottom.Tag = "greek " + App.STATE.PrimaryLanguage.EnglishName;
                    secondaryLibraryGridViewTop.Tag = "hebrew " + App.STATE.SecondaryLanguage.EnglishName;
                    secondaryLibraryGridViewBottom.Tag = "greek " + App.STATE.SecondaryLanguage.EnglishName;
                }
                else
                {
                    primaryLibraryGridViewTop.Tag = App.STATE.PrimaryLanguage.EnglishName;
                    primaryLibraryGridViewBottom.Tag = App.STATE.PrimaryLanguage.EnglishName;
                    secondaryLibraryGridViewTop.Tag = App.STATE.SecondaryLanguage.EnglishName;
                    secondaryLibraryGridViewBottom.Tag = App.STATE.SecondaryLanguage.EnglishName;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void LibraryGridView_Click(object sender, AdapterView.ItemClickEventArgs e)
        {
            App.STATE.CurrentArticleGroup = e.Position;

            int book = 0;
            int chapter = 0;
            int verse = 0;
            string lang = string.Empty;
            string mepsID = string.Empty;
            string storehouse = string.Empty;
            string insightGroup = "it-" + e.Position;
            string tag = (string)((LibraryGridView)sender).Tag;
            bool showPrompt = PreferenceManager.GetDefaultSharedPreferences(Activity.ApplicationContext).GetBoolean("chapterSelectionFirst", true);
            //string title = ((LibraryGridView)sender).Adapter.GetItem(e.Position).ToString().Replace("\n", "<br/>").Split('<')[0];

            NavStruct article = new NavStruct();
            WOLPublication pub = new WOLPublication();
            
            if (tag.Contains("English"))
            {
                storehouse = LibraryStorehouse.English;
                lang = "en-";
            }
            else if (tag.Contains("Simplified"))
            {
                storehouse = LibraryStorehouse.Chinese;
                lang = "ch-";
            }
            else if (tag.Contains("Pinyin"))
            {
                storehouse = LibraryStorehouse.Pinyin;
                lang = "ch-";
            }
            
            // Bible Book
            if (LibraryMode == Library.Bible)
            {
                pub.Code = PublicationType.Bible;

                book = (tag.Contains("hebrew")) ? e.Position + 1 : e.Position + 40;
                mepsID = LibraryMode.ToString() + book.ToString();
                chapter = (App.STATE.ArticleNavigation.TryGetValue(mepsID, out article)) ? App.STATE.ArticleNavigation[mepsID].Chapter : 1;
            }
            // Publication
            else if (LibraryMode == Library.Publications)
            {
                pub = App.FUNCTIONS.GetAllWOLPublications(true).ElementAt(e.Position);

                book = int.Parse(pub.Number);
                mepsID = LibraryMode.ToString() + book.ToString();
                chapter = (App.STATE.ArticleNavigation.TryGetValue(mepsID, out article)) ? App.STATE.ArticleNavigation[mepsID].Chapter : 1;
            }
            // Insight
            else if (LibraryMode == Library.Insight)
            {
                pub.Code = PublicationType.Insight;

                WOLArticle insight = new SQLiteConnection(storehouse).Query<WOLArticle>("select ArticleNumber from WOLArticle where ArticleGroup = ? limit 1", insightGroup).Single();

                book = e.Position;
                chapter = (App.STATE.ArticleNavigation.TryGetValue(mepsID, out article)) ? App.STATE.ArticleNavigation[mepsID].Chapter : NavStruct.Parse(insight.ArticleNumber).Chapter;

                insight = new SQLiteConnection(storehouse).Query<WOLArticle>("select ArticleNumber from WOLArticle where ArticleNumber like ? limit 1", "%" + chapter.ToString() + "%").Single();
                verse = NavStruct.Parse(insight.ArticleNumber).Verse;
            }

            // Set WOL article to load into ArticleFragment;
            article = new NavStruct()
            {
                Book = book,
                Chapter = chapter,
                Verse = verse
            };

            if(showPrompt)
            {
                //Set Publication attributes to load chapter titles into dialog for selection
                int width = (int)(App.STATE.Activity.Resources.GetDimension(Resource.Dimension.bible_nav_bible_book_grid_width) / App.STATE.Activity.Resources.DisplayMetrics.Density);
                if (LibraryMode == Library.Bible)
                {
                    pub.Name = App.FUNCTIONS.GetAllBibleBooks(App.STATE.PrimaryLanguage.EnglishName).Single(b => b.Number.Equals((book - 1).ToString())).Name;
                }
                else
                {
                    if (width > 100)
                    {
                        pub.Name = App.FUNCTIONS.GetPublicationName(App.STATE.Language, pub.Code);
                    }
                    else
                    {
                        pub.Name = App.FUNCTIONS.GetPublicationName(App.STATE.Language, pub.Code, true);
                    }
                }
                pub.Group = insightGroup;

                ShowChapterPrompt(storehouse, pub, article);
            }
            else
            {
                LoadArticle(article);
            }
        }

        private void ShowChapterPrompt(string storehouse, WOLPublication pub, NavStruct article)
        {
            LayoutInflater inflater = (LayoutInflater)Activity.GetSystemService(Context.LayoutInflaterService);
            View view = inflater.Inflate(Resource.Layout.DialogChapterSelect, null);
            HeaderFooterGridView gridview = view.FindViewById<HeaderFooterGridView>(Resource.Id.chapterSelectGridView);
            gridview.SetSelector(Android.Resource.Color.Transparent);

            List<WOLArticle> articles;
            List<ISpanned> titles;
            if (LibraryMode == Library.Bible)
            {
                gridview.NumColumns = -1;
                gridview.StretchMode = StretchMode.NoStretch;

                string bookNumber = article.Book.ToString() + ".";

                articles = JwStore.QueryArticleChapterTitles(PublicationType.Bible, storehouse)
                    .Where(a => a.ArticleNumber.StartsWith(bookNumber)).ToList();
               
                titles = articles.Select(a => Html.FromHtml(a.ArticleTitle.ToString().Split(new[] { ' ' }).Last())).ToList();
                if(titles.Count == 1)
                {
                    LoadArticle(article);

                    return;
                }

            }
            else if(LibraryMode == Library.Insight)
            {
                gridview.NumColumns = 2;
                gridview.StretchMode = StretchMode.StretchColumnWidth;

                articles = JwStore.QueryArticleChapterTitles(PublicationType.Insight, storehouse)
                    .Where(i => i.ArticleGroup.Equals(pub.Group))
                    .ToList();

                titles = articles.Select(a => Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>").Split('<')[0] + "<br/><i>" + a.ArticleLocation + "</i>")).ToList();
            }
            else
            {
                gridview.NumColumns = 1;
                gridview.StretchMode = StretchMode.StretchColumnWidth;

                articles = JwStore.QueryArticleChapterTitles(pub.Code, storehouse)
                    .ToList();

                titles = articles.Select(a => Html.FromHtml(a.ArticleTitle.Replace("\n", "<br/>").Split('<')[0] + "<br/><i>" + a.ArticleLocation + "</i>")).ToList();
                if (titles.Count == 1)
                {
                    LoadArticle(article);

                    return;
                }
            }
            
            MaterialDialog dialog = null;

            gridview.Adapter = new ArticleButtonAdapter(Activity, titles.ToArray());
            gridview.ItemClick += (s, args) =>
            {
                dialog.Dismiss();

                article = NavStruct.Parse(articles[args.Position].ArticleNumber);
                LoadArticle(article);
            };

            MaterialDialog.Builder popup = new MaterialDialog.Builder(Activity);
            popup.SetCustomView(view, false);
            popup.SetTitle(pub.Name.Replace("\n", "<br/>").Split('<')[0]);
            popup.SetNegativeText("X");

            dialog = popup.Show();
        }

        private void LoadArticle(NavStruct article)
        {
            Fragment fragment = new ArticleFragment(article, LibraryMode);
            fragment.RetainInstance = true;

            FragmentManager manager = Activity.SupportFragmentManager;
            FragmentTransaction transaction = manager.BeginTransaction();
            transaction.SetTransition((int)FragmentTransit.FragmentFade);
            transaction.Add(Resource.Id.content_frame, fragment, null);
            transaction.AddToBackStack(null);
            transaction.Commit();

            bool memorize = PreferenceManager.GetDefaultSharedPreferences(Activity.ApplicationContext).GetBoolean("memorizeLibraryArticle", true);
            if (memorize)
            {
                ((MainLibraryActivity)Activity).stacks[(int)LibraryMode].Push(fragment);
            }
        }
                
        void STATE_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {
            InitializeLibraryParadigm(LibraryMode);
        }

        private void InitializeLibraryParadigm(Library mode)
        {
            // Bible
            if (mode == Library.Bible)
            {
                PopulateBibleGrid();
            }
            // Insight
            else if (mode == Library.Insight)
            {
                PopulateInsightGrid();
            }
            // Daily Text
            else if (mode == Library.DailyText)
            {
                //TODO
            }
            // Books
            else if (mode == Library.Publications)
            {
                PopulatePublicationsGrid();
            }

            UpdateUI();
        }

        private void PopulateBibleGrid()
        {
            int width = (int)(App.STATE.Activity.Resources.GetDimension(Resource.Dimension.bible_nav_bible_book_grid_width) / App.STATE.Activity.Resources.DisplayMetrics.Density);

            // Full title
            if (width > 100)
            {
                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllBibleBooks(App.STATE.PrimaryLanguage.EnglishName)).ContinueWith(antecedent =>
                {
                    primaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.Name).Take(39).ToList<string>()));
                    primaryLibraryGridViewBottom.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.Name).Skip(39).ToList<string>()));
                }).Wait();

                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllBibleBooks(App.STATE.SecondaryLanguage.EnglishName)).ContinueWith(antecedent =>
                {
                    secondaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.Name).Take(39).ToList<string>()));
                    secondaryLibraryGridViewBottom.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.Name).Skip(39).ToList<string>()));
                }).Wait();

            }
            // Short title
            else if (width > 60)
            {
                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllBibleBooks(App.STATE.PrimaryLanguage.EnglishName)).ContinueWith(antecedent =>
                {
                    primaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.ShortName).Take(39).ToList<string>()));
                    primaryLibraryGridViewBottom.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.ShortName).Skip(39).ToList<string>()));
                }).Wait();

                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllBibleBooks(App.STATE.SecondaryLanguage.EnglishName)).ContinueWith(antecedent =>
                {
                    secondaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.ShortName).Take(39).ToList<string>()));
                    secondaryLibraryGridViewBottom.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.ShortName).Skip(39).ToList<string>()));
                }).Wait();

            }
            // Abbreviated title
            else
            {
                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllBibleBooks(App.STATE.PrimaryLanguage.EnglishName)).ContinueWith(antecedent =>
                {
                    primaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.Abbreviation).Take(39).ToList<string>()));
                    primaryLibraryGridViewBottom.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.Abbreviation).Skip(39).ToList<string>()));
                }).Wait();

                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllBibleBooks(App.STATE.SecondaryLanguage.EnglishName)).ContinueWith(antecedent =>
                {
                    secondaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.Abbreviation).Take(39).ToList<string>()));
                    secondaryLibraryGridViewBottom.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result.Select(b => b.Abbreviation).Skip(39).ToList<string>()));
                }).Wait();
            }
        }

        private void PopulateInsightGrid()
        {
            List<string> primary = App.FUNCTIONS.GetAllInsightGroups(App.STATE.PrimaryLanguage.EnglishName);
            List<string> secondary = App.FUNCTIONS.GetAllInsightGroups(App.STATE.SecondaryLanguage.EnglishName);

            primaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, primary);
            secondaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, secondary);
        }

        private void PopulatePublicationsGrid()
        {
            int width = (int)(App.STATE.Activity.Resources.GetDimension(Resource.Dimension.bible_nav_bible_book_grid_width) / App.STATE.Activity.Resources.DisplayMetrics.Density);

            // Full title
            if (width > 100)
            {
                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllPublicationNames(App.STATE.PrimaryLanguage.EnglishName, true)).ContinueWith(antecedent =>
                {
                    primaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result));
                }).Wait();

                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllPublicationNames(App.STATE.SecondaryLanguage.EnglishName, true)).ContinueWith(antecedent =>
                {
                    secondaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result));
                }).Wait();

            }
            // Abbreviated title
            else
            {
                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllPublicationNames(App.STATE.PrimaryLanguage.EnglishName, true, true)).ContinueWith(antecedent =>
                {
                    primaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result));
                }).Wait();

                Task.Factory.StartNew(() => App.FUNCTIONS.GetAllPublicationNames(App.STATE.SecondaryLanguage.EnglishName, true, true)).ContinueWith(antecedent =>
                {
                    secondaryLibraryGridViewTop.Adapter = new LibraryGridButtonAdapter(Activity, (antecedent.Result));
                }).Wait();
            }
        }
    }
}