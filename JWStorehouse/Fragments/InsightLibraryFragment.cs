using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Views;
using Android.Widget;

using StickyGridHeaders;

using System;
using System.Collections.Generic;
using System.Linq;

using Xamarin.ActionbarSherlockBinding.App;
using Xamarin.ActionbarSherlockBinding.Views;

using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using ListFragment = Android.Support.V4.App.ListFragment;


namespace JWStorehouse
{
    public class InsightLibraryFragment : SherlockFragment, StickyGridHeadersGridView.IOnHeaderClickListener, StickyGridHeadersGridView.IOnHeaderLongClickListener
    {
        private View view;
        public GridView primaryGrid;
        public GridView secondaryGrid;

        private TextView primaryLibraryTextView;
        private TextView secondaryLibraryTextView;

        private Toast toast;

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

            view = inflater.Inflate(Resource.Layout.InsightLibraryFragment, container, false);

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();

            Console.WriteLine("LibraryFragment -> Resume " + LibraryMode.ToString());

            AttachStackedFragments();
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

        void STATE_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {

        }

        private void AttachStackedFragments()
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

        private void DetachStackedFragments()
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

        public void OnHeaderClick(AdapterView parent, View view, long id)
        {
            Console.WriteLine("CLICK!");

            string text = "Header " + ((TextView)view.FindViewById(Android.Resource.Id.Text1)).Text + " was tapped.";
            if (toast == null)
            {
                toast = Toast.MakeText(Activity, text, ToastLength.Short);
            }
            else
            {
                toast.SetText(text);
            }
            toast.Show();
        }

        public bool OnHeaderLongClick(AdapterView parent, View view, long id)
        {
            Console.WriteLine("LONG CLICK!");

            string text = "Header " + ((TextView)view.FindViewById(Android.Resource.Id.Text1)).Text + " was long pressed.";
            if (toast == null)
            {
                toast = Toast.MakeText(Activity, text, ToastLength.Short);
            }
            else
            {
                toast.SetText(text);
            }
            toast.Show();

            return true;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            primaryLibraryTextView = view.FindViewById<TextView>(Resource.Id.primaryLibraryTitle);
            secondaryLibraryTextView = view.FindViewById<TextView>(Resource.Id.secondaryLibraryTitle);
            primaryLibraryTextView.Text = App.STATE.PrimaryLanguage.LanguageName;
            secondaryLibraryTextView.Text = App.STATE.SecondaryLanguage.LanguageName;

            primaryGrid = (GridView)view.FindViewById(Resource.Id.insightPrimaryGridView);
            secondaryGrid = (GridView)view.FindViewById(Resource.Id.insightSecondaryGridView);

            List<WOLArticle> primaryArticles = new List<WOLArticle>();
            List<WOLArticle> secondaryArticles = new List<WOLArticle>();

            List<InsightArticle> primaryArticleNames = App.STATE.PrimaryInsightArticles;
            List<InsightArticle> secondaryArticleNames = App.STATE.SecondaryInsightArticles;

            primaryGrid.Adapter = new StickyGridHeadersSimpleArrayAdapter<string>(Activity, primaryArticleNames.Select(a => a.Title).ToArray(), Resource.Layout.LibraryGridHeader, Resource.Layout.LibraryGridItem);
            primaryGrid.ItemClick += delegate(object sender, AdapterView.ItemClickEventArgs e)
            {
                Console.WriteLine("ITEM CLICK!");
            };
            ((StickyGridHeadersGridView)primaryGrid).OnHeaderClickListener = this;
            ((StickyGridHeadersGridView)primaryGrid).OnHeaderLongClickListener = this;

            secondaryGrid.Adapter = new StickyGridHeadersSimpleArrayAdapter<string>(Activity, secondaryArticleNames.Select(a => a.Title).ToArray(), Resource.Layout.LibraryGridHeader, Resource.Layout.LibraryGridItem);
            secondaryGrid.ItemClick += delegate(object sender, AdapterView.ItemClickEventArgs args)
            {
                Console.WriteLine("ITEM CLICK!");
            };
            ((StickyGridHeadersGridView)secondaryGrid).OnHeaderClickListener = this;
            ((StickyGridHeadersGridView)secondaryGrid).OnHeaderLongClickListener = this;

            SetHasOptionsMenu(true);
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
    }
}