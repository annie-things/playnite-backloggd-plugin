using Playnite.SDK.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BackloggdCommunityScore
{
    internal static class BackloggdUrlResolver
    {
        public static string Resolve(Game game)
        {
            if (game == null)
            {
                return null;
            }

            if (game.Links != null && game.Links.Count > 0)
            {
                var directBackloggdLink = game.Links
                    .Select(link => link?.Url)
                    .FirstOrDefault(url => TryNormalizeBackloggdGameUrl(url, out _));

                if (TryNormalizeBackloggdGameUrl(directBackloggdLink, out var normalizedBackloggdUrl))
                {
                    return normalizedBackloggdUrl;
                }

                var igdbLink = game.Links.Select(link => link?.Url).FirstOrDefault(IsIgdbUrl);
                if (TryConvertIgdbUrlToBackloggd(igdbLink, out var convertedBackloggdUrl))
                {
                    return convertedBackloggdUrl;
                }
            }

            if (IgdbMetadataMatchClient.Shared.TryGetIgdbGameUrl(game, out var igdbGameUrl, out _)
                && TryConvertIgdbUrlToBackloggd(igdbGameUrl, out var backendMatchedBackloggdUrl))
            {
                return backendMatchedBackloggdUrl;
            }

            if (TryBuildNameBasedBackloggdUrl(game.Name, out var nameBasedUrl))
            {
                return nameBasedUrl;
            }

            return null;
        }

        private static bool IsIgdbUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return false;
            }

            return uri.Host.Equals("igdb.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("www.igdb.com", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryNormalizeBackloggdGameUrl(string url, out string normalizedUrl)
        {
            normalizedUrl = null;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
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

            normalizedUrl = $"https://www.backloggd.com/games/{slug}/";
            return true;
        }

        private static bool TryConvertIgdbUrlToBackloggd(string igdbUrl, out string backloggdUrl)
        {
            backloggdUrl = null;

            if (!Uri.TryCreate(igdbUrl, UriKind.Absolute, out var uri))
            {
                return false;
            }

            if (!IsIgdbUrl(igdbUrl))
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

            backloggdUrl = $"https://www.backloggd.com/games/{slug}/";
            return true;
        }

        private static bool TryBuildNameBasedBackloggdUrl(string gameName, out string backloggdUrl)
        {
            backloggdUrl = null;

            if (string.IsNullOrWhiteSpace(gameName))
            {
                return false;
            }

            var slug = BuildSlug(gameName);
            if (string.IsNullOrWhiteSpace(slug))
            {
                return false;
            }

            backloggdUrl = $"https://www.backloggd.com/games/{slug}/";
            return true;
        }

        private static string BuildSlug(string source)
        {
            var normalized = source
                .Trim()
                .ToLowerInvariant()
                .Normalize(NormalizationForm.FormD);

            var slugBuilder = new StringBuilder(normalized.Length);
            var previousWasHyphen = false;

            foreach (var c in normalized)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(c))
                {
                    slugBuilder.Append(c);
                    previousWasHyphen = false;
                    continue;
                }

                if (c == '&')
                {
                    if (!previousWasHyphen && slugBuilder.Length > 0)
                    {
                        slugBuilder.Append('-');
                    }

                    slugBuilder.Append("and");
                    previousWasHyphen = false;
                    continue;
                }

                if (!previousWasHyphen && slugBuilder.Length > 0)
                {
                    slugBuilder.Append('-');
                    previousWasHyphen = true;
                }
            }

            var slug = slugBuilder.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? null : slug;
        }
    }
}
