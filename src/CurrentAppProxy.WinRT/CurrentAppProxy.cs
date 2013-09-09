using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
#if DEBUG //Only need access to the WinRT file system when debugging
using Windows.Foundation;
using Windows.Storage;
#endif

namespace MarkedUp
{
    /// <summary>
    /// Helpers which enables us swap between using CurrentApp in release and CurrentAppSimulator during testing
    /// </summary>
    public static class CurrentAppProxy
    {
#if DEBUG   
        /// <summary>
        /// Reloads and restarts the simulator when provided with a new StorageFile reference
        /// </summary>
        /// <param name="storageFile">A new WinStoreAppProxy.xml file with the desired settings</param>
        public static async Task ReloadSimulatorSettingsAsync(StorageFile storageFile)
        {
            await CurrentAppSimulator.ReloadSimulatorAsync(storageFile);
        }
#endif
        /// <summary>
        /// Returns the licensing information for the current app
        /// </summary>
        public static LicenseInformation LicenseInformation
        {
            get
            {
#if DEBUG
                return LicenseInformation.Create(CurrentAppSimulator.LicenseInformation);

#else
                var licenseInfo = LicenseInformation.Create(CurrentApp.LicenseInformation);
                return licenseInfo;
#endif
            }
        }

        /// <summary>
        /// Gets the Windows Store ID of the app
        /// </summary>
        public static Guid AppId
        {
            get
            {
#if DEBUG
                return CurrentAppSimulator.AppId;
#else
                return CurrentApp.AppId;
#endif
            }
        }

        /// <summary>
        /// The Uri for this app in the Windows Store
        /// </summary>
        public static Uri LinkUri
        {
            get
            {
#if DEBUG
                return CurrentAppSimulator.LinkUri;

#else
                return CurrentApp.LinkUri;
#endif
            }
        }

        /// <summary>
        /// Loads the app's listing information asynchronously.
        /// </summary>
        /// <returns></returns>
        public static async Task<ListingInformation> LoadListingInformationAsync()
        {
#if DEBUG
            return ListingInformation.Create(await CurrentAppSimulator.LoadListingInformationAsync());
            
#else
            return ListingInformation.Create(await CurrentApp.LoadListingInformationAsync());
#endif
        }

        public static async Task<string> RequestProductPurchaseAsync(string productId, bool includeReceipt)
        {
#if DEBUG
            return await CurrentAppSimulator.RequestProductPurchaseAsync(productId, includeReceipt);
#else
            return await CurrentApp.RequestProductPurchaseAsync(productId, includeReceipt);
#endif
        }

        public static async Task<string> RequestAppPurchaseAsync(bool includeReceipt)
        {
#if DEBUG
            return await CurrentAppSimulator.RequestAppPurchaseAsync(includeReceipt);
#else
            return await CurrentApp.RequestAppPurchaseAsync(includeReceipt);
#endif
        }

        public static async Task<string> GetAppReceiptAsync()
        {
#if DEBUG
            return await CurrentAppSimulator.GetAppReceiptAsync();
#else
            return await CurrentApp.GetAppReceiptAsync();
#endif
        }

        public static async Task<string> GetProductReceiptAsync(string productId)
        {
#if DEBUG
            return await CurrentAppSimulator.GetProductReceiptAsync(productId);
#else
            return await CurrentApp.GetProductReceiptAsync(productId);
#endif
        }
    }

    #region Windows Store Wrappers - designed to make it easier to mock / direct behavior of Windows Store across platforms

    /// <summary>
    /// Class used to describe the marketplace listing information for an in-app purchase
    /// </summary>
    public sealed class ProductListing
    {
        /// <summary>
        /// The unique identifier for this app or in-app purchase available to customers
        /// in this market for this app
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// The human-readable name of the app or in-app purchase available to customers
        /// in this market for this app
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The product's purchase price with appropriate region/market-specific formatting
        /// </summary>
        public string FormattedPrice { get; set; }

#if WINDOWS_PHONE //Windows Phone-only members

        /// <summary>
        /// The Uri of the image associated with this product
        /// </summary>
        public Uri ImageUri { get; set; }

        /// <summary>
        /// The search keywords associated with this product
        /// </summary>
        public IEnumerable<string> Keywords { get; set; }

        /// <summary>
        /// Gets the description for the product
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// The type of product; can be either ProductType.Durable or ProductType.Consumable
        /// </summary>
        public ProductType ProductType { get; set; }

        /// <summary>
        /// The tag string that contains custom information about this product
        /// </summary>
        public string Tag { get; set; }
#endif

        /// <summary>
        /// Creates a new ProductListing instance based on a populated Windows.ApplicationModel.Store.ProductListing instance
        /// </summary>
        /// <param name="source">A valid Windows.ApplicationModel.Store.ProductListing from the CurrentApp or CurrentAppSimulator class</param>
        /// <returns>A ProductListing instance with all properties copied from the original</returns>
        public static ProductListing Create(Windows.ApplicationModel.Store.ProductListing source)
        {
            var productListing = new ProductListing
            {
                ProductId = source.ProductId,
                Name = source.Name,

#if WINDOWS_PHONE //Windows Phone-only members
                Description = source.Description,
                ImageUri = source.ImageUri,
                Keywords = source.Keywords.ToList(),
                ProductType = source.ProductType,
                Tag = source.Tag,
#endif
                FormattedPrice = source.FormattedPrice
            };

            return productListing;
        }

        /// <summary>
        /// Overload to be used by the CurrentAppSimulator for Windows Phone 8; decided
        /// that this was a nicer way to do it than a bunch more IFDEFs
        /// </summary>
        /// <param name="clone">A fully instantiated ProductListing implementation</param>
        /// <returns>The initial product listing implementation</returns>
        internal static ProductListing Create(ProductListing clone)
        {
            return clone;
        }
    }

    /// <summary>
    /// Class used to describe the licensing status of an in-app purchase
    /// </summary>
    public sealed class ProductLicense
    {
        /// <summary>
        /// The id of this in-app purchase in the store
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// True if the license is active and valid; false otherwise.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Windows Phone 8 only. Determines if this in-app purchase is consumable (so it must
        /// be repurchased after each time it's used)
        /// </summary>
        public bool IsConsumable { get; set; }

        /// <summary>
        /// The date an in-app purchase exires (not Implemented on Windows Phone)
        /// </summary>
        public DateTimeOffset ExpirationDate { get; set; }

        /// <summary>
        /// Creates a new ProductLicense class based on a listing supplied directly from the Windows Store APIs
        /// </summary>
        /// <param name="source">An original Windows.ApplicationModel.Store.ProductLicense from the CurrentApp class</param>
        /// <returns>A populated ProductLicense class</returns>
        public static ProductLicense Create(Windows.ApplicationModel.Store.ProductLicense source)
        {
            var productLicense = new ProductLicense
            {
#if WINDOWS_PHONE //Windows Phone 8
                    ExpirationDate = DateTimeOffset.MaxValue, //MaxValue means that the license is active and will not expire
                    IsConsumable = source.IsConsumable,
#else //WinRT
                ExpirationDate = source.ExpirationDate,
#endif

                IsActive = source.IsActive,
                ProductId = source.ProductId
            };

            return productLicense;
        }

        /// <summary>
        /// Overload to be used by the CurrentAppSimulator for Windows Phone 8; decided
        /// that this was a nicer way to do it than a bunch more IFDEFs
        /// </summary>
        /// <param name="clone">A fully instantiated ProductLicense implementation</param>
        /// <returns>The initial ProductLicense implementation</returns>
        public static ProductLicense Create(ProductLicense clone)
        {
            return clone;
        }
    }

    /// <summary>
    /// Class used to describe the marketplace listing information for an app
    /// and all of its associated in-app purchases
    /// </summary>
    public sealed class ListingInformation
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public string FormattedPrice { get; set; }

#if !WINDOWS_PHONE //Not supported on WP8

        /// <summary>
        /// The age rating for this app (WinRT only)
        /// </summary>
        public uint AgeRating { get; set; }

#endif

        public string CurrentMarket { get; set; }

        public Dictionary<string, ProductListing> ProductListings { get; set; }

        public ListingInformation()
        {
            ProductListings = new Dictionary<string, ProductListing>();
        }

        public static ListingInformation Create(Windows.ApplicationModel.Store.ListingInformation source)
        {
            var listingInformation = new ListingInformation()
            {

#if !WINDOWS_PHONE //Not supported on WP8
                AgeRating = source.AgeRating,
                CurrentMarket = source.CurrentMarket,
#else
                /*
                 * On Windows Phone 8, the CurrentRegion set for your phone dictates which market a user will
                 * have access to. See: http://stackoverflow.com/questions/14141404/how-to-get-the-currency-of-an-in-app-purchase-product-on-windows-phone-8
                 */
                CurrentMarket = System.Globalization.RegionInfo.CurrentRegion.TwoLetterISORegionName,
#endif
                Description = source.Description,
                FormattedPrice = source.FormattedPrice,
                Name = source.Name,
                ProductListings = source.ProductListings.ToDictionary(key => key.Key, value => ProductListing.Create(value.Value))
            };


            return listingInformation;
        }

        /// <summary>
        /// Overload to be used by the CurrentAppSimulator for Windows Phone 8; decided
        /// that this was a nicer way to do it than a bunch more IFDEFs
        /// </summary>
        /// <param name="clone">A fully instantiated ListingInformation implementation</param>
        /// <returns>The initial ListingInformation implementation</returns>
        public static ListingInformation Create(ListingInformation clone)
        {
            return clone;
        }
    }

    /// <summary>
    /// Class used to describe all licensing information specific to the installed app 
    /// </summary>
    public sealed class LicenseInformation
    {
        /// <summary>
        /// True if the license is active and valid; false otherwise.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// True if this is a trial license for this application; false otherwise.
        /// </summary>
        public bool IsTrial { get; set; }

        /// <summary>
        /// The date an in-app purchase exires (not Implemented on Windows Phone)
        /// </summary>
        public DateTimeOffset ExpirationDate { get; set; }

        /// <summary>
        /// A list of licenses for the app's features that can be
        /// bought via in-app purchase
        /// </summary>
        public Dictionary<string, ProductLicense> ProductLicenses { get; set; }

        /// <summary>
        /// Raises a notification event if the app's license changes
        /// </summary>
#pragma warning disable 67
        public event LicenseChangedEventHandler LicenseChanged;
#pragma warning restore 67

        /// <summary>
        /// Creates a LicenseInformation class based upon a Windows.ApplicationModel.Store.LicenseInformation object
        /// </summary>
        /// <param name="source">A valid Windows.ApplicationModel.Store.LicenseInformation object</param>
        /// <returns>A LicenseInformation class with all properties mapped from source</returns>
        public static LicenseInformation Create(Windows.ApplicationModel.Store.LicenseInformation source)
        {
            var licenseInformation = new LicenseInformation()
            {
                IsActive = source.IsActive,
                IsTrial = source.IsTrial,
                ExpirationDate = source.ExpirationDate,
                ProductLicenses = source.ProductLicenses.ToDictionary(key => key.Key, value => ProductLicense.Create(value.Value))
            };

#if WINDOWS_PHONE && !DEBUG //The Windows Phone  new Microsoft.Phone.Marketplace.LicenseInformation().IsTrial(); is what really determines the Trial vs. Full license status in production for WP8
             licenseInformation.IsTrial = new Microsoft.Phone.Marketplace.LicenseInformation().IsTrial();
#endif

            return licenseInformation;
        }

        /// <summary>
        /// Overload to be used by the CurrentAppSimulator for Windows Phone 8; decided
        /// that this was a nicer way to do it than a bunch more IFDEFs
        /// </summary>
        /// <param name="clone">A fully instantiated LicenseInformation implementation</param>
        /// <returns>The initial LicenseInformation implementation</returns>
        public static LicenseInformation Create(LicenseInformation clone)
        {
            return clone;
        }

    }

    #endregion
}
