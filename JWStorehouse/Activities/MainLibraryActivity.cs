using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Threading;
using Xamarin.ActionbarSherlockBinding.App;
using Xamarin.ActionbarSherlockBinding.Views;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using IMenu = global::Xamarin.ActionbarSherlockBinding.Views.IMenu;
using IMenuItem = global::Xamarin.ActionbarSherlockBinding.Views.IMenuItem;

namespace JWStorehouse
{
    [Activity(Label = "JW Storehouse", Icon = "@drawable/icon", Theme = "@style/Theme.Storehouse", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainLibraryActivity : SherlockFragmentActivity
    {
        public DrawerLayout drawer;
        public MyActionBarDrawerToggle toggle;
        public ListView list;
        public ListView nav;

        public Fragment SelectedFragment;

        public Dictionary<int, Stack<Fragment>> stacks = new Dictionary<int, Stack<Fragment>>();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.MainLibraryActivity);

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            App.STATE.Context = this;
            App.STATE.Activity = this;

            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            App.STATE.SetActionBarTitle(SupportActionBar, "JW Storehouse");

            list = FindViewById<ListView>(Resource.Id.left_drawer);
            nav = FindViewById<ListView>(Resource.Id.nav_drawer);
            list.ItemClick += (sender, args) => SelectLibrary(args.Position);

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            toggle = new MyActionBarDrawerToggle(this, drawer, Resource.Drawable.ic_drawer, Resource.String.drawer_open, Resource.String.drawer_close);
            toggle.DrawerClosed += delegate
            {
                SupportInvalidateOptionsMenu();
            };
            toggle.DrawerOpened += delegate
            {
                SupportInvalidateOptionsMenu();
            };
            drawer.SetDrawerListener(toggle);

            App.STATE.LoadUserPreferences();
            App.STATE.LanguageChanged += STATE_LanguageChanged;
        }

        protected override void OnPostCreate(Bundle bundle)
        {
            base.OnPostCreate(bundle);

            toggle.SyncState();

            InitializeStorehouse();
        }

        void STATE_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {
            RunOnUiThread(() =>
            {
                InitializeStorehouse();
            });

            ThreadPool.QueueUserWorkItem((o) => App.STATE.SaveUserPreferences());
        }

        private void InitializeStorehouse()
        {
           if (string.IsNullOrEmpty(App.STATE.Language))
            {
                if (App.FUNCTIONS.ConnectedToNetwork(this))
                {
                    App.FUNCTIONS.DownloadDuelLanguagePackDialog(this).Show();
                }
                else
                {
                    Toast.MakeText(this, "Unable to load languages. Check your internet connection.", ToastLength.Short).Show();
                }
            }
            else
            {
                GenerateStacks();
                LoadLeftNavigation();

                try
                {
                    // If there are fragments loaded, do nothing 
                    Stack<Fragment> stack = stacks[(int)App.STATE.CurrentLibrary];
                    if (stack.Count > 0)
                    {
                        drawer.CloseDrawer(list);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                SelectLibrary(App.STATE.CurrentLibrary);
            }
        }

        private void GenerateStacks()
        {
            if (stacks.Count > 0)
            {
                return;
            }

            foreach (Library m in App.STATE.Libraries)
            {
                stacks.Add((int)m, new Stack<Fragment>());
            }
        }

        private void LoadLeftNavigation()
        {
            List<string> navigation = new List<string>();
            string[] temp = Resources.GetStringArray(Resource.Array.LibraryNavigation);
            foreach (Library m in App.STATE.Libraries)
            {
                navigation.Add(temp[(int)m]);
            }
            list.Adapter = new NavArrayAdapter<string>(this, Resource.Layout.DrawerListItem, Resource.Id.left_nav_item, navigation, "Roboto-Light", TypefaceStyle.Normal);
        }

        private void SelectLibrary(int position)
        {
            // If selected the same, do nothing
            if ((int)App.STATE.CurrentLibrary == position)
            {
                drawer.CloseDrawer(list);
                return;
            }

            try
            {
                SelectLibrary(App.STATE.Libraries[position]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void SelectLibrary(Library library)
        {
            if(IsFinishing)
            {
                return;
            }

            App.STATE.CurrentLibrary = library;
            string tag = Enum.GetName(typeof(Library), library);

            FragmentTransaction transaction = SupportFragmentManager.BeginTransaction();
            transaction.SetTransition((int)FragmentTransit.FragmentFade);
            Fragment fragment = SupportFragmentManager.FindFragmentByTag(tag);

            if (App.STATE.CanTranslate())
            {
                if (fragment == null)
                {
                    if (App.STATE.CurrentLibrary == Library.DailyText)
                    {
                        string date = App.FUNCTIONS.FormatDateTime(DateTime.Now);

                        fragment = new ArticleFragment(NavStruct.Parse(date), library);
                        fragment.RetainInstance = true;
                    }
                    //else if (App.STATE.CurrentLibrary == Library.Insight)
                    //{
                    //    fragment = new InsightLibraryFragment();
                    //    fragment.RetainInstance = true;
                    //}
                    else
                    {
                        fragment = new LibraryFragment();
                        fragment.RetainInstance = true;
                    }

                    if (SelectedFragment != null)
                    {
                        transaction.Detach(SelectedFragment);
                    }
                    transaction.Add(Resource.Id.content_frame, fragment, tag);
                    transaction.Commit();
                }
                else
                {
                    transaction.Detach(SelectedFragment);
                    transaction.Attach(fragment);
                    transaction.Commit();
                }

                SelectedFragment = fragment;
                
                int index = App.STATE.Libraries.IndexOf(library);
                list.SetItemChecked(index, true);
                list.SetSelection(index);

                drawer.CloseDrawer(list);
            }
            else
            {
                // Temporary HACK
                SupportFragmentManager.PopBackStack(null, (int)PopBackStackFlags.Inclusive);

                SelectedFragment = null;
                transaction.Replace(Resource.Id.content_frame, new Fragment()).Commit();
                RunOnUiThread(() =>
                {
                    list.Adapter = null;
                });
            }

            Console.WriteLine("Current LibraryMode is " + App.STATE.CurrentLibrary.ToString());
        }
        
        public override void OnConfigurationChanged(Configuration config)
        {
            base.OnConfigurationChanged(config);

            toggle.OnConfigurationChanged(config);
        }


        private const int SWAP_MENU = 10;
        private const int RESET_MENU = 11;
        private const int SETTINGS_MENU = 12;

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            int group = 0;

            IMenuItem item1 = menu.Add(group, SWAP_MENU, SWAP_MENU, "Swap");
            item1.SetShowAsAction((int)ShowAsAction.Never | (int)ShowAsAction.WithText);

            IMenuItem item2 = menu.Add(group, RESET_MENU, RESET_MENU, "Reset Library");
            item2.SetShowAsAction((int)ShowAsAction.Never | (int)ShowAsAction.WithText);

            //IMenuItem item3 = menu.Add(group, SETTINGS_MENU, SETTINGS_MENU, "Settings");
            //item3.SetShowAsAction((int)ShowAsAction.Never | (int)ShowAsAction.WithText);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (item.ItemId == Android.Resource.Id.Home)
            {
                if (drawer.IsDrawerOpen(list))
                {
                    drawer.CloseDrawer(list);
                }
                else
                {
                    drawer.OpenDrawer(list);
                }
                return true;
            }

            switch (item.ItemId)
            {
                case (SWAP_MENU):
                    App.STATE.SwapLanguage();
                    return true;
                case (RESET_MENU):
                    ResetStorehouseConfirmation();
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void ResetStorehouseConfirmation()
        {
            var builder = new StorehouseDialogBuilder(this);
            builder.SetTitle("Reset Storehouse");
            builder.SetMessage("Are you sure? This will delete all languages.");
            builder.SetIcon(Resource.Drawable.Icon);
            builder.SetTitleTextColor(Resource.Color.storehouse_white);
            builder.SetTitleBackgroundColor(Android.Resource.Color.Transparent);
            builder.SetPositiveButton("RESET", (sender, e) =>
            {
                App.STATE.Language = "";
                stacks = new Dictionary<int, Stack<Fragment>>();
                SelectLibrary(Library.None);
                App.STATE.ResetStorehouse();
            });
            builder.SetNegativeButton("Cancel", delegate
            {

            });

            builder.Show();
        }
    }

    public class ActionBarDrawerEventArgs : EventArgs
    {
        public View DrawerView { get; set; }
        public float SlideOffset { get; set; }
        public int State { get; set; }
    }

    public delegate void ActionBarDrawerChangedEventHandler(object s, ActionBarDrawerEventArgs e);

    public class MyActionBarDrawerToggle : ActionBarDrawerToggle
    {
        public MyActionBarDrawerToggle(Activity activity, DrawerLayout drawerLayout, int drawerImageRes, int openDrawerContentDescRes, int closeDrawerContentDescRes)
            : base(activity, drawerLayout, drawerImageRes, openDrawerContentDescRes, closeDrawerContentDescRes)
        { }

        public event ActionBarDrawerChangedEventHandler DrawerClosed;
        public event ActionBarDrawerChangedEventHandler DrawerOpened;
        public event ActionBarDrawerChangedEventHandler DrawerSlide;
        public event ActionBarDrawerChangedEventHandler DrawerStateChanged;

        public override void OnDrawerClosed(View drawerView)
        {
            if (null != DrawerClosed)
                DrawerClosed(this, new ActionBarDrawerEventArgs { DrawerView = drawerView });
            base.OnDrawerClosed(drawerView);
        }

        public override void OnDrawerOpened(View drawerView)
        {
            if (null != DrawerOpened)
                DrawerOpened(this, new ActionBarDrawerEventArgs { DrawerView = drawerView });
            base.OnDrawerOpened(drawerView);
        }

        public override void OnDrawerSlide(View drawerView, float slideOffset)
        {
            if (null != DrawerSlide)
                DrawerSlide(this, new ActionBarDrawerEventArgs
                {
                    DrawerView = drawerView,
                    SlideOffset = slideOffset
                });
            base.OnDrawerSlide(drawerView, slideOffset);
        }

        public override void OnDrawerStateChanged(int state)
        {
            if (null != DrawerStateChanged)
                DrawerStateChanged(this, new ActionBarDrawerEventArgs
                {
                    State = state
                });
            base.OnDrawerStateChanged(state);
        }
    }
}