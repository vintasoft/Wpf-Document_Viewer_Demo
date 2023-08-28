﻿#if !REMOVE_PDF_PLUGIN

using System;
using System.Text;
using System.Windows;
using Vintasoft.Imaging.Fonts;
using Vintasoft.Imaging.Pdf;
using Vintasoft.Imaging.Utils;

using WpfDemosCommonCode.Imaging;


namespace WpfDemosCommonCode.Pdf
{
    /// <summary>
    /// Allows to use custom PDF font program controller for documents.
    /// </summary>
    public static class FontProgramsTools
    {
     
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="FontProgramsTools"/> class.
        /// </summary>
        static FontProgramsTools()
        {
        }

        #endregion



        #region Methods

        #region PUBLIC

        /// <summary>
        /// Shows a dialog for refreshing the font names of programs controller
        /// and refreshes the PostScript font names of programs controller.
        /// </summary>
        /// <param name="dialogOwner">The dialog owner.</param>
        public static bool? RefreshFontNamesOfProgramsController(Window dialogOwner)
        {
            StringBuilder messageString = new StringBuilder();
            messageString.AppendLine("This operation will parse all fonts available on system and refresh");
            messageString.AppendLine("the font map by replacing current names with actual PostScript names.");
            messageString.AppendLine("It can be time consuming.");
            messageString.AppendLine("Do you want to refresh names of all available fonts?");

            if (MessageBox.Show(messageString.ToString(), "Refresh font names?", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                ActionProgressWindow actionProgressForm = new ActionProgressWindow(RefreshFontNames, 1, "Refreshing font names...");
                return actionProgressForm.RunAndShowDialog(dialogOwner);
            }
            else
            {
                return null;
            }
        }

        #endregion


        #region PRIVATE

        /// <summary>
        /// Refreshes the PostScript font names of programs controller.
        /// </summary>
        /// <param name="progressController">Progress controller.</param>
        private static void RefreshFontNames(IActionProgressController progressController)
        {
            FileFontProgramsControllerBase fontProgramsController = FontProgramsControllerBase.Default as FileFontProgramsControllerBase;
            if (fontProgramsController != null)
                fontProgramsController.RefreshFontNames(progressController);
        }

        #endregion

        #endregion

    }
}
#endif