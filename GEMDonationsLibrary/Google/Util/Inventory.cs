using System.Collections.Generic;

namespace AndroidDonationsLibrary.Google.Util
{


	/// <summary>
	/// Represents a block of information about in-app items.
	/// An Inventory is returned by such methods as <seealso cref="IabHelper#queryInventory"/>.
	/// </summary>
	public class Inventory
	{
        private IDictionary<string, SkuDetails> mSkuMap = new Dictionary<string, SkuDetails>();
        private IDictionary<string, Purchase> mPurchaseMap = new Dictionary<string, Purchase>();

        public Inventory()
		{

		}

		/// <summary>
		/// Returns the listing details for an in-app product. </summary>
		public SkuDetails getSkuDetails(string sku)
		{
			return mSkuMap[sku];
		}

		/// <summary>
		/// Returns purchase information for a given product, or null if there is no purchase. </summary>
		public Purchase getPurchase(string sku)
		{
			return mPurchaseMap[sku];
		}

		/// <summary>
		/// Returns whether or not there exists a purchase of the given product. </summary>
		public bool hasPurchase(string sku)
		{
			return mPurchaseMap.ContainsKey(sku);
		}

		/// <summary>
		/// Return whether or not details about the given product are available. </summary>
		public bool hasDetails(string sku)
		{
			return mSkuMap.ContainsKey(sku);
		}

		/// <summary>
		/// Erase a purchase (locally) from the inventory, given its product ID. This just
		/// modifies the Inventory object locally and has no effect on the server! This is
		/// useful when you have an existing Inventory object which you know to be up to date,
		/// and you have just consumed an item successfully, which means that erasing its
		/// purchase data from the Inventory you already have is quicker than querying for
		/// a new Inventory.
		/// </summary>
		public void erasePurchase(string sku)
		{
			if (mPurchaseMap.ContainsKey(sku))
			{
				mPurchaseMap.Remove(sku);
			}
		}

		/// <summary>
		/// Returns a list of all owned product IDs. </summary>
        private IList<string> AllOwnedSkus
		{
			get
			{
				return new List<string>(mPurchaseMap.Keys);
			}
		}

		/// <summary>
		/// Returns a list of all owned product IDs of a given type </summary>
        public IList<string> getAllOwnedSkus(string itemType)
		{
			IList<string> result = new List<string>();
			foreach (Purchase p in mPurchaseMap.Values)
			{
				if (p.ItemType.Equals(itemType))
				{
					result.Add(p.Sku);
				}
			}
			return result;
		}

		/// <summary>
		/// Returns a list of all purchases. </summary>
        private IList<Purchase> AllPurchases
		{
			get
			{
				return new List<Purchase>(mPurchaseMap.Values);
			}
		}

        public void addSkuDetails(SkuDetails d)
		{
			mSkuMap[d.Sku] = d;
		}

        public void addPurchase(Purchase p)
		{
			mPurchaseMap[p.Sku] = p;
		}
	}

}