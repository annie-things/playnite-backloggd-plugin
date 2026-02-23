// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 Annie <annieannie@anche.no>
// Trans rights are human rights

using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace BackloggdCommunityScore
{
    public class BackloggdCommunityScoreSettingsView : UserControl
    {
        public BackloggdCommunityScoreSettingsView(BackloggdCommunityScoreSettingsViewModel viewModel)
        {
            DataContext = viewModel;

            var descriptionCheck = new CheckBox
            {
                Content = "Write Backloggd rating count at the top of the game description",
                Margin = new Thickness(0, 0, 0, 8)
            };
            descriptionCheck.SetBinding(ToggleButton.IsCheckedProperty, new Binding("Settings.WriteRatingCountToDescription")
            {
                Mode = BindingMode.TwoWay
            });

            var notesCheck = new CheckBox
            {
                Content = "Write Backloggd rating count to game notes (Play Notes plugin recommended for viewing)",
                Margin = new Thickness(0, 0, 0, 16)
            };
            notesCheck.SetBinding(ToggleButton.IsCheckedProperty, new Binding("Settings.WriteRatingCountToNotes")
            {
                Mode = BindingMode.TwoWay
            });

            var rewriteLabel = new TextBlock
            {
                Text = "Re-write Backloggd rating count to game descriptions/notes:",
                Margin = new Thickness(0, 0, 0, 8)
            };

            var rewriteAllButton = new Button
            {
                Content = "All games",
                Margin = new Thickness(0, 0, 8, 0),
                MinWidth = 120
            };
            rewriteAllButton.Click += (_, __) => viewModel.RewriteAllGames();

            var rewriteSelectedButton = new Button
            {
                Content = "Selected games",
                MinWidth = 140
            };
            rewriteSelectedButton.Click += (_, __) => viewModel.RewriteSelectedGames();

            var buttonRow = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };
            buttonRow.Children.Add(rewriteAllButton);
            buttonRow.Children.Add(rewriteSelectedButton);

            var panel = new StackPanel
            {
                Margin = new Thickness(20)
            };
            panel.Children.Add(descriptionCheck);
            panel.Children.Add(notesCheck);
            panel.Children.Add(rewriteLabel);
            panel.Children.Add(buttonRow);

            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = panel
            };
        }
    }
}
