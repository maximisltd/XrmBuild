using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Microsoft.Xrm.Sdk.Client;
using System.Collections.Generic;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class DeployAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment being DEPLOYED TO (i.e. LIVE or TEST environment)
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Get all Organizations
            IEnumerable<OrganizationConfig> allOrgConfigs = config.Environments.SelectMany(q => q.Organizations);

            // Loop through each target environment
            foreach (OrganizationConfig targetOrg in GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.Deployment != null && q.Deployment.DeployFromOrgs.Any()))
            {
                OutputDivider("Deployment: " + targetOrg.FriendlyName);

                // Export the Source solutions
                ExportSolutions(envConfig.ExportPath, allOrgConfigs.Where(q => targetOrg.Deployment.DeployFromOrgs.Contains(q.UniqueName)), false, targetOrg.CrmContext.Version);

                // Import the managed versions into the target environment
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(targetOrg.CrmContext))
                {
                    ImportManagedSolutions(orgService, targetOrg.Deployment.DeployFromOrgs, targetOrg.UniqueName, targetOrg.Deployment.OverwriteUnmanaged, envConfig.ImportSolutionsAsync);
                }
            }
        }
    }
}