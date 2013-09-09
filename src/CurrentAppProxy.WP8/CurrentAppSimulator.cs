using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.Store;
using Windows.Storage;

namespace CurrentAppProxy
{
    public class CurrentAppSimulator
    {
        private static CurrentAppSimulator _instance;

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
            if(String.IsNullOrEmpty(expirationString) || !DateTime.TryParse(expirationString, out expirationDate))
                expirationDate = new DateTime(504911232000000000, DateTimeKind.Utc); //The date a developer license expires ({12/31/1600 12:00:00 AM UTC})

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
                listingInfo.CurrentMarket = (string)appNode.Element("CurrentMarket");

                var marketData = appNode.Element("MarketData");

                listingInfo.Description = marketData.Element("Description").SafeRead();
                listingInfo.Name = (string) marketData.Element("Name");
                listingInfo.FormattedPrice = (string) marketData.Element("CurrencySymbol") +
                                             (string) marketData.Element("Price");
                
                //Products
                foreach (var product in node.Elements("Product"))
                {
                    var productListing = new ProductListing();
                    productListing.ProductId = product.Attribute("ProductId").SafeRead();

                    var iapMarketData = node.Element("MarketData");

                    productListing.Description = iapMarketData.Element("Description").SafeRead();
                    productListing.Name = iapMarketData.Element("Name").SafeRead();
                    productListing.FormattedPrice = (string)iapMarketData.Element("CurrencySymbol") +
                                                 (string)iapMarketData.Element("Price");
                    productListing.ProductType = (ProductType)iapMarketData.Element("ProductType").SafeRead(ProductType.Unknown);
                    listingInfo.ProductListings.Add(productListing.ProductId, productListing);
                    
                }
            }

            return resultantListing;
        }

        

        /// <summary>
        /// Loads all of the defaults for simulating CurrentAppSimulator failures on WP8
        /// </summary>
        /// <returns>A populated read-only dictionary containing method names and a boolean indicating if the value is successful or not</returns>
        private static IDictionary<string, bool> DefaultMethodResults()
        {
            return new Dictionary<string, bool>(){ { "LoadListingInformationAsync_GetResult", true } };
        }
    
#endregion
    }

    class AppListing
    {
        public Guid AppId { get; set; }
        public Uri LinkUri { get; set; }
        public ListingInformation ListingInformation { get; set; }
    }

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

}
