using ShelfAuditingAutomation.Models;
using System;
using System.Collections.ObjectModel;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ShelfAuditingAutomation.Views
{
    public sealed partial class InputView : UserControl
    {
        public static readonly DependencyProperty SpecsDataCollectionProperty =
            DependencyProperty.Register(
                "SpecsDataCollection",
                typeof(ObservableCollection<SpecsData>),
                typeof(InputView),
                new PropertyMetadata(null));

        public ObservableCollection<SpecsData> SpecsDataCollection
        {
            get { return (ObservableCollection<SpecsData>)GetValue(SpecsDataCollectionProperty); }
            set { SetValue(SpecsDataCollectionProperty, value); }
        }

        public event EventHandler<Tuple<SpecsData, StorageFile>> ImageSelected;

        public InputView()
        {
            this.InitializeComponent();

            // default sample images
            this.imagePicker.SetSuggestedImageList(
                "ms-appx:///Assets/ImageSamples/1.jpg",
                "ms-appx:///Assets/ImageSamples/2.jpg",
                "ms-appx:///Assets/ImageSamples/3.jpg",
                "ms-appx:///Assets/ImageSamples/4.jpg"
            );
        }

        private void OnImageSearchCompleted(object sender, StorageFile imageFile)
        {
            var specsData = this.projectsComboBox.SelectedItem as SpecsData;
            this.ImageSelected?.Invoke(this, new Tuple<SpecsData, StorageFile>(specsData, imageFile));
        }

        private void ProjectsComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.projectsComboBox.SelectedItem is SpecsData specsData)
            {
                this.imagePicker.SetSuggestedImageList(specsData.SampleImages);
            }
        }
    }
}
