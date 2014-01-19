namespace xLibrary.UnitTests
{
    using System.Xml;
    using Chains.Play.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using xLibrary;
    using xLibrary.Actions;

    [TestClass]
    public class PersistenceBehavior
    {
        [TestMethod]
        public void SaveTag_Session_WhenTagIsSaved_ThenIsLoadedAgain()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' modetype='session'><data type='text/name-value' name='test'><![CDATA[starting value]]></data></template></r>");
            var persistedValue = "New value";

            var httpContextInfo = new HttpContextInfo();

            var xTagContext = new xContext(httpContextInfo).Do(new LoadLibrary(doc)).Do(new CreateTag("template"));
            xTagContext.xTag.Data["test"] = persistedValue;
            xTagContext.Do(new SaveTag());

            var xTagContext2 = new xContext(httpContextInfo).Do(new LoadLibrary(doc)).Do(new CreateTag("template")).Do(new LoadTag());

            Assert.AreEqual(xTagContext2.xTag.Data["test"], persistedValue);
        }

        [TestMethod]
        public void SaveTag_Application_WhenTagIsSaved_ThenIsLoadedAgain()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' modetype='application'><data type='text/name-value' name='test'><![CDATA[starting value]]></data></template></r>");
            var persistedValue = "New value";

            var httpContextInfo = new HttpContextInfo();

            var xTagContext = new xContext(httpContextInfo).Do(new LoadLibrary(doc)).Do(new CreateTag("template"));
            xTagContext.xTag.Data["test"] = persistedValue;
            xTagContext.Do(new SaveTag());

            var xTagContext2 = new xContext(httpContextInfo).Do(new LoadLibrary(doc)).Do(new CreateTag("template")).Do(new LoadTag());

            Assert.AreEqual(xTagContext2.xTag.Data["test"], persistedValue);
        }
    }
}
