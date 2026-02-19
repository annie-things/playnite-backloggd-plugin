using Playnite.SDK;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace BackloggdCommunityScore
{
    internal sealed class BackloggdClient
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private static readonly TimeSpan requestTimeout = TimeSpan.FromSeconds(20);

        private readonly HttpClient httpClient;
        private readonly ConcurrentDictionary<string, BackloggdAggregateScore> scoreCache;
        private readonly SemaphoreSlim throttleLock;

        public static BackloggdClient Shared { get; } = new BackloggdClient();

        private BackloggdClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            httpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            {
                Timeout = requestTimeout
            };

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Playnite-BackloggdCommunityScore/0.1");
            scoreCache = new ConcurrentDictionary<string, BackloggdAggregateScore>(StringComparer.OrdinalIgnoreCase);
            throttleLock = new SemaphoreSlim(1, 1);
        }

        public bool TryGetAggregateScore(string backloggdGameUrl, out BackloggdAggregateScore score, out string error)
        {
            score = null;
            error = null;

            if (string.IsNullOrWhiteSpace(backloggdGameUrl))
            {
                error = "Backloggd URL is empty.";
                return false;
            }

            if (scoreCache.TryGetValue(backloggdGameUrl, out score))
            {
                return true;
            }

            var lockTaken = false;

            try
            {
                throttleLock.Wait();
                lockTaken = true;

                // Double-check after acquiring the lock to avoid duplicate requests.
                if (scoreCache.TryGetValue(backloggdGameUrl, out score))
                {
                    return true;
                }

                var html = httpClient.GetStringAsync(backloggdGameUrl).GetAwaiter().GetResult();

                if (!BackloggdHtmlParser.TryParseAggregateScore(html, out score, out error))
                {
                    return false;
                }

                scoreCache[backloggdGameUrl] = score;
                return true;
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Backloggd request failed for '{backloggdGameUrl}'.");
                error = ex.Message;
                score = null;
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
    }
}
