using System;
using System.Collections.Generic;
using System.Drawing;

namespace Kolos.SolutionLoadManager.Settings
{
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
        public ProjectInfo(string name, Guid projectId, LoadPriority priority, ProjectInfo parent)
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
        public string Name { get; private set; }

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
