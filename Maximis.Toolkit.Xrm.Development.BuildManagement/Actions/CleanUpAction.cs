using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class CleanUpAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            OutputDivider("Clean Up: " + envConfig.UniqueName);

            foreach (CleanUpFolder cleanUp in envConfig.CleanUp)
            {
                DirectoryInfo di = new DirectoryInfo(cleanUp.Path);
                if (!di.Exists) continue;

                // Delete Files older than the specified number of days
                DateTime deleteOlderThan = DateTime.Today.AddDays(cleanUp.FileAgeDays * -1);
                foreach (FileInfo fi in di.EnumerateFiles("*.*", SearchOption.AllDirectories))
                {
                    if (fi.CreationTime <= deleteOlderThan)
                    {
                        Trace.WriteLine(string.Format("Deleting file '{0}'", fi.FullName));
                        fi.Delete();
                    }
                }

                // Delete any empty directories left over from deleting files
                IEnumerable<DirectoryInfo> childDirs = di.EnumerateDirectories("*", SearchOption.AllDirectories);
                childDirs = childDirs.OrderByDescending(q => q.FullName.Count(x => x == '\\'));
                foreach (DirectoryInfo child in childDirs)
                {
                    if (!child.EnumerateFileSystemInfos().Any())
                    {
                        Trace.WriteLine(string.Format("Deleting folder '{0}'", child.FullName));
                        child.Delete();
                    }
                }
            }
        }
    }
}