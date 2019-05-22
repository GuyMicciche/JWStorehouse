using Org.Json;

namespace AndroidDonationsLibrary.Google.Util
{
	/// <summary>
	/// Represents an in-app billing purchase.
	/// </summary>
	public class Purchase
	{
        private string mItemType; // ITEM_TYPE_INAPP or ITEM_TYPE_SUBS
        private string mOrderId;
        private string mPackageName;
        private string mSku;
        private long mPurchaseTime;
		private int mPurchaseState;
		private string mDeveloperPayload;
		private string mToken;
		private string mOriginalJson;
		private string mSignature;

		public Purchase(string itemType, string jsonPurchaseInfo, string signature)
		{
			mItemType = itemType;
			mOriginalJson = jsonPurchaseInfo;
			JSONObject o = new JSONObject(mOriginalJson);
			mOrderId = o.OptString("orderId");
            mPackageName = o.OptString("packageName");
            mSku = o.OptString("productId");
			mPurchaseTime = o.OptLong("purchaseTime");
			mPurchaseState = o.OptInt("purchaseState");
            mDeveloperPayload = o.OptString("developerPayload");
            mToken = o.OptString("token", o.OptString("purchaseToken"));
			mSignature = signature;
		}

		public string ItemType
		{
			get
			{
				return mItemType;
			}
		}
		public string OrderId
		{
			get
			{
				return mOrderId;
			}
		}
		public string PackageName
		{
			get
			{
				return mPackageName;
			}
		}
		public string Sku
		{
			get
			{
				return mSku;
			}
		}
		public long PurchaseTime
		{
			get
			{
				return mPurchaseTime;
			}
		}
		public int PurchaseState
		{
			get
			{
				return mPurchaseState;
			}
		}
		public string DeveloperPayload
		{
			get
			{
				return mDeveloperPayload;
			}
		}
		public string Token
		{
			get
			{
				return mToken;
			}
		}
		public string OriginalJson
		{
			get
			{
				return mOriginalJson;
			}
		}
		public string Signature
		{
			get
			{
				return mSignature;
			}
		}

		public override string ToString()
		{
			return "PurchaseInfo(type:" + mItemType + "):" + mOriginalJson;
		}
	}
}