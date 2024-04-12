﻿using System.Windows.Controls;

using Vintasoft.Imaging.Codecs.Decoders;

namespace WpfDemosCommonCode.Imaging
{
    /// <summary>
    /// A control that allows to edit <see cref="XlsxPageLayoutSettingsType"/>.
    /// </summary>
    public partial class XlsxPageLayoutSettingsTypeEditorControl : UserControl
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XlsxPageLayoutSettingsTypeEditorControl"/> class.
        /// </summary>
        public XlsxPageLayoutSettingsTypeEditorControl()
        {
            InitializeComponent();
        }

        #endregion



        #region Properties

#if !REMOVE_OFFICE_PLUGIN
        /// <summary>
        /// Gets or sets the <see cref="XlsxPageLayoutSettingsType"/>.
        /// </summary>
        [System.ComponentModel.DefaultValue(Vintasoft.Imaging.Codecs.Decoders.XlsxPageLayoutSettingsType.Undefined)]
        [System.ComponentModel.Browsable(false)]
        public XlsxPageLayoutSettingsType Settings
        {
            get
            {
                XlsxPageLayoutSettingsType result = XlsxPageLayoutSettingsType.Undefined;

                if (useWorksheetWidthAsPageWidthCheckBox.IsChecked.Value == true)
                    result |= XlsxPageLayoutSettingsType.UseWorksheetWidth;
                if (useWorksheetHeightAsPageHeightCheckBox.IsChecked.Value == true)
                    result |= XlsxPageLayoutSettingsType.UseWorksheetHeight;
                if (usePrintAreaCheckBox.IsChecked.Value == true)
                    result |= XlsxPageLayoutSettingsType.UsePrintArea;
                if (usePageMarginCheckBox.IsChecked.Value == true)
                    result |= XlsxPageLayoutSettingsType.UsePageMargin;
                if (usePageScaleCheckBox.IsChecked.Value == true)
                    result |= XlsxPageLayoutSettingsType.UsePageScale;

                return result;
            }
            set
            {
                UpdateCheckBox(useWorksheetWidthAsPageWidthCheckBox, value, XlsxPageLayoutSettingsType.UseWorksheetWidth);
                UpdateCheckBox(useWorksheetHeightAsPageHeightCheckBox, value, XlsxPageLayoutSettingsType.UseWorksheetHeight);
                UpdateCheckBox(usePrintAreaCheckBox, value, XlsxPageLayoutSettingsType.UsePrintArea);
                UpdateCheckBox(usePageMarginCheckBox, value, XlsxPageLayoutSettingsType.UsePageMargin);
                UpdateCheckBox(usePageScaleCheckBox, value, XlsxPageLayoutSettingsType.UsePageScale);

                UpdateUI();
            }
        } 
#endif

        #endregion



        #region Methods

        #region UI

        /// <summary>
        /// Handles the CheckedChanged event of useWorksheetWidthAsPageWidthCheckBox object.
        /// </summary>
        private void useWorksheetWidthAsPageWidthCheckBox_CheckedChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateUI();
        }

        /// <summary>
        /// Handles the CheckedChanged event of useWorksheetHeightAsPageHeightCheckBox object.
        /// </summary>
        private void useWorksheetHeightAsPageHeightCheckBox_CheckedChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateUI();
        }

        #endregion


#if !REMOVE_OFFICE_PLUGIN
        /// <summary>
        /// Updates check state of specified check box.
        /// </summary>
        /// <param name="checkBox">The check box.</param>
        /// <param name="settings">The <see cref="XlsxPageLayoutSettingsType"/> current value.</param>
        /// <param name="checkBoxValue">The <see cref="XlsxPageLayoutSettingsType"/> value of the specified check box.</param>
        private void UpdateCheckBox(CheckBox checkBox, XlsxPageLayoutSettingsType settings, XlsxPageLayoutSettingsType checkBoxValue)
        {
            // if flag is not defined
            if ((settings & checkBoxValue) == 0)
                checkBox.IsChecked = false;
            else
                checkBox.IsChecked = true;
        } 
#endif

        /// <summary>
        /// Updates the user interface of this control.
        /// </summary>
        private void UpdateUI()
        {
            if (useWorksheetHeightAsPageHeightCheckBox.IsChecked.Value == true ||
                useWorksheetWidthAsPageWidthCheckBox.IsChecked.Value == true)
                usePageScaleCheckBox.IsEnabled = false;
            else
                usePageScaleCheckBox.IsEnabled = true;
        }

        #endregion

    }
}
