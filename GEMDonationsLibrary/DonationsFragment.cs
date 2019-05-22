using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using AndroidDonationsLibrary.Google.Util;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fragment = Android.Support.V4.App.Fragment;

namespace AndroidDonationsLibrary
{
	public class DonationsFragment : Fragment
	{
		public const string ARG_DEBUG = "debug";

		public const string ARG_GOOGLE_ENABLED = "googleEnabled";
		public const string ARG_GOOGLE_PUBKEY = "googlePubkey";
		public const string ARG_GOOGLE_CATALOG = "googleCatalog";
		public const string ARG_GOOGLE_CATALOG_VALUES = "googleCatalogValues";

		public const string ARG_PAYPAL_ENABLED = "paypalEnabled";
		public const string ARG_PAYPAL_USER = "paypalUser";
		public const string ARG_PAYPAL_CURRENCY_CODE = "paypalCurrencyCode";
		public const string ARG_PAYPAL_ITEM_NAME = "mPaypalItemName";

		public const string ARG_FLATTR_ENABLED = "flattrEnabled";
		public const string ARG_FLATTR_PROJECT_URL = "flattrProjectUrl";
		public const string ARG_FLATTR_URL = "flattrUrl";

		public const string ARG_BITCOIN_ENABLED = "bitcoinEnabled";
		public const string ARG_BITCOIN_ADDRESS = "bitcoinAddress";

		private const string TAG = "Donations Library";

		// http://developer.android.com/google/play/billing/billing_testing.html
		private static readonly string[] CATALOG_DEBUG = new string[]{"android.test.purchased", "android.test.canceled", "android.test.refunded", "android.test.item_unavailable"};

		private Spinner googleSpinner;
		private TextView glattrUrlTextView;

		// Google Play helper object
		private IabHelper helper;

		private bool debug = false;

		private bool googleEnabled = false;
		private string googlePubkey = "";
		private string[] googleCatalog = new string[]{};
		private string[] googleCatalogValues = new string[]{};

		private bool paypalEnabled = false;
		private string paypalUser = "";
		private string paypalCurrencyCode = "";
		private string paypalItemName = "";

		private bool flattrEnabled = false;
		private string flattrProjectUrl = "";
		private string flattrUrl = "";

		private bool bitcoinEnabled = false;
		private string bitcoinAddress = "";

		/// <summary>
		/// Instantiate DonationsFragment.
		/// </summary>
		/// <param name="debug">               You can use BuildConfig.DEBUG to propagate the debug flag from your app to the Donations library </param>
		/// <param name="googleEnabled">       Enabled Google Play donations </param>
		/// <param name="googlePubkey">        Your Google Play public key </param>
		/// <param name="googleCatalog">       Possible item names that can be purchased from Google Play </param>
		/// <param name="googleCatalogValues"> Values for the names </param>
		/// <param name="paypalEnabled">       Enable PayPal donations </param>
		/// <param name="paypalUser">          Your PayPal email address </param>
		/// <param name="paypalCurrencyCode">  Currency code like EUR. See here for other codes:
		///                            https://developer.paypal.com/webapps/developer/docs/classic/api/currency_codes/#id09A6G0U0GYK </param>
		/// <param name="paypalItemName">      Display item name on PayPal, like "Donation for NTPSync" </param>
		/// <param name="flattrEnabled">       Enable Flattr donations </param>
		/// <param name="flattrProjectUrl">    The project URL used on Flattr </param>
		/// <param name="flattrUrl">           The Flattr URL to your thing. NOTE: Enter without http:// </param>
		/// <param name="bitcoinEnabled">      Enable bitcoin donations </param>
		/// <param name="bitcoinAddress">      The address to receive bitcoin </param>
		/// <returns> DonationsFragment </returns>
		public static DonationsFragment newInstance(bool debug, bool googleEnabled, string googlePubkey, string[] googleCatalog, string[] googleCatalogValues, bool paypalEnabled, string paypalUser, string paypalCurrencyCode, string paypalItemName, bool flattrEnabled, string flattrProjectUrl, string flattrUrl, bool bitcoinEnabled, string bitcoinAddress)
		{
			DonationsFragment donationsFragment = new DonationsFragment();
			Bundle args = new Bundle();

			args.PutBoolean(ARG_DEBUG, debug);

			args.PutBoolean(ARG_GOOGLE_ENABLED, googleEnabled);
			args.PutString(ARG_GOOGLE_PUBKEY, googlePubkey);
			args.PutStringArray(ARG_GOOGLE_CATALOG, googleCatalog);
			args.PutStringArray(ARG_GOOGLE_CATALOG_VALUES, googleCatalogValues);

			args.PutBoolean(ARG_PAYPAL_ENABLED, paypalEnabled);
			args.PutString(ARG_PAYPAL_USER, paypalUser);
			args.PutString(ARG_PAYPAL_CURRENCY_CODE, paypalCurrencyCode);
			args.PutString(ARG_PAYPAL_ITEM_NAME, paypalItemName);

			args.PutBoolean(ARG_FLATTR_ENABLED, flattrEnabled);
			args.PutString(ARG_FLATTR_PROJECT_URL, flattrProjectUrl);
			args.PutString(ARG_FLATTR_URL, flattrUrl);

			args.PutBoolean(ARG_BITCOIN_ENABLED, bitcoinEnabled);
			args.PutString(ARG_BITCOIN_ADDRESS, bitcoinAddress);

			donationsFragment.Arguments = args;
			return donationsFragment;
		}

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			debug = Arguments.GetBoolean(ARG_DEBUG);

			googleEnabled = Arguments.GetBoolean(ARG_GOOGLE_ENABLED);
			googlePubkey = Arguments.GetString(ARG_GOOGLE_PUBKEY);
			googleCatalog = Arguments.GetStringArray(ARG_GOOGLE_CATALOG);
			googleCatalogValues = Arguments.GetStringArray(ARG_GOOGLE_CATALOG_VALUES);

			paypalEnabled = Arguments.GetBoolean(ARG_PAYPAL_ENABLED);
			paypalUser = Arguments.GetString(ARG_PAYPAL_USER);
			paypalCurrencyCode = Arguments.GetString(ARG_PAYPAL_CURRENCY_CODE);
			paypalItemName = Arguments.GetString(ARG_PAYPAL_ITEM_NAME);

			flattrEnabled = Arguments.GetBoolean(ARG_FLATTR_ENABLED);
			flattrProjectUrl = Arguments.GetString(ARG_FLATTR_PROJECT_URL);
			flattrUrl = Arguments.GetString(ARG_FLATTR_URL);

			bitcoinEnabled = Arguments.GetBoolean(ARG_BITCOIN_ENABLED);
			bitcoinAddress = Arguments.GetString(ARG_BITCOIN_ADDRESS);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			View view = inflater.Inflate(Resource.Layout.donations__fragment, container, false);

			return view;
		}

		public override void OnActivityCreated(Bundle savedInstanceState)
		{
			base.OnActivityCreated(savedInstanceState);

			/* Flattr */
			if (flattrEnabled)
			{
				// inflate flattr view into stub
				ViewStub flattrViewStub = (ViewStub) Activity.FindViewById(Resource.Id.donations__flattr_stub);
				flattrViewStub.Inflate();

				buildFlattrView();
			}

			/* Google */
			if (googleEnabled)
			{
				// inflate google view into stub
                ViewStub googleViewStub = (ViewStub)Activity.FindViewById(Resource.Id.donations__google_stub);
				googleViewStub.Inflate();

				// choose donation amount
                googleSpinner = (Spinner)Activity.FindViewById(Resource.Id.donations__google_android_market_spinner);

				ArrayAdapter adapter;
				if (debug)
				{
                    adapter = new ArrayAdapter(Activity, Resource.Layout.donation__item, CATALOG_DEBUG);
				}
				else
				{
                    adapter = new ArrayAdapter(Activity, Resource.Layout.donation__item, googleCatalogValues);
				}

                adapter.SetDropDownViewResource(Resource.Layout.donation__item);
				googleSpinner.Adapter = adapter;

                Button btGoogle = (Button)Activity.FindViewById(Resource.Id.donations__google_android_market_donate_button);
				btGoogle.SetOnClickListener(new OnClickListenerAnonymousInnerClassHelper(this));

				// Create the helper, passing it our context and the public key to verify signatures with
				if (debug)
				{
					Log.Debug(TAG, "Creating IAB helper.");
				}
				helper = new IabHelper(Activity, googlePubkey);

				// enable debug logging (for a production application, you should set this to false).
				helper.enableDebugLogging(debug);

				// Start setup. This is asynchronous and the specified listener
				// will be called once setup completes.
				if (debug)
				{
					Log.Debug(TAG, "Starting setup.");
				}
				helper.startSetup(new OnIabSetupFinishedListenerAnonymousInnerClassHelper(this));
			}

			/* PayPal */
			if (paypalEnabled)
			{
				// inflate paypal view into stub
                ViewStub paypalViewStub = (ViewStub)Activity.FindViewById(Resource.Id.donations__paypal_stub);
				paypalViewStub.Inflate();

                Button btPayPal = (Button)Activity.FindViewById(Resource.Id.donations__paypal_donate_button);
				btPayPal.SetOnClickListener(new OnClickListenerAnonymousInnerClassHelper2(this));
			}

			/* Bitcoin */
			if (bitcoinEnabled)
			{
				// inflate bitcoin view into stub
                ViewStub bitcoinViewStub = (ViewStub)Activity.FindViewById(Resource.Id.donations__bitcoin_stub);
				bitcoinViewStub.Inflate();

                Button btBitcoin = (Button)Activity.FindViewById(Resource.Id.donations__bitcoin_button);
                btBitcoin.SetOnClickListener(new OnClickListenerAnonymousInnerClassHelper3(this));
				btBitcoin.SetOnLongClickListener(new OnLongClickListenerAnonymousInnerClassHelper(this));
			}
		}

		private class OnClickListenerAnonymousInnerClassHelper : Java.Lang.Object, View.IOnClickListener
		{
			private DonationsFragment outerInstance;

			public OnClickListenerAnonymousInnerClassHelper(DonationsFragment outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public void OnClick(View v)
			{
				outerInstance.donateGoogleOnClick(v);
			}
		}

		private class OnIabSetupFinishedListenerAnonymousInnerClassHelper : IabHelper.OnIabSetupFinishedListener
		{
			private DonationsFragment outerInstance;

			public OnIabSetupFinishedListenerAnonymousInnerClassHelper(DonationsFragment outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public void onIabSetupFinished(IabResult result)
			{
				if (outerInstance.debug)
				{
					Log.Debug(TAG, "Setup finished.");
				}

				if (!result.Success)
				{
					// Oh noes, there was a problem.
					outerInstance.openDialog(Android.Resource.Drawable.IcDialogAlert, Resource.String.donations__google_android_market_not_supported_title, outerInstance.GetString(Resource.String.donations__google_android_market_not_supported));
					return;
				}

				// Have we been disposed of in the meantime? If so, quit.
				if (outerInstance.helper == null)
				{
					return;
				}
			}
		}

        private class OnClickListenerAnonymousInnerClassHelper2 : Java.Lang.Object, View.IOnClickListener
		{
			private readonly DonationsFragment outerInstance;

			public OnClickListenerAnonymousInnerClassHelper2(DonationsFragment outerInstance)
			{
				this.outerInstance = outerInstance;
			}


			public void OnClick(View v)
			{
				outerInstance.donatePayPalOnClick(v);
			}
		}

        private class OnClickListenerAnonymousInnerClassHelper3 : Java.Lang.Object, View.IOnClickListener
		{
			private DonationsFragment outerInstance;

			public OnClickListenerAnonymousInnerClassHelper3(DonationsFragment outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public void OnClick(View v)
			{
				outerInstance.donateBitcoinOnClick(v);
			}
		}

        private class OnLongClickListenerAnonymousInnerClassHelper : Java.Lang.Object, View.IOnLongClickListener
		{
			private DonationsFragment outerInstance;

			public OnLongClickListenerAnonymousInnerClassHelper(DonationsFragment outerInstance)
			{
				this.outerInstance = outerInstance;
			}


			public bool OnLongClick(View v)
			{
				Toast.MakeText(outerInstance.Activity, Resource.String.donations__bitcoin_toast_copy, ToastLength.Long).Show();
				if (Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Honeycomb)
				{
					ClipboardManager clipboard = (ClipboardManager)outerInstance.Activity.GetSystemService(Context.ClipboardService);
					clipboard.Text = outerInstance.bitcoinAddress;
				}
				else
				{
					ClipboardManager clipboard = (ClipboardManager)outerInstance.Activity.GetSystemService(Context.ClipboardService);
					ClipData clip = ClipData.NewPlainText(outerInstance.bitcoinAddress, outerInstance.bitcoinAddress);
					clipboard.PrimaryClip = clip;
				}
				return true;
			}
		}

		/// <summary>
		/// Open dialog
		/// </summary>
		/// <param name="icon"> </param>
		/// <param name="title"> </param>
		/// <param name="message"> </param>
		private void openDialog(int icon, int title, string message)
		{
			AlertDialog.Builder dialog = new AlertDialog.Builder(Activity);
			dialog.SetIcon(icon);
            dialog.SetTitle(title);
            dialog.SetMessage(message);
            dialog.SetCancelable(true);
            dialog.SetNeutralButton(Resource.String.donations__button_close, new OnClickListenerAnonymousInnerClassHelper4(this, dialog));
			dialog.Show();
		}

        private class OnClickListenerAnonymousInnerClassHelper4 : Java.Lang.Object, IDialogInterfaceOnClickListener
		{
			private DonationsFragment outerInstance;

			private AlertDialog.Builder dialog;

			public OnClickListenerAnonymousInnerClassHelper4(DonationsFragment outerInstance, AlertDialog.Builder dialog)
			{
				this.outerInstance = outerInstance;
				this.dialog = dialog;
			}

			public void OnClick(IDialogInterface dialog, int which)
			{
				dialog.Dismiss();
			}
		}

		/// <summary>
		/// Donate button executes donations based on selection in spinner
		/// </summary>
		/// <param name="view"> </param>
		public void donateGoogleOnClick(View view)
		{
			int index;
			index = googleSpinner.SelectedItemPosition;
			if (debug)
			{
				Log.Debug(TAG, "selected item in spinner: " + index);
			}

            // Callback for when a purchase is finished
		    IabHelper.OnIabPurchaseFinishedListener mPurchaseFinishedListener = new OnIabPurchaseFinishedListenerAnonymousInnerClassHelper(this);

			if (debug)
			{
				// when debugging, choose android.test.x item
				helper.launchPurchaseFlow(Activity, CATALOG_DEBUG[index], IabHelper.ITEM_TYPE_INAPP, 0, mPurchaseFinishedListener, null);
			}
			else
			{
				helper.launchPurchaseFlow(Activity, googleCatalog[index], IabHelper.ITEM_TYPE_INAPP, 0, mPurchaseFinishedListener, null);
			}
		}

		private class OnIabPurchaseFinishedListenerAnonymousInnerClassHelper : IabHelper.OnIabPurchaseFinishedListener
		{
            private DonationsFragment outerInstance;

			public OnIabPurchaseFinishedListenerAnonymousInnerClassHelper(DonationsFragment outerInstance)
			{
                this.outerInstance = outerInstance;
			}

			public void onIabPurchaseFinished(IabResult result, Purchase purchase)
			{
                // Called when consumption is complete
                IabHelper.OnConsumeFinishedListener mConsumeFinishedListener = new OnConsumeFinishedListenerAnonymousInnerClassHelper(outerInstance);

				if (outerInstance.debug)
				{
					Log.Debug(TAG, "Purchase finished: " + result + ", purchase: " + purchase);
				}

				// if we were disposed of in the meantime, quit.
				if (outerInstance.helper == null)
				{
					return;
				}

				if (result.Success)
				{
					if (outerInstance.debug)
					{
						Log.Debug(TAG, "Purchase successful.");
					}

					// directly consume in-app purchase, so that people can donate multiple times
					outerInstance.helper.consumeAsync(purchase, mConsumeFinishedListener);

					// show thanks openDialog
					outerInstance.openDialog(Android.Resource.Drawable.IcDialogInfo, Resource.String.donations__thanks_dialog_title, outerInstance.GetString(Resource.String.donations__thanks_dialog));
				}
			}
		}

		private class OnConsumeFinishedListenerAnonymousInnerClassHelper : IabHelper.OnConsumeFinishedListener
		{
            private DonationsFragment outerInstance;

			public OnConsumeFinishedListenerAnonymousInnerClassHelper(DonationsFragment outerInstance)
			{
                this.outerInstance = outerInstance;
			}

			public void onConsumeFinished(Purchase purchase, IabResult result)
			{
				if (outerInstance.debug)
				{
					Log.Debug(TAG, "Consumption finished. Purchase: " + purchase + ", result: " + result);
				}

				// if we were disposed of in the meantime, quit.
				if (outerInstance.helper == null)
				{
					return;
				}

				if (result.Success)
				{
					if (outerInstance.debug)
					{
						Log.Debug(TAG, "Consumption successful. Provisioning.");
					}
				}
				if (outerInstance.debug)
				{
					Log.Debug(TAG, "End consumption flow.");
				}
			}
		}

		public override void OnActivityResult(int requestCode, int resultCode, Intent data)
		{
			if (debug)
			{
				Log.Debug(TAG, "onActivityResult(" + requestCode + "," + resultCode + "," + data);
			}
			if (helper == null)
			{
				return;
			}

			// Pass on the fragment result to the helper for handling
			if (!helper.handleActivityResult(requestCode, resultCode, data))
			{
				// not handled, so handle it ourselves (here's where you'd
				// perform any handling of activity results not related to in-app
				// billing...
				base.OnActivityResult(requestCode, resultCode, data);
			}
			else
			{
				if (debug)
				{
					Log.Debug(TAG, "onActivityResult handled by IABUtil.");
				}
			}
		}


		/// <summary>
		/// Donate button with PayPal by opening browser with defined URL For possible parameters see:
		/// https://developer.paypal.com/webapps/developer/docs/classic/paypal-payments-standard/integration-guide/Appx_websitestandard_htmlvariables/
		/// </summary>
		/// <param name="view"> </param>
		public void donatePayPalOnClick(View view)
		{
			Uri.Builder uriBuilder = new Uri.Builder();
			uriBuilder.Scheme("https").Authority("www.paypal.com").Path("cgi-bin/webscr");
			uriBuilder.AppendQueryParameter("cmd", "_donations");

			uriBuilder.AppendQueryParameter("business", paypalUser);
			//uriBuilder.AppendQueryParameter("lc", "US");
			uriBuilder.AppendQueryParameter("item_name", paypalItemName);
			uriBuilder.AppendQueryParameter("no_note", "1");
			//uriBuilder.AppendQueryParameter("no_note", "0");
			//uriBuilder.AppendQueryParameter("cn", "Note to the developer");
			uriBuilder.AppendQueryParameter("no_shipping", "1");
			//uriBuilder.AppendQueryParameter("currency_code", paypalCurrencyCode);
			Uri payPalUri = uriBuilder.Build();

            System.Console.WriteLine(payPalUri.ToString());

			if (debug)
			{
				Log.Debug(TAG, "Opening the browser with the url: " + payPalUri.ToString());
			}

			// Start your favorite browser
			try
			{
				Intent viewIntent = new Intent(Intent.ActionView, payPalUri);
				StartActivity(viewIntent);
			}
			catch (ActivityNotFoundException e)
			{
				openDialog(Android.Resource.Drawable.IcDialogAlert, Resource.String.donations__alert_dialog_title, GetString(Resource.String.donations__alert_dialog_no_browser));
			}
		}

		/// <summary>
		/// Donate with bitcoin by opening a bitcoin: intent if available.
		/// </summary>
		/// <param name="view"> </param>
		public void donateBitcoinOnClick(View view)
		{
			Intent i = new Intent(Intent.ActionView);
			i.SetData(Uri.Parse("bitcoin:" + bitcoinAddress));

			if (debug)
			{
				Log.Debug(TAG, "Attempting to donate bitcoin using URI: " + i.DataString);
			}

			try
			{
				StartActivity(i);
			}
			catch (ActivityNotFoundException e)
			{
				((Button) view.FindViewById(Resource.Id.donations__bitcoin_button)).PerformLongClick();
			}
		}

		/// <summary>
		/// Build view for Flattr. see Flattr API for more information:
		/// http://developers.flattr.net/button/
		/// </summary>
		private void buildFlattrView()
		{
			FrameLayout mLoadingFrame;
			WebView mFlattrWebview;

			mFlattrWebview = (WebView) Activity.FindViewById(Resource.Id.donations__flattr_webview);
			mLoadingFrame = (FrameLayout) Activity.FindViewById(Resource.Id.donations__loading_frame);

			// disable hardware acceleration for this webview to get transparent background working
			if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Honeycomb)
			{
				mFlattrWebview.SetLayerType(LayerType.Software, null);
			}

			// define own webview client to override loading behaviour
			mFlattrWebview.SetWebViewClient(new WebViewClientAnonymousInnerClassHelper(this, mLoadingFrame, mFlattrWebview));

			// make text white and background transparent
			string htmlStart = "<html> <head><style type='text/css'>*{color: #FFFFFF; background-color: transparent;}</style>";

			// https is not working in android 2.1 and 2.2
			string flattrScheme;
			if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Gingerbread)
			{
				flattrScheme = "https://";
			}
			else
			{
				flattrScheme = "http://";
			}

			// set url of flattr link
			glattrUrlTextView = (TextView) Activity.FindViewById(Resource.Id.donations__flattr_url);
			glattrUrlTextView.Text = flattrScheme + flattrUrl;

			string flattrJavascript = "<script type='text/javascript'>" + "/* <![CDATA[ */" + "(function() {" + "var s = document.createElement('script'), t = document.getElementsByTagName('script')[0];" + "s.type = 'text/javascript';" + "s.async = true;" + "s.src = '" + flattrScheme + "api.flattr.com/js/0.6/load.js?mode=auto';" + "t.parentNode.insertBefore(s, t);" + "})();" + "/* ]]> */" + "</script>";
			string htmlMiddle = "</head> <body> <div align='center'>";
			string flattrHtml = "<a class='FlattrButton' style='display:none;' href='" + flattrProjectUrl + "' target='_blank'></a> <noscript><a href='" + flattrScheme + flattrUrl + "' target='_blank'> <img src='" + flattrScheme + "api.flattr.com/button/flattr-badge-large.png' alt='Flattr this' title='Flattr this' border='0' /></a></noscript>";
			string htmlEnd = "</div> </body> </html>";

			string flattrCode = htmlStart + flattrJavascript + htmlMiddle + flattrHtml + htmlEnd;

			mFlattrWebview.Settings.JavaScriptEnabled = true;

			mFlattrWebview.LoadData(flattrCode, "text/html", "utf-8");

			// disable scroll on touch
			mFlattrWebview.SetOnTouchListener(new OnTouchListenerAnonymousInnerClassHelper(this));

			// make background of webview transparent
			// has to be called AFTER loadData
			// http://stackoverflow.com/questions/5003156/android-webview-style-background-colortransparent-ignored-on-android-2-2
			mFlattrWebview.SetBackgroundColor(Android.Graphics.Color.Transparent);
		}

		private class WebViewClientAnonymousInnerClassHelper : WebViewClient
		{
			private readonly DonationsFragment outerInstance;

			private FrameLayout mLoadingFrame;
			private WebView mFlattrWebview;

			public WebViewClientAnonymousInnerClassHelper(DonationsFragment outerInstance, FrameLayout mLoadingFrame, WebView mFlattrWebview)
			{
				this.outerInstance = outerInstance;
				this.mLoadingFrame = mLoadingFrame;
				this.mFlattrWebview = mFlattrWebview;
			}

			/// <summary>
			/// Open all links in browser, not in webview
			/// </summary>
			public override bool ShouldOverrideUrlLoading(WebView view, string urlNewString)
			{
				try
				{
					view.Context.StartActivity(new Intent(Intent.ActionView, Uri.Parse(urlNewString)));
				}
				catch (ActivityNotFoundException e)
				{
					outerInstance.openDialog(Android.Resource.Drawable.IcDialogAlert, Resource.String.donations__alert_dialog_title, outerInstance.GetString(Resource.String.donations__alert_dialog_no_browser));
				}

				return false;
			}

			/// <summary>
			/// Links in the flattr iframe should load in the browser not in the iframe itself,
			/// http:/
			/// /stackoverflow.com/questions/5641626/how-to-get-webview-iframe-link-to-launch-the
			/// -browser
			/// </summary>
			public override void OnLoadResource(WebView view, string url)
			{
				if (url.Contains("flattr"))
				{
					WebView.HitTestResult result = view.GetHitTestResult();
					if (result != null && result.Type > 0)
					{
						try
						{
							view.Context.StartActivity(new Intent(Intent.ActionView, Uri.Parse(url)));
						}
						catch (ActivityNotFoundException e)
						{
							outerInstance.openDialog(Android.Resource.Drawable.IcDialogAlert, Resource.String.donations__alert_dialog_title, outerInstance.GetString(Resource.String.donations__alert_dialog_no_browser));
						}
						view.StopLoading();
					}
				}
			}

			/// <summary>
			/// After loading is done, remove frame with progress circle
			/// </summary>
			public override void OnPageFinished(WebView view, string url)
			{
				// remove loading frame, show webview
				if (mLoadingFrame.Visibility == ViewStates.Visible)
				{
					mLoadingFrame.Visibility = ViewStates.Gone;
					mFlattrWebview.Visibility = ViewStates.Visible;
				}
			}
		}

        private class OnTouchListenerAnonymousInnerClassHelper : Java.Lang.Object, View.IOnTouchListener
		{
			private readonly DonationsFragment outerInstance;

			public OnTouchListenerAnonymousInnerClassHelper(DonationsFragment outerInstance)
			{
				this.outerInstance = outerInstance;
			}

			public bool OnTouch(View view, MotionEvent motionEvent)
			{
				// already handled (returns true) when moving
				return (motionEvent.Action == MotionEventActions.Move);
			}
		}
	}

}