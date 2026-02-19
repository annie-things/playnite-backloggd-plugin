using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Playnite.SDK.Models;

namespace BackloggdCommunityScore
{
    public class BackloggdCommunityScoreProvider : OnDemandMetadataProvider
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly MetadataRequestOptions options;
        private readonly BackloggdCommunityScorePlugin plugin;
        private readonly BackloggdClient backloggdClient;

        private bool fetchAttempted;
        private BackloggdAggregateScore cachedScore;
        private string cachedBackloggdUrl;

        public override List<MetadataField> AvailableFields => plugin.SupportedFields;

        public BackloggdCommunityScoreProvider(MetadataRequestOptions options, BackloggdCommunityScorePlugin plugin)
        {
            this.options = options;
            this.plugin = plugin;
            backloggdClient = BackloggdClient.Shared;
        }

        public override int? GetCommunityScore(GetMetadataFieldArgs args)
        {
            if (!TryGetBackloggdData(out var score, out _))
            {
                return null;
            }

            var mappedCommunityScore = MapBackloggdScoreToPlaynite(score.RatingValue);
            logger.Info(
                $"Imported Backloggd community score for '{options?.GameData?.Name}': " +
                $"{score.RatingValue.ToString("0.###", CultureInfo.InvariantCulture)} -> {mappedCommunityScore}.");

            return mappedCommunityScore;
        }

        public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
        {
            if (!TryGetBackloggdData(out var score, out var backloggdGameUrl))
            {
                return null;
            }

            return new[]
            {
                new Link(BuildBackloggdLinkName(score.RatingCount), backloggdGameUrl)
            };
        }

        public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
        {
            return GetRatingsMetadataProperties();
        }

        public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
        {
            return GetRatingsMetadataProperties();
        }

        private bool TryGetBackloggdData(out BackloggdAggregateScore score, out string backloggdGameUrl)
        {
            score = null;
            backloggdGameUrl = null;

            if (fetchAttempted)
            {
                score = cachedScore;
                backloggdGameUrl = cachedBackloggdUrl;
                return score != null;
            }

            fetchAttempted = true;

            var game = options?.GameData;
            backloggdGameUrl = BackloggdUrlResolver.Resolve(game);

            if (string.IsNullOrWhiteSpace(backloggdGameUrl))
            {
                logger.Info($"Skipping '{game?.Name}': could not resolve a Backloggd game URL from links or game name.");
                return false;
            }

            logger.Info($"Resolved Backloggd URL for '{game?.Name}': {backloggdGameUrl}");

            if (!backloggdClient.TryGetAggregateScore(backloggdGameUrl, out score, out var error))
            {
                logger.Warn($"Failed to import Backloggd score for '{game?.Name}' from '{backloggdGameUrl}': {error}");
                return false;
            }

            cachedBackloggdUrl = backloggdGameUrl;
            cachedScore = score;
            return true;
        }

        private static int MapBackloggdScoreToPlaynite(decimal backloggdScore)
        {
            // Backloggd uses a 0.0-5.0 scale (half-star increments), while Playnite uses 0-100.
            var mapped = (int)Math.Round(backloggdScore * 20m, MidpointRounding.AwayFromZero);
            if (mapped < 0)
            {
                return 0;
            }

            if (mapped > 100)
            {
                return 100;
            }

            return mapped;
        }

        private static string BuildBackloggdLinkName(int? ratingCount)
        {
            if (!ratingCount.HasValue)
            {
                return "Backloggd";
            }

            return $"Backloggd ({ratingCount.Value.ToString("N0", CultureInfo.InvariantCulture)} ratings)";
        }

        private IEnumerable<MetadataProperty> GetRatingsMetadataProperties()
        {
            if (!TryGetBackloggdData(out var score, out _))
            {
                return null;
            }

            if (!score.RatingCount.HasValue)
            {
                return null;
            }

            var count = score.RatingCount.Value.ToString("N0", CultureInfo.InvariantCulture);
            var name = $"Backloggd Ratings: {count}";
            return new[] { new MetadataNameProperty(name) }.ToList();
        }
    }
}
