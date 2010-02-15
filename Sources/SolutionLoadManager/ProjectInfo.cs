using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Drawing;

namespace EMCCaptiva.SolutionLoadManager
{
    [Serializable]
    public enum LoadPriority
    {
        DemandLoad,
        BackgroundLoad,
        LoadIfNeeded,
        ExplicitLoadOnly
    }

    [Serializable]
    public class ProjectInfo
    {
        private readonly List<ProjectInfo> m_Children = new List<ProjectInfo>();

        public ProjectInfo(String name, Guid projectId, LoadPriority priority, ProjectInfo parent)
        {
            Name = name;
            ProjectId = projectId;
            Priority = priority;
            Parent = parent;
        }

        public Bitmap Icon { get; set; }

        public String Name { get; private set; }

        public Guid ProjectId { get; private set; }

        public LoadPriority Priority { get; set; }

        public ProjectInfo Parent { get; private set; }
        
        public List<ProjectInfo> Children 
        {
            get { return m_Children; }
        }
    }

    class ProjectInfoBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            var typeInfo = typeName.Split('.');

            //The latest item is the class name
            var className = typeInfo[typeInfo.Length - 1];
            if (className.Equals("ProjectInfo"))
                return typeof(ProjectInfo);

            return Type.GetType(string.Format("{0}, {1}", typeName, assemblyName));
        }
    }

}
