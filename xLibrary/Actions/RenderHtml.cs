namespace xLibrary.Actions
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Web;

    using Chains;

    public class RenderHtml : IChainableAction<xTagContext,HttpResultContextWithxContext>
    {
        private readonly Func<xTag, string> customRendering;

        private readonly bool throwOnBodyTag;

        private readonly bool renderJavascriptOnBodyTag;

        private readonly bool renderCssOnHeadTag;

        private readonly IChainableAction<xContext, HttpResultContextWithxContext> cssRenderAction;

        private readonly IChainableAction<xTagContext, HttpResultContextWithxContext> javascriptRenderAction;

        private CheckIfRestRequest restService;

        public RenderHtml(
            Func<xTag, string> customRendering = null,
            bool throwOnBodyTag = false,
            bool renderJavascriptOnBodyTag = true,
            bool renderCssOnHeadTag = true,
            IChainableAction<xContext, HttpResultContextWithxContext> cssRenderAction = null,
            IChainableAction<xTagContext, HttpResultContextWithxContext> javascriptRenderAction = null)
        {
            this.customRendering = customRendering;
            this.throwOnBodyTag = throwOnBodyTag;
            this.renderJavascriptOnBodyTag = renderJavascriptOnBodyTag;
            this.renderCssOnHeadTag = renderCssOnHeadTag;
            this.cssRenderAction = cssRenderAction;
            this.javascriptRenderAction = javascriptRenderAction;

            if (this.cssRenderAction == null)
                this.cssRenderAction = new RenderPageHeadCss();

            if (this.javascriptRenderAction == null)
                this.javascriptRenderAction = new RenderPageJavascript();
        }

        public HttpResultContextWithxContext Act(xTagContext context)
        {
            restService = context.Get<CheckIfRestRequest>();
            var httpResultContext = new HttpResultContextWithxContext(context.Parent);
            this.Render(context.Parent, context.xTag, httpResultContext: httpResultContext);
            return httpResultContext;
        }

        public void Render(xContext context, xTag xtag, bool isChild = false, HttpResultContextWithxContext httpResultContext = null)
        {
            if (customRendering != null)
            {
                string customRenderedValue = customRendering(xtag);
                if (!string.IsNullOrEmpty(customRenderedValue))
                {
                    httpResultContext.ResponseText.Append(customRenderedValue);
                    return;
                }
            }

            if (xtag.TagName == "#text")
            {
                httpResultContext.ResponseText.Append(HttpUtility.HtmlEncode(xtag.Text));
                return;
            }

            if (xtag.TagName == "html")
            {
                httpResultContext.ResponseText.Append("<!DOCTYPE html>\n");
            }

            httpResultContext.ResponseText.Append("<" + xtag.TagName);

            if (xtag.TagName != "html")
            {
                if (!isChild)
                    httpResultContext.ResponseText.Append(" id='" + xtag.GetId() + "'");
                else if (!string.IsNullOrEmpty(xtag.Id))
                    httpResultContext.ResponseText.Append(" id='" + xtag.Id + "'");
            }
            else
            {
                if (throwOnBodyTag)
                    throw new InvalidOperationException("The html tag is not supported from this context.");

                if (xtag.LCID != -1)
                {
                    var tagCultureInfo = new CultureInfo(xtag.LCID, false);
                    httpResultContext.ResponseText.Append(" lang='" + tagCultureInfo.TwoLetterISOLanguageName + "'");
                }
            }

            foreach (string key in xtag.Attributes.Keys)
            {
                switch (key)
                {
                    case "name":
                        if (xtag.TagName != "input" && xtag.TagName != "textarea")
                            httpResultContext.ResponseText.Append(" " + key + "='" + xtag.Attributes[key] + "'");
                        break;
                    case "value":
                        if (xtag.TagName != "textarea")
                            httpResultContext.ResponseText.Append(" " + key + "='" + xtag.Attributes[key] + "'");
                        break;
                    default:
                        httpResultContext.ResponseText.Append(" " + key + "='" + xtag.Attributes[key] + "'");
                        break;
                }
            }

            if (xtag.TagName == "form" && xtag.Mode == xTagMode.Server && !xtag.Attributes.ContainsKey("action"))
            {
                // Add server origin atribute or leave blank (posts to current page)
                if (!string.IsNullOrEmpty(xtag.ServerOrigin))
                    httpResultContext.ResponseText.Append(" action='" + xtag.ServerOrigin + "'");
            }

            if (xtag.TagName != "img" &&
                xtag.TagName != "input" &&
                xtag.TagName != "br" &&
                xtag.TagName != "hr" &&
                xtag.TagName != "meta" &&
                xtag.TagName != "link")
            {
                httpResultContext.ResponseText.Append(">");

                for (int n = 0; n < xtag.Children.Count; ++n)
                {
                    Render(context, xtag.Children[n], true, httpResultContext);
                }

                // in the end so we do not break js attachment
                if (xtag.TagName == "form")
                {
                    if (xtag.Mode == xTagMode.Server)
                    {
                        // Render values as hidden - enabling REST
                        foreach (string key in xtag.Data.Keys)
                        {
                            renderItemToStringBuilder(
                                httpResultContext.ResponseText,
                                "<input type='hidden' name='" + Uri.EscapeDataString(key) + "' value='"
                                    + Uri.EscapeDataString(xtag.Data[key] as string) + "' />");
                        }

                        if (restService != null)
                        {
                            // Add CSRF token
                            restService.EnsureCSRFTokenExists(httpResultContext, xtag);
                        }

                        renderItemToStringBuilder(httpResultContext.ResponseText, "<input type='hidden' name='xtags-token' value='" + xtag.Token + "' />");
                        renderItemToStringBuilder(httpResultContext.ResponseText, "<input type='hidden' name='xtags-return-url' value='" + HttpContext.Current.Request.RawUrl + "' />");
                        renderItemToStringBuilder(httpResultContext.ResponseText, "<input type='hidden' name='xtags-id' value='" + xtag.GetId() + "' />");

                        if (!string.IsNullOrEmpty(xtag.Session))
                            renderItemToStringBuilder(httpResultContext.ResponseText, "<input type='hidden' name='xtags-session' value='" + Uri.EscapeDataString(xtag.Session) + "' />");
                        if (!string.IsNullOrEmpty(xtag.ApiKey))
                            renderItemToStringBuilder(httpResultContext.ResponseText, "<input type='hidden' name='xtags-apikey' value='" + Uri.EscapeDataString(xtag.ApiKey) + "' />");
                    }
                }

                if (xtag.TagName == "head" && renderCssOnHeadTag)
                {
                    if (throwOnBodyTag)
                        throw new InvalidOperationException("The head tag is not supported from this context.");

                    httpResultContext.Aggregate(context, this.cssRenderAction);
                }

                if (xtag.TagName == "body")
                {
                    if (throwOnBodyTag)
                        throw new InvalidOperationException("The body tag is not supported from this context.");

                    if (renderJavascriptOnBodyTag)
                    {
                        httpResultContext.Aggregate(new xTagContext(context, xtag), this.javascriptRenderAction);
                    }
                }

                httpResultContext.ResponseText.Append("</" + xtag.TagName + ">");
            }
            else
            {
                // And close tag
                httpResultContext.ResponseText.Append(" />");
            }

            return;
        }

        void renderItemToStringBuilder(StringBuilder sb, string line)
        {
            sb.Append(line);
        }
    }
}
