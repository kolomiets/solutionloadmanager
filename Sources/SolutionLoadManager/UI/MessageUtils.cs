using System.Windows.Forms;

namespace Kolos.SolutionLoadManager.UI
{
    /// <summary>
    /// Class provides utility methods to simplify work with user notifications.
    /// </summary>
    internal static class MessageUtils
    {
        /// <summary>
        /// Shows simple warning message to the user.
        /// </summary>
        /// <param name="message"></param>
        public static void ShowWarning(string message)
        {
            MessageBox.Show(message, Resources.ExtensionName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        /// <summary>
        /// Asks user simple OK/Cancel question.
        /// </summary>
        /// <param name="message">Text of the question.</param>
        /// <returns>Returns true if user hit OK button.</returns>
        public static bool AskOkCancelQuestion(string message)
        {
            return MessageBox.Show(message, 
                                   Resources.ExtensionName, 
                                   MessageBoxButtons.OKCancel, 
                                   MessageBoxIcon.Question) == DialogResult.OK;            
        }
    }
}
