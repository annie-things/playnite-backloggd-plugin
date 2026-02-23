// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Annie <annieannie@anche.no>
// Trans rights are human rights

using Playnite.SDK.Models;

namespace BackloggdCommunityScore
{
    internal static class BackloggdScoreLookup
    {
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

            return BackloggdClient.Shared.TryGetAggregateScore(backloggdGameUrl, out score, out error);
        }
    }
}
