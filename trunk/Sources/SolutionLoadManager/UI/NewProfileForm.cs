using System;
using System.Linq;
using System.Windows.Forms;
using Kolos.SolutionLoadManager.Settings;
using System.Collections.Generic;

namespace Kolos.SolutionLoadManager.UI
{
    /// <summary>
    /// This form is used to create new profile or copy existing one.
    /// </summary>
    partial class NewProfileForm : Form
    {
        #region Public Members

        /// <summary>
        /// Creates new instance of the form.
        /// </summary>
        /// <param name="settings">
        /// Settings manager which is used to retrieve information about existing profiles.
        /// </param>
        public NewProfileForm(IEnumerable<String> profiles)
        {
            InitializeComponent();

            profilesComboBox.Items.AddRange(profiles.ToArray());

            // select <Empty> profile
            profilesComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Name of the new profile.
        /// </summary>
        public String ProfileName
        {
            get 
            { 
                return profileNameTextBox.Text;
            }
        }

        /// <summary>
        /// Name of the existing profile which settings will be copied to the new one.
        /// </summary>
        public String CopyFromProfile
        {
            get
            {
                return (0 == profilesComboBox.SelectedIndex) ? String.Empty : profilesComboBox.SelectedItem as String;
            }
        }

        #endregion

        #region Event Handlers

        private void okButton_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(ProfileName))
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
                DialogResult = System.Windows.Forms.DialogResult.OK;
            }
        }

        #endregion
    }
}
