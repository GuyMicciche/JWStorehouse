using Android.Content;
using System.Collections.Generic;
using Android.OS;
using Com.Android.Vending.Billing;
using Android.App;
using System.Threading.Tasks;
using System;
using Xamarin.InAppBilling;

namespace JWChinese
{
	/// <summary>
	/// In app billing service helper.
	/// </summary>
	public class InAppBillingHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JWChinese.InAppBillingHelper"/> class.
		/// </summary>
		/// <param name="activity">Activity.</param>
		/// <param name="billingService">Billing service.</param>
		public InAppBillingHelper (Activity activity, IInAppBillingService billingService)
		{
			this.billingService = billingService;
			this.activity = activity;
		}

		/// <summary>
		/// Queries the inventory asynchronously.
		/// </summary>
		/// <returns>List of strings</returns>
		/// <param name="skuList">Sku list.</param>
		/// <param name="itemType">Item type.</param>
		public Task<IList<string>> QueryInventoryAsync (List<string> skuList, string itemType)
		{

			var getSkuDetailsTask = Task.Factory.StartNew<IList<string>> (() => {

				Bundle querySku = new Bundle ();
                querySku.PutStringArrayList(Billing.ItemIdList, skuList);


                Bundle skuDetails = billingService.GetSkuDetails(Billing.APIVersion, activity.PackageName, itemType, querySku);

                if (skuDetails.ContainsKey(Billing.SkuDetailsList))
                {
                    return skuDetails.GetStringArrayList(Billing.SkuDetailsList);
				}

				return null;
			});

			return getSkuDetailsTask;
		}

		/// <summary>
		/// Buys an items
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="payload">Payload.</param>
		public void LaunchPurchaseFlow (Product product)
		{
			payload = Guid.NewGuid ().ToString ();
			LaunchPurchaseFlow (product.ProductId, product.Type, payload);
		}

		/// <summary>
		/// Buys an item.
		/// </summary>
		/// <param name="sku">Sku.</param>
		/// <param name="itemType">Item type.</param>
		/// <param name="payload">Payload.</param>
		public void LaunchPurchaseFlow (string sku, string itemType, string payload)
		{

//#if DEBUG
//			var consume = _billingService.ConsumePurchase(Constants.APIVersion, _activity.PackageName, "inapp:com.xamarin.InAppService:android.test.purchased");
//			Console.WriteLine ("Consumed: {0}", consume);
//#endif

            var buyIntentBundle = billingService.GetBuyIntent(Billing.APIVersion, activity.PackageName, sku, itemType, payload);
			var response = GetResponseCodeFromBundle (buyIntentBundle);

			if (response != BillingResult.OK) {
				return;
			}

			var pendingIntent = buyIntentBundle.GetParcelable (Response.BuyIntent) as PendingIntent;
			if (pendingIntent != null) {
				activity.StartIntentSenderForResult (pendingIntent.IntentSender, PurchaseRequestCode, new Intent (), 0, 0, 0);
			}
		}

		public void GetPurchases (string itemType)
		{
            Bundle ownedItems = billingService.GetPurchases(Billing.APIVersion, activity.PackageName, itemType, null);
			var response = GetResponseCodeFromBundle (ownedItems);

			if (response != BillingResult.OK) {
				return;
			}

			var list = ownedItems.GetStringArrayList (Response.InAppPurchaseItemList);
			var data = ownedItems.GetStringArrayList (Response.InAppPurchaseDataList);
			Console.WriteLine (list);

			//TODO: Get more products if continuation token is not null
		}

		public void HandleActivityResult (int requestCode, Result resultCode, Intent data)
		{
			if (PurchaseRequestCode != requestCode || data == null) {
				return;
			}

			var response = GetReponseCodeFromIntent (data);
			var purchaseData = data.GetStringExtra (Response.InAppPurchaseData);
			var purchaseSign = data.GetStringExtra (Response.InAppDataSignature);
		}

		int GetReponseCodeFromIntent (Intent intent)
		{
			object response = intent.Extras.Get (Response.Code);

			if (response == null) {
				//Bundle with null response code, assuming OK (known issue)
				return BillingResult.OK;
			}

			if (response is Java.Lang.Number) {
				return ((Java.Lang.Number)response).IntValue ();
			}

			return BillingResult.Error;
		}

		int GetResponseCodeFromBundle (Bundle bunble)
		{
			object response = bunble.Get (Response.Code);
			if (response == null) {
				//Bundle with null response code, assuming OK (known issue)
				return BillingResult.OK;
			}

			if (response is Java.Lang.Number) {
				return ((Java.Lang.Number)response).IntValue ();
			}

			return BillingResult.Error;
		}

		Activity activity;
		string payload;
		IInAppBillingService billingService;
		const int PurchaseRequestCode = 1001;
	}
}

