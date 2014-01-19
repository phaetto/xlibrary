namespace xLibrary.UnitTests
{
    using System;
    using Chains.Play.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using xLibrary;

    [TestClass]
    public class xContextTests
    {
        [TestMethod]
        public void FixPathToWebPath_WhenNoHttpContext_ThenReturnsBasedOnAbsoluteApplicationPath()
        {
            var link = "~/somewhere/else";

            var xContext = new xContext(new HttpContextInfo());

            var transformedLink = xContext.FixPathToWebPath(link);

            Assert.IsTrue(transformedLink.StartsWith("/"));
            Assert.IsTrue(transformedLink.EndsWith("/somewhere/else"));
        }

        [TestMethod]
        public void FixPathToFileOrHttpPath_WhenNoHttpContext_ThenReturnsBasedOnAbsoluteApplicationPath()
        {
            var link = "~/somewhere/else";

            var xContext = new xContext(new HttpContextInfo());

            var transformedLink = xContext.FixPathToFileOrHttpPath(link);

            Assert.AreEqual(AppDomain.CurrentDomain.BaseDirectory + "\\somewhere\\else", transformedLink);
        }

        [TestMethod]
        public void FixPathToFileOrHttpPath_WhenNoHttpContextButNoTilde_ThenReturnsBasedOnAbsoluteApplicationPath()
        {
            var link = "/somewhere/else";

            var xContext = new xContext(new HttpContextInfo());

            var transformedLink = xContext.FixPathToFileOrHttpPath(link);

            Assert.AreEqual("/somewhere/else", transformedLink);
        }
    }
}
