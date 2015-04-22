using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class EnvSyncOutAction : BaseAction
    {
        private Dictionary<string, string> unmanagedSolutions = new Dictionary<string, string>();

        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get all Organizations
            IEnumerable<OrganizationConfig> allOrgConfigs = config.Environments.SelectMany(q => q.Organizations);

            // Get Source Environment
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Loop through all applicable Organizations in the Source Environment
            foreach (OrganizationConfig sourceOrg in GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.EnvSyncOut.Any()))
            {
                OutputDivider("Environment Sync (Outbound): " + sourceOrg.FriendlyName);

                // Export the solution (if not already done)
                if (sourceOrg != null && !unmanagedSolutions.ContainsKey(sourceOrg.UniqueName))
                {
                    // Ensure Directory Exists
                    string fullExportPath = Path.Combine(envConfig.ExportPath, "Solutions", sourceOrg.UniqueName);
                    if (!Directory.Exists(fullExportPath)) Directory.CreateDirectory(fullExportPath);

                    // Export Solution
                    using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(sourceOrg.CrmContext))
                    {
                        Trace.WriteLine(string.Format("Export Unmanaged Solution '{0}'", sourceOrg.UniqueName));
                        SolutionHelper.PublishAllCustomisations(orgService);
                        unmanagedSolutions.Add(sourceOrg.UniqueName, SolutionHelper.ExportSolution(orgService, sourceOrg.SolutionName, false, fullExportPath, true));
                    }
                }

                foreach (string toImport in sourceOrg.EnvSyncOut)
                {
                    // Get Target Organization Config
                    OrganizationConfig targetOrg = allOrgConfigs.SingleOrDefault(q => q.UniqueName == toImport);

                    // Import Solution
                    using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(targetOrg.CrmContext))
                    {
                        Trace.WriteLine(string.Format("Import Unmanaged Solution '{0}' into Organization '{1}'", sourceOrg.SolutionName, targetOrg.UniqueName));
                        SolutionHelper.DeleteSolution(orgService, sourceOrg.SolutionName);
                        SolutionHelper.ImportSolution(orgService, unmanagedSolutions[sourceOrg.UniqueName], importAsync: envConfig.ImportSolutionsAsync, overwriteUnmanaged: true, publishDedupeRules: true);
                        SolutionHelper.SyncSolutionComponents(orgService, sourceOrg.SolutionName, targetOrg.SolutionName);
                        SolutionHelper.DeleteSolution(orgService, sourceOrg.SolutionName);
                        SolutionHelper.PublishAllCustomisations(orgService);
                    }
                }
            }
        }
    }
}