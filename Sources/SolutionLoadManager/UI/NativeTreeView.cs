using System.Windows.Forms;

namespace Kolos.SolutionLoadManager.UI
{
    /// <summary>
    /// Slightly updated TreeView control with enabled visual styles 
    /// (nice triangles on Windows Vista/7 instead '+'/'-' buttons).
    /// </summary>
    public class NativeTreeView : TreeView
    {
        /// <inheritdoc/>
        protected override void CreateHandle()
        {
            base.CreateHandle();
            NativeMethods.SetWindowTheme(Handle, "explorer", null);
        }
    }
}
