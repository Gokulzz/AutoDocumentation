using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using Task = System.Threading.Tasks.Task;
using System.ComponentModel.Design;

namespace AutoDocumentation
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class MyCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid("a360351f-cbd8-4824-b5de-284bd79eb9db");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly AsyncPackage package;

        /// <summary>
        /// Initializes a new instance of the <see cref="MyCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private MyCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static MyCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => this.package;

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new MyCommand(package, commandService);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private async void Execute(object sender, EventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // Get selected code from the editor
            string selectedCode = await GetSelectedCodeFromEditorAsync();

            if (string.IsNullOrEmpty(selectedCode))
            {
                // Show a message box if no code is selected
                VsShellUtilities.ShowMessageBox(
                    this.package,
                    "No code selected. Please select some code before generating documentation.",
                    "No Selection",
                    OLEMSGICON.OLEMSGICON_WARNING,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                return;
            }
            
            // Generate documentation using OpenAI API
            var (summary, lineByLineComments) = await OpenAIHelper.GenerateDocumentationAsync(selectedCode);

            // Combine the summary and line-by-line comments
            string combinedDocumentation = $"// Summary:\n// {summary}\n\n// Line-by-Line Comments:\n{lineByLineComments}";

            // Insert the generated documentation into the editor
            await InsertDocumentationIntoEditorAsync(combinedDocumentation);
        }

        private async Task<string> GetSelectedCodeFromEditorAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = await ServiceProvider.GetServiceAsync(typeof(SDTE)) as EnvDTE80.DTE2;
            var activeDocument = dte?.ActiveDocument;
            var textSelection = activeDocument?.Selection as EnvDTE.TextSelection;

            return textSelection?.Text;
        }

        private async Task InsertDocumentationIntoEditorAsync(string documentation)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = await ServiceProvider.GetServiceAsync(typeof(SDTE)) as EnvDTE80.DTE2;
            var activeDocument = dte?.ActiveDocument;
            var textSelection = activeDocument?.Selection as EnvDTE.TextSelection;

            if (textSelection != null)
            {
                textSelection.Insert(documentation + Environment.NewLine, (int)EnvDTE.vsInsertFlags.vsInsertFlagsContainNewText);
            }
        }
    }
}
