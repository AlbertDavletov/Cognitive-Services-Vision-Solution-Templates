using ShelfAuditingAutomation.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ShelfAuditingAutomation.Views
{
    public sealed partial class InputView : UserControl
    {
        private static readonly string[] DefaultImageList = new string[] 
        {
            "ms-appx:///Assets/ImageSamples/1.jpg",
            "ms-appx:///Assets/ImageSamples/2.jpg",
            "ms-appx:///Assets/ImageSamples/3.jpg",
            "ms-appx:///Assets/ImageSamples/4.jpg"
        };

        public ObservableCollection<SpecsData> SpecsDataCollection { get; set; } = new ObservableCollection<SpecsData>();

        public event EventHandler<Tuple<SpecsData, StorageFile>> ImageSelected;

        public InputView()
        {
            this.InitializeComponent();

            // default sample images
            this.imagePicker.SetSuggestedImageList(DefaultImageList);
        }

        public void SetDataSource(List<SpecsData> data = null)
        {
            SpecsDataCollection.Clear();
            if (data != null)
            {
                SpecsDataCollection.AddRange(data);
                this.projectsComboBox.SelectedIndex = 0;
            }
        }

        private void OnImageSearchCompleted(object sender, StorageFile imageFile)
        {
            if (this.projectsComboBox.SelectedItem is SpecsData specsData)
            {
                this.ImageSelected?.Invoke(this, new Tuple<SpecsData, StorageFile>(specsData, imageFile));
            }
        }

        private void ProjectsComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.projectsComboBox.SelectedItem is SpecsData specsData)
            {
                this.imagePicker.SetSuggestedImageList(specsData.SampleImages);
            }
            else
            {
                this.imagePicker.SetSuggestedImageList(DefaultImageList);
            }
        }
    }
}
