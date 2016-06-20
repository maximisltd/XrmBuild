using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class SyncRibbonsAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            foreach (OrganizationConfig orgConfig in GetOrgConfigs(envConfig, orgUniqueNames).Where(q => !string.IsNullOrWhiteSpace(q.SyncRibbonsWith)))
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
    }
}