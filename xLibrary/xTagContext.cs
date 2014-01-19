namespace xLibrary
{
    using Chains;

    sealed public class xTagContext : ChainWithHistoryAndParent<xTagContext, xContext>
    {
        public readonly xTag xTag;

        public xTagContext(xContext xcontext, xTag xtag) : base(xcontext)
        {
            xTag = xtag;
        }
    }
}
