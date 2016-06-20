using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class StaticDataResetAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Organizations which are configured for Import or Export
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.ImportData || q.ExportData);

            // Loop through Organizations
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Delete Imported Data: " + orgConfig.FriendlyName);
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    // Loop through all Entities and delete if possible
                    foreach (string entityType in config.DataImportExport.Select(q => q.EntityName))
                    {
                        Trace.WriteLine(string.Format("Deleting '{0}'", entityType));
                        QueryExpression query = new QueryExpression(entityType);
                        try
                        {
                            foreach (Entity entity in orgService.RetrieveMultiple(query).Entities)
                            {
                                try
                                {
                                    orgService.Delete(entity.LogicalName, entity.Id);
                                }
                                catch (Exception ex)
                                {
                                    Trace.WriteLine(string.Format("ERROR :: '{0}'", ex.Message));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine(string.Format("ERROR :: '{0}'", ex.Message));
                        }
                    }
                }
            }
        }
    }
}