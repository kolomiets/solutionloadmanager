using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhino.Mocks;
using Microsoft.VisualStudio.Settings;
using Kolos.SolutionLoadManager.Settings;
using System.IO;

namespace UnitTests
{
    [TestClass]
    public class VsSettingsManagerTest
    {
        private const String SettingsCollectionName = "Solution Load Manager Settings";
        private const String DefaultProfileName = "Default Profile";
        private const String ActiveProfileProperty = "<Active Profile>";

        private const String SolutionId = "Solution ID";
        private String SolutionSettings = Path.Combine(SettingsCollectionName, SolutionId);

        MockRepository mocks;
        WritableSettingsStore store;

        [TestInitialize]
        public void SetUp()
        {
            mocks = new MockRepository();
            store = mocks.StrictMock<WritableSettingsStore>();
        }
        
        [TestMethod]
        public void CreateDefaultSettingsTest()
        {
            var defaultProfileSettings = Path.Combine(SolutionSettings, DefaultProfileName);

            using (mocks.Record())
            {
                Expect.Call(store.CollectionExists(SolutionSettings)).Return(false); 
                Expect.Call(() => store.CreateCollection(defaultProfileSettings));
                Expect.Call(() => store.SetString(SolutionSettings, ActiveProfileProperty, DefaultProfileName));
            }

            using (mocks.Playback())
            {
                var settings = new VsSettingsManager(SolutionId, store);
            }
        }

        [TestMethod]
        public void GetActiveProfileTest()
        {
            using (mocks.Record())
            {
                // we already have default settings
                Expect.Call(store.CollectionExists(SolutionSettings)).Return(true);
                Expect.Call(store.GetString(SolutionSettings, ActiveProfileProperty)).Return(DefaultProfileName);
            }

            using (mocks.Playback())
            {
                var settings = new VsSettingsManager(SolutionId, store);
                Assert.AreEqual(DefaultProfileName, settings.ActiveProfile);
            }
        }

        [TestMethod]
        public void SetActiveProfileTest()
        {
            var newActiveProfileName = "Some profile";

            using (mocks.Record())
            {
                // we already have default settings
                Expect.Call(store.CollectionExists(SolutionSettings)).Return(true);
                Expect.Call(() => store.SetString(SolutionSettings, ActiveProfileProperty, newActiveProfileName));
            }

            using (mocks.Playback())
            {
                var settings = new VsSettingsManager(SolutionId, store);
                settings.ActiveProfile = newActiveProfileName;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProfileValidationTest()
        {
            using (mocks.Record())
            {
                // we already have default settings
                Expect.Call(store.CollectionExists(SolutionSettings)).Return(true);
            }

            using (mocks.Playback())
            {
                var settings = new VsSettingsManager(SolutionId, store);
                settings.AddProfile(String.Empty, String.Empty);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProfileThatAlreadyExists()
        {
            var newProfileName = "Some profile";
            var newProfileSettings = Path.Combine(SolutionSettings, newProfileName);

            using (mocks.Record())
            {
                // we already have default settings
                Expect.Call(store.CollectionExists(SolutionSettings)).Return(true);
                // validation
                Expect.Call(store.CollectionExists(newProfileSettings)).Return(true);
            }

            using (mocks.Playback())
            {
                var settings = new VsSettingsManager(SolutionId, store);
                settings.AddProfile(newProfileName, String.Empty);
            }
        }

        [TestMethod]
        public void AddEmptyProfileTest()
        {
            var newProfileName = "Some profile";
            var newProfileSettings = Path.Combine(SolutionSettings, newProfileName);

            using (mocks.Record())
            {
                // we already have default settings
                Expect.Call(store.CollectionExists(SolutionSettings)).Return(true);
                // validation
                Expect.Call(store.CollectionExists(newProfileSettings)).Return(false);
                // actual add
                Expect.Call(() => store.CreateCollection(newProfileSettings));
            }

            using (mocks.Playback())
            {
                var settings = new VsSettingsManager(SolutionId, store);
                settings.AddProfile(newProfileName, String.Empty);
            }
        }

        [TestMethod]
        public void RemoveProfileTest()
        {
            var defaultProfileSettings = Path.Combine(SolutionSettings, DefaultProfileName);

            using (mocks.Record())
            {
                // we already have default settings
                Expect.Call(store.CollectionExists(SolutionSettings)).Return(true);
                // validation
                Expect.Call(store.CollectionExists(defaultProfileSettings)).Return(true);
                // actual delete
                Expect.Call(store.DeleteCollection(defaultProfileSettings)).Return(true);
            }

            using (mocks.Playback())
            {
                var settings = new VsSettingsManager(SolutionId, store);
                settings.RemoveProfile(DefaultProfileName);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RemoveNonExistingProfile()
        {
            var fakeProfile = "Some fake profile";
            var fakeProfileSettings = Path.Combine(SolutionSettings, fakeProfile);

            using (mocks.Record())
            {
                // we already have default settings
                Expect.Call(store.CollectionExists(SolutionSettings)).Return(true);
                // validation
                Expect.Call(store.CollectionExists(fakeProfileSettings)).Return(false);
            }

            using (mocks.Playback())
            {
                var settings = new VsSettingsManager(SolutionId, store);
                settings.RemoveProfile(fakeProfile);
            }
        }
    }
}
