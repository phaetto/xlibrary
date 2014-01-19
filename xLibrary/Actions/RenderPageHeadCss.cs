namespace xLibrary.Actions
{
    using Chains;

    public class RenderPageHeadCss : IChainableAction<xContext, HttpResultContextWithxContext>
    {
        public const string MoveToTopFrameJavascript = @"<script type='text/javascript'><!--
try { if (top.location.hostname != self.location.hostname) top.location.replace(self.location.href); } catch(e) { top.location.replace(self.location.href); }
--></script>";

        private readonly bool clickjackingSupport;

        public RenderPageHeadCss(bool clickjackingSupport = true)
        {
            this.clickjackingSupport = clickjackingSupport;
        }

        public HttpResultContextWithxContext Act(xContext context)
        {
            return RenderHeadCss(context);
        }

        HttpResultContextWithxContext RenderHeadCss(xContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context);

            for (int n = 0; n < context.CssLinks.Count; ++n)
            {
                httpResultContext.ResponseText.Append("<link rel='stylesheet' type='text/css' href='" + context.CssLinks[n] + "' />");
            }

            if (context.CssText.Count > 0)
            {
                httpResultContext.ResponseText.Append("<style type='text/css'>");
                for (int n = 0; n < context.CssText.Count; ++n)
                {
                    httpResultContext.ResponseText.Append(context.CssText[n]);
                }
                httpResultContext.ResponseText.Append("</style>");
            }

            // When you render code on the head possibly you do not want to clickjacked (no frame support)
            if (clickjackingSupport)
            {
                httpResultContext.AddHeader("X-Frame-Options", "deny");
                httpResultContext.ResponseText.AppendLine(MoveToTopFrameJavascript);
            }

            return httpResultContext;
        }
    }
}
