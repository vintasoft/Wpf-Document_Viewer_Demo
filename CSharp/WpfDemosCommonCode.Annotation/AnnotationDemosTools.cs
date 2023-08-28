﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;

using Vintasoft.Data;
using Vintasoft.Imaging;
using Vintasoft.Imaging.Annotation;
using Vintasoft.Imaging.Annotation.Formatters;
using Vintasoft.Imaging.Annotation.Wpf.UI;
using Vintasoft.Imaging.Codecs.Decoders;
using Vintasoft.Imaging.ImageProcessing;
#if !REMOVE_PDF_PLUGIN
using Vintasoft.Imaging.Annotation.Pdf;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Pdf.Drawing;
using Vintasoft.Imaging.Pdf.Tree;
using Vintasoft.Imaging.Pdf.Tree.Annotations;
#endif
using Vintasoft.Imaging.Undo;
using Vintasoft.Imaging.Wpf;

namespace WpfDemosCommonCode.Annotation
{
    /// <summary>
    /// Contains collection of helper-algorithms for demo applications with annotations.
    /// </summary>
    public class AnnotationDemosTools
    {

        #region Constructors

        /// <summary>
        /// Initializes the <see cref="AnnotationDemosTools"/> class.
        /// </summary>
        public AnnotationDemosTools()
        {
        }

        #endregion



        #region Methods

        /// <summary>
        /// Loads annotation collection from file.
        /// </summary>
        public static void LoadAnnotationsFromFile(WpfAnnotationViewer annotationViewer, OpenFileDialog openFileDialog, CompositeUndoManager undoManager)
        {
            annotationViewer.CancelAnnotationBuilding();

            openFileDialog.FileName = null;
            openFileDialog.Filter = "Binary Annotations(*.vsab)|*.vsab|XMP Annotations(*.xmp)|*.xmp|WANG Annotations(*.wng)|*.wng|All Formats(*.vsab;*.xmp;*.wng)|*.vsab;*.xmp;*.wng";
            openFileDialog.FilterIndex = 4;

            if (openFileDialog.ShowDialog().Value)
            {
                // begin the composite undo action
                undoManager.BeginCompositeAction("Load annotations from file");
                try
                {
                    using (FileStream fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read))
                    {
                        // get the annotation collection
                        AnnotationDataCollection annotations = annotationViewer.AnnotationDataCollection;
                        // clear the annotation collection
                        annotations.ClearAndDisposeItems();
                        // add annotations from stream to the annotation collection
                        annotations.AddFromStream(fs, annotationViewer.Image.Resolution);
                    }
                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }
                finally
                {
                    // end the composite undo action
                    undoManager.EndCompositeAction();
                }
            }
        }

        /// <summary>
        /// Saves annotation collection to a file.
        /// </summary>
        public static void SaveAnnotationsToFile(WpfAnnotationViewer annotationViewer, SaveFileDialog saveFileDialog)
        {
            annotationViewer.CancelAnnotationBuilding();

            saveFileDialog.FileName = null;
            saveFileDialog.Filter = "Binary Annotations|*.vsab|XMP Annotations|*.xmp|WANG Annotations|*.wng";
            saveFileDialog.FilterIndex = 1;

            if (saveFileDialog.ShowDialog().Value)
            {
                try
                {
                    using (FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.ReadWrite))
                    {
                        AnnotationFormatter annotationFormatter = null;
                        if (saveFileDialog.FilterIndex == 1)
                            annotationFormatter = new AnnotationVintasoftBinaryFormatter();
                        else if (saveFileDialog.FilterIndex == 2)
                            annotationFormatter = new AnnotationVintasoftXmpFormatter();
                        else if (saveFileDialog.FilterIndex == 3)
                        {
                            if (MessageBox.Show(
                                "Important: Some data in annotations will be lost. Do you want to continue anyway?",
                                "Warning",
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Warning) == MessageBoxResult.Cancel)
                            {
                                return;
                            }

                            annotationFormatter = new AnnotationWangFormatter(annotationViewer.Image.Resolution);
                        }

                        //
                        AnnotationDataCollection annotations = annotationViewer.AnnotationDataController[annotationViewer.FocusedIndex];
                        //
                        annotationFormatter.Serialize(fs, annotations);
                    }
                }
                catch (Exception ex)
                {
                    DemosTools.ShowErrorMessage(ex);
                }
            }
        }

        /// <summary>
        /// Groups/ungroups selected annotations of annotation collection.
        /// </summary>
        public static void GroupUngroupSelectedAnnotations(WpfAnnotationViewer annotationViewer, CompositeUndoManager undoManager)
        {
            annotationViewer.CancelAnnotationBuilding();

            Collection<WpfAnnotationView> selectedAnnotations = annotationViewer.AnnotationVisualTool.SelectedAnnotations;

            if (selectedAnnotations.Count == 0)
                return;

            if (selectedAnnotations.Count > 1)
            {
                undoManager.BeginCompositeAction("Group annotations");

                try
                {
                    // group annotations
                    GroupAnnotationData group = new GroupAnnotationData();
                    WpfAnnotationView[] annotations = new WpfAnnotationView[selectedAnnotations.Count];
                    selectedAnnotations.CopyTo(annotations, 0);
                    foreach (WpfAnnotationView view in annotations)
                    {
                        group.Items.Add(view.Data);
                        annotationViewer.AnnotationDataCollection.Remove(view.Data);
                    }
                    annotationViewer.AnnotationDataCollection.Add(group);
                    annotationViewer.FocusedAnnotationData = group;
                    annotationViewer.AnnotationVisualTool.SelectedAnnotations.Clear();
                    annotationViewer.AnnotationVisualTool.SelectedAnnotations.Add(annotationViewer.FocusedAnnotationView);
                }
                finally
                {
                    undoManager.EndCompositeAction();
                }
            }
            else
            {
                WpfGroupAnnotationView groupView = annotationViewer.AnnotationVisualTool.SelectedAnnotations[0] as WpfGroupAnnotationView;
                if (groupView != null)
                {
                    undoManager.BeginCompositeAction("Ungroup annotations");

                    try
                    {
                        // ungroup annotations
                        selectedAnnotations.Clear();
                        GroupAnnotationData data = (GroupAnnotationData)groupView.Data;
                        annotationViewer.AnnotationDataCollection.Remove(data);
                        annotationViewer.AnnotationDataCollection.AddRange(data.Items.ToArray());
                        if (annotationViewer.AnnotationMultiSelect)
                            foreach (AnnotationData itemData in data.Items)
                                selectedAnnotations.Add(annotationViewer.AnnotationViewCollection.FindView(itemData));
                        data.Items.Clear();
                        data.Dispose();
                    }
                    finally
                    {
                        undoManager.EndCompositeAction();
                    }
                }
            }
        }

        /// <summary>
        /// Groups all annotations of annotation collection.
        /// </summary>
        public static void GroupAllAnnotations(WpfAnnotationViewer annotationViewer, CompositeUndoManager undoManager)
        {
            annotationViewer.CancelAnnotationBuilding();

            // get reference to the annotation collection of focused image
            AnnotationDataCollection annotationDataCollection = annotationViewer.AnnotationDataController[annotationViewer.FocusedIndex];
            if (annotationDataCollection.Count == 0)
                return;

            undoManager.BeginCompositeAction("Group all annotations");

            try
            {
                // save the references to annotations in array
                AnnotationData[] annotationDataArray = annotationDataCollection.ToArray();

                // clear the annotation collection of focused image
                annotationDataCollection.Clear();

                // create the group annotation
                GroupAnnotationData groupAnnotationData = new GroupAnnotationData();

                // for each annotation in array
                for (int i = 0; i < annotationDataArray.Length; i++)
                {
                    // add annotations from array to group annotation
                    groupAnnotationData.Items.Add(annotationDataArray[i]);
                }

                // add the group annotation to the annotation collection of focused image
                annotationDataCollection.Add(groupAnnotationData);

                annotationViewer.FocusedAnnotationData = groupAnnotationData;
            }
            finally
            {
                undoManager.EndCompositeAction();
            }
        }

        /// <summary>
        /// Rotates image with annotations.
        /// </summary>
        public static void RotateImageWithAnnotations(WpfAnnotationViewer annotationViewer, CompositeUndoManager undoManager, Window owner)
        {
            annotationViewer.CancelAnnotationBuilding();

            WpfRotateImageWithAnnotationsWindow dlg = new WpfRotateImageWithAnnotationsWindow(annotationViewer.Image.PixelFormat);
            dlg.Owner = owner;
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if ((bool)dlg.ShowDialog())
            {
                if (undoManager.IsEnabled)
                {
                    undoManager.BeginCompositeAction("Rotate image with annotations");

                    if (!undoManager.ContainsActionForSourceObject(annotationViewer.Image))
                    {
                        ChangeImageUndoAction originalImageAction = new ChangeImageUndoAction(annotationViewer.Image);
                        undoManager.AddAction(originalImageAction, null);
                    }

                    try
                    {
                        ChangeImageUndoAction action = new ChangeImageUndoAction(annotationViewer.Image, "Rotate");
                        using (VintasoftImage prevImage = annotationViewer.Image.Clone() as VintasoftImage)
                        {
                            // rotate focused image with annotations
                            RotateFocusedImageWithAnnotations(annotationViewer, (float)dlg.Angle, dlg.BorderColorType, dlg.SourceImagePixelFormat);

                            // add action to the undo manager
                            undoManager.AddAction(action, prevImage);
                        }
                    }
                    finally
                    {
                        undoManager.EndCompositeAction();
                    }
                }
                else
                {
                    // rotate focused image with annotations
                    RotateFocusedImageWithAnnotations(annotationViewer, (float)dlg.Angle, dlg.BorderColorType, dlg.SourceImagePixelFormat);
                }
            }
        }

        /// <summary>
        /// Rotates focused image with annotations.
        /// </summary>
        /// <param name="rotationAngle">Rotation angle in degrees.</param>
        /// <param name="borderColorType">Border color type.</param>
        /// <param name="pixelFormat">New pixel format.</param>
        private static void RotateFocusedImageWithAnnotations(WpfAnnotationViewer annotationViewer, float rotationAngle, BorderColorType borderColorType, PixelFormat pixelFormat)
        {
            // if pixel formats are not equal
            if (pixelFormat != annotationViewer.Image.PixelFormat)
            {
                // change pixel format
                ChangePixelFormatCommand command = new ChangePixelFormatCommand(pixelFormat);
                command.ExecuteInPlace(annotationViewer.Image);
            }

            // rotate the focused image with annotations
            annotationViewer.RotateImageWithAnnotations(rotationAngle, borderColorType);
        }

        /// <summary>
        /// Burns annotations on image.
        /// </summary>
        public static void BurnAnnotationsOnImage(WpfAnnotationViewer annotationViewer, CompositeUndoManager undoManager, IDataStorage dataStorage)
        {
            annotationViewer.CancelAnnotationBuilding();

            VintasoftImage sourceImage = annotationViewer.Image;
#if !REMOVE_PDF_PLUGIN
            if (sourceImage.IsVectorImage && sourceImage.SourceInfo.Decoder is PdfDecoder)
            {
                PdfPage page = PdfDocumentController.GetPageAssociatedWithImage(sourceImage);
                if (!page.IsImageOnly)
                {
                    MessageBoxResult result = MessageBox.Show(string.Format("{0}\n\n{1}\n\n{2}\n\n{3}",
                        "This page is a vector page of PDF document. How annotations should be drawn on the page?",
                        "Press 'Yes' if you want convert annotations to PDF annotations and draw annotations on PDF page in a vector form.",
                        "Press 'No' if you want rasterize PDF page and draw annotations on raster image in raster form.",
                        "Press 'Cancel' to cancel burning."),
                        "Burn Annotations", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                    if (result == MessageBoxResult.Yes)
                    {
                        BurnPdfAnnotations(
                            annotationViewer.AnnotationDataCollection,
                            sourceImage.Resolution,
                            page);
                        annotationViewer.AnnotationDataCollection.ClearAndDisposeItems();
                        sourceImage.Reload(true);
                        return;
                    }
                    if (result == MessageBoxResult.Cancel)
                        return;
                }
            }
#endif

            if (undoManager.IsEnabled)
            {
                using (VintasoftImage image = (VintasoftImage)sourceImage.Clone())
                {
                    undoManager.BeginCompositeAction("Burn annotations on image");

                    if (!undoManager.ContainsActionForSourceObject(sourceImage))
                    {
                        ChangeImageUndoAction originalImageAction = new ChangeImageUndoAction(dataStorage, sourceImage);
                        undoManager.AddAction(originalImageAction, null);
                    }
                    try
                    {
                        annotationViewer.AnnotationViewController.BurnAnnotationCollectionOnImage(annotationViewer.FocusedIndex);
                        ChangeImageUndoAction action = new ChangeImageUndoAction(dataStorage, sourceImage, "Burn annotations on image");
                        undoManager.AddAction(action, image);
                    }
                    finally
                    {
                        undoManager.EndCompositeAction();
                    }
                }
            }
            else
            {
                annotationViewer.AnnotationViewController.BurnAnnotationCollectionOnImage(annotationViewer.FocusedIndex);
            }
        }

#if !REMOVE_PDF_PLUGIN
        /// <summary>
        /// Burns the annotations on PDF page use vector graphics.
        /// </summary>
        /// <param name="annotations">The annotations.</param>
        /// <param name="resolution">The resolution.</param>
        /// <param name="page">The page.</param>
        private static void BurnPdfAnnotations(
            AnnotationDataCollection annotations,
            Resolution resolution,
            PdfPage page)
        {
            PdfAnnotation[] annots =
                AnnotationConverter.ConvertToPdfAnnotations(annotations, resolution, page);

            using (PdfGraphics g = page.GetGraphics())
                foreach (PdfAnnotation annot in annots)
                    g.DrawAnnotation(annot);
        }
#endif
        /// <summary>
        /// Copies image with annotations to clipboard.
        /// </summary>
        public static void CopyImageToClipboard(WpfAnnotationViewer annotationViewer)
        {
            if (!CheckImage(annotationViewer))
                return;

            try
            {
                using (VintasoftImage image = annotationViewer.AnnotationViewController.GetImageWithAnnotations(annotationViewer.FocusedIndex))
                    Clipboard.SetImage(VintasoftImageConverter.ToBitmapSource(image));
            }
            catch (Exception ex)
            {
                DemosTools.ShowErrorMessage(ex);
            }
        }

        /// <summary>
        /// Changes the location of annotation or a group of annotations.
        /// </summary>
        public static void ChangeAnnotationsLocation(
            System.Drawing.PointF locationDelta,
            AnnotationData[] annotations,
            AnnotationData source)
        {
            foreach (AnnotationData data in annotations)
            {
                if (data is CompositeAnnotationData)
                {
                    CompositeAnnotationData compositeData = (CompositeAnnotationData)data;
                    List<AnnotationData> subAnnotations = new List<AnnotationData>();
                    foreach (AnnotationData subAnnotation in compositeData)
                        subAnnotations.Add(subAnnotation);
                    ChangeAnnotationsLocation(locationDelta, subAnnotations.ToArray(), source);
                }
                else
                {
                    if (data != source)
                    {
                        System.Drawing.PointF location = data.Location;
                        data.Location = new System.Drawing.PointF(location.X + locationDelta.X, location.Y + locationDelta.Y);
                    }
                }
            }
        }

        /// <summary>
        /// Checks that focused image is present and correct.
        /// </summary>
        /// <returns></returns>
        public static bool CheckImage(WpfAnnotationViewer annotationViewer)
        {
            if (annotationViewer.Image.IsBad)
            {
                MessageBox.Show("Focused image is bad.");
                return false;
            }
            return true;
        }

        #endregion

    }
}
