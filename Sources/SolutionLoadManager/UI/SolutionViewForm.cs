using System;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Collections;
using Kolos.SolutionLoadManager.Settings;

namespace Kolos.SolutionLoadManager.UI
{
    internal partial class SolutionViewForm : Form
    {
        #region Fields   

        private static readonly Color DemandLoadColor = Color.FromArgb(255, 192, 192);
        private static readonly Color BackgroundLoadColor = Color.FromArgb(255, 224, 192);
        private static readonly Color LoadIfNeededColor = Color.FromArgb(255, 255, 192);
        private static readonly Color ExplicitLoadOnlyColor = Color.FromArgb(192, 255, 192);

        private readonly ISettingsManager _settingsManager;
        private Int32 _newProfileIndex; 
        private Int32 _editProfilesIndex;


        #endregion

        #region Ctors

        public SolutionViewForm(ProjectInfo solution, ISettingsManager settingsManager)
        {
            InitializeComponent();
            projectsTreeView.TreeViewNodeSorter = new SolutionNodeComparer();
            
            _settingsManager = settingsManager;
            RootProject = solution;

            ReloadProfilesList();
            SelectProfile(_settingsManager.ActiveProfile);
        }

        #endregion

        #region Properties

        public ProjectInfo RootProject
        {
            set { UpdateTree(value); }
        }

        #endregion

        #region Events

        public event EventHandler<PriorityChangedEventArgs> PriorityChanged;

        public event EventHandler ReloadRequested;

        #endregion

        #region Private Static Methods

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

        private static void UpdateNodes(TreeNode node, Action<TreeNode> action)
        {
            action(node);
            foreach (TreeNode child in node.Nodes)
                UpdateNodes(child, action);
        }

        #endregion

        #region UI Handlers

        private void OnPriorityChanged(PriorityChangedEventArgs e)
        {
            var handler = PriorityChanged;
            handler?.Invoke(this, e);
        }

        private void OnReloadRequested(EventArgs e)
        {
            var handler = ReloadRequested;
            handler?.Invoke(this, e);
        }

        private void ProjectsTreeViewNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            projectsTreeView.SelectedNode = e.Node;
        }

        private void ProjectsTreeViewAfterCheck(object sender, TreeViewEventArgs e)
        {
            foreach (TreeNode child in e.Node.Nodes)
                child.Checked = e.Node.Checked;
        }

        private void ExpandToolStripMenuItemClick(object sender, EventArgs e)
        {
            var node = projectsTreeView.SelectedNode;
            if (null != node)
                node.ExpandAll();
            else
                projectsTreeView.ExpandAll();
        }

        private void CollapseToolStripMenuItemClick(object sender, EventArgs e)
        {
            var node = projectsTreeView.SelectedNode;
            if (null != node)
                node.Collapse();
            else
                projectsTreeView.CollapseAll();
        }

        private void SelectAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            projectsTreeView.Nodes[0].Checked = true;
        }

        private void ClearAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            projectsTreeView.Nodes[0].Checked = false;
        }

        private void PriorityButtonClick(object sender, EventArgs e)
        {
            var button = sender as ToolStripButton;
            SetLoadPriority(int.Parse(button.Tag as string));
        }

        private void CloseButtonClick(object sender, EventArgs e)
        {
            Close();
        }

        private void ReloadButtonClick(object sender, EventArgs e)
        {
            using (new WaitCursor(this))
            {
                OnReloadRequested(EventArgs.Empty);
            }
        }

        private void ProfileComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (_newProfileIndex == profilesComboBox.SelectedIndex)
            {
                var dlg = new NewProfileForm(_settingsManager.Profiles);
                if (DialogResult.OK == dlg.ShowDialog())
                {
                    _settingsManager.AddProfile(dlg.ProfileName, dlg.CopyFromProfile);

                    ReloadProfilesList();
                    SelectProfile(dlg.ProfileName);
                }
            }
            else if (_editProfilesIndex == profilesComboBox.SelectedIndex)
            {
                var dlg = new EditProfilesForm(_settingsManager);
                dlg.ShowDialog();

                ReloadProfilesList();
                SelectProfile(_settingsManager.ActiveProfile);
            }
            else
            {
                ChangeActiveProfile(profilesComboBox.SelectedItem as string);
            }
        }

        #endregion

        #region Tree Processing Logic

        private void ReloadProfilesList()
        {
            profilesComboBox.Items.Clear();
            profilesComboBox.Items.AddRange(_settingsManager.Profiles.ToArray());
            // Add two special items to create new or edit existing profiles
            _newProfileIndex = profilesComboBox.Items.Add(Resources.NewProfileListItem);
            _editProfilesIndex = profilesComboBox.Items.Add(Resources.EditProfilesListItem);
        }

        private void SelectProfile(string profile)
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

            // Show load priority only for projects
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

        private void UpdateCheckedProjectsPriority(LoadPriority priority)
        {
            String activeProfile = _settingsManager.ActiveProfile;

            UpdateNodes(projectsTreeView.Nodes[0], n =>
            {
                if (n.Checked)
                {
                    var info = (ProjectInfo)n.Tag;
                    info.Priority = priority;
                    OnPriorityChanged(new PriorityChangedEventArgs(info));

                    // Save new project load priority
                    _settingsManager.SetProjectLoadPriority(activeProfile, info.ProjectId, priority);

                    // Show load priory only for projects
                    if (0 == n.GetNodeCount(false))
                        n.BackColor = GetPriorityColor(info);
                }
            });
        }

        private void ChangeActiveProfile(string activeProfile)
        {
            _settingsManager.ActiveProfile = activeProfile;

            UpdateNodes(projectsTreeView.Nodes[0], n =>
            {
                var info = (ProjectInfo)n.Tag;
                info.Priority = _settingsManager.GetProjectLoadPriority(activeProfile, info.ProjectId); 
                OnPriorityChanged(new PriorityChangedEventArgs(info));

                // Show load priory only for projects
                if (0 == n.GetNodeCount(false))
                    n.BackColor = GetPriorityColor(info);
            });
        }

        private void SetLoadPriority(int priorityIndex)
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

        #endregion

        #region Nested Classes

        /// <summary>
        /// Compares solution tree nodes. Node with children "less" then other nodes.
        /// </summary>
        private class SolutionNodeComparer : IComparer
        {
            /// <summary>
            /// Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
            /// </summary>
            /// <returns>
            /// A signed integer that indicates the relative values of <paramref name="x"/> and <paramref name="y"/>, 
            /// as shown in the following table.Value Meaning Less than zero <paramref name="x"/> is less than <paramref name="y"/>. 
            /// Zero <paramref name="x"/> equals <paramref name="y"/>. Greater than zero <paramref name="x"/> is greater than <paramref name="y"/>. 
            /// </returns>
            /// <param name="x">The first object to compare.</param>
            /// <param name="y">The second object to compare.</param>           
            public int Compare(object x, object y)
            {
                var tx = x as TreeNode;
                var ty = y as TreeNode;

                if (0 == tx.GetNodeCount(false) && 0 != ty.GetNodeCount(false))
                    return 1;

                if (0 != tx.GetNodeCount(false) && 0 == ty.GetNodeCount(false))
                    return -1;

                return string.Compare(tx.Text, ty.Text, StringComparison.InvariantCulture);
            }
        }

        #endregion
    }

    internal class PriorityChangedEventArgs : EventArgs
    {
        public PriorityChangedEventArgs(ProjectInfo project)
        {
            Project = project;
        }

        public ProjectInfo Project { get; private set; }
    }
}
