using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Kolos.SolutionLoadManager.Settings
{
    public interface ISettingsManager
    {
        String ActiveProfile { get; set; }
        IEnumerable<String> Profiles { get; }

        void AddProfile(String profile, String copySettingsFrom);
        void RemoveProfile(String profile);
        void RenameProfile(String profile, String newProfile);

        void SetProjectLoadPriority(String profile, Guid projectGuid, LoadPriority priority);
        LoadPriority GetProjectLoadPriority(String profile, Guid projectGuid);
    }
}
