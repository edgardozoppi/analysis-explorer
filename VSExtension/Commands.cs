//------------------------------------------------------------------------------
// <copyright file="Command1.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using EnvDTE;
using System.IO;

namespace VSExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Commands
    {
        public const int CommandId_ShowIL = 0x0101;
        public const int CommandId_ShowTAC = 0x0102;
        public const int CommandId_ShowWebs = 0x0103;
        public const int CommandId_ShowSSA = 0x0104;
        public const int CommandId_ShowCFG = 0x0105;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("72b3b0c6-ab01-4963-9084-07553be34931");

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Commands Instance { get; private set; }

        private readonly AnalysisPackage _package;
        private readonly IDictionary<string, bool> _projectBuildResult;
        private bool _buildInProgress;
        private Action _action;

        /// <summary>
        /// Initializes a new instance of the <see cref="Commands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private Commands(AnalysisPackage package)
        {
            _package = package ?? throw new ArgumentNullException("package");
            _projectBuildResult = new Dictionary<string, bool>();

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            dte.Events.BuildEvents.OnBuildDone += OnBuildDone;
            dte.Events.BuildEvents.OnBuildProjConfigDone += OnBuildProjConfigDone;

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService != null)
            {
                var menuCommandId = new CommandID(CommandSet, CommandId_ShowIL);
                var menuItem = new OleMenuCommand((s, e) => OnCommand(ShowIL), menuCommandId);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);

                menuCommandId = new CommandID(CommandSet, CommandId_ShowTAC);
                menuItem = new OleMenuCommand((s, e) => OnCommand(ShowTAC), menuCommandId);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);

                menuCommandId = new CommandID(CommandSet, CommandId_ShowWebs);
                menuItem = new OleMenuCommand((s, e) => OnCommand(ShowWebs), menuCommandId);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);

                menuCommandId = new CommandID(CommandSet, CommandId_ShowSSA);
                menuItem = new OleMenuCommand((s, e) => OnCommand(ShowSSA), menuCommandId);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);

                menuCommandId = new CommandID(CommandSet, CommandId_ShowCFG);
                menuItem = new OleMenuCommand((s, e) => OnCommand(ShowCFG), menuCommandId);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get { return _package; }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(AnalysisPackage package)
        {
            Instance = new Commands(package);
        }

        private void OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            if (_buildInProgress)
            {
                _action();
            }
        }

        private void OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            var info = GetActiveConfigurationInfo(project, projectConfig, platform);
            _projectBuildResult[info] = success;
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand command)
            {
                var visible = false;
                var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                var selectionContainer = dte.SelectedItems.SelectionContainer;

                if (selectionContainer != null &&
                    selectionContainer.Count > 0)
                {
                    visible = true;
                }

                command.Visible = visible;
            }
        }

        private void OnCommand(Action action)
        {
            _action = () =>
            {
                _buildInProgress = false;
                _action = null;
                action();
                _projectBuildResult.Clear();
            };

            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;

            _buildInProgress = true;
            dte.ExecuteCommand("Build.BuildSelection");
        }

        private object[] GetSelectedItems()
        {
            object[] result = null;
            var monitor = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            monitor.GetCurrentSelection(out IntPtr hierarchyPtr, out uint projectItemId, out IVsMultiItemSelect multiItemPtr, out IntPtr containerPtr);

            var container = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(containerPtr) as ISelectionContainer;
            container.CountObjects((uint)Microsoft.VisualStudio.Shell.Interop.Constants.GETOBJS_SELECTED, out uint count);

            if (count > 0)
            {
                result = new object[count];
                container.GetObjects((uint)Microsoft.VisualStudio.Shell.Interop.Constants.GETOBJS_SELECTED, count, result);
            }

            return result;
        }

        private string GetActiveConfigurationInfo(string projectName, string configurationName, string platformName)
        {
            return $"{projectName}@{configurationName}|{platformName}";
        }

        private string GetActiveConfigurationInfo(Project project)
        {
            var projectName = project.UniqueName;
            var configurationName = project.ConfigurationManager.ActiveConfiguration.ConfigurationName;
            var platformName = project.ConfigurationManager.ActiveConfiguration.PlatformName;

            return GetActiveConfigurationInfo(projectName, configurationName, platformName);
        }

        private string GetOutputFileName(Project project)
        {
            var fullPath = project.Properties.Item("FullPath").Value as string;
            var outputFileName = project.Properties.Item("OutputFileName").Value as string;
            var outputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value as string;

            var result = Path.Combine(fullPath, outputPath, outputFileName);
            return result;
        }

        private void ShowFile(Func<AnalysisHelper, Model.Types.MethodDefinition, string> generate, string kind, string extension)
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            var selectedItems = GetSelectedItems();

            using (var helper = new AnalysisHelper())
            {
                foreach (var item in selectedItems)
                {
                    if (item is ISelectedGraphNode node)
                    {
                        //var id = node.Node.GetValue("Id");
                    }
                    else if (item is CodeFunction function)
                    {
                        ShowFile(helper, function, generate, kind, extension);
                    }
                    else if (item is CodeProperty property)
                    {
                        ShowFile(helper, property.Getter, generate, kind, extension);
                        ShowFile(helper, property.Setter, generate, kind, extension);
                    }
                }
            }
        }

        private void ShowFile(AnalysisHelper helper, CodeFunction function, Func<AnalysisHelper, Model.Types.MethodDefinition, string> generate, string kind, string extension)
        {
            var project = function.ProjectItem.ContainingProject;
            var info = GetActiveConfigurationInfo(project);
            var ok = _projectBuildResult.TryGetValue(info, out bool succeeded);

            if (ok && succeeded)
            {
                var assemblyFileName = GetOutputFileName(project);
                helper.LoadAssembly(assemblyFileName);

                var type = Utils.GetParent(function);
                var containingType = Utils.GetTypeReference(type);
                var signature = Utils.GetSignature(function);
                var reference = helper.FindMethod(containingType, signature);
                var method = helper.Resolve(reference);

                if (method.HasBody)
                {
                    var text = generate(helper, method);

                    var fileName = method.ToFullDisplayName();
                    fileName = $"{kind} - {fileName}.{extension}";
                    fileName = Utils.GetSafeFileName(fileName);
                    fileName = Path.Combine(Path.GetTempPath(), fileName);

                    File.WriteAllText(fileName, text);

                    var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
                    dte.ItemOperations.OpenFile(fileName);
                }
                else
                {
                    VsShellUtilities.ShowMessageBox
                    (
                        this.ServiceProvider,
                        "The selected member doesn't have a body implementation",
                        AnalysisPackage.Name,
                        OLEMSGICON.OLEMSGICON_INFO,
                        OLEMSGBUTTON.OLEMSGBUTTON_OK,
                        OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST
                    );
                }
            }
        }

        private void ShowIL() => ShowFile((helper, method) => helper.GenerateIL(method), "IL", "txt");

        private void ShowTAC() => ShowFile((helper, method) => helper.GenerateTAC(method), "TAC", "txt");

        private void ShowCFG() => ShowFile((helper, method) => helper.GenerateCFG(method), "CFG", "dgml");

        private void ShowWebs() => ShowFile((helper, method) => helper.GenerateWebs(method), "Webs", "txt");

        private void ShowSSA() => ShowFile((helper, method) => helper.GenerateSSA(method), "SSA", "txt");
    }
}
