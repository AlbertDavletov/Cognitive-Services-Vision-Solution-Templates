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
        public static readonly DependencyProperty ProjectsProperty =
            DependencyProperty.Register(
                "Projects",
                typeof(ObservableCollection<ProjectViewModel>),
                typeof(InputView),
                new PropertyMetadata(null));

        public ObservableCollection<ProjectViewModel> Projects
        {
            get { return (ObservableCollection<ProjectViewModel>)GetValue(ProjectsProperty); }
            set { SetValue(ProjectsProperty, value); }
        }

        public event EventHandler<Tuple<ProjectViewModel, StorageFile>> ImageSelected;

        public InputView()
        {
            this.InitializeComponent();

            this.imagePicker.SetSuggestedImageList(
                "ms-appx:///Assets/ImageSamples/1.jpg",
                "ms-appx:///Assets/ImageSamples/2.jpg",
                "ms-appx:///Assets/ImageSamples/3.jpg",
                "ms-appx:///Assets/ImageSamples/4.jpg"
            );
        }

        private void OnImageSearchCompleted(object sender, StorageFile imageFile)
        {
            var project = this.projectsComboBox.SelectedItem as ProjectViewModel;
            this.ImageSelected?.Invoke(this, new Tuple<ProjectViewModel, StorageFile>(project, imageFile));
        }
    }
}
