using Android.App;
using Android.Content;
using Android.Preferences;
using Android.Util;

using System;

namespace JWChinese
{
    public class DonateDialogPreference : DialogPreference
    {
        private Context context;

        public DonateDialogPreference(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            this.context = context;
        }

        protected override void OnDialogClosed(bool positiveResult)
        {
            base.OnDialogClosed(positiveResult);
        }

        protected override void OnPrepareDialogBuilder(AlertDialog.Builder builder)
        {
            base.OnPrepareDialogBuilder(builder);

            builder.SetIcon(Resource.Drawable.Icon);
            builder.SetPositiveButton("PayPal", pos_Click);
            builder.SetNegativeButton("Google Play", neg_Click);
        }

        void pos_Click(object sender, EventArgs e)
        {
            //Intent browserIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse("https://dl.dropboxusercontent.com/u/826238/paypaldonate.html"));
            //context.StartActivity(browserIntent);

            Intent intent = new Intent(context, typeof(AndroidDonationsActivity));
            intent.PutExtra("donationType", "paypal");
            context.StartActivity(intent);

            //string html = "<html><head><meta name=\"HandheldFriendly\" content=\"true\" /><meta name=\"viewport\" content=\"width=device-width, height=device-height, user-scalable=no\" /></head><body><center><h2>JW Chinese</h2><div>Thank you Jehovah and all other supporters.</div><div>Donations help maintain JW Chinese for Android.</div><p><div><form action=\"https://www.paypal.com/cgi-bin/webscr\" method=\"post\" target=\"_top\"><input type=\"hidden\" name=\"cmd\" value=\"_s-xclick\"><input type=\"hidden\" name=\"hosted_button_id\" value=\"B5EZZKJ7JTAZU\"><input type=\"image\" src=\"https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif\" border=\"0\" name=\"submit\" alt=\"PayPal - The safer, easier way to pay online!\"><img alt=\"\" border=\"0\" src=\"https://www.paypalobjects.com/en_US/i/scr/pixel.gif\" width=\"1\" height=\"1\"></form></div></center></body></html>";
            //App.FUNCTIONS.PresentationDialog(context, "Donations", html, true).Show();
        }

        void neg_Click(object sender, EventArgs e)
        {
            Intent intent = new Intent(context, typeof(AndroidDonationsActivity));
            intent.PutExtra("donationType", "google");
            context.StartActivity(intent);
        }
    }
}