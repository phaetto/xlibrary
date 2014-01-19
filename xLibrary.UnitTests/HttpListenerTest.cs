namespace xLibrary.UnitTests
{
    using Chains.Play.Web;
    using Chains.Play.Web.HttpListener;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using xLibrary.UnitTests.Classes;

    [TestClass]
    public class HttpListenerTest
    {
        [TestMethod]
        public void HttpServer_WhenSessionIsCorrectInASecuredContext_ThenChangesTheContext()
        {
            var server = new ServerHost(new Client("localhost", 901));
            string response;

            using (var httpServer = server.Do(
                new StartHttpServer(
                    new[]
                    {
                        "/test-template/"
                    })))
            {
                httpServer.Modules.Add(new xTagsHttpContextForTest("<r><template id='a' /></r>", "template"));
                var responseResult = HttpRequest.DoRequest("http://localhost:901/test-template/");
                Assert.IsFalse(responseResult.HasError);
                response = responseResult.Response;
            }

            Assert.AreEqual("<div id='a'></div>", response);
        }
    }
}
