using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Wpf;
using Vintasoft.Imaging.Wpf.UI;
using Vintasoft.Imaging.Wpf.UI.VisualTools;


namespace WpfDemosCommonCode.Imaging
{
    /// <summary>
    /// The visual tool that allows to scroll the pages in the image viewer.
    /// </summary>
    public class WpfScrollPages : WpfVisualTool
    {

        #region Enums

        /// <summary>
        /// Specifies available scroll actions.
        /// </summary>
        private enum ScrollAction
        {
            /// <summary>
            /// No action.
            /// </summary>
            None,

            /// <summary>
            /// Move To Next Page action.
            /// </summary>
            MoveToNextPage,

            /// <summary>
            /// Move To Previous Page action.
            /// </summary>
            MoveToPreviousPage
        }

        #endregion



        #region Fields

        /// <summary>
        /// The current scroll action.
        /// </summary>
        ScrollAction _scrollAction = ScrollAction.None;

        /// <summary>
        /// A value indicating whether the page changing is in progress.
        /// </summary>
        bool _isPageChanging;

        /// <summary>
        /// X coordinate of autoscroll position of previous focused image.
        /// </summary>
        double _previousFocusedImageAutoScrollPositionX;

        /// <summary>
        /// Width of previous focused image.
        /// </summary>
        int _previousFocusedImageWidth;

        #endregion



        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="ScrollPages"/> object.
        /// </summary>
        public WpfScrollPages()
            : base()
        {
            base.Cursor = Cursors.Arrow;
            base.ActionCursor = base.Cursor;
        }

        #endregion



        #region Properties

        /// <summary>
        /// Gets the name of the visual tool.
        /// </summary>
        public override string ToolName
        {
            get { return "Scroll pages"; }
        }

        int _scrollStep = 50;
        /// <summary>
        /// Gets or sets the scroll step size, in pixels, of this tool.
        /// </summary>
        [Description("The scroll step size in pixels.")]
        [DefaultValue(50)]
        public int ScrollStep
        {
            get
            {
                return _scrollStep;
            }
            set
            {
                if (value > 100)
                    value = 100;
                if (value < 0)
                    value = 0;
                _scrollStep = value;
            }
        }

        #endregion



        #region Methods

        #region PROTECTED

        /// <summary>
        /// Raises the MouseWheel event.
        /// </summary>
        /// <param name="e">A <see cref="MouseWheelEventArgs"/> that contains the event data.</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (!Enabled)
                return;

            if (_isPageChanging)
                return;

            Scroll(e);

            e.Handled = true;
        }

        #endregion


        #region PRIVATE

        /// <summary>
        /// Scrolls the current page.
        /// </summary>
        /// <param name="e">A MouseEventArgs that contains the event data.</param>
        private void Scroll(MouseEventArgs e)
        {
            if (!Enabled)
                return;

            if (_isPageChanging)
                return;

            int delta = (e as MouseWheelEventArgs).Delta;

            if (delta <= 0)
                Scroll(_scrollStep);
            else
                Scroll(-_scrollStep);
        }

        /// <summary>
        /// Returns center point of image viewer in coordinate space of specified image.
        /// </summary>
        /// <param name="image">An image.</param>
        /// <returns>Center point of image viewer in coordinate space of specified image.</returns>
        private Point GetCenterPoint(VintasoftImage image)
        {
            // get old visible point
            Point centerPoint = new Point(ImageViewer.ViewportWidth / 2, ImageViewer.ViewportHeight / 2);
            centerPoint.Y += ImageViewer.VerticalOffset;
            centerPoint.X += ImageViewer.HorizontalOffset;

            // get transform from image space to viewer space
            AffineMatrix pointTransformMatrix = ImageViewer.GetTransformFromVisualToolToImage(image);
            // transform the point
            centerPoint = WpfPointAffineTransform.TransformPoint(pointTransformMatrix, centerPoint);

            return centerPoint;
        }

        /// <summary>
        /// Returns a value, which determines that the image edge is visible.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="imageEdge">The image edge.</param>
        /// <returns>
        /// <b>true</b> - if image edge is visible.
        /// <b>false</b> - if image edge is not visible.
        /// </returns>
        private bool GetIsImageEdgeVisible(VintasoftImage image, Vintasoft.Imaging.AnchorType imageEdge)
        {
            // get image viewer state for the image
            WpfImageViewerState focusedImageViewerState = ImageViewer.GetViewerState(image);

            // get the visible image rectangle in image viewer
            Rect imageVisibleRect = ImageViewer.ViewerState.ImageVisibleRect;
            if (ImageViewer.IsMultipageDisplayMode)
            {
                imageVisibleRect = new Rect(0, 0, ImageViewer.ViewportWidth, ImageViewer.ViewportHeight);
            }
            else
            {
                imageVisibleRect = focusedImageViewerState.ImageVisibleRect;
            }

            // get the image rectangle in image viewer
            Rect imageRect;
            if (ImageViewer.IsMultipageDisplayMode)
            {
                imageRect = focusedImageViewerState.ImageBoundingBox;
            }
            else
            {
                imageRect = new Rect(0, 0, ImageViewer.Image.Width, ImageViewer.Image.Height);
            }
            if (imageVisibleRect == new Rect(0, 0, 0, 0))
                return false;

            // get the image "anchor" line
            Rect imageLine = new Rect(0, 0, 0, 0);
            if (imageEdge == Vintasoft.Imaging.AnchorType.Bottom)
            {
                // get bottom line of image
                imageLine = new Rect(imageRect.X, imageRect.Y + imageRect.Height - 1, imageRect.Width, 1);
            }
            else if (imageEdge == Vintasoft.Imaging.AnchorType.Top)
            {
                // get top line of image
                imageLine = new Rect(imageRect.X, imageRect.Y, imageRect.Width, 1);
            }
            else if (imageEdge == Vintasoft.Imaging.AnchorType.Left)
            {
                // get left line of image
                imageLine = new Rect(0, 0, 1, ImageViewer.Image.Height);
            }
            else if (imageEdge == Vintasoft.Imaging.AnchorType.Right)
            {
                // get right line of image
                imageLine = new Rect(ImageViewer.Image.Width - 1, 0, 1, ImageViewer.Image.Height);
            }

            // return a value that indicates whether the visible rectangle intersects with image line
            return imageVisibleRect.IntersectsWith(imageLine);
        }

        /// <summary>
        /// Scrolls the image viewer.
        /// </summary>
        /// <param name="scrollStepSize">A scroll step size in pixels.</param>
        private void Scroll(int scrollStepSize)
        {
            // determine the scroll direction
            bool scrollForward = true;
            _scrollAction = ScrollAction.MoveToNextPage;
            Vintasoft.Imaging.AnchorType imageEdge = Vintasoft.Imaging.AnchorType.Bottom;

            // if scroll step size is negative 
            if (scrollStepSize < 0)
            {
                // change the scroll direction
                scrollForward = false;
                _scrollAction = ScrollAction.MoveToPreviousPage;
                imageEdge = Invert(imageEdge);
            }

            // get new scroll step size according to the focused image resolution
            float newScrollStepSize = scrollStepSize * (float)ImageViewer.Image.Resolution.Vertical / 96.0f;

            // if edge (top/bottom) of focused image is visible
            if (GetIsImageEdgeVisible(ImageViewer.Image, imageEdge))
            {
                // if focused image is first image in image viewer
                // and viewer must be scrolled backward
                if (ImageViewer.FocusedIndex == 0 && !scrollForward)
                    return;

                // if focused image is last in image viewer
                // and viewer must be scrolled forward
                if (ImageViewer.FocusedIndex >= ImageViewer.Images.Count - 1 && scrollForward)
                    return;

                _isPageChanging = true;

                int newFocusedIndex = ImageViewer.FocusedIndex;
                if (scrollForward)
                    newFocusedIndex++;
                else
                    newFocusedIndex--;
                // if image viewer is in multipage mode
                if (ImageViewer.IsMultipageDisplayMode)
                {
                    // change focused image
                    ChangeFocusedImage(newFocusedIndex);

                    _isPageChanging = false;
                }
                // if image viewer in single page mode
                else
                {
                    ImageViewer.ImageLoading += new EventHandler<ImageLoadingEventArgs>(ImageViewer_ImageLoading);
                    // save information about previous focused image
                    _previousFocusedImageAutoScrollPositionX = ImageViewer.ViewerState.AutoScrollPosition.X;
                    _previousFocusedImageWidth = ImageViewer.Image.Width;
                    // change focused index
                    ChangeFocusedImage(newFocusedIndex);
                }
            }

            // get the center point of previous focused image
            Point previousFocusedImageCenterPoint = GetCenterPoint(ImageViewer.Image);

            // get the scroll point of new focused image
            Point newFocusedImageScrollPoint;

            int imageRotationAngle = ImageViewer.GetImageViewRotationAngle(ImageViewer.Image);

            switch (imageRotationAngle)
            {
                case 90:
                    newFocusedImageScrollPoint = new Point(
                        previousFocusedImageCenterPoint.X + newScrollStepSize,
                        previousFocusedImageCenterPoint.Y);
                    break;

                case 180:
                    newFocusedImageScrollPoint = new Point(
                        previousFocusedImageCenterPoint.X,
                        previousFocusedImageCenterPoint.Y - newScrollStepSize);
                    break;

                case 270:
                    newFocusedImageScrollPoint = new Point(
                        previousFocusedImageCenterPoint.X - newScrollStepSize,
                        previousFocusedImageCenterPoint.Y);
                    break;

                default:
                    newFocusedImageScrollPoint = new Point(
                        previousFocusedImageCenterPoint.X,
                        previousFocusedImageCenterPoint.Y + newScrollStepSize);
                    break;
            }

            // scroll to the scroll point on new focused image
            ImageViewer.ScrollToPoint(newFocusedImageScrollPoint,
                    AnchorType.Bottom |
                    AnchorType.Left |
                    AnchorType.Right |
                    AnchorType.Top);

            _scrollAction = ScrollAction.None;
            SetFocusToVisibleImage();
        }

        /// <summary>
        /// Inverts the specified anchor.
        /// </summary>
        /// <param name="anchor">The anchor.</param>
        /// <returns>
        /// The inverted value.
        /// </returns>
        private AnchorType Invert(AnchorType anchor)
        {
            switch (anchor)
            {
                case AnchorType.Top:
                    return AnchorType.Bottom;

                case AnchorType.Bottom:
                    return AnchorType.Top;

                case AnchorType.Left:
                    return AnchorType.Right;

                case AnchorType.Right:
                    return AnchorType.Left;

                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Changes the focused image.
        /// </summary>
        /// <param name="newFocusedIndex">New index of the focused image.</param>
        private void ChangeFocusedImage(int newFocusedIndex)
        {
            ImageViewer.DisableAutoScrollToFocusedImage();
            ImageViewer.SetFocusedIndexSync(newFocusedIndex);
            ImageViewer.EnableAutoScrollToFocusedImage();
        }

        /// <summary>
        /// Image is loading in image viewer.
        /// </summary>
        private void ImageViewer_ImageLoading(object sender, ImageLoadingEventArgs e)
        {
            // if scrolling is in progress
            if (_scrollAction != ScrollAction.None)
            {
                ImageViewer.ImageLoading -= new EventHandler<ImageLoadingEventArgs>(ImageViewer_ImageLoading);

                double autoScrollPositionX;
                double autoScrollPositionY;

                // autoScrollPositionX
                double proportion = (double)ImageViewer.Image.Width / _previousFocusedImageWidth;
                autoScrollPositionX = _previousFocusedImageAutoScrollPositionX * proportion;

                // autoScrollPositionY
                switch (_scrollAction)
                {
                    case ScrollAction.MoveToNextPage:
                        autoScrollPositionY = 0;
                        break;
                    case ScrollAction.MoveToPreviousPage:
                        autoScrollPositionY = ImageViewer.Image.Height;
                        break;
                    default:
                        throw new Exception();
                }

                // set scroll of new focused image
                ImageViewer.ViewerState.AutoScrollPosition = new Point(autoScrollPositionX, autoScrollPositionY);

                _scrollAction = ScrollAction.None;
            }

            _isPageChanging = false;
        }

        /// <summary>
        /// Sets focus to the visible image.
        /// </summary>
        /// <remarks>
        /// Focus will be changed if focused image is not visible.
        /// </remarks>
        private void SetFocusToVisibleImage()
        {
            // if focused image is not visible
            if (ImageViewer.ViewerState.ImageVisibleRect == new Rect(0, 0, 0, 0))
            {
                // get central point of image viewer
                Point centerPoint = new Point(ImageViewer.ViewportWidth / 2, ImageViewer.ViewportHeight / 2);

                double minDistance = ImageViewer.ViewportWidth * ImageViewer.ViewportWidth +
                    ImageViewer.ViewportHeight * ImageViewer.ViewportHeight;
                VintasoftImage minDistanceImage = null;

                // for each visible image
                foreach (VintasoftImage image in ImageViewer.GetVisibleImages())
                {
                    // calculate distance between central point and image rectangle
                    double distanceBetweenImageAndPoint = GetDistanceBetweenPointAndImageRect(centerPoint, image);

                    if (distanceBetweenImageAndPoint < minDistance)
                    {
                        minDistance = distanceBetweenImageAndPoint;
                        minDistanceImage = image;
                    }
                }

                // if there is visible image
                if (minDistanceImage != null)
                {
                    _isPageChanging = true;
                    // get index of visible image
                    int indexOfVisibleImage = ImageViewer.Images.IndexOf(minDistanceImage);

                    // set focus to visible image
                    ChangeFocusedImage(indexOfVisibleImage);

                    _isPageChanging = false;
                }
            }
        }

        /// <summary>
        /// Returns distance between point and image rectangle.
        /// </summary>
        /// <param name="point">A point in image viewer space.</param>
        /// <param name="image">An image.</param>
        /// <returns>Distance between point and image rectangle.</returns>
        private double GetDistanceBetweenPointAndImageRect(Point point, VintasoftImage image)
        {
            AffineMatrix transformMatrix = ImageViewer.GetTransformFromImageToVisualTool(image);
            // get image rectangle
            Point topLeftRectPoint = WpfPointAffineTransform.TransformPoint(transformMatrix, new Point(0, 0));
            Point bottomRightRectPoint = WpfPointAffineTransform.TransformPoint(transformMatrix, new Point(image.Width, image.Height));
            topLeftRectPoint.X -= ImageViewer.HorizontalOffset;
            topLeftRectPoint.Y -= ImageViewer.VerticalOffset;
            bottomRightRectPoint.X -= ImageViewer.HorizontalOffset;
            bottomRightRectPoint.Y -= ImageViewer.VerticalOffset;
            Rect imageRect = new Rect(topLeftRectPoint, bottomRightRectPoint);

            Point imagePoint = new Point();
            // get X coordinate of point on image
            if (point.X < imageRect.X)
                imagePoint.X = imageRect.X;
            else if (point.X > imageRect.X + imageRect.Width)
                imagePoint.X = imageRect.X + imageRect.Width;
            else
                imagePoint.X = point.X;

            // get Y coordinate of point on image
            if (point.Y < imageRect.Y)
                imagePoint.Y = imageRect.Y;
            else if (point.Y > imageRect.Y + imageRect.Height)
                imagePoint.Y = imageRect.Y + imageRect.Height;
            else
                imagePoint.Y = point.Y;

            // calculate distance
            double dx = (point.X - imagePoint.X);
            double dy = (point.Y - imagePoint.Y);
            double distanceBetweenImageAndPoint = dx * dx + dy * dy;

            return distanceBetweenImageAndPoint;
        }

        #endregion

        #endregion

    }
}
