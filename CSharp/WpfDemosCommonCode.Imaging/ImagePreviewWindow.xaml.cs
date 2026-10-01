using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

using Vintasoft.Imaging;
using Vintasoft.Imaging.UI;
using Vintasoft.Imaging.Wpf.UI;

namespace WpfCommonCode.Imaging
{
    /// <summary>
    /// Interaction logic for ImagePreviewWindow.xaml
    /// </summary>
    public partial class ImagePreviewWindow : Window
    {

        #region Fields

        /// <summary>
        /// The last folder path.
        /// </summary>
        static string LastFolderPath = Environment.CurrentDirectory;

        /// <summary>
        /// Manages asynchronous operations of an image viewer images.
        /// </summary>
        WpfImageViewerImagesManager _imagesManager;

        #endregion



        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ImagePreviewWindow"/> class.
        /// </summary>
        public ImagePreviewWindow()
        {
            InitializeComponent();

            _imagesManager = new WpfImageViewerImagesManager(thumbnailViewer);

            DocumentPasswordWindow.EnableAuthentication(thumbnailViewer);

            FolderPath = LastFolderPath;

            DestImagesManager = null;

            thumbnailViewer.ThumbnailCaption.IsVisible = true;
            thumbnailViewer.ThumbnailCaption.CaptionFormat = "Page {PageNumber}";
            thumbnailViewer.ThumbnailCaption.Padding = new Thickness(4);
            thumbnailViewer.SelectedThumbnails.Changed += SelectedThumbnails_Changed;
            thumbnailViewer.Images.ImageCollectionChanged += thumbnailViewer_Images_ImageCollectionChanged;

            addSelectedButton.IsEnabled = false;
        }

        #endregion



        #region Properties

        /// <summary>
        /// Gets or sets the path of the directory to watch thumnails.
        /// </summary>
        [Browsable(false)]
        public string FolderPath
        {
            get
            {
                return folderThumbnailViewer.FolderPath;
            }
            set
            {
                folderThumbnailViewer.FolderPath = value;
                folderPathTextBox.Text = value;
                LastFolderPath = value;
            }
        }

        ImageCollectionManager _destImagesManager = null;
        /// <summary>
        /// Gets or sets the destination images manager, that uses to add images.
        /// </summary>
        [Browsable(false)]
        public ImageCollectionManager DestImagesManager
        {
            get
            {
                return _destImagesManager;
            }
            set
            {
                _destImagesManager = value;
                Visibility visibility = Visibility.Collapsed;
                if(value!=null)
                    visibility = Visibility.Visible;
                addAllButton.Visibility = visibility;
                addSelectedButton.Visibility = visibility;
                if (value != null)
                {
                    // copy layout settings
                    value.Images.LayoutSettings.CopyTo(folderThumbnailViewer.Images.LayoutSettings);
                    value.Images.LayoutSettings.CopyTo(thumbnailViewer.Images.LayoutSettings);
                }
            }
        }

        #endregion



        #region Methods

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (!e.Cancel)
            {
                _imagesManager.Cancel();
                folderThumbnailViewer.FolderPath = null;
            }
        }

        /// <summary>
        /// Changes folder path in FolderThumbnailViewer.
        /// </summary>
        private void changeFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.SelectedPath = FolderPath;
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                FolderPath = folderBrowserDialog.SelectedPath;
        }

        /// <summary>
        /// Sets the focused image file.
        /// </summary>
        private void folderThumbnailViewer_FocusedIndexChanged(object sender, PropertyChangedEventArgs<int> e)
        {
            if (e.OldValue != e.NewValue)
            {
                _imagesManager.Cancel();
                _imagesManager.Images.ClearAndDisposeItems();
                WpfFolderThumbnailViewer.FileThumbnailInfo fileThumbnailInfo = folderThumbnailViewer.FocusedFileThumbnailInfo;
                if (fileThumbnailInfo != null)
                {
                    if (fileThumbnailInfo.LoadingError != null)
                    {
                        DemosTools.ShowErrorMessage(fileThumbnailInfo.LoadingError);
                    }
                    else
                    {
                        _imagesManager.Add(fileThumbnailInfo.Filename, true);
                    }
                }
            }
        }

        /// <summary>
        /// Adds all pages to destiation image collection manager.
        /// </summary>
        private void addAllButton_Click(object sender, RoutedEventArgs e)
        {
            DestImagesManager.Add(folderThumbnailViewer.FocusedFilename);
        }

        /// <summary>
        /// Adds selected pages to destiation image collection manager.
        /// </summary>
        private void addSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (VintasoftImage image in thumbnailViewer.GetSelectedImages())
                DestImagesManager.Images.Add(new VintasoftImage(image.SourceInfo.Decoder, image.SourceInfo.PageIndex));
        }

        /// <summary>
        /// Handles the Click event of closeButton object.
        /// </summary>
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// Handles the Changed event of SelectedThumbnails object.
        /// </summary>
        private void SelectedThumbnails_Changed(object sender, EventArgs e)
        {
            addSelectedButton.IsEnabled = thumbnailViewer.SelectedThumbnails.Count > 0;
        }

        /// <summary>
        /// Handles the Images_ImageCollectionChanged event of thumbnailViewer object.
        /// </summary>
        private void thumbnailViewer_Images_ImageCollectionChanged(object sender, ImageCollectionChangeEventArgs e)
        {
            if (thumbnailViewer.InvokeRequired)
                Dispatcher.Invoke(new ThreadStart(UpdateFilenameText));
            else
                UpdateFilenameText();
        }

        /// <summary>
        /// Updates the filename text.
        /// </summary>
        private void UpdateFilenameText()
        {
            int pageCount = thumbnailViewer.Images.Count;
            if (pageCount == 0)
            {
                if (folderThumbnailViewer.FocusedFilename != null)
                    fileNameTextBox.Text = Path.GetFileName(folderThumbnailViewer.FocusedFilename);
                else
                    fileNameTextBox.Text = "";
            }
            else
            {
                string filename = Path.GetFileName(thumbnailViewer.Images[0].SourceInfo.Filename);
                if (pageCount > 1)
                    fileNameTextBox.Text = string.Format("{0} ({1} pages)", filename, pageCount);
                else
                    fileNameTextBox.Text = filename;
            }
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of folderThumbnailViewer object.
        /// </summary>
        private void folderThumbnailViewer_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DestImagesManager != null)
            {
                int thumbnailIndex = folderThumbnailViewer.PointToThumbnailIndex(e.GetPosition(folderThumbnailViewer));
                if (thumbnailIndex >= 0)
                {
                    // needs change focused index when image added
                    DestImagesManager.Images.ImageCollectionChanged += SetFocusedIndexToAddedImage;

                    // add focused image file to DestImagesManager
                    DestImagesManager.Add(folderThumbnailViewer.FocusedFilename);

                    DialogResult = true;
                }
            }
        }

        /// <summary>
        /// Sets the focused index to added image.
        /// </summary>
        private void SetFocusedIndexToAddedImage(object sender, ImageCollectionChangeEventArgs e)
        {
            if (e.Action == ImageCollectionChangeAction.AddImages)
            {
                ((ImageCollection)sender).ImageCollectionChanged -= SetFocusedIndexToAddedImage;
                if (DestImagesManager is WpfImageViewerImagesManager)
                {
                    WpfImageViewerBase imageViewer = ((WpfImageViewerImagesManager)DestImagesManager).ImageViewer;
                    imageViewer.FocusedIndex = imageViewer.Images.IndexOf(e.Images[0]);
                }
            }
        }

        #endregion


    }
}
