using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Windows.Storage;
using MarkedUp;

namespace MarkedUp.Tests
{
    [TestClass]
    public class CurrentAppSimulatorTests
    {
#if DEBUG
        [TestMethod]
        public async Task Should_get_app_listing_information_for_premium_app()
        {
            //arrange
            var premiumAppConfigFile = await StorageFile.GetFileFromApplicationUriAsync(TestDataUris.FullLicenseFileUri);
            await CurrentAppProxy.ReloadSimulatorSettingsAsync(premiumAppConfigFile);

            //act
            var appStoreListing = await CurrentAppProxy.LoadListingInformationAsync();

            //assert
            Assert.IsNotNull(appStoreListing);
            Assert.AreEqual("$4.99", appStoreListing.FormattedPrice);
#if !WINDOWS_PHONE //Windows Phone 8 does not support age ratings
            Assert.AreEqual(3u, appStoreListing.AgeRating);
#endif
            Assert.AreEqual("Full license", appStoreListing.Name);
            Assert.AreEqual("Sample app for demonstrating trial license management", appStoreListing.Description);
            Assert.AreEqual("US", appStoreListing.CurrentMarket);
        }

        /// <summary>
        /// Should be able to load an app listing from the Windows Store that does not have pricing
        /// </summary>
        [TestMethod]
        public async Task Should_get_app_listing_information_for_free_app()
        {
            //arrange
            var freeAppConfigFile = await StorageFile.GetFileFromApplicationUriAsync(TestDataUris.FreeLicenseUri);
            await CurrentAppProxy.ReloadSimulatorSettingsAsync(freeAppConfigFile);

            //act
            var appStoreListing = await CurrentAppProxy.LoadListingInformationAsync();

            //assert
            Assert.IsNotNull(appStoreListing);
            Assert.AreEqual("$0.00", appStoreListing.FormattedPrice);
#if !WINDOWS_PHONE //Age rating is not supported for Windows Phone 8
            Assert.AreEqual(3u, appStoreListing.AgeRating);
#endif
            Assert.AreEqual("Free license", appStoreListing.Name);
            Assert.AreEqual("Sample app for demonstrating trial license management", appStoreListing.Description);
            Assert.AreEqual("US", appStoreListing.CurrentMarket);
        }

        /// <summary>
        /// Should be able to get the local price and currency for a given in-app purchase
        /// </summary>
        [TestMethod]
        public async Task Should_get_price_and_currency_for_in_app_purchases()
        {
            //arrange
            var freeAppConfigFile = await StorageFile.GetFileFromApplicationUriAsync(TestDataUris.InAppPurchaseLicenseUri);
            await CurrentAppProxy.ReloadSimulatorSettingsAsync(freeAppConfigFile);

            //act
            var productListingWithIAPs = await CurrentAppProxy.LoadListingInformationAsync();

            //assert
            Assert.IsNotNull(productListingWithIAPs);
            Assert.AreEqual(3, productListingWithIAPs.ProductListings.Count);

            //assert MarkedUpExtraFeature
            var markedUpExtraFeature = productListingWithIAPs.ProductListings.FirstOrDefault(x => x.Key == "MarkedUpExtraFeature").Value;
            Assert.IsNotNull(markedUpExtraFeature);
            Assert.AreEqual("MarkedUpExtraFeature", markedUpExtraFeature.Name);
            Assert.AreEqual("$1.99", markedUpExtraFeature.FormattedPrice);

            //assert MarkedUpPremiumFeature
            var markedUpPremiumFeature = productListingWithIAPs.ProductListings.FirstOrDefault(x => x.Key == "MarkedUpPremiumFeature").Value;
            Assert.IsNotNull(markedUpPremiumFeature);
            Assert.AreEqual("MarkedUpPremiumFeature", markedUpPremiumFeature.Name);
            Assert.AreEqual("$4.99", markedUpPremiumFeature.FormattedPrice);

            //assert SomeOtherFeature
            var someOtherFeature = productListingWithIAPs.ProductListings.FirstOrDefault(x => x.Key == "SomeOtherFeature").Value;
            Assert.IsNotNull(someOtherFeature);
            Assert.AreEqual("SomeOtherFeature", someOtherFeature.Name);
            Assert.AreEqual("$14.99", someOtherFeature.FormattedPrice);
        }


#else //don't run any tests in release mode (where CurrentApp is used instead of CurrentAppSimulator)
        [Ignore]
        public void NotRun()
        {
        }
#endif
    }
}
