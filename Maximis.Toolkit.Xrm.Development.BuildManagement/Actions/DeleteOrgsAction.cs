﻿using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class DeleteOrgsAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            foreach (OrganizationConfig orgConfig in GetOrgConfigs(envConfig, orgUniqueNames))
            {
                OutputDivider("Delete Organization: " + orgConfig.UniqueName);
                OrganizationHelper.DeleteOrganization(orgConfig.CrmContext);
            }
        }
    }
}