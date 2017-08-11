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
                var menuItem = new MenuCommand((s, e) => OnCommand(ShowIL), menuCommandId);
                commandService.AddCommand(menuItem);

                menuCommandId = new CommandID(CommandSet, CommandId_ShowTAC);
                menuItem = new MenuCommand((s, e) => OnCommand(ShowTAC), menuCommandId);
                commandService.AddCommand(menuItem);

                menuCommandId = new CommandID(CommandSet, CommandId_ShowWebs);
                menuItem = new MenuCommand((s, e) => OnCommand(ShowWebs), menuCommandId);
                commandService.AddCommand(menuItem);

				menuCommandId = new CommandID(CommandSet, CommandId_ShowSSA);
				menuItem = new MenuCommand((s, e) => OnCommand(ShowSSA), menuCommandId);
				commandService.AddCommand(menuItem);

				menuCommandId = new CommandID(CommandSet, CommandId_ShowCFG);
				menuItem = new MenuCommand((s, e) => OnCommand(ShowCFG), menuCommandId);
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
            if (_buildInProgress)
            {
                _projectBuildResult[project] = success;
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
            var monitor = Package.GetGlobalService(typeof(SVsShellMonitorSelection)) as IVsMonitorSelection;
            monitor.GetCurrentSelection(out IntPtr hierarchyPtr, out uint projectItemId, out IVsMultiItemSelect multiItemPtr, out IntPtr containerPtr);

            var container = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(containerPtr) as ISelectionContainer;
            container.CountObjects((uint)Microsoft.VisualStudio.Shell.Interop.Constants.GETOBJS_SELECTED, out uint count);

            var selectedItems = new object[count];
            container.GetObjects((uint)Microsoft.VisualStudio.Shell.Interop.Constants.GETOBJS_SELECTED, count, selectedItems);

            return selectedItems;
        }

		private string GetOutputFileName(Project project)
		{
			var fullPath = project.Properties.Item("FullPath").Value as string;
			var outputFileName = project.Properties.Item("OutputFileName").Value as string;
			var outputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value as string;

			var result = Path.Combine(fullPath, outputPath, outputFileName);
			return result;
		}

		private void ShowFile(Func<AnalysisHelper, Model.Types.IMethodReference, string> generate, string kind, string extension)
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
						var project = function.ProjectItem.ContainingProject;
						var ok = _projectBuildResult.TryGetValue(project.UniqueName, out bool succeeded);

						if (ok && succeeded)
						{
							var assemblyFileName = GetOutputFileName(project);
							helper.LoadAssembly(assemblyFileName);

							var containingType = Utils.GetTypeReference(function.Parent);
							var signature = Utils.GetSignature(function);
							var reference = helper.FindMethod(containingType, signature);
							var text = generate(helper, reference);

							var fileName = string.Format("{0} - {1}.{2}", kind, function.FullName, extension);
							fileName = Path.Combine(Path.GetTempPath(), fileName);

							File.WriteAllText(fileName, text);
							dte.ItemOperations.OpenFile(fileName);
						}
					}
				}
			}
		}

		private void ShowIL() => ShowFile((helper, reference) => helper.GenerateIL(reference), "IL", "txt");

		private void ShowTAC() => ShowFile((helper, reference) => helper.GenerateTAC(reference), "TAC", "txt");

		private void ShowCFG() => ShowFile((helper, reference) => helper.GenerateCFG(reference), "CFG", "dgml");

		private void ShowWebs() => ShowFile((helper, reference) => helper.GenerateWebs(reference), "Webs", "txt");

		private void ShowSSA() => ShowFile((helper, reference) => helper.GenerateSSA(reference), "SSA", "txt");

    }
}
