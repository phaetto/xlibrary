namespace xLibrary.UnitTests
{
    using System.Xml;
    using Chains.Play.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using xLibrary;
    using xLibrary.Actions;

    [TestClass]
    public class OfflineManifestRendering
    {
        [TestMethod]
        public void RenderOfflineManifest_WhenNoLibraryIsRendered_ThenItHasAnEmptyManifest()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a'><span /></template></r>");

            var result = new xContext(new HttpContextInfo())
                .Do(new RenderOfflineManifest(new string[0]));

            var resultText = result.ResponseText.ToString();
            Assert.AreEqual(result.ContentType, "text/cache-manifest");
            Assert.IsTrue(resultText.StartsWith("CACHE MANIFEST\n# Last-modified:"));
        }
    }
}
