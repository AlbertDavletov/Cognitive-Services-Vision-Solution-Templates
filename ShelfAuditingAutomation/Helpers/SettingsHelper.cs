using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace ShelfAuditingAutomation.Helpers
{
    public class SettingsHelper : INotifyPropertyChanged
    {
        public static readonly string DefaultCustomVisionApiEndpoint = "https://southcentralus.api.cognitive.microsoft.com";

        public event PropertyChangedEventHandler PropertyChanged;
        public static SettingsHelper Instance { get; private set; }

        public static Func<Task> InitConfigHandler { get; set; }

        static SettingsHelper()
        {
            Instance = new SettingsHelper();
        }

        public async Task Initialize()
        {
            await LoadRoamingSettings();
            ApplicationData.Current.DataChanged += RoamingDataChanged;
        }

        private async void RoamingDataChanged(ApplicationData sender, object args)
        {
            await LoadRoamingSettings();
        }

        private async Task LoadRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["RecentlyUsedProducts"];
            if (value != null)
            {
                string valueStr = value.ToString();
                this.RecentlyUsedProducts = !string.IsNullOrEmpty(valueStr) ? JsonConvert.DeserializeObject<List<string>>(valueStr) : new List<string>();
            }

            value = ApplicationData.Current.RoamingSettings.Values["LowConfidence"];
            if (value != null)
            {
                if (double.TryParse(value.ToString(), out double lowConfidence))
                {
                    this.LowConfidence = lowConfidence;
                }
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionTrainingApiKey"];
            if (value != null)
            {
                this.CustomVisionTrainingApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionPredictionApiKey"];
            if (value != null)
            {
                this.CustomVisionPredictionApiKey = value.ToString();
            }

            value = ApplicationData.Current.RoamingSettings.Values["CustomVisionApiKeyEndpoint"];
            if (value != null)
            {
                this.CustomVisionApiKeyEndpoint = value.ToString();
            }

            // load custom settings from file as the content is too big to be saved as a string-like setting
            try
            {
                using (Stream stream = await ApplicationData.Current.RoamingFolder.OpenStreamForReadAsync("CustomConfigSettings.json"))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        this.CustomConfigSettings = await reader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception)
            {
                this.RestoreCustomConfigToDefaultFile();
            }
        }

        private double lowConfidence = 0.3;
        public double LowConfidence
        {
            get { return this.lowConfidence; }
            set
            {
                this.lowConfidence = value;
                this.OnSettingChanged("LowConfidence", value);
            }
        }

        private List<string> recentlyUsedProducts = new List<string>();
        public List<string> RecentlyUsedProducts
        {
            get { return this.recentlyUsedProducts; }
            set
            {
                this.recentlyUsedProducts = value;
                this.OnSettingChanged("RecentlyUsedProducts", JsonConvert.SerializeObject(value));
            }
        }

        private string customVisionTrainingApiKey = string.Empty;
        public string CustomVisionTrainingApiKey
        {
            get { return this.customVisionTrainingApiKey; }
            set
            {
                this.customVisionTrainingApiKey = value;
                this.OnSettingChanged("CustomVisionTrainingApiKey", value);
            }
        }

        private string customVisionPredictionApiKey = string.Empty;
        public string CustomVisionPredictionApiKey
        {
            get { return this.customVisionPredictionApiKey; }
            set
            {
                this.customVisionPredictionApiKey = value;
                this.OnSettingChanged("CustomVisionPredictionApiKey", value);
            }
        }

        private string customVisionApiKeyEndpoint = DefaultCustomVisionApiEndpoint;
        public string CustomVisionApiKeyEndpoint
        {
            get { return this.customVisionApiKeyEndpoint; }
            set
            {
                this.customVisionApiKeyEndpoint = value;
                this.OnSettingChanged("CustomVisionApiKeyEndpoint", value);
            }
        }

        private string customConfigSettings = string.Empty;
        public string CustomConfigSettings
        {
            get { return this.customConfigSettings; }
            set
            {
                this.customConfigSettings = value;
                this.OnSettingChanged("CustomConfigSettings", value);
            }
        }

        private async void OnSettingChanged(string propertyName, object value)
        {
            if (propertyName == "CustomConfigSettings")
            {
                // save to file as the content is too big to be saved as a string-like setting
                StorageFile file = await ApplicationData.Current.RoamingFolder.CreateFileAsync(
                    "CustomConfigSettings.json",
                    CreationCollisionOption.ReplaceExisting);

                using (Stream stream = await file.OpenStreamForWriteAsync())
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        await writer.WriteAsync(value.ToString());
                    }
                }
            }
            else
            {
                ApplicationData.Current.RoamingSettings.Values[propertyName] = value;
            }

            Instance.OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName)
        {
            Instance.PropertyChanged?.Invoke(Instance, new PropertyChangedEventArgs(propertyName));
        }

        public void RestoreCustomConfigToDefaultFile()
        {
            this.CustomConfigSettings = File.ReadAllText("Assets\\defaultCustomConfig.json");
        }

        public async Task PushSettingsToServices()
        {
            await (InitConfigHandler?.Invoke() ?? Task.CompletedTask);
        }
    }
}
