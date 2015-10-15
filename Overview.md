Solution Load Manager is a Visual Studio 2010 extension that provides access to project load priority settings. The extension is also available through [Visual Studio Gallery](http://visualstudiogallery.msdn.microsoft.com/en-us/66350dbe-ed01-4120-bea2-5564eff7b0b2)

# Version Log #

## Version 0.4 ##
Released on 29 September 2012

Profile settings are now stored alongside with VS solution file (with .slm file extension). It was one of the most requested features - now you can place load priority settings to the source control to share them with other developers.

## Version 0.3 ##
Released on 26 December 2011

Profiles support has been added to keep several load priority settings for different scenarios (For example, you may have "Full Solution" profile that always loads all projects, "Unit Tests" profile that loads only unit tests projects, etc.)

# Introduction #

Before Visual Studio 2010 release you had to spent a lot of time waiting while Visual Studio opens our big/huge/enormous solutions. It becomes a real development issue for large products. Often it is not possible to split solution to small pieces because of project dependencies, unit tests and convenience of development within one workspace.

But usually you don't use _all_ these projects at once. You change one bit here, one line there and you do not want to wait until all these utility projects will be successfully loaded. Things get worse with native (C++) projects where a lot of processor power is spent for parsing header files. First of all, we just want to write some code!

Visual Studio 2010 provides new API to solve exactly this problem. From now on it is possible to choose whether specified project should be loaded during solution "start up" or not. Moreover, there are four options (see [MSDN documentation](http://msdn.microsoft.com/en-us/library/ee462384(VS.100).aspx)):
  * **Demand Load** (the default): when a solution is opened, projects are loaded asynchronously. If this priority is set on an unloaded project after the solution is already open, the project will be loaded at the next idle point.
  * **Background Load**: when a solution is opened, projects are loaded in the background, allowing the user to access the projects as they are loaded without having to wait until all the projects are loaded.
  * **Load if Needed**:  projects are loaded when they are accessed. A project is accessed when the user expands the project node in the Solution Explorer, when a file belonging to the project is opened when the solution opens because it is in the open document list (persisted in the solution's user options file), or when another project that is being loaded has a dependency on the project.
  * **Explicit Load Only**: projects are not to be loaded unless the user explicitly requests it. This is the case when projects are explicitly unloaded.

Unfortunately this functionality available only as API - Visual Studio 2010 has no built-in support for load priority settings.

# Solution Load Manager extension #
After extension installation user has several ways of accessing load priority settings. First of all there is a menu item in Tools menu. You can also use right-click on solution item in Solution Explorer and select "Solution Load Manager" in context menu.

Main window of the extension provides birds-eye view of the solution hierarchy including solution folders:
http://solutionloadmanager.googlecode.com/svn/trunk/Sources/SolutionLoadManager/Preview.PNG

To change load priority settings for a project (or a group of projects) you should select it in the tree and choose desired load priority. You could simply recognize current load priority level by background color of the project item.

Additionally, you can define several "profiles" of settings. For example, you may have "Full Solution" profile that always loads all projects, "Unit Tests" profile that loads only unit tests projects and so on. You can have arbitrary number of profiles and easily switch between them.

Reload Solution button allows reload solution immediately to apply all changed priority levels.