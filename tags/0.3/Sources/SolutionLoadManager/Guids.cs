// Guids.cs
// MUST match guids.h
using System;

namespace Kolos.SolutionLoadManager
{
    static class GuidList
    {
        public const string guidSolutionLoadManagerPkgString = "2c8b59e2-48fb-4629-896f-4a550925e24c";
        public const string guidSolutionLoadManagerCmdSetString = "fc8076ea-63e2-4fcf-a1d7-6aab2c322ad1";

        public static readonly Guid guidSolutionLoadManagerCmdSet = new Guid(guidSolutionLoadManagerCmdSetString);
    };
}