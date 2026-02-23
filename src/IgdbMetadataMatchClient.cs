// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Annie <annieannie@anche.no>
// Trans rights are human rights

using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Text;

namespace BackloggdCommunityScore
{
    internal sealed class IgdbMetadataMatchClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly Uri baseUri = new Uri("https://api2.playnite.link/api/");
        private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(20);
        private const string NoUrlSentinel = "<none>";

        private readonly HttpClient httpClient;
        private readonly ConcurrentDictionary<string, string> urlCache;

        public static IgdbMetadataMatchClient Shared { get; } = new IgdbMetadataMatchClient();

        private IgdbMetadataMatchClient()
        {
            httpClient = new HttpClient { BaseAddress = baseUri, Timeout = requestTimeout };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Playnite-BackloggdCommunityScore/0.1");
            urlCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetIgdbGameUrl(Game game, out string igdbGameUrl, out string error)
        {
            igdbGameUrl = null;
            error = null;

            if (string.IsNullOrWhiteSpace(game?.Name))
            {
                error = "Game name is empty.";
                return false;
            }

            var cacheKey = BuildCacheKey(game);
            if (urlCache.TryGetValue(cacheKey, out var cachedValue))
            {
                if (cachedValue == NoUrlSentinel)
                {
                    error = "IGDB metadata cache has no URL for this game.";
                    return false;
                }

                igdbGameUrl = cachedValue;
                return true;
            }

            try
            {
                var request = new IgdbMetadataRequest
                {
                    Name = game.Name,
                    ReleaseYear = game.ReleaseYear ?? 0,
                    LibraryId = game.PluginId == Guid.Empty ? null : game.PluginId.ToString(),
                    GameId = game.GameId
                };

                var requestBody = Serialization.ToJson(request);
                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = httpClient.PostAsync("igdb/metadata", content).GetAwaiter().GetResult();
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    error = $"IGDB metadata HTTP {(int)response.StatusCode}.";
                    logger.Warn($"IGDB metadata request failed for '{game.Name}': {error}");
                    urlCache[cacheKey] = NoUrlSentinel;
                    return false;
                }

                var parsed = Serialization.FromJson<IgdbMetadataResponse>(responseBody);
                igdbGameUrl = parsed?.Data?.Url;

                if (string.IsNullOrWhiteSpace(igdbGameUrl))
                {
                    error = "IGDB metadata did not return a game URL.";
                    urlCache[cacheKey] = NoUrlSentinel;
                    return false;
                }

                urlCache[cacheKey] = igdbGameUrl;
                return true;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"IGDB metadata request failed for '{game.Name}'.");
                error = ex.Message;
                urlCache[cacheKey] = NoUrlSentinel;
                return false;
            }
        }

        private static string BuildCacheKey(Game game)
        {
            return string.Format(
                "{0}|{1}|{2}|{3}",
                game.Name?.Trim() ?? string.Empty,
                game.ReleaseYear ?? 0,
                game.PluginId,
                game.GameId ?? string.Empty);
        }

        private sealed class IgdbMetadataRequest
        {
            public string LibraryId { get; set; }
            public string GameId { get; set; }
            public string Name { get; set; }
            public int ReleaseYear { get; set; }
        }

        private sealed class IgdbMetadataResponse
        {
            public IgdbMetadataGame Data { get; set; }
        }

        private sealed class IgdbMetadataGame
        {
            public string Url { get; set; }
        }
    }
}
