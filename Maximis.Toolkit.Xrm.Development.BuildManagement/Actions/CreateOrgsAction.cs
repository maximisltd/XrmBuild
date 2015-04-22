using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class CreateOrgsAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            foreach (OrganizationConfig orgConfig in GetOrgConfigs(envConfig, orgUniqueNames))
            {
                OutputDivider("Create Organization: " + orgConfig.FriendlyName);
                OrganizationHelper.CreateOrganization(orgConfig.CrmContext, orgConfig.UniqueName, orgConfig.FriendlyName, envConfig.SqlServerName, envConfig.SrsUrl, true);
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    OrganizationHelper.SetLocale(orgService);
                }
            }
        }
    }
}