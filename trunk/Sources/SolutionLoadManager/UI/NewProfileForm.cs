using System;
using System.Linq;
using System.Windows.Forms;
using Kolos.SolutionLoadManager.Settings;

namespace Kolos.SolutionLoadManager.UI
{
    /// <summary>
    /// This form is used to create new profile or copy existing one.
    /// </summary>
    partial class NewProfileForm : Form
    {
        #region Private Members

        private ISettingsManager m_SettingsManager;

        #endregion

        #region Public Members

        /// <summary>
        /// Creates new instance of the form.
        /// </summary>
        /// <param name="settings">
        /// Settings manager which is used to retrieve information about existing profiles.
        /// </param>
        public NewProfileForm(ISettingsManager settings)
        {
            InitializeComponent();

            m_SettingsManager = settings;
            profilesComboBox.Items.AddRange(m_SettingsManager.Profiles.ToArray());

            // <Empty> profile
            profilesComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Name of the new profile.
        /// </summary>
        public String ProfileName
        {
            get { return profileNameTextBox.Text; }
        }

        #endregion

        #region Event Handlers

        private void okButton_Click(object sender, EventArgs e)
        {
            // TODO: name validation

            String sourceProfile = (0 == profilesComboBox.SelectedIndex)
                                    ? String.Empty
                                    : profilesComboBox.SelectedItem as String;

            m_SettingsManager.AddProfile(profileNameTextBox.Text, sourceProfile);
            Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        #endregion
    }
}
