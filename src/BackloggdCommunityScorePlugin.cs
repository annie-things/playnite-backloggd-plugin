using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace BackloggdCommunityScore
{
    public class BackloggdCommunityScorePlugin : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        public override Guid Id { get; } = Guid.Parse("05106d44-f505-4fd9-9e57-cc8cc737f3a9");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.CommunityScore,
            MetadataField.Links,
            MetadataField.Tags,
            MetadataField.Features
        };

        public override string Name => "Backloggd Community Score";

        public BackloggdCommunityScorePlugin(IPlayniteAPI api) : base(api)
        {
            logger.Info("Backloggd Community Score loaded.");
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new BackloggdCommunityScoreProvider(options, this);
        }
    }
}
