namespace xLibrary.Actions
{
    using System;
    using System.Text;
    using Chains;

    public class RenderAsJavascriptClientModel : IChainableAction<xTagContext, HttpResultContextWithxContext>
    {
        private readonly bool RenderLinesOnOutput;
        private readonly bool RenderElementsForAjax;

        private CheckIfRestRequest restService;

        public RenderAsJavascriptClientModel(bool RenderLinesOnOutput = false, bool renderElementsForAjax = true)
        {
            this.RenderLinesOnOutput = RenderLinesOnOutput;
            this.RenderElementsForAjax = renderElementsForAjax;
        }

        public HttpResultContextWithxContext Act(xTagContext context)
        {
            restService = context.Get<CheckIfRestRequest>();
            var httpResultContext = new HttpResultContextWithxContext(context.Parent, contentType: "text/javascript");
            return RenderJs(context.Parent, context.xTag, renderElements: RenderElementsForAjax, httpResultContext: httpResultContext);
        }

        public HttpResultContextWithxContext RenderJs(xContext context, xTag xtag, string argumentsPrefix = null, bool renderElements = true, HttpResultContextWithxContext httpResultContext = null)
        {
            bool isRoot = false;
            if (string.IsNullOrEmpty(argumentsPrefix))
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, "(function(){");

                if (renderElements)
                {
                    // Uri.EscapeDataString MUST GO

                    // Now secure the templates according to their origin
                    for (int n = 0; n < context.TemplateExternalServerOrigin.Count; ++n)
                    {
                        renderItemToStringBuilder(httpResultContext.ResponseText, "xTags.TemplateExternalServerOrigin[\"" + jsEscape(context.TemplateExternalServerOrigin[n]) + "\"] = true;");
                    }

                    // Now add templates and libraries
                    for (int n = 0; n < context.Libraries.Count; ++n)
                    {
                        renderItemToStringBuilder(httpResultContext.ResponseText, "xTags.AnalyseText(\"" + jsEscape(context.Libraries[n].InnerXml) + "\");");
                    }
                }

                xtag.argId = argumentsPrefix = "'" + xtag.GetId() + "'";
                isRoot = true;
            }

            if (string.IsNullOrEmpty(xtag.argId))
                xtag.argId = argumentsPrefix;

            string jsPrefix = "_" + xtag.argId.Replace(".", "").Replace("'", "").Replace(",", "");
            if (renderElements)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, "var " + jsPrefix + " = xTags._b(" +
                    (xtag.RootTag != null && !string.IsNullOrEmpty(xtag.RootTag.argId) ? "[" + xtag.RootTag.argId + "]," : "null,") +
                    (xtag.MainTag != null && !string.IsNullOrEmpty(xtag.MainTag.argId) ? "[" + xtag.MainTag.argId + "]," : "null,") +
                    (!string.IsNullOrEmpty(xtag.xTemplate) ? "\"" + xtag.xTemplate + "\"" : "\"\"") + "," +
                    argumentsPrefix + ");");
            }
            else
            {
                if (isRoot)
                {
                    renderItemToStringBuilder(httpResultContext.ResponseText, "var " + jsPrefix + " = new xTag();");
                }
                else
                    renderItemToStringBuilder(httpResultContext.ResponseText, "var " + jsPrefix + " = xTags._c(" +
                        (xtag.RootTag != null && !string.IsNullOrEmpty(xtag.RootTag.argId) ? "[" + xtag.RootTag.argId + "]," : "null,") +
                        (xtag.MainTag != null && !string.IsNullOrEmpty(xtag.MainTag.argId) ? "[" + xtag.MainTag.argId + "]," : "null,") +
                        "_" + xtag.ParentTag.argId.Replace(".", "").Replace("'", "").Replace(",", "") + "," +
                        xtag.ParentIndex.ToString() + ");");
            }

            if (xtag.LCID > -1)
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".LCID = " + xtag.LCID.ToString() + ";");
            if (!string.IsNullOrEmpty(xtag.xTemplate))
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".xTemplate = '" + xtag.xTemplate + "';");
            if (!string.IsNullOrEmpty(xtag.TagName) && xtag.TagName != "div")
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".TagName = '" + xtag.TagName + "';");
            if (!string.IsNullOrEmpty(xtag.Text))
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Text = \"" + jsEscape(xtag.Text) + "\";");
            if (!string.IsNullOrEmpty(xtag.DefaultText))
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".DefaultText = \"" + jsEscape(xtag.DefaultText) + "\";");
            if (!string.IsNullOrEmpty(xtag.PhraseId))
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".PhraseId = '" + xtag.PhraseId + "';");
            if (!string.IsNullOrEmpty(xtag.Id))
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Id = '" + xtag.Id + "';");
                renderItemToStringBuilder(httpResultContext.ResponseText, "xTags.All['" + xtag.Id + "']=" + jsPrefix + ";");
            }
            if (!string.IsNullOrEmpty(xtag.Session))
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Session = '" + xtag.Session + "';");
            if (!string.IsNullOrWhiteSpace(xtag.EventsData))
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Events = xTags.EvaluateEventsStructure(\"" + jsEscape(xtag.EventsData) + "\");");
            if (!string.IsNullOrEmpty(xtag.DataSource))
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".DataSource = '" + xtag.DataSource + "';");

            foreach (string key in xtag.Data.Keys)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Data['" + key + "']=\"" + jsEscape(xtag.Data[key] as string) + "\";");
            }

            foreach (string key in xtag.Attributes.Keys)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Attributes['" + key + "']=\"" + jsEscape(xtag.Attributes[key]) + "\";");
            }

            foreach (string key in xtag.DefaultData.Keys)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".DefaultData['" + key + "']=\"" + jsEscape(xtag.DefaultData[key] as string) + "\";");
            }

            foreach (string key in xtag.DefaultAttributes.Keys)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".DefaultAttributes['" + key + "']=\"" + jsEscape(xtag.DefaultAttributes[key]) + "\";");
            }

            switch (xtag.Mode)
            {
                // case xTagMode.Normal: // Default
                case xTagMode.Browser:
                    renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Mode = xTags.Mode.Browser;"); break;
                case xTagMode.Server:
                    renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Mode = xTags.Mode.Server;");
                    if (string.IsNullOrEmpty(xtag.ServerOrigin))
                        renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".ServerOrigin = '" + context.ContextInfo.PageUri() + "';");
                    else
                        renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".ServerOrigin = '" + xtag.ServerOrigin + "';");

                    if (string.IsNullOrEmpty(xtag.ModeType))
                        renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".ModeType = '" + xtag.ModeType + "';");

                    if (restService != null)
                    {
                        restService.EnsureCSRFTokenExists(httpResultContext, xtag);
                        renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Token = '" + xtag.Token + "';");
                    }
                    else
                    {
                        renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Token = '" + jsEscape(CheckIfRestRequest.NoToken) + "';");                        
                    }
                    break;
            }

            for (int n = 0; n < xtag.Children.Count; ++n)
            {
                RenderJs(context, xtag.Children[n], argumentsPrefix + "," + n.ToString(), renderElements, httpResultContext);
            }

            foreach (string key in xtag.NamedTagsNames)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".NamedTagsNames.push('" + key + "');");
            }

            foreach (string key in xtag.NamedTags.Keys)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + "['" + key + "']=xTags._a(" + xtag.NamedTags[key].argId + ");");
            }

            if (isRoot)
            {
                if (renderElements)
                {
                    renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".Attach();");
                    renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + ".SetLCID(" + xtag.LCID.ToString() + ", true);");
                    renderItemToStringBuilder(httpResultContext.ResponseText, jsPrefix + " = null;");
                }
                else
                    renderItemToStringBuilder(httpResultContext.ResponseText, "_z = " + jsPrefix + ";");

                renderItemToStringBuilder(httpResultContext.ResponseText, "})();");
            }

            return httpResultContext;
        }

        void renderItemToStringBuilder(StringBuilder sb, string line)
        {
            if (!RenderLinesOnOutput)
                sb.Append(line);
            else
                sb.AppendLine(line);
        }

        string jsEscape(string str)
        {
            return str != null ? str.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\t", "\\t").Replace("\r", "").Replace("\"", "\\\"") : "";
        }
    }
}
