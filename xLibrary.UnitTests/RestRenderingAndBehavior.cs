namespace xLibrary.UnitTests
{
    using System.Web;
    using System.Xml;
    using Chains.Play.Web;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using xLibrary;
    using xLibrary.Actions;

    [TestClass]
    public class RestRenderingAndBehavior
    {
        [TestMethod]
        public void CheckIfRestRequest_WhenRequestBeenSendForNotAServerTemplate_ThenNormaHtmlShouldBeProcessed()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' /></r>");

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(), new RenderHtml());

            var responseText = result.ResponseText.ToString();
            Assert.AreEqual(responseText, "<div id='a'></div>");
        }

        [TestMethod]
        public void CheckIfRestRequest_Session_WhenAjaxSentWithoutCSRFToken_ThenTokenShouldBeSentFirst()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: EmptyGetHandler), new RenderHtml());

            var responseText = result.ResponseText.ToString();

            Assert.IsTrue(responseText.StartsWith("{\"xtags-renew-token\":\""));
            Assert.IsTrue(responseText.EndsWith("\"}"));
            Assert.AreEqual(result.ContentType, "text/plain");
            Assert.AreEqual(httpContextInfo.Session("a"), responseText.Replace("{\"xtags-renew-token\":\"", string.Empty).Replace("\"}", string.Empty));
        }

        [TestMethod]
        public void CheckIfRestRequest_Cookies_WhenAjaxSentWithoutCSRFToken_ThenTokenShouldBeSentFirst()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: EmptyGetHandler, useCsrfCookies: true), new RenderHtml());

            var responseText = result.ResponseText.ToString();

            Assert.IsTrue(responseText.StartsWith("{\"xtags-renew-token\":\""));
            Assert.IsTrue(responseText.EndsWith("\"}"));
            Assert.AreEqual(result.ContentType, "text/plain");
            Assert.AreEqual(result.ResponseCookies["a"].Value, responseText.Replace("{\"xtags-renew-token\":\"", string.Empty).Replace("\"}", string.Empty));
        }

        [TestMethod]
        public void CheckIfRestRequest_Session_WhenAjaxCSRFTokenInvalid_ThenTokenShouldBeSentFirst()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.QueryString.Add("xtags-token", "Invalid token");

            httpContextInfo.Session("a", "Invalid");

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: EmptyGetHandler), new RenderHtml());

            var responseText = result.ResponseText.ToString();

            Assert.IsTrue(responseText.StartsWith("{\"xtags-renew-token\":\""));
            Assert.IsTrue(responseText.EndsWith("\"}"));
            Assert.AreEqual(result.ContentType, "text/plain");
            Assert.AreEqual(httpContextInfo.Session("a"), responseText.Replace("{\"xtags-renew-token\":\"", string.Empty).Replace("\"}", string.Empty));
        }

        [TestMethod]
        public void CheckIfRestRequest_Cookies_WhenAjaxCSRFTokenInvalid_ThenTokenShouldBeSentFirst()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.QueryString.Add("xtags-token", "Invalid token");

            httpContextInfo.Cookies.Add(new HttpCookie("a"));
            httpContextInfo.Cookies["a"].Value = "Invalid";

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: EmptyGetHandler, useCsrfCookies: true), new RenderHtml());

            var responseText = result.ResponseText.ToString();

            Assert.IsTrue(responseText.StartsWith("{\"xtags-renew-token\":\""));
            Assert.IsTrue(responseText.EndsWith("\"}"));
            Assert.AreEqual(result.ContentType, "text/plain");
            Assert.AreEqual(result.ResponseCookies["a"].Value, responseText.Replace("{\"xtags-renew-token\":\"", string.Empty).Replace("\"}", string.Empty));
        }

        [TestMethod]
        public void CheckIfRestRequest_Session_WhenAjaxCSRFTokenIsValid_ThenGetRequestShouldBeAjax()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var validToken = "valid-token";

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.QueryString.Add("xtags-token", validToken);

            httpContextInfo.Session("a", validToken);

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: (tag, isAjax) =>
                    {
                        Assert.IsTrue(isAjax);
                        Assert.AreEqual(tag.Id, "a");
                    }), new RenderHtml());

            var responseText = result.ResponseText.ToString();

            Assert.IsTrue(responseText.StartsWith("(function(){"));
            Assert.IsTrue(responseText.Contains("'a'"));
            Assert.IsTrue(responseText.Contains("'" + httpContextInfo.PageUri() + "'"));
            Assert.IsTrue(responseText.EndsWith("})();"));
            Assert.AreEqual(result.ContentType, "text/plain");
        }

        [TestMethod]
        public void CheckIfRestRequest_Cookie_WhenAjaxCSRFTokenIsValid_ThenGetRequestShouldBeAjax()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var validToken = "valid-token";

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.QueryString.Add("xtags-token", validToken);

            httpContextInfo.Cookies.Add(new HttpCookie("a"));
            httpContextInfo.Cookies["a"].Value = validToken;

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: (tag, isAjax) =>
                    {
                        Assert.IsTrue(isAjax);
                        Assert.AreEqual(tag.Id, "a");
                    }, useCsrfCookies: true), new RenderHtml());

            var responseText = result.ResponseText.ToString();

            Assert.IsTrue(responseText.StartsWith("(function(){"));
            Assert.IsTrue(responseText.Contains("'a'"));
            Assert.IsTrue(responseText.Contains("'" + httpContextInfo.PageUri() + "'"));
            Assert.IsTrue(responseText.EndsWith("})();"));
            Assert.AreEqual(result.ContentType, "text/plain");
        }

        [TestMethod]
        public void CheckIfRestRequest_FormGet_WhenAjaxCSRFTokenIsValid_ThenGetRequestShouldNotBeAjax()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var validToken = "valid-token";

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.QueryString.Add("xtags-token", validToken);

            httpContextInfo.Session("a", validToken);

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: (tag, isAjax) =>
                    {
                        Assert.IsFalse(isAjax);
                        Assert.AreEqual(tag.Id, "a");
                    }), new RenderHtml());

            var responseText = result.ResponseText.ToString();
            Assert.AreEqual(responseText, "<div id='a'></div>");
            Assert.AreEqual(result.ContentType, "text/html");
        }

        [TestMethod]
        public void CheckIfRestRequest_FormGet_WhenAjaxCSRFTokenInvalid_ThenServerGetIsNotRequestedAndJustRenderResponse()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var validToken = "valid-token";

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.QueryString.Add("xtags-token", validToken);

            httpContextInfo.Session("a", "invalid-token");

            var isMethodCalled = false;

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: (tag, isAjax) =>
                    {
                        isMethodCalled = true;
                    }), new RenderHtml());

            var responseText = result.ResponseText.ToString();
            Assert.AreEqual(responseText, "<div id='a'></div>");
            Assert.AreEqual(result.ContentType, "text/html");
            Assert.IsFalse(isMethodCalled);
        }

        [TestMethod]
        public void CheckIfRestRequest_FormPost_WhenAjaxCSRFTokenIsValid_ThenGetRequestShouldBeAjax()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var validToken = "valid-token";

            var httpContextInfo = new HttpContextInfo(httpMethod: "POST");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.Form.Add("xtags-token", validToken);

            httpContextInfo.Session("a", validToken);

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onPost: (tag, isAjax) =>
                    {
                        Assert.IsFalse(isAjax);
                        Assert.AreEqual(tag.Id, "a");
                    }), new RenderHtml());

            var responseText = result.ResponseText.ToString();
            Assert.AreEqual(responseText, "<div id='a'></div>");
            Assert.AreEqual(result.ContentType, "text/html");
        }

        [TestMethod]
        public void CheckIfRestRequest_FormPost_WhenCSRFTokenIsValidButValuesAreMixedBetweenFormAndQuerystring_ThenTheHandlerShouldNotBeExecuted()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var validToken = "valid-token";

            var httpContextInfo = new HttpContextInfo(httpMethod: "POST");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.Form.Add("xtags-token", validToken);

            httpContextInfo.Session("a", validToken);

            var isMethodCalled = false;

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onPost: (tag, isAjax) =>
                    {
                        isMethodCalled = true;
                    }), new RenderHtml());

            var responseText = result.ResponseText.ToString();
            Assert.AreEqual(responseText, "<div id='a'></div>");
            Assert.AreEqual(result.ContentType, "text/html");
            Assert.IsFalse(isMethodCalled);
        }

        [TestMethod]
        public void CheckIfRestRequest_FormPost_WhenCSRFTokenIsValidButValuesAreMixedBetweenFormAndQuerystring2_ThenTheHandlerShouldNotBeExecuted()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var validToken = "valid-token";

            var httpContextInfo = new HttpContextInfo(httpMethod: "POST");
            httpContextInfo.Form.Add("xtags-id", "a");
            httpContextInfo.QueryString.Add("xtags-token", validToken);

            httpContextInfo.Session("a", validToken);

            var isMethodCalled = false;

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onPost: (tag, isAjax) =>
                    {
                        isMethodCalled = true;
                    }), new RenderHtml());

            var responseText = result.ResponseText.ToString();
            Assert.AreEqual(responseText, "<div id='a'></div>");
            Assert.AreEqual(result.ContentType, "text/html");
            Assert.IsFalse(isMethodCalled);
        }

        [TestMethod]
        public void CheckIfRestRequest_Session_WhenAjaxHasCallback_ThenJsonpResponseRendered()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='a' mode='Server' /></r>");

            var validToken = "valid-token";

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "a");
            httpContextInfo.QueryString.Add("xtags-token", validToken);
            httpContextInfo.QueryString.Add("callback", "callbackMethod");

            httpContextInfo.Session("a", validToken);

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: (tag, isAjax) =>
                    {
                        Assert.IsTrue(isAjax);
                        Assert.AreEqual(tag.Id, "a");
                    }), new RenderHtml())
                    .Do(new RenderJsonpIfRequested());

            // var responseText = result.ResponseText.ToString();
            Assert.AreEqual(result.ContentType, "text/javascript");
        }

        [TestMethod]
        public void CheckIfRestRequest_WhenAjaxSentWithoutCSRFTokenWhenCSRFIsDisabled_ThenNormalAjaxRequest()
        {
            var doc = new XmlDocument();
            doc.LoadXml("<r><template id='b' mode='Server' /></r>");

            var httpContextInfo = new HttpContextInfo();
            httpContextInfo.QueryString.Add("xtags-xajax", "xtags-xajax");
            httpContextInfo.QueryString.Add("xtags-http-method", "GET");
            httpContextInfo.QueryString.Add("xtags-id", "b");

            var result =
                new xContext(httpContextInfo)
                    .Do(new LoadLibrary(doc))
                    .Do(new CreateTag("template"))
                    .DoFirst(x => x != null, new CheckIfRestRequest(onGet: (tag, isAjax) =>
                    {
                        Assert.IsTrue(isAjax);
                        Assert.AreEqual(tag.Id, "b");
                    }, csrfProtectionEnabled: false), new RenderHtml());

            Assert.AreEqual(result.ContentType, "text/plain");
            Assert.IsNull(httpContextInfo.Session("b"));
            Assert.IsNull(result.ResponseCookies["b"]);
        }

        // xtags-values-only

        private void EmptyGetHandler(xTag xtag, bool iAjax)
        {
        }
    }
}
