namespace xLibrary.Actions
{
    using Chains;

    public class RenderPageBodyCssJsHtml : IChainableAction<xTagContext, HttpResultContextWithxContext>
    {
        public HttpResultContextWithxContext Act(xTagContext context)
        {
            return this.RenderBodyCssJsHtml(context);
        }

        HttpResultContextWithxContext RenderBodyCssJsHtml(xTagContext context)
        {
            return context.Parent.Do(new RenderPageBodyCss())
                   .Aggregate(context, new RenderHtml(throwOnBodyTag: true))
                   .Aggregate(context, new RenderPageJavascript());
        }
    }
}
