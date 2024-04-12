using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Win32;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Annotation;
using Vintasoft.Imaging.Annotation.Formatters;
using Vintasoft.Imaging.Annotation.UI;
using Vintasoft.Imaging.Annotation.Wpf.Print;
using Vintasoft.Imaging.Annotation.Wpf.UI;
using Vintasoft.Imaging.Annotation.Wpf.UI.VisualTools;
using Vintasoft.Imaging.Codecs.Decoders;
using Vintasoft.Imaging.Codecs.Encoders;
using Vintasoft.Imaging.ImageProcessing;
using Vintasoft.Imaging.Metadata;
using Vintasoft.Imaging.Print;
using Vintasoft.Imaging.Text;
using Vintasoft.Imaging.UI;
using Vintasoft.Imaging.UIActions;
using Vintasoft.Imaging.Undo;
using Vintasoft.Imaging.Wpf.UI;
using Vintasoft.Imaging.Wpf.UI.VisualTools;
using Vintasoft.Imaging.Wpf.UI.VisualTools.UserInteraction;

using WpfDemosCommonCode;
using WpfDemosCommonCode.Annotation;
using WpfDemosCommonCode.Imaging;
using WpfDemosCommonCode.Imaging.Codecs;
using WpfDemosCommonCode.Office;
using WpfDemosCommonCode.Twain;

using Vintasoft.Imaging.Annotation.Comments;
using Vintasoft.Imaging.Annotation.Wpf.UI.Comments;
#if !REMOVE_OFFICE_PLUGIN
using Vintasoft.Imaging.Office.OpenXml.Wpf.UI.VisualTools.UserInteraction;
#endif

#if !REMOVE_PDF_PLUGIN
using WpfDemosCommonCode.Pdf;
#endif

namespace WpfDocumentViewerDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Constants

        /// <summary>
        /// A value indicating whether the application window must be per-monitor DPI-aware.
        /// </summary>
        const bool PER_MONITOR_DPI_ENABLED = true;

        /// <summary>
        /// The value, in screen pixels, that defines how annotation position will be changed when user pressed arrow key.
        /// </summary>
        const int ANNOTATION_KEYBOARD_MOVE_DELTA = 2;

        /// <summary>
        /// The value, in screen pixels, that defines how annotation size will be changed when user pressed "+/-" key.
        /// </summary>
        const int ANNOTATION_KEYBOARD_RESIZE_DELTA = 4;

        #endregion



        #region Fields

        public static RoutedCommand _openCommand = new RoutedCommand();
        public static RoutedCommand _addCommand = new RoutedCommand();
        public static RoutedCommand _saveAsCommand = new RoutedCommand();
        public static RoutedCommand _closeCommand = new RoutedCommand();
        public static RoutedCommand _printCommand = new RoutedCommand();
        public static RoutedCommand _exitCommand = new RoutedCommand();
        public static RoutedCommand _undoCommand = new RoutedCommand();
        public static RoutedCommand _redoCommand = new RoutedCommand();
        public static RoutedCommand _findTextCommand = new RoutedCommand();
        public static RoutedCommand _cutCommand = new RoutedCommand();
        public static RoutedCommand _copyCommand = new RoutedCommand();
        public static RoutedCommand _pasteCommand = new RoutedCommand();
        public static RoutedCommand _deleteCommand = new RoutedCommand();
        public static RoutedCommand _deleteAllCommand = new RoutedCommand();
        public static RoutedCommand _selectAllCommand = new RoutedCommand();
        public static RoutedCommand _deselectAllCommand = new RoutedCommand();
        public static RoutedCommand _groupCommand = new RoutedCommand();
        public static RoutedCommand _groupAllCommand = new RoutedCommand();
        public static RoutedCommand _rotateClockwiseCommand = new RoutedCommand();
        public static RoutedCommand _rotateCounterclockwiseCommand = new RoutedCommand();
        public static RoutedCommand _aboutCommand = new RoutedCommand();

        /// <summary>
        /// Template of application title.
        /// </summary>
        string _titlePrefix = "VintaSoft WPF Document Viewer Demo v" + ImagingGlobalSettings.ProductVersion + " - {0}";

        /// <summary>
        /// Selected "View - Image scale mode" menu item.
        /// </summary>
        MenuItem _imageScaleModeSelectedMenuItem;

        /// <summary>
        /// Dictionary: the menu item => the annotation type.
        /// </summary>
        Dictionary<MenuItem, AnnotationType> _menuItemToAnnotationType = new Dictionary<MenuItem, AnnotationType>();

        /// <summary>
        /// Name of opened image file.
        /// </summary>
        string _sourceFilename = null;

        /// <summary>
        /// A value indicating whether file is opened in read-only mode.
        /// </summary>
        bool _isFileReadOnlyMode = false;

        /// <summary>
        /// A value indicating whether the source image file is changing.
        /// </summary> 
        bool _isSourceChanging = false;

        /// <summary>
        /// Manages asynchronous operations of the annotation viewer images.
        /// </summary>
        WpfImageViewerImagesManager _imagesManager;

        /// <summary>
        /// Filename where image collection must be saved.
        /// </summary>
        string _saveFilename;
        SaveFileDialog _saveFileDialog = new SaveFileDialog();

        /// <summary>
        /// Start time of image loading.
        /// </summary>
        DateTime _imageLoadingStartTime;

        /// <summary>
        /// Time of image loading.
        /// </summary>
        TimeSpan _imageLoadingTime = TimeSpan.Zero;
        OpenFileDialog _openFileDialog = new OpenFileDialog();

        /// <summary>
        /// The visual tool that allows to search and select text on document page.
        /// </summary>
        WpfTextSelectionTool _textSelectionTool;

        /// <summary>
        /// The visual tool for document navigation.
        /// </summary>
        WpfDocumentNavigationTool _navigationTool;

        /// <summary>
        /// List of initialized annotations.
        /// </summary>
        List<AnnotationData> _initializedAnnotations = new List<AnnotationData>();   

        /// <summary>
        /// A value indicating whether transforming of annotation is started.
        /// </summary>
        bool _isAnnotationTransforming = false;

        /// <summary>
        /// Manager of interaction areas.
        /// </summary>
        WpfAnnotationInteractionAreaAppearanceManager _interactionAreaAppearanceManager;

        /// <summary>
        /// Print manager.
        /// </summary>
        WpfAnnotatedImagePrintManager _printManager;

        /// <summary>
        /// The undo manager.
        /// </summary>
        CompositeUndoManager _undoManager;

        /// <summary>
        /// The undo monitor of annotation viewer.
        /// </summary>
        CustomAnnotationViewerUndoMonitor _annotationViewerUndoMonitor;

        /// <summary>
        /// Window with annotation history.
        /// </summary>
        WpfUndoManagerHistoryWindow _historyWindow;

        /// <summary>
        /// Simple TWAIN manager.
        /// </summary>
        WpfSimpleTwainManager _simpleTwainManager;

        /// <summary>
        /// A value indicating whether the application window is closing.
        /// </summary>
        bool _isWindowClosing = false;

        /// <summary>
        /// The comment visual tool.
        /// </summary>
        WpfCommentVisualTool _commentVisualTool;

        /// <summary>
        /// The context menu position.
        /// </summary>
        Point _contextMenuPosition;

        /// <summary>
        /// Manages the layout settings of DOCX document image collections.
        /// </summary>
        ImageCollectionDocxLayoutSettingsManager _imageCollectionDocxLayoutSettingsManager;

        /// <summary>
        /// Manages the layout settings of XLSX document image collections.
        /// </summary>
        ImageCollectionXlsxLayoutSettingsManager _imageCollectionXlsxLayoutSettingsManager;

        #endregion



        #region Constructors


        public MainWindow()
        {
            // register the evaluation license for VintaSoft Imaging .NET SDK
            Vintasoft.Imaging.ImagingGlobalSettings.Register("REG_USER", "REG_EMAIL", "EXPIRATION_DATE", "REG_CODE");

            InitializeComponent();

            Jbig2AssemblyLoader.Load();
            Jpeg2000AssemblyLoader.Load();
            PdfAnnotationsAssemblyLoader.Load();
            DocxAssemblyLoader.Load();
            RawAssemblyLoader.Load();
            DicomAssemblyLoader.Load();

            ImagingTypeEditorRegistrator.Register();
            AnnotationTypeEditorRegistrator.Register();

#if !REMOVE_OFFICE_PLUGIN
            AnnotationOfficeWpfUIAssembly.Init();
#endif

            InitAddAnnotationMenuItems();

            InitImageDisplayMode();

            InitUndoManager();

            InitImagesManager();

            // init text selection tool
            InitTextSelectionTool();

            //init document navigation tool
            InitNavigationTool();

            AnnotationCommentController annotationCommentController = new AnnotationCommentController(annotationViewer1.AnnotationDataController);
            WpfImageViewerCommentController imageViewerCommentsController = new WpfImageViewerCommentController(annotationCommentController);

            _commentVisualTool = new WpfCommentVisualTool(imageViewerCommentsController, new CommentControlFactory());
            annotationCommentsControl1.ImageViewer = annotationViewer1;
            annotationCommentsControl1.CommentTool = _commentVisualTool;
            annotationCommentsControl1.AnnotationTool = annotationViewer1.AnnotationVisualTool;


            // set default visual tool
            annotationViewer1.VisualTool = new WpfCompositeVisualTool(
                _commentVisualTool,
#if !REMOVE_OFFICE_PLUGIN
               new Vintasoft.Imaging.Office.OpenXml.Wpf.UI.VisualTools.UserInteraction.WpfOfficeDocumentVisualEditorTextTool(),
#endif
                annotationViewer1.AnnotationVisualTool,
                _navigationTool,
                _textSelectionTool,
                annotationViewer1.AnnotationSelectionTool);
            visualToolsToolBar.MandatoryVisualTool = annotationViewer1.VisualTool;
            visualToolsToolBar.ImageViewer = annotationViewer1;
            visualToolsToolBar.VisualToolsMenuItem = visualToolsMenuItem;

            _interactionAreaAppearanceManager = new WpfAnnotationInteractionAreaAppearanceManager();
            _interactionAreaAppearanceManager.VisualTool = annotationViewer1.AnnotationVisualTool;
            enableSpellCheckingMenuItem.IsEnabled = _interactionAreaAppearanceManager.IsSpellCheckingEnabled;

            CloseCurrentFile();

            DemosTools.SetTestFilesFolder(_openFileDialog);

            thumbnailViewer1.MasterViewer = annotationViewer1;

            // set default rendering settings
#if REMOVE_PDF_PLUGIN && REMOVE_OFFICE_PLUGIN
            annotationViewer1.ImageRenderingSettings = RenderingSettings.Empty;
#elif REMOVE_OFFICE_PLUGIN
            annotationViewer1.ImageRenderingSettings = new PdfRenderingSettings();
#elif REMOVE_PDF_PLUGIN
            annotationViewer1.ImageRenderingSettings = new CompositeRenderingSettings(
                new DocxRenderingSettings(),
                new XlsxRenderingSettings());
#else
            annotationViewer1.ImageRenderingSettings = new CompositeRenderingSettings(
                new PdfRenderingSettings(),
                new DocxRenderingSettings(),
                new XlsxRenderingSettings());
#endif

            // bind viewer to tool strips
            visualToolsToolBar.ImageViewer = annotationViewer1;
            annotationsToolBar.AnnotationViewer = annotationViewer1;
            annotationsToolBar.ViewerToolBar = MainToolbar;
            annotationsToolBar.CommentBuilder = new AnnotationCommentBuilder(_commentVisualTool, annotationViewer1.AnnotationVisualTool);

            NoneAction noneAction = visualToolsToolBar.FindAction<NoneAction>();
            noneAction.Activated += NoneAction_Activated;
            noneAction.Deactivated += NoneAction_Deactivated;

            MainToolbar.ImageViewer = annotationViewer1;

            annotationViewer1.PreviewMouseMove += new MouseEventHandler(annotationViewer_PreviewMouseMove);
            annotationViewer1.SelectedAnnotations.Changed += new EventHandler(SelectedAnnotations_Changed);
            annotationViewer1.FocusedAnnotationViewChanged += new EventHandler<WpfAnnotationViewChangedEventArgs>(annotationViewer_SelectedAnnotationViewChanged);
            annotationViewer1.AutoScrollPositionExChanging += new EventHandler<PropertyChangingEventArgs<Point>>(annotationViewer_AutoScrollPositionExChanging);
            annotationViewer1.Images.ImageCollectionSavingProgress += new EventHandler<ProgressEventArgs>(SavingProgress);
            annotationViewer1.Images.ImageCollectionSavingFinished += new EventHandler(images_ImageCollectionSavingFinished);
            annotationViewer1.Images.ImageSavingException += new EventHandler<ExceptionEventArgs>(Images_ImageSavingException);
            annotationViewer1.AnnotationDataController.AnnotationDataDeserializationException += new EventHandler<AnnotationDataDeserializationExceptionEventArgs>(AnnotationDataController_AnnotationDataDeserializationException);

            annotationViewer1.AnnotationBuildingStarted += new EventHandler<WpfAnnotationViewEventArgs>(annotationViewer_AnnotationBuildingStarted);
            annotationViewer1.AnnotationBuildingFinished += new EventHandler<WpfAnnotationViewEventArgs>(annotationViewer_AnnotationBuildingFinished);
            annotationViewer1.AnnotationBuildingCanceled += new EventHandler<WpfAnnotationViewEventArgs>(annotationViewer_AnnotationBuildingCanceled);

            InitPrintManager();

            // remember current image scale mode
            _imageScaleModeSelectedMenuItem = bestFitMenuItem;

            // initialize color management
            ColorManagementHelper.EnableColorManagement(annotationViewer1);

            // update the UI
            UpdateUI();

            annotationViewer1.CatchVisualToolExceptions = true;
            annotationViewer1.VisualToolException += new EventHandler<Vintasoft.Imaging.ExceptionEventArgs>(annotationViewer_VisualToolException);
            annotationViewer1.InputGestureDelete = null;

            InitCustomAnnotations();

            // set CustomFontProgramsController for all opened PDF documents
            CustomFontProgramsController.SetDefaultFontProgramsController();

            moveAnnotationsBetweenImagesMenuItem.IsChecked = annotationViewer1.CanMoveAnnotationsBetweenImages;

            // define custom serialization binder for correct deserialization of TriangleAnnotation v6.1 and earlier
            AnnotationSerializationBinder.Current = new CustomAnnotationSerializationBinder();

            // init visual tools
            InitVisualToolsToolBar();

            DocumentPasswordWindow.EnableAuthentication(annotationViewer1);

#if !REMOVE_OFFICE_PLUGIN
            // specify that image collection of annotation viewer must handle layout settings requests
            _imageCollectionDocxLayoutSettingsManager = new ImageCollectionDocxLayoutSettingsManager(annotationViewer1.Images);
            _imageCollectionXlsxLayoutSettingsManager = new ImageCollectionXlsxLayoutSettingsManager(annotationViewer1.Images);
#else
            documentLayoutSettingsMenuItem.Visibility = Visibility.Collapsed;
#endif
        }

        #endregion



        #region Properties

        bool _isFileOpening = false;
        /// <summary>
        /// Gets or sets a value indicating whether file is opening.
        /// </summary>
        internal bool IsFileOpening
        {
            get
            {
                return _isFileOpening;
            }
            set
            {
                _isFileOpening = value;

                if (_isFileOpening)
                    Cursor = Cursors.AppStarting;
                else
                    Cursor = Cursors.Arrow;

                UpdateUI();
            }
        }

        bool _isFileSaving = false;
        /// <summary>
        /// Gets or sets a value indicating whether file is saving.
        /// </summary>
        internal bool IsFileSaving
        {
            get
            {
                return _isFileSaving;
            }
            set
            {
                _isFileSaving = value;

                if (Dispatcher.Thread == Thread.CurrentThread)
                    UpdateUI();
                else
                    InvokeUpdateUI();
            }
        }

        bool _isTextSearching = false;
        /// <summary>
        /// Gets or sets a value indicating whether text search is in progress.
        /// </summary>
        internal bool IsTextSearching
        {
            get
            {
                return _isTextSearching;
            }
            set
            {
                _isTextSearching = value;
                UpdateUI();
            }
        }

        #endregion



        #region Methods

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.ContentRendered" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> that contains the event data.</param>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

#if !REMOVE_OFFICE_PLUGIN
            WpfDemosCommonCode.Office.OfficeDocumentVisualEditorWindow documentVisualEditorForm = new WpfDemosCommonCode.Office.OfficeDocumentVisualEditorWindow();
            documentVisualEditorForm.Owner = this;
            documentVisualEditorForm.AddVisualTool(annotationViewer1.AnnotationVisualTool);
#endif
        }


        #region Init

        /// <summary>
        /// Initializes the "Annotation" -> "Menu" menu items.
        /// </summary>
        private void InitAddAnnotationMenuItems()
        {
            _menuItemToAnnotationType.Clear();

            _menuItemToAnnotationType.Add(rectangleMenuItem, AnnotationType.Rectangle);
            _menuItemToAnnotationType.Add(ellipseMenuItem, AnnotationType.Ellipse);
            _menuItemToAnnotationType.Add(highlightMenuItem, AnnotationType.Highlight);
            _menuItemToAnnotationType.Add(textHighlightMenuItem, AnnotationType.TextHighlight);
            _menuItemToAnnotationType.Add(embeddedImageMenuItem, AnnotationType.EmbeddedImage);
            _menuItemToAnnotationType.Add(referencedImageMenuItem, AnnotationType.ReferencedImage);
            _menuItemToAnnotationType.Add(textMenuItem, AnnotationType.Text);
            _menuItemToAnnotationType.Add(stickyNoteMenuItem, AnnotationType.StickyNote);
            _menuItemToAnnotationType.Add(freeTextMenuItem, AnnotationType.FreeText);
            _menuItemToAnnotationType.Add(rubberStampMenuItem, AnnotationType.RubberStamp);
            _menuItemToAnnotationType.Add(linkMenuItem, AnnotationType.Link);
            _menuItemToAnnotationType.Add(lineMenuItem, AnnotationType.Line);
            _menuItemToAnnotationType.Add(linesMenuItem, AnnotationType.Lines);
            _menuItemToAnnotationType.Add(linesWithInterpolationMenuItem, AnnotationType.LinesWithInterpolation);
            _menuItemToAnnotationType.Add(freehandLinesMenuItem, AnnotationType.FreehandLines);
            _menuItemToAnnotationType.Add(polygonMenuItem, AnnotationType.Polygon);
            _menuItemToAnnotationType.Add(polygonWithInterpolationMenuItem, AnnotationType.PolygonWithInterpolation);
            _menuItemToAnnotationType.Add(freehandPolygonMenuItem, AnnotationType.FreehandPolygon);
            _menuItemToAnnotationType.Add(rulerMenuItem, AnnotationType.Ruler);
            _menuItemToAnnotationType.Add(rulersMenuItem, AnnotationType.Rulers);
            _menuItemToAnnotationType.Add(angleMenuItem, AnnotationType.Angle);
            _menuItemToAnnotationType.Add(triangleCustomAnnotationMenuItem, AnnotationType.Triangle);
            _menuItemToAnnotationType.Add(markCustomAnnotationMenuItem, AnnotationType.Mark);
        }

        /// <summary>
        /// Initializes the text selection tool.
        /// </summary>
        private void InitTextSelectionTool()
        {
            _textSelectionTool = new WpfTextSelectionTool(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(56, 0, 0, 255)));
            _textSelectionTool.SelectionChanged += new EventHandler(TextSelectionTool_SelectionChanged);
            _textSelectionTool.TextSearching += new EventHandler(_textSelectionTool_TextSearching);
            _textSelectionTool.TextSearchingProgress += new EventHandler<TextSearchingProgressEventArgs>(_textSelectionTool_TextSearchingProgress);
            _textSelectionTool.TextSearched += new EventHandler<TextSearchedEventArgs>(_textSelectionTool_TextSearched);
            _textSelectionTool.TextExtractionProgress += new EventHandler<ProgressEventArgs>(TextSelectionTool_TextExtractionProgress);
            _textSelectionTool.IsMouseSelectionEnabled = true;
            _textSelectionTool.IsKeyboardSelectionEnabled = true;
            textSelectionControl.TextSelectionTool = _textSelectionTool;
            findTextToolBar.TextSelectionTool = _textSelectionTool;
        }

        /// <summary>
        /// Initializes the navigation tool.
        /// </summary>
        private void InitNavigationTool()
        {
            _navigationTool = new WpfDocumentNavigationTool();

            _navigationTool.ActionExecutor = new WpfPageContentActionCompositeExecutor(
                new WpfUriActionExecutor(),
                new WpfLaunchActionExecutor(),
                _navigationTool.ActionExecutor);
            _navigationTool.FocusedActionChanged +=
                new PropertyChangedEventHandler<PageContentActionMetadata>(NavigationTool_FocusedActionChanged);
        }

        /// <summary>
        /// Initializes custom annotations.
        /// </summary>
        private void InitCustomAnnotations()
        {
            // register view for mark annotation data
            WpfAnnotationViewFactory.RegisterViewForAnnotationData(
               typeof(MarkAnnotationData),
               typeof(WpfMarkAnnotationView));
            // register view for triangle annotation data
            WpfAnnotationViewFactory.RegisterViewForAnnotationData(
                typeof(TriangleAnnotationData),
                typeof(WpfTriangleAnnotationView));
        }

        /// <summary>
        /// Initializes the undo manager.
        /// </summary>
        private void InitUndoManager()
        {
            _undoManager = new CompositeUndoManager();
            _undoManager.UndoLevel = 100;
            _undoManager.IsEnabled = false;
            _undoManager.Changed += new EventHandler<UndoManagerChangedEventArgs>(annotationUndoManager_Changed);
            _undoManager.Navigated += new EventHandler<UndoManagerNavigatedEventArgs>(annotationUndoManager_Navigated);

            _annotationViewerUndoMonitor = new CustomAnnotationViewerUndoMonitor(_undoManager, annotationViewer1);
        }

        /// <summary>
        /// Initializes the images manager.
        /// </summary>
        private void InitImagesManager()
        {
            // create images manager
            _imagesManager = new WpfImageViewerImagesManager(annotationViewer1);
            _imagesManager.IsAsync = true;
            _imagesManager.AddStarting += new EventHandler(ImagesManager_AddStarting);
            _imagesManager.AddFinished += new EventHandler(ImagesManager_AddFinished);
            _imagesManager.ImageSourceAddStarting += new EventHandler<ImageSourceEventArgs>(ImagesManager_ImageSourceAddStarting);
            _imagesManager.ImageSourceAddFinished += new EventHandler<ImageSourceEventArgs>(ImagesManager_ImageSourceAddFinished);
            _imagesManager.ImageSourceAddException += new EventHandler<ImageSourceExceptionEventArgs>(ImagesManager_ImageSourceAddException);
        }

        /// <summary>
        /// Initializes the print manager.
        /// </summary>
        private void InitPrintManager()
        {
            // create the print manager
            _printManager = new WpfAnnotatedImagePrintManager(annotationViewer1.AnnotationDataController);
            _printManager.PrintScaleMode = PrintScaleMode.BestFit;
        }

        /// <summary>
        /// Initializes image display mode.
        /// </summary>
        private void InitImageDisplayMode()
        {
            // init "View => Image Display Mode" menu
            singlePageMenuItem.Tag = ImageViewerDisplayMode.SinglePage;
            twoColumnsMenuItem.Tag = ImageViewerDisplayMode.TwoColumns;
            singleContinuousRowMenuItem.Tag = ImageViewerDisplayMode.SingleContinuousRow;
            singleContinuousColumnMenuItem.Tag = ImageViewerDisplayMode.SingleContinuousColumn;
            twoContinuousRowsMenuItem.Tag = ImageViewerDisplayMode.TwoContinuousRows;
            twoContinuousColumnsMenuItem.Tag = ImageViewerDisplayMode.TwoContinuousColumns;
        }

        /// <summary>
        /// Initializes elements of visual tools toolbar.
        /// </summary>
        private void InitVisualToolsToolBar()
        {
            // create action, which allows to measure objects on image in image viewer
            MeasurementVisualToolActionFactory.CreateActions(visualToolsToolBar);

            // create different zoom actions
            ZoomVisualToolActionFactory.CreateActions(visualToolsToolBar);

            // create action, which allows to crop an image in image viewer
            VisualToolAction cropSelectionToolAction = new VisualToolAction(
               new WpfCropSelectionTool(),
               "Crop Selection",
               "Crop Selection",
               DemosResourcesManager.GetResourceAsBitmap("WpfDemosCommonCode.Imaging.VisualToolsToolBar.VisualTools.ImageProcessingVisualTools.Resources.WpfCropSelectionTool.png"));
            visualToolsToolBar.AddAction(cropSelectionToolAction);

            // create action, which allows to pan an image in image viewer
            VisualToolAction panToolAction = new VisualToolAction(
                new WpfPanTool(),
                "Pan Tool",
                "Pan",
                DemosResourcesManager.GetResourceAsBitmap("WpfDemosCommonCode.Imaging.VisualToolsToolBar.VisualTools.ImageProcessingVisualTools.Resources.WpfPanTool.png"));
            visualToolsToolBar.AddAction(panToolAction);

            // create action, which allows to scroll pages in image viewer
            VisualToolAction scrollPagesVisualToolAction = new VisualToolAction(
                new WpfScrollPages(),
                "Scroll Pages",
                "Scroll Pages",
                DemosResourcesManager.GetResourceAsBitmap("WpfDemosCommonCode.Imaging.VisualToolsToolBar.VisualTools.CustomVisualTools.Resources.WpfScrollPagesTool.png"));
            visualToolsToolBar.AddAction(scrollPagesVisualToolAction);
        }

        #endregion


        #region MainWindow

        /// <summary>
        /// Handles the Loaded event of Window object.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // process command line of the application
            string[] appArgs = Environment.GetCommandLineArgs();
            if (appArgs.Length > 0)
            {
                if (appArgs.Length == 2)
                {
                    try
                    {
                        // add image file to the annotation viewer
                        OpenFile(appArgs[1]);
                    }
                    catch (Exception ex)
                    {
                        DemosTools.ShowErrorMessage(ex);
                    }
                }
                else
                {
                    // get filenames from application arguments
                    string[] filenames = new string[appArgs.Length - 1];
                    Array.Copy(appArgs, 1, filenames, 0, filenames.Length);

                    try
                    {
                        // add image file(s) to the image collection of the annotation viewer
                        AddFiles(filenames);
                    }
                    catch (Exception ex)
                    {
                        DemosTools.ShowErrorMessage(ex);
                    }
                }

                thumbnailViewer1.Focus();
            }

            if (PER_MONITOR_DPI_ENABLED)
            {
                PresentationSource visual = PresentationSource.FromVisual(this);
                double xScale = 1 / visual.CompositionTarget.TransformToDevice.M11;
                double yScale = 1 / visual.CompositionTarget.TransformToDevice.M22;
                Width = Width * xScale;
                Height = Height * yScale;
                UpdateLayoutTransform(xScale, yScale);
            }
        }

        /// <summary>
        /// Updates the layout transform.
        /// </summary>
        void UpdateLayoutTransform(double xScale, double yScale)
        {
            System.Windows.Media.Visual child = GetVisualChild(0);
            if (xScale != 1.0 && yScale != 1.0)
            {
                System.Windows.Media.ScaleTransform dpiScale = new System.Windows.Media.ScaleTransform(xScale, yScale);
                child.SetValue(Window.LayoutTransformProperty, dpiScale);
            }
            else
            {
                child.SetValue(Window.LayoutTransformProperty, null);
            }
        }

        /// <summary>
        /// Main window is closing.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            annotationViewer1.CancelAnnotationBuilding();

            _isWindowClosing = true;
            _printManager.Dispose();
        }

        /// <summary>
        /// Main window is closed.
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            CloseCurrentFile();

            _annotationViewerUndoMonitor.Dispose();
            _undoManager.Dispose();

            _interactionAreaAppearanceManager.Dispose();

            _imagesManager.Dispose();
        }

        #endregion


        #region UI state

        /// <summary>
        /// Updates the user interface of this window.
        /// </summary>
        private void UpdateUI()
        {
            // get the current status of application
            bool isFileOpening = IsFileOpening;
            bool isFileLoaded = _sourceFilename != null;
            bool isFileReadOnlyMode = _isFileReadOnlyMode;
            bool isFileEmpty = true;
            if (annotationViewer1.Images != null)
                isFileEmpty = annotationViewer1.Images.Count <= 0;
            bool isFileSaving = IsFileSaving;
            bool isImageSelected = annotationViewer1.Image != null;
            bool isAnnotationEmpty = true;
            if (isImageSelected)
                isAnnotationEmpty = annotationViewer1.AnnotationDataController[annotationViewer1.FocusedIndex].Count <= 0;
            bool isAnnotationSelected = annotationViewer1.FocusedAnnotationView != null || annotationViewer1.SelectedAnnotations.Count > 0;
            bool isAnnotationBuilding = annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding;
            bool isInteractionModeAuthor = annotationViewer1.AnnotationInteractionMode == AnnotationInteractionMode.Author;
            bool isCanUndo = _undoManager.UndoCount > 0 && !annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding;
            bool isCanRedo = _undoManager.RedoCount > 0 && !annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding;
            bool isTextSearching = IsTextSearching;

            // "File" menu
            fileMenuItem.IsEnabled = !isFileSaving;
            openImageMenuItem.IsEnabled = !isFileOpening && !isTextSearching;
            documentLayoutSettingsMenuItem.IsEnabled = !isFileOpening;
            saveMenuItem.IsEnabled = !isFileOpening && isFileLoaded && !isFileEmpty && !isFileReadOnlyMode && !isTextSearching;
            saveAsMenuItem.IsEnabled = !isFileOpening && !isFileEmpty && !isTextSearching;
            closeAllMenuItem.IsEnabled = !isFileEmpty || isFileOpening;
            pageSettingsMenuItem.IsEnabled = !isFileEmpty;
            printMenuItem.IsEnabled = !isFileOpening && !isFileEmpty && !IsFileSaving && !isTextSearching;

            // "Edit" menu
            editMenuItem.IsEnabled = !isFileEmpty && !isFileSaving;
            if (!editMenuItem.IsEnabled)
                CloseHistoryWindow();
            findTextMenuItem.IsEnabled = !isTextSearching;
            enableUndoRedoMenuItem.IsChecked = _undoManager.IsEnabled;
            undoMenuItem.IsEnabled = _undoManager.IsEnabled && !isFileOpening && !isFileSaving && isCanUndo;
            redoMenuItem.IsEnabled = _undoManager.IsEnabled && !isFileOpening && !isFileSaving && isCanRedo;
            historyDialogMenuItem.IsEnabled = _undoManager.IsEnabled && !isFileOpening && !isFileSaving;

            // "View" menu
            moveAnnotationsBetweenImagesMenuItem.IsEnabled =
                annotationViewer1.DisplayMode != ImageViewerDisplayMode.SinglePage;
            documentMetadataMenuItem.IsEnabled = !isFileEmpty;

            // update "View => Image Display Mode" menu
            singlePageMenuItem.IsChecked = false;
            twoColumnsMenuItem.IsChecked = false;
            singleContinuousRowMenuItem.IsChecked = false;
            singleContinuousColumnMenuItem.IsChecked = false;
            twoContinuousRowsMenuItem.IsChecked = false;
            twoContinuousColumnsMenuItem.IsChecked = false;
            switch (annotationViewer1.DisplayMode)
            {
                case ImageViewerDisplayMode.SinglePage:
                    singlePageMenuItem.IsChecked = true;
                    break;

                case ImageViewerDisplayMode.TwoColumns:
                    twoColumnsMenuItem.IsChecked = true;
                    break;

                case ImageViewerDisplayMode.SingleContinuousRow:
                    singleContinuousRowMenuItem.IsChecked = true;
                    break;

                case ImageViewerDisplayMode.SingleContinuousColumn:
                    singleContinuousColumnMenuItem.IsChecked = true;
                    break;

                case ImageViewerDisplayMode.TwoContinuousRows:
                    twoContinuousRowsMenuItem.IsChecked = true;
                    break;

                case ImageViewerDisplayMode.TwoContinuousColumns:
                    twoContinuousColumnsMenuItem.IsChecked = true;
                    break;
            }

            // "Annotations" menu
            //
            annotationsInfoMenuItem.IsEnabled = !isFileEmpty;
            //
            interactionModeMenuItem.IsEnabled = !isFileEmpty;
            //
            loadFromFileMenuItem.IsEnabled = !isFileEmpty;
            saveToFileMenuItem.IsEnabled = !isAnnotationEmpty && !isFileSaving;
            //
            addAnnotationMenuItem.IsEnabled = !isFileEmpty && isInteractionModeAuthor;
            buildAnnotationsContinuouslyMenuItem.IsEnabled = !isFileEmpty;
            //
            bringToBackMenuItem1.IsEnabled = !isFileEmpty && isInteractionModeAuthor && !isAnnotationBuilding && isAnnotationSelected;
            bringToFrontMenuItem1.IsEnabled = !isFileEmpty && isInteractionModeAuthor && !isAnnotationBuilding && isAnnotationSelected;
            //
            multiSelectMenuItem.IsEnabled = !isFileEmpty;
            //
            groupSelectedMenuItem.IsEnabled = !isFileEmpty && isInteractionModeAuthor && !isAnnotationBuilding;
            groupAllMenuItem.IsEnabled = !isFileEmpty && isInteractionModeAuthor && !isAnnotationBuilding;
            //
            rotateImageWithAnnotationsMenuItem.IsEnabled = !isFileEmpty;
            burnAnnotationsOnImageMenuItem.IsEnabled = !isAnnotationEmpty;
            cloneImageWithAnnotationsMenuItem.IsEnabled = !isFileEmpty;
            saveImageWithAnnotationsMenuItem.IsEnabled = !isAnnotationEmpty && !isFileSaving;
            copyImageToClipboardMenuItem.IsEnabled = isImageSelected;
            deleteImageMenuItem.IsEnabled = isImageSelected && !isFileSaving;

            // annotation tool strip 
            annotationsToolBar.IsEnabled = !isFileEmpty;

            // thumbnailViewer1 & annotationViewer & propertyGrid1 & annotationComboBox
            panel5.IsEnabled = !isFileEmpty;
            if (annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding)
                annotationComboBox.IsEnabled = false;
            else
                annotationComboBox.IsEnabled = true;

            // thumbnail viewer
            thumbnailViewer1.IsEnabled = !isTextSearching;

            // viewer tool strip
            MainToolbar.IsEnabled = !isFileSaving;
            MainToolbar.OpenButtonEnabled = openImageMenuItem.IsEnabled;
            MainToolbar.SaveButtonEnabled = saveAsMenuItem.IsEnabled;
            MainToolbar.ScanButtonEnabled = !isFileOpening && !isTextSearching;
            MainToolbar.PrintButtonEnabled = printMenuItem.IsEnabled;

            // set window title
            if (!isFileOpening)
            {
                string str;
                if (_sourceFilename != null)
                    str = Path.GetFileName(_sourceFilename);
                else
                    str = "(Untitled)";

                if (_isFileReadOnlyMode)
                    str += " [Read Only]";

                Title = string.Format(_titlePrefix, str);
            }
        }

        /// <summary>
        /// Updates the UI safely.
        /// </summary>
        private void InvokeUpdateUI()
        {
            if (Dispatcher.Thread == Thread.CurrentThread)
                UpdateUI();
            else
                Dispatcher.Invoke(new UpdateUIDelegate(UpdateUI));
        }

        #endregion


        #region 'File' menu

        /// <summary>
        /// Clears image collection of annotation viewer and
        /// adds image(s) to the image collection of annotation viewer.
        /// </summary>
        private void openImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // set file filters in Open file dialog
            CodecsFileFilters.SetFilters(_openFileDialog);
            // add text file filter to the Open file dialog
            AddTxtFileFilterToOpenFileDialog(_openFileDialog);

            // select image file
            if (_openFileDialog.ShowDialog().Value)
            {
                try
                {
                    // add image file to the annotation viewer
                    OpenFile(_openFileDialog.FileName);
                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }
            }
        }

        /// <summary>
        /// Clears image collection of image viewer and
        /// adds image(s) to an image collection of image viewer.
        /// </summary>
        private void MainToolBar_OpenFile(object sender, EventArgs e)
        {
            openImageMenuItem_Click(sender, null);
        }

        /// <summary>
        /// Adds image(s) to an image collection of annotation viewer.
        /// </summary>
        private void addImagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CodecsFileFilters.SetFilters(_openFileDialog);
            // add text file filter to the Open file dialog
            AddTxtFileFilterToOpenFileDialog(_openFileDialog);

            _openFileDialog.Multiselect = true;

            // select image file(s)
            if (_openFileDialog.ShowDialog().Value)
            {
                try
                {
                    // add image file(s) to image collection of the annotation viewer
                    AddFiles(_openFileDialog.FileNames);
                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }
            }

            _openFileDialog.Multiselect = false;
        }

        /// <summary>
        /// Handles the Click event of docxLayoutSettingsMenuItem object.
        /// </summary>
        private void docxLayoutSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _imageCollectionDocxLayoutSettingsManager.EditLayoutSettingsUseDialog(this);
        }

        /// <summary>
        /// Handles the Click event of xlsxLayoutSettingsMenuItem object.
        /// </summary>
        private void xlsxLayoutSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _imageCollectionXlsxLayoutSettingsManager.EditLayoutSettingsUseDialog(this);
        }

        /// <summary>
        /// Saves image collection with annotations of image viewer to new source and
        /// switches to the new source.
        /// </summary>
        private void saveMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveToSourceFile();
        }

        /// <summary>
        /// Saves image collection with annotations of annotation viewer to new source and
        /// switches to the new source.
        /// </summary>
        private void MainToolBar_SaveAs(object sender, EventArgs e)
        {
            SaveImageCollectionToNewFile(true);
        }

        /// <summary>
        /// Saves image collection with annotations of annotation viewer to new source and
        /// switches to the new source.
        /// </summary>
        private void saveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveImageCollectionToNewFile(true);
        }

        /// <summary>
        /// Closes the current file.
        /// </summary>
        private void closeAllImagesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CloseCurrentFile();

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Shows a page settings dialog.
        /// </summary>
        private void pageSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PageSettingsWindow pageSettingsForm = new PageSettingsWindow(_printManager, _printManager.PagePadding, _printManager.ImagePadding);
            pageSettingsForm.Owner = this;
            if (pageSettingsForm.ShowDialog().Value)
            {
                _printManager.PagePadding = pageSettingsForm.PagePadding;
                _printManager.ImagePadding = pageSettingsForm.ImagePadding;
            }
        }

        /// <summary>
        /// Prints images with annotations.
        /// </summary>
        private void MainToolBar_Print(object sender, EventArgs e)
        {
            printMenuItem_Click(sender, null);
        }

        /// <summary>
        /// Prints images with annotations.
        /// </summary>
        private void printMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog printDialog = _printManager.PrintDialog;
            printDialog.MinPage = 1;
            printDialog.MaxPage = (uint)_printManager.Images.Count;
            printDialog.UserPageRangeEnabled = true;

            // show print dialog and
            // start print if dialog results is OK
            if (printDialog.ShowDialog().Value)
            {
                try
                {
                    _printManager.Print(this.Title);
                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }
            }
        }

        /// <summary>
        /// Acquires image(s) from scanner.
        /// </summary>
        private void MainToolBar_Scan(object sender, EventArgs e)
        {
            bool viewerToolstripCanScan = MainToolbar.CanScan;
            MainToolbar.ScanButtonEnabled = false;
            try
            {
                if (_simpleTwainManager == null)
                    _simpleTwainManager = new WpfSimpleTwainManager(this, annotationViewer1.Images);

                _simpleTwainManager.SelectDeviceAndAcquireImage();
            }
            catch (Exception ex)
            {
                DemosTools.ShowErrorMessage(ex);
            }
            finally
            {
                MainToolbar.ScanButtonEnabled = viewerToolstripCanScan;
            }
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        private void exitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion


        #region 'Edit' menu

        /// <summary>
        /// "Edit" menu is opening.
        /// </summary>
        private void EditMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            UpdateEditMenuItems();
        }

        /// <summary>
        /// Updates the UI action items in "Edit" menu.
        /// </summary>
        private void UpdateEditMenuItems()
        {
            // if the thumbnail viewer has the input focus
            if (thumbnailViewer1.IsFocused)
            {
                UpdateEditMenuItem(cutMenuItem, null, "Cut");
                UpdateEditMenuItem(copyMenuItem, null, "Copy");
                UpdateEditMenuItem(pasteMenuItem, null, "Paste");
                deleteMenuItem.IsEnabled = true;
                deleteMenuItem.Header = "Delete Page(s)";
                deleteAllMenuItem.IsEnabled = false;
                deleteAllMenuItem.Header = "Delete All";
                bool isFileEmpty = true;
                if (annotationViewer1.Images != null)
                    isFileEmpty = annotationViewer1.Images.Count <= 0;
                selectAllMenuItem.IsEnabled = !isFileEmpty && !IsFileOpening;
                selectAllMenuItem.Header = "Select All Pages";
                UpdateEditMenuItem(deselectAllMenuItem, null, "Deselect All");
            }
            // if the thumbnail viewer does NOT have the input focus
            else
            {
                UpdateEditMenuItem(cutMenuItem, GetUIAction<CutItemUIAction>(annotationViewer1.VisualTool), "Cut");
                UpdateEditMenuItem(copyMenuItem, GetUIAction<CopyItemUIAction>(annotationViewer1.VisualTool), "Copy");
                UpdateEditMenuItem(pasteMenuItem, GetUIAction<PasteItemUIAction>(annotationViewer1.VisualTool), "Paste");
                UpdateEditMenuItem(deleteMenuItem, GetUIAction<DeleteItemUIAction>(annotationViewer1.VisualTool), "Delete");
                UpdateEditMenuItem(deleteAllMenuItem, GetUIAction<DeleteAllItemsUIAction>(annotationViewer1.VisualTool), "Delete All");
                UpdateEditMenuItem(selectAllMenuItem, GetUIAction<SelectAllItemsUIAction>(annotationViewer1.VisualTool), "Select All");
                UpdateEditMenuItem(deselectAllMenuItem, GetUIAction<DeselectAllItemsUIAction>(annotationViewer1.VisualTool), "Deselect All");
            }
        }

        /// <summary>
        /// Updates the UI action item in "Edit" menu.
        /// </summary>
        /// <param name="menuItem">The "Edit" menu item.</param>
        /// <param name="uiAction">The UI action, which is associated with the "Edit" menu item.</param>
        /// <param name="defaultText">The default text for the "Edit" menu item.</param>
        private void UpdateEditMenuItem(MenuItem editMenuItem, UIAction uiAction, string defaultText)
        {
            // if UI action is specified AND UI action is enabled
            if (uiAction != null && uiAction.IsEnabled)
            {
                // enable the menu item
                editMenuItem.IsEnabled = true;
                // set text to the menu item
                editMenuItem.Header = uiAction.Name;
            }
            else
            {
                // disable the menu item
                editMenuItem.IsEnabled = false;
                // set the default text to the menu item
                editMenuItem.Header = defaultText;
            }
        }

        /// <summary>
        /// Copies selected item into clipboard.
        /// </summary>
        private void copyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get UI action
            CopyItemUIAction copyUIAction = GetUIAction<CopyItemUIAction>(annotationViewer1.VisualTool);
            // if UI action is not empty AND UI action is enabled
            if (copyUIAction != null && copyUIAction.IsEnabled)
            {
                // execute action
                copyUIAction.Execute();
            }

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Cuts selected item into clipboard.
        /// </summary>
        private void cutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get UI action
            CutItemUIAction cutUIAction = GetUIAction<CutItemUIAction>(annotationViewer1.VisualTool);
            // if UI action is not empty AND UI action is enabled
            if (cutUIAction != null && cutUIAction.IsEnabled)
            {
                // begin the composite undo action
                _undoManager.BeginCompositeAction("WpfAnnotationViewCollection: Cut");

                try
                {
                    cutUIAction.Execute();
                }
                finally
                {
                    // end the composite undo action
                    _undoManager.EndCompositeAction();
                }
            }

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Pastes previously copied item from clipboard.
        /// </summary>
        private void pasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get UI action
            PasteItemWithOffsetUIAction pasteUIAction = GetUIAction<PasteItemWithOffsetUIAction>(annotationViewer1.VisualTool);
            // if UI action is not empty AND UI action is enabled
            if (pasteUIAction != null && pasteUIAction.IsEnabled)
            {
                pasteUIAction.OffsetX = 20;
                pasteUIAction.OffsetY = 20;

                _undoManager.BeginCompositeAction("WpfAnnotationViewCollection: Paste");

                try
                {
                    pasteUIAction.Execute();
                }
                finally
                {
                    _undoManager.EndCompositeAction();
                }
            }

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Removes selected annotation from annotation collection
        /// or page from page collection.
        /// </summary>
        private void deleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // if thumbnail viewer is focused
            if (thumbnailViewer1.IsFocused)
            {
                thumbnailViewer1.DoDelete();
            }
            else
            {
                // delete the selected annotation from image
                DeleteAnnotation(false);
            }

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Removes all annotations from annotation collection.
        /// </summary>
        private void deleteAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // delete all annotations from image
            DeleteAnnotation(true);

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Selects all annotations of annotation collection
        /// or pages of page collection.
        /// </summary>
        private void selectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.CancelAnnotationBuilding();

            // if thumbnail viewer is focused
            if (thumbnailViewer1.IsFocused)
            {
                thumbnailViewer1.DoSelectAll();
            }
            else
            {
                // get UI action
                SelectAllItemsUIAction selectAllUIAction = GetUIAction<SelectAllItemsUIAction>(annotationViewer1.VisualTool);
                // if UI action is not empty AND UI action is enabled
                if (selectAllUIAction != null && selectAllUIAction.IsEnabled)
                {
                    // execute UI action
                    selectAllUIAction.Execute();
                }
            }

            UpdateUI();
        }

        /// <summary>
        /// Deselects all annotations of annotation collection.
        /// </summary>
        private void deselectAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.CancelAnnotationBuilding();

            // if thumbnail viewer is not focused
            if (!thumbnailViewer1.IsFocused)
            {
                // get UI action
                DeselectAllItemsUIAction deselectAllUIAction = GetUIAction<DeselectAllItemsUIAction>(annotationViewer1.VisualTool);
                // if UI action is not empty AND UI action is enabled
                if (deselectAllUIAction != null && deselectAllUIAction.IsEnabled)
                {
                    // execute UI action
                    deselectAllUIAction.Execute();
                }
            }

            UpdateUI();
        }

        /// <summary>
        /// Opens the find text window.
        /// </summary>
        private void findTextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IsTextSearching = true;

            FindTextWindow findTextDialog = new FindTextWindow(_textSelectionTool);

            TabItem selectedTab = (TabItem)toolsTabControl.SelectedItem;
            toolsTabControl.SelectedItem = textExtractionPage;

            if (_textSelectionTool.HasSelectedText)
                findTextDialog.FindWhat = _textSelectionTool.SelectedText;

            findTextDialog.Owner = this;
            findTextDialog.ShowDialog();

            toolsTabControl.SelectedItem = selectedTab;

            IsTextSearching = false;

        }

        /// <summary>
        /// Returns the UI action of the visual tool.
        /// </summary>
        /// <param name="visualTool">Visual tool.</param>
        /// <returns>The UI action of the visual tool.</returns>
        private T GetUIAction<T>(WpfVisualTool visualTool)
            where T : UIAction
        {
            IList<UIAction> uiActions = null;
            // if visual tool has actions
            if (TryGetCurrentToolActions(visualTool, out uiActions))
            {
                // for each action in list
                foreach (UIAction uiAction in uiActions)
                {
                    if (uiAction is T)
                        return (T)uiAction;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Returns the UI actions of visual tool.
        /// </summary>
        /// <param name="visualTool">The visual tool.</param>
        /// <param name="uiActions">The list of UI actions supported by the current visual tool.</param>
        /// <returns>
        /// <b>true</b> - UI actions are found; otherwise, <b>false</b>.
        /// </returns>
        private bool TryGetCurrentToolActions(
            WpfVisualTool visualTool,
            out IList<UIAction> uiActions)
        {
            uiActions = null;
            ISupportUIActions currentToolWithUIActions = visualTool as ISupportUIActions;
            if (currentToolWithUIActions != null)
                uiActions = currentToolWithUIActions.GetSupportedUIActions();

            return uiActions != null;
        }

        /// <summary>
        /// Enables/disables the undo manager.
        /// </summary>
        private void enableUndoRedoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            bool isUndoManagerEnabled = _undoManager.IsEnabled ^ true;


            if (!isUndoManagerEnabled)
            {
                CloseHistoryWindow();

                _undoManager.Clear();
            }

            _undoManager.IsEnabled = isUndoManagerEnabled;

            UpdateUndoRedoMenu(_undoManager);

            // update UI
            UpdateUI();
        }

        /// <summary>
        /// Undoes changes in annotation collection or annotation.
        /// </summary>
        private void undoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding)
                return;

            _undoManager.Undo(1);
            UpdateUI();
        }

        /// <summary>
        /// Redoes changes in annotation collection or annotation.
        /// </summary>
        private void redoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding)
                return;

            _undoManager.Redo(1);
            UpdateUI();
        }

        /// <summary>
        /// "Annotation history" menu is clicked.
        /// </summary>
        private void historyDialogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            historyDialogMenuItem.IsChecked ^= true;

            if (historyDialogMenuItem.IsChecked)
                // show the image processing history window
                ShowHistoryWindow();
            else
                // close the image processing history window
                CloseHistoryWindow();
        }

        #endregion


        #region 'View' menu

        /// <summary>
        /// Changes settings of thumbanil viewer.
        /// </summary>
        private void thumbnailViewerSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ThumbnailViewerSettingsWindow viewerSettingsDialog = new ThumbnailViewerSettingsWindow(thumbnailViewer1, (Style)Resources["ThumbnailItemStyle"]);
            viewerSettingsDialog.Owner = this;
            viewerSettingsDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            viewerSettingsDialog.ShowDialog();
        }

        /// <summary>
        /// Enables/disables the annotation tool.
        /// </summary>
        private void annotationToolToolStripMenuItem_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                bool isEnabled = annotationToolMenuItem.IsChecked;
                annotationViewer1.AnnotationVisualTool.Enabled = isEnabled;
                annotationViewer1.AnnotationSelectionTool.Enabled = isEnabled;
                annotationsToolBar.IsEnabled = isEnabled;
                annotationDataPropertyGrid.Enabled = isEnabled;
                annotationComboBox.IsEnabled = isEnabled;
            }
        }

        /// <summary>
        /// Enables/disables the text extraction tool.
        /// </summary>
        private void textSelectionToolStripMenuItem_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                _textSelectionTool.IsEnabled = textSelectionToolMenuItem.IsChecked;
                if (textSelectionToolMenuItem.IsChecked)
                    textSelectionControl.TextSelectionTool = _textSelectionTool;
                else
                {
                    _textSelectionTool.FocusedTextSymbol = null;
                    _textSelectionTool.SelectedRegion = null;
                    textSelectionControl.TextSelectionTool = null;
                }
                findTextMenuItem.IsEnabled = textSelectionToolMenuItem.IsChecked;
                findTextToolBar.IsEnabled = textSelectionToolMenuItem.IsChecked;
            }
        }

        /// <summary>
        /// Enables/disables the document navigation tool.
        /// </summary>
        private void navigationToolStripMenuItem_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                _navigationTool.Enabled = navigationToolMenuItem.IsChecked;
            }
        }

        /// <summary>
        /// Changes image display mode of annotation viewer.
        /// </summary>
        private void ImageDisplayMode_Click(object sender, RoutedEventArgs e)
        {
            MenuItem imageDisplayModeMenuItem = (MenuItem)sender;
            annotationViewer1.DisplayMode = (ImageViewerDisplayMode)imageDisplayModeMenuItem.Tag;
            UpdateUI();
        }

        /// <summary>
        /// Sets an image size mode.
        /// </summary>
        private void imageSizeMode_Click(object sender, RoutedEventArgs e)
        {
            // disable previously checked menu
            _imageScaleModeSelectedMenuItem.IsChecked = false;

            //
            MenuItem item = (MenuItem)sender;
            switch (item.Header.ToString())
            {
                case "Normal":
                    annotationViewer1.SizeMode = ImageSizeMode.Normal;
                    break;
                case "Best fit":
                    annotationViewer1.SizeMode = ImageSizeMode.BestFit;
                    break;
                case "Fit to width":
                    annotationViewer1.SizeMode = ImageSizeMode.FitToWidth;
                    break;
                case "Fit to height":
                    annotationViewer1.SizeMode = ImageSizeMode.FitToHeight;
                    break;
                case "Pixel to Pixel":
                    annotationViewer1.SizeMode = ImageSizeMode.PixelToPixel;
                    break;
                case "Scale":
                    annotationViewer1.SizeMode = ImageSizeMode.Zoom;
                    break;
                case "25%":
                    annotationViewer1.SizeMode = ImageSizeMode.Zoom;
                    annotationViewer1.Zoom = 25;
                    break;
                case "50%":
                    annotationViewer1.SizeMode = ImageSizeMode.Zoom;
                    annotationViewer1.Zoom = 50;
                    break;
                case "100%":
                    annotationViewer1.SizeMode = ImageSizeMode.Zoom;
                    annotationViewer1.Zoom = 100;
                    break;
                case "200%":
                    annotationViewer1.SizeMode = ImageSizeMode.Zoom;
                    annotationViewer1.Zoom = 200;
                    break;
                case "400%":
                    annotationViewer1.SizeMode = ImageSizeMode.Zoom;
                    annotationViewer1.Zoom = 400;
                    break;
            }

            _imageScaleModeSelectedMenuItem = item;
            _imageScaleModeSelectedMenuItem.IsChecked = true;
        }

        /// <summary>
        /// Rotates images in both annotation viewer and thumbnail viewer by 90 degrees clockwise.
        /// </summary>
        private void rotateClockwiseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RotateViewClockwise();
        }

        /// <summary>
        /// Rotates images in both annotation viewer and thumbnail viewer by 90 degrees counterclockwise.
        /// </summary>
        private void rotateCounterclockwiseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RotateViewCounterClockwise();
        }

        /// <summary>
        /// Rotates images in both annotation viewer and thumbnail viewer by 90 degrees clockwise.
        /// </summary>
        private void RotateViewClockwise()
        {
            if (annotationViewer1.ImageRotationAngle != 270)
            {
                annotationViewer1.ImageRotationAngle += 90;
                thumbnailViewer1.ImageRotationAngle += 90;
            }
            else
            {
                annotationViewer1.ImageRotationAngle = 0;
                thumbnailViewer1.ImageRotationAngle = 0;
            }
        }

        /// <summary>
        /// Rotates images in both annotation viewer and thumbnail viewer by 90 degrees counterclockwise.
        /// </summary>
        private void RotateViewCounterClockwise()
        {
            if (annotationViewer1.ImageRotationAngle != 0)
            {
                annotationViewer1.ImageRotationAngle -= 90;
                thumbnailViewer1.ImageRotationAngle -= 90;
            }
            else
            {
                annotationViewer1.ImageRotationAngle = 270;
                thumbnailViewer1.ImageRotationAngle = 270;
            }
        }


        /// <summary>
        /// Changes settings of annotation viewer.
        /// </summary>
        private void viewerSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ImageViewerSettingsWindow viewerSettingsDialog = new ImageViewerSettingsWindow(annotationViewer1);
            viewerSettingsDialog.Owner = this;
            viewerSettingsDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            viewerSettingsDialog.ShowDialog();
            UpdateUI();
        }

        /// <summary>
        /// Changes rendering settings of annotation viewer.
        /// </summary>
        private void viewerRenderingSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CompositeRenderingSettingsWindow viewerRenderingSettingsDialog = new CompositeRenderingSettingsWindow(annotationViewer1.ImageRenderingSettings);
            viewerRenderingSettingsDialog.Owner = this;
            viewerRenderingSettingsDialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            viewerRenderingSettingsDialog.ShowDialog();
            UpdateUI();
        }

        /// <summary>
        /// Shows settings of interaction area.
        /// </summary>
        private void annotationInteractionPointsSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            WpfInteractionAreaAppearanceManagerWindow window = new WpfInteractionAreaAppearanceManagerWindow();
            window.InteractionAreaSettings = _interactionAreaAppearanceManager;
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            window.ShowDialog();
        }

        /// <summary>
        /// Enables/disables the ability to move annotations between images.
        /// </summary>
        private void moveAnnotationsBetweenImagesMenuItem_CheckedChanged(object sender, RoutedEventArgs e)
        {
            annotationViewer1.CanMoveAnnotationsBetweenImages = moveAnnotationsBetweenImagesMenuItem.IsChecked;
        }

        /// <summary>
        /// Enables/disables usage of bounding box during creation/transformation of annotation.
        /// </summary>
        private void boundAnnotationsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.IsAnnotationBoundingRectEnabled = boundAnnotationsToolStripMenuItem.IsChecked;
        }

        /// <summary>
        /// Edits the spell check settings.
        /// </summary>
        private void enableSpellCheckingMenuItem_CheckChanged(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
                _interactionAreaAppearanceManager.IsSpellCheckingEnabled = enableSpellCheckingMenuItem.IsChecked;
        }

        /// <summary>
        /// Edits the color management settings.
        /// </summary>
        private void colorManagementMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ColorManagementSettingsWindow.EditColorManagement(annotationViewer1);
        }

        /// <summary>
        /// Shows document metadata window.
        /// </summary>
        private void documentMetadataMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DocumentMetadata metadata = annotationViewer1.Image.SourceInfo.Decoder.GetDocumentMetadata();

            if (metadata != null)
            {
                PropertyGridWindow propertyWindow = new PropertyGridWindow(metadata, "Document Metadata");
                propertyWindow.Owner = this;
                propertyWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                propertyWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("File does not contain metadata.", "Message", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion


        #region 'Annotation' menu

        /// <summary>
        /// "Annotations" menu is opening.
        /// </summary>
        private void annotationsMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            if (annotationViewer1.FocusedAnnotationView != null && annotationViewer1.FocusedAnnotationView is WpfLineAnnotationViewBase)
            {
                transformationModeMenuItem.IsEnabled = true;
                UpdateTransformationMenu();
            }
            else
            {
                transformationModeMenuItem.IsEnabled = false;
            }

            UpdateEditMenuItems();
        }

        /// <summary>
        /// Shows information about annotation collections of all images.
        /// </summary>
        private void annotationsInfoMenuItem_Click(object sender, RoutedEventArgs e)
        {
            WpfAnnotationsInfoWindow ai = new WpfAnnotationsInfoWindow(annotationViewer1.AnnotationDataController);
            ai.Owner = this;
            ai.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ai.ShowDialog();
        }


        #region Annotation interaction mode

        /// <summary>
        /// Changes the annotation interaction mode to None.
        /// </summary>
        private void annotationInteractionModeNoneMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.AnnotationInteractionMode = AnnotationInteractionMode.None;
        }

        /// <summary>
        /// Changes the annotation interaction mode to View.
        /// </summary>
        private void annotationInteractionModeViewMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.AnnotationInteractionMode = AnnotationInteractionMode.View;
        }

        /// <summary>
        /// Changes the annotation interaction mode to Author.
        /// </summary>
        private void annotationInteractionModeAuthorMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.AnnotationInteractionMode = AnnotationInteractionMode.Author;
        }

        #endregion


        #region Transformation Mode

        /// <summary>
        /// Updates "Annotations -> Transformation Mode" menu. 
        /// </summary>
        private void UpdateTransformationMenu()
        {
            // transformation mode for focused annotation
            GripMode gripMode = ((WpfLineAnnotationViewBase)annotationViewer1.FocusedAnnotationView).GripMode;
            // update menus
            transformationModeRectangularMenuItem.IsChecked = gripMode == GripMode.Rectangular;
            transformationModePointsMenuItem.IsChecked = gripMode == GripMode.Points;
            transformationModeRectangularAndPointsMenuItem.IsChecked = gripMode == GripMode.RectangularAndPoints;
        }

        /// <summary>
        /// Sets "rectangular" transformation mode for focused annotation.
        /// </summary>
        private void transformationModeRectangularMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((WpfLineAnnotationViewBase)annotationViewer1.FocusedAnnotationView).GripMode = GripMode.Rectangular;
            UpdateTransformationMenu();
        }

        /// <summary>
        /// Sets "points" transformation mode for focused annotation. 
        /// </summary>
        private void transformationModePointsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((WpfLineAnnotationViewBase)annotationViewer1.FocusedAnnotationView).GripMode = GripMode.Points;
            UpdateTransformationMenu();
        }

        /// <summary>
        /// Sets "rectangular and points" transformation mode for focused annotation.
        /// </summary>
        private void transformationModeRectangularAndPointsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ((WpfLineAnnotationViewBase)annotationViewer1.FocusedAnnotationView).GripMode = GripMode.RectangularAndPoints;
            UpdateTransformationMenu();
        }

        #endregion


        #region Load and Save annotations

        /// <summary>
        /// Loads annotation collection from file.
        /// </summary>
        private void loadFromFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IsFileOpening = true;

            AnnotationDemosTools.LoadAnnotationsFromFile(annotationViewer1, _openFileDialog, _undoManager);

            IsFileOpening = false;
        }

        /// <summary>
        /// Saves annotation collection to a file.
        /// </summary>
        private void saveToFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            IsFileSaving = true;

            AnnotationDemosTools.SaveAnnotationsToFile(annotationViewer1, _saveFileDialog);

            IsFileSaving = false;
        }

        #endregion


        /// <summary>
        /// Starts building of annotation.
        /// </summary>
        private void addAnnotationMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AnnotationType annotationType = _menuItemToAnnotationType[(MenuItem)sender];

            // start new annotation building process and specify that this is the first process
            annotationsToolBar.AddAndBuildAnnotation(annotationType);
        }

        /// <summary>
        /// Enables/disables the continuous building of annotations.
        /// </summary>
        private void buildAnnotationsContinuouslyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            buildAnnotationsContinuouslyMenuItem.IsChecked ^= true;
            annotationsToolBar.NeedBuildAnnotationsContinuously = buildAnnotationsContinuouslyMenuItem.IsChecked;
        }


        #region UI actions

        /// <summary>
        /// Brings the selected annotation to the first position in annotation collection.
        /// </summary>
        private void bringToBackMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.CancelAnnotationBuilding();

            annotationViewer1.BringSelectedAnnotationToBack();

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Brings the selected annotation to the last position in annotation collection.
        /// </summary>
        private void bringToFrontMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.CancelAnnotationBuilding();

            annotationViewer1.BringSelectedAnnotationToFront();

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Enables/disables multi selection of annotations in viewer.
        /// </summary>
        private void multiSelectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            annotationViewer1.AnnotationMultiSelect = multiSelectMenuItem.IsChecked;
            UpdateUI();
        }

        /// <summary>
        /// Groups/ungroups selected annotations of annotation collection.
        /// </summary>
        private void groupSelectedMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AnnotationDemosTools.GroupUngroupSelectedAnnotations(annotationViewer1, _undoManager);
        }

        /// <summary>
        /// Groups all annotations of annotation collection.
        /// </summary>
        private void groupAllMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AnnotationDemosTools.GroupAllAnnotations(annotationViewer1, _undoManager);
        }


        #region Rotate, Burn, Clone

        /// <summary>
        /// Rotates image with annotations.
        /// </summary>
        private void rotateImageWithAnnotationsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AnnotationDemosTools.RotateImageWithAnnotations(annotationViewer1, _undoManager, this);
            }
            catch (Exception exc)
            {
                DemosTools.ShowErrorMessage(exc);
            }
        }

        /// <summary>
        /// Burns an annotation collection on image.
        /// </summary>
        private void burnAnnotationsOnImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!AnnotationDemosTools.CheckImage(annotationViewer1))
                return;

            Cursor currentCursor = Cursor;

            try
            {
                Cursor = Cursors.Wait;
                AnnotationDemosTools.BurnAnnotationsOnImage(annotationViewer1, _undoManager, null);
                UpdateUI();
            }
            catch (ImageProcessingException ex)
            {
                Cursor = currentCursor;
                MessageBox.Show(ex.Message, "Burn annotations on image", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception exc)
            {
                Cursor = currentCursor;
                DemosTools.ShowErrorMessage(exc);
            }
            Cursor = currentCursor;
        }

        /// <summary>
        /// Clones image with annotations.
        /// </summary>
        private void cloneImageWithAnnotationsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                annotationViewer1.CancelAnnotationBuilding();

                annotationViewer1.AnnotationDataController.CloneImageWithAnnotations(annotationViewer1.FocusedIndex, annotationViewer1.Images.Count);
                annotationViewer1.FocusedIndex = annotationViewer1.Images.Count - 1;
            }
            catch (Exception exc)
            {
                DemosTools.ShowErrorMessage(exc);
            }
        }

        #endregion

        #endregion

        #endregion


        #region 'Help' menu

        /// <summary>
        /// Shows the About dialog.
        /// </summary>
        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder description = new StringBuilder();

            description.AppendLine("This project demonstrates the following SDK capabilities: ");
            description.AppendLine();
            description.AppendLine("- Load documents(DOC, DOCX, XLS, XLSX, PDF) and images(BMP, CUR, EMF, GIF, ICO, JBIG2, JPEG, JPEG2000, JPEG-LS, PCX, PNG, TIFF, BigTIFF, WMF, RAW) from file.");
            description.AppendLine();
            description.AppendLine("- Acquire images from scanner.");
            description.AppendLine();
            description.AppendLine("- Display and print loaded documents and images.");
            description.AppendLine();
            description.AppendLine("- Annotate loaded documents and images: add / edit / copy / paste / cut / delete annotation, burn annotations on image.");
            description.AppendLine();
            description.AppendLine("- Use 20+ predefined annotation types.");
            description.AppendLine();
            description.AppendLine("- Extract text from loaded documents.");
            description.AppendLine();
            description.AppendLine("- Search text in loaded documents.");
            description.AppendLine();
            description.AppendLine("- Change settings for image(s) and thumbnail(s) preview.");
            description.AppendLine();
            description.AppendLine("- Use visual tools in viewer: selection, magnifier, zoom, pan, scroll.");
            description.AppendLine();
            description.AppendLine("- Save annotated documents and images to a PDF, TIFF, JPEG or PNG file.");
            description.AppendLine();
            description.AppendLine();
            description.AppendLine("The project is available in C# and VB.NET for Visual Studio .NET.");

            WpfAboutBoxBaseWindow dlg = new WpfAboutBoxBaseWindow("vsannotation-dotnet");
            dlg.Description = description.ToString();
            dlg.Owner = this;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.ShowDialog();
        }

        #endregion


        #region Context menu

        /// <summary>
        /// Saves focused image with annotations to a file.
        /// </summary>
        private void saveImageWithAnnotationsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveImageToNewFile();
        }

        /// <summary>
        /// Copies focused image with annotations to clipboard.
        /// </summary>
        private void copyImageToClipboardMenuItem_Click(object sender, RoutedEventArgs e)
        {
            AnnotationDemosTools.CopyImageToClipboard(annotationViewer1);
        }

        /// <summary>
        /// Deletes focused image.
        /// </summary>
        private void deleteImageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteImages();

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Pastes annotation in mouse position.
        /// </summary>
        private void pasteAnnotationInMousePositionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // get mouse position on image in DIP
            Point mousePositionOnImageInDip = annotationViewer1.PointFromControlToDip(_contextMenuPosition);

            annotationViewer1.PasteAnnotationsFromClipboard(mousePositionOnImageInDip);
        }

        /// <summary>
        /// The annotation context menu is opened.
        /// </summary>
        private void annotationMenu_Opened(object sender, RoutedEventArgs e)
        {
            _contextMenuPosition = Mouse.GetPosition(annotationViewer1);
        }

        #endregion


        #region Annotation viewer

        /// <summary>
        /// The mouse is moved in annotation viewer.
        /// </summary>
        private void annotationViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // if viewer must be scrolled when annotation is moved
            if (scrollViewerWhenAnnotationIsMovedMenuItem.IsChecked)
            {
                // if left mouse button is pressed
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    // get the interaction controller of annotation viewer
                    IWpfInteractionController interactionController =
                        annotationViewer1.AnnotationVisualTool.ActiveInteractionController;
                    // if user interacts with annotation
                    if (interactionController != null && interactionController.IsInteracting)
                    {
                        const int delta = 20;

                        // get the "visible area" of annotation viewer
                        Rect rect = new Rect(0, 0,
                                             annotationViewer1.ViewportWidth,
                                             annotationViewer1.ViewportHeight);
                        // remove "border" from the "visible area"
                        rect.Inflate(-delta, -delta);
                        // get the mouse location
                        Point mousePosition = e.GetPosition(annotationViewer1);

                        // if mouse is located in "border"
                        if (!rect.Contains(mousePosition))
                        {
                            // calculate how to scroll the annotation viewer
                            double deltaX = 0;
                            if (mousePosition.X < delta)
                                deltaX = -(delta - mousePosition.X);
                            if (mousePosition.X > delta + rect.Width)
                                deltaX = -(delta + rect.Width - mousePosition.X);
                            double deltaY = 0;
                            if (mousePosition.Y < delta)
                                deltaY = -(delta - mousePosition.Y);
                            if (mousePosition.Y > delta + rect.Height)
                                deltaY = -(delta + rect.Height - mousePosition.Y);

                            // get the auto scroll position of annotation viewer
                            Point autoScrollPosition = new Point(
                                Math.Abs(annotationViewer1.HorizontalOffset),
                                Math.Abs(annotationViewer1.VerticalOffset));

                            // calculate new auto scroll position
                            if (annotationViewer1.ViewerState.AutoScrollSize.Width > 0 && deltaX != 0)
                                autoScrollPosition.X += deltaX;
                            if (annotationViewer1.ViewerState.AutoScrollSize.Height > 0 && deltaY != 0)
                                autoScrollPosition.Y += deltaY;

                            // if auto scroll position is changed
                            if (autoScrollPosition != annotationViewer1.ViewerState.AutoScrollPosition)
                                // set new auto scroll position
                                annotationViewer1.ViewerState.AutoScrollPosition = autoScrollPosition;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The scroll position of the annotation viewer is changing.
        /// </summary>
        private void annotationViewer_AutoScrollPositionExChanging(object sender, PropertyChangingEventArgs<Point> e)
        {
            // if viewer must be scrolled when annotation is moved
            if (scrollViewerWhenAnnotationIsMovedMenuItem.IsChecked)
            {
                // get the interaction controller of annotation viewer
                IWpfInteractionController interactionController =
                    annotationViewer1.AnnotationVisualTool.ActiveInteractionController;
                // if user interacts with annotation
                if (interactionController != null && interactionController.IsInteracting)
                {
                    // get bounding box of displayed images
                    Rect displayedImagesBBox = annotationViewer1.GetDisplayedImagesBoundingBox();

                    // get the scroll position
                    Point scrollPosition = e.NewValue;

                    // cut the coordinates for getting coordinates inside the focused image
                    scrollPosition.X = Math.Max(displayedImagesBBox.X, Math.Min(scrollPosition.X, displayedImagesBBox.Right));
                    scrollPosition.Y = Math.Max(displayedImagesBBox.Y, Math.Min(scrollPosition.Y, displayedImagesBBox.Bottom));

                    // update the scroll position
                    e.NewValue = scrollPosition;
                }
            }
        }

        /// <summary>
        /// Occurs when visual tool throws an exception.
        /// </summary>
        private void annotationViewer_VisualToolException(
            object sender,
            Vintasoft.Imaging.ExceptionEventArgs e)
        {
            DemosTools.ShowErrorMessage(e.Exception);
        }

        /// <summary>
        /// Annotation deserialization error occurs.
        /// </summary>
        private void AnnotationDataController_AnnotationDataDeserializationException(object sender, Vintasoft.Imaging.Annotation.AnnotationDataDeserializationExceptionEventArgs e)
        {
            DemosTools.ShowErrorMessage("AnnotationData deserialization exception", e.Exception);
        }

        /// <summary>
        /// Image loading in viewer is started.
        /// </summary>
        private void annotationViewer_ImageLoading(object sender, ImageLoadingEventArgs e)
        {
            imageLoadingProgressBar.Visibility = Visibility.Visible;
            statusLabelLoadingImage.Visibility = Visibility.Visible;
            _imageLoadingStartTime = DateTime.Now;
        }

        /// <summary>
        /// Image loading in viewer is in progress.
        /// </summary>
        private void annotationViewer_ImageLoadingProgress(object sender, ProgressEventArgs e)
        {
            if (_isWindowClosing)
            {
                e.Cancel = true;
                return;
            }
            imageLoadingProgressBar.Value = e.Progress;
        }

        /// <summary>
        /// Image loading in viewer is finished.
        /// </summary>
        private void annotationViewer_ImageLoaded(object sender, ImageLoadedEventArgs e)
        {
            _imageLoadingTime = DateTime.Now.Subtract(_imageLoadingStartTime);

            imageLoadingProgressBar.Visibility = Visibility.Collapsed;
            statusLabelLoadingImage.Visibility = Visibility.Collapsed;

            VintasoftImage image = annotationViewer1.Image;

            // show error message if not critical error occurs during image loading
            string imageLoadingErrorString = "";
            if (image.LoadingError)
                imageLoadingErrorString = string.Format("[{0}] ", image.LoadingErrorString);
            // show information about the image
            imageInfoStatusLabel.Text = string.Format("{0} Width={1}; Height={2}; PixelFormat={3}; Resolution={4}",
                imageLoadingErrorString, image.Width, image.Height, image.PixelFormat, image.Resolution);

            // if image loading time more than 0
            if (_imageLoadingTime != TimeSpan.Zero)
                // show information about image loading time
                imageInfoStatusLabel.Text = string.Format("[Loading time: {0}ms] {1}", _imageLoadingTime.TotalMilliseconds, imageInfoStatusLabel.Text);

            // if image has annotations
            if (image.Metadata.AnnotationsFormat != AnnotationsFormat.None)
                // show information about format of annotations
                imageInfoStatusLabel.Text = string.Format("[AnnotationsFormat: {0}] {1}", image.Metadata.AnnotationsFormat, imageInfoStatusLabel.Text);

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Key is down in annotation viewer.
        /// </summary>
        private void annotationViewer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!CanInteractWithFocusedAnnotationUseKeyboard())
                return;

            // if Enter key (13) pressed
            if (e.Key == Key.Enter)
            {
                if (annotationViewer1.IsAnnotationBuilding)
                    annotationViewer1.FinishAnnotationBuilding();
            }
            // if ESC key (27) pressed
            else if (e.Key == Key.Escape)
            {
                if (annotationViewer1.IsAnnotationBuilding)
                    annotationViewer1.CancelAnnotationBuilding();
                else
                    annotationViewer1.AnnotationVisualTool.CancelActiveInteraction();
            }
            else if (annotationViewer1.IsFocused &&
                     annotationViewer1.FocusedAnnotationView != null)
            {
                // get transform from AnnotationViewer space to DIP space
                AffineMatrix matrix = annotationViewer1.GetTransformFromVisualToolToDip();
                System.Drawing.PointF deltaVector = PointFAffineTransform.TransformVector(
                    matrix,
                    new System.Drawing.PointF(ANNOTATION_KEYBOARD_MOVE_DELTA, ANNOTATION_KEYBOARD_MOVE_DELTA));

                System.Drawing.PointF resizeVector = PointFAffineTransform.TransformVector(
                    matrix,
                    new System.Drawing.PointF(ANNOTATION_KEYBOARD_RESIZE_DELTA, ANNOTATION_KEYBOARD_RESIZE_DELTA));

                // current annotation location 
                Point location = annotationViewer1.FocusedAnnotationView.Location;
                Size size = annotationViewer1.FocusedAnnotationView.Size;

                switch (e.Key)
                {
                    case Key.Up:
                        annotationViewer1.FocusedAnnotationView.Location = new Point(location.X, location.Y - deltaVector.Y);
                        e.Handled = true;
                        break;
                    case Key.Down:
                        annotationViewer1.FocusedAnnotationView.Location = new Point(location.X, location.Y + deltaVector.Y);
                        e.Handled = true;
                        break;
                    case Key.Right:
                        annotationViewer1.FocusedAnnotationView.Location = new Point(location.X + deltaVector.X, location.Y);
                        e.Handled = true;
                        break;
                    case Key.Left:
                        annotationViewer1.FocusedAnnotationView.Location = new Point(location.X - deltaVector.X, location.Y);
                        e.Handled = true;
                        break;
                    case Key.Add:
                        annotationViewer1.FocusedAnnotationView.Size = new Size(size.Width + resizeVector.X, size.Height + resizeVector.Y);
                        e.Handled = true;
                        break;
                    case Key.Subtract:
                        if (size.Width > resizeVector.X)
                            annotationViewer1.FocusedAnnotationView.Size = new Size(size.Width - resizeVector.X, size.Height);

                        size = annotationViewer1.FocusedAnnotationView.Size;

                        if (size.Height > resizeVector.Y)
                            annotationViewer1.FocusedAnnotationView.Size = new Size(size.Width, size.Height - resizeVector.Y);
                        e.Handled = true;
                        break;
                }
                annotationDataPropertyGrid.Refresh();
            }
        }

        /// <summary>
        /// Determines whether can move focused annotation use keyboard.
        /// </summary>
        private bool CanInteractWithFocusedAnnotationUseKeyboard()
        {
            if (annotationViewer1.FocusedAnnotationView == null)
                return false;

#if !REMOVE_OFFICE_PLUGIN
            WpfOfficeDocumentVisualEditor documentEditor = WpfUserInteractionVisualTool.GetActiveInteractionController<WpfOfficeDocumentVisualEditor>(annotationViewer1.VisualTool);
            if (documentEditor != null && documentEditor.IsEditingEnabled)
            {
                return false;
            }
#endif
            return true;
        }

        /// <summary>
        /// Annotation interaction mode of viewer is changed.
        /// </summary>
        private void annotationViewer_AnnotationInteractionModeChanged(object sender, AnnotationInteractionModeChangedEventArgs e)
        {
            annotationInteractionModeNoneMenuItem.IsChecked = false;
            annotationInteractionModeViewMenuItem.IsChecked = false;
            annotationInteractionModeAuthorMenuItem.IsChecked = false;

            AnnotationInteractionMode annotationInteractionMode = e.NewValue;
            switch (annotationInteractionMode)
            {
                case AnnotationInteractionMode.None:
                    annotationInteractionModeNoneMenuItem.IsChecked = true;
                    break;

                case AnnotationInteractionMode.View:
                    annotationInteractionModeViewMenuItem.IsChecked = true;
                    break;

                case AnnotationInteractionMode.Author:
                    annotationInteractionModeAuthorMenuItem.IsChecked = true;
                    break;
            }

            // update the UI
            UpdateUI();
        }

        #endregion


        #region Thumbnail viewer

        /// <summary>
        /// Loading of thumbnails is in progress.
        /// </summary>
        private void thumbnailViewer_ThumbnailLoadingProgress(object sender, ProgressEventArgs e)
        {
            actionLabel.Content = "Creating thumbnails:";
            thumbnailLoadingProgerssBar.Value = e.Progress;
            thumbnailLoadingProgerssBar.Visibility = Visibility.Visible;
            actionLabel.Visibility = Visibility.Visible;
            if (thumbnailLoadingProgerssBar.Value == 100)
            {
                thumbnailLoadingProgerssBar.Visibility = Visibility.Collapsed;
                actionLabel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Initializes thumbnail viewer context menu.
        /// </summary>
        private void thumbnailViewer_ThumbnailAdded(object sender, ThumbnailImageItemEventArgs e)
        {
            e.Thumbnail.MouseEnter += Thumbnail_MouseEnter;
        }

        /// <summary>
        /// Handles the MouseEnter event of the ThumbnailImageItem control.
        /// </summary>
        private void Thumbnail_MouseEnter(object sender, MouseEventArgs e)
        {
            ThumbnailImageItem thumbnail = (ThumbnailImageItem)sender;
            if (thumbnail.ContextMenu == null)
            {
                ContextMenu thumbnailContextMenu = new ContextMenu();

                MenuItem saveImageWithAnnotationsItem = new MenuItem();
                saveImageWithAnnotationsItem.Header = "Save image with annotations...";
                saveImageWithAnnotationsItem.Click += new RoutedEventHandler(saveImageWithAnnotationsMenuItem_Click);
                thumbnailContextMenu.Items.Add(saveImageWithAnnotationsItem);

                MenuItem burnAnnotationsItem = new MenuItem();
                burnAnnotationsItem.Header = "Burn annotations on image";
                burnAnnotationsItem.Click += new RoutedEventHandler(burnAnnotationsOnImageMenuItem_Click);
                thumbnailContextMenu.Items.Add(burnAnnotationsItem);

                MenuItem copyImageItem = new MenuItem();
                copyImageItem.Header = "Copy image to clipboard";
                copyImageItem.Click += new RoutedEventHandler(copyImageToClipboardMenuItem_Click);
                thumbnailContextMenu.Items.Add(copyImageItem);

                MenuItem deleteImageItem = new MenuItem();
                deleteImageItem.Header = "Delete image(s)";
                deleteImageItem.Click += new RoutedEventHandler(deleteImageMenuItem_Click);
                thumbnailContextMenu.Items.Add(deleteImageItem);

                thumbnail.ContextMenu = thumbnailContextMenu;
            }
        }


        /// <summary>
        /// Sets the ToolTip of hovered thumbnail.
        /// </summary>
        private void thumbnailViewer_HoveredThumbnailChanged(object sender, RoutedPropertyChangedEventArgs<ThumbnailImageItem> e)
        {
            ThumbnailImageItem thumbnailImage = (ThumbnailImageItem)e.NewValue;
            if (thumbnailImage != null)
            {
                try
                {
                    // get information about hovered image in thumbnail viewer
                    ImageSourceInfo imageSourceInfo = thumbnailImage.Source.SourceInfo;
                    string filename = null;

                    // if image loaded from file
                    if (imageSourceInfo.SourceType == ImageSourceType.File)
                    {
                        // get image file name
                        filename = Path.GetFileName(imageSourceInfo.Filename);
                    }
                    // if image loaded from stream
                    else if (imageSourceInfo.SourceType == ImageSourceType.Stream)
                    {
                        // if stream is file stream
                        if (imageSourceInfo.Stream is FileStream)
                            // get image file name
                            filename = Path.GetFileName(((FileStream)imageSourceInfo.Stream).Name);
                    }
                    // if image is new image
                    else
                    {
                        filename = "Bitmap";
                    }

                    // if image is multipage image
                    if (imageSourceInfo.PageCount > 1)
                        thumbnailImage.ToolTip = string.Format("{0}, page {1}", filename, imageSourceInfo.PageIndex + 1);
                    else
                        thumbnailImage.ToolTip = filename;
                }
                catch
                {
                    thumbnailImage.ToolTip = "";
                }
            }
        }

        #endregion


        #region Annotations's combobox AND annotation's property grid

        /// <summary>
        /// Fills combobox with information about annotations of image.
        /// </summary>
        private void FillAnnotationComboBox()
        {
            annotationComboBox.Items.Clear();

            if (annotationViewer1.FocusedIndex >= 0)
            {
                AnnotationDataCollection annotations = annotationViewer1.AnnotationDataController[annotationViewer1.FocusedIndex];
                for (int i = 0; i < annotations.Count; i++)
                {
                    annotationComboBox.Items.Add(string.Format("[{0}] {1}", i, annotations[i].GetType().Name));
                    if (annotationViewer1.FocusedAnnotationData == annotations[i])
                        annotationComboBox.SelectedIndex = i;
                }
            }
        }

        /// <summary>
        /// Shows information about annotation in property grid.
        /// </summary>
        private void ShowAnnotationProperties(WpfAnnotationView annotation)
        {
            AnnotationData data = null;
            if (annotation != null)
                data = annotation.Data;
            if (annotationDataPropertyGrid.SelectedObject != data)
                annotationDataPropertyGrid.SelectedObject = data;
            else if (!_isAnnotationTransforming)
                annotationDataPropertyGrid.Refresh();
        }

        /// <summary>
        /// Handler of the DropDown event of the ComboBox of annotations.
        /// </summary>
        private void annotationComboBox_DropDownOpened(object sender, EventArgs e)
        {
            FillAnnotationComboBox();
        }

        /// <summary>
        /// Selected annotation is changed using annotation's combobox.
        /// </summary>
        private void annotationComboBox_SelectedIndexChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (annotationViewer1.FocusedIndex != -1 && annotationComboBox.SelectedIndex != -1)
            {
                annotationViewer1.FocusedAnnotationData = annotationViewer1.AnnotationDataCollection[annotationComboBox.SelectedIndex];
            }
        }

        /// <summary>
        /// Selected annotation is changed in annotation viewer.
        /// </summary>
        private void annotationViewer_SelectedAnnotationViewChanged(object sender, WpfAnnotationViewChangedEventArgs e)
        {
            FillAnnotationComboBox();
            ShowAnnotationProperties(annotationViewer1.FocusedAnnotationView);

            // update the UI
            UpdateUI();
        }

        /// <summary>
        /// Collection of selected annotations is changed.
        /// </summary>
        private void SelectedAnnotations_Changed(object sender, EventArgs e)
        {
            // update the UI
            UpdateUI();
        }

        #endregion


        #region File manipulation

        /// <summary>
        /// Adds the TXT file filter to the Open file dialog.
        /// </summary>
        /// <param name="dialog">The open file dialog.</param>
        private void AddTxtFileFilterToOpenFileDialog(OpenFileDialog dialog)
        {
            try
            {
                int test = dialog.Filter.IndexOf(new String("|All Image Files|".ToCharArray()));
                dialog.Filter = dialog.Filter.Insert(test, "|TXT Files|*.txt");
                dialog.Filter += "*.txt;";
                dialog.FilterIndex++;
            }
            catch
            {
            }
        }

        /// <summary>
        /// Opens file stream and adds stream to the image collection of annotation viewer.
        /// </summary>
        /// <param name="filename">Source file.</param>
        private void OpenFile(string filename)
        {
            // close the previosly opened file
            CloseCurrentFile();

            // file, that is being opened, will be a new source
            _isSourceChanging = true;

            // save the source filename
            _sourceFilename = Path.GetFullPath(filename);

            // if source file is text file
            if (Path.GetExtension(filename).ToUpperInvariant() == ".TXT")
            {
                try
                {
                    // create stream that contains DOCX document created from text file
                    Stream stream = OfficeDemosTools.ConvertTxtFileToDocxDocument(filename);

                    if (stream != null)
                    {
                        // add created DOCX document to the viewer
                        _imagesManager.Add(stream, true, filename);
                    }
                }
                catch (Exception ex)
                {
                    string message = string.Format("Cannot open {0} : {1}", Path.GetFileName(filename), ex.Message);
                    DemosTools.ShowErrorMessage(message);
                }
            }
            else
            {
                // check the source file for read-write access
                CheckSourceFileForReadWriteAccess();

                // add the source file to the viewer
                _imagesManager.Add(filename, _isFileReadOnlyMode);
            }
        }

        /// <summary>
        /// Adds files to the image collection of annotation viewer.
        /// </summary>
        /// <param name="filenames">The names of files.</param>
        private void AddFiles(string[] filenames)
        {
            foreach (string filename in filenames)
            {
                // if source file is text file
                if (Path.GetExtension(filename).ToUpperInvariant() == ".TXT")
                {
                    try
                    {
                        // create stream that contains DOCX document created from text file
                        Stream stream = OfficeDemosTools.ConvertTxtFileToDocxDocument(filename);

                        if (stream != null)
                        {
                            // add created DOCX document to the viewer
                            _imagesManager.Add(stream, true, filename);
                        }
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("Cannot open {0} : {1}", Path.GetFileName(filename), ex.Message);
                        DemosTools.ShowErrorMessage(message);
                    }
                }
                else
                {
                    // add the source file to the viewer
                    _imagesManager.Add(filename);
                }
            }
        }

        /// <summary>
        /// Closes current file.
        /// </summary>
        private void CloseCurrentFile()
        {
            _imagesManager.Cancel();

            _isFileReadOnlyMode = false;
            _sourceFilename = null;

            annotationViewer1.Images.ClearAndDisposeItems();
        }

        /// <summary>
        /// Checks the source file for read-write access.
        /// </summary>
        private void CheckSourceFileForReadWriteAccess()
        {
            _isFileReadOnlyMode = false;
            Stream stream = null;
            try
            {
                stream = new FileStream(_sourceFilename, FileMode.Open, FileAccess.ReadWrite);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            if (stream == null)
            {
                _isFileReadOnlyMode = true;
            }
            else
            {
                stream.Close();
                stream.Dispose();
            }
        }

        /// <summary>
        /// Handler of the ImageViewerImagesManager.AddStarting event.
        /// </summary>
        private void ImagesManager_AddStarting(object sender, EventArgs e)
        {
            IsFileOpening = true;
        }

        /// <summary>
        /// Handler of the ImageViewerImagesManager.ImageSourceAddStarting event.
        /// </summary>
        private void ImagesManager_ImageSourceAddStarting(object sender, ImageSourceEventArgs e)
        {
            // update window title
            string fileState = string.Format("Opening {0}...", Path.GetFileName(e.SourceFilename));
            Title = string.Format(_titlePrefix, fileState);
        }

        /// <summary>
        /// Handler of the ImageViewerImagesManager.ImageSourceAddFinished event.
        /// </summary>
        private void ImagesManager_ImageSourceAddFinished(object sender, ImageSourceEventArgs e)
        {
            // if source is changed
            if (_isSourceChanging)
                _isSourceChanging = false;
        }

        /// <summary>
        /// Handler of the ImageViewerImagesManager.AddFinished event.
        /// </summary>
        private void ImagesManager_AddFinished(object sender, EventArgs e)
        {
            IsFileOpening = false;
            _isSourceChanging = false;
        }

        /// <summary>
        /// Handler of the ImageViewerImagesManager.ImageSourceAddException event.
        /// </summary>
        private void ImagesManager_ImageSourceAddException(object sender, ImageSourceExceptionEventArgs e)
        {
            // show error message
            string message = string.Format("Cannot open {0} : {1}", Path.GetFileName(e.SourceFilename), e.Exception.Message);
            DemosTools.ShowErrorMessage(message);

            // if new source failed to set, close file
            if (_isSourceChanging)
                CloseCurrentFile();
        }

        #endregion


        #region Image manipulation

        /// <summary>
        /// Deletes selected images or focused image.
        /// </summary>
        private void DeleteImages()
        {
            // get an array of selected images
            VintasoftImage[] selectedImages = new VintasoftImage[thumbnailViewer1.SelectedThumbnails.Count];

            // if selection is present
            if (selectedImages.Length > 0)
            {
                for (int i = 0; i < selectedImages.Length; i++)
                    selectedImages[i] = thumbnailViewer1.SelectedThumbnails[i].Source;
            }
            // if selection is not present
            else
            {
                int focusedIndex = thumbnailViewer1.FocusedIndex;
                // if there is no focused image
                if (focusedIndex == -1)
                    return;
                // if there is focused image
                selectedImages = new VintasoftImage[1];
                selectedImages[0] = thumbnailViewer1.Thumbnails[focusedIndex].Source;
            }

            // remove selected images from the image collection
            for (int i = 0; i < selectedImages.Length; i++)
                thumbnailViewer1.Images.Remove(selectedImages[i]);

            // dispose selected images
            for (int i = 0; i < selectedImages.Length; i++)
                selectedImages[i].Dispose();

            imageInfoStatusLabel.Text = "";
        }

        #endregion


        #region Annotation

        /// <summary>
        /// Updates information about selected thumbnails.
        /// </summary>
        private void thumbnailViewer_SelectedThumbnailsChanged(object sender, RoutedEventArgs e)
        {
            if (thumbnailViewer1.SelectedThumbnails.Count > 0)
                imageInfoStatusLabel.Text = string.Format("Selected {0} thumbnails", thumbnailViewer1.SelectedThumbnails.Count);
            else
                imageInfoStatusLabel.Text = "";
        }

       
        /// <summary>
        /// Begins initialization of the specified annotation.
        /// </summary>
        private void BeginInit(AnnotationData annotation)
        {
            if (!_initializedAnnotations.Contains(annotation))
            {
                _initializedAnnotations.Add(annotation);
                annotation.BeginInit();
            }
        }

        /// <summary>
        /// Ends initialization of the specified annotation.
        /// </summary>
        private void EndInit(AnnotationData annotation)
        {
            if (_initializedAnnotations.Contains(annotation))
            {
                _initializedAnnotations.Remove(annotation);
                annotation.EndInit();
            }
        }

        /// <summary>
        /// Annotation transforming is started.
        /// </summary>
        private void annotationViewer_AnnotationTransformingStarted(
            object sender,
            WpfAnnotationViewEventArgs e)
        {
            _isAnnotationTransforming = true;

            // begin the initialization of annotation
            BeginInit(e.AnnotationView.Data);
            // for each view of annotation
            foreach (WpfAnnotationView view in annotationViewer1.SelectedAnnotations)
                // begin the initialization of annotation view
                BeginInit(view.Data);
        }



        /// <summary>
        /// Annotation transforming is finished.
        /// </summary>
        private void annotationViewer_AnnotationTransformingFinished(
            object sender,
            WpfAnnotationViewEventArgs e)
        {
            _isAnnotationTransforming = false;

            // end the initialization of annotation
            EndInit(e.AnnotationView.Data);
            // for each view of annotation
            foreach (WpfAnnotationView view in annotationViewer1.SelectedAnnotations)
                // end the initialization of annotation view
                EndInit(view.Data);

            // refresh the property grid
            annotationDataPropertyGrid.Refresh();
        }

        /// <summary>
        /// Deletes the selected annotation or all annotations from image.
        /// </summary>
        /// <param name="deleteAll">Determines that all annotations must be deleted from image.</param>
        private void DeleteAnnotation(bool deleteAll)
        {
            annotationViewer1.CancelAnnotationBuilding();

            // get UI action
            UIAction deleteUIAction = null;
            if (deleteAll)
                deleteUIAction = GetUIAction<DeleteAllItemsUIAction>(annotationViewer1.VisualTool);
            else
                deleteUIAction = GetUIAction<DeleteItemUIAction>(annotationViewer1.VisualTool);

            // if UI action is not empty  AND UI action is enabled
            if (deleteUIAction != null && deleteUIAction.IsEnabled)
            {
                string actionName = "AnnotationViewCollection: Delete";
                if (deleteAll)
                    actionName = actionName + " All";
                _undoManager.BeginCompositeAction(actionName);

                try
                {
                    deleteUIAction.Execute();
                }
                finally
                {
                    _undoManager.EndCompositeAction();
                }
            }

            UpdateUI();
        }

        /// <summary>
        /// Annotation building is started.
        /// </summary>
        private void annotationViewer_AnnotationBuildingStarted(object sender, WpfAnnotationViewEventArgs e)
        {
            annotationComboBox.IsEnabled = false;

            DisableUndoRedoMenu();
            if (_historyWindow != null)
                _historyWindow.CanNavigateOnHistory = false;
        }

        /// <summary>
        /// Annotation building is canceled.
        /// </summary>
        private void annotationViewer_AnnotationBuildingCanceled(object sender, WpfAnnotationViewEventArgs e)
        {
            annotationComboBox.IsEnabled = true;

            EnableUndoRedoMenu();
            if (_historyWindow != null)
                _historyWindow.CanNavigateOnHistory = true;
        }

        /// <summary>
        /// Annotation building is finished.
        /// </summary>
        private void annotationViewer_AnnotationBuildingFinished(object sender, WpfAnnotationViewEventArgs e)
        {
            bool isBuildingFinished = true;

            if (annotationsToolBar.NeedBuildAnnotationsContinuously)
            {
                if (annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding)
                    isBuildingFinished = false;
            }

            if (isBuildingFinished)
            {
                annotationComboBox.IsEnabled = true;

                EnableUndoRedoMenu();
                if (_historyWindow != null)
                    _historyWindow.CanNavigateOnHistory = true;
            }

            ShowAnnotationProperties(annotationViewer1.FocusedAnnotationView);
        }

        /// <summary>
        /// Disables the comment visual tool.
        /// </summary>
        private void NoneAction_Deactivated(object sender, EventArgs e)
        {
            _commentVisualTool.Enabled = false;
        }

        /// <summary>
        /// Enables the comment visual tool.
        /// </summary>
        private void NoneAction_Activated(object sender, EventArgs e)
        {
            _commentVisualTool.Enabled = true;
        }

        #endregion


        #region Annotation undo manager

        /// <summary>
        /// Updates the "Undo/Redo" menu.
        /// </summary>
        private void UpdateUndoRedoMenu(UndoManager undoManager)
        {
            bool canUndo = false;
            bool canRedo = false;

            if (undoManager != null && undoManager.IsEnabled)
            {
                if (!annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding)
                {
                    canUndo = undoManager.UndoCount > 0;
                    canRedo = undoManager.RedoCount > 0;
                }
            }


            string undoMenuItemText = "Undo";
            if (canUndo && !string.IsNullOrEmpty(undoManager.UndoDescription))
                undoMenuItemText = string.Format("Undo {0}", undoManager.UndoDescription).Trim();

            undoMenuItem.IsEnabled = canUndo;
            undoMenuItem.Header = undoMenuItemText;


            string redoMenuItemText = "Redo";
            if (canRedo && !string.IsNullOrEmpty(undoManager.RedoDescription))
                redoMenuItemText = string.Format("Redo {0}", undoManager.RedoDescription).Trim();

            redoMenuItem.IsEnabled = canRedo;
            redoMenuItem.Header = redoMenuItemText;
        }

        /// <summary>
        /// Enables the undo redo menu.
        /// </summary>
        private void EnableUndoRedoMenu()
        {
            UpdateUndoRedoMenu(_undoManager);
            enableUndoRedoMenuItem.IsEnabled = true;
        }

        /// <summary>
        /// Disables the undo redo menu.
        /// </summary>
        private void DisableUndoRedoMenu()
        {
            undoMenuItem.IsEnabled = false;
            redoMenuItem.IsEnabled = false;
            enableUndoRedoMenuItem.IsEnabled = false;
        }

        /// <summary>
        /// Annotation undo manager is changed.
        /// </summary>
        private void annotationUndoManager_Changed(object sender, UndoManagerChangedEventArgs e)
        {
            UpdateUndoRedoMenu((UndoManager)sender);
        }

        /// <summary>
        /// Annotation undo manager is navigated.
        /// </summary>
        private void annotationUndoManager_Navigated(object sender, UndoManagerNavigatedEventArgs e)
        {
            UpdateUndoRedoMenu((UndoManager)sender);
            UpdateUI();
        }

        /// <summary>
        /// Shows the history window.
        /// </summary>
        private void ShowHistoryWindow()
        {
            if (annotationViewer1.Image == null)
                return;

            _historyWindow = new WpfUndoManagerHistoryWindow(this, _undoManager);
            _historyWindow.CanNavigateOnHistory = !annotationViewer1.AnnotationVisualTool.IsFocusedAnnotationBuilding;
            _historyWindow.Closed += new EventHandler(historyWindow_Closed);
            _historyWindow.Show();
        }

        /// <summary>
        /// Closes the history window.
        /// </summary>
        private void CloseHistoryWindow()
        {
            if (_historyWindow != null)
                _historyWindow.Close();
        }

        /// <summary>
        /// History window is closed.
        /// </summary>
        private void historyWindow_Closed(object sender, EventArgs e)
        {
            historyDialogMenuItem.IsChecked = false;
            _historyWindow = null;
        }

        #endregion


        #region Save image(s)

        /// <summary>
        /// Saves modified image collection with annotations back to the source file.
        /// If source file does not support annotations, saves to new file, that 
        /// supports annotations and switches to it.
        /// </summary>
        private void SaveToSourceFile()
        {
            // cancel annotation building
            annotationViewer1.CancelAnnotationBuilding();

            // if focused image is NOT correct
            if (!AnnotationDemosTools.CheckImage(annotationViewer1))
                return;

            // specify that image file saving is started
            IsFileSaving = true;

            try
            {
                // if source file supports annotations
                if (IsAnnotationsSupported(_sourceFilename))
                {
                    PluginsEncoderFactory encoderFactory = new PluginsEncoderFactory();
                    EncoderBase encoder = null;

                    // get the decoder name of the first image in image collection
                    string encoderName = annotationViewer1.Images[0].SourceInfo.DecoderName;

                    // if image encoder is found
                    if (encoderFactory.GetEncoderByName(encoderName, out encoder))
                    {
                        encoder.SaveAndSwitchSource = true;

                        // asynchronously save image collection to a file
                        annotationViewer1.Images.SaveAsync(_sourceFilename, encoder);
                    }
                    // if image encoder is NOT found
                    else
                    {
                        DemosTools.ShowErrorMessage("Images are not saved.");
                        // specify that image file saving is finished
                        IsFileSaving = false;
                    }
                }
                // if source file does NOT support annotations
                else
                    // open the save file dialog and save image collection to a new file
                    SaveImageCollectionToNewFile(true);
            }
            catch (Exception ex)
            {
                DemosTools.ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// Determines that file supports annotations.
        /// </summary>
        /// <param name="fileName">File name with extension.</param>
        /// <returns><b>True</b> if file supports annotations; otherwise, <b>false</b>.</returns>
        private bool IsAnnotationsSupported(string fileName)
        {
            string fileExtension = Path.GetExtension(fileName).ToUpperInvariant();

            switch (fileExtension)
            {
                case ".PDF":
                case ".TIF":
                case ".TIFF":
                case ".PNG":
                case ".JPG":
                case ".JPEG":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Opens the save file dialog and saves image collection to a new file.
        /// </summary>
        private void SaveImageCollectionToNewFile(bool switchSource)
        {
            // cancel annotation building
            annotationViewer1.CancelAnnotationBuilding();

            // if focused image is NOT correct
            if (!AnnotationDemosTools.CheckImage(annotationViewer1))
                return;

            // specify that image file saving is started
            IsFileSaving = true;

            bool multipage = annotationViewer1.Images.Count > 1;

            // set file filters in file saving dialog
            CodecsFileFilters.SetFiltersWithAnnotations(_saveFileDialog, multipage);
            // show the file saving dialog
            if (_saveFileDialog.ShowDialog().Value)
            {
                try
                {
                    string saveFilename = Path.GetFullPath(_saveFileDialog.FileName);

                    PluginsEncoderFactory encoderFactory = new PluginsEncoderFactory();
                    EncoderBase encoder = null;

                    // if image encoder is found
                    if (encoderFactory.GetEncoder(saveFilename, out encoder))
                    {
                        RenderingSettingsWindow.SetRenderingSettingsIfNeed(annotationViewer1.Images, encoder, annotationViewer1.ImageRenderingSettings);

                        _saveFilename = saveFilename;
                        encoder.SaveAndSwitchSource = switchSource;

                        // save image collection to a file
                        annotationViewer1.Images.SaveAsync(saveFilename, encoder);
                    }
                    // if image encoder is NOT found
                    else
                    {
                        DemosTools.ShowErrorMessage("Images are not saved.");
                        // specify that image file saving is finished
                        IsFileSaving = false;
                    }
                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }

                if (!switchSource)
                    // specify that image file saving is finished
                    IsFileSaving = false;
            }
            else
                // specify that image file saving is finished
                IsFileSaving = false;
        }

        /// <summary>
        /// Opens the save file dialog and saves image with annotations to a new file.
        /// </summary>
        private void SaveImageToNewFile()
        {
            // cancel annotation building
            annotationViewer1.CancelAnnotationBuilding();

            // if focused image is NOT correct
            if (!AnnotationDemosTools.CheckImage(annotationViewer1))
                return;

            // specify that image file saving is started
            IsFileSaving = true;

            // set file filters in file saving dialog
            CodecsFileFilters.SetFiltersWithAnnotations(_saveFileDialog, false);
            // show the file saving dialog
            if (_saveFileDialog.ShowDialog().Value)
            {
                try
                {
                    string saveFilename = Path.GetFullPath(_saveFileDialog.FileName);
                    PluginsEncoderFactory encoderFactory = new PluginsEncoderFactory();
                    EncoderBase encoder = null;

                    // if image encoder is found
                    if (encoderFactory.GetEncoder(saveFilename, out encoder))
                    {
                        RenderingSettingsWindow.SetRenderingSettingsIfNeed(annotationViewer1.Image, encoder, annotationViewer1.ImageRenderingSettings);

                        // save image to a file
                        annotationViewer1.Image.Save(saveFilename, encoder, SavingProgress);
                    }
                    // if image encoder is NOT found
                    else
                    {
                        DemosTools.ShowErrorMessage("Images are not saved.");
                    }
                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }
            }

            // specify that image file saving is finished
            IsFileSaving = false;
        }

        /// <summary>
        /// Image collection saving is in-progress.
        /// </summary>
        private void SavingProgress(object sender, ProgressEventArgs e)
        {
            Dispatcher.Invoke(new UpdateSavingProgressDelegate(UpdateSavingProgress), e.Progress);
        }

        /// <summary>
        /// Image collection saving is in-progress.
        /// </summary>
        private void UpdateSavingProgress(int progress)
        {
            actionLabel.Content = "Saving:";
            thumbnailLoadingProgerssBar.Value = progress;
            if (progress != 100)
                thumbnailLoadingProgerssBar.Visibility = Visibility.Visible;
            else
                thumbnailLoadingProgerssBar.Visibility = Visibility.Collapsed;
            actionLabel.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Image collection is saved.
        /// </summary>
        private void images_ImageCollectionSavingFinished(object sender, EventArgs e)
        {
            if (_saveFilename != null)
            {
                _sourceFilename = _saveFilename;
                _saveFilename = null;
                _isFileReadOnlyMode = false;
            }
            IsFileSaving = false;
        }

        /// <summary>
        /// Image saving error occurs.
        /// </summary>
        private void Images_ImageSavingException(object sender, ExceptionEventArgs e)
        {
            DemosTools.ShowErrorMessage(e.Exception);
        }

        #endregion


        #region Text Selection Tool

        /// <summary>
        /// Text selection is changed.
        /// </summary>
        private void TextSelectionTool_SelectionChanged(object sender, EventArgs e)
        {
            UpdateUI();
        }

        /// <summary>
        /// Text searching is started.
        /// </summary>
        private void _textSelectionTool_TextSearching(object sender, EventArgs e)
        {
            IsTextSearching = true;
        }

        /// <summary>
        /// Text search is in progress.
        /// </summary>
        private void _textSelectionTool_TextSearchingProgress(
            object sender,
            TextSearchingProgressEventArgs e)
        {
            actionLabel.Content = string.Format("Search on page {0}...", e.ImageIndex + 1);
            actionLabel.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Text searching is finished.
        /// </summary>
        private void _textSelectionTool_TextSearched(object sender, TextSearchedEventArgs e)
        {
            actionLabel.Content = "";
            actionLabel.Visibility = Visibility.Collapsed;
            IsTextSearching = false;
        }

        /// <summary>
        /// Text extraction is in progress.
        /// </summary>
        private void TextSelectionTool_TextExtractionProgress(object sender, ProgressEventArgs e)
        {
            // show status
            if (e.Progress == 100)
            {
                actionLabel.Content = "";
                actionLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                actionLabel.Visibility = Visibility.Visible;
                actionLabel.Content = string.Format("Extracting text {0}%...", e.Progress);
                DemosTools.DoEvents();
            }
        }

        #endregion


        #region Navigation Tool

        /// <summary>
        /// Shows focused action in the status strip.
        /// </summary>
        private void NavigationTool_FocusedActionChanged(object sender, EventArgs e)
        {
            PageContentActionMetadata action = _navigationTool.FocusedAction;
            if (action != null)
            {
                if (action is UriActionMetadata)
                {
                    actionLabel.Content = string.Format("Open URL: '{0}'", ((UriActionMetadata)action).Uri);
                    actionLabel.Visibility = Visibility.Visible;
                }
                else if (action is LaunchActionMetadata)
                {
                    actionLabel.Content = string.Format("Launch Application: '{0}'", ((LaunchActionMetadata)action).CommandLine);
                    actionLabel.Visibility = Visibility.Visible;
                }
                else if (action is NamedActionMetadata)
                {
                    actionLabel.Content = string.Format("Named Action: '{0}'", ((NamedActionMetadata)action).ActionName);
                    actionLabel.Visibility = Visibility.Visible;
                }
                else if (action is GotoActionMetadata)
                {
                    GotoActionMetadata gotoAction = (GotoActionMetadata)action;

                    DecoderBase decoder = annotationViewer1
                                          .Images[annotationViewer1.FocusedIndex]
                                          .SourceInfo
                                          .Decoder;

                    if (gotoAction.DestPageIndex >= 0)
                    {
                        int globalImageIndex = annotationViewer1.Images.GetImageIndex(decoder, gotoAction.DestPageIndex);
                        if (globalImageIndex >= 0)
                        {
                            actionLabel.Content = string.Format("Goto page {0}", globalImageIndex + 1);
                            actionLabel.Visibility = Visibility.Visible;
                        }
                    }

                }
            }
            else
            {
                actionLabel.Visibility = Visibility.Collapsed;
                actionLabel.Content = "";
            }
        }

        #endregion


        #region Hot keys

        /// <summary>
        /// Handles the CanExecute event of openCommandBinding object.
        /// </summary>
        private void openCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = openImageMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of addCommandBinding object.
        /// </summary>
        private void addCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = addImagesMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of saveAsCommandBinding object.
        /// </summary>
        private void saveAsCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = saveAsMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of closeCommandBinding object.
        /// </summary>
        private void closeCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = closeAllMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of printCommandBinding object.
        /// </summary>
        private void printCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = printMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of exitCommandBinding object.
        /// </summary>
        private void exitCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = exitMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of findTextCommandBinding object.
        /// </summary>
        private void findTextCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = textMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of cutCommandBinding object.
        /// </summary>
        private void cutCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = cutMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of copyCommandBinding object.
        /// </summary>
        private void copyCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = copyMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of pasteCommandBinding object.
        /// </summary>
        private void pasteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = pasteMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of deleteCommandBinding object.
        /// </summary>
        private void deleteCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = deleteMenuItem.IsEnabled && annotationViewer1.IsMouseOver;
            e.ContinueRouting = !e.CanExecute;
        }

        /// <summary>
        /// Handles the CanExecute event of deleteAllCommandBinding object.
        /// </summary>
        private void deleteAllCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = deleteAllMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of selectAllCommandBinding object.
        /// </summary>
        private void selectAllCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = selectAllMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of deselectAllCommandBinding object.
        /// </summary>
        private void deselectAllCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = deselectAllMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of groupCommandBinding object.
        /// </summary>
        private void groupCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = groupSelectedMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of groupAllCommandBinding object.
        /// </summary>
        private void groupAllCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = groupAllMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of rotateClockwiseCommandBinding object.
        /// </summary>
        private void rotateClockwiseCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = rotateClockwiseMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of rotateCounterclockwiseCommandBinding object.
        /// </summary>
        private void rotateCounterclockwiseCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = rotateCounterclockwiseMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of undoCommandBinding object.
        /// </summary>
        private void undoCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = undoMenuItem.IsEnabled;
        }

        /// <summary>
        /// Handles the CanExecute event of redoCommandBinding object.
        /// </summary>
        private void redoCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = redoMenuItem.IsEnabled;
        }

        #endregion

        #endregion



        #region Delegates

        private delegate void UpdateUIDelegate();

        private delegate void SetIsFileOpeningDelegate(bool isFileOpening);

        private delegate void SetAddingFilenameDelegate(string filename);

        private delegate void CloseCurrentFileDelegate();

        private delegate void UpdateSavingProgressDelegate(int progress);

        #endregion

    }
}
