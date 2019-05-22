using Android.App;
using Android.Content;
using Android.OS;
using Android.Text;
using Android.Util;

using Com.Android.Vending.Billing;

using Java.Lang;

using Org.Json;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace AndroidDonationsLibrary.Google.Util
{
	/// <summary>
	/// Provides convenience methods for in-app billing. You can create one instance of this
	/// class for your application and use it to process in-app billing operations.
	/// It provides synchronous (blocking) and asynchronous (non-blocking) methods for
	/// many common in-app billing operations, as well as automatic signature
	/// verification.
	/// 
	/// After instantiating, you must perform setup in order to start using the object.
	/// To perform setup, call the <seealso cref="#startSetup"/> method and provide a listener;
	/// that listener will be notified when setup is complete, after which (and not before)
	/// you may call other methods.
	/// 
	/// After setup is complete, you will typically want to request an inventory of owned
	/// items and subscriptions. See <seealso cref="#queryInventory"/>, <seealso cref="#queryInventoryAsync"/>
	/// and related methods.
	/// 
	/// When you are done with this object, don't forget to call <seealso cref="#dispose"/>
	/// to ensure proper cleanup. This object holds a binding to the in-app billing
	/// service, which will leak unless you dispose of it correctly. If you created
	/// the object on an Activity's onCreate method, then the recommended
	/// place to dispose of it is the Activity's onDestroy method.
	/// 
	/// A note about threading: When using this object from a background thread, you may
	/// call the blocking versions of methods; when using from a UI thread, call
	/// only the asynchronous versions and handle the results via callbacks.
	/// Also, notice that you can only call one asynchronous operation at a time;
	/// attempting to start a second asynchronous operation while the first one
	/// has not yet completed will result in an exception being thrown.
	/// 
	/// @author Bruno Oliveira (Google)
	/// 
	/// </summary>
	public class IabHelper
	{
		// Is debug logging enabled?
        private bool mDebugLog = false;
        private string mDebugTag = "IabHelper";

		// Is setup done?
        private bool mSetupDone = false;

		// Has this object been disposed of? (If so, we should ignore callbacks, etc)
        private bool mDisposed = false;

		// Are subscriptions supported?
        private bool mSubscriptionsSupported = false;

		// Is an asynchronous operation in progress?
		// (only one at a time can be in progress)
        private bool mAsyncInProgress = false;

		// (for logging/debugging)
		// if mAsyncInProgress == true, what asynchronous operation is in progress?
        private string mAsyncOperation = "";

		// Context we were passed during initialization
        private Context mContext;

		// Connection to the service
        private IInAppBillingService mService;
        private IServiceConnection mServiceConn;

		// The request code used to launch purchase flow
        private int mRequestCode;

		// The item type of the current purchase flow
        private string mPurchasingItemType;

		// Public key for verifying signature, in base64 encoding
        private string mSignatureBase64 = null;

		// Billing response codes
		public const int BILLING_RESPONSE_RESULT_OK = 0;
		public const int BILLING_RESPONSE_RESULT_USER_CANCELED = 1;
		public const int BILLING_RESPONSE_RESULT_BILLING_UNAVAILABLE = 3;
		public const int BILLING_RESPONSE_RESULT_ITEM_UNAVAILABLE = 4;
		public const int BILLING_RESPONSE_RESULT_DEVELOPER_ERROR = 5;
		public const int BILLING_RESPONSE_RESULT_ERROR = 6;
		public const int BILLING_RESPONSE_RESULT_ITEM_ALREADY_OWNED = 7;
		public const int BILLING_RESPONSE_RESULT_ITEM_NOT_OWNED = 8;

		// IAB Helper error codes
		public const int IABHELPER_ERROR_BASE = -1000;
		public const int IABHELPER_REMOTE_EXCEPTION = -1001;
		public const int IABHELPER_BAD_RESPONSE = -1002;
		public const int IABHELPER_VERIFICATION_FAILED = -1003;
		public const int IABHELPER_SEND_INTENT_FAILED = -1004;
		public const int IABHELPER_USER_CANCELLED = -1005;
		public const int IABHELPER_UNKNOWN_PURCHASE_RESPONSE = -1006;
		public const int IABHELPER_MISSING_TOKEN = -1007;
		public const int IABHELPER_UNKNOWN_ERROR = -1008;
		public const int IABHELPER_SUBSCRIPTIONS_NOT_AVAILABLE = -1009;
		public const int IABHELPER_INVALID_CONSUMPTION = -1010;

		// Keys for the responses from InAppBillingService
		public const string RESPONSE_CODE = "RESPONSE_CODE";
		public const string RESPONSE_GET_SKU_DETAILS_LIST = "DETAILS_LIST";
		public const string RESPONSE_BUY_INTENT = "BUY_INTENT";
		public const string RESPONSE_INAPP_PURCHASE_DATA = "INAPP_PURCHASE_DATA";
		public const string RESPONSE_INAPP_SIGNATURE = "INAPP_DATA_SIGNATURE";
		public const string RESPONSE_INAPP_ITEM_LIST = "INAPP_PURCHASE_ITEM_LIST";
		public const string RESPONSE_INAPP_PURCHASE_DATA_LIST = "INAPP_PURCHASE_DATA_LIST";
		public const string RESPONSE_INAPP_SIGNATURE_LIST = "INAPP_DATA_SIGNATURE_LIST";
		public const string INAPP_CONTINUATION_TOKEN = "INAPP_CONTINUATION_TOKEN";

		// Item types
		public const string ITEM_TYPE_INAPP = "inapp";
		public const string ITEM_TYPE_SUBS = "subs";

		// some fields on the getSkuDetails response bundle
		public const string GET_SKU_DETAILS_ITEM_LIST = "ITEM_ID_LIST";
		public const string GET_SKU_DETAILS_ITEM_TYPE_LIST = "ITEM_TYPE_LIST";

		/// <summary>
		/// Creates an instance. After creation, it will not yet be ready to use. You must perform
		/// setup by calling <seealso cref="#startSetup"/> and wait for setup to complete. This constructor does not
		/// block and is safe to call from a UI thread.
		/// </summary>
		/// <param name="ctx"> Your application or Activity context. Needed to bind to the in-app billing service. </param>
		/// <param name="base64PublicKey"> Your application's public key, encoded in base64.
		///     This is used for verification of purchase signatures. You can find your app's base64-encoded
		///     public key in your application's page on Google Play Developer Console. Note that this
		///     is NOT your "developer public key". </param>
		public IabHelper(Context ctx, string base64PublicKey)
		{
			mContext = ctx.ApplicationContext;
			mSignatureBase64 = base64PublicKey;
			logDebug("IAB helper created.");
		}

		/// <summary>
		/// Enables or disable debug logging through LogCat.
		/// </summary>
		public void enableDebugLogging(bool enable, string tag)
		{
			checkNotDisposed();
			mDebugLog = enable;
			mDebugTag = tag;
		}

		public void enableDebugLogging(bool enable)
		{
			checkNotDisposed();
			mDebugLog = enable;
		}

		/// <summary>
		/// Callback for setup process. This listener's <seealso cref="#onIabSetupFinished"/> method is called
		/// when the setup process is complete.
		/// </summary>
		public interface OnIabSetupFinishedListener
		{
			/// <summary>
			/// Called to notify that setup is complete.
			/// </summary>
			/// <param name="result"> The result of the setup process. </param>
			void onIabSetupFinished(IabResult result);
		}

		/// <summary>
		/// Starts the setup process. This will start up the setup process asynchronously.
		/// You will be notified through the listener when the setup process is complete.
		/// This method is safe to call from a UI thread.
		/// </summary>
		/// <param name="listener"> The listener to notify when the setup process is complete. </param>
		public void startSetup(OnIabSetupFinishedListener listener)
		{
			// If already set up, can't do it again.
			checkNotDisposed();
			if (mSetupDone)
			{
				throw new IllegalStateException("IAB helper is already set up.");
			}

			// Connection to IAB service
			logDebug("Starting in-app billing setup.");
			mServiceConn = new ServiceConnectionAnonymousInnerClassHelper(this, listener);

			Intent serviceIntent = new Intent("com.android.vending.billing.InAppBillingService.BIND");
			serviceIntent.SetPackage("com.android.vending");

            var list = mContext.PackageManager.QueryIntentServices(serviceIntent, 0);

            if ((list == null) ? false : list.Any())
			{
				// service available to handle that Intent
				mContext.BindService(serviceIntent, mServiceConn, Bind.AutoCreate);
			}
			else
			{
				// no service available to handle that Intent
				if (listener != null)
				{
					listener.onIabSetupFinished(new IabResult(BILLING_RESPONSE_RESULT_BILLING_UNAVAILABLE, "Billing service unavailable on device."));
				}
			}
		}

        private class ServiceConnectionAnonymousInnerClassHelper : Java.Lang.Object, IServiceConnection
		{
			private readonly IabHelper outerInstance;

			private OnIabSetupFinishedListener listener;

			public ServiceConnectionAnonymousInnerClassHelper(IabHelper outerInstance, OnIabSetupFinishedListener listener)
			{
				this.outerInstance = outerInstance;
				this.listener = listener;
			}

			public void OnServiceDisconnected(ComponentName name)
			{
				outerInstance.logDebug("Billing service disconnected.");
				outerInstance.mService = null;
			}

			public void OnServiceConnected(ComponentName name, IBinder service)
			{
				if (outerInstance.mDisposed)
				{
					return;
				}
				outerInstance.logDebug("Billing service connected.");
                outerInstance.mService = IInAppBillingServiceStub.AsInterface(service);
				string packageName = outerInstance.mContext.PackageName;
				try
				{
					outerInstance.logDebug("Checking for in-app billing 3 support.");

					// check for in-app billing v3 support
					int response = outerInstance.mService.IsBillingSupported(3, packageName, ITEM_TYPE_INAPP);
					if (response != BILLING_RESPONSE_RESULT_OK)
					{
						if (listener != null)
						{
							listener.onIabSetupFinished(new IabResult(response, "Error checking for billing v3 support."));
						}

						// if in-app purchases aren't supported, neither are subscriptions.
						outerInstance.mSubscriptionsSupported = false;
						return;
					}
					outerInstance.logDebug("In-app billing version 3 supported for " + packageName);

					// check for v3 subscriptions support
                    response = outerInstance.mService.IsBillingSupported(3, packageName, ITEM_TYPE_SUBS);
					if (response == BILLING_RESPONSE_RESULT_OK)
					{
						outerInstance.logDebug("Subscriptions AVAILABLE.");
						outerInstance.mSubscriptionsSupported = true;
					}
					else
					{
						outerInstance.logDebug("Subscriptions NOT AVAILABLE. Response: " + response);
					}

					outerInstance.mSetupDone = true;
				}
				catch (RemoteException e)
				{
					if (listener != null)
					{
						listener.onIabSetupFinished(new IabResult(IABHELPER_REMOTE_EXCEPTION, "RemoteException while setting up in-app billing."));
					}
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
					return;
				}

				if (listener != null)
				{
					listener.onIabSetupFinished(new IabResult(BILLING_RESPONSE_RESULT_OK, "Setup successful."));
				}
			}
		}

		/// <summary>
		/// Dispose of object, releasing resources. It's very important to call this
		/// method when you are done with this object. It will release any resources
		/// used by it such as service connections. Naturally, once the object is
		/// disposed of, it can't be used again.
		/// </summary>
		public void dispose()
		{
			logDebug("Disposing.");
			mSetupDone = false;
			if (mServiceConn != null)
			{
				logDebug("Unbinding from service.");
				if (mContext != null)
				{
					mContext.UnbindService(mServiceConn);
				}
			}
			mDisposed = true;
			mContext = null;
			mServiceConn = null;
			mService = null;
			mPurchaseListener = null;
		}

		private void checkNotDisposed()
		{
			if (mDisposed)
			{
				throw new IllegalStateException("IabHelper was disposed of, so it cannot be used.");
			}
		}

		/// <summary>
		/// Returns whether subscriptions are supported. </summary>
		public bool subscriptionsSupported()
		{
			checkNotDisposed();
			return mSubscriptionsSupported;
		}


		/// <summary>
		/// Callback that notifies when a purchase is finished.
		/// </summary>
		public interface OnIabPurchaseFinishedListener
		{
			/// <summary>
			/// Called to notify that an in-app purchase finished. If the purchase was successful,
			/// then the sku parameter specifies which item was purchased. If the purchase failed,
			/// the sku and extraData parameters may or may not be null, depending on how far the purchase
			/// process went.
			/// </summary>
			/// <param name="result"> The result of the purchase. </param>
			/// <param name="info"> The purchase information (null if purchase failed) </param>
			void onIabPurchaseFinished(IabResult result, Purchase info);
		}

		// The listener registered on launchPurchaseFlow, which we have to call back when
		// the purchase finishes
        private OnIabPurchaseFinishedListener mPurchaseListener;

		public void launchPurchaseFlow(Activity act, string sku, int requestCode, OnIabPurchaseFinishedListener listener)
		{
			launchPurchaseFlow(act, sku, requestCode, listener, "");
		}

		public void launchPurchaseFlow(Activity act, string sku, int requestCode, OnIabPurchaseFinishedListener listener, string extraData)
		{
			launchPurchaseFlow(act, sku, ITEM_TYPE_INAPP, requestCode, listener, extraData);
		}

		public void launchSubscriptionPurchaseFlow(Activity act, string sku, int requestCode, OnIabPurchaseFinishedListener listener)
		{
			launchSubscriptionPurchaseFlow(act, sku, requestCode, listener, "");
		}

		public void launchSubscriptionPurchaseFlow(Activity act, string sku, int requestCode, OnIabPurchaseFinishedListener listener, string extraData)
		{
			launchPurchaseFlow(act, sku, ITEM_TYPE_SUBS, requestCode, listener, extraData);
		}

		/// <summary>
		/// Initiate the UI flow for an in-app purchase. Call this method to initiate an in-app purchase,
		/// which will involve bringing up the Google Play screen. The calling activity will be paused while
		/// the user interacts with Google Play, and the result will be delivered via the activity's
		/// <seealso cref="android.app.Activity#onActivityResult"/> method, at which point you must call
		/// this object's <seealso cref="#handleActivityResult"/> method to continue the purchase flow. This method
		/// MUST be called from the UI thread of the Activity.
		/// </summary>
		/// <param name="act"> The calling activity. </param>
		/// <param name="sku"> The sku of the item to purchase. </param>
		/// <param name="itemType"> indicates if it's a product or a subscription (ITEM_TYPE_INAPP or ITEM_TYPE_SUBS) </param>
		/// <param name="requestCode"> A request code (to differentiate from other responses --
		///     as in <seealso cref="android.app.Activity#startActivityForResult"/>). </param>
		/// <param name="listener"> The listener to notify when the purchase process finishes </param>
		/// <param name="extraData"> Extra data (developer payload), which will be returned with the purchase data
		///     when the purchase completes. This extra data will be permanently bound to that purchase
		///     and will always be returned when the purchase is queried. </param>
		public void launchPurchaseFlow(Activity act, string sku, string itemType, int requestCode, OnIabPurchaseFinishedListener listener, string extraData)
		{
			checkNotDisposed();
			checkSetupDone("launchPurchaseFlow");
			flagStartAsync("launchPurchaseFlow");
			IabResult result;

			if (itemType.Equals(ITEM_TYPE_SUBS) && !mSubscriptionsSupported)
			{
				IabResult r = new IabResult(IABHELPER_SUBSCRIPTIONS_NOT_AVAILABLE, "Subscriptions are not available.");
				flagEndAsync();
				if (listener != null)
				{
					listener.onIabPurchaseFinished(r, null);
				}
				return;
			}

			try
			{
				logDebug("Constructing buy intent for " + sku + ", item type: " + itemType);
				Bundle buyIntentBundle = mService.GetBuyIntent(3, mContext.PackageName, sku, itemType, extraData);
				int response = getResponseCodeFromBundle(buyIntentBundle);
				if (response != BILLING_RESPONSE_RESULT_OK)
				{
					logError("Unable to buy item, Error response: " + getResponseDesc(response));
					flagEndAsync();
					result = new IabResult(response, "Unable to buy item");
					if (listener != null)
					{
						listener.onIabPurchaseFinished(result, null);
					}
					return;
				}

				PendingIntent pendingIntent = (PendingIntent)buyIntentBundle.GetParcelable(RESPONSE_BUY_INTENT);
				logDebug("Launching buy intent for " + sku + ". Request code: " + requestCode);
				mRequestCode = requestCode;
				mPurchaseListener = listener;
				mPurchasingItemType = itemType;
                act.StartIntentSenderForResult(pendingIntent.IntentSender, requestCode, new Intent(), ActivityFlags.BroughtToFront, ActivityFlags.BroughtToFront, Convert.ToInt32(0));
			}
			catch (Android.Content.IntentSender.SendIntentException e)
			{
				logError("SendIntentException while launching purchase flow for sku " + sku);
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
				flagEndAsync();

				result = new IabResult(IABHELPER_SEND_INTENT_FAILED, "Failed to send intent.");
				if (listener != null)
				{
					listener.onIabPurchaseFinished(result, null);
				}
			}
			catch (RemoteException e)
			{
				logError("RemoteException while launching purchase flow for sku " + sku);
				Console.WriteLine(e.ToString());
				Console.Write(e.StackTrace);
				flagEndAsync();

				result = new IabResult(IABHELPER_REMOTE_EXCEPTION, "Remote exception while starting purchase flow");
				if (listener != null)
				{
					listener.onIabPurchaseFinished(result, null);
				}
			}
		}

		/// <summary>
		/// Handles an activity result that's part of the purchase flow in in-app billing. If you
		/// are calling <seealso cref="#launchPurchaseFlow"/>, then you must call this method from your
		/// Activity's <seealso cref="android.app.Activity@onActivityResult"/> method. This method
		/// MUST be called from the UI thread of the Activity.
		/// </summary>
		/// <param name="requestCode"> The requestCode as you received it. </param>
		/// <param name="resultCode"> The resultCode as you received it. </param>
		/// <param name="data"> The data (Intent) as you received it. </param>
		/// <returns> Returns true if the result was related to a purchase flow and was handled;
		///     false if the result was not related to a purchase, in which case you should
		///     handle it normally. </returns>
		public bool handleActivityResult(int requestCode, int resultCode, Intent data)
		{
			IabResult result;
			if (requestCode != mRequestCode)
			{
				return false;
			}

			checkNotDisposed();
			checkSetupDone("handleActivityResult");

			// end of async purchase operation that started on launchPurchaseFlow
			flagEndAsync();

			if (data == null)
			{
				logError("Null data in IAB activity result.");
				result = new IabResult(IABHELPER_BAD_RESPONSE, "Null data in IAB result");
				if (mPurchaseListener != null)
				{
					mPurchaseListener.onIabPurchaseFinished(result, null);
				}
				return true;
			}

			int responseCode = getResponseCodeFromIntent(data);
			string purchaseData = data.GetStringExtra(RESPONSE_INAPP_PURCHASE_DATA);
			string dataSignature = data.GetStringExtra(RESPONSE_INAPP_SIGNATURE);

			if (resultCode == (int)Result.Ok && responseCode == BILLING_RESPONSE_RESULT_OK)
			{
				logDebug("Successful resultcode from purchase activity.");
				logDebug("Purchase data: " + purchaseData);
				logDebug("Data signature: " + dataSignature);
				logDebug("Extras: " + data.Extras);
				logDebug("Expected item type: " + mPurchasingItemType);

				if (purchaseData == null || dataSignature == null)
				{
					logError("BUG: either purchaseData or dataSignature is null.");
					logDebug("Extras: " + data.Extras.ToString());
					result = new IabResult(IABHELPER_UNKNOWN_ERROR, "IAB returned null purchaseData or dataSignature");
					if (mPurchaseListener != null)
					{
						mPurchaseListener.onIabPurchaseFinished(result, null);
					}
					return true;
				}

				Purchase purchase = null;
				try
				{
					purchase = new Purchase(mPurchasingItemType, purchaseData, dataSignature);
					string sku = purchase.Sku;

					// Verify signature
					if (!Security.verifyPurchase(mSignatureBase64, purchaseData, dataSignature))
					{
						logError("Purchase signature verification FAILED for sku " + sku);
						result = new IabResult(IABHELPER_VERIFICATION_FAILED, "Signature verification failed for sku " + sku);
						if (mPurchaseListener != null)
						{
							mPurchaseListener.onIabPurchaseFinished(result, purchase);
						}
						return true;
					}
					logDebug("Purchase signature successfully verified.");
				}
				catch (JSONException e)
				{
					logError("Failed to parse purchase data.");
					Console.WriteLine(e.ToString());
					Console.Write(e.StackTrace);
					result = new IabResult(IABHELPER_BAD_RESPONSE, "Failed to parse purchase data.");
					if (mPurchaseListener != null)
					{
						mPurchaseListener.onIabPurchaseFinished(result, null);
					}
					return true;
				}

				if (mPurchaseListener != null)
				{
					mPurchaseListener.onIabPurchaseFinished(new IabResult(BILLING_RESPONSE_RESULT_OK, "Success"), purchase);
				}
			}
			else if (resultCode == (int)Result.Ok)
			{
				// result code was OK, but in-app billing response was not OK.
				logDebug("Result code was OK but in-app billing response was not OK: " + getResponseDesc(responseCode));
				if (mPurchaseListener != null)
				{
					result = new IabResult(responseCode, "Problem purchashing item.");
					mPurchaseListener.onIabPurchaseFinished(result, null);
				}
			}
            else if (resultCode == (int)Result.Canceled)
			{
				logDebug("Purchase canceled - Response: " + getResponseDesc(responseCode));
				result = new IabResult(IABHELPER_USER_CANCELLED, "User canceled.");
				if (mPurchaseListener != null)
				{
					mPurchaseListener.onIabPurchaseFinished(result, null);
				}
			}
			else
			{
				logError("Purchase failed. Result code: " + Convert.ToString(resultCode) + ". Response: " + getResponseDesc(responseCode));
				result = new IabResult(IABHELPER_UNKNOWN_PURCHASE_RESPONSE, "Unknown purchase response.");
				if (mPurchaseListener != null)
				{
					mPurchaseListener.onIabPurchaseFinished(result, null);
				}
			}
			return true;
		}

		public Inventory queryInventory(bool querySkuDetails, IList<string> moreSkus)
		{
			return queryInventory(querySkuDetails, moreSkus, null);
		}

		/// <summary>
		/// Queries the inventory. This will query all owned items from the server, as well as
		/// information on additional skus, if specified. This method may block or take long to execute.
		/// Do not call from a UI thread. For that, use the non-blocking version <seealso cref="#refreshInventoryAsync"/>.
		/// </summary>
		/// <param name="querySkuDetails"> if true, SKU details (price, description, etc) will be queried as well
		///     as purchase information. </param>
		/// <param name="moreItemSkus"> additional PRODUCT skus to query information on, regardless of ownership.
		///     Ignored if null or if querySkuDetails is false. </param>
		/// <param name="moreSubsSkus"> additional SUBSCRIPTIONS skus to query information on, regardless of ownership.
		///     Ignored if null or if querySkuDetails is false. </param>
		/// <exception cref="IabException"> if a problem occurs while refreshing the inventory. </exception>
		public Inventory queryInventory(bool querySkuDetails, IList<string> moreItemSkus, IList<string> moreSubsSkus)
		{
			checkNotDisposed();
			checkSetupDone("queryInventory");
			try
			{
				Inventory inv = new Inventory();
				int r = this.queryPurchases(inv, ITEM_TYPE_INAPP);
				if (r != BILLING_RESPONSE_RESULT_OK)
				{
					throw new IabException(r, "Error refreshing inventory (querying owned items).");
				}

				if (querySkuDetails)
				{
                    r = this.querySkuDetails(ITEM_TYPE_INAPP, inv, moreItemSkus);
					if (r != BILLING_RESPONSE_RESULT_OK)
					{
						throw new IabException(r, "Error refreshing inventory (querying prices of items).");
					}
				}

				// if subscriptions are supported, then also query for subscriptions
				if (mSubscriptionsSupported)
				{
                    r = this.queryPurchases(inv, ITEM_TYPE_SUBS);
					if (r != BILLING_RESPONSE_RESULT_OK)
					{
						throw new IabException(r, "Error refreshing inventory (querying owned subscriptions).");
					}

					if (querySkuDetails)
					{
                        r = this.querySkuDetails(ITEM_TYPE_SUBS, inv, moreItemSkus);
						if (r != BILLING_RESPONSE_RESULT_OK)
						{
							throw new IabException(r, "Error refreshing inventory (querying prices of subscriptions).");
						}
					}
				}

				return inv;
			}
			catch (RemoteException e)
			{
				throw new IabException(IABHELPER_REMOTE_EXCEPTION, "Remote exception while refreshing inventory.", e);
			}
			catch (JSONException e)
			{
				throw new IabException(IABHELPER_BAD_RESPONSE, "Error parsing JSON response while refreshing inventory.", e);
			}
		}

		/// <summary>
		/// Listener that notifies when an inventory query operation completes.
		/// </summary>
		public interface QueryInventoryFinishedListener
		{
			/// <summary>
			/// Called to notify that an inventory query operation completed.
			/// </summary>
			/// <param name="result"> The result of the operation. </param>
			/// <param name="inv"> The inventory. </param>
			void onQueryInventoryFinished(IabResult result, Inventory inv);
		}


		/// <summary>
		/// Asynchronous wrapper for inventory query. This will perform an inventory
		/// query as described in <seealso cref="#queryInventory"/>, but will do so asynchronously
		/// and call back the specified listener upon completion. This method is safe to
		/// call from a UI thread.
		/// </summary>
		/// <param name="querySkuDetails"> as in <seealso cref="#queryInventory"/> </param>
		/// <param name="moreSkus"> as in <seealso cref="#queryInventory"/> </param>
		/// <param name="listener"> The listener to notify when the refresh operation completes. </param>
		public void queryInventoryAsync(bool querySkuDetails, IList<string> moreSkus, QueryInventoryFinishedListener listener)
		{
			Handler handler = new Handler();
			checkNotDisposed();
			checkSetupDone("queryInventory");
			flagStartAsync("refresh inventory");
			(new Java.Lang.Thread(new RunnableAnonymousInnerClassHelper(this, querySkuDetails, moreSkus, listener, handler))).Start();
		}

        private class RunnableAnonymousInnerClassHelper : Java.Lang.Object, IRunnable
		{
			private readonly IabHelper outerInstance;

			private bool querySkuDetails;
			private IList<string> moreSkus;
			private QueryInventoryFinishedListener listener;
			private Handler handler;

			public RunnableAnonymousInnerClassHelper(IabHelper outerInstance, bool querySkuDetails, IList<string> moreSkus, QueryInventoryFinishedListener listener, Handler handler)
			{
				this.outerInstance = outerInstance;
				this.querySkuDetails = querySkuDetails;
				this.moreSkus = moreSkus;
				this.listener = listener;
				this.handler = handler;
			}

			public void Run()
			{
				IabResult result = new IabResult(BILLING_RESPONSE_RESULT_OK, "Inventory refresh successful.");
				Inventory inv = null;
				try
				{
					inv = outerInstance.queryInventory(querySkuDetails, moreSkus);
				}
				catch (IabException ex)
				{
					result = ex.Result;
				}

				outerInstance.flagEndAsync();

				IabResult result_f = result;
				Inventory inv_f = inv;
				if (!outerInstance.mDisposed && listener != null)
				{
					handler.Post(new RunnableAnonymousInnerClassHelper2(this, result_f, inv_f));
				}
			}

            private class RunnableAnonymousInnerClassHelper2 : Java.Lang.Object, IRunnable
			{
				private readonly RunnableAnonymousInnerClassHelper outerInstance;

				private IabResult result_f;
				private Inventory inv_f;

				public RunnableAnonymousInnerClassHelper2(RunnableAnonymousInnerClassHelper outerInstance, IabResult result_f, Inventory inv_f)
				{
					this.outerInstance = outerInstance;
					this.result_f = result_f;
					this.inv_f = inv_f;
				}

				public void Run()
				{
					outerInstance.listener.onQueryInventoryFinished(result_f, inv_f);
				}
			}
		}

		public void queryInventoryAsync(QueryInventoryFinishedListener listener)
		{
			queryInventoryAsync(true, null, listener);
		}

		public void queryInventoryAsync(bool querySkuDetails, QueryInventoryFinishedListener listener)
		{
			queryInventoryAsync(querySkuDetails, null, listener);
		}


		/// <summary>
		/// Consumes a given in-app product. Consuming can only be done on an item
		/// that's owned, and as a result of consumption, the user will no longer own it.
		/// This method may block or take long to return. Do not call from the UI thread.
		/// For that, see <seealso cref="#consumeAsync"/>.
		/// </summary>
		/// <param name="itemInfo"> The PurchaseInfo that represents the item to consume. </param>
		/// <exception cref="IabException"> if there is a problem during consumption. </exception>
		private void consume(Purchase itemInfo)
		{
			checkNotDisposed();
			checkSetupDone("consume");

			if (!itemInfo.ItemType.Equals(ITEM_TYPE_INAPP))
			{
				throw new IabException(IABHELPER_INVALID_CONSUMPTION, "Items of type '" + itemInfo.ItemType + "' can't be consumed.");
			}

			try
			{
				string token = itemInfo.Token;
				string sku = itemInfo.Sku;
				if (token == null || token.Equals(""))
				{
				   logError("Can't consume " + sku + ". No token.");
				   throw new IabException(IABHELPER_MISSING_TOKEN, "PurchaseInfo is missing token for sku: " + sku + " " + itemInfo);
				}

				logDebug("Consuming sku: " + sku + ", token: " + token);
				int response = mService.ConsumePurchase(3, mContext.PackageName, token);
				if (response == BILLING_RESPONSE_RESULT_OK)
				{
				   logDebug("Successfully consumed sku: " + sku);
				}
				else
				{
				   logDebug("Error consuming consuming sku " + sku + ". " + getResponseDesc(response));
				   throw new IabException(response, "Error consuming sku " + sku);
				}
			}
			catch (RemoteException e)
			{
				throw new IabException(IABHELPER_REMOTE_EXCEPTION, "Remote exception while consuming. PurchaseInfo: " + itemInfo, e);
			}
		}

		/// <summary>
		/// Callback that notifies when a consumption operation finishes.
		/// </summary>
		public interface OnConsumeFinishedListener
		{
			/// <summary>
			/// Called to notify that a consumption has finished.
			/// </summary>
			/// <param name="purchase"> The purchase that was (or was to be) consumed. </param>
			/// <param name="result"> The result of the consumption operation. </param>
			void onConsumeFinished(Purchase purchase, IabResult result);
		}

		/// <summary>
		/// Callback that notifies when a multi-item consumption operation finishes.
		/// </summary>
		public interface OnConsumeMultiFinishedListener
		{
			/// <summary>
			/// Called to notify that a consumption of multiple items has finished.
			/// </summary>
			/// <param name="purchases"> The purchases that were (or were to be) consumed. </param>
			/// <param name="results"> The results of each consumption operation, corresponding to each
			///     sku. </param>
			void onConsumeMultiFinished(IList<Purchase> purchases, IList<IabResult> results);
		}

		/// <summary>
		/// Asynchronous wrapper to item consumption. Works like <seealso cref="#consume"/>, but
		/// performs the consumption in the background and notifies completion through
		/// the provided listener. This method is safe to call from a UI thread.
		/// </summary>
		/// <param name="purchase"> The purchase to be consumed. </param>
		/// <param name="listener"> The listener to notify when the consumption operation finishes. </param>
		public void consumeAsync(Purchase purchase, OnConsumeFinishedListener listener)
		{
			checkNotDisposed();
			checkSetupDone("consume");
			IList<Purchase> purchases = new List<Purchase>();
			purchases.Add(purchase);
			consumeAsyncInternal(purchases, listener, null);
		}

		/// <summary>
		/// Same as <seealso cref="consumeAsync"/>, but for multiple items at once. </summary>
		/// <param name="purchases"> The list of PurchaseInfo objects representing the purchases to consume. </param>
		/// <param name="listener"> The listener to notify when the consumption operation finishes. </param>
		public void consumeAsync(IList<Purchase> purchases, OnConsumeMultiFinishedListener listener)
		{
			checkNotDisposed();
			checkSetupDone("consume");
			consumeAsyncInternal(purchases, null, listener);
		}

		/// <summary>
		/// Returns a human-readable description for the given response code.
		/// </summary>
		/// <param name="code"> The response code </param>
		/// <returns> A human-readable string explaining the result code.
		///     It also includes the result code numerically. </returns>
		public static string getResponseDesc(int code)
		{
			string[] iab_msgs = ("0:OK/1:User Canceled/2:Unknown/" + "3:Billing Unavailable/4:Item unavailable/" + "5:Developer Error/6:Error/7:Item Already Owned/" + "8:Item not owned").Split('/');
			string[] iabhelper_msgs = ("0:OK/-1001:Remote exception during initialization/" + "-1002:Bad response received/" + "-1003:Purchase signature verification failed/" + "-1004:Send intent failed/" + "-1005:User cancelled/" + "-1006:Unknown purchase response/" + "-1007:Missing token/" + "-1008:Unknown error/" + "-1009:Subscriptions not available/" + "-1010:Invalid consumption attempt").Split('/');

			if (code <= IABHELPER_ERROR_BASE)
			{
				int index = IABHELPER_ERROR_BASE - code;
				if (index >= 0 && index < iabhelper_msgs.Length)
				{
					return iabhelper_msgs[index];
				}
				else
				{
					return Convert.ToString(code) + ":Unknown IAB Helper Error";
				}
			}
			else if (code < 0 || code >= iab_msgs.Length)
			{
				return Convert.ToString(code) + ":Unknown";
			}
			else
			{
				return iab_msgs[code];
			}
		}


		// Checks that setup was done; if not, throws an exception.
        private void checkSetupDone(string operation)
		{
			if (!mSetupDone)
			{
				logError("Illegal state for operation (" + operation + "): IAB helper is not set up.");
				throw new IllegalStateException("IAB helper is not set up. Can't perform operation: " + operation);
			}
		}

		// Workaround to bug where sometimes response codes come as Long instead of Integer
        private int getResponseCodeFromBundle(Bundle b)
		{
			var o = b.Get(RESPONSE_CODE);
			if (o == null)
			{
				logDebug("Bundle with null response code, assuming OK (known issue)");
				return BILLING_RESPONSE_RESULT_OK;
			}
			else if (o is Java.Lang.Integer)
			{
				return (int)o;
			}
            else if (o is Java.Lang.Long)
			{
                return (int)o;
			}
			else
			{
				logError("Unexpected type for bundle response code.");
				logError(o.GetType().Name);
                throw new Java.Lang.Exception("Unexpected type for bundle response code: " + o.GetType().Name);
			}
		}

		// Workaround to bug where sometimes response codes come as Long instead of Integer
        private int getResponseCodeFromIntent(Intent i)
		{
            var o = i.Extras.Get(RESPONSE_CODE);
			if (o == null)
			{
				logError("Intent with no response code, assuming OK (known issue)");
				return BILLING_RESPONSE_RESULT_OK;
			}
			else if (o is Java.Lang.Integer)
			{
				return (int)o;
			}
            else if (o is Java.Lang.Long)
			{
                return (int)o;
			}
			else
			{
				logError("Unexpected type for intent response code.");
				logError(o.GetType().Name);
				throw new Java.Lang.Exception("Unexpected type for intent response code: " + o.GetType().Name);
			}
		}

        private void flagStartAsync(string operation)
		{
			if (mAsyncInProgress)
			{
				throw new IllegalStateException("Can't start async operation (" + operation + ") because another async operation(" + mAsyncOperation + ") is in progress.");
			}
			mAsyncOperation = operation;
			mAsyncInProgress = true;
			logDebug("Starting async operation: " + operation);
		}

        private void flagEndAsync()
		{
			logDebug("Ending async operation: " + mAsyncOperation);
			mAsyncOperation = "";
			mAsyncInProgress = false;
		}
        
        private int queryPurchases(Inventory inv, string itemType)
		{
			// Query purchases
			logDebug("Querying owned items, item type: " + itemType);
			logDebug("Package name: " + mContext.PackageName);
			bool verificationFailed = false;
			string continueToken = null;

			do
			{
				logDebug("Calling getPurchases with continuation token: " + continueToken);
				Bundle ownedItems = mService.GetPurchases(3, mContext.PackageName, itemType, continueToken);

				int response = getResponseCodeFromBundle(ownedItems);
				logDebug("Owned items response: " + Convert.ToString(response));
				if (response != BILLING_RESPONSE_RESULT_OK)
				{
					logDebug("getPurchases() failed: " + getResponseDesc(response));
					return response;
				}
                if (!ownedItems.ContainsKey(RESPONSE_INAPP_ITEM_LIST) || !ownedItems.ContainsKey(RESPONSE_INAPP_PURCHASE_DATA_LIST) || !ownedItems.ContainsKey(RESPONSE_INAPP_SIGNATURE_LIST))
				{
					logError("Bundle returned from getPurchases() doesn't contain required fields.");
					return IABHELPER_BAD_RESPONSE;
				}

                IList<string> ownedSkus = ownedItems.GetStringArrayList(RESPONSE_INAPP_ITEM_LIST);
                IList<string> purchaseDataList = ownedItems.GetStringArrayList(RESPONSE_INAPP_PURCHASE_DATA_LIST);
                IList<string> signatureList = ownedItems.GetStringArrayList(RESPONSE_INAPP_SIGNATURE_LIST);

				for (int i = 0; i < purchaseDataList.Count; ++i)
				{
					string purchaseData = purchaseDataList[i];
					string signature = signatureList[i];
					string sku = ownedSkus[i];

                    JSONObject o = new JSONObject(purchaseData);
                    string purchaseToken = o.OptString("token", o.OptString("purchaseToken"));
                    mService.ConsumePurchase(3, mContext.PackageName, purchaseToken);

					if (Security.verifyPurchase(mSignatureBase64, purchaseData, signature))
					{
						logDebug("Sku is owned: " + sku);
						Purchase purchase = new Purchase(itemType, purchaseData, signature);

						if (TextUtils.IsEmpty(purchase.Token))
						{
							logWarn("BUG: empty/null token!");
							logDebug("Purchase data: " + purchaseData);
						}

						// Record ownership and token
						inv.addPurchase(purchase);
					}
					else
					{
						logWarn("Purchase signature verification **FAILED**. Not adding item.");
						logDebug("   Purchase data: " + purchaseData);
						logDebug("   Signature: " + signature);
						verificationFailed = true;
					}
				}

				continueToken = ownedItems.GetString(INAPP_CONTINUATION_TOKEN);
				logDebug("Continuation token: " + continueToken);
			} while (!TextUtils.IsEmpty(continueToken));

			return verificationFailed ? IABHELPER_VERIFICATION_FAILED : BILLING_RESPONSE_RESULT_OK;
		}

		private int querySkuDetails(string itemType, Inventory inv, IList<string> moreSkus)
		{
			logDebug("Querying SKU details.");
			List<string> skuList = new List<string>();
			skuList.AddRange(inv.getAllOwnedSkus(itemType));
			if (moreSkus != null)
			{
				foreach (string sku in moreSkus)
				{
					if (!skuList.Contains(sku))
					{
						skuList.Add(sku);
					}
				}
			}

			if (skuList.Count == 0)
			{
				logDebug("queryPrices: nothing to do because there are no SKUs.");
				return BILLING_RESPONSE_RESULT_OK;
			}

			Bundle querySkus = new Bundle();
			querySkus.PutStringArrayList(GET_SKU_DETAILS_ITEM_LIST, skuList);
			Bundle skuDetails = mService.GetSkuDetails(3, mContext.PackageName, itemType, querySkus);

			if (!skuDetails.ContainsKey(RESPONSE_GET_SKU_DETAILS_LIST))
			{
				int response = getResponseCodeFromBundle(skuDetails);
				if (response != BILLING_RESPONSE_RESULT_OK)
				{
					logDebug("getSkuDetails() failed: " + getResponseDesc(response));
					return response;
				}
				else
				{
					logError("getSkuDetails() returned a bundle with neither an error nor a detail list.");
					return IABHELPER_BAD_RESPONSE;
				}
			}

			IList<string> responseList = skuDetails.GetStringArrayList(RESPONSE_GET_SKU_DETAILS_LIST);

			foreach (string thisResponse in responseList)
			{
				SkuDetails d = new SkuDetails(itemType, thisResponse);
				logDebug("Got sku details: " + d);
				inv.addSkuDetails(d);
			}
			return BILLING_RESPONSE_RESULT_OK;
		}

		private void consumeAsyncInternal(IList<Purchase> purchases, OnConsumeFinishedListener singleListener, OnConsumeMultiFinishedListener multiListener)
		{
			Handler handler = new Handler();
			flagStartAsync("consume");
			(new Java.Lang.Thread(new RunnableAnonymousInnerClassHelper3(this, purchases, singleListener, multiListener, handler))).Start();
		}

        private class RunnableAnonymousInnerClassHelper3 : Java.Lang.Object, IRunnable
		{
			private readonly IabHelper outerInstance;

			private IList<Purchase> purchases;
			private OnConsumeFinishedListener singleListener;
			private OnConsumeMultiFinishedListener multiListener;
			private Handler handler;

			public RunnableAnonymousInnerClassHelper3(IabHelper outerInstance, IList<Purchase> purchases, OnConsumeFinishedListener singleListener, OnConsumeMultiFinishedListener multiListener, Handler handler)
			{
				this.outerInstance = outerInstance;
				this.purchases = purchases;
				this.singleListener = singleListener;
				this.multiListener = multiListener;
				this.handler = handler;
			}

			public void Run()
			{
				IList<IabResult> results = new List<IabResult>();
				foreach (Purchase purchase in purchases)
				{
					try
					{
						outerInstance.consume(purchase);
						results.Add(new IabResult(BILLING_RESPONSE_RESULT_OK, "Successful consume of sku " + purchase.Sku));
					}
					catch (IabException ex)
					{
						results.Add(ex.Result);
					}
				}

				outerInstance.flagEndAsync();
				if (!outerInstance.mDisposed && singleListener != null)
				{
					handler.Post(new RunnableAnonymousInnerClassHelper4(this, results));
				}
				if (!outerInstance.mDisposed && multiListener != null)
				{
					handler.Post(new RunnableAnonymousInnerClassHelper5(this, results));
				}
			}

            private class RunnableAnonymousInnerClassHelper4 : Java.Lang.Object, IRunnable
			{
				private readonly RunnableAnonymousInnerClassHelper3 outerInstance;

				private IList<IabResult> results;

				public RunnableAnonymousInnerClassHelper4(RunnableAnonymousInnerClassHelper3 outerInstance, IList<IabResult> results)
				{
					this.outerInstance = outerInstance;
					this.results = results;
				}

				public void Run()
				{
					outerInstance.singleListener.onConsumeFinished(outerInstance.purchases[0], results[0]);
				}
			}

			private class RunnableAnonymousInnerClassHelper5 : Java.Lang.Object, IRunnable
			{
				private readonly RunnableAnonymousInnerClassHelper3 outerInstance;

				private IList<IabResult> results;

				public RunnableAnonymousInnerClassHelper5(RunnableAnonymousInnerClassHelper3 outerInstance, IList<IabResult> results)
				{
					this.outerInstance = outerInstance;
					this.results = results;
				}

				public void Run()
				{
					outerInstance.multiListener.onConsumeMultiFinished(outerInstance.purchases, results);
				}
			}
		}

        private void logDebug(string msg)
		{
			if (mDebugLog)
			{
				Log.Debug(mDebugTag, msg);
			}
		}

        private void logError(string msg)
		{
			Log.Error(mDebugTag, "In-app billing error: " + msg);
		}

        private void logWarn(string msg)
		{
			Log.Warn(mDebugTag, "In-app billing warning: " + msg);
		}
	}
}


