// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Annie <annieannie@anche.no>
// Trans rights are human rights

using System;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace BackloggdCommunityScore
{
    internal static class BackloggdHtmlParser
    {
        private static readonly Regex jsonLdScriptRegex = new Regex(
            "<script[^>]*type\\s*=\\s*\"application/ld\\+json\"[^>]*>(?<json>.*?)</script>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private static readonly Regex aggregateTypeRegex = new Regex(
            "\"@type\"\\s*:\\s*\"AggregateRating\"",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ratingValueRegex = new Regex(
            "\"ratingValue\"\\s*:\\s*\"?(?<value>[0-9]+(?:\\.[0-9]+)?)\"?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ratingCountRegex = new Regex(
            "\"ratingCount\"\\s*:\\s*\"?(?<value>[0-9][0-9,]*)\"?",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex fallbackVisibleScoreRegex = new Regex(
            "id\\s*=\\s*\"game-rating\".*?<h1[^>]*>(?<value>[0-9]+(?:\\.[0-9]+)?)</h1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        public static bool TryParseAggregateScore(string html, out BackloggdAggregateScore score, out string error)
        {
            score = null;
            error = null;

            if (string.IsNullOrWhiteSpace(html))
            {
                error = "HTML payload was empty.";
                return false;
            }

            foreach (Match scriptMatch in jsonLdScriptRegex.Matches(html))
            {
                var json = WebUtility.HtmlDecode(scriptMatch.Groups["json"].Value);

                if (!aggregateTypeRegex.IsMatch(json))
                {
                    continue;
                }

                if (!TryParseRatingValue(json, out var ratingValue))
                {
                    continue;
                }

                TryParseRatingCount(json, out var ratingCount);

                score = new BackloggdAggregateScore
                {
                    RatingValue = ratingValue,
                    RatingCount = ratingCount
                };

                return true;
            }

            if (TryParseFallbackVisibleScore(html, out var fallbackScore))
            {
                score = new BackloggdAggregateScore
                {
                    RatingValue = fallbackScore,
                    RatingCount = null
                };

                return true;
            }

            error = "Could not locate Backloggd aggregate rating in page HTML.";
            return false;
        }

        private static bool TryParseRatingValue(string source, out decimal ratingValue)
        {
            ratingValue = 0m;
            var match = ratingValueRegex.Match(source);
            if (!match.Success)
            {
                return false;
            }

            return decimal.TryParse(
                match.Groups["value"].Value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out ratingValue);
        }

        private static bool TryParseRatingCount(string source, out int? ratingCount)
        {
            ratingCount = null;
            var match = ratingCountRegex.Match(source);
            if (!match.Success)
            {
                return false;
            }

            var normalized = match.Groups["value"].Value.Replace(",", string.Empty);
            if (!int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedCount))
            {
                return false;
            }

            ratingCount = parsedCount;
            return true;
        }

        private static bool TryParseFallbackVisibleScore(string html, out decimal ratingValue)
        {
            ratingValue = 0m;
            var match = fallbackVisibleScoreRegex.Match(html);
            if (!match.Success)
            {
                return false;
            }

            return decimal.TryParse(
                match.Groups["value"].Value,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out ratingValue);
        }
    }
}
