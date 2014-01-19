namespace xLibrary.UnitTests.Classes
{
    using System.Net;
    using System.Xml;
    using Chains;
    using Chains.Play.Web;
    using Chains.Play.Web.HttpListener;
    using xLibrary.Actions;

    public class xTagsHttpContextForTest : Chain<xTagsHttpContextForTest>, IHttpRequestHandler
    {
        public readonly string templateXml;
        public readonly string templateName;

        public xTagsHttpContextForTest(string templateXml, string templateName)
        {
            this.templateXml = templateXml;
            this.templateName = templateName;
        }

        public bool ResolveRequest(HttpListenerContext context)
        {
            var doc = new XmlDocument();
            doc.LoadXml(templateXml);

            new xContext(new HttpContextInfo(context)).Do(new LoadLibrary(doc))
                                                      .Do(new CreateTag(templateName))
                                                      .Do(new RenderHtml())
                                                      .ApplyOutputToHttpContext();

            return true;
        }
    }
}
