using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.EntitySerialisation;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class StaticDataImportAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // Get the Environment Config
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);

            // Ensure Data Import Folder Exists
            string fullImportDir = Path.Combine(envConfig.ExportPath, "Data");
            if (!Directory.Exists(fullImportDir))
            {
                OutputDivider("Data Import");
                Trace.WriteLine(string.Format("Data import directory '{0}' not found.", fullImportDir));
                return;
            }

            // Get the Organizations which are configured for Import
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.ImportData);

            // Loop through Organizations
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Data Import: " + orgConfig.FriendlyName);
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    // Create Entity Deserialiser
                    EntityDeserialiser ser = new EntityDeserialiser(new CrmContext(orgService, orgConfig.UniqueName));

                    // Loop through all XML files
                    foreach (string xmlFile in Directory.EnumerateFiles(fullImportDir))
                    {
                        // Load XML
                        Trace.WriteLine(string.Empty);
                        Trace.WriteLine(string.Format("Importing file '{0}'", xmlFile));
                        XmlDocument xd = new XmlDocument();
                        xd.Load(xmlFile);

                        // Loop through all "<ent>" elements
                        foreach (XmlElement ent in xd.DocumentElement.SelectNodes("ent"))
                        {
                            Entity entity = DeserialiseAndCreateOrUpdate(ser, orgService, ent, config.DataImportExport);

                            foreach (XmlElement rel in ent.SelectNodes("rel"))
                            {
                                foreach (XmlElement relEnt in rel.SelectNodes("ent"))
                                {
                                    Entity related = DeserialiseAndCreateOrUpdate(ser, orgService, relEnt, config.DataImportExport);
                                    try
                                    {
                                        UpdateHelper.RelateEntitiesLazy(orgService, entity.ToEntityReference(), related.ToEntityReference());
                                    }
                                    catch (Exception ex)
                                    {
                                        Trace.WriteLine("ERROR: " + ex.Message);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private Entity DeserialiseAndCreateOrUpdate(EntityDeserialiser ser, OrganizationServiceProxy orgService, XmlElement ent, List<DataConfig> dataConfigs)
        {
            Entity entity = null;
            try
            {
                // Deserialise into Entity
                entity = ser.DeserialiseEntity(ent.OuterXml);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("ERROR :: '{0}'", ex.Message));
                return null;
            }

            // Find an Existing record using an attribute other than Id
            // Used to retrieve Calendar records by PrimaryUserId for update
            Entity customMatch = null;
            DataConfig dataConfig = dataConfigs.SingleOrDefault(q => q.EntityName == entity.LogicalName);
            if (dataConfig != null && dataConfig.ExistingMatch != null && dataConfig.ExistingMatch.Any())
            {
                QueryExpression query = new QueryExpression(entity.LogicalName);
                foreach (string attrName in dataConfig.ExistingMatch)
                {
                    if (!entity.Contains(attrName)) continue;
                    object val = entity[attrName];
                    if (val is EntityReference) val = ((EntityReference)val).Id;
                    else if (val is OptionSetValue) val = ((OptionSetValue)val).Value;
                    query.Criteria.AddCondition(attrName, ConditionOperator.Equal, val);
                }
                customMatch = QueryHelper.RetrieveSingleEntity(orgService, query);

                if (customMatch != null)
                {
                    ser.IdMappings[entity.Id] = customMatch.Id;
                    entity.Id = customMatch.Id;
                };
            }

            try
            {
                Trace.WriteLine(string.Format("Attempting Create or Update of '{0}' with id '{1:N}'", entity.LogicalName, entity.Id));
                UpdateHelper.SmartUpdate(orgService, entity, customMatch);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(string.Format("ERROR :: '{0}'", ex.Message));
            }

            return entity;
        }
    }
}