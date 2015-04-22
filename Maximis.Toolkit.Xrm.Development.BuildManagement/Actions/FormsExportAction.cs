using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Xrm.Sdk.Client;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class FormsExportAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Find Organization Configs with ExportForms set
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.ExportForms.Any());

            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Forms Export: " + orgConfig.FriendlyName);

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    foreach (FormConfig formConfig in orgConfig.ExportForms)
                    {
                        Trace.WriteLine(string.Format("Exporting forms for Entity '{0}'", formConfig.EntityName));
                        string exportPath = Path.Combine(envConfig.ExportPath, "Forms");
                        FormHelper.ExportFormXml(orgService, exportPath, formConfig.EntityName, formConfig.FormType);
                    }
                }
            }
        }
    }
}