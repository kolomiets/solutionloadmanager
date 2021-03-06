﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
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
    [InstalledProductRegistration("#110", "#112", "0.6.1", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(UIContextGuids.NoSolution)]
    [Guid(GuidList.guidSolutionLoadManagerPkgString)]
    public sealed partial class SolutionLoadManagerPackage : Package, IVsSolutionLoadManager
    {
        private readonly HashSet<Guid> _projectGuids = new HashSet<Guid>();
        private readonly Dictionary<string, Guid> _projectNames = new Dictionary<string, Guid>();

        private MenuCommand _settingsToolsButton;
        private MenuCommand _activeProfileCombo;
        private MenuCommand _settingsToolbarButton;
        private MenuCommand _reloadToolbarButton;

        private uint _solutionEventsCoockie;
        private ProjectInfo _rootProject;
        private ProjectInfo _currentProject;
        private ProjectInfo _lastProject;

        private ISettingsManager _settingsManager;
        private IVsSolutionLoadManagerSupport _loadManagerSupport;

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
                _settingsToolsButton = new MenuCommand(OnOpenSettingsMenuItem, menuCommandId);
                _settingsToolsButton.Visible = false;
                mcs.AddCommand(_settingsToolsButton);

                // Create commands for the context menu.
                var settingsContextMenuCommandId = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidSolutionLoadManagerContext);
                mcs.AddCommand(new MenuCommand(OnOpenSettingsMenuItem, settingsContextMenuCommandId));

                var reloadContextMenuCommandId = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidReloadSolutionContext);
                mcs.AddCommand(new MenuCommand(OnReloadSolutionMenuItem, reloadContextMenuCommandId));
                
                // Initialize Active Profile combo.          
                var activeProfileComboCommandId = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidActiveProfileCombo);
                _activeProfileCombo = new OleMenuCommand(OnMenuMyDropDownCombo, activeProfileComboCommandId);
                mcs.AddCommand(_activeProfileCombo);

                // Initialize the "GetList" command for Active Profile combo.
                var activeProfileComboGetListCommandId = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidActiveProfileComboGetList);
                var activeProfileComboGetListCommand = new OleMenuCommand(OnMenuMyDropDownComboGetList, activeProfileComboGetListCommandId);
                mcs.AddCommand(activeProfileComboGetListCommand);
               
                var settingsToolbarCommandId = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidSolutionLoadManagerToolbar);
                _settingsToolbarButton = new MenuCommand(OnOpenSettingsMenuItem, settingsToolbarCommandId);
                mcs.AddCommand(_settingsToolbarButton);

                var reloadToobarCommandId = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidReloadSolutionToolbar);
                _reloadToolbarButton = new MenuCommand(OnReloadSolutionMenuItem, reloadToobarCommandId);
                mcs.AddCommand(_reloadToolbarButton);

                // Disable button by default
                EnableToolbarControls(false);              
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
        /// This function is the callback used to execute a command when "Settings..." context menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void OnOpenSettingsMenuItem(object sender, EventArgs e)
        {            
            var solution = UpdateEntireSolution();

            var form = new SolutionViewForm(solution, _settingsManager);
            form.PriorityChanged += (s, args) => UpdateProjectLoadPriority(args.Project);
            form.ReloadRequested += (s, args) => { ReloadSolution(); form.RootProject = UpdateEntireSolution(); };
            form.ShowDialog();
        }

        /// <summary>
        /// This function is the callback used to execute a command when "Reload Solution" menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void OnReloadSolutionMenuItem(object sender, EventArgs e)
        {
            ReloadSolution();
        }

        private void OnMenuMyDropDownCombo(object sender, EventArgs e)
        {
            var eventArgs = e as OleMenuCmdEventArgs;
            if (eventArgs != null)
            {
                var input = eventArgs.InValue;
                var output = eventArgs.OutValue;

                if (output != IntPtr.Zero)
                {
                    // The IDE requests for the current value
                    Marshal.GetNativeVariantForObject(_settingsManager.ActiveProfile, output);
                }
                else if (input != null)
                {
                    // New value was selected 
                    var inputString = input.ToString();
                    if (!string.Equals(_settingsManager.ActiveProfile, inputString))
                    {
                        _settingsManager.ActiveProfile = input.ToString();
                        ReloadSolution();
                    }
                }
            }
        }

        private void OnMenuMyDropDownComboGetList(object sender, EventArgs e)
        {
            var eventArgs = e as OleMenuCmdEventArgs;
            if (eventArgs != null)
            {
                var input = eventArgs.InValue;
                var output = eventArgs.OutValue;

                if (input == null && output != IntPtr.Zero)
                {
                    var profiles = _settingsManager.Profiles.ToArray();
                    Marshal.GetNativeVariantForObject(profiles, output);
                }
            }
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
                uint loadState;
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

        private void EnableToolbarControls(bool enabled)
        {
            _activeProfileCombo.Enabled = _settingsToolbarButton.Enabled = _reloadToolbarButton.Enabled = enabled;
        }

        #endregion

        #region IVsSolutionLoadManager Members

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public int OnDisconnect()
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
