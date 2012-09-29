using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.VisualStudio.Shell.Interop;

namespace Kolos.SolutionLoadManager
{
    /// <summary>
    /// Load priority of the project within Visual Studio solution.
    /// </summary>
    [Serializable]
    public enum LoadPriority
    {
        /// <summary>
        /// When a solution is opened, projects are loaded asynchronously. If this priority is 
        /// set on an unloaded project after the solution is already open, 
        /// the project will be loaded at the next idle point.
        /// </summary>
        DemandLoad = _VSProjectLoadPriority.PLP_DemandLoad,

        /// <summary>
        /// When a solution is opened, projects are loaded in the background, allowing the user 
        /// to access the projects as they are loaded without having to wait until all the projects are loaded.
        /// </summary>
        BackgroundLoad = _VSProjectLoadPriority.PLP_BackgroundLoad,

        /// <summary>
        /// Projects are loaded when they are accessed. A project is accessed when the user expands 
        /// the project node in the Solution Explorer, when a file belonging to the project is opened 
        /// when the solution opens because it is in the open document list (persisted in the solution's user options file), 
        /// or when another project that is being loaded has a dependency on the project.
        /// </summary>
        LoadIfNeeded = _VSProjectLoadPriority.PLP_LoadIfNeeded,

        /// <summary>
        /// Projects are not to be loaded unless the user explicitly requests it. 
        /// This is the case when projects are explicitly unloaded.
        /// </summary>
        ExplicitLoadOnly = _VSProjectLoadPriority.PLP_ExplicitLoadOnly
    }

    /// <summary>
    /// Supplementary information about Visual Studio project.
    /// </summary>
    [Serializable]
    public class ProjectInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectInfo"/> class. 
        /// </summary>
        /// <param name="name">Name of the project.</param>
        /// <param name="projectId">Project ID.</param>
        /// <param name="priority">Project load priority.</param>
        /// <param name="parent">Parent project, if any.</param>
        public ProjectInfo(String name, Guid projectId, LoadPriority priority, ProjectInfo parent)
        {
            Name = name;
            ProjectId = projectId;
            Priority = priority;
            Parent = parent;
            Children = new List<ProjectInfo>();
        }

        /// <summary>
        /// Gets or sets project icon.
        /// </summary>
        public Bitmap Icon { get; set; }

        /// <summary>
        /// Gets project name.
        /// </summary>
        public String Name { get; private set; }

        /// <summary>
        /// Gets project ID.
        /// </summary>
        public Guid ProjectId { get; private set; }

        /// <summary>
        /// Gets project load priority.
        /// </summary>
        public LoadPriority Priority { get; set; }

        /// <summary>
        /// Gets project parent.
        /// </summary>
        public ProjectInfo Parent { get; private set; }
        
        /// <summary>
        /// Gets children projects.
        /// </summary>
        public List<ProjectInfo> Children { get; private set; }
    }
}
