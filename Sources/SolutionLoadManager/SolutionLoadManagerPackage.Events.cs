using Kolos.SolutionLoadManager.Settings;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Kolos.SolutionLoadManager
{
    partial class SolutionLoadManagerPackage: IVsSolutionEvents, IVsSolutionLoadEvents
    {
        #region IVsSolutionEvents Members

        /// <inheritdoc/>
        public int OnAfterCloseSolution(object pUnkReserved)
        {
            _rootProject = _currentProject = _lastProject = null;

			// Tools menu
            _settingsToolsButton.Visible = false;
            EnableToolbarControls(false);

            _projectNames.Clear();
            _projectGuids.Clear();

            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
			// Tools menu
            _settingsToolsButton.Visible = true;		
			EnableToolbarControls(true);

            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsSolutionLoadEvents

        /// <inheritdoc/>
        public int OnAfterBackgroundSolutionLoadComplete()
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnBeforeOpenSolution(string pszSolutionFilename)
        {
            // Activate solution load manager
            var solution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (null != solution)
            {
                object selectedLoadManager;
                solution.GetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, out selectedLoadManager);
                if (this != selectedLoadManager)
                    solution.SetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, this);
            }

            _settingsManager = new XmlSettingsManager(pszSolutionFilename);

            return VSConstants.S_OK;
        }

        /// <inheritdoc/>
        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
