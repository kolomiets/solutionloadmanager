using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using EnvDTE;
using System.IO;

namespace EMCCaptiva.SolutionLoadManager
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
    public sealed class SolutionLoadManagerPackage : Package, IVsSolutionLoadManager, IVsSolutionEvents
    {
        private MenuCommand m_LoadManagerMenuItem;

        private UInt32 m_SolutionEventsCoockie;
        private ProjectInfo m_RootProject;
        private ProjectInfo m_CurrentProject;
        private ProjectInfo m_LastProject;

        private IVsSolutionLoadManagerSupport m_LoadManagerSupport;
        private bool m_ForceProjectLoad;

        private const String MyOptionKey = "SolutionLoadManager";

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
            AddOptionKey(MyOptionKey);
        }

        protected override void OnLoadOptions(string key, Stream stream)
        {
            // if key is a string this package cares about then
            // stream can be used to read the data for runtime use
            if (key == MyOptionKey)
            {
                try
                {
                    var formatter = new BinaryFormatter();
                    formatter.Binder = new ProjectInfoBinder();
                    m_RootProject = (ProjectInfo)formatter.Deserialize(stream);
                }
                catch (Exception)
                {
                    // no options, ok, no problem.
                }
            }
            else
            {
                // If this isn't a key this package cares about, 
                // then call the base class implementation
                base.OnLoadOptions(key, stream);
            }
        }

        protected override void OnSaveOptions(string key, Stream stream)
        {
            // if key is a string this package cares about then
            // stream can be used to write the data
            if (key == MyOptionKey)
            {
                try
                {
                    if (null != m_RootProject)
                    {
                        var formatter = new BinaryFormatter();
                        formatter.Serialize(stream, m_RootProject);
                    }
                }
                catch (Exception)
                {
                    // no options, ok, no problem.
                }
            }
            else
            {
                // if this isn't a key this package cares about,
                // then call the base class implementation
                base.OnSaveOptions(key, stream);
            }
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
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
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the menu item.
                CommandID menuCommandID = new CommandID(GuidList.guidSolutionLoadManagerCmdSet, (int)PkgCmdIDList.cmdidSolutionLoadManager);
                m_LoadManagerMenuItem = new MenuCommand(MenuItemCallback, menuCommandID);
                m_LoadManagerMenuItem.Visible = false;
                mcs.AddCommand(m_LoadManagerMenuItem);
            }
            
            IVsSolution solution = GetService(typeof(SVsSolution)) as IVsSolution;
            if (null != solution)
            {
                solution.AdviseSolutionEvents(this, out m_SolutionEventsCoockie);

                Object selectedLoadManager;
                solution.GetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, out selectedLoadManager);
                if (this != selectedLoadManager)
                    solution.SetProperty((int)__VSPROPID4.VSPROPID_ActiveSolutionLoadManager, this);
            }
            
        }
        #endregion

        private void UpdateProjectLoadPriority(ProjectInfo project)
        {
            if (null != m_LoadManagerSupport)
            {
                Guid projectGuid = project.ProjectId;
                _VSProjectLoadPriority loadState = _VSProjectLoadPriority.PLP_DemandLoad;
                switch(project.Priority)
                {
                    case LoadPriority.DemandLoad:
                        loadState = _VSProjectLoadPriority.PLP_DemandLoad;
                        break;
                    case LoadPriority.BackgroundLoad:
                        loadState = _VSProjectLoadPriority.PLP_BackgroundLoad;
                        break;
                    case LoadPriority.LoadIfNeeded:
                        loadState = _VSProjectLoadPriority.PLP_LoadIfNeeded;
                        break;
                    case LoadPriority.ExplicitLoadOnly:
                        loadState = _VSProjectLoadPriority.PLP_ExplicitLoadOnly;
                        break;
                }

                int hr = m_LoadManagerSupport.SetProjectLoadPriority(ref projectGuid, (uint)loadState);
            }
        }

        private ProjectInfo UpdateEntireSolution()
        {
            m_RootProject = null;
            //Get the solution service so we can traverse each project hierarchy contained within.
            IVsSolution solution = (IVsSolution)GetService(typeof(SVsSolution));
            if (null != solution)
            {         
                IVsHierarchy solutionHierarchy = solution as IVsHierarchy;
                if (null != solutionHierarchy)
                {
                    OutputCommandString("\n\nTraverse All Items Recursively:\n");
                    EnumHierarchyItems(solutionHierarchy, VSConstants.VSITEMID_ROOT, 0, true, false);
                    
                }
            }
            return m_RootProject;
        }

        private void ReloadSolution()
        {
            //Get the solution service so we can traverse each project hierarchy contained within.
            IVsSolution solution = (IVsSolution)GetService(typeof(SVsSolution));
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
            var form = new SolutionViewForm { RootProject = m_RootProject ?? UpdateEntireSolution() };
            form.PriorityChanged += (s, args) => UpdateProjectLoadPriority(args.Project);
            form.ReloadRequested += (s, args) => ReloadSolution();
            form.RefreshRequested += (s, args) =>
                {
                    m_ForceProjectLoad = true;
                    ReloadSolution();
                    m_ForceProjectLoad = false;
                    form.RootProject = UpdateEntireSolution();

                };
            form.ShowDialog();
            return;
        }

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
                IVsHierarchy nestedHierarchy = Marshal.GetObjectForIUnknown(nestedHierarchyObj) as IVsHierarchy;
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

                // Display name and type of the node in the Output Window
                DisplayHierarchyNode(hierarchy, itemid, recursionLevel);

                hr = hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_TypeName, out pVar);
                if (String.IsNullOrEmpty((string)pVar))
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
                    hr = hierarchy.GetProperty(itemid,
                                               ((visibleNodesOnly || (hierIsSolution && recursionLevel == 1)
                                                     ?
                                                         (int)__VSHPROPID.VSHPROPID_FirstVisibleChild
                                                     : (int)__VSHPROPID.VSHPROPID_FirstChild)),
                                               out pVar);
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
                    if (VSConstants.S_OK == hr)
                    {
                        m_CurrentProject = m_LastProject;

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
                            hr = hierarchy.GetProperty(childId,
                                                       ((visibleNodesOnly || (hierIsSolution && recursionLevel == 1))
                                                            ?
                                                                (int)__VSHPROPID.VSHPROPID_NextVisibleSibling
                                                            : (int)__VSHPROPID.VSHPROPID_NextSibling),
                                                       out pVar);
                            if (VSConstants.S_OK == hr)
                            {
                                childId = GetItemId(pVar);
                            }
                            else
                            {
                                Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(hr);
                                break;
                            }
                        }

                        m_CurrentProject = m_CurrentProject.Parent;
                    }
                }
            }
        }

        /// <summary>
        /// This function diplays the name of the Hierarchy node. This function is passed to the 
        /// Hierarchy enumeration routines to process the current node.
        /// </summary>
        /// <param name="hierarchy">Hierarchy of the current node</param>
        /// <param name="itemid">Itemid of the current node</param>
        /// <param name="recursionLevel">Depth of recursion in hierarchy enumeration. We add one tab
        /// for each level in the recursion.</param>
        private void DisplayHierarchyNode(IVsHierarchy hierarchy, uint itemid, int recursionLevel)
        {
            object pVar;
            int hr;

            string text = "";

            for (int i = 0; i < recursionLevel; i++)
                text += "\t";

            //Get the name of the root node in question here and dump its value
            hr = hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_Name, out pVar);
            text += (string)pVar;

            Guid projectGuid = Guid.Empty;
            hr = hierarchy.GetGuidProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out projectGuid);

            LoadPriority loadPriority = LoadPriority.DemandLoad;
            if (Guid.Empty != projectGuid)
            {
                UInt32 loadState;
                m_LoadManagerSupport.GetProjectLoadPriority(ref projectGuid, out loadState);
                switch ((_VSProjectLoadPriority)loadState)
                {
                    case _VSProjectLoadPriority.PLP_DemandLoad:
                        loadPriority = LoadPriority.DemandLoad;
                        break;
                    case _VSProjectLoadPriority.PLP_BackgroundLoad:
                        loadPriority = LoadPriority.BackgroundLoad;
                        break;
                    case _VSProjectLoadPriority.PLP_LoadIfNeeded:
                        loadPriority = LoadPriority.LoadIfNeeded;
                        break;
                    case _VSProjectLoadPriority.PLP_ExplicitLoadOnly:
                        loadPriority = LoadPriority.ExplicitLoadOnly;
                        break;
                }
            }

            if (null == m_RootProject)
            {
                m_RootProject = m_LastProject = new ProjectInfo((string)pVar, projectGuid, loadPriority, null);
            }
            else
            {
                m_LastProject = new ProjectInfo((string)pVar, projectGuid, loadPriority, m_CurrentProject);
                m_CurrentProject.Children.Add(m_LastProject);
            }

            // Create Project information tree
            hr = hierarchy.GetProperty(itemid, (int)__VSHPROPID.VSHPROPID_TypeName, out pVar);
            text += String.Format(" ({0})", (string)pVar);

            OutputCommandString(text);
        }

        /// <summary>
        /// This functions prints on the debug ouput and on the generic pane of the output window
        /// a text.
        /// </summary>
        /// <param name="text">text to send to Output Window.</param>
        private static void OutputCommandString(string text)
        {
            // Build the string to write on the debugger and output window.
            StringBuilder outputText = new StringBuilder(text);
            outputText.Append("\n");

            // Now print the string on the output window.
            // The first step is to get a reference to IVsOutputWindow.
            IVsOutputWindow outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            // If we fail to get it we can exit now.
            if (null == outputWindow)
            {
                Trace.WriteLine("Failed to get a reference to IVsOutputWindow");
                return;
            }
            // Now get the window pane for the general output.
            Guid guidGeneral = Microsoft.VisualStudio.VSConstants.GUID_OutWindowGeneralPane;
            IVsOutputWindowPane windowPane;
            if (Microsoft.VisualStudio.ErrorHandler.Failed(outputWindow.GetPane(ref guidGeneral, out windowPane)))
            {
                Trace.WriteLine("Failed to get a reference to the Output Window General pane");
                return;
            }
            if (Microsoft.VisualStudio.ErrorHandler.Failed(windowPane.OutputString(outputText.ToString())))
            {
                Trace.WriteLine("Failed to write on the output window");
            }
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
        
        #region IVsSolutionLoadManager Members

        public int OnBeforeOpenProject(ref Guid guidProjectID, ref Guid guidProjectType, string pszFileName, IVsSolutionLoadManagerSupport pSLMgrSupport)
        {
            m_LoadManagerSupport = pSLMgrSupport;

            if (m_ForceProjectLoad)
                pSLMgrSupport.SetProjectLoadPriority(guidProjectID, (uint)_VSProjectLoadPriority.PLP_DemandLoad);
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
            m_RootProject = m_CurrentProject = m_LastProject = null;
            m_LoadManagerMenuItem.Visible = false;
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

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            m_LoadManagerMenuItem.Visible = true;
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
    }
}
