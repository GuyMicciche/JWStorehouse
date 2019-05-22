using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Util;
using Android.Widget;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xamarin.InAppBilling;
using Xamarin.InAppBilling.Utilities;

namespace JWChinese
{
    [Activity(Label = "JW Chinese | Donations", Icon = "@drawable/icon", Theme = "@style/Theme.Storehouse.Material")]
    public class DonationsActivity : ActionBarActivity, AdapterView.IOnItemSelectedListener, AdapterView.IOnItemClickListener
    {
        #region Private Variables
        private Button buyButton;
        private Spinner productSpinner;
        private Product selectedProduct;
        private IList<Product> products;
        private InAppBillingServiceConnection serviceConnection;
        private ListView lvPurchsedItems;
        private PurchaseAdapter purchasesAdapter;
        #endregion

        #region Override Methods
        /// <summary>
        /// Starts the current <c>Activity</c>
        /// </summary>
        /// <param name="bundle">Bundle.</param>
        protected override void OnCreate(Bundle bundle)
        {
            // Do the base setup
            base.OnCreate(bundle);

            App.FUNCTIONS.SetActionBarDrawable(this);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.DonationsActivity);

            SupportActionBar.SetDisplayShowHomeEnabled(true);
            //SupportActionBar.SetIcon(Resource.Drawable.Icon);

            // Attach to the GUI elements
            productSpinner = FindViewById<Spinner>(Resource.Id.productSpinner);
            buyButton = FindViewById<Button>(Resource.Id.buyButton);
            //lvPurchsedItems = FindViewById<ListView>(Resource.Id.purchasedItemsList);

            // Configure buy button
            buyButton.Click += (sender, e) =>
            {
                // Ask the open connection's billing handler to purchase the selected product
                if (selectedProduct != null)
                {
                    serviceConnection.BillingHandler.BuyProduct(selectedProduct);
                }
            };

            // Configure the purchased items list
            //lvPurchsedItems.OnItemClickListener = this;

            // Configure the available product spinner
            productSpinner.Enabled = false;
            productSpinner.OnItemSelectedListener = this;

            // Initialize the list of available items
            products = new List<Product>();
            // Attempt to attach to the Google Play Service
            StartSetup();
        }

        /// <summary>
        /// Perform any final cleanup before an activity is destroyed.
        /// </summary>
        protected override void OnDestroy()
        {
            // Are we attached to the Google Play Service?
            if (serviceConnection != null)
            {
                // Yes, disconnect
                serviceConnection.Disconnect();
            }

            // Call base method
            base.OnDestroy();
        }

        /// <Docs>The integer request code originally supplied to
        ///  startActivityForResult(), allowing you to identify who this
        ///  result came from.</Docs>
        /// <param name="data">An Intent, which can return result data to the caller
        ///  (various data can be attached to Intent "extras").</param>
        /// <summary>
        /// Raises the activity result event.
        /// </summary>
        /// <param name="requestCode">Request code.</param>
        /// <param name="resultCode">Result code.</param>
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            // Ask the open service connection's billing handler to process this request
            serviceConnection.BillingHandler.HandleActivityResult(requestCode, resultCode, data);

            //TODO: Use a call back to update the purchased items
            UpdatePurchasedItems();

            serviceConnection.BillingHandler.OnProductPurchased += (int response, Purchase purchase, string purchaseData, string purchaseSignature) =>
            {
                serviceConnection.BillingHandler.ConsumePurchase(purchase);
            };
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Loads the purchased items.
        /// </summary>
        private void LoadPurchasedItems()
        {
            // Ask the open connection's billing handler to get any purchases
            var purchases = serviceConnection.BillingHandler.GetPurchases(ItemType.Product);

            // Display any existing purchases
            purchasesAdapter = new PurchaseAdapter(this, purchases);
            //lvPurchsedItems.Adapter = purchasesAdapter;
        }

        /// <summary>
        /// Updates the purchased items.
        /// </summary>
        private void UpdatePurchasedItems()
        {
            // Ask the open connection's billing handler to get any purchases
            var purchases = serviceConnection.BillingHandler.GetPurchases(ItemType.Product);

            // Is there a data adapter for purchases?
            if (purchasesAdapter != null)
            {
                // Yes, add new items to adapter
                foreach (var item in purchases)
                {
                    purchasesAdapter.Items.Add(item);
                }

                // Ask the adapter to display the new items
                purchasesAdapter.NotifyDataSetChanged();
            }
        }

        /// <summary>
        /// Connects to the Google Play Service and gets a list of products that are available
        /// for purchase.
        /// </summary>
        /// <returns>The inventory.</returns>
        private async Task GetInventory()
        {
            // Ask the open connection's billing handler to return a list of avilable products for the 
            // given list of items.
            // NOTE: We are asking for the Reserved Test Product IDs that allow you to test In-App
            // Billing without actually making a purchase.
            //products = await serviceConnection.BillingHandler.QueryInventoryAsync(new List<string> { "appra_01_test", "appra_02_sub", ReservedTestProductIDs.Purchased }, ItemType.Product);

            // Real products
            products = await serviceConnection.BillingHandler.QueryInventoryAsync(new List<string> { "android.test.purchased", "guy.donate.1", "guy.donate.2", "guy.donate.3", "guy.donate.5", "guy.donate.7", "guy.donate.14", "guy.donate.20", "guy.donate.50" }, ItemType.Product);
            //products = await serviceConnection.BillingHandler.QueryInventoryAsync(new List<string> { ReservedTestProductIDs.Purchased }, ItemType.Product);

            // Were any products returned?
            if (products == null)
            {
                // No, abort
                return;
            }

            // Enable the list of products
            productSpinner.Enabled = (products.Count > 0);

            // Populate list of available products
            var items = products.OrderBy(x => x.Price).Select(p => p.Title).ToList();
            productSpinner.Adapter = new ArrayAdapter<string>(this, Resource.Layout.LibraryGridItem, Resources.GetStringArray(Resource.Array.donation_google_catalog_values));
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Starts the setup of this Android application by connection to the Google Play Service
        /// to handle In-App purchases.
        /// </summary>
        public void StartSetup()
        {
            // A Licensing and In-App Billing public key is required before an app can communicate with
            // Google Play, however you DON'T want to store the key in plain text with the application.
            // The Unify command provides a simply way to obfuscate the key by breaking it into two or
            // or more parts, specifying the order to reassemlbe those parts and optionally providing
            // a set of key/value pairs to replace in the final string. 
            string value = Security.Unify(
                new string[] { "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnAtEDv/z61eaiFcyVOOB+OjbV5faLPLzhn6sMNSxb+HizNsslVS4s", 
					"G5w/YfW6NJnkwMDa4BNPSMeHHlbEQDIoupU4f0U4+QqKNhdStsbu1TZ3WdpnCwqlsZALs+DGwq76NOP/IxIdSv7PX7Hd0cy9Z", 
					"yQGu9t4Mvf+CyV+Y94XPMvsQvKFSHot+UC+GdpFzKHn9LI+fqoif2hW6oh2JC2Z7bs2BS3I7gV0jXK6aJd3ZXiGuL+iiRvi5a", 
					"uzRN4wv6m1jDY7mZKL62fDVqqyRwH4Q7qRdVkezWj/mCXV0HZvpFPDtWX/X3U4dtm8d3Tw4wwVwQg6GbDHN6lw8JWYgEtcwIDAQAB" },
                new int[] { 0, 1, 2, 3 });

            // Create a new connection to the Google Play Service
            serviceConnection = new InAppBillingServiceConnection(this, "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnAtEDv/z61eaiFcyVOOB+OjbV5faLPLzhn6sMNSxb+HizNsslVS4sG5w/YfW6NJnkwMDa4BNPSMeHHlbEQDIoupU4f0U4+QqKNhdStsbu1TZ3WdpnCwqlsZALs+DGwq76NOP/IxIdSv7PX7Hd0cy9ZyQGu9t4Mvf+CyV+Y94XPMvsQvKFSHot+UC+GdpFzKHn9LI+fqoif2hW6oh2JC2Z7bs2BS3I7gV0jXK6aJd3ZXiGuL+iiRvi5auzRN4wv6m1jDY7mZKL62fDVqqyRwH4Q7qRdVkezWj/mCXV0HZvpFPDtWX/X3U4dtm8d3Tw4wwVwQg6GbDHN6lw8JWYgEtcwIDAQAB");
            serviceConnection.OnConnected += async () =>
            {
                // Attach to the various error handlers to report issues
                serviceConnection.BillingHandler.OnGetProductsError += (int responseCode, Bundle ownedItems) =>
                {
                    Console.WriteLine("Error getting products");
                    Log.Error("InAppBillingServiceConnection", "Error getting products");
                };

                serviceConnection.BillingHandler.OnInvalidOwnedItemsBundleReturned += (Bundle ownedItems) =>
                {
                    Console.WriteLine("Invalid owned items bundle returned");
                    Log.Error("InAppBillingServiceConnection", "Invalid owned items bundle returned");
                };

                serviceConnection.BillingHandler.OnProductPurchasedError += (int responseCode, string sku) =>
                {
                    Console.WriteLine("Error purchasing item {0}", sku);
                    Log.Error("InAppBillingServiceConnection", "Error purchasing item " + sku);
                };

                serviceConnection.BillingHandler.OnPurchaseConsumedError += (int responseCode, string token) =>
                {
                    Console.WriteLine("Error consuming previous purchase");
                    Log.Error("InAppBillingServiceConnection", "Error consuming previous purchase");
                };

                serviceConnection.BillingHandler.InAppBillingProcesingError += (message) =>
                {
                    Console.WriteLine("In app billing processing error {0}", message);
                    Log.Error("InAppBillingServiceConnection", "In app billing processing error " + message);
                };

                // Load inventory or available products
                await GetInventory();

                // Load any items already purchased
                LoadPurchasedItems();
            };

            // Attempt to connect to the service
            serviceConnection.Connect();
        }
        #endregion

        #region User Interaction Routines
        /// <summary>
        /// Handle the user selecting an item from the list of available products
        /// </summary>
        /// <param name="parent">Parent.</param>
        /// <param name="view">View.</param>
        /// <param name="position">Position.</param>
        /// <param name="id">Identifier.</param>
        public void OnItemSelected(AdapterView parent, Android.Views.View view, int position, long id)
        {
            // Grab the selecting product
            try
            {
                selectedProduct = products[position];
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// Handle nothing being selected
        /// </summary>
        /// <param name="parent">Parent.</param>
        public void OnNothingSelected(AdapterView parent)
        {
            // Do nothing
        }

        /// <summary>
        /// Handle the user consuming a previously purchased item
        /// </summary>
        /// <param name="parent">Parent.</param>
        /// <param name="view">View.</param>
        /// <param name="position">Position.</param>
        /// <param name="id">Identifier.</param>
        public void OnItemClick(AdapterView parent, Android.Views.View view, int position, long id)
        {
            // Access item being clicked on
            string productid = ((TextView)view).Text;
            var purchases = purchasesAdapter.Items;
            var purchasedItem = purchases.FirstOrDefault(p => p.ProductId == productid);

            // Was anyting selected?
            if (purchasedItem != null)
            {
                // Yes, attempt to consume the given product
                bool result = serviceConnection.BillingHandler.ConsumePurchase(purchasedItem);

                // Was the product consumed?
                if (result)
                {
                    // Yes, update interface
                    purchasesAdapter.Items.Remove(purchasedItem);
                    purchasesAdapter.NotifyDataSetChanged();
                }
            }
        }
        #endregion
    }
}


