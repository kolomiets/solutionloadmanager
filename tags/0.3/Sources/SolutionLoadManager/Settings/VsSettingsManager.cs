using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using System.Diagnostics.Contracts;
using System.IO;

namespace Kolos.SolutionLoadManager.Settings
{
    /// <summary>
    /// Implementation of ISettingsManager interface that uses VS settings 
    /// mechanism to store load priority settings.
    /// </summary>
    public class VsSettingsManager: ISettingsManager
    {
        private String m_SolutionId;
        private WritableSettingsStore m_Settings;

        private const String SettingsCollectionName = "Solution Load Manager Settings";
        private const String DefaultProfileCollectionName = "Default Profile";

        private const String ActiveProfilePropertyName = "<Active Profile>";

        /// <summary>
        /// Creates instance of settings manager
        /// </summary>
        /// <param name="solutionId">Unique solution identifier</param>
        /// <param name="settingsStore">Settings store to write settings to</param>
        public VsSettingsManager(String solutionId, WritableSettingsStore settingsStore)
        {
            m_SolutionId = solutionId;
            m_Settings = settingsStore;

            CreateDefaultSolutionSettings();
        }

        #region ISettingsManager Members

        /// <summary>
        /// Gets or sets name of the active profile
        /// </summary>
        public String ActiveProfile
        {
            get { return m_Settings.GetString(SolutionCollection, ActiveProfilePropertyName); }

            set { m_Settings.SetString(SolutionCollection, ActiveProfilePropertyName, value); }
        }

        /// <summary>
        /// Returns all available profiles
        /// </summary>
        public IEnumerable<String> Profiles
        {
            get { return m_Settings.GetSubCollectionNames(SolutionCollection); }
        }

        /// <summary>
        /// Adds new profile. Existing profile name can be specified to copy settings from.
        /// </summary>
        /// <param name="profile">Name of the new profile</param>
        /// <param name="copySettingsFrom">Existing profile name or empty/null string</param>
        public void AddProfile(String profile, String copySettingsFrom)
        {
            ValidateNewProfile(profile);
            if (!String.IsNullOrEmpty(copySettingsFrom))
                ValidateExistingProfile(copySettingsFrom);

            if (String.IsNullOrEmpty(copySettingsFrom))
                m_Settings.CreateCollection(GetProfileCollection(profile));
            else
                CopyProfile(copySettingsFrom, profile);
        }

        /// <summary>
        /// Removes existing profile
        /// </summary>
        /// <param name="profile">Existing profile name</param>
        public void RemoveProfile(String profile)
        {
            ValidateExistingProfile(profile);

            m_Settings.DeleteCollection(GetProfileCollection(profile));
        }

        /// <summary>
        /// Renames existing profile to the new name
        /// </summary>
        /// <param name="profile">Existing profile name</param>
        /// <param name="newProfile">New profile name</param>
        public void RenameProfile(String profile, String newProfile)
        {
            ValidateExistingProfile(profile);
            ValidateNewProfile(newProfile);

            CopyProfile(profile, newProfile);
            RemoveProfile(profile);
        }

        /// <summary>
        /// Gets project load priority for specified profile
        /// </summary>
        /// <param name="profile">Existing profile name</param>
        /// <param name="projectGuid">Project ID</param>
        /// <returns>Load priority of the project</returns>
        public LoadPriority GetProjectLoadPriority(String profile, Guid projectGuid)
        {
            ValidateExistingProfile(profile);

            UInt32 loadState = m_Settings.GetUInt32(GetProfileCollection(profile), projectGuid.ToString(), 0);
            return (LoadPriority)loadState;
        }

        /// <summary>
        /// Sets project load priority for specified profile
        /// </summary>
        /// <param name="profile">Existing profile name</param>
        /// <param name="projectGuid">Project ID</param>
        /// <param name="priority">Load priority</param>
        public void SetProjectLoadPriority(String profile, Guid projectGuid, LoadPriority priority)
        {
            ValidateExistingProfile(profile);

            m_Settings.SetUInt32(GetProfileCollection(profile), projectGuid.ToString(), (uint)priority);
        }

        #endregion

        private String SolutionCollection
        {
            get { return Path.Combine(SettingsCollectionName, m_SolutionId); }
        }

        private String GetProfileCollection(String profile)
        {
            return Path.Combine(SolutionCollection, profile);
        }

        private void CopyProfile(String source, String destination)
        {
            String sourceProperties = GetProfileCollection(source);
            String targetProperties = GetProfileCollection(destination);

            // Create settings collection if it does not exist
            m_Settings.CreateCollection(targetProperties);

            foreach (var propertyName in m_Settings.GetPropertyNames(sourceProperties))
            {
                UInt32 value = m_Settings.GetUInt32(sourceProperties, propertyName);
                m_Settings.SetUInt32(targetProperties, propertyName, value);
            }
        }

        private void CreateDefaultSolutionSettings()
        {
            // check for settings collection
            if (!m_Settings.CollectionExists(SolutionCollection))
            {
                m_Settings.CreateCollection(GetProfileCollection(DefaultProfileCollectionName));
                ActiveProfile = DefaultProfileCollectionName;
            }
        }

        private void ValidateExistingProfile(String profile)
        {
            if (String.IsNullOrEmpty(profile) || !m_Settings.CollectionExists(GetProfileCollection(profile)))
                throw new ArgumentException("Profile does not exist");
        }

        private void ValidateNewProfile(String profile)
        {
            if (String.IsNullOrEmpty(profile))
                throw new ArgumentException("Invalid profile name");

            if (m_Settings.CollectionExists(GetProfileCollection(profile)))
                throw new ArgumentException("Profile already exist");
        }
    }
}
