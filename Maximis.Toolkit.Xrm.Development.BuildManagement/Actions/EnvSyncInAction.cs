using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class EnvSyncInAction : BaseAction
    {
        private Dictionary<string, string> unmanagedSolutions = new Dictionary<string, string>();

        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get all Organizations
            IEnumerable<OrganizationConfig> allOrgConfigs = config.Environments.SelectMany(q => q.Organizations);

            // Get the Target Environment
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            foreach (OrganizationConfig targetOrg in GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.EnvSyncIn.Any()))
            {
                OutputDivider("Environment Sync (Inbound): " + targetOrg.FriendlyName);

                foreach (string toExport in targetOrg.EnvSyncIn)
                {
                    // Get Source Organization Config
                    OrganizationConfig sourceOrg = allOrgConfigs.SingleOrDefault(q => q.UniqueName == toExport);

                    // If we don't already have it, export
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

                    // Import Solution
                    using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(targetOrg.CrmContext))
                    {
                        Trace.WriteLine(string.Format("Import Unmanaged Solution '{0}' into Organization '{1}'", sourceOrg.SolutionName, targetOrg.UniqueName));
                        SolutionHelper.ImportSolution(orgService, unmanagedSolutions[sourceOrg.UniqueName], importAsync: true, overwriteUnmanaged: true, publishDedupeRules: true);
                        SolutionHelper.PublishAllCustomisations(orgService);
                    }
                }
            }
        }
    }
}