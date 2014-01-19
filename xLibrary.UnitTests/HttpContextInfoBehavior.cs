namespace xLibrary.UnitTests
{
    using System;
    using Chains.Play.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class HttpContextInfoBehavior
    {
        [TestMethod]
        public void HttpContextInfo_WhenServerMapPathIsLocal_ThenTransformToFilePath()
        {
            var httpContextInfo = new HttpContextInfo();

            var path = httpContextInfo.ServerMapPath("~/files/file.js");

            Assert.AreEqual(AppDomain.CurrentDomain.BaseDirectory + "\\files\\file.js", path);
        }
    }
}
