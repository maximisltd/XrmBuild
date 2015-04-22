using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class SetupSolutionsAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Loop through each Organization
            foreach (OrganizationConfig orgConfig in GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.InternalSync != null && q.InternalSync.ExportPass > 0))
            {
                // Create Publisher and Empty Solution
                OutputDivider("Create Publisher and Solution: " + orgConfig.FriendlyName);
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    SolutionDefinition solution = new SolutionDefinition
                    {
                        UniqueName = orgConfig.SolutionName,
                        FriendlyName = orgConfig.FriendlyName,
                        PublisherId = OrganizationHelper.CreatePublisher(orgService, config.Publisher)
                    };
                    SolutionHelper.CreateSolution(orgService, solution);
                }
            }
        }
    }
}