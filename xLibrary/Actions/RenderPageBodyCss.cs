namespace xLibrary.Actions
{
    using System.Text;

    using Chains;

    public class RenderPageBodyCss : IChainableAction<xContext, HttpResultContextWithxContext>
    {
        public HttpResultContextWithxContext Act(xContext context)
        {
            return this.RenderBodyCss(context);
        }

        HttpResultContextWithxContext RenderBodyCss(xContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context);

            if (context.CssLinks.Count == 0 && context.CssText.Count == 0)
                return httpResultContext;

            renderItemToStringBuilder(httpResultContext.ResponseText, "<style type='text/css'>");
            for (int n = 0; n < context.CssLinks.Count; ++n)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, "@import url(" + context.CssLinks[n] + ");");
            }

            for (int n = 0; n < context.CssText.Count; ++n)
            {
                renderItemToStringBuilder(httpResultContext.ResponseText, context.CssText[n]);
            }

            renderItemToStringBuilder(httpResultContext.ResponseText, "</style>");

            return httpResultContext;
        }

        void renderItemToStringBuilder(StringBuilder sb, string line)
        {
            sb.Append(line);
        }
    }
}
