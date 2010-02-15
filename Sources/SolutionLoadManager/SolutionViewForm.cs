using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;

namespace EMCCaptiva.SolutionLoadManager
{
    public partial class SolutionViewForm : Form
    {
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

        public SolutionViewForm()
        {
            InitializeComponent();
            treeView1.TreeViewNodeSorter = new NodeSorter();
        }

        public ProjectInfo RootProject
        {
            set { UpdateTree(value); }
        }

        private void UpdateTree(ProjectInfo info)
        {
            treeView1.BeginUpdate();
            treeView1.Nodes.Clear();
            if (null != info)
            {
                var rootNode = CreateTreeNode(info);
                treeView1.Nodes.Add(rootNode);
                treeView1.Sort();

                rootNode.Expand();
            }
            treeView1.EndUpdate();
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
            treeView1.SelectedNode = e.Node;
        }

        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            foreach (TreeNode child in e.Node.Nodes)
                child.Checked = e.Node.Checked;
        }

        private void UpdateCheckedProjectsPriority(LoadPriority priority)
        {
            UpdateNodes(treeView1.Nodes[0], n =>
            {
                if (n.Checked)
                {
                    var info = (ProjectInfo)n.Tag;
                    info.Priority = priority;
                    OnPriorityChanged(new PriorityChangedEventArgs(info));

                    // Show load priory only for projects
                    if (0 == n.GetNodeCount(false))
                        n.BackColor = GetPriorityColor(info);
                }
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
            var node = treeView1.SelectedNode;
            if (null != node)
                node.ExpandAll();
            else
                treeView1.ExpandAll();
        }

        private void collapseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var node = treeView1.SelectedNode;
            if (null != node)
                node.Collapse();
            else
                treeView1.CollapseAll();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.Nodes[0].Checked = true;
        }

        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.Nodes[0].Checked = false;
        }

        private void priorityComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (priorityComboBox.SelectedIndex)
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

            priorityComboBox.SelectedIndex = -1;
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void refreshButton_Click(object sender, EventArgs e)
        {
            OnRefreshRequested(EventArgs.Empty);
        }

        private void reloadButton_Click(object sender, EventArgs e)
        {
            OnReloadRequested(EventArgs.Empty);
        }

        #region Events

        public event EventHandler<PriorityChangedEventArgs> PriorityChanged;

        private void OnPriorityChanged(PriorityChangedEventArgs e)
        {
            var handler = PriorityChanged;
            if (null != handler)
                handler(this, e);
        }

        public event EventHandler RefreshRequested;

        private void OnRefreshRequested(EventArgs e)
        {
            var handler = RefreshRequested;
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
