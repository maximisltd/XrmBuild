using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
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

            bool exportManaged = orgConfigs.Any(q => q.InternalSync.ImportManaged.Any());

            int minExportPass = orgConfigs.Select(q => q.InternalSync.ExportPass).Where(q => q > 0).Min();
            int minImportPass = orgConfigs.Select(q => q.InternalSync.ImportPass).Where(q => q > 0).Min();
            int currentPass = new[] { minExportPass, minImportPass }.Min();

            while (true)
            {
                // Get the configs for this pass
                IEnumerable<OrganizationConfig> toExportThisPass = orgConfigs.Where(q => q.InternalSync.ExportPass == currentPass);
                IEnumerable<OrganizationConfig> toImportThisPass = orgConfigs.Where(q => q.InternalSync.ImportPass == currentPass);

                // Drop out if no more to do
                if (!toExportThisPass.Any() && !toImportThisPass.Any()) break;

                // Do Exports
                if (toExportThisPass.Any())
                {
                    OutputDivider(string.Format("Internal Sync - Export Solutions (Pass {0})", currentPass));
                    ExportSolutions(envConfig.ExportPath, toExportThisPass, exportManaged);

                    // Check in to TFS
                    if (config.SourceControl != null)
                    {
                        try
                        {
                            CheckSolutionsIntoSourceControl(config, envConfig, toExportThisPass);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("Check-in failed: {0}", ex.Message);
                        }
                    }
                }

                // Do Imports
                if (toImportThisPass.Any())
                {
                    OutputDivider(string.Format("Internal Sync - Import Solutions (Pass {0})", currentPass));
                    ImportSolutionsInternalSync(toImportThisPass, envConfig.ImportSolutionsAsync, false, config.PostDeployment);

                    foreach (OrganizationConfig orgConfig in toExportThisPass.Where(q => !string.IsNullOrEmpty(q.SyncRibbonsWith)))
                    {
                        OutputDivider("Synchronise Ribbon: " + orgConfig.FriendlyName);

                        if (SyncRibbons(orgConfig, new SyncRibbonConfig
                        {
                            ExportPath = envConfig.ExportPath,
                            ImportSolutionsAsync = envConfig.ImportSolutionsAsync,
                            OrgConfig = GetOrgConfigs(envConfig, orgConfig.SyncRibbonsWith).Single(),
                            PublisherId = config.Publisher.PublisherId
                        }))
                        {
                            using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                            {
                                SolutionHelper.PublishAllCustomisations(orgService);
                            }
                        }
                    }
                }

                currentPass++;
            }
        }
    }
}