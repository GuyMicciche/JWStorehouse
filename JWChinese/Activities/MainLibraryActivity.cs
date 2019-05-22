using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Preferences;
using Android.Support.V4.Widget;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Text;
using Android.Views;
using Android.Widget;
using AndroidDonationsLibrary;
using MaterialDialogs;
using Storehouse.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;
using IMenu = Android.Views.IMenu;
using IMenuItem = Android.Views.IMenuItem;

namespace JWChinese
{
    [Activity(Label = "JW Chinese", Icon = "@drawable/icon", Theme = "@style/Theme.Storehouse.Material", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainLibraryActivity : ActionBarActivity
    {
        public DrawerLayout drawer;
        public MyActionBarDrawerToggle toggle;
        public ListView list;
        public ListView nav;

        private bool active;

        public Fragment SelectedFragment;

        public Dictionary<int, Stack<Fragment>> stacks = new Dictionary<int, Stack<Fragment>>();

        private ActionMode actionMode = null;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            SetContentView(Resource.Layout.MainLibraryActivity);

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            //Window.SetBackgroundDrawableResource(Resource.Color.storehouse_blue);
            Color c = Resources.GetColor(Resource.Color.storehouse_blue);
            Drawable b = Resources.GetDrawable(Resource.Drawable.app_background);
            b.SetColorFilter(c, PorterDuff.Mode.Multiply);
            //Window.SetBackgroundDrawable(b);
            Window.SetBackgroundDrawableResource(Resource.Color.storehouse_blue);

            App.STATE.Activity = this;

            SupportActionBar.SetHomeButtonEnabled(true);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);
            //SupportActionBar.SetIcon(Resource.Drawable.Icon);
            
            App.STATE.SetActionBarTitle(SupportActionBar, "JW Chinese");

            list = FindViewById<ListView>(Resource.Id.left_drawer);
            nav = FindViewById<ListView>(Resource.Id.nav_drawer);
            list.ItemClick += (sender, args) => SelectLibrary(args.Position);

            // Set background of NavigationDrawer
            Color color = Resources.GetColor(Resource.Color.storehouse_blue);
            Drawable background = Resources.GetDrawable(Resource.Drawable.blue_bg);
            background.SetColorFilter(color, PorterDuff.Mode.Multiply);
            list.SetBackgroundDrawable(background);

            App.FUNCTIONS.SetActionBarDrawable(this);

            drawer = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            toggle = new MyActionBarDrawerToggle(this, drawer, Resource.String.drawer_open, Resource.String.drawer_close);
            toggle.DrawerClosed += delegate
            {
                SetActionBarArrow();
                SupportInvalidateOptionsMenu();
            };
            toggle.DrawerOpened += delegate
            {
                toggle.DrawerIndicatorEnabled = true;
                SupportInvalidateOptionsMenu();
            };
            drawer.SetDrawerListener(toggle);

            SupportFragmentManager.BackStackChanged += SupportFragmentManager_BackStackChanged;

            // Add stacks for each LibraryMode section
            stacks.Add((int)Library.Bible, new Stack<Fragment>());
            stacks.Add((int)Library.Insight, new Stack<Fragment>());
            stacks.Add((int)Library.DailyText, new Stack<Fragment>());
            stacks.Add((int)Library.Publications, new Stack<Fragment>());

            App.STATE.LoadUserPreferences();
            App.STATE.LanguageChanged += STATE_LanguageChanged;
        }
                        
        protected override void OnPostCreate(Bundle bundle)
        {
            base.OnPostCreate(bundle);

            toggle.SyncState();

            InitializeStorehouse();
        }

        protected override void OnStart()
        {
            base.OnStart();
            active = true;
        }

        protected override void OnStop()
        {
            base.OnStop();
            active = false;
        }

        protected override void OnDestroy()
        {
            SupportFragmentManager.BackStackChanged -= SupportFragmentManager_BackStackChanged;
            base.OnDestroy();
        }

        private void STATE_LanguageChanged(object sender, App.LanguageChangedArgs e)
        {
            RunOnUiThread(() =>
            {
                InitializeStorehouse();
            });
            
            ThreadPool.QueueUserWorkItem((o) => App.STATE.SaveUserPreferences());
        }

        private void SupportFragmentManager_BackStackChanged(object sender, EventArgs e)
        {
            SetActionBarArrow();
        }

        private void InitializeStorehouse()
        {
            LoadLeftNavigation();
            SelectLibrary(App.STATE.CurrentLibrary);
        }

        private void LoadLeftNavigation()
        {
            List<string> navigation = new List<string>();

            Console.WriteLine("Loading left navigation . . . " + App.STATE.Language);

            if (App.STATE.Language.Contains("English"))
            {
                foreach (string m in Resources.GetStringArray(Resource.Array.LibraryNavigation))
                {
                    navigation.Add(m);
                }
            }
            else if (App.STATE.Language.Contains("Simplified"))
            {
                foreach (string m in Resources.GetStringArray(Resource.Array.LibraryNavigationChinese))
                {
                    navigation.Add(m);
                }
            }
            else if (App.STATE.Language.Contains("Pinyin"))
            {
                foreach (string m in Resources.GetStringArray(Resource.Array.LibraryNavigationPinyin))
                {
                    navigation.Add(m);
                }
            }

            list.Adapter = new NavArrayAdapter<string>(this, Resource.Layout.DrawerListItem, Resource.Id.left_nav_item, navigation, "Roboto-Light", TypefaceStyle.Normal, true);
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
                bool memorize = App.STATE.Preferences.GetBoolean("memorizeLibraryArticle", true);
                if(memorize)
                {
                    SelectLibrary(App.STATE.Libraries[position]);
                }
                else
                {
                    SelectNewLibrary(App.STATE.Libraries[position]);
                }

            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void SelectNewLibrary(Library library)
        {
            if (IsFinishing)
            {
                return;
            }

            // If there are fragments loaded, do nothing 
            Stack<Fragment> stack = stacks[(int)App.STATE.CurrentLibrary];
            if (stack.Count > 0)
            {
                drawer.CloseDrawer(list);
                return;
            }

            Fragment fragment;

            App.STATE.CurrentLibrary = library;
            string tag = Enum.GetName(typeof(Library), library);

            FragmentManager manager = SupportFragmentManager;
            FragmentTransaction transaction = manager.BeginTransaction();
            transaction.SetTransition((int)FragmentTransit.FragmentFade);

            if (App.STATE.CurrentLibrary == Library.DailyText)
            {
                string date = App.FUNCTIONS.FormatDateTime(DateTime.Now);

                fragment = new ArticleFragment(NavStruct.Parse(date), library);
            }
            else
            {
                fragment = new LibraryFragment();
            }

            transaction.Replace(Resource.Id.content_frame, fragment);
            transaction.Commit();

            int index = App.STATE.Libraries.IndexOf(library);
            list.SetItemChecked(index, true);
            list.SetSelection(index);

            drawer.CloseDrawer(list);

            Console.WriteLine("Current LibraryMode is " + App.STATE.CurrentLibrary.ToString());
        }

        private void SelectLibrary(Library library)
        {
            if (IsFinishing)
            {
                return;
            }

            //// If there are fragments loaded, do nothing 
            //Stack<Fragment> stack = stacks[(int)App.STATE.CurrentLibrary];
            //if (stack.Count > 0)
            //{
            //    drawer.CloseDrawer(list);
            //    return;
            //}

            App.STATE.CurrentLibrary = library;
            string tag = Enum.GetName(typeof(Library), library);

            FragmentManager manager = SupportFragmentManager;
            FragmentTransaction transaction = manager.BeginTransaction();
            transaction.SetTransition((int)FragmentTransit.FragmentFade);
            Fragment fragment = SupportFragmentManager.FindFragmentByTag(tag);

            if (fragment == null)
            {
                if (App.STATE.CurrentLibrary == Library.DailyText)
                {
                    string date = App.FUNCTIONS.FormatDateTime(DateTime.Now);

                    fragment = new ArticleFragment(NavStruct.Parse(date), library);
                    fragment.RetainInstance = true;
                }
                else
                {
                    fragment = new LibraryFragment();
                    fragment.RetainInstance = true;
                }

                if(SelectedFragment != null)
                {
                    transaction.Detach(SelectedFragment);
                }
                transaction.Add(Resource.Id.content_frame, fragment, tag);
            }
            else
            {
                transaction.Detach(SelectedFragment);
                transaction.Attach(fragment);
            }

            // COMMIT TRANSACTION
            if (active)
            {
                transaction.Commit();
            }
            else
            {
                transaction.CommitAllowingStateLoss();
            }

            SelectedFragment = fragment;

            int index = App.STATE.Libraries.IndexOf(library);
            list.SetItemChecked(index, true);
            list.SetSelection(index);

            drawer.CloseDrawer(list);

            Console.WriteLine("Current LibraryMode is " + App.STATE.CurrentLibrary.ToString());
        }
        
        private const int SWAP_MENU = 10;
        private const int PINYIN_MENU = 11;
        private const int SYNC_MENU = 12;
        private const int SETTINGS_MENU = 13;

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            int group = 0;

            IMenuItem item1 = menu.Add(group, SWAP_MENU, SWAP_MENU, "Swap Language");
            item1.SetIcon(Resource.Drawable.swap);
            item1.SetShowAsAction(ShowAsAction.Never | ShowAsAction.WithText);

            IMenuItem item2 = menu.Add(group, PINYIN_MENU, PINYIN_MENU, "Pinyin Toggle");
            item2.SetIcon(Resource.Drawable.pinyin);
            item2.SetShowAsAction(ShowAsAction.IfRoom);

            //IMenuItem item3 = menu.Add(group, SYNC_MENU, SYNC_MENU, "Sync");
            //item3.SetShowAsAction(ShowAsAction.Never | ShowAsAction.WithText);

            IMenuItem item4 = menu.Add(group, SETTINGS_MENU, SETTINGS_MENU, "Settings");
            item4.SetShowAsAction(ShowAsAction.Never | ShowAsAction.WithText);

            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            if (toggle.DrawerIndicatorEnabled && toggle.OnOptionsItemSelected((Android.Views.IMenuItem)item))
            {
                return true;
            }

            if (item.ItemId == Android.Resource.Id.Home)
            {
                SupportFragmentManager.PopBackStackImmediate();

                return true;
            }

            switch (item.ItemId)
            {
                case (SWAP_MENU):
                    App.STATE.SwapLanguage();
                    return true;
                case (PINYIN_MENU):
                    App.STATE.PinyinToggleParadigm();
                    return true;
                case (SYNC_MENU):
                    App.FUNCTIONS.UserSyncDialog(this);
                    return true;
                case (SETTINGS_MENU):
                    StartActivity(typeof(SettingsActivity));
                    return true;
            }

            return base.OnOptionsItemSelected(item);
        }

        public override void OnBackPressed()
        {
            Stack<Fragment> stack = stacks[(int)App.STATE.CurrentLibrary];
            if (stack.Count > 0)
            {
                if(stack.Count == 1)
                {
                    drawer.SetDrawerLockMode(DrawerLayout.LockModeLockedClosed, nav);
                }
                base.OnBackPressed();
            }
            else
            {
                MaterialDialog dialog = null;
                MaterialDialog.Builder popup = new MaterialDialog.Builder(this);
                popup.SetTitle("Exit Application?");
                popup.SetPositiveText("Yes", (o, e) =>
                {
                    base.OnBackPressed();
                });
                popup.SetNegativeText("No");

                dialog = popup.Show();
            }
        }

        public void SetActionBarArrow()
        {
            int backStackEntryCount = SupportFragmentManager.BackStackEntryCount;
            toggle.DrawerIndicatorEnabled = (backStackEntryCount == 0);
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
        private MainLibraryActivity activity;

        public MyActionBarDrawerToggle(MainLibraryActivity activity, DrawerLayout drawerLayout, int openDrawerContentDescRes, int closeDrawerContentDescRes)
            : base(activity, drawerLayout, openDrawerContentDescRes, closeDrawerContentDescRes)
        {
            this.activity = activity;
        }

        public event ActionBarDrawerChangedEventHandler DrawerClosed;
        public event ActionBarDrawerChangedEventHandler DrawerOpened;
        public event ActionBarDrawerChangedEventHandler DrawerSlide;
        public event ActionBarDrawerChangedEventHandler DrawerStateChanged;

        public override void OnDrawerClosed(View drawerView)
        {
            if (null != DrawerClosed)
            {
                DrawerClosed(this, new ActionBarDrawerEventArgs 
                { 
                    DrawerView = drawerView 
                });
            }

            base.OnDrawerClosed(drawerView);
        }

        public override void OnDrawerOpened(View drawerView)
        {
            if (null != DrawerOpened)
            {
                DrawerOpened(this, new ActionBarDrawerEventArgs 
                { 
                    DrawerView = drawerView
                });
            }

            base.OnDrawerOpened(drawerView);
        }

        public override void OnDrawerSlide(View drawerView, float slideOffset)
        {
            if (null != DrawerSlide)
            {
                DrawerSlide(this, new ActionBarDrawerEventArgs
                {
                    DrawerView = drawerView,
                    SlideOffset = slideOffset
                });
            }

            base.OnDrawerSlide(drawerView, slideOffset);
        }

        public override void OnDrawerStateChanged(int state)
        {
            if (null != DrawerStateChanged)
            {
                DrawerStateChanged(this, new ActionBarDrawerEventArgs
                {
                    State = state
                });
            }

            base.OnDrawerStateChanged(state);
        }
    }
}