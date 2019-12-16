using Newtonsoft.Json;
using System.Collections.Generic;
using Windows.Storage;

namespace ShelfAuditingAutomation.Helpers
{
    public class SettingsHelper
    {
        public static SettingsHelper Instance { get; private set; }

        static SettingsHelper()
        {
            Instance = new SettingsHelper();
        }

        public void Initialize()
        {
            LoadRoamingSettings();
            ApplicationData.Current.DataChanged += RoamingDataChanged;
        }

        private void RoamingDataChanged(ApplicationData sender, object args)
        {
            LoadRoamingSettings();
        }

        private void LoadRoamingSettings()
        {
            object value = ApplicationData.Current.RoamingSettings.Values["RecentlyUsedProducts"];
            if (value != null)
            {
                string valueStr = value.ToString();
                this.RecentlyUsedProducts = !string.IsNullOrEmpty(valueStr) ? JsonConvert.DeserializeObject<List<string>>(valueStr) : new List<string>();
            }
        }

        private List<string> recentlyUsedProducts = new List<string>();
        public List<string> RecentlyUsedProducts
        {
            get { return this.recentlyUsedProducts; }
            set
            {
                this.recentlyUsedProducts = value;
                ApplicationData.Current.RoamingSettings.Values["RecentlyUsedProducts"] = JsonConvert.SerializeObject(value);
            }
        }
    }
}
