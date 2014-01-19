namespace xLibrary.Actions
{
    using System.Collections.Generic;
    using System.Xml;

    using Chains;

    public class RenderServerLibraryHelp : IChainableAction<xContext, HttpResultContextWithxContext>
    {
        private readonly string library;
        private readonly IEnumerable<xTag> xTags;

        public HttpResultContextWithxContext Act(xContext context)
        {
            return GetServerLibraryHelp(context, library, xTags);
        }

        static public HttpResultContextWithxContext GetServerLibraryHelp(xContext context, string library, IEnumerable<xTag> supportedServerTags)
        {
            var httpResultContext = new HttpResultContextWithxContext(context);
            string libraryPath = context.FixPathToFileOrHttpPath(library);
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(libraryPath);
            XmlNode libraryMetaData = xmlDoc.SelectSingleNode("//library-info[@src]");
            string libraryUri = "http://xtags.msd.am/xTags/xTags.Library.Help.xml";

            xTag page = context.Create("Library.Server.Page.Capabilities", libraryUri);

            if (!context.IsExternalUri(library))
                library = context.ContextInfo.ServerUri() + context.FixPathToWebPath(library);

            string server = context.ContextInfo.ServerUri() + context.ContextInfo.RequestPath;
            if (page.NamedTags.ContainsKey("serverOriginDd"))
            {
                page.NamedTags["serverOriginDd"].Children[0].Text = server;
                page.NamedTags["libraryDd"].Children[0].Text = library;
            }

            page.NamedTags["serverVersion"].Children[0].Text = xContext.xTagsVersion();

            var restService = context.Get<CheckIfRestRequest>();

            if (page.NamedTags.ContainsKey("capabilitiesTable"))
            {
                xTag ctable = page.NamedTags["capabilitiesTable"];
                xTag cbody = ctable.NamedTags["tBody"];

                foreach (xTag tag in supportedServerTags)
                {
                    xTag item = context.Create("Library.Server.Capabilities.Item", libraryUri);
                    cbody.Append(item);

                    string template;

                    if (!string.IsNullOrEmpty(tag.xTemplate))
                    {
                        template = item.NamedTags["Template"].Children[0].Text = tag.xTemplate;
                    }
                    else
                    {
                        item.NamedTags["Template"].Children[0].Text = tag.TagName;
                        template = tag.MainTag.xTemplate;
                        item.NamedTags["Template"].Children[0].Text += " (" + template + ")";
                    }

                    var id = item.NamedTags["Id"].Children[0].Text = tag.GetId();
                    item.NamedTags["Type"].Children[0].Text = tag.ModeType;

                    // Verbs supported
                    item.NamedTags["RestVerbs"].Children[0].Text = string.Empty;

                    if (restService.onGet != null)
                        item.NamedTags["RestVerbs"].Children[0].Text +=
                            (string.IsNullOrEmpty(item.NamedTags["RestVerbs"].Children[0].Text) ? string.Empty : ", ") +
                                "GET";

                    if (restService.onPost != null)
                        item.NamedTags["RestVerbs"].Children[0].Text +=
                            (string.IsNullOrEmpty(item.NamedTags["RestVerbs"].Children[0].Text) ? string.Empty : ", ") +
                                "POST";

                    if (restService.onPut != null)
                        item.NamedTags["RestVerbs"].Children[0].Text +=
                            (string.IsNullOrEmpty(item.NamedTags["RestVerbs"].Children[0].Text) ? string.Empty : ", ") +
                                "PUT";

                    if (restService.onDelete != null)
                        item.NamedTags["RestVerbs"].Children[0].Text +=
                            (string.IsNullOrEmpty(item.NamedTags["RestVerbs"].Children[0].Text) ? string.Empty : ", ") +
                                "DELETE";

                    if (restService.onCustomVerb != null)
                        item.NamedTags["RestVerbs"].Children[0].Text +=
                            (string.IsNullOrEmpty(item.NamedTags["RestVerbs"].Children[0].Text) ? string.Empty : ", ") +
                                "Custom verb handler";

                    if (restService.onAjax != null)
                        item.NamedTags["RestVerbs"].Children[0].Text +=
                            (string.IsNullOrEmpty(item.NamedTags["RestVerbs"].Children[0].Text) ? string.Empty : ", ") +
                                "Ajax extended handler";

                    // How to use it
                    item = context.Create("Library.Server.Capabilities.ItemUsage", libraryUri);
                    cbody.Append(item);
                    replaceText(item, "XmlDefinition", server, library, template, id);
                    replaceText(item, "XmlUsage", server, library, template, id);
                    replaceText(item, "JsUsage", server, library, template, id);
                    replaceText(item, "CsUsage", server, library, template, id);
                }
            }

            //httpResultContext.ResponseText.Append("<!DOCTYPE html>");
            //httpResultContext.ResponseText.Append("<html>");
            //httpResultContext.ResponseText.Append("<head>");
            //httpResultContext.ResponseText.Append("<title>Service " + context.ContextInfo.PageUri() + "</title>");
            //httpResultContext.Do(context, new RenderPageHeadCss());
            //httpResultContext.ResponseText.Append("</head><body>");
            //httpResultContext.Do(xtagContext, new RenderHtml());
            //httpResultContext.Do(xtagContext, new RenderPageJavascript());
            //httpResultContext.ResponseText.Append("</body></html>");

            httpResultContext.ResponseText.Append("<!DOCTYPE html>");
            httpResultContext.Aggregate(new xTagContext(context, page), new RenderHtml());
            return httpResultContext;
        }

        static void replaceText(xTag item, string name, string server, string library, string template, string id)
        {
            item.GetTagsByTemplateName("Library.ServerTemplateUsage")[0].NamedTags[name].NamedTags["Code"].Children[0].Text =
                item.GetTagsByTemplateName("Library.ServerTemplateUsage")[0].NamedTags[name].NamedTags["Code"].Children[0].Text
                .Replace("[Template]", template).Replace("[Library]", library).Replace("[Server]", server).Replace("[Id]", id);
        }
    }
}
