namespace xLibrary.Actions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using Chains;

    public class LoadTag : IChainableAction<xTagContext, xTagContext>
    {
        private readonly Action<xTag, string, string, string> customPersistence;

        public LoadTag(Action<xTag, string, string, string> customPersistence = null)
        {
            this.customPersistence = customPersistence;
        }

        public xTagContext Act(xTagContext context)
        {
            Load(context);
            return context;
        }

        void Load(xTagContext context)
        {
            // If we have a different ServerOrigin than context.xTag page
            // then we have to go and GET those values

            var id = GetPersistNameTag(context.xTag);
            var newHt = new Dictionary<string, string>();
            foreach (string key in context.xTag.Data.Keys)
            {
                newHt[key] = context.xTag.Data[key];
                if (!key.ToLowerInvariant().StartsWith("xtags-"))
                {
                    switch (context.xTag.ModeType.ToLowerInvariant())
                    {
                        case "session":
                            if (!context.Parent.ContextInfo.HasSession())
                                throw new NotSupportedException("Session is not available in context.xTag context");

                            if (context.Parent.ContextInfo.Session(id + "." + key) != null)
                                newHt[key] = context.Parent.ContextInfo.Session(id + "." + key) as string;
                            break;
                        case "application":
                            if (context.Parent.ContextInfo.Application(id + "." + key) != null)
                                newHt[key] = context.Parent.ContextInfo.Application(id + "." + key) as string;
                            break;
                        default:
                            if (customPersistence != null)
                            {
                                customPersistence(
                                    context.xTag, context.xTag.ModeType, key, context.xTag.Data[key] as string);
                                break;
                            }

                            throw new InvalidOperationException("Tag cannot be loaded.");
                    }

                    if (context.xTag.Attributes.ContainsKey(key))
                        context.xTag.Attributes[key] = newHt[key];
                }
            }

            context.xTag.Data = newHt;
        }

        public string GetPersistNameTag(xTag xtag)
        {
            return xtag.GetId();
        }
    }
}
