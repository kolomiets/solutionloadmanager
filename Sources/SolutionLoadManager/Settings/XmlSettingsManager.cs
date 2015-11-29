using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Kolos.SolutionLoadManager.Settings
{
    /// <summary>
    /// <para>
    /// Settings manager implementation that stores project load settings 
    /// alongside with VS solution file (in separate file with solution_file_name.sln.slm file name).
    /// </para>
    /// <para>
    /// Thanks for Jenks Hofmann for initial implementation.
    /// </para>
    /// </summary>
    public class XmlSettingsManager : ISettingsManager
    {
        #region Fields

        private const string SettingsFileExtension = ".slm";
        private const string ActiveProfilePropertyName = "<Active Profile>";

        private readonly SolutionLoadInfo _solutionLoadInfo;
        private readonly string _solutionFilePath;
        
        #endregion

        #region Ctors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSettingsManager"/> class. 
        /// </summary>
        /// <param name="solutionFilePath">Path to the VS solution file (.sln).</param>
        public XmlSettingsManager(string solutionFilePath)
        {
            if (string.IsNullOrEmpty(solutionFilePath))
                throw new ArgumentException("solutionFilePath is null or empty!");

            if (!File.Exists(solutionFilePath))
                throw new ArgumentException("The provided solution file does not exist: " + solutionFilePath);

            _solutionFilePath = solutionFilePath;
            _solutionLoadInfo = ReadSettings() ?? CreateDefaultSolutionSettings();
        }

        #endregion

        #region ISettingsManager Members

        /// <summary>
        /// Gets or sets active profile name.
        /// </summary>
        public string ActiveProfile
        {
            get
            {
                return _solutionLoadInfo.ActiveProfileName;
            }

            set
            {
                var profile = GetExistingProfile(value);
                _solutionLoadInfo.ActiveProfileName = profile.ProfileName;
                WriteSettings();
            }
        }

        /// <summary>
        /// Gets list of available profile names.
        /// </summary>
        public IEnumerable<string> Profiles
        {
            get { return _solutionLoadInfo.Profiles.Select(p => p.ProfileName); }
        }

        /// <summary>
        /// Adds new profile. Existing profile name can be specified to copy settings from.
        /// </summary>
        /// <param name="profile">Name of the new profile.</param>
        /// <param name="copySettingsFrom">Existing profile name or empty/null string.</param>
        public void AddProfile(string profile, string copySettingsFrom)
        {
            if (_solutionLoadInfo.GetProfile(profile) != null)
                throw new ArgumentException("Profile already exists:" + profile);

            SolutionLoadProfile sourceProfile = null;
            if (!string.IsNullOrEmpty(copySettingsFrom))
            {
                sourceProfile = _solutionLoadInfo.GetProfile(copySettingsFrom);
                if (sourceProfile == null)
                    throw new ArgumentException("Profile to copy settings from does not exist:" + copySettingsFrom);
            }

            SolutionLoadProfile newProfile;
            if (sourceProfile != null)
            {
                newProfile = sourceProfile.Clone();
                newProfile.ProfileName = profile;
            }
            else
            {
                newProfile = new SolutionLoadProfile(profile);
            }

            _solutionLoadInfo.Profiles.Add(newProfile);
            WriteSettings();
        }

        /// <summary>
        /// Removes existing profile.
        /// </summary>
        /// <param name="profile">Existing profile name.</param>
        public void RemoveProfile(string profile)
        {
            var sourceProfile = GetExistingProfile(profile);
            _solutionLoadInfo.Profiles.Remove(sourceProfile);
            WriteSettings();
        }

        /// <summary>
        /// Renames existing profile to the new name.
        /// </summary>
        /// <param name="profile">Existing profile name.</param>
        /// <param name="newProfile">New profile name.</param>
        public void RenameProfile(string profile, string newProfile)
        {
            var sourceProfile = GetExistingProfile(profile);

            var targetProfile = _solutionLoadInfo.GetProfile(newProfile);
            if (targetProfile != null)
                throw new ArgumentException("New profile name does already exist:" + newProfile);

            sourceProfile.ProfileName = newProfile;
            WriteSettings();
        }

        /// <summary>
        /// Sets project load priority for specified profile.
        /// </summary>
        /// <param name="profile">Existing profile name.</param>
        /// <param name="projectGuid">Project ID.</param>
        /// <param name="priority">Load priority of the project.</param>
        /// <returns>Load priority of the project.</returns>
        public void SetProjectLoadPriority(string profile, Guid projectGuid, LoadPriority priority)
        {
            var profileInfo = GetExistingProfile(profile);
            var project = profileInfo.GetProject(projectGuid);

            if (project == null)
            {
                project = new ProjectLoadInfo { ProjectGuid = projectGuid };
                profileInfo.Projects.Add(project);
            }

            project.LoadPriority = priority;
            WriteSettings();
        }

        /// <summary>
        /// Gets project load priority for specified profile.
        /// </summary>
        /// <param name="profile">Existing profile name.</param>
        /// <param name="projectGuid">Project ID.</param>
        /// <returns>Load priority of the project.</returns>
        public LoadPriority GetProjectLoadPriority(string profile, Guid projectGuid)
        {
            var profileInfo = GetExistingProfile(profile);
            var project = profileInfo.GetProject(projectGuid);
            return (project == null) ? LoadPriority.DemandLoad : project.LoadPriority;
        }

        #endregion

        #region Private Methods

        private SolutionLoadProfile GetExistingProfile(string profile)
        {
            var profileInfo = _solutionLoadInfo.GetProfile(profile);
            if (profileInfo == null)
                throw new ArgumentException("Profile does not exist:" + profile);

            return profileInfo;
        }

        private SolutionLoadInfo ReadSettings()
        {
            return SolutionLoadInfoSerializer.Deserialize(GetSettingsFilePath());
        }

        private void WriteSettings()
        {
            SolutionLoadInfoSerializer.Serialize(_solutionLoadInfo, GetSettingsFilePath());
        }

        private string GetSettingsFilePath()
        {
            return _solutionFilePath + SettingsFileExtension;
        }

        private static SolutionLoadInfo CreateDefaultSolutionSettings()
        {
            return new SolutionLoadInfo
            {
                ActiveProfileName = ActiveProfilePropertyName,
                Profiles = { new SolutionLoadProfile(ActiveProfilePropertyName) }
            };
        }

        #endregion

        #region Serialization classes

        public class ProjectLoadInfo
        {
            public Guid ProjectGuid { get; set; }
            public LoadPriority LoadPriority { get; set; }

            public ProjectLoadInfo Clone()
            {
                return (ProjectLoadInfo)MemberwiseClone();
            }
        }

        public class SolutionLoadProfile
        {
            public SolutionLoadProfile() : this(null)
            { }

            public SolutionLoadProfile(string profileName, IEnumerable<ProjectLoadInfo> projects = null)
            {
                ProfileName = profileName;
                Projects = new List<ProjectLoadInfo>(projects ?? Enumerable.Empty<ProjectLoadInfo>());
            }

            public string ProfileName { get; set; }

            public List<ProjectLoadInfo> Projects { get; }

            public ProjectLoadInfo GetProject(Guid projectGuid)
            {
                return Projects.FirstOrDefault(p => p.ProjectGuid.Equals(projectGuid));
            }

            public SolutionLoadProfile Clone()
            {
                return new SolutionLoadProfile(ProfileName, Projects.Select(p => p.Clone()));
            }
        }

        public class SolutionLoadInfo
        {
            public SolutionLoadInfo()
            {
                Profiles = new List<SolutionLoadProfile>();
            }

            public string ActiveProfileName { get; set; }
            public List<SolutionLoadProfile> Profiles { get; set; }

            public SolutionLoadProfile GetProfile(string profileName)
            {
                return Profiles.FirstOrDefault(p => p.ProfileName.Equals(profileName));
            }
        }

        static class SolutionLoadInfoSerializer
        {
            public static void Serialize(SolutionLoadInfo solutionLoadInfo, string filePath)
            {
                if (solutionLoadInfo == null)
                    throw new ArgumentNullException(nameof(solutionLoadInfo));

                var serializer = new XmlSerializer(typeof(SolutionLoadInfo));
                using (var fileStream = File.Create(filePath))
                    serializer.Serialize(fileStream, solutionLoadInfo);
            }

            public static SolutionLoadInfo Deserialize(string filePath)
            {
                if (!File.Exists(filePath))
                    return null;

                var serializer = new XmlSerializer(typeof(SolutionLoadInfo));
                using (var fileStream = File.OpenRead(filePath))
                    return (SolutionLoadInfo)serializer.Deserialize(fileStream);
            }
        }

        #endregion
    }
}
