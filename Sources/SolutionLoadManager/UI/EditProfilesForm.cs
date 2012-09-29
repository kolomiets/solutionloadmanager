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

        private Boolean _renameInProcess;
        private String _originalProfileName;
        private readonly ISettingsManager _settingsManager;

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

            _settingsManager = settingsManager;

            PopulateProfilesList();
        }

        #endregion

        #region Private Members

        private void PopulateProfilesList()
        {
            profilesListView.Items.Clear();

            foreach (var profile in _settingsManager.Profiles)
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

        private void CloseButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        private void RemoveButtonClick(object sender, EventArgs e)
        {
            var profile = GetSelectedItem().Text;

            if (MessageUtils.AskOKCancelQuestion(String.Format(Resources.RemoveProfileQuestion, profile)))
            {
                _settingsManager.RemoveProfile(profile);
                
                // Reload profiles information
                PopulateProfilesList();
            }

            // Clear DialogResult
            DialogResult = DialogResult.None;
        }

        private void RenameButtonClick(object sender, EventArgs e)
        {
            _renameInProcess = true;

            var selectedItem = GetSelectedItem();
            _originalProfileName = selectedItem.Text;
            selectedItem.BeginEdit();           
        }

        private void ProfilesListViewBeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            e.CancelEdit = !_renameInProcess;
        }

        private void ProfilesListViewAfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            _renameInProcess = false;

            // If we cancel rename operation by hitting 'Esc' then label is null
            if (!String.IsNullOrEmpty(e.Label))
            {
                _settingsManager.RenameProfile(_originalProfileName, e.Label);
                // Reload profiles information
                PopulateProfilesList();
            }
        }

        private void ProfilesListViewSelectedIndexChanged(object sender, EventArgs e)
        {
            Boolean profileSelected = profilesListView.SelectedIndices.Count != 0;

            // At least one profile should be available
            removeButton.Enabled = profileSelected && profilesListView.Items.Count > 1;
            renameButton.Enabled = profileSelected;
        }

        #endregion
    }
}
