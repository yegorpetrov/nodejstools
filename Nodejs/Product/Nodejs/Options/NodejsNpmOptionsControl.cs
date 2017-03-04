// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.NodejsTools.Project;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.NodejsTools.Options
{
    public partial class NodejsNpmOptionsControl : UserControl
    {
        public NodejsNpmOptionsControl()
        {
            InitializeComponent();
        }

        internal void SyncControlWithPageSettings(NodejsNpmOptionsPage page)
        {
            this._showOutputWhenRunningNpm.Checked = page.ShowOutputWindowWhenExecutingNpm;
            this._cacheClearedSuccessfully.Visible = false;
        }

        internal void SyncPageWithControlSettings(NodejsNpmOptionsPage page)
        {
            page.ShowOutputWindowWhenExecutingNpm = this._showOutputWhenRunningNpm.Checked;
        }

        private void ClearCacheButton_Click(object sender, EventArgs e)
        {
            var didClearNpmCache = TryDeleteCacheDirectory(NodejsConstants.NpmCachePath);
            var didClearTools = TryDeleteCacheDirectory(NodejsConstants.ExternalToolsPath);

            if (!didClearNpmCache || !didClearTools)
            {
                MessageBox.Show(
                   string.Format(CultureInfo.CurrentCulture, Resources.CacheDirectoryClearFailedCaption, NodejsConstants.NtvsLocalAppData),
                   Resources.CacheDirectoryClearFailedTitle,
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Information);
            }

            this._cacheClearedSuccessfully.Visible = didClearNpmCache && didClearTools;
        }

        private static bool TryDeleteCacheDirectory(string cachePath)
        {
            if (!Directory.Exists(cachePath))
            {
                return true;
            }

            try
            {
                // To handle long paths, nuke the directory contents with robocopy
                var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                Directory.CreateDirectory(tempDirectory);
                var psi = new ProcessStartInfo("cmd.exe", string.Format(CultureInfo.InvariantCulture, @"/C robocopy /mir ""{0}"" ""{1}""", tempDirectory, cachePath))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit(10000);
                }

                // Then delete the directory itself
                try
                {
                    Directory.Delete(cachePath, true);
                }
                catch (DirectoryNotFoundException)
                {
                    // noop
                }

                return !Directory.Exists(cachePath);
            }
            catch (IOException)
            {
                // files are in use or path is too long
                return false;
            }
            catch (Exception exception)
            {
                try
                {
                    ActivityLog.LogError(SR.ProductName, exception.ToString());
                }
                catch (InvalidOperationException)
                {
                    // Activity Log is unavailable.
                }
            }
            return false;
        }
    }
}

