using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.EntitySerialisation;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
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

            EntityDeserialiser ser = new EntityDeserialiser();

            // Get the Organizations which are configured for Import or Export
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.ImportData || q.ExportData.Any());

            // Loop through Organizations
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Data Import: " + orgConfig.FriendlyName);
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    // Loop through all XML files
                    foreach (string xmlFile in Directory.EnumerateFiles(fullImportDir))
                    {
                        // Load XML
                        Trace.WriteLine(string.Empty);
                        Trace.WriteLine(string.Format("Importing file '{0}'", xmlFile));
                        XmlDocument xd = new XmlDocument();
                        xd.Load(xmlFile);

                        // Loop through all "<ent>" elements
                        foreach (XmlElement el in xd.DocumentElement.SelectNodes("ent"))
                        {
                            Entity newEntity = null;
                            try
                            {
                                // Deserialise into Entity
                                newEntity = ser.DeserialiseEntity(orgService, el.OuterXml);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine(string.Format("ERROR :: '{0}'", ex.Message));
                                continue;
                            }
                            try
                            {
                                // Try to Create record
                                Trace.WriteLine(string.Format("Attempting CREATE of '{0}' with id '{1:N}'", newEntity.LogicalName, newEntity.Id));
                                orgService.Create(newEntity);
                            }
                            catch (Exception exCreate)
                            {
                                Trace.WriteLine(string.Format("ERROR :: '{0}'", exCreate.Message));
                                try
                                {
                                    // Try to Update record
                                    Trace.WriteLine(string.Format("Create failed - attempting UPDATE of '{0}' with id '{1:N}'", newEntity.LogicalName, newEntity.Id));
                                    orgService.Update(newEntity);
                                }
                                catch (Exception exUpdate)
                                {
                                    Trace.WriteLine(string.Format("ERROR :: '{0}'", exUpdate.Message));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}