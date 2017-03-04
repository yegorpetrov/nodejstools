// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using Microsoft.NodejsTools.Telemetry;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudioTools;

namespace Microsoft.NodejsTools.Project.ImportWizard
{
    internal class ImportSettings : DependencyObject
    {
        public static readonly string DefaultLanguageExtensionsFilter = string.Join(";",
            new[] {
                ".txt",
                ".htm",
                ".html",
                ".css",
                ".png",
                ".jpg",
                ".gif",
                ".bmp",
                ".ico",
                ".svg",
                ".json",
                ".md",
                ".ejs",
                ".styl",
                ".xml",
                ".ts",
                Jade.JadeContentTypeDefinition.JadeFileExtension,
                Jade.JadeContentTypeDefinition.PugFileExtension
            }.Select(x => "*" + x));

        private bool _isAutoGeneratedProjectPath;

        public ImportSettings()
        {
            this.TopLevelJavaScriptFiles = new BulkObservableCollection<string>();

            this.Filters = DefaultLanguageExtensionsFilter;
        }

        public string ProjectPath
        {
            get { return (string)GetValue(ProjectPathProperty); }
            set { SetValue(ProjectPathProperty, value); }
        }

        public string SourcePath
        {
            get { return (string)GetValue(SourcePathProperty); }
            set { SetValue(SourcePathProperty, value); }
        }

        public string Filters
        {
            get { return (string)GetValue(FiltersProperty); }
            set { SetValue(FiltersProperty, value); }
        }

        public ObservableCollection<string> TopLevelJavaScriptFiles
        {
            get { return (ObservableCollection<string>)GetValue(TopLevelJavaScriptFilesProperty); }
            private set { SetValue(TopLevelJavaScriptFilesPropertyKey, value); }
        }

        public string StartupFile
        {
            get { return (string)GetValue(StartupFileProperty); }
            set { SetValue(StartupFileProperty, value); }
        }

        public static readonly DependencyProperty ProjectPathProperty = DependencyProperty.Register("ProjectPath", typeof(string), typeof(ImportSettings), new PropertyMetadata(ProjectPath_Updated));
        public static readonly DependencyProperty SourcePathProperty = DependencyProperty.Register("SourcePath", typeof(string), typeof(ImportSettings), new PropertyMetadata(SourcePath_Updated));
        public static readonly DependencyProperty FiltersProperty = DependencyProperty.Register("Filters", typeof(string), typeof(ImportSettings), new PropertyMetadata());
        private static readonly DependencyPropertyKey TopLevelJavaScriptFilesPropertyKey = DependencyProperty.RegisterReadOnly("TopLevelJavaScriptFiles", typeof(ObservableCollection<string>), typeof(ImportSettings), new PropertyMetadata());
        public static readonly DependencyProperty TopLevelJavaScriptFilesProperty = TopLevelJavaScriptFilesPropertyKey.DependencyProperty;
        public static readonly DependencyProperty StartupFileProperty = DependencyProperty.Register("StartupFile", typeof(string), typeof(ImportSettings), new PropertyMetadata());

        public bool IsValid
        {
            get { return (bool)GetValue(IsValidProperty); }
            private set { SetValue(IsValidPropertyKey, value); }
        }

        private static void RecalculateIsValid(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!d.Dispatcher.CheckAccess())
            {
                d.Dispatcher.Invoke((Action)(() => RecalculateIsValid(d, e)));
                return;
            }

            var s = d as ImportSettings;
            if (s == null)
            {
                d.SetValue(IsValidPropertyKey, false);
                return;
            }
            d.SetValue(IsValidPropertyKey,
                CommonUtils.IsValidPath(s.SourcePath) &&
                CommonUtils.IsValidPath(s.ProjectPath) &&
                Directory.Exists(s.SourcePath)
            );
        }

        private static void SourcePath_Updated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!d.Dispatcher.CheckAccess())
            {
                d.Dispatcher.BeginInvoke((Action)(() => SourcePath_Updated(d, e)));
                return;
            }

            RecalculateIsValid(d, e);

            var s = d as ImportSettings;
            if (s == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(s.ProjectPath) || s._isAutoGeneratedProjectPath)
            {
                s.ProjectPath = CommonUtils.GetAvailableFilename(s.SourcePath, Path.GetFileName(s.SourcePath), ".njsproj");
                s._isAutoGeneratedProjectPath = true;
            }

            var sourcePath = s.SourcePath;
            if (Directory.Exists(sourcePath))
            {
                var filters = s.Filters;
                var dispatcher = s.Dispatcher;

                // Nice async machinery does not work correctly in unit-tests,
                // so using Dispatcher directly.
                Task.Factory.StartNew(() =>
                {
                    var files = Directory.EnumerateFiles(sourcePath, "*.js", SearchOption.TopDirectoryOnly);

                    if (filters.Split(';').Any(x => x.EndsWith(NodejsConstants.TypeScriptExtension, StringComparison.OrdinalIgnoreCase)))
                    {
                        files = Directory.EnumerateFiles(
                            sourcePath,
                            "*.ts",
                            SearchOption.TopDirectoryOnly
                        ).Concat(files);
                    }

                    var fileList = files.Select(f => Path.GetFileName(f)).ToList();
                    dispatcher.BeginInvoke((Action)(() =>
                    {
                        var tlpf = s.TopLevelJavaScriptFiles as BulkObservableCollection<string>;
                        if (tlpf != null)
                        {
                            tlpf.Clear();
                            tlpf.AddRange(fileList);
                        }
                        else
                        {
                            s.TopLevelJavaScriptFiles.Clear();
                            foreach (var file in fileList)
                            {
                                s.TopLevelJavaScriptFiles.Add(file);
                            }
                        }
                        if (fileList.Contains("server.ts"))
                        {
                            s.StartupFile = "server.ts";
                        }
                        else if (fileList.Contains("server.js"))
                        {
                            s.StartupFile = "server.js";
                        }
                        else if (fileList.Contains("app.ts"))
                        {
                            s.StartupFile = "app.ts";
                        }
                        else if (fileList.Contains("app.js"))
                        {
                            s.StartupFile = "app.js";
                        }
                        else if (fileList.Count > 0)
                        {
                            s.StartupFile = fileList.First();
                        }
                    }));
                });
            }
            else
            {
                s.TopLevelJavaScriptFiles.Clear();
            }
        }

        private static void ProjectPath_Updated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as ImportSettings;
            if (self != null)
            {
                self._isAutoGeneratedProjectPath = false;
            }
            RecalculateIsValid(d, e);
        }

        private static readonly DependencyPropertyKey IsValidPropertyKey = DependencyProperty.RegisterReadOnly("IsValid", typeof(bool), typeof(ImportSettings), new PropertyMetadata(false));
        public static readonly DependencyProperty IsValidProperty = IsValidPropertyKey.DependencyProperty;

        private static XmlWriter GetDefaultWriter(string projectPath)
        {
            var settings = new XmlWriterSettings
            {
                CloseOutput = true,
                Encoding = Encoding.UTF8,
                Indent = true,
                IndentChars = "    ",
                NewLineChars = Environment.NewLine,
                NewLineOnAttributes = false
            };

            var dir = Path.GetDirectoryName(projectPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return XmlWriter.Create(projectPath, settings);
        }

        public string CreateRequestedProject()
        {
            var task = CreateRequestedProjectAsync();
            task.Wait();
            return task.Result;
        }

        public Task<string> CreateRequestedProjectAsync()
        {
            var projectPath = this.ProjectPath;
            var sourcePath = this.SourcePath;
            var filters = this.Filters;
            var startupFile = this.StartupFile;

            return Task.Run<string>(() =>
            {
                var success = false;
                Guid projectGuid;
                try
                {
                    using (var writer = GetDefaultWriter(projectPath))
                    {
                        WriteProjectXml(writer, projectPath, sourcePath, filters, startupFile, true, out projectGuid);
                    }
                    NodejsPackage.Instance?.TelemetryLogger.LogProjectImported(projectGuid);
                    success = true;
                    return projectPath;
                }
                finally
                {
                    if (!success)
                    {
                        try
                        {
                            File.Delete(projectPath);
                        }
                        catch
                        {
                            // Try and avoid leaving stray files, but it does
                            // not matter much if we do.
                        }
                    }
                }
            });
        }

        private static bool ShouldIncludeDirectory(string dirName)
        {
            // Why relative paths only?
            // Consider the following absolute path:
            //   c:\sources\.dotted\myselectedfolder\routes\
            // Where the folder selected in the wizard is:
            //   c:\sources\.dotted\myselectedfolder\
            // We don't want to exclude that folder from the project, despite a part
            // of that path having a dot prefix.
            // By evaluating relative paths only:
            //   routes\
            // We won't reject the folder.
            Debug.Assert(!Path.IsPathRooted(dirName));
            return !dirName.Split(new char[] { '/', '\\' }).Any(name => name.StartsWith("."));
        }

        internal static void WriteProjectXml(
            XmlWriter writer,
            string projectPath,
            string sourcePath,
            string filters,
            string startupFile,
            bool excludeNodeModules,
            out Guid projectGuid
        )
        {
            var projectHome = CommonUtils.GetRelativeDirectoryPath(Path.GetDirectoryName(projectPath), sourcePath);
            projectGuid = Guid.NewGuid();

            writer.WriteStartDocument();
            writer.WriteStartElement("Project", "http://schemas.microsoft.com/developer/msbuild/2003");
            writer.WriteAttributeString("DefaultTargets", "Build");

            writer.WriteStartElement("PropertyGroup");

            writer.WriteStartElement("Configuration");
            writer.WriteAttributeString("Condition", " '$(Configuration)' == '' ");
            writer.WriteString("Debug");
            writer.WriteEndElement();

            writer.WriteElementString("SchemaVersion", "2.0");
            writer.WriteElementString("ProjectGuid", projectGuid.ToString("B"));
            writer.WriteElementString("ProjectHome", projectHome);
            writer.WriteElementString("ProjectView", "ShowAllFiles");

            if (CommonUtils.IsValidPath(startupFile))
            {
                writer.WriteElementString("StartupFile", Path.GetFileName(startupFile));
            }
            else
            {
                writer.WriteElementString("StartupFile", String.Empty);
            }
            writer.WriteElementString("WorkingDirectory", ".");
            writer.WriteElementString("OutputPath", ".");
            writer.WriteElementString("ProjectTypeGuids", "{3AF33F2E-1136-4D97-BBB7-1795711AC8B8};{349c5851-65df-11da-9384-00065b846f21};{9092AA53-FB77-4645-B42D-1CCCA6BD08BD}");
            var typeScriptSupport = EnumerateAllFiles(sourcePath, filters, excludeNodeModules)
                .Any(filename => NodejsConstants.TypeScriptExtension.Equals(Path.GetExtension(filename), StringComparison.OrdinalIgnoreCase));

            if (typeScriptSupport)
            {
                writer.WriteElementString("TypeScriptSourceMap", "true");
                writer.WriteElementString("TypeScriptModuleKind", "CommonJS");
                writer.WriteElementString("EnableTypeScript", "true");
            }

            writer.WriteStartElement("VisualStudioVersion");
            writer.WriteAttributeString("Condition", "'$(VisualStudioVersion)' == ''");
            writer.WriteString("14.0");
            writer.WriteEndElement();

            writer.WriteStartElement("VSToolsPath");
            writer.WriteAttributeString("Condition", "'$(VSToolsPath)' == ''");
            writer.WriteString(@"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)");
            writer.WriteEndElement();

            writer.WriteEndElement(); // </PropertyGroup>

            // VS requires property groups with conditions for Debug
            // and Release configurations or many COMExceptions are
            // thrown.
            writer.WriteStartElement("PropertyGroup");
            writer.WriteAttributeString("Condition", "'$(Configuration)' == 'Debug'");
            writer.WriteEndElement();
            writer.WriteStartElement("PropertyGroup");
            writer.WriteAttributeString("Condition", "'$(Configuration)' == 'Release'");
            writer.WriteEndElement();

            var folders = new HashSet<string>(
                Directory.EnumerateDirectories(sourcePath, "*", SearchOption.AllDirectories)
                    .Select(dirName =>
                        CommonUtils.TrimEndSeparator(
                            CommonUtils.GetRelativeDirectoryPath(sourcePath, dirName)
                        )
                    )
                    .Where(ShouldIncludeDirectory)
            );

            // Exclude node_modules and bower_components folders.
            if (excludeNodeModules)
            {
                folders.RemoveWhere(NodejsConstants.ContainsNodeModulesOrBowerComponentsFolder);
            }

            writer.WriteStartElement("ItemGroup");
            foreach (var file in EnumerateAllFiles(sourcePath, filters, excludeNodeModules))
            {
                var ext = Path.GetExtension(file);
                if (NodejsConstants.JavaScriptExtension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    writer.WriteStartElement("Compile");
                }
                else if (NodejsConstants.TypeScriptExtension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                {
                    writer.WriteStartElement("TypeScriptCompile");
                }
                else
                {
                    writer.WriteStartElement("Content");
                }
                writer.WriteAttributeString("Include", file);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("ItemGroup");
            foreach (var folder in folders.Where(s => !string.IsNullOrWhiteSpace(s)).OrderBy(s => s))
            {
                writer.WriteStartElement("Folder");
                writer.WriteAttributeString("Include", folder);
                writer.WriteEndElement();
            }
            writer.WriteEndElement();

            writer.WriteStartElement("Import");
            writer.WriteAttributeString("Project", @"$(MSBuildToolsPath)\Microsoft.Common.targets");
            writer.WriteAttributeString("Condition", @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')");
            writer.WriteEndElement();

            writer.WriteComment("Do not delete the following Import Project.  While this appears to do nothing it is a marker for setting TypeScript properties before our import that depends on them.");
            writer.WriteStartElement("Import");
            writer.WriteAttributeString("Project", @"$(MSBuildExtensionsPath32)\Microsoft\VisualStudio\v$(VisualStudioVersion)\TypeScript\Microsoft.TypeScript.targets");
            writer.WriteAttributeString("Condition", @"False");
            writer.WriteEndElement();

            writer.WriteStartElement("Import");
            writer.WriteAttributeString("Project", @"$(VSToolsPath)\Node.js Tools\Microsoft.NodejsTools.targets");
            writer.WriteEndElement();

            writer.WriteRaw(@"
    <ProjectExtensions>
        <VisualStudio>
          <FlavorProperties GUID=""{349c5851-65df-11da-9384-00065b846f21}"">
            <WebProjectProperties>
              <UseIIS>False</UseIIS>
              <AutoAssignPort>True</AutoAssignPort>
              <DevelopmentServerPort>0</DevelopmentServerPort>
              <DevelopmentServerVPath>/</DevelopmentServerVPath>
              <IISUrl>http://localhost:48022/</IISUrl>
              <NTLMAuthentication>False</NTLMAuthentication>
              <UseCustomServer>True</UseCustomServer>
              <CustomServerUrl>http://localhost:1337</CustomServerUrl>
              <SaveServerSettingsInUserFile>False</SaveServerSettingsInUserFile>
            </WebProjectProperties>
          </FlavorProperties>
          <FlavorProperties GUID=""{349c5851-65df-11da-9384-00065b846f21}"" User="""">
            <WebProjectProperties>
              <StartPageUrl>
              </StartPageUrl>
              <StartAction>CurrentPage</StartAction>
              <AspNetDebugging>True</AspNetDebugging>
              <SilverlightDebugging>False</SilverlightDebugging>
              <NativeDebugging>False</NativeDebugging>
              <SQLDebugging>False</SQLDebugging>
              <ExternalProgram>
              </ExternalProgram>
              <StartExternalURL>
              </StartExternalURL>
              <StartCmdLineArguments>
              </StartCmdLineArguments>
              <StartWorkingDirectory>
              </StartWorkingDirectory>
              <EnableENC>False</EnableENC>
              <AlwaysStartWebServerOnDebug>False</AlwaysStartWebServerOnDebug>
            </WebProjectProperties>
          </FlavorProperties>
        </VisualStudio>
    </ProjectExtensions>
");

            writer.WriteEndElement(); // </Project>

            writer.WriteEndDocument();
        }

        private static IEnumerable<string> EnumerateAllFiles(string source, string filters, bool excludeNodeModules)
        {
            var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var patterns = filters.Split(';').Concat(new[] { "*.js" }).Select(p => p.Trim()).ToArray();

            var directories = new List<string>() { source };

            try
            {
                directories.AddRange(
                    Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories)
                    .Where(dirName => ShouldIncludeDirectory(CommonUtils.TrimEndSeparator(CommonUtils.GetRelativeDirectoryPath(source, dirName))))
                );
            }
            catch (UnauthorizedAccessException)
            {
            }

            foreach (var dir in directories)
            {
                if (excludeNodeModules && NodejsConstants.ContainsNodeModulesOrBowerComponentsFolder(dir))
                {
                    continue;
                }
                try
                {
                    foreach (var filter in patterns)
                    {
                        files.UnionWith(Directory.EnumerateFiles(dir, filter, SearchOption.TopDirectoryOnly));
                    }
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            var res = files
                .Where(path => path.StartsWith(source, StringComparison.Ordinal))
                .Select(path => path.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
                .Distinct(StringComparer.OrdinalIgnoreCase);

            return res;
        }
    }
}

