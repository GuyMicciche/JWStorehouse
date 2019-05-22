using Android.App;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Xamarin.ActionbarSherlockBinding.App;

using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace JWStorehouse
{
    public class LibraryFragment : SherlockFragment
    {
        private View view;

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

            Console.WriteLine("Library Mode is " + (int)App.STATE.CurrentLibrary);

            view = inflater.Inflate(Resource.Layout.LibraryFragment, container, false);

            AddEventHandlers();

            InitializeLayoutParadigm(view);

            InitializeLibraryParadigm(LibraryMode);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            Console.WriteLine("LibraryFragment -> Resume " + LibraryMode.ToString());

            AttachStackedFragments();

            UpdateUI();
        }

        public override void OnPause()
        {
            base.OnPause();

            Console.WriteLine("LibraryFragment -> Pause " + LibraryMode.ToString());

            DetachStackedFragments();
        }

        public override void OnDestroy()
        {
            base.OnPause();

            Console.WriteLine("LibraryFragment -> Destroy " + LibraryMode.ToString());

            DestroyEventHandlers();
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

            if (LibraryMode == Library.Bible)
            {
                primaryLibraryGridViewTop.Tag = "hebrew";
                primaryLibraryGridViewBottom.Tag = "greek";
                secondaryLibraryGridViewTop.Tag = "hebrew";
                secondaryLibraryGridViewBottom.Tag = "greek";
            }
            else
            {
                primaryLibraryGridViewTop.Tag = App.STATE.PrimaryLanguage.EnglishName;
                primaryLibraryGridViewBottom.Tag = App.STATE.PrimaryLanguage.EnglishName;
                secondaryLibraryGridViewTop.Tag = App.STATE.SecondaryLanguage.EnglishName;
                secondaryLibraryGridViewBottom.Tag = App.STATE.SecondaryLanguage.EnglishName;

                ViewGroup primaryContainer = (ViewGroup)primaryLibraryGridViewBottom.Parent;
                ViewGroup secondaryContainer = (ViewGroup)secondaryLibraryGridViewBottom.Parent;

                primaryContainer.RemoveView(primaryLibraryGridViewBottom);
                secondaryContainer.RemoveView(secondaryLibraryGridViewBottom);

                if (LibraryMode == Library.Insight)
                {
                    secondaryContainer.RemoveView(secondaryLibraryGridViewTop);
                    secondaryContainer.RemoveView(view.FindViewById<TextView>(Resource.Id.secondaryLibraryTitle));
                }
            }

            primaryLibraryGridViewTop.ItemClick += LibraryGridView_Click;
            primaryLibraryGridViewBottom.ItemClick += LibraryGridView_Click;
            secondaryLibraryGridViewTop.ItemClick += LibraryGridView_Click;
            secondaryLibraryGridViewBottom.ItemClick += LibraryGridView_Click;

            UpdateUI();
        }

        public void UpdateUI()
        {
            var activity = ((MainLibraryActivity)Activity);
            App.STATE.SetActionBarTitle(activity.SupportActionBar, "JW Storehouse");

            primaryLibraryTextView.Text = App.STATE.PrimaryLanguage.LanguageName;
            secondaryLibraryTextView.Text = App.STATE.SecondaryLanguage.LanguageName;
        }

        private void LibraryGridView_Click(object sender, AdapterView.ItemClickEventArgs e)
        {
            NavStruct navStruct;
            int book = 0;
            int chapter = 0;
            int verse = 0;
            string nav = string.Empty;
            string tag = (string)((LibraryGridView)sender).Tag;

            // Bible Book
            if (LibraryMode == Library.Bible)
            {
                book = (tag == "hebrew") ? e.Position + 1 : e.Position + 40;
                nav = LibraryMode.ToString() + book.ToString();
                chapter = (App.STATE.ArticleNavigation.TryGetValue(nav, out navStruct)) ? App.STATE.ArticleNavigation[nav].Chapter : 1;
            }
            else if (App.STATE.CurrentLibrary == Library.Books)
            {
                // Selected book
                string selectedBook = (string)(sender as LibraryGridView).adapter.GetItem(e.Position);

                WOLArticle pub = App.STATE.PrimaryBooks.Single(a => a.PublicationName.Contains(selectedBook));

                book = NavStruct.Parse(pub.ArticleMEPSID).Book;
                nav = LibraryMode.ToString() + book.ToString();
                chapter = (App.STATE.ArticleNavigation.TryGetValue(nav, out navStruct)) ? App.STATE.ArticleNavigation[nav].Chapter : 1;
            }
            else if (App.STATE.CurrentLibrary == Library.Insight)
            {
                InsightArticle insight;

                insight = App.FUNCTIONS.GetInsightArticlesByGroup(e.Position).FirstOrDefault();

                //book = e.Position;
                //nav = LibraryMode.ToString() + e.Position.ToString();
                //chapter = (App.STATE.ArticleNavigation.TryGetValue(nav, out navStruct)) ? App.STATE.ArticleNavigation[nav].Chapter : int.Parse(insight.MEPSID);
                //verse = (App.STATE.ArticleNavigation.TryGetValue(nav, out navStruct)) ? App.STATE.ArticleNavigation[nav].Verse : insight.OrderNumber;

                book = e.Position;
                chapter = int.Parse(insight.MEPSID);
                verse = insight.OrderNumber;
            }

            App.STATE.CurrentArticleGroup = e.Position;

            NavStruct article = new NavStruct()
            {
                Book = book,
                Chapter = chapter,
                Verse = verse
            };

            LoadArticle(article); 
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

            ((MainLibraryActivity)Activity).stacks[(int)LibraryMode].Push(fragment);
        }

        void STATE_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {
            if (App.STATE.CanTranslate())
            {
                InitializeLibraryParadigm(LibraryMode);
            }
        }

        private void InitializeLibraryParadigm(Library mode)
        {
            if (App.STATE.CanTranslate())
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
                else if (mode == Library.Books)
                {
                    PopulateBooksGrid();
                }                
                // All other publications
                else
                {
                    App.STATE.Activity.RunOnUiThread(() =>
                    {
                        Toast.MakeText(App.STATE.Activity, "Not yet available in storehouse.", ToastLength.Long).Show();
                    });
                }

                UpdateUI();
            }            
        }

        private void PopulateBibleGrid()
        {
            int width = (int)(App.STATE.Context.Resources.GetDimension(Resource.Dimension.bible_nav_bible_book_grid_width) / App.STATE.Context.Resources.DisplayMetrics.Density);

            ArrayAdapter primaryHebrewAdapter;
            ArrayAdapter secondaryHebrewAdapter;
            ArrayAdapter primaryGreekAdapter;
            ArrayAdapter secondaryGreekAdapter;

            if (width > 100)
            {
                primaryHebrewAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.PrimaryBibleBooks.Select(b => b.Name).Take(39).ToArray());
                secondaryHebrewAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.SecondaryBibleBooks.Select(b => b.Name).Take(39).ToArray());
                primaryGreekAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.PrimaryBibleBooks.Select(b => b.Name).Skip(39).ToArray());
                secondaryGreekAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.SecondaryBibleBooks.Select(b => b.Name).Skip(39).ToArray());
            }
            else
            {
                primaryHebrewAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.PrimaryBibleBooks.Select(b => b.Abbreviation).Take(39).ToArray());
                secondaryHebrewAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.SecondaryBibleBooks.Select(b => b.Abbreviation).Take(39).ToArray());
                primaryGreekAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.PrimaryBibleBooks.Select(b => b.Abbreviation).Skip(39).ToArray());
                secondaryGreekAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.SecondaryBibleBooks.Select(b => b.Abbreviation).Skip(39).ToArray());
            }

            Activity.RunOnUiThread(() =>
            {
                primaryLibraryGridViewTop.SetAdapter(primaryHebrewAdapter);
                secondaryLibraryGridViewTop.SetAdapter(secondaryHebrewAdapter);
                primaryLibraryGridViewBottom.SetAdapter(primaryGreekAdapter);
                secondaryLibraryGridViewBottom.SetAdapter(secondaryGreekAdapter);
            });
        }

        private void PopulateInsightGrid()
        {
            var primaryAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.PrimaryInsightGroups);
            var secondaryAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.SecondaryInsightGroups);

            Activity.RunOnUiThread(() =>
            {
                primaryLibraryGridViewTop.SetAdapter(primaryAdapter);
                secondaryLibraryGridViewTop.SetAdapter(secondaryAdapter);
            });
        }

        private void PopulateBooksGrid()
        {            
            var primaryAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.PrimaryBooks.Select(b => b.PublicationName).ToArray());
            var secondaryAdapter = new ArrayAdapter(Activity, Resource.Layout.LibraryGridItem, App.STATE.SecondaryBooks.Select(b => b.PublicationName).ToArray());

            Activity.RunOnUiThread(() =>
            {
                primaryLibraryGridViewTop.SetAdapter(primaryAdapter);
                secondaryLibraryGridViewTop.SetAdapter(secondaryAdapter);
            });
        }
    }
}