using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace xLibrary.Actions
{
    using Chains;

    public class FindTag : IChainableAction<xTagContext,xTagContext>
    {
        readonly private Func<xTagContext, xTag> findTag;

        public FindTag(Func<xTagContext, xTag> findTag)
        {
            this.findTag = findTag;
        }

        public xTagContext Act(xTagContext context)
        {
            return new xTagContext(context.Parent, findTag(context));
        }
    }
}
