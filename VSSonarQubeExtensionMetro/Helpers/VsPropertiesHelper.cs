// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VsPropertiesHelper.cs" company="">
//   
// </copyright>
// <summary>
//   The vs properties helper.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace VSSonarQubeExtension.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    using EnvDTE;

    using EnvDTE80;

    using ExtensionTypes;

    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Editor;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;

    using VSSonarPlugins;

    using Process = System.Diagnostics.Process;
    using Thread = System.Threading.Thread;

    /// <summary>
    ///     The vs properties helper.
    /// </summary>
    public class VsPropertiesHelper : IVsEnvironmentHelper
    {
        #region Fields

        /// <summary>
        ///     The environment 2.
        /// </summary>
        private readonly DTE2 environment;

        /// <summary>
        ///     The provider.
        /// </summary>
        private readonly IServiceProvider provider;

        /// <summary>
        /// The text view host.
        /// </summary>
        private IWpfTextViewHost textViewHost;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="VsPropertiesHelper"/> class.
        /// </summary>
        /// <param name="environment">
        /// The environment 2.
        /// </param>
        /// <param name="service">
        /// The service.
        /// </param>
        public VsPropertiesHelper(DTE2 environment, IServiceProvider service)
        {
            this.TempDataFolder = Path.GetTempPath();
            this.provider = service;
            this.environment = environment;
            this.ApplicationDataUserSettingsFile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)
                                                   + "\\VSSonarExtension\\settings.cfg";
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="VsPropertiesHelper" /> class.
        /// </summary>
        public VsPropertiesHelper()
        {
            this.ApplicationDataUserSettingsFile = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData)
                                                   + "\\VSSonarExtension\\settings.cfg";
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets or sets the application data user settings file.
        /// </summary>
        public string ApplicationDataUserSettingsFile { get; set; }

        /// <summary>
        ///     Gets or sets the custom pane.
        /// </summary>
        public IVsOutputWindowPane CustomPane { get; set; }

        /// <summary>
        ///     Gets or sets the erro message.
        /// </summary>
        public string ErroMessage { get; set; }

        /// <summary>
        ///     Gets or sets the temp data folder.
        /// </summary>
        public string TempDataFolder { get; set; }

        /// <summary>
        /// Gets the text view host.
        /// </summary>
        public IWpfTextViewHost TextViewHost
        {
            get
            {
                if (this.textViewHost == null)
                {
                    var textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
                    IVsTextView textView;
                    textManager.GetActiveView(1, null, out textView);

                    var data = textView as IVsUserData;
                    if (data != null)
                    {
                        Guid guid = DefGuidList.guidIWpfTextViewHost;
                        object obj;
                        int hr = data.GetData(ref guid, out obj);
                        if ((hr == VSConstants.S_OK) && obj != null && obj is IWpfTextViewHost)
                        {
                            this.textViewHost = obj as IWpfTextViewHost;
                        }
                    }
                }

                return this.textViewHost;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get document language.
        /// </summary>
        /// <param name="doc">
        /// The doc.
        /// </param>
        /// <returns>
        /// The System.String.
        /// </returns>
        public static string GetDocumentLanguage(Document doc)
        {
            var textDoc = doc.Object("TextDocument") as TextDocument;

            if (textDoc == null)
            {
                return string.Empty;
            }

            return textDoc.Language.ToLower();
        }

        /// <summary>
        /// The solution path.
        /// </summary>
        /// <param name="applicationObject">
        /// The application object.
        /// </param>
        /// <returns>
        /// The System.String.
        /// </returns>
        public static string SolutionPath(DTE2 applicationObject)
        {
            return applicationObject != null ? Path.GetDirectoryName(applicationObject.Solution.FullName) : string.Empty;
        }

        /// <summary>
        ///     The get active file full path 1.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string ActiveFileFullPath()
        {
            try
            {
                Document doc = this.environment.ActiveDocument;
                return doc != null ? this.GetProperFilePathCapitalization(doc.FullName) : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        ///     The get active project from solution full name.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string ActiveProjectFileFullPath()
        {
            if (this.environment == null)
            {
                return string.Empty;
            }

            var projects = (Array)this.environment.ActiveSolutionProjects;
            return this.GetProperFilePathCapitalization(((Project)projects.GetValue(0)).FullName);
        }

        /// <summary>
        ///     The get active project from solution name.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string ActiveProjectName()
        {
            if (this.environment == null)
            {
                return string.Empty;
            }

            var projects = (Array)this.environment.ActiveSolutionProjects;
            return ((Project)projects.GetValue(0)).Name;
        }

        /// <summary>
        ///     The get solution path.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string ActiveSolutionName()
        {
            if (this.environment == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(this.environment.Solution.FullName))
            {
                return string.Empty;
            }

            return (this.environment.Solution != null) ? Path.GetFileNameWithoutExtension(this.environment.Solution.FullName) : string.Empty;
        }

        /// <summary>
        ///     The get solution path.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string ActiveSolutionPath()
        {
            if (this.environment == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(this.environment.Solution.FullName))
            {
                return string.Empty;
            }

            return Path.GetDirectoryName(this.GetProperFilePathCapitalization(this.environment.Solution.FullName));
        }

        /// <summary>
        ///     The are we running in visual studio.
        /// </summary>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        public bool AreWeRunningInVisualStudio()
        {
            return this.environment != null;
        }

        /// <summary>
        ///     The get document language.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string CurrentSelectedDocumentLanguage()
        {
            if (this.environment == null)
            {
                return string.Empty;
            }

            Document doc = this.environment.ActiveDocument;

            if (doc == null)
            {
                return string.Empty;
            }

            var textDoc = doc.Object("TextDocument") as TextDocument;
            return textDoc == null ? string.Empty : textDoc.Language.ToLower();
        }

        /// <summary>
        /// The delete options for plugin.
        /// </summary>
        /// <param name="pluginKey">
        /// The plugin key.
        /// </param>
        /// <param name="project">
        /// The project.
        /// </param>
        public void DeleteOptionsForPlugin(string pluginKey, Resource project)
        {
            if (project == null || string.IsNullOrEmpty(pluginKey))
            {
                return;
            }

            IEnumerable<string> content = new List<string>();
            string contentWrite = string.Empty;
            if (File.Exists(this.ApplicationDataUserSettingsFile))
            {
                content = File.ReadLines(this.ApplicationDataUserSettingsFile);
            }
            else
            {
                Directory.CreateDirectory(Directory.GetParent(this.ApplicationDataUserSettingsFile).ToString());
                using (File.Create(this.ApplicationDataUserSettingsFile))
                {
                }
            }

            contentWrite = content.Where(line => !line.StartsWith(pluginKey + "=" + project.Key + "."))
                .Aggregate(contentWrite, (current, line) => current + (line + "\r\n"));

            using (var writer = new StreamWriter(this.ApplicationDataUserSettingsFile))
            {
                writer.Write(contentWrite);
            }
        }

        /// <summary>
        ///     The get environment.
        /// </summary>
        /// <returns>
        ///     The <see cref="DTE2" />.
        /// </returns>
        public DTE2 Environment()
        {
            return this.environment;
        }

        /// <summary>
        ///     The get current text in view.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string GetCurrentTextInView()
        {
            IWpfTextView view = this.GetCurrentView();

            return view == null ? string.Empty : view.TextBuffer.CurrentSnapshot.GetText();
        }

        /// <summary>
        ///     The get current view.
        /// </summary>
        /// <returns>
        ///     The <see cref="IWpfTextView" />.
        /// </returns>
        public IWpfTextView GetCurrentView()
        {
            IWpfTextViewHost viewHost = this.TextViewHost;
            if (viewHost != null)
            {
                return this.TextViewHost.TextView;
            }

            return null;
        }

        /// <summary>
        /// The get file real path for solution.
        /// </summary>
        /// <param name="fileInView">
        /// The file in view.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string GetFileRealPathForSolution(string fileInView)
        {
            string solutionPath = this.ActiveSolutionPath();
            string driveLetter = solutionPath.Substring(0, 1);
            return driveLetter + fileInView.Substring(1);
        }

        /// <summary>
        /// The get proper file path capitalization.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string GetProperFilePathCapitalization(string filename)
        {
            var fileInfo = new FileInfo(filename);
            DirectoryInfo dirInfo = fileInfo.Directory;
            return dirInfo != null ? Path.Combine(GetProperDirectoryCapitalization(dirInfo), dirInfo.GetFiles(fileInfo.Name)[0].Name) : string.Empty;
        }

        /// <summary>
        /// The navigate to resource.
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        public void NavigateToResource(string url)
        {
            if (this.environment == null)
            {
                Process.Start(url);
                return;
            }

            (new Thread(() => this.environment.ItemOperations.Navigate(url, vsNavigateOptions.vsNavigateOptionsNewWindow))).Start();
        }

        /// <summary>
        /// The open resource in visual studio.
        /// </summary>
        /// <param name="workfolder">
        /// The workfolder.
        /// </param>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <param name="line">
        /// The line.
        /// </param>
        /// <param name="editorCommandExec">
        /// The editor Command Exec.
        /// </param>
        public void OpenResourceInVisualStudio(string workfolder, string filename, int line, string editorCommandExec = "notepad")
        {
            if (this.environment == null)
            {
                if (!string.IsNullOrEmpty(workfolder) && !Path.IsPathRooted(filename))
                {
                    if (!File.Exists(Path.Combine(workfolder, filename)))
                    {
                        var filesData = new List<string>();
                        this.RecursePath(filename, workfolder, filesData);

                        foreach (string file in filesData)
                        {
                            string file1 = file;
                            (new Thread(() => Process.Start(editorCommandExec, file1))).Start();
                        }
                    }
                    else
                    {
                        (new Thread(() => Process.Start(editorCommandExec, Path.Combine(workfolder, filename)))).Start();
                    }
                }

                return;
            }

            ProjectItem files = this.environment.Solution.FindProjectItem(filename);
            if (files != null)
            {
                (new Thread(
                    () =>
                        {
                            try
                            {
                                object masterPath = files.Properties.Item("FullPath").Value;
                                this.environment.ItemOperations.OpenFile(masterPath.ToString());
                                var textSelection = (TextSelection)this.environment.ActiveDocument.Selection;
                                textSelection.GotoLine(line < 1 ? 1 : line);
                                textSelection.SelectLine();
                            }
                            catch
                            {
                                try
                                {
                                    if (File.Exists(filename))
                                    {
                                        this.environment.ItemOperations.OpenFile(filename);
                                        var textSelection = (TextSelection)this.environment.ActiveDocument.Selection;
                                        textSelection.GotoLine(line < 1 ? 1 : line);
                                        textSelection.SelectLine();
                                    }
                                    else
                                    {
                                        this.ErroMessage = "Cannot Open File: " + filename;
                                    }
                                }
                                catch (Exception ex1)
                                {
                                    this.ErroMessage = "Exception Occured: " + filename + " : " + Convert.ToString(line) + " ex: " + ex1.Message;
                                }
                            }
                        })).Start();
            }
            else
            {
                try
                {
                    this.environment.ItemOperations.OpenFile(filename);
                }
                catch (Exception ex)
                {
                    this.ErroMessage = "Exception Occured: " + filename + " : " + Convert.ToString(line) + " ex: " + ex.Message;
                }
            }
        }

        /// <summary>
        /// The get all options for plugin option in application data.
        /// </summary>
        /// <param name="pluginKey">
        /// The plugin key.
        /// </param>
        /// <returns>
        /// The
        ///     <see>
        ///         <cref>Dictionary</cref>
        ///     </see>
        ///     .
        /// </returns>
        public Dictionary<string, string> ReadAllAvailableOptionsInSettings(string pluginKey)
        {
            var options = new Dictionary<string, string>();
            if (!File.Exists(this.ApplicationDataUserSettingsFile))
            {
                return options;
            }

            string[] data = File.ReadAllLines(this.ApplicationDataUserSettingsFile);
            foreach (string s in data.Where(s => s.Contains(pluginKey + "=")))
            {
                try
                {
                    options.Add(s.Split('=')[1].Trim().Split(',')[0], s.Substring(s.IndexOf(',') + 1));
                }
                catch (ArgumentException)
                {
                }
            }

            return options;
        }

        /// <summary>
        /// The get option from application data.
        /// </summary>
        /// <param name="pluginKey">
        /// The plugin Key.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string ReadOptionFromApplicationData(string pluginKey, string key)
        {
            string outstring = string.Empty;

            if (!File.Exists(this.ApplicationDataUserSettingsFile) || string.IsNullOrEmpty(pluginKey))
            {
                return string.Empty;
            }

            string[] data = File.ReadAllLines(this.ApplicationDataUserSettingsFile);
            foreach (string s in data.Where(s => s.Contains(pluginKey + "=" + key + ",")))
            {
                outstring = s.Substring(s.IndexOf(',') + 1);
            }

            return outstring;
        }

        /// <summary>
        /// The get saved option.
        /// </summary>
        /// <param name="category">
        /// The category.
        /// </param>
        /// <param name="page">
        /// The page.
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public string ReadSavedOption(string category, string page, string item)
        {
            if (this.environment == null)
            {
                return string.Empty;
            }

            Properties props = this.environment.Properties[category, page];
            try
            {
                var data = props.Item(item).Value as string;
                return data;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        ///     The restart visual studio.
        /// </summary>
        public void RestartVisualStudio()
        {
            var obj = (IVsShell4)this.provider.GetService(typeof(SVsShell));
            obj.Restart((uint)__VSRESTARTTYPE.RESTART_Normal);
        }

        /// <summary>
        /// The show source diff.
        /// </summary>
        /// <param name="resourceInEditor">
        /// The resource in editor.
        /// </param>
        /// <param name="documentInViewPath">
        /// The document in view path.
        /// </param>
        public void ShowSourceDiff(string resourceInEditor, string documentInViewPath)
        {
            var diff = (IVsDifferenceService)this.provider.GetService(typeof(SVsDifferenceService));
            if (!Directory.Exists(this.TempDataFolder))
            {
                Directory.CreateDirectory(this.TempDataFolder);
            }

            string tempfile = Path.Combine(this.TempDataFolder, "server." + Path.GetFileName(documentInViewPath));

            File.WriteAllText(tempfile, resourceInEditor);

            diff.OpenComparisonWindow(tempfile, documentInViewPath);
        }

        /// <summary>
        ///     The get user app data configuration file.
        /// </summary>
        /// <returns>
        ///     The <see cref="string" />.
        /// </returns>
        public string UserAppDataConfigurationFile()
        {
            return this.ApplicationDataUserSettingsFile;
        }

        /// <summary>
        /// The get vs project item.
        /// </summary>
        /// <param name="filename">
        /// The filename.
        /// </param>
        /// <returns>
        /// The <see cref="VSSonarPlugins.VsProjectItem"/>.
        /// </returns>
        public VsProjectItem VsProjectItem(string filename)
        {
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            string driveLetter = filename.Substring(0, 1);
            ProjectItem item = this.environment.Solution.FindProjectItem(filename);

            if (item == null)
            {
                return null;
            }

            string documentName = item.Document.Name;
            string documentPath = driveLetter + this.GetProperFilePathCapitalization(item.Document.FullName).Substring(1);
            string projectName = item.ContainingProject.Name;
            string projectPath = driveLetter + this.GetProperFilePathCapitalization(item.ContainingProject.FullName).Substring(1);
            string solutionName = this.ActiveSolutionName();
            string solutionPath = driveLetter + this.ActiveSolutionPath().Substring(1);
            return new VsProjectItem(documentName, documentPath, projectName, projectPath, solutionName, solutionPath);
        }

        /// <summary>
        /// The set all options for plugin option in application data.
        /// </summary>
        /// <param name="pluginKey">
        /// The plugin key.
        /// </param>
        /// <param name="project">
        /// The project.
        /// </param>
        /// <param name="options">
        /// The options.
        /// </param>
        public void WriteAllOptionsForPluginOptionInApplicationData(string pluginKey, Resource project, Dictionary<string, string> options)
        {
            this.DeleteOptionsForPlugin(pluginKey, project);
            foreach (var option in options)
            {
                this.WriteOptionInApplicationData(pluginKey, option.Key, option.Value);
            }
        }

        /// <summary>
        /// The set default option.
        /// </summary>
        /// <param name="category">
        /// The category.
        /// </param>
        /// <param name="page">
        /// The page.
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public void WriteDefaultOption(string category, string page, string item, string value)
        {
            if (this.environment == null)
            {
                return;
            }

            Properties props = this.environment.Properties[category, page];
            if (props.Item(item) == null)
            {
                this.WriteOption(category, page, item, value);
            }
            else
            {
                string data = this.ReadSavedOption(category, page, item);
                if (string.IsNullOrEmpty(data))
                {
                    this.WriteOption(category, page, item, value);
                }

                if (this.ReadSavedOption(category, page, item).Contains("vera++\\vera++"))
                {
                    this.WriteOption(category, page, item, value);
                }
            }
        }

        /// <summary>
        /// The set option.
        /// </summary>
        /// <param name="category">
        /// The category.
        /// </param>
        /// <param name="page">
        /// The page.
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public void WriteOption(string category, string page, string item, string value)
        {
            if (this.environment == null)
            {
                return;
            }

            Properties props = this.environment.Properties[category, page];
            props.Item(item).Value = value;
        }

        /// <summary>
        /// The set option in application data.
        /// </summary>
        /// <param name="pluginKey">
        /// The plugin key.
        /// </param>
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        public void WriteOptionInApplicationData(string pluginKey, string key, string value)
        {
            if (string.IsNullOrEmpty(pluginKey) || string.IsNullOrEmpty(key))
            {
                return;
            }

            IEnumerable<string> content = new List<string>();
            string contentWrite = string.Empty;
            if (File.Exists(this.ApplicationDataUserSettingsFile))
            {
                content = File.ReadLines(this.ApplicationDataUserSettingsFile);
            }
            else
            {
                Directory.CreateDirectory(Directory.GetParent(this.ApplicationDataUserSettingsFile).ToString());
                using (File.Create(this.ApplicationDataUserSettingsFile))
                {
                }
            }

            contentWrite += pluginKey + "=" + key + "," + value + "\r\n";
            contentWrite = content.Where(line => !line.Contains(pluginKey + "=" + key + ","))
                .Aggregate(contentWrite, (current, line) => current + (line + "\r\n"));

            using (var writer = new StreamWriter(this.ApplicationDataUserSettingsFile))
            {
                writer.Write(contentWrite);
            }
        }

        /// <summary>
        /// The write to visual studio output.
        /// </summary>
        /// <param name="errorMessage">
        /// The error message.
        /// </param>
        public void WriteToVisualStudioOutput(string errorMessage)
        {
            if (this.CustomPane != null)
            {
                this.CustomPane.OutputString(errorMessage + "\r\n");
                this.CustomPane.FlushToTaskList();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The get long path name.
        /// </summary>
        /// <param name="shortPath">
        /// The short path.
        /// </param>
        /// <param name="sb">
        /// The sb.
        /// </param>
        /// <param name="buffer">
        /// The buffer.
        /// </param>
        /// <returns>
        /// The <see cref="uint"/>.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint GetLongPathName(string shortPath, StringBuilder sb, int buffer);

        /// <summary>
        /// The get proper directory capitalization.
        /// </summary>
        /// <param name="dirInfo">
        /// The dir info.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetProperDirectoryCapitalization(DirectoryInfo dirInfo)
        {
            DirectoryInfo parentDirInfo = dirInfo.Parent;
            return null == parentDirInfo
                       ? dirInfo.Name
                       : Path.Combine(GetProperDirectoryCapitalization(parentDirInfo), parentDirInfo.GetDirectories(dirInfo.Name)[0].Name);
        }

        /// <summary>
        /// The recurse path.
        /// </summary>
        /// <param name="fileName">
        /// The file name.
        /// </param>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="paths">
        /// The paths.
        /// </param>
        private void RecursePath(string fileName, string path, List<string> paths)
        {
            try
            {
                var dir = new DirectoryInfo(path);
                paths.AddRange(from info in dir.GetFiles() where info.FullName.EndsWith(fileName) select info.FullName);

                foreach (DirectoryInfo info in dir.GetDirectories().Where(info => !info.Name.StartsWith(".")))
                {
                    this.RecursePath(fileName, info.FullName, paths);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #endregion
    }
}