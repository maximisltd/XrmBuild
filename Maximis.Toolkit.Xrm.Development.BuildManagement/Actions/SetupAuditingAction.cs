using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class SetupAuditingAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Loop through each Organization
            foreach (OrganizationConfig orgConfig in GetOrgConfigs(envConfig, orgUniqueNames))
            {
                OutputDivider("Setup Auditing: " + orgConfig.FriendlyName);

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    AuditConfigurationHelper.SetOrganizationAuditingEnabled(orgService, orgConfig.EnableAuditing);
                    AuditConfigurationHelper.SetEntityAuditingEnabled(orgService, config.AuditedEntities.ToArray());
                    SolutionHelper.PublishAllCustomisations(orgService);
                }
            }
        }
    }
}