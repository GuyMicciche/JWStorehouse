using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace JWChinese
{
    [Activity(MainLauncher = true, Theme = "@style/Theme.Splash", NoHistory = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class SplashActivity : Activity
    {
        public SplashActivity()
        {

        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            StartActivity(typeof(LibraryDownloaderActivity));
        }
    }
}