// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Annie <annieannie@anche.no>

using Playnite.SDK;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;

namespace BackloggdCommunityScore
{
    internal sealed class BackloggdClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(20);
        private static readonly Regex titleRegex = new Regex(
            "<title>\\s*(?<value>.*?)\\s*</title>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

        private readonly HttpClient httpClient;
        private readonly ConcurrentDictionary<string, BackloggdAggregateScore> scoreCache;
        private readonly ConcurrentDictionary<string, int?> titleYearCache;
        private readonly SemaphoreSlim throttleLock;

        public static BackloggdClient Shared { get; } = new BackloggdClient();

        private BackloggdClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            })
            {
                Timeout = requestTimeout
            };

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36");
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
            httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Upgrade-Insecure-Requests", "1");
            scoreCache = new ConcurrentDictionary<string, BackloggdAggregateScore>(StringComparer.OrdinalIgnoreCase);
            titleYearCache = new ConcurrentDictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
            throttleLock = new SemaphoreSlim(1, 1);
        }

        public bool TryGetAggregateScore(string backloggdGameUrl, out BackloggdAggregateScore score, out string error)
        {
            return TryGetAggregateScoreWithTitleYear(backloggdGameUrl, out score, out _, out error);
        }

        public bool TryGetAggregateScoreWithTitleYear(string backloggdGameUrl, out BackloggdAggregateScore score, out int? titleYear, out string error)
        {
            score = null;
            titleYear = null;
            error = null;

            if (string.IsNullOrWhiteSpace(backloggdGameUrl))
            {
                error = "Backloggd URL is empty.";
                return false;
            }

            if (scoreCache.TryGetValue(backloggdGameUrl, out score))
            {
                titleYearCache.TryGetValue(backloggdGameUrl, out titleYear);
                return true;
            }

            var lockTaken = false;

            try
            {
                throttleLock.Wait();
                lockTaken = true;

                if (scoreCache.TryGetValue(backloggdGameUrl, out score))
                {
                    titleYearCache.TryGetValue(backloggdGameUrl, out titleYear);
                    return true;
                }

                using (var request = new HttpRequestMessage(HttpMethod.Get, backloggdGameUrl))
                {
                    request.Headers.Referrer = new Uri("https://backloggd.com/");

                    using (var response = httpClient.SendAsync(request).GetAwaiter().GetResult())
                    {
                        var html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? backloggdGameUrl;
                        if (!response.IsSuccessStatusCode)
                        {
                            error = $"Backloggd HTTP {(int)response.StatusCode} ({response.ReasonPhrase}) at '{finalUrl}'.";
                            return false;
                        }

                        if (!BackloggdHtmlParser.TryParseAggregateScore(html, out score, out error))
                        {
                            error = $"{error} {DescribeHtml(html, finalUrl)}";
                            return false;
                        }

                        BackloggdHtmlParser.TryParseTitleYear(html, out titleYear);

                        scoreCache[backloggdGameUrl] = score;
                        titleYearCache[backloggdGameUrl] = titleYear;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Backloggd request failed for '{backloggdGameUrl}'.");
                error = ex.Message;
                score = null;
                titleYear = null;
                return false;
            }
            finally
            {
                if (lockTaken)
                {
                    throttleLock.Release();
                }
            }
        }

        private static string DescribeHtml(string html, string finalUrl)
        {
            var title = "<missing>";
            var titleMatch = titleRegex.Match(html ?? string.Empty);
            if (titleMatch.Success)
            {
                title = WebUtility.HtmlDecode(titleMatch.Groups["value"].Value).Trim();
            }

            var hasJsonLd = Contains(html, "application/ld+json");
            var hasAggregateType = Contains(html, "\"@type\": \"AggregateRating\"");
            var hasVisibleScore = Contains(html, "id=\"game-rating\"");
            var hasBunnyShield = Contains(html, "Establishing a secure connection")
                || Contains(html, "bunny-shield")
                || Contains(html, "Hold tight");

            return string.Format(
                "FinalUrl='{0}', Title='{1}', JsonLd={2}, AggregateType={3}, GameRating={4}, BunnyShieldLike={5}.",
                finalUrl,
                title,
                hasJsonLd,
                hasAggregateType,
                hasVisibleScore,
                hasBunnyShield);
        }

        private static bool Contains(string value, string fragment)
        {
            return (value ?? string.Empty).IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
