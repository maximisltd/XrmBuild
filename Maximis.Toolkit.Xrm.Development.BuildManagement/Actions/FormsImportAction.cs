using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class FormsImportAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment Config
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Ensure Form Import Folder Exists
            string fullImportDir = Path.Combine(envConfig.ExportPath, "Forms");
            if (!Directory.Exists(fullImportDir))
            {
                OutputDivider("Form Import");
                Trace.WriteLine(string.Format("Form import directory '{0}' not found.", fullImportDir));
                return;
            }

            // Get the Organizations which are configured for Import
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.ImportForms);

            // Loop through Organizations
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Forms Import: " + orgConfig.FriendlyName);
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    FormHelper.ImportFormXml(orgService, fullImportDir);
                    SolutionHelper.PublishAllCustomisations(orgService);
                }
            }
        }
    }
}