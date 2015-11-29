using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Kolos.SolutionLoadManager.UI
{
    /// <summary>
    /// This form is used to create new profile or copy existing one.
    /// </summary>
    internal partial class NewProfileForm : Form
    {
        #region Public Members

        /// <summary>
        /// Creates new instance of the form.
        /// </summary>
        /// <param name="profiles">Existing profiles to show in the form.</param>
        public NewProfileForm(IEnumerable<string> profiles)
        {
            InitializeComponent();

            profilesComboBox.Items.AddRange(profiles.ToArray());

            // select <Empty> profile
            profilesComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Gets the name of the new profile.
        /// </summary>
        public string ProfileName => profileNameTextBox.Text;

        /// <summary>
        /// Gets the name of the existing profile which settings will be copied to the new one.
        /// </summary>
        public string CopyFromProfile => 
            (0 == profilesComboBox.SelectedIndex) ? string.Empty : profilesComboBox.SelectedItem as string;

        #endregion

        #region Event Handlers

        private void OkButtonClick(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(ProfileName))
            {
                MessageUtils.ShowWarning(Resources.EmptyProfileNameWarning);
            }
            else if (profilesComboBox.Items.Contains(ProfileName))
            {
                MessageUtils.ShowWarning(Resources.ProfileAlreadyExistsWarning);
            }
            else
            {
                // All is OK. Close dialog.
                DialogResult = DialogResult.OK;
            }
        }

        #endregion
    }
}
