using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Store;
using Windows.Storage;

namespace MarkedUp
{
#if DEBUG //This class should not exist in production, just like the real thing on WinRT\
    public class CurrentAppSimulator
    {
        private static readonly DateTime DEVELOPER_LICENSE_EXPIRES = new DateTime(504911232000000000, DateTimeKind.Utc);

        private static CurrentAppSimulator _instance = DefaultAppSimulator();

        private readonly IDictionary<string, bool> _methodResults;
        private readonly AppListing _listingInformation;
        private readonly LicenseInformation _licenseInformation;

        private CurrentAppSimulator(AppListing listingInformation, LicenseInformation licenseInformation,
                                    IDictionary<string, bool> methodResults)
        {
            _listingInformation = listingInformation;
            _licenseInformation = licenseInformation;
            _methodResults = methodResults;
        }

        #region public methods


        public static async Task ReloadSimulatorAsync(StorageFile storageFile)
        {
            using (var stream = await storageFile.OpenStreamForReadAsync().ConfigureAwait(false))
            {
                var document = XDocument.Load(stream);
                _instance = FromXml(document);
            }
        }

        public static Guid AppId
        {
            get { return _instance._listingInformation.AppId; }
        }

        public static Uri LinkUri
        {
            get { return _instance._listingInformation.LinkUri; }
        }

        public static string CurrentMarket
        {
            get { return _instance._listingInformation.ListingInformation.CurrentMarket; }
        }

        public static LicenseInformation LicenseInformation
        {
            get { return _instance._licenseInformation; }
        }

        public static async Task<ListingInformation> LoadListingInformationAsync()
        {
            return await Task.Run(() =>
                {
                    if (!_instance._methodResults["LoadListingInformationAsync_GetResult"])
                        throw new ApplicationException("LoadListingInformationAsync was programmed to fail in CurrentAppSimulator settings");
                    return _instance._listingInformation.ListingInformation;
                });
        }

        #endregion

        #region Default CurrentAppSimulator settings

        /// <summary>
        /// Populates a default CurrentAppSimulator implementation based off of what's available in the WMAppManifest.xml file
        /// </summary>
        /// <returns>A CurrentAppSimulator instance populated via the contents of the WMAppManifest.xml file</returns>
        private static CurrentAppSimulator DefaultAppSimulator()
        {
            var appProperties = new AppListing();
            appProperties.ListingInformation = new ListingInformation();
            var appManifestXml = XDocument.Load(
               "WMAppManifest.xml");

            using (var rdr = appManifestXml.CreateReader(ReaderOptions.None))
            {
                rdr.ReadToDescendant("App");
                if (!rdr.IsStartElement())
                {
                    throw new System.FormatException("WMAppManifest.xml is missing.");
                }

                var productId = rdr.GetAttribute("ProductID");
                appProperties.AppId = Guid.Parse(productId);
                appProperties.LinkUri = new Uri(string.Format("https://store.windows.com/en-US/{0}", appProperties.AppId.ToString()));
                appProperties.ListingInformation.CurrentMarket = RegionInfo.CurrentRegion.TwoLetterISORegionName;
                appProperties.ListingInformation.Description = rdr.GetAttribute("Description");
                appProperties.ListingInformation.Name = rdr.GetAttribute("Title");
                appProperties.ListingInformation.FormattedPrice = string.Format("{0}{1}",
                                                                                RegionInfo.CurrentRegion
                                                                                          .ISOCurrencySymbol, 0.00d);
            }

            return new CurrentAppSimulator(appProperties, DefaultLicenseInformation(), DefaultMethodResults());
        }

        #endregion

        #region XML populating methods (used for populating the CurrentAppSimulator object)

        private static CurrentAppSimulator FromXml(XDocument simulatorXml)
        {
            var listingInformation = GetListingFromXml(simulatorXml.Descendants("ListingInformation"));
            var licenseInformation = GetLicenseInformationFromXml(simulatorXml.Descendants("LicenseInformation"));
            var methodSimulations = GetMethodSimulationsFromXml(simulatorXml.Descendants("Simulation"));

            return new CurrentAppSimulator(listingInformation, licenseInformation, methodSimulations);
        }

        private static IDictionary<string, bool> GetMethodSimulationsFromXml(IEnumerable<XElement> simulationNodes)
        {
            var simulatedResponses = DefaultMethodResults();

            foreach (var defaultResponseNode in simulationNodes.Elements("DefaultResponse"))
            {
                var methodName = defaultResponseNode.Attribute("MethodName").SafeRead();
                var hResult = defaultResponseNode.Attribute("HResult").SafeRead("E_FAIL");
                var methodSucceeds = !hResult.Equals("E_FAIL");

                if (simulatedResponses.ContainsKey(methodName))
                    simulatedResponses[methodName] = methodSucceeds;
                else
                {
                    simulatedResponses.Add(methodName, methodSucceeds);
                }

            }

            return simulatedResponses;
        }

        private static LicenseInformation GetLicenseInformationFromXml(IEnumerable<XElement> licenseNodes)
        {
            var li = new LicenseInformation();

            var licenseNode = licenseNodes.First();

            var expirationString = licenseNode.Element("ExpirationDate").SafeRead();
            DateTime expirationDate;
            if (String.IsNullOrEmpty(expirationString) || !DateTime.TryParse(expirationString, out expirationDate))
                expirationDate = DEVELOPER_LICENSE_EXPIRES; //The date a developer license expires ({12/31/1600 12:00:00 AM UTC})

            li.ExpirationDate = expirationDate;
            li.IsActive = bool.Parse(licenseNode.Element("IsActive").SafeRead("true"));
            li.IsTrial = bool.Parse(licenseNode.Element("IsTrial").SafeRead("true"));

            return li;
        }

        private static AppListing GetListingFromXml(IEnumerable<XElement> listingNodes)
        {
            var resultantListing = new AppListing();
            var listingInfo = new ListingInformation();
            var node = listingNodes.First();
            {
                var appNode = node.Element("App");
                resultantListing.AppId = Guid.Parse((string)appNode.Element("AppId"));
                resultantListing.LinkUri = new Uri((string)appNode.Element("LinkUri"));
                listingInfo.CurrentMarket = appNode.Element("CurrentMarket").SafeRead();

                //Parse out the correct ISO country code for the current market based on the region information included in the XML file
                if (listingInfo.CurrentMarket == null)
                    listingInfo.CurrentMarket = RegionInfo.CurrentRegion.TwoLetterISORegionName; //use current region by default
                else
                    listingInfo.CurrentMarket = new RegionInfo(listingInfo.CurrentMarket).TwoLetterISORegionName;

                var marketData = appNode.Element("MarketData");

                listingInfo.Description = marketData.Element("Description").SafeRead();
                listingInfo.Name = (string)marketData.Element("Name");
                listingInfo.FormattedPrice = string.Format("{0}{1}", (string)marketData.Element("CurrencySymbol"),
                                             Double.Parse(marketData.Element("Price").SafeRead("0.00")).ToString("0.00", CultureInfo.CurrentUICulture));

                //Products
                foreach (var product in node.Elements("Product"))
                {
                    var productListing = new ProductListing();
                    productListing.ProductId = product.Attribute("ProductId").SafeRead();

                    var iapMarketData = product.Element("MarketData");

                    productListing.Description = iapMarketData.Element("Description").SafeRead();
                    productListing.Name = iapMarketData.Element("Name").SafeRead();
                    productListing.FormattedPrice = string.Format("{0}{1}", (string)iapMarketData.Element("CurrencySymbol"),
                                             Double.Parse(iapMarketData.Element("Price").SafeRead("0.00")).ToString("0.00", CultureInfo.CurrentUICulture));
                    productListing.ProductType = (ProductType)iapMarketData.Element("ProductType").SafeRead(ProductType.Unknown);
                    listingInfo.ProductListings.Add(productListing.ProductId, productListing);

                }
            }

            resultantListing.ListingInformation = listingInfo;
            return resultantListing;
        }



        /// <summary>
        /// Loads all of the defaults for simulating CurrentAppSimulator failures on WP8
        /// </summary>
        /// <returns>A populated read-only dictionary containing method names and a boolean indicating if the value is successful or not</returns>
        private static IDictionary<string, bool> DefaultMethodResults()
        {
            return new Dictionary<string, bool>() { { "LoadListingInformationAsync_GetResult", true } };
        }

        /// <summary>
        /// Default license information if none is specified through Simulator settings
        /// </summary>
        /// <returns>A populated LicenseInformation object</returns>
        private static LicenseInformation DefaultLicenseInformation()
        {
            return new LicenseInformation()
                {
                    ExpirationDate = DEVELOPER_LICENSE_EXPIRES,
                    IsActive = true,
                    IsTrial = true
                };
        }

        #endregion
    }

    #region AppListing class - used to hold internal state for WP8 - CurrentAppSimulator

    class AppListing
    {
        public Guid AppId { get; set; }
        public Uri LinkUri { get; set; }
        public ListingInformation ListingInformation { get; set; }
    }

    #endregion

    #region XDocument (Linq-to-XML) extension methods to make parsing fun and safe!

    public static class XDocumentExtensions
    {
        public static string SafeRead(this XElement element, string defaultValue = null)
        {
            return element == null ? defaultValue : element.Value;
        }

        public static string SafeRead(this XAttribute attribute, string defaultValue = null)
        {
            return attribute == null ? defaultValue : attribute.Value;
        }

        public static Enum SafeRead(this XElement element, Enum defaultValueIfNull)
        {
            if (element == null)
                return defaultValueIfNull;

            return (Enum)Enum.Parse(defaultValueIfNull.GetType(), element.Value);
        }
    }

    #endregion

#endif

}
