namespace xLibrary.Actions
{
    using Chains;

    public sealed class RenderPageHeadCssAsExternalFile : IChainableAction<xContext, HttpResultContextWithxContext>
    {
        public HttpResultContextWithxContext Act(xContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context, contentType: "text/css");

            for (int n = 0; n < context.CssText.Count; ++n)
            {
                httpResultContext.ResponseText.AppendLine(context.CssText[n]);
            }

            return httpResultContext;
        }
    }
}
