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

namespace VSExtension
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class Commands
    {
        /// <summary>
        /// Commands IDs.
        /// </summary>
        public const int CommandId_ShowTAC = 0x0101;
        public const int CommandId_ShowSSA = 0x0102;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("72b3b0c6-ab01-4963-9084-07553be34931");

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static Commands Instance { get; private set; }

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        /// <summary>
        /// Initializes a new instance of the <see cref="Commands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private Commands(Package package)
        {
            this.package = package ?? throw new ArgumentNullException("package");

            var commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId_ShowTAC);
                var menuItem = new MenuCommand(OnShowTAC, menuCommandID);

                commandService.AddCommand(menuItem);

                menuCommandID = new CommandID(CommandSet, CommandId_ShowSSA);
                menuItem = new MenuCommand(OnShowSSA, menuCommandID);

                commandService.AddCommand(menuItem);
            }
        }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider
        {
            get { return package; }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new Commands(package);
        }

        private void OnShowTAC(object sender, EventArgs e)
        {
            var message = string.Format("Inside {0}.OnShowTAC()", this.GetType().FullName);
            var title = "Show Three Address Code";

            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private void OnShowSSA(object sender, EventArgs e)
        {
            var message = string.Format("Inside {0}.OnShowSSA()", this.GetType().FullName);
            var title = "Show Static Single Assignment";

            VsShellUtilities.ShowMessageBox(
                this.ServiceProvider,
                message,
                title,
                OLEMSGICON.OLEMSGICON_INFO,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
