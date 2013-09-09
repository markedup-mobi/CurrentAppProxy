using System;
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
    }
#else //don't run any tests in release mode (where CurrentApp is used instead of CurrentAppSimulator)
    [Ignore]
    public void NotRun(){
    }
#endif
}
