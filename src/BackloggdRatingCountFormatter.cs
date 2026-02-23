// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Annie <annieannie@anche.no>
// Trans rights are human rights

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BackloggdCommunityScore
{
    internal static class BackloggdRatingCountFormatter
    {
        private const string Prefix = "Backloggd Ratings: ";
        private static readonly Regex LeadingLineRegex = new Regex(
            @"^\s*Backloggd Ratings:\s*[\d,]+(?:\s*(?:<br\s*/?>|\r?\n))*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex LeadingBreakRegex = new Regex(
            @"^(?:\s|<br\s*/?>)+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string BuildRatingCountLine(int? ratingCount)
        {
            if (!ratingCount.HasValue)
            {
                return null;
            }

            return $"{Prefix}{ratingCount.Value.ToString("N0", CultureInfo.InvariantCulture)}";
        }

        public static string UpsertLineAtTop(string existingText, string backloggdLine)
        {
            if (string.IsNullOrWhiteSpace(backloggdLine))
            {
                return existingText;
            }

            var text = RemoveLeadingBackloggdLine(existingText ?? string.Empty);
            text = LeadingBreakRegex.Replace(text, string.Empty, 1);
            if (string.IsNullOrWhiteSpace(text))
            {
                return backloggdLine + Environment.NewLine;
            }

            var separator = LooksLikeHtml(text) ? "<br/><br/>" : Environment.NewLine + Environment.NewLine;
            return backloggdLine + separator + text;
        }

        private static string RemoveLeadingBackloggdLine(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return LeadingLineRegex.Replace(text, string.Empty, 1);
        }

        private static bool LooksLikeHtml(string text)
        {
            return text.IndexOf("<br", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("<p", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("</", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
