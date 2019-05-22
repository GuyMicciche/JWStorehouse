using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Text;
using Android.Views;
using Android.Widget;

using AndroidDonationsLibrary;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using FragmentTransaction = Android.Support.V4.App.FragmentTransaction;

namespace JWChinese
{
    [Activity(Label = "JW Chinese | Donations", Icon = "@drawable/icon", Theme = "@style/Theme.Storehouse.Material")]
    public class AndroidDonationsActivity : ActionBarActivity
    {
        // Google
        private const string GOOGLE_PUBKEY = "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnAtEDv/z61eaiFcyVOOB+OjbV5faLPLzhn6sMNSxb+HizNsslVS4sG5w/YfW6NJnkwMDa4BNPSMeHHlbEQDIoupU4f0U4+QqKNhdStsbu1TZ3WdpnCwqlsZALs+DGwq76NOP/IxIdSv7PX7Hd0cy9ZyQGu9t4Mvf+CyV+Y94XPMvsQvKFSHot+UC+GdpFzKHn9LI+fqoif2hW6oh2JC2Z7bs2BS3I7gV0jXK6aJd3ZXiGuL+iiRvi5auzRN4wv6m1jDY7mZKL62fDVqqyRwH4Q7qRdVkezWj/mCXV0HZvpFPDtWX/X3U4dtm8d3Tw4wwVwQg6GbDHN6lw8JWYgEtcwIDAQAB";
        private static string[] GOOGLE_CATALOG = new string[] { "guy.donate.1", "guy.donate.2", "guy.donate.3", "guy.donate.5", "guy.donate.7", "guy.donate.14", "guy.donate.20", "guy.donate.50" };

        // PayPal
        private const string PAYPAL_USER = "king.guy@outlook.com";
        private const string PAYPAL_CURRENCY_CODE = "USD";

        // Flattr
        private const string FLATTR_PROJECT_URL = "https://github.com/dschuermann/android-donations-lib/";
        private const string FLATTR_URL = "flattr.com/thing/712895/dschuermannandroid-donations-lib-on-GitHub";

        // BitCoin
        private const string BITCOIN_ADDRESS = "1CXUJDMaXNed69U42okCxeMyiGHjboVw1j";

        private string donationType = "google";

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            App.FUNCTIONS.SetActionBarDrawable(this);

            SetContentView(Resource.Layout.GEMDonationsActivity);

            donationType = Intent.GetStringExtra("donationType") ?? "google";
                
            // Set ActionBar type
            App.STATE.SetActionBarTitle(SupportActionBar, "JW Chinese", "Donations");

            FragmentTransaction ft = SupportFragmentManager.BeginTransaction();
            DonationsFragment donationsFragment;
            if (donationType == "google")
            {
                donationsFragment = DonationsFragment.newInstance(false, true, GOOGLE_PUBKEY, GOOGLE_CATALOG, Resources.GetStringArray(Resource.Array.donation_google_catalog_values), false, null, null, null, false, null, null, false, null);
            }
            else
            {
                donationsFragment = DonationsFragment.newInstance(false, false, null, null, null, true, PAYPAL_USER, PAYPAL_CURRENCY_CODE, GetString(Resource.String.donation_paypal_item), false, FLATTR_PROJECT_URL, FLATTR_URL, false, BITCOIN_ADDRESS);
            }

            ft.Replace(Resource.Id.donations_activity_container, (Fragment)donationsFragment, "donationsFragment");
            ft.Commit();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            FragmentManager fragmentManager = SupportFragmentManager;
            Fragment fragment = fragmentManager.FindFragmentByTag("donationsFragment");
            if (fragment != null)
            {
                fragment.OnActivityResult(requestCode, (int)resultCode, data);
            }
        }
    }

    public static class BuildConfig
    {
        public static bool DonationsGoogle
        {
            get
            {
                return true;
            }
        }

        public static bool Debug
        {
            get
            {
                return false;
            }
        }
    }
}