using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Microsoft.Xrm.Sdk.Client;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class PostDeploymentAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Loop through each Organization
            foreach (OrganizationConfig orgConfig in GetOrgConfigs(envConfig, orgUniqueNames))
            {
                OutputDivider(string.Format("Post Deployment - {0}", orgConfig.FriendlyName));

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    PerformPostDeploymentSteps(orgService, config.PostDeployment);
                }
            }
        }
    }
}