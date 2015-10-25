using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Kolos.SolutionLoadManager.Settings;
using Kolos.SolutionLoadManager.UI;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Kolos.SolutionLoadManager
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    [Guid(GuidList.guidSolutionLoadManagerPkgString)]
    public sealed class SolutionLoadManagerPackage : Package, IVsSolutionLoadManager, IVsSolutionEvents, IVsSolutionLoadEvents
    {
        private readonly HashSet<Guid> _projectGuids = new HashSet<Guid>();
        private readonly Dictionary<String, Guid> _projectNames = new Dictionary<String, Guid>();

        private MenuCommand _loadManagerMenuItem;

        private UInt32 _solutionEventsCoockie;
        private ProjectInfo _rootProject;
        private ProjectInfo _currentProject;
        private ProjectInfo _lastProject;

        private IVsSolutionLoadManagerSupport _loadManagerSupport;
        private ISettingsManager _settingsManager;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public SolutionLoadManagerPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                var menuCommandId = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidSolutionLoadManager);
                _loadManagerMenuItem = new MenuCommand(MenuItemCallback, menuCommandId);
                _loadManagerMenuItem.Visible = false;
                mcs.AddCommand(_loadManagerMenuItem);

                // Create the command for the context menu item.
                var contextMenuCommandId = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidSolutionLoadManagerContext);
                mcs.AddCommand(new MenuCommand(MenuItemCallback, contextMenuCommandId));
            }  
          
            // Subscribe to solution events
            var solution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (null != solution)
            {
                solution.AdviseSolutionEvents(this, out _solutionEventsCoockie);
            }
        }

        #endregion

        private void UpdateProjectLoadPriority(ProjectInfo project)
        {
            UpdateProjectLoadPriority(project.ProjectId, project.Priority);
        }

        private void UpdateProjectLoadPriority(Guid projectId, LoadPriority priority)
        {
            if (null != _loadManagerSupport)
            {
                var projectGuid = projectId;
                int hr = _loadManagerSupport.SetProjectLoadPriority(ref projectGuid, (uint)priority);
            }
        }

        private ProjectInfo UpdateEntireSolution()
        {
            _rootProject = null;
            //Get the solution service so we can traverse each project hierarchy contained within.
            var solution = (IVsSolution)GetService(typeof(SVsSolution));
            if (null != solution)
            {         
                var solutionHierarchy = solution as IVsHierarchy;
                if (null != solutionHierarchy)
                    EnumHierarchyItems(solutionHierarchy, VSConstants.VSITEMID_ROOT, 0, true, false);
            }
            return _rootProject;
        }

        private void ReloadSolution()
        {
            //Get the solution service so we can traverse each project hierarchy contained within.
            var solution = (IVsSolution)GetService(typeof(SVsSolution));
            if (null != solution)
            {
                string directory, fileName, options;
                solution.GetSolutionInfo(out directory, out fileName, out options);
                solution.CloseSolutionElement((uint)__VSSLNSAVEOPTIONS.SLNSAVEOPT_PromptSave, null, 0);
                solution.OpenSolutionFile((uint)__VSSLNOPENOPTIONS.SLNOPENOPT_Silent, fileName);
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void MenuItemCallback(object sender, EventArgs e)
        {            
            var solution = UpdateEntireSolution();

            var form = new SolutionViewForm(solution, _settingsManager);
            form.PriorityChanged += (s, args) => UpdateProjectLoadPriority(args.Project);
            form.ReloadRequested += (s, args) => { ReloadSolution(); form.RootProject = UpdateEntireSolution(); };
            form.ShowDialog();
        }

        #region Enumerate Projects Hierarchy

        /// <summary>
        /// Enumerates over the hierarchy items for the given hierarchy traversing into nested hierarchies.
        /// </summary>
        /// <param name="hierarchy">hierarchy to enmerate over.</param>
        /// <param name="itemid">item id of the hierarchy</param>
        /// <param name="recursionLevel">Depth of recursion. e.g. if recursion started with the Solution
        /// node, then : Level 0 -- Solution node, Level 1 -- children of Solution, etc.</param>
        /// <param name="hierIsSolution">true if hierarchy is Solution Node. This is needed to special
        /// case the children of the solution to work around a bug with VSHPROPID_FirstChild and 
        /// VSHPROPID_NextSibling implementation of the Solution.</param>
        /// <param name="visibleNodesOnly">true if only nodes visible in the Solution Explorer should
        /// be traversed. false if all project items should be traversed.</param>
        /// <param name="processNodeFunc">pointer to function that should be processed on each
        /// node as it is visited in the depth first enumeration.</param>
        private void EnumHierarchyItems(IVsHierarchy hierarchy, uint itemid, int recursionLevel, bool hierIsSolution, bool visibleNodesOnly)
        {
            int hr;
            IntPtr nestedHierarchyObj;
            uint nestedItemId;
            Guid hierGuid = typeof(IVsHierarchy).GUID;

            // Check first if this node has a nested hierarchy. If so, then there really are two 
            // identities for this node: 1. hierarchy/itemid 2. nestedHierarchy/nestedItemId.
            // We will recurse and call EnumHierarchyItems which will display this node using
            // the inner nestedHierarchy/nestedItemId identity.
            hr = hierarchy.GetNestedHierarchy(itemid, ref hierGuid, out nestedHierarchyObj, out nestedItemId);
            if (VSConstants.S_OK == hr && IntPtr.Zero != nestedHierarchyObj)
            {
                var nestedHierarchy = Marshal.GetObjectForIUnknown(nestedHierarchyObj) as IVsHierarchy;
                Marshal.Release(nestedHierarchyObj);    // we are responsible to release the refcount on the out IntPtr parameter
                if (nestedHierarchy != null)
                {
                    // Display name and type of the node in the Output Window
                    EnumHierarchyItems(nestedHierarchy, nestedItemId, recursionLevel, false, visibleNodesOnly);
                }
            }
            else
            {
                object pVar;

                ProcessHierarchyNode(hierarchy, itemid);
                if (!_projectGuids.Contains(_lastProject.ProjectId))
                {
                    recursionLevel++;

                    //Get the first child node of the current hierarchy being walked
                    // NOTE: to work around a bug with the Solution implementation of VSHPROPID_FirstChild,
                    // we keep track of the recursion level. If we are asking for the first child under
                    // the Solution, we use VSHPROPID_FirstVisibleChild instead of _FirstChild. 
                    // In VS 2005 and earlier, the Solution improperly enumerates all nested projects
                    // in the Solution (at any depth) as if they are immediate children of the Solution.
                    // Its implementation _FirstVisibleChild is correct however, and given that there is
                    // not a feature to hide a SolutionFolder or a Project, thus _FirstVisibleChild is 
                    // expected to return the identical results as _FirstChild.
                    hr = hierarchy.GetProperty(itemid, ((visibleNodesOnly || (hierIsSolution && recursionLevel == 1)
                                                        ? (int)__VSHPROPID.VSHPROPID_FirstVisibleChild
                                                        : (int)__VSHPROPID.VSHPROPID_FirstChild)), out pVar);
                    ErrorHandler.ThrowOnFailure(hr);
                    if (VSConstants.S_OK == hr)
                    {
                        _currentProject = _lastProject;

                        //We are using Depth first search so at each level we recurse to check if the node has any children
                        // and then look for siblings.
                        uint childId = GetItemId(pVar);
                        while (childId != VSConstants.VSITEMID_NIL)
                        {
                            EnumHierarchyItems(hierarchy, childId, recursionLevel, false, visibleNodesOnly);
                            // NOTE: to work around a bug with the Solution implementation of VSHPROPID_NextSibling,
                            // we keep track of the recursion level. If we are asking for the next sibling under
                            // the Solution, we use VSHPROPID_NextVisibleSibling instead of _NextSibling. 
                            // In VS 2005 and earlier, the Solution improperly enumerates all nested projects
                            // in the Solution (at any depth) as if they are immediate children of the Solution.
                            // Its implementation   _NextVisibleSibling is correct however, and given that there is
                            // not a feature to hide a SolutionFolder or a Project, thus _NextVisibleSibling is 
                            // expected to return the identical results as _NextSibling.
                            hr = hierarchy.GetProperty(childId, ((visibleNodesOnly || (hierIsSolution && recursionLevel == 1))
                                                                 ? (int)__VSHPROPID.VSHPROPID_NextVisibleSibling
                                                                 : (int)__VSHPROPID.VSHPROPID_NextSibling), out pVar);
                            if (VSConstants.S_OK == hr)
                            {
                                childId = GetItemId(pVar);
                            }
                            else
                            {
                                ErrorHandler.ThrowOnFailure(hr);
                                break;
                            }
                        }

                        _currentProject = _currentProject.Parent;
                    }
                }
            }
        }

        private void ProcessHierarchyNode(IVsHierarchy hierarchy, uint itemid)
        {
            int hr;

            // Canonical Name
            string canonicalName;
            hr = hierarchy.GetCanonicalName(itemid, out canonicalName);

            // Project Name
            object projectName;
            ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_Name, out projectName));

            // Project GUID
            Guid projectGuid;
            hr = hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out projectGuid);
            if (Guid.Empty == projectGuid) // in case when project is unloaded
                _projectNames.TryGetValue(canonicalName, out projectGuid);

            // Project Icon
            var icon = RetrieveProjectIcon(hierarchy);

            // Load Priority
            var loadPriority = RetrieveProjectLoadPriority(projectGuid);

            if (null == _rootProject)
            {
                _rootProject = _lastProject = new ProjectInfo((String)projectName, projectGuid, loadPriority, null) { Icon = icon };
            }
            else
            {
                _lastProject = new ProjectInfo((String)projectName, projectGuid, loadPriority, _currentProject) { Icon = icon };
                _currentProject.Children.Add(_lastProject);
            }
        }

        private static Bitmap RetrieveProjectIcon(IVsHierarchy hierarchy)
        {
            Bitmap icon = null;
            try
            {
                // Try to get icon from image list
                object imageList, index;
                ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_IconImgList, out imageList));
                ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_IconIndex, out index));
                
                IntPtr hIcon = NativeMethods.ImageList_GetIcon(new IntPtr((int)imageList), (int)index, 0);
                icon = Bitmap.FromHicon(hIcon);
            }
            catch (Exception)
            {
                try
                {
                    // There is something wrong with image list. Try to use icon handle instead
                    object iconHandle;
                    ErrorHandler.ThrowOnFailure(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_IconHandle, out iconHandle));
                    icon = Bitmap.FromHicon(new IntPtr((int)iconHandle));
                }
                catch (Exception)
                {
                    // We didn't find project icon... Well, let's go on without one.
                }
            }
            return icon;
        }

        private LoadPriority RetrieveProjectLoadPriority(Guid projectGuid)
        {
            var loadPriority = LoadPriority.DemandLoad;
            if (Guid.Empty != projectGuid)
            {
                UInt32 loadState;
                if (VSConstants.S_OK == _loadManagerSupport.GetProjectLoadPriority(ref projectGuid, out loadState))
                {
                    loadPriority = (LoadPriority)loadState;

                    // Update active profile settings
                    _settingsManager.SetProjectLoadPriority(_settingsManager.ActiveProfile, projectGuid, loadPriority);
                }
            }
            return loadPriority;
        }

        /// <summary>
        /// Gets the item id.
        /// </summary>
        /// <param name="pvar">VARIANT holding an itemid.</param>
        /// <returns>Item Id of the concerned node</returns>
        private uint GetItemId(object pvar)
        {
            if (pvar == null) return VSConstants.VSITEMID_NIL;
            if (pvar is int) return (uint)(int)pvar;
            if (pvar is uint) return (uint)pvar;
            if (pvar is short) return (uint)(short)pvar;
            if (pvar is ushort) return (uint)(ushort)pvar;
            if (pvar is long) return (uint)(long)pvar;
            return VSConstants.VSITEMID_NIL;
        }

        #endregion

        #region IVsSolutionLoadManager Members
        
        public int OnBeforeOpenProject(ref Guid guidProjectID, ref Guid guidProjectType, string pszFileName, IVsSolutionLoadManagerSupport pSLMgrSupport)
        {
            _loadManagerSupport = pSLMgrSupport;

            _projectGuids.Add(guidProjectID);
            _projectNames.Add(pszFileName, guidProjectID);

            // Set project priority according to profile
            var priority = _settingsManager.GetProjectLoadPriority(_settingsManager.ActiveProfile, guidProjectID);
            UpdateProjectLoadPriority(guidProjectID, priority);
            
            return VSConstants.S_OK;
        }

        public int OnDisconnect()
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsSolutionEvents Members

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            _rootProject = _currentProject = _lastProject = null;
            _loadManagerMenuItem.Visible = false;

            _projectNames.Clear();
            _projectGuids.Clear();

            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            _loadManagerMenuItem.Visible = true;
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {           
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        #endregion

        #region IVsSolutionLoadEvents

        public int OnAfterBackgroundSolutionLoadComplete()
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeBackgroundSolutionLoadBegins()
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeLoadProjectBatch(bool fIsBackgroundIdleBatch)
        {
            return VSConstants.S_OK;
        }

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

        public int OnQueryBackgroundLoadProjectBatch(out bool pfShouldDelayLoadToNextIdle)
        {
            pfShouldDelayLoadToNextIdle = false;
            return VSConstants.S_OK;
        }

        #endregion
    }
}
