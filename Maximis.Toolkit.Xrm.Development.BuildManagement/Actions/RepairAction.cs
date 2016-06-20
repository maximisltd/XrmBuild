using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using System.Collections.Generic;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class RepairAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment and its Organization Configs
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames);

            // STEP 1: Export Data
            if (config.Repair != null && config.Repair.ExportData)
            {
                new StaticDataExportAction().PerformAction(config, environmentName, orgUniqueNames);
            }

            // STEP 2: Export all first-level solutions (InternalSync.ExportPass == 1)
            OutputDivider("Export Solutions");
            IEnumerable<OrganizationConfig> firstLevelOrgs = orgConfigs.Where(q => q.InternalSync != null && q.InternalSync.ExportPass == 1);
            ExportSolutions(envConfig.ExportPath, firstLevelOrgs, true);

            // STEP 3: Export any solutions from other Orgs which need to be installed either side of the Internal Sync.
            // (This is necessary if a combination of all the level 1 solutions isn't sufficient to rebuild the entire app).
            IEnumerable<OrganizationConfig> restoreBeforeSync = orgConfigs.Where(q => q.RepairRestore == RepairRestoreOption.BeforeSync);
            ExportSolutions(envConfig.ExportPath, restoreBeforeSync, true);
            IEnumerable<OrganizationConfig> restoreAfterSync = orgConfigs.Where(q => q.RepairRestore == RepairRestoreOption.AfterSync);
            ExportSolutions(envConfig.ExportPath, restoreAfterSync, true);

            // STEP 4: Ensure Attribute Max Lengths and Database Fields Sizes are correct
            foreach (string solutionPath in unmanagedSolutions.Values.Union(managedSolutions.Values))
            {
                SolutionHelper.FixAttributeLengthDiscrepancies(solutionPath);
            }

            // STEP 5: Delete Organizations
            new DeleteOrgsAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 6: Re-create Organizations
            new CreateOrgsAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 7: Setup Admin Users
            new SetupAdminUsersAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 8: Setup Publisher and Solutions
            new SetupSolutionsAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 9: Install Pre-Requisite Solutions
            new InstallPreReqsAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 10: Import solutions into first-level Orgs (note: this handles dependencies, e.g. if Org B requires Org A managed, Org A is installed managed first)
            OutputDivider("Import Solutions");
            ImportSolutionsInternalSync(firstLevelOrgs, envConfig.ImportSolutionsAsync, true, config.PostDeployment);

            // STEP 11: Import other solutions required to be in place before the Internal Sync
            ImportSolutionsInternalSync(restoreBeforeSync, envConfig.ImportSolutionsAsync, true, config.PostDeployment);

            // STEP 12: Internal Sync
            if (orgConfigs.Any(q => q.InternalSync != null && q.InternalSync.ImportPass > 0))
            {
                new InternalSyncAction().PerformAction(config, environmentName, orgUniqueNames);
            }

            // STEP 13: Import other solutions required after the Internal Sync
            ImportSolutionsInternalSync(restoreAfterSync, envConfig.ImportSolutionsAsync, true, config.PostDeployment);

            // STEP 14: Import Data
            if (config.Repair != null && config.Repair.ImportData)
            {
                new StaticDataImportAction().PerformAction(config, environmentName, orgUniqueNames);
            }
        }
    }
}