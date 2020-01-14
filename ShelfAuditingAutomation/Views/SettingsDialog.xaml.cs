using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using ShelfAuditingAutomation.Helpers;
using System;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ShelfAuditingAutomation.Views
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
            this.DataContext = SettingsHelper.Instance;
        }

        private async void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            bool hasError = !ValidateSettings();
            if (hasError)
            {
                return;
            }
            
            // validate key
            try
            {
                //custom vision training
                var client = new CustomVisionTrainingClient { Endpoint = CustomVisionEndpoint.Text, ApiKey = CustomVisionTrainingApi.Password };
                await client.GetDomainsAsync();
            }
            catch (Exception ex)
            {
                CustomVisionTrainingApiError.Text = Util.GetMessageFromException(ex);
                CustomVisionTrainingApiError.Visibility = Visibility.Visible;
                hasError = true;
            }

            // validate config
            bool isValidConfig = CustomSpecsDataLoader.ValidateData(CustomConfigSettings.Text);
            if (!isValidConfig)
            {
                ConfigError.Text = "Failure validating your Config file. Please make sure your config file has correct format and valid data.";
                ConfigError.Visibility = Visibility.Visible;
                hasError = true;
            }

            // save settings
            if (!hasError)
            {
                SettingsHelper.Instance.CustomVisionTrainingApiKey = CustomVisionTrainingApi.Password;
                SettingsHelper.Instance.CustomVisionPredictionApiKey = CustomVisionPredictionApi.Password;
                SettingsHelper.Instance.CustomVisionApiKeyEndpoint = CustomVisionEndpoint.Text;
                await SettingsHelper.Instance.PushSettingsToServices();

                args.Cancel = false;
                Hide();
            }
        }

        private async void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await SettingsHelper.Instance.PushSettingsToServices();
        }

        private async void OnGetConfigFromFileButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                StorageFile file = await Util.PickSingleFileAsync(new string[] { ".json" });
                string fileContent = await FileIO.ReadTextAsync(file);
                SettingsHelper.Instance.CustomConfigSettings = fileContent;
            }
            catch (Exception ex)
            {
                await Util.GenericApiCallExceptionHandler(ex, "Failure processing local file");
            }
        }

        private void ResetConfigSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            SettingsHelper.Instance.RestoreCustomConfigToDefaultFile();
        }

        private bool ValidateSettings()
        {
            bool isValidSettings = true;

            CustomVisionTrainingApiError.Visibility = string.IsNullOrWhiteSpace(CustomVisionTrainingApi.Password) ? Visibility.Visible : Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(CustomVisionTrainingApi.Password))
            {
                CustomVisionTrainingApiError.Text = Util.RequiredFieldMessage;
                isValidSettings = false;
            }

            CustomVisionPredictionApiError.Visibility = string.IsNullOrWhiteSpace(CustomVisionPredictionApi.Password) ? Visibility.Visible : Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(CustomVisionPredictionApi.Password))
            {
                CustomVisionPredictionApiError.Text = Util.RequiredFieldMessage;
                isValidSettings = false;
            }

            CustomVisionEndpointError.Visibility = string.IsNullOrWhiteSpace(CustomVisionEndpoint.Text) ? Visibility.Visible : Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(CustomVisionEndpoint.Text))
            {
                CustomVisionEndpointError.Text = Util.RequiredFieldMessage;
                isValidSettings = false;
            }

            ConfigError.Visibility = string.IsNullOrWhiteSpace(ConfigError.Text) ? Visibility.Visible : Visibility.Collapsed;
            if (string.IsNullOrWhiteSpace(CustomConfigSettings.Text))
            {
                ConfigError.Text = Util.RequiredFieldMessage;
                isValidSettings = false;
            }

            return isValidSettings;
        }
    }
}
