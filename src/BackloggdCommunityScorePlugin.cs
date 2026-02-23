// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Annie <annieannie@anche.no>
// Trans rights are human rights

using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace BackloggdCommunityScore
{
    public class BackloggdCommunityScorePlugin : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly BackloggdCommunityScoreSettingsViewModel settings;

        public override Guid Id { get; } = Guid.Parse("05106d44-f505-4fd9-9e57-cc8cc737f3a9");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.CommunityScore,
            MetadataField.Links,
            MetadataField.Description
        };

        public override string Name => "Backloggd Community Score";
        internal BackloggdCommunityScoreSettings Settings => settings.Settings;

        public BackloggdCommunityScorePlugin(IPlayniteAPI api) : base(api)
        {
            settings = new BackloggdCommunityScoreSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
            logger.Info("Backloggd Community Score loaded.");
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new BackloggdCommunityScoreSettingsView(settings);
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new BackloggdCommunityScoreProvider(options, this);
        }

        internal void RewriteRatingCountForAllGames()
        {
            RewriteRatingCount(PlayniteApi.Database.Games.ToList(), "all games");
        }

        internal void RewriteRatingCountForSelectedGames()
        {
            var selectedGames = PlayniteApi.MainView.SelectedGames?.ToList() ?? new List<Playnite.SDK.Models.Game>();
            RewriteRatingCount(selectedGames, "selected games");
        }

        private void RewriteRatingCount(List<Playnite.SDK.Models.Game> games, string scopeLabel)
        {
            if (!Settings.WriteRatingCountToDescription && !Settings.WriteRatingCountToNotes)
            {
                PlayniteApi.Dialogs.ShowMessage(
                    "Enable at least one destination in settings before rewriting (Description and/or Notes).",
                    "Backloggd Community Score");
                return;
            }

            if (!games.HasItems())
            {
                PlayniteApi.Dialogs.ShowMessage(
                    $"No {scopeLabel} found.",
                    "Backloggd Community Score");
                return;
            }

            var progressResult = PlayniteApi.Dialogs.ActivateGlobalProgress(
                args =>
                {
                    args.ProgressMaxValue = games.Count;
                    args.CurrentProgressValue = 0;
                    args.IsIndeterminate = false;

                    using (PlayniteApi.Database.BufferedUpdate())
                    {
                        for (var i = 0; i < games.Count; i++)
                        {
                            if (args.CancelToken.IsCancellationRequested)
                            {
                                return;
                            }

                            var sourceGame = games[i];
                            var game = PlayniteApi.Database.Games[sourceGame.Id];
                            if (game == null)
                            {
                                continue;
                            }

                            args.Text = $"Updating {i + 1}/{games.Count}: {game.Name}";
                            args.CurrentProgressValue = i + 1;

                            if (!BackloggdScoreLookup.TryGetAggregateScore(game, out var score, out _, out _))
                            {
                                continue;
                            }

                            var ratingLine = BackloggdRatingCountFormatter.BuildRatingCountLine(score.RatingCount);
                            if (string.IsNullOrWhiteSpace(ratingLine))
                            {
                                continue;
                            }

                            var changed = false;

                            if (Settings.WriteRatingCountToDescription)
                            {
                                var updatedDescription = BackloggdRatingCountFormatter.UpsertLineAtTop(game.Description, ratingLine);
                                if (!string.Equals(game.Description, updatedDescription, StringComparison.Ordinal))
                                {
                                    game.Description = updatedDescription;
                                    changed = true;
                                }
                            }

                            if (Settings.WriteRatingCountToNotes)
                            {
                                var updatedNotes = BackloggdRatingCountFormatter.UpsertLineAtTop(game.Notes, ratingLine);
                                if (!string.Equals(game.Notes, updatedNotes, StringComparison.Ordinal))
                                {
                                    game.Notes = updatedNotes;
                                    changed = true;
                                }
                            }

                            if (changed)
                            {
                                PlayniteApi.Database.Games.Update(game);
                            }
                        }
                    }
                },
                new GlobalProgressOptions("Re-writing Backloggd rating count...", true));

            if (progressResult?.Error != null)
            {
                logger.Error(progressResult.Error, "Failed to re-write Backloggd rating count.");
                PlayniteApi.Dialogs.ShowErrorMessage(
                    $"Re-write failed: {progressResult.Error.Message}",
                    "Backloggd Community Score");
                return;
            }

            if (progressResult?.Canceled == true)
            {
                PlayniteApi.Dialogs.ShowMessage("Re-write canceled.", "Backloggd Community Score");
                return;
            }

            PlayniteApi.Dialogs.ShowMessage("Re-write finished.", "Backloggd Community Score");
        }
    }
}
