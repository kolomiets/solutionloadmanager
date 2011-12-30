using System;
using System.Windows.Forms;

namespace Kolos.SolutionLoadManager.UI
{
    /// <summary>
    /// Helper class that shows wait cursor during long operation.
    /// </summary>
    class WaitCursor : IDisposable
    {
        private readonly Form _parentForm;
        private readonly Cursor _originalCursor;

        public WaitCursor(Form parentForm)
        {
            _parentForm = parentForm;
            _originalCursor = _parentForm.Cursor;
            _parentForm.Cursor = Cursors.WaitCursor;
        }

        public void Dispose()
        {
            _parentForm.Cursor = _originalCursor;
        }
    }
}
