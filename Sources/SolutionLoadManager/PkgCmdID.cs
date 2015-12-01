// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;

namespace Kolos.SolutionLoadManager
{
    static class PkgCmdIDList
    {
        // Tools Menu Commands
        public const uint cmdidSolutionLoadManager = 0x100;

        // Context Menu Commands
        public const uint cmdidSolutionLoadManagerContext = 0x101;
        public const uint cmdidReloadSolutionContext = 0x102;

        // Toolbar Commands
        public const int cmdidActiveProfileCombo = 0x110;
        public const int cmdidActiveProfileComboGetList = 0x111;

        public const int cmdidSolutionLoadManagerToolbar = 0x0112;
        public const int cmdidReloadSolutionToolbar = 0x0113;
    };
}