namespace xLibrary.Actions
{
    using Chains;

    public class RenderAsGoogleGadget : IChainableAction<xTagContext, HttpResultContextWithxContext>
    {
        private const string GadgetTemplatePrefix = @"<?xml version=""1.0"" encoding=""utf-8"" ?>
	<ModulePrefs title=""{0}"" />
	<Content type=""html"">
		<![CDATA[";

        private const string GadgetTemplateSuffix = @"]]>
	</Content>
</Module>";

        private readonly string gadgetTitle;

        public RenderAsGoogleGadget(string gadgetTitle)
        {
            this.gadgetTitle = gadgetTitle;
        }

        public HttpResultContextWithxContext Act(xTagContext context)
        {
            var httpResultContext = new HttpResultContextWithxContext(context.Parent)
                                    {
                                        ContentType = "text/xml"
                                    };

            httpResultContext.ResponseText.Append(string.Format(GadgetTemplatePrefix, gadgetTitle));
            httpResultContext.Aggregate(context, new RenderPageBodyCssJsHtml());
            httpResultContext.ResponseText.Append(GadgetTemplateSuffix);
            return httpResultContext;
        }
    }
}
