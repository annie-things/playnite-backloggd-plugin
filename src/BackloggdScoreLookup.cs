// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Annie <annieannie@anche.no>
// Trans rights are human rights

using Playnite.SDK.Models;
using System;
using System.Text.RegularExpressions;

namespace BackloggdCommunityScore
{
    internal static class BackloggdScoreLookup
    {
        private static readonly Regex variantSuffixRegex = new Regex(
            "^(?<base>.+)--\\d+$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool TryGetAggregateScore(Game game, out BackloggdAggregateScore score, out string backloggdGameUrl, out string error)
        {
            score = null;
            backloggdGameUrl = BackloggdUrlResolver.Resolve(game);
            error = null;

            if (string.IsNullOrWhiteSpace(backloggdGameUrl))
            {
                error = "Could not resolve a Backloggd game URL from links or game name.";
                return false;
            }

            var primarySuccess = BackloggdClient.Shared.TryGetAggregateScoreWithTitleYear(
                backloggdGameUrl,
                out var primaryScore,
                out var primaryTitleYear,
                out var primaryError);

            if (!TryBuildCanonicalCandidateUrl(backloggdGameUrl, out var canonicalUrl) || game == null || !game.ReleaseYear.HasValue)
            {
                score = primaryScore;
                error = primaryError;
                return primarySuccess;
            }

            var canonicalSuccess = BackloggdClient.Shared.TryGetAggregateScoreWithTitleYear(
                canonicalUrl,
                out var canonicalScore,
                out var canonicalTitleYear,
                out var canonicalError);

            if (!primarySuccess && !canonicalSuccess)
            {
                error = !string.IsNullOrWhiteSpace(primaryError) ? primaryError : canonicalError;
                return false;
            }

            if (!canonicalSuccess)
            {
                score = primaryScore;
                error = primaryError;
                return true;
            }

            if (!primarySuccess)
            {
                score = canonicalScore;
                backloggdGameUrl = canonicalUrl;
                error = canonicalError;
                return true;
            }

            var targetYear = game.ReleaseYear.Value;
            var primaryMatchesYear = primaryTitleYear.HasValue && primaryTitleYear.Value == targetYear;
            var canonicalMatchesYear = canonicalTitleYear.HasValue && canonicalTitleYear.Value == targetYear;

            if (canonicalMatchesYear && !primaryMatchesYear)
            {
                score = canonicalScore;
                backloggdGameUrl = canonicalUrl;
                return true;
            }

            if (primaryMatchesYear && !canonicalMatchesYear)
            {
                score = primaryScore;
                return true;
            }

            // If year doesn't disambiguate (or both match), prefer the entry with more votes.
            // Ports and duplicate variant pages usually have lower rating counts than the main entry.
            var primaryCount = primaryScore?.RatingCount;
            var canonicalCount = canonicalScore?.RatingCount;
            if (primaryCount.HasValue && canonicalCount.HasValue && canonicalCount.Value != primaryCount.Value)
            {
                if (canonicalCount.Value > primaryCount.Value)
                {
                    score = canonicalScore;
                    backloggdGameUrl = canonicalUrl;
                    return true;
                }

                score = primaryScore;
                return true;
            }

            score = primaryScore;
            return true;
        }

        private static bool TryBuildCanonicalCandidateUrl(string backloggdGameUrl, out string canonicalUrl)
        {
            canonicalUrl = null;

            if (!Uri.TryCreate(backloggdGameUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!uri.Host.EndsWith("backloggd.com", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length < 2 || !segments[0].Equals("games", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var slug = segments[1].Trim();
            if (string.IsNullOrWhiteSpace(slug))
            {
                return false;
            }

            var match = variantSuffixRegex.Match(slug);
            if (!match.Success)
            {
                return false;
            }

            var baseSlug = match.Groups["base"].Value.Trim();
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                return false;
            }

            canonicalUrl = $"https://www.backloggd.com/games/{baseSlug}/";
            return !canonicalUrl.Equals(backloggdGameUrl, StringComparison.OrdinalIgnoreCase);
        }
    }
}
