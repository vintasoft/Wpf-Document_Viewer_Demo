using System;
using System.Diagnostics;
using System.Windows.Forms;

using Vintasoft.Imaging;
using Vintasoft.Imaging.Metadata;
using Vintasoft.Imaging.Wpf.UI;
using Vintasoft.Imaging.Wpf.UI.VisualTools;

namespace WpfCommonCode.Imaging
{
    /// <summary>
    /// Represents an executor of "URI" actions that opens URL using the default internet browser.
    /// </summary>
    public class WpfUriActionExecutor : IWpfPageContentActionExecutor
    {

        /// <summary>
        /// Executes the action in image viewer.
        /// </summary>
        /// <param name="viewer">Image viewer.</param>
        /// <param name="image">Instance of <see cref="VintasoftImage"/> on wich action will be executed.</param>
        /// <param name="action">The action.</param>
        /// <returns><b>True</b> if action is executed; otherwise, <b>false</b>.</returns>
        public bool ExecuteAction(WpfImageViewer viewer, VintasoftImage image, PageContentActionMetadata action)
        {
            UriActionMetadata uriAction = action as UriActionMetadata;
            if (uriAction != null && uriAction.Uri != null)
            {
                Uri actionUri = UriActionMetadata.GetAbsoluteUri(uriAction.Uri, image);
                if (MessageBox.Show(string.Format("Open URL '{0}' ?", actionUri), "Open URL", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        DemosTools.OpenBrowser(actionUri.ToString());
                    }
                    catch (Exception exc)
                    {
                        DemosTools.ShowErrorMessage(exc);
                        return false;
                    }
                }
                return true;
            }
            ResourceActionMetadata resourceAction = action as ResourceActionMetadata;
            if (resourceAction != null && resourceAction.ResourceUri != null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = resourceAction.ResourceUri.ToString();
                saveFileDialog.Title = "Save resource";
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        resourceAction.SaveResourceToFile(saveFileDialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        DemosTools.ShowErrorMessage(ex);
                        return true;
                    }
                    if (MessageBox.Show(string.Format("Open file '{0}' use default application?", saveFileDialog.FileName), "Open file", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        try
                        {
                            ProcessStartInfo processInfo = new ProcessStartInfo(saveFileDialog.FileName);
                            processInfo.UseShellExecute = true;
                            Process.Start(processInfo);
                        }
                        catch (Exception exc)
                        {
                            DemosTools.ShowErrorMessage(exc);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

    }
}
