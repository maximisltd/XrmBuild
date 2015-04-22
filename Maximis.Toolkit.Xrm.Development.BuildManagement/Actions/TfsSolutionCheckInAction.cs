﻿using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using System.Collections.Generic;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class TfsSolutionCheckInAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Organizations which have their Solutions kept in Source Control
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.TfsSync != null && q.TfsSync.CheckInSolution);

            // Export Solutions
            ExportSolutions(envConfig.ExportPath, orgConfigs, exportManaged: false);

            CheckSolutionsIntoSourceControl(config, environmentName, orgConfigs);
        }
    }
}