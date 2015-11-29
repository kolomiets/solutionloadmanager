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
    };
}