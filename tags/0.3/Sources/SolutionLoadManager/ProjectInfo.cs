using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Drawing;
using Microsoft.VisualStudio.Shell.Interop;

namespace Kolos.SolutionLoadManager
{
    [Serializable]
    public enum LoadPriority
    {
        DemandLoad = _VSProjectLoadPriority.PLP_DemandLoad,
        BackgroundLoad = _VSProjectLoadPriority.PLP_BackgroundLoad,
        LoadIfNeeded = _VSProjectLoadPriority.PLP_LoadIfNeeded,
        ExplicitLoadOnly = _VSProjectLoadPriority.PLP_ExplicitLoadOnly
    }

    [Serializable]
    public class ProjectInfo
    {
        public ProjectInfo(String name, Guid projectId, LoadPriority priority, ProjectInfo parent)
        {
            Name = name;
            ProjectId = projectId;
            Priority = priority;
            Parent = parent;
            Children = new List<ProjectInfo>();
        }

        public Bitmap Icon { get; set; }

        public String Name { get; private set; }

        public Guid ProjectId { get; private set; }

        public LoadPriority Priority { get; set; }

        public ProjectInfo Parent { get; private set; }
        
        public List<ProjectInfo> Children { get; private set; }
    }
}
