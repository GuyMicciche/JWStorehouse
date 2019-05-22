using Org.Json;

namespace AndroidDonationsLibrary.Google.Util
{
	/// <summary>
	/// Represents an in-app product's listing details.
	/// </summary>
	public class SkuDetails
	{
		internal string mItemType;
		internal string mSku;
		internal string mType;
		internal string mPrice;
		internal string mTitle;
		internal string mDescription;
		internal string mJson;

		public SkuDetails(string jsonSkuDetails) : this(IabHelper.ITEM_TYPE_INAPP, jsonSkuDetails)
		{

		}

		public SkuDetails(string itemType, string jsonSkuDetails)
		{
			mItemType = itemType;
			mJson = jsonSkuDetails;
			JSONObject o = new JSONObject(mJson);
			mSku = o.OptString("productId");
            mType = o.OptString("type");
            mPrice = o.OptString("price");
            mTitle = o.OptString("title");
            mDescription = o.OptString("description");
		}

		public virtual string Sku
		{
			get
			{
				return mSku;
			}
		}
		public virtual string Type
		{
			get
			{
				return mType;
			}
		}
		public virtual string Price
		{
			get
			{
				return mPrice;
			}
		}
		public virtual string Title
		{
			get
			{
				return mTitle;
			}
		}
		public virtual string Description
		{
			get
			{
				return mDescription;
			}
		}

		public override string ToString()
		{
			return "SkuDetails:" + mJson;
		}
	}

}