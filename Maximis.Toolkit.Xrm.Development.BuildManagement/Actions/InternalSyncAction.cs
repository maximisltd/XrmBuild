using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class InternalSyncAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.InternalSync != null);

            int currentPass = orgConfigs.Select(q => q.InternalSync.ExportPass).Min();
            if (currentPass < 1) currentPass = 1;

            while (true)
            {
                // Get the configs for this pass
                IEnumerable<OrganizationConfig> toExportThisPass = orgConfigs.Where(q => q.InternalSync.ExportPass == currentPass);
                IEnumerable<OrganizationConfig> toImportThisPass = orgConfigs.Where(q => q.InternalSync.ImportPass == currentPass);

                // Drop out if no more to do
                if (!toExportThisPass.Any() && !toImportThisPass.Any()) break;

                // Do Exports
                OutputDivider(string.Format("Internal Sync - Export Solutions (Pass {0})", currentPass));
                ExportSolutions(envConfig.ExportPath, toExportThisPass);
                try
                {
                    CheckSolutionsIntoSourceControl(config, environmentName, toExportThisPass);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Check-in failed: {0}", ex.Message);
                }

                // Do Imports
                OutputDivider(string.Format("Internal Sync - Import Solutions (Pass {0})", currentPass));
                ImportSolutionsInternalSync(toImportThisPass, envConfig.ImportSolutionsAsync);

                currentPass++;
            }
        }
    }
}