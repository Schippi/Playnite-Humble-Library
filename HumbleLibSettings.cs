using Newtonsoft.Json;
using Playnite.SDK;
using System.Collections.Generic;
using System.Text;
using System;

namespace humble
{
    public class HumbleLibSettings : ObservableObject, ISettings
    {
        #region Settings
        public string GamesLocation { get; set; } = string.Empty;

        public string LoginToken { get; set; } = string.Empty;

        private static ILogger logger = LogManager.GetLogger();

        public bool AlwaysScanEverything { get; set; } = false;

        #endregion Settings

        [JsonIgnore]
        public RelayCommand<object> LoginCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                Login();
            });
        }

        [JsonIgnore]
        public bool IsUserLoggedIn
        {
            get
            {
                using (var view = api.WebViews.CreateOffscreenView())
                {
                    var api = new HumbleAccountClient(view);
                    return api.GetIsUserLoggedIn();
                }
            }
        }

        private HumbleLibSettings editingClone;
        private HumbleGameLibrary library;
        private IPlayniteAPI api;

        private void Login()
        {
            try
            {
                using (var view = api.WebViews.CreateView(490, 670))
                {
                    var api = new HumbleAccountClient(view);
                    api.Login();
                }

                OnPropertyChanged(nameof(IsUserLoggedIn));
            }
            catch (Exception e)// when (!Environment.IsDebugBuild)
            {
                logger.Error(e, "Failed to authenticate user.");
            }
        }

        public HumbleLibSettings(HumbleGameLibrary library, IPlayniteAPI api) : base()
        {
            this.api = api;
            this.library = library;
            if (library is null)
            {
                //System.IO.File.AppendAllText("C:/TEMP/games.txt", System.Environment.StackTrace+"\n", Encoding.UTF8);
            }
            else
            {
                try
                {
                    var settings = library.LoadPluginSettings<HumbleLibSettings>();
                    LoadValues(settings);
                }
                catch (Exception e)
                {

                    logger.Info($"The file was not found: '{e}'");
                }
            }
        }

        public HumbleLibSettings GetClone()
        {
            var result = new HumbleLibSettings(library, api);
            result.LoadValues(this);
            return result;
        }

        public void BeginEdit()
        {
            // Code executed when settings view is opened and user starts editing values.
            editingClone = this.GetClone();
        }

        public void CancelEdit()
        {
            // Code executed when user decides to cancel any changes made since BeginEdit was called.
            // This method should revert any changes made to Option1 and Option2.
            LoadValues(editingClone);
        }

        private void LoadValues(HumbleLibSettings source)
        {
            this.GamesLocation = source.GamesLocation;
            this.LoginToken = source.LoginToken;
        }

        public void EndEdit()
        {
            // Code executed when user decides to confirm changes made since BeginEdit was called.
            // This method should save settings made to Option1 and Option2.
            library.SavePluginSettings(this);
        }

        public bool VerifySettings(out List<string> errors)
        {
            // Code execute when user decides to confirm changes made since BeginEdit was called.
            // Executed before EndEdit is called and EndEdit is not called if false is returned.
            // List of errors is presented to user if verification fails.
            errors = new List<string>();
            return true;
        }
    }
}