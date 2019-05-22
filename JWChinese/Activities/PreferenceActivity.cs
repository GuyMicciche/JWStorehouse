using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Support.V7.App;
using Android.Text;

using System;

namespace JWChinese
{
    [Activity(Label = "JW Chinese | Preferences", Theme = "@style/Theme.Storehouse.Material", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class SettingsActivity : ActionBarActivity
    {
        public static bool references;
        public static bool threeLine;
        public static bool learningParadigm;
        public static bool pinyinReferences;
        public static bool pinyinWol;

        public static string REFERENCES = "references";
        public static string THREE_LINE = "threeLine";
        public static string LEARNING_PARADIGM = "learningParadigm";
        public static string PINYIN_REFERENCES = "pinyinReferences";
        public static string PINYIN_WOL = "pinyinWol";

        public static bool refresh = false;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            App.FUNCTIONS.SetActionBarDrawable(this);

            SupportActionBar.SetDisplayShowHomeEnabled(true);
            //SupportActionBar.SetIcon(Resource.Drawable.Icon);

            // Display the fragment as the main content.
            FragmentManager.BeginTransaction().Replace(Android.Resource.Id.Content, new SettingsFragment()).Commit();

            // Set ActionBar type
            App.STATE.SetActionBarTitle(SupportActionBar, "JW Chinese", "Preferences & Settings");
        }

        public class SettingsFragment : PreferenceFragment, ISharedPreferencesOnSharedPreferenceChangeListener
        {
            public override void OnCreate(Bundle bundle)
            {
                base.OnCreate(bundle);

                AddPreferencesFromResource(Resource.Layout.PreferenceActivity);

                Preference appVersion = (Preference)FindPreference("appVersion");
                appVersion.Summary = GetVersion();

                SetKeys();

                if (pinyinWol)
                {
                    ((CheckBoxPreference)FindPreference(LEARNING_PARADIGM)).Enabled = false;
                    ((CheckBoxPreference)FindPreference(THREE_LINE)).Enabled = false;
                    ((CheckBoxPreference)FindPreference(PINYIN_REFERENCES)).Enabled = false;
                }
            }

            public override void OnResume()
            {
                base.OnResume();

                PreferenceScreen.SharedPreferences.RegisterOnSharedPreferenceChangeListener(this);
            }

            public override void OnPause()
            {
                base.OnPause();

                PreferenceScreen.SharedPreferences.UnregisterOnSharedPreferenceChangeListener(this);

                if (references != ((CheckBoxPreference)FindPreference(REFERENCES)).Checked)
                {
                    App.STATE.RefreshWebViews = true;
                }
                if (threeLine != ((CheckBoxPreference)FindPreference(THREE_LINE)).Checked)
                {
                    App.STATE.RefreshWebViews = true;
                }
                if (learningParadigm != ((CheckBoxPreference)FindPreference(LEARNING_PARADIGM)).Checked)
                {
                    App.STATE.RefreshWebViews = true;
                }
                if (pinyinWol != ((CheckBoxPreference)FindPreference(PINYIN_WOL)).Checked)
                {
                    App.STATE.RefreshWebViews = true;
                }
            }

            private string GetVersion()
            {
                try
                {
                    PackageInfo obj = Activity.PackageManager.GetPackageInfo(Activity.PackageName.ToString(), 0);
                    string version = ((PackageInfo)obj).VersionName;
                    return version;
                }
                catch (PackageManager.NameNotFoundException e)
                {
                    while (true)
                    {
                        Console.WriteLine("Unable to get package manager.", e);
                    }
                }
            }

            public void OnSharedPreferenceChanged(ISharedPreferences prefs, string key)
            {
                if (key.Equals(THREE_LINE))
                {
                    if (((CheckBoxPreference)FindPreference(THREE_LINE)).Checked)
                    {
                        ((CheckBoxPreference)FindPreference(LEARNING_PARADIGM)).Checked = false;
                    }
                }

                if (key.Equals(LEARNING_PARADIGM))
                {
                    if (((CheckBoxPreference)FindPreference(LEARNING_PARADIGM)).Checked)
                    {
                        ((CheckBoxPreference)FindPreference(THREE_LINE)).Checked = false;
                    }
                }

                if (key.Equals(PINYIN_WOL))
                {
                    ((CheckBoxPreference)FindPreference(LEARNING_PARADIGM)).Checked = false;
                    ((CheckBoxPreference)FindPreference(THREE_LINE)).Checked = false;
                    ((CheckBoxPreference)FindPreference(PINYIN_REFERENCES)).Checked = false;

                    TogglePinyinWol();
                }
            }

            private void SetKeys()
            {
                references = ((CheckBoxPreference)FindPreference(REFERENCES)).Checked;
                threeLine = ((CheckBoxPreference)FindPreference(THREE_LINE)).Checked;
                learningParadigm = ((CheckBoxPreference)FindPreference(LEARNING_PARADIGM)).Checked;
                pinyinReferences = ((CheckBoxPreference)FindPreference(PINYIN_REFERENCES)).Checked;
                pinyinWol = ((CheckBoxPreference)FindPreference(PINYIN_WOL)).Checked;
            }

            private void TogglePinyinWol(bool update = false)
            {
                bool enabled = ((CheckBoxPreference)FindPreference(PINYIN_WOL)).Checked;

                ((CheckBoxPreference)FindPreference(LEARNING_PARADIGM)).Checked = !enabled;
                ((CheckBoxPreference)FindPreference(LEARNING_PARADIGM)).Enabled = !enabled;
                ((CheckBoxPreference)FindPreference(THREE_LINE)).Enabled = !enabled;
                ((CheckBoxPreference)FindPreference(PINYIN_REFERENCES)).Enabled = !enabled;

                if (App.STATE.SecondaryLanguage.EnglishName.Contains("Pinyin") || App.STATE.PrimaryLanguage.EnglishName.Contains("Pinyin"))
                {
                    App.STATE.PinyinToggleParadigm(true);
                }
            }
        }
    }
}