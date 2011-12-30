using System;
using System.Linq;
using System.Windows.Forms;
using Kolos.SolutionLoadManager.Settings;

namespace Kolos.SolutionLoadManager.UI
{
    /// <summary>
    /// This forms is used to perform different operation on existing profiles.
    /// </summary>
    partial class EditProfilesForm : Form
    {
        #region Private Fields

        private Boolean m_RenameEnabled;
        private String m_OriginalProfileName;
        private ISettingsManager m_SettingsManager;

        #endregion

        #region Public Members

        /// <summary>
        /// Creates new instance of the form.
        /// </summary>
        /// <param name="settingsManager">
        /// Settings manager which is used to retrieve information about existing profiles.
        /// </param>
        public EditProfilesForm(ISettingsManager settingsManager)
        {
            InitializeComponent();

            m_SettingsManager = settingsManager;

            PopulateProfilesList();
        }

        #endregion

        #region Private Members

        private void PopulateProfilesList()
        {
            profilesListView.Items.Clear();

            foreach (var profile in m_SettingsManager.Profiles)
                profilesListView.Items.Add(new ListViewItem(profile));

            // Select first profile
            profilesListView.SelectedIndices.Add(0);
        }

        private ListViewItem GetSelectedItem()
        {
            return profilesListView.SelectedItems.Cast<ListViewItem>().First();
        }

        #endregion

        #region Event Handlers

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void removeButton_Click(object sender, EventArgs e)
        {
            var selectedItem = GetSelectedItem();

            // TODO: move strings to resources
            if (MessageBox.Show("Are you sure you want to remove '" + selectedItem.Text + "'?", 
                                "Solution Load Manager",
                                MessageBoxButtons.OKCancel, 
                                MessageBoxIcon.Question) == System.Windows.Forms.DialogResult.OK)
            {
                m_SettingsManager.RemoveProfile(selectedItem.Text);
                
                // Reload profiles information
                PopulateProfilesList();

                // Clear DialogResult
                this.DialogResult = System.Windows.Forms.DialogResult.None;
            }
        }

        private void renameButton_Click(object sender, EventArgs e)
        {
            m_RenameEnabled = true;

            var selectedItem = GetSelectedItem();
            m_OriginalProfileName = selectedItem.Text;
            selectedItem.BeginEdit();           
        }

        private void profilesListView_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            e.CancelEdit = !m_RenameEnabled;
        }

        private void profilesListView_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            m_RenameEnabled = false;

            // If we cancel rename operation by hitting 'Esc' then label is null
            if (!String.IsNullOrEmpty(e.Label))
            {
                m_SettingsManager.RenameProfile(m_OriginalProfileName, e.Label);
                // Reload profiles information
                PopulateProfilesList();
            }
        }

        private void profilesListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            Boolean profileSelected = profilesListView.SelectedIndices.Count != 0;

            // At least one profile should be available
            removeButton.Enabled = profileSelected && profilesListView.Items.Count > 1;
            renameButton.Enabled = profileSelected;
        }

        #endregion
    }
}
