using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class InstallPreReqsAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Loop through each Organization
            foreach (OrganizationConfig orgConfig in GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.PreRequisiteSolutions != null && q.PreRequisiteSolutions.Any()))
            {
                // Install Pre-requisite Solutions
                OutputDivider("Installing Pre-requisite Solutions: " + orgConfig.FriendlyName);
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    foreach (string solutionPath in orgConfig.PreRequisiteSolutions)
                    {
                        SolutionHelper.ImportSolution(orgService, solutionPath, envConfig.ImportSolutionsAsync, false, true);
                    }
                }
            }
        }
    }
}