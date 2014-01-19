namespace xLibrary.Actions
{
    using System;

    using Chains;

    public class SaveTag : IChainableAction<xTagContext, xTagContext>
    {
        private readonly Action<xTag, string, string, string> customPersistence;

        public SaveTag(Action<xTag, string, string, string> customPersistence = null)
        {
            this.customPersistence = customPersistence;
        }

        public xTagContext Act(xTagContext context)
        {
            Save(context);
            return context;
        }

        void Save(xTagContext context)
        {
            // If we have a different ServerOrigin than context.xTag page
            // then we have to go and PUT those values

            string id = GetPersistNameTag(context.xTag);
            foreach (string key in context.xTag.Data.Keys)
            {
                if (!key.ToLowerInvariant().StartsWith("xtags-"))
                {
                    switch (context.xTag.ModeType.ToLowerInvariant())
                    {
                        case "session":
                            if (!context.Parent.ContextInfo.HasSession())
                                throw new NotSupportedException("Session is not available in context.xTag context");
                            context.Parent.ContextInfo.Session(id + "." + key, context.xTag.Data[key] as string); break;
                        case "application":
                            context.Parent.ContextInfo.Application(id + "." + key, context.xTag.Data[key] as string); break;
                        default:
                            if (customPersistence != null)
                            {
                                customPersistence(
                                    context.xTag, context.xTag.ModeType, key, context.xTag.Data[key] as string);
                                break;
                            }

                            throw new InvalidOperationException("Tag cannot be saved.");
                    }
                }
            }
        }

        public string GetPersistNameTag(xTag xtag)
        {
            return xtag.GetId();
        }
    }
}
