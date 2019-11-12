using ObjectCountingExplorer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ObjectCountingExplorer.Controls
{
    public sealed partial class InitialView : UserControl
    {
        public static readonly DependencyProperty ProjectsProperty =
            DependencyProperty.Register(
                "Projects",
                typeof(ObservableCollection<ProjectViewModel>),
                typeof(InitialView),
                new PropertyMetadata(null));

        public ObservableCollection<ProjectViewModel> Projects
        {
            get { return (ObservableCollection<ProjectViewModel>)GetValue(ProjectsProperty); }
            set { SetValue(ProjectsProperty, value); }
        }

        public event EventHandler<Tuple<ProjectViewModel, StorageFile>> ImageSelected;

        public InitialView()
        {
            this.InitializeComponent();

            this.imagePicker.SetSuggestedImageList(
                "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/1.jpg",
                "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/2.jpg",
                "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/3.jpg",
                "https://howoldkiosk.blob.core.windows.net/kiosksuggestedphotos/4.jpg"
            );
        }

        private void OnImageSearchCompleted(object sender, StorageFile imageFile)
        {
            var project = this.projectsComboBox.SelectedItem as ProjectViewModel;
            this.ImageSelected?.Invoke(this, new Tuple<ProjectViewModel, StorageFile>(project, imageFile));
        }
    }
}
