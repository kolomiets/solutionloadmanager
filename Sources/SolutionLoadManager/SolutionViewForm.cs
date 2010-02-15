using System;
using System.Drawing;
using System.Windows.Forms;

namespace EMCCaptiva.SolutionLoadManager
{
    public partial class SolutionViewForm : Form
    {
        private static readonly Color DemandLoadColor = Color.FromArgb(255, 192, 192);
        private static readonly Color BackgroundLoadColor = Color.FromArgb(255, 224, 192);
        private static readonly Color LoadIfNeededColor = Color.FromArgb(255, 255, 192);
        private static readonly Color ExplicitLoadOnlyColor = Color.FromArgb(192, 255, 192);

        public SolutionViewForm()
        {
            InitializeComponent();
        }

        public ProjectInfo RootProject
        {
            set { UpdateTree(value); }
        }

        private void UpdateTree(ProjectInfo info)
        {
            treeView1.Nodes.Clear();
            if (null != info)
            {
                var rootNode = CreateTreeNode(info);
                treeView1.Nodes.Add(rootNode);
                treeView1.Sort();

                rootNode.Expand();
            }
        }

        private static TreeNode CreateTreeNode(ProjectInfo info)
        {
            var node = new TreeNode(info.Name) {Tag = info, BackColor = GetPriorityColor(info.Priority)};
            foreach (var child in info.Children)
                node.Nodes.Add(CreateTreeNode(child));

            return node;
        }

        private static Color GetPriorityColor(LoadPriority loadPriority)
        {
            switch (loadPriority)
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

                    n.BackColor = GetPriorityColor(priority);
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
