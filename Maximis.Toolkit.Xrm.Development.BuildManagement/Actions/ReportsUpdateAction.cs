using Maximis.Toolkit.IO;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Actions.SourceControl;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class ReportsUpdateAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Organizations which are configured to sync Reports with TFS
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.SourceControl != null && !string.IsNullOrEmpty(q.SourceControl.ReportsLocation));

            // Loop through Organizations
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Update Reports from TFS: " + orgConfig.FriendlyName);

                // Get Source Control Provider
                BaseSourceControlProvider srcControl = GetSourceControlProvider(config.SourceControl, orgConfig.SourceControl.ReportsLocation);

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    // Get existing Reports in solution
                    EntityCollection existingReports = ReportsHelper.GetSolutionReports(orgService, orgConfig.SolutionName);

                    // Download latest files from Source Control
                    string localReportsPath = srcControl.DownloadAllFiles(null);

                    // Loop through files
                    foreach (string rdlFile in Directory.EnumerateFiles(localReportsPath, "*.rdl"))
                    {
                        // Get Name of RDL File
                        string fileName = rdlFile.RightOfLast('/');

                        // Get Name of Report (Filename minus extension)
                        string reportName = fileName.LeftOfFirst('.');

                        // Get Report RDL
                        string reportContent = FileHelper.ReadFromFile(rdlFile);

                        // Add or update Report
                        ReportsHelper.AddOrUpdateReport(orgService, orgConfig.SolutionName, fileName, reportName, reportContent, existingReports);
                    }
                }
            }
        }
    }
}