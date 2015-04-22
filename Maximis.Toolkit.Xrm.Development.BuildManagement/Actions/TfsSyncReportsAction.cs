using Maximis.Toolkit.Tfs;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class TfsSyncReportsAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Organizations which are configured to sync Reports with TFS
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.TfsSync != null && !string.IsNullOrEmpty(q.TfsSync.ReportsPath));

            // Loop through Organizations
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Update Reports from TFS: " + orgConfig.FriendlyName);
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    // Get existing Reports in solution
                    EntityCollection existingReports = ReportsHelper.GetSolutionReports(orgService, orgConfig.SolutionName);

                    // Get contents of TFS folder
                    VersionControlServer versionControl = VersionControlHelper.GetVersionControlServer(config.TfsConfig.ProjectCollectionUri);
                    ItemSet tfsItems = versionControl.GetItems(orgConfig.TfsSync.ReportsPath, RecursionType.Full);

                    // Loop through TFS Items
                    foreach (Item tfsItem in tfsItems.Items.Where(q => q.ServerItem.EndsWith(".rdl")))
                    {
                        // Get Name of RDL File
                        string fileName = tfsItem.ServerItem;
                        fileName = fileName.Substring(fileName.LastIndexOf("/") + 1);

                        // Get Name of Report (Filename minus extension)
                        string reportName = fileName.Substring(0, fileName.LastIndexOf(".rdl"));

                        // Get Report RDL
                        string reportContent = null;
                        using (StreamReader sr = new StreamReader(tfsItem.DownloadFile()))
                        {
                            reportContent = sr.ReadToEnd();
                        }

                        // Add or update Report
                        ReportsHelper.AddOrUpdateReport(orgService, orgConfig.SolutionName, fileName, reportName, reportContent, existingReports);
                    }
                }
            }
        }
    }
}