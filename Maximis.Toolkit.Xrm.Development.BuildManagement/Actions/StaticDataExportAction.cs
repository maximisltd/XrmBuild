using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.EntitySerialisation;
using Maximis.Toolkit.Xrm.ImportExport;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Metadata;
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
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.ExportData);

            int index = 1;

            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Data Export: " + orgConfig.FriendlyName);

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    CrmContext context = new CrmContext(orgService, orgConfig.UniqueName);

                    // Loop through each "DataImportExport" item
                    foreach (DataConfig dataConfig in config.DataImportExport)
                    {
                        Trace.WriteLine(string.Format("Exporting entity records of type '{0}'", dataConfig.EntityName));
                        try
                        {
                            // Ensure Export Directory Exists
                            string fullExportDir = Path.Combine(envConfig.ExportPath, "Data");
                            if (!Directory.Exists(fullExportDir)) Directory.CreateDirectory(fullExportDir);

                            // Construct Query
                            ColumnSet colSet = new ColumnSet();
                            if (dataConfig.Attributes != null && dataConfig.Attributes.Count > 0)
                                colSet.Columns.AddRange(dataConfig.Attributes);
                            else
                                colSet.AllColumns = true;
                            QueryExpression query = new QueryExpression(dataConfig.EntityName) { ColumnSet = colSet };
                            foreach (ConditionExpression condition in dataConfig.Conditions) query.Criteria.AddCondition(condition);
                            foreach (OrderExpression order in dataConfig.Orders) query.Orders.Add(order);

                            // Set up ExportOptions object
                            ExportOptions options = new ExportOptions { QueryExpression = query };
                            if (dataConfig.RelatedEntities != null && dataConfig.RelatedEntities.Count > 0)
                            {
                                List<string> scopeEntities = new List<string>();
                                scopeEntities.Add(dataConfig.EntityName);
                                scopeEntities.AddRange(dataConfig.RelatedEntities);

                                EntityMetadata entityMeta = MetadataHelper.GetEntityMetadata(orgService, dataConfig.EntityName, EntityFilters.Relationships);
                                IEnumerable<ManyToManyRelationshipMetadata> manyManyRels =
                                    entityMeta.ManyToManyRelationships.Where(q => scopeEntities.Contains(q.Entity1LogicalName) && scopeEntities.Contains(q.Entity2LogicalName));

                                options.Scopes = new List<EntitySerialiserScope>();
                                foreach (string scopeEntity in scopeEntities)
                                {
                                    options.Scopes.Add(new EntitySerialiserScope
                                    {
                                        EntityType = scopeEntity,
                                        Columns = config.DataImportExport.Single(q => q.EntityName == scopeEntity).Attributes.ToArray(),
                                        Relationships = manyManyRels.Where(q => q.Entity1LogicalName == scopeEntity || q.Entity2LogicalName == scopeEntity).Select(q => q.SchemaName).ToArray()
                                    });
                                }
                            }

                            // Create XML file
                            string xmlPath = Path.Combine(fullExportDir, string.Format("{0:000}_{1}.xml", index++, dataConfig.EntityName));

                            XmlImportExportHelper.ExportSingleFile(context, xmlPath, options);
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