using System;
using System.Collections.Generic;

namespace Kolos.SolutionLoadManager.Settings
{
    /// <summary>
    /// Abstraction over actual storage of VS load profile settings.
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// Gets or sets active profile name.
        /// </summary>
        String ActiveProfile { get; set; }

        /// <summary>
        /// Gets the list of all available profile names.
        /// </summary>
        IEnumerable<String> Profiles { get; }

        /// <summary>
        /// Adds new profile. Existing profile name can be specified to copy settings from.
        /// </summary>
        /// <param name="profile">Name of the new profile.</param>
        /// <param name="copySettingsFrom">Existing profile name or empty/null string.</param>
        void AddProfile(String profile, String copySettingsFrom);

        /// <summary>
        /// Removes existing profile.
        /// </summary>
        /// <param name="profile">Existing profile name.</param>
        void RemoveProfile(String profile);

        /// <summary>
        /// Renames existing profile to the new name.
        /// </summary>
        /// <param name="profile">Existing profile name.</param>
        /// <param name="newProfile">New profile name.</param>
        void RenameProfile(String profile, String newProfile);

        /// <summary>
        /// Sets project load priority for specified profile.
        /// </summary>
        /// <param name="profile">Existing profile name.</param>
        /// <param name="projectGuid">Project ID.</param>
        /// <param name="priority">Load priority of the project.</param>
        /// <returns>Load priority of the project.</returns>
        void SetProjectLoadPriority(String profile, Guid projectGuid, LoadPriority priority);

        /// <summary>
        /// Gets project load priority for specified profile.
        /// </summary>
        /// <param name="profile">Existing profile name.</param>
        /// <param name="projectGuid">Project ID.</param>
        /// <returns>Load priority of the project.</returns>
        LoadPriority GetProjectLoadPriority(String profile, Guid projectGuid);
    }
}
