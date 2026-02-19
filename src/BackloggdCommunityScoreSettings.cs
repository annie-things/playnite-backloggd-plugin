using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace BackloggdCommunityScore
{
    public class BackloggdCommunityScoreSettings : ObservableObject
    {
        private bool writeRatingCountToDescription = true;
        private bool writeRatingCountToNotes = false;

        public bool WriteRatingCountToDescription
        {
            get => writeRatingCountToDescription;
            set => SetValue(ref writeRatingCountToDescription, value);
        }

        public bool WriteRatingCountToNotes
        {
            get => writeRatingCountToNotes;
            set => SetValue(ref writeRatingCountToNotes, value);
        }
    }

    public class BackloggdCommunityScoreSettingsViewModel : ObservableObject, ISettings
    {
        private readonly BackloggdCommunityScorePlugin plugin;
        private BackloggdCommunityScoreSettings editingClone;

        private BackloggdCommunityScoreSettings settings;
        public BackloggdCommunityScoreSettings Settings
        {
            get => settings;
            set
            {
                settings = value;
                OnPropertyChanged();
            }
        }

        public BackloggdCommunityScoreSettingsViewModel(BackloggdCommunityScorePlugin plugin)
        {
            this.plugin = plugin;
            Settings = plugin.LoadPluginSettings<BackloggdCommunityScoreSettings>() ?? new BackloggdCommunityScoreSettings();
        }

        public void RewriteAllGames()
        {
            plugin.RewriteRatingCountForAllGames();
        }

        public void RewriteSelectedGames()
        {
            plugin.RewriteRatingCountForSelectedGames();
        }

        public void BeginEdit()
        {
            editingClone = Serialization.GetClone(Settings);
        }

        public void CancelEdit()
        {
            Settings = editingClone;
        }

        public void EndEdit()
        {
            plugin.SavePluginSettings(Settings);
        }

        public bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            return true;
        }
    }
}
