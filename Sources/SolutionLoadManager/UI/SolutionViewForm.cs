using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections;
using Kolos.SolutionLoadManager.Settings;
using Kolos.SolutionLoadManager.UI;

namespace Kolos.SolutionLoadManager.UI
{
    partial class SolutionViewForm : Form
    {
        private class WaitCursor : IDisposable
        {
            public WaitCursor(Form parentForm)
            {
                ParentForm = parentForm;
                OriginalCursor = ParentForm.Cursor;
                ParentForm.Cursor = Cursors.WaitCursor;
            }

            public void Dispose()
            {
                ParentForm.Cursor = OriginalCursor;
            }

            public Form ParentForm { get; private set; }
            private Cursor OriginalCursor { get; set; }
        }

        // Create a node sorter that implements the IComparer interface.
        private class NodeSorter : IComparer
        {
            // Compare the length of the strings, or the strings
            // themselves, if they are the same length.
            public int Compare(object x, object y)
            {
                TreeNode tx = x as TreeNode;
                TreeNode ty = y as TreeNode;

                if (0 == tx.GetNodeCount(false) && 0 != ty.GetNodeCount(false))
                    return 1;
                else if (0 != tx.GetNodeCount(false) && 0 == ty.GetNodeCount(false))
                    return -1;
                else
                    return string.Compare(tx.Text, ty.Text);
            }
        }        
        
        private static readonly Color DemandLoadColor = Color.FromArgb(255, 192, 192);
        private static readonly Color BackgroundLoadColor = Color.FromArgb(255, 224, 192);
        private static readonly Color LoadIfNeededColor = Color.FromArgb(255, 255, 192);
        private static readonly Color ExplicitLoadOnlyColor = Color.FromArgb(192, 255, 192);

        private ISettingsManager m_SettingsManager;
        private Int32 newProfileIndex; 
        private Int32 editProfilesIndex;

        public SolutionViewForm(ProjectInfo solution, ISettingsManager settingsManager)
        {
            InitializeComponent();
            projectsTreeView.TreeViewNodeSorter = new NodeSorter();
            
            m_SettingsManager = settingsManager;
            RootProject = solution;

            PopulateProfilesList();
            SetActiveProfile(m_SettingsManager.ActiveProfile);
        }

        private void PopulateProfilesList()
        {
            profilesComboBox.Items.Clear();
            profilesComboBox.Items.AddRange(m_SettingsManager.Profiles.ToArray());
            // Add two special items to create new or edit existing profiles
            newProfileIndex = profilesComboBox.Items.Add("<New...>");
            editProfilesIndex = profilesComboBox.Items.Add("<Edit...>");
        }

        private void SetActiveProfile(String profile)
        {
            // Select current active profile...
            if (profilesComboBox.Items.Contains(profile))
            {
                profilesComboBox.SelectedItem = profile;
            }
            else
            {
                //... if there is no active profile, just select the first one
                profilesComboBox.SelectedIndex = 0;
            }
        }

        public ProjectInfo RootProject
        {
            set { UpdateTree(value); }
        }

        private void UpdateTree(ProjectInfo info)
        {
            projectsTreeView.BeginUpdate();
            projectsTreeView.Nodes.Clear();
            if (null != info)
            {
                var rootNode = CreateTreeNode(info);
                projectsTreeView.Nodes.Add(rootNode);
                projectsTreeView.Sort();

                rootNode.Expand();
            }
            projectsTreeView.EndUpdate();
        }

        private TreeNode CreateTreeNode(ProjectInfo info)
        {
            var node = new TreeNode(info.Name) {Tag = info};
            foreach (var child in info.Children)
                node.Nodes.Add(CreateTreeNode(child));

            // Show load priory only for projects
            if (0 == node.GetNodeCount(false))
                node.BackColor = GetPriorityColor(info);

            // Assign project icon
            if (null != info.Icon)
            {
                projectIcons.Images.Add(info.Icon);
                node.ImageIndex = node.SelectedImageIndex = projectIcons.Images.Count - 1;
            }
            
            return node;
        }

        private static Color GetPriorityColor(ProjectInfo info)
        {
            switch (info.Priority)
            {
                case LoadPriority.DemandLoad:
                    return DemandLoadColor;
                case LoadPriority.BackgroundLoad:
                    return BackgroundLoadColor;
                case LoadPriority.LoadIfNeeded:
                    return LoadIfNeededColor;
                case LoadPriority.ExplicitLoadOnly:
                    return ExplicitLoadOnlyColor;
                default:
                    return Color.White;
            }
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            projectsTreeView.SelectedNode = e.Node;
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            foreach (TreeNode child in e.Node.Nodes)
                child.Checked = e.Node.Checked;
        }

        private void UpdateCheckedProjectsPriority(LoadPriority priority)
        {
            String activeProfile = m_SettingsManager.ActiveProfile;

            UpdateNodes(projectsTreeView.Nodes[0], n =>
            {
                if (n.Checked)
                {
                    var info = (ProjectInfo)n.Tag;
                    info.Priority = priority;
                    OnPriorityChanged(new PriorityChangedEventArgs(info));

                    // Save new project load priority
                    m_SettingsManager.SetProjectLoadPriority(activeProfile, info.ProjectId, priority);

                    // Show load priory only for projects
                    if (0 == n.GetNodeCount(false))
                        n.BackColor = GetPriorityColor(info);
                }
            });
        }

        private void ChangeActiveProfile(String activeProfile)
        {
            m_SettingsManager.ActiveProfile = activeProfile;

            UpdateNodes(projectsTreeView.Nodes[0], n =>
            {
                var info = (ProjectInfo)n.Tag;
                info.Priority = m_SettingsManager.GetProjectLoadPriority(activeProfile, info.ProjectId); 
                OnPriorityChanged(new PriorityChangedEventArgs(info));

                // Show load priory only for projects
                if (0 == n.GetNodeCount(false))
                    n.BackColor = GetPriorityColor(info);
            });
        }

        private static void UpdateNodes(TreeNode node, Action<TreeNode> action)
        {
            action(node);
            foreach (TreeNode child in node.Nodes)
                UpdateNodes(child, action);
        }

        private void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = projectsTreeView.SelectedNode;
            if (null != node)
                node.ExpandAll();
            else
                projectsTreeView.ExpandAll();
        }

        private void collapseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = projectsTreeView.SelectedNode;
            if (null != node)
                node.Collapse();
            else
                projectsTreeView.CollapseAll();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectsTreeView.Nodes[0].Checked = true;
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            projectsTreeView.Nodes[0].Checked = false;
        }

        private void priorityButton_Click(object sender, EventArgs e)
        {
            var button = sender as ToolStripButton;
            SetLoadPriority(Int32.Parse(button.Tag as String));
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            using (var waitCursor = new WaitCursor(this))
            {
                OnReloadRequested(EventArgs.Empty);
            }
        }

        #region Events

        public event EventHandler<PriorityChangedEventArgs> PriorityChanged;

        private void OnPriorityChanged(PriorityChangedEventArgs e)
        {
            var handler = PriorityChanged;
            if (null != handler)
                handler(this, e);
        }

        public event EventHandler ReloadRequested;

        private void OnReloadRequested(EventArgs e)
        {
            var handler = ReloadRequested;
            if (null != handler)
                handler(this, e);
        }

        #endregion

        private void SetLoadPriority(Int32 priorityIndex)
        {
            switch (priorityIndex)
            {
                case 0:
                    UpdateCheckedProjectsPriority(LoadPriority.DemandLoad);
                    break;
                case 1:
                    UpdateCheckedProjectsPriority(LoadPriority.BackgroundLoad);
                    break;
                case 2:
                    UpdateCheckedProjectsPriority(LoadPriority.LoadIfNeeded);
                    break;
                case 3:
                    UpdateCheckedProjectsPriority(LoadPriority.ExplicitLoadOnly);
                    break;
            }
        }

        private void profileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (newProfileIndex == profilesComboBox.SelectedIndex)
            {
                var dlg = new NewProfileForm(m_SettingsManager);
                dlg.ShowDialog();

                PopulateProfilesList();
                SetActiveProfile(dlg.ProfileName);
            }
            else if (editProfilesIndex == profilesComboBox.SelectedIndex)
            {
                var dlg = new EditProfilesForm(m_SettingsManager);
                dlg.ShowDialog();

                PopulateProfilesList();
                SetActiveProfile(m_SettingsManager.ActiveProfile);
            }
            else
            {
                ChangeActiveProfile(profilesComboBox.SelectedItem as String);
            }
        }
    }

    public class PriorityChangedEventArgs : EventArgs
    {
        public PriorityChangedEventArgs(ProjectInfo project)
        {
            Project = project;
        }

        public ProjectInfo Project { get; private set; }
    }
}
