using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class DeployFromDiskAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment being DEPLOYED TO (i.e. LIVE or TEST environment)
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Build a list from solutions located on disk
            foreach (OrganizationConfig orgConfig in config.Environments.SelectMany(q => q.Organizations))
            {
                string solutionExportPath = Path.Combine(envConfig.ExportPath, "Solutions", orgConfig.UniqueName);
                if (!Directory.Exists(solutionExportPath)) continue;

                string latestManagedFile = Directory.EnumerateFiles(solutionExportPath, "*_managed.zip").OrderByDescending(q => q).FirstOrDefault();
                if (!string.IsNullOrEmpty(latestManagedFile)) managedSolutions.Add(orgConfig.UniqueName, latestManagedFile);

                string latestUnmanagedFile = Directory.EnumerateFiles(solutionExportPath, "*_unmanaged.zip").OrderByDescending(q => q).FirstOrDefault();
                if (!string.IsNullOrEmpty(latestUnmanagedFile)) unmanagedSolutions.Add(orgConfig.UniqueName, latestUnmanagedFile);
            }

            // Loop through each target environment
            foreach (OrganizationConfig targetOrg in GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.Deployment != null && q.Deployment.DeployFromDisk.Any()))
            {
                OutputDivider("Deployment: " + targetOrg.FriendlyName);

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(targetOrg.CrmContext))
                {
                    // Import the managed solutions into the target environment
                    ImportSolutions(orgService, targetOrg.UniqueName, targetOrg.Deployment.Mode, envConfig.ImportSolutionsAsync, targetOrg.Deployment.DeployFromDisk.ToArray());

                    // Import Security Roles
                    foreach (string deployFrom in targetOrg.Deployment.DeployFromDisk)
                    {
                        SolutionHelper.ImportSecurityRoles(orgService, targetOrg.SolutionName, Path.Combine(envConfig.ExportPath, "SecurityRoles", deployFrom));
                    }
                }
            }
        }
    }
}