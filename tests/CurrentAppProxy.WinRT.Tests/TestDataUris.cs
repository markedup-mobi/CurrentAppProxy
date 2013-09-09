using System;

namespace MarkedUp.Tests
{
    /// <summary>
    /// Uris that contain TestData files - since it's super awkward for WinRT to load this itself
    /// </summary>
    public static class TestDataUris
    {
        /*
         * WinStoreProxy.xml file locations
         */
        public static readonly Uri DeveloperLicenseFileUri = new Uri("ms-appx:///TestData/CurrentAppSimulator/developer-appinfo.xml");
        public static readonly Uri TrialLicenseFileUri = new Uri("ms-appx:///TestData/CurrentAppSimulator/trial-appinfo.xml");
        public static readonly Uri FullLicenseFileUri = new Uri("ms-appx:///TestData/CurrentAppSimulator/purchased-appinfo.xml");
        public static readonly Uri FreeLicenseUri = new Uri("ms-appx:///TestData/CurrentAppSimulator/free-appinfo.xml");
        public static readonly Uri InAppPurchaseLicenseUri = new Uri("ms-appx:///TestData/CurrentAppSimulator/inapppurchase-appinfo.xml");
        public static readonly Uri FailedAppStoreListingLookupUri = new Uri("ms-appx:///TestData/CurrentAppSimulator/purchased-appinfo-fail.xml");
    }
}
