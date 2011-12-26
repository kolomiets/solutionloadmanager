using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Kolos.SolutionLoadManager.Settings;

namespace Kolos.SolutionLoadManager.UI
{
    partial class NewProfileForm : Form
    {
        private ISettingsManager m_SettingsManager;

        public NewProfileForm(ISettingsManager settings)
        {
            InitializeComponent();

            m_SettingsManager = settings;
            profilesComboBox.Items.AddRange(m_SettingsManager.Profiles.ToArray());

            // <Empty> profile
            profilesComboBox.SelectedIndex = 0;
        }

        public String ProfileName
        {
            get { return profileNameTextBox.Text; }
        }

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
    }
}
