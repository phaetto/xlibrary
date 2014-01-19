namespace xLibrary.Actions
{
    using Chains;

    public class CreateTag : IChainableAction<xContext,xTagContext>
    {
        private readonly string templateName;
        private readonly string libraryUri;
        private readonly int lcid;

        public CreateTag(string templateName, string libraryUri = null, int lcid = -1)
        {
            this.templateName = templateName;
            this.libraryUri = libraryUri;
            this.lcid = lcid;
        }

        public xTagContext Act(xContext context)
        {
            var xtag = context.Create(templateName, libraryUri);
            xtag.LCID = lcid;
            return new xTagContext(context, xtag);
        }
    }
}
