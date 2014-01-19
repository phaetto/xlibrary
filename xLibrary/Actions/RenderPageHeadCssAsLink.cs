namespace xLibrary.Actions
{
    using Chains;

    public sealed class RenderPageHeadCssAsLink : IChainableAction<xContext, HttpResultContextWithxContext>
    {
        private readonly string link;

        public RenderPageHeadCssAsLink(string link)
        {
            this.link = link;
        }

        public HttpResultContextWithxContext Act(xContext context)
        {
            HttpResultContextWithxContext httpResultContext = new HttpResultContextWithxContext(context);

            for (int n = 0; n < context.CssLinks.Count; ++n)
            {
                httpResultContext.ResponseText.Append("<link rel='stylesheet' type='text/css' href='" + context.CssLinks[n] + "' />");
            }

            httpResultContext.ResponseText.AppendLine("<link rel='stylesheet' type='text/css' href='" + link + "' />");
            return httpResultContext;
        }
    }
}
