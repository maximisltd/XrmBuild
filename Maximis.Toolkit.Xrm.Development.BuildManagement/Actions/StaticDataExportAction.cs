using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.ImportExport;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class StaticDataExportAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Find Organization Configs with ExportData set
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.ExportData.Any());

            int index = 1;

            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Data Export: " + orgConfig.FriendlyName);

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    // Loop through each "ExportData" item
                    foreach (DataConfig dataConfig in orgConfig.ExportData)
                    {
                        Trace.WriteLine(string.Format("Exporting entity records of type '{0}'", dataConfig.EntityName));
                        try
                        {
                            // Ensure Export Directory Exists
                            string fullExportDir = Path.Combine(envConfig.ExportPath, "Data");
                            if (!Directory.Exists(fullExportDir)) Directory.CreateDirectory(fullExportDir);

                            // Construct Query
                            QueryExpression query = new QueryExpression(dataConfig.EntityName) { ColumnSet = new ColumnSet(dataConfig.Attributes.ToArray()) };
                            foreach (ConditionExpression condition in dataConfig.Conditions) query.Criteria.AddCondition(condition);

                            // Create XML file
                            string xmlPath = Path.Combine(fullExportDir, string.Format("{0:000}_{1}.xml", index++, dataConfig.EntityName));
                            XmlImportExportHelper.ExportSingleFile(orgService, xmlPath, new ExportOptions { QueryExpression = query });
                        }

                        catch (Exception ex)
                        {
                            Trace.WriteLine("ERROR :: " + ex.Message);
                        }
                    }
                }
            }
        }
    }
}