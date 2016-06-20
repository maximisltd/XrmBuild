using Maximis.Toolkit.Csv;
using Maximis.Toolkit.IO;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Actions.SourceControl;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Xml;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public enum SolutionImportMode { Managed, ManagedOverwrite, Unmanaged }

    public abstract class BaseImportExportAction : BaseAction
    {
        protected Dictionary<string, string> managedSolutions = new Dictionary<string, string>();
        protected Dictionary<string, string> securityRoles = new Dictionary<string, string>();
        protected Dictionary<string, string> solutionVersions = new Dictionary<string, string>();
        protected Dictionary<string, string> unmanagedSolutions = new Dictionary<string, string>();

        private Guid systemSolutionId = Guid.Empty;

        protected void CheckSolutionsIntoSourceControl(XrmBuildConfig config, EnvironmentConfig envConfig, IEnumerable<OrganizationConfig> orgConfigs)
        {
            if (!orgConfigs.Any(q => q.SourceControl != null && !string.IsNullOrEmpty(q.SourceControl.SolutionLocation))) return;

            // Loop through each Organization and check in each one separately
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                // Get Source Control Provider
                BaseSourceControlProvider srcControl = GetSourceControlProvider(config.SourceControl, orgConfig.SourceControl.SolutionLocation);

                // Make sure Solution can be checked in
                if (!unmanagedSolutions.ContainsKey(orgConfig.UniqueName)) continue;
                Trace.WriteLine(string.Format("Checking solution '{0}' into source control...", orgConfig.SolutionName));

                // Use SDK Solution Packager to extract solution
                string extractPath = srcControl.GetSolutionLocalPath(orgConfig.SolutionName);
                Process p = new Process();
                p.StartInfo.FileName = config.SourceControl.SolutionPackagerPath;
                p.StartInfo.Arguments = string.Format("/action:Extract /zipfile:\"{0}\" /folder:\"{1}\" /allowDelete:Yes /clobber /nologo", unmanagedSolutions[orgConfig.UniqueName], extractPath);
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();

                // Copy solution zip into Source Controlled directory
                string zipCopyPath = Path.Combine(extractPath, "Zip", string.Format("{0}_unmanaged.zip", orgConfig.SolutionName));
                FileHelper.EnsureDirectoryExists(zipCopyPath, PathType.File);
                File.Copy(unmanagedSolutions[orgConfig.UniqueName], zipCopyPath);

                // Check In Files
                srcControl.CheckInFiles(new CheckInOptions { Description = string.Format("Solution '{0}' Version '{1}'", orgConfig.SolutionName, GetSolutionVersion(orgConfig.UniqueName)) });
            }
        }

        protected void ExportSolutions(string exportFolderPath, IEnumerable<OrganizationConfig> orgConfigs, bool exportManaged, bool incrementVersion = true, string targetVersion = null)
        {
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                // Skip if  solutions have already been exported
                bool unmanagedExported = unmanagedSolutions.ContainsKey(orgConfig.UniqueName);
                bool managedExported = managedSolutions.ContainsKey(orgConfig.UniqueName);
                if (unmanagedExported && (managedExported || !exportManaged)) continue;

                string solutionExportPath = Path.Combine(exportFolderPath, "Solutions", orgConfig.UniqueName);
                if (!Directory.Exists(solutionExportPath)) Directory.CreateDirectory(solutionExportPath);

                string securityRoleExportPath = Path.Combine(exportFolderPath, "SecurityRoles", orgConfig.UniqueName);

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    Trace.WriteLine(string.Format("[Export Solution '{0}' from Organization '{1}']", orgConfig.SolutionName, orgConfig.UniqueName));

                    // Update version and publish
                    if (incrementVersion) solutionVersions.Add(orgConfig.UniqueName, SolutionHelper.SetSolutionVersion(orgService, orgConfig.SolutionName));
                    SolutionHelper.PublishAllCustomisations(orgService);

                    // Export Security Roles
                    Dictionary<string, string> solutionRoles = SolutionHelper.ExportSecurityRoles(orgService, orgConfig.SolutionName, securityRoleExportPath);
                    if (solutionRoles != null)
                    {
                        foreach (string key in solutionRoles.Keys) securityRoles[string.Format("{0}_{1}", orgConfig.UniqueName, key)] = solutionRoles[key];
                    }

                    // Export Unmanaged
                    if (!unmanagedSolutions.ContainsKey(orgConfig.UniqueName))
                        unmanagedSolutions.Add(orgConfig.UniqueName, SolutionHelper.ExportSolution(orgService, orgConfig.SolutionName, false, solutionExportPath, true, targetVersion));

                    // Export Managed
                    if (exportManaged && !managedSolutions.ContainsKey(orgConfig.UniqueName))
                        managedSolutions.Add(orgConfig.UniqueName, SolutionHelper.ExportSolution(orgService, orgConfig.SolutionName, true, solutionExportPath, true, targetVersion));
                }
                Trace.WriteLine(string.Empty);
            }
        }

        protected string GetSolutionVersion(string orgConfigName)
        {
            return solutionVersions.ContainsKey(orgConfigName) ? solutionVersions[orgConfigName] : "[Unknown]";
        }

        protected bool ImportSolutions(OrganizationServiceProxy orgService, string importOrgUniqueName, SolutionImportMode importMode, bool importAsync, params string[] solutionsToImport)
        {
            bool solutionsImported = false;
            foreach (string toImport in solutionsToImport)
            {
                if (importMode == SolutionImportMode.Unmanaged)
                {
                    if (!unmanagedSolutions.ContainsKey(toImport)) continue;

                    Trace.WriteLine(string.Format("[Import Unmanaged Solution '{0}' into Organization '{1}']", solutionNames[toImport], importOrgUniqueName));
                    SolutionHelper.ImportSolution(orgService, unmanagedSolutions[toImport], importAsync: importAsync, publishDedupeRules: true);
                    solutionsImported = true;
                }
                else
                {
                    if (!managedSolutions.ContainsKey(toImport)) continue;

                    Trace.WriteLine(string.Format("[Import Managed Solution '{0}' into Organization '{1}']", solutionNames[toImport], importOrgUniqueName));
                    SolutionHelper.ImportSolution(orgService, managedSolutions[toImport], importAsync: importAsync, overwriteUnmanaged: importMode == SolutionImportMode.ManagedOverwrite, publishDedupeRules: true);
                    solutionsImported = true;
                }
            }
            return solutionsImported;
        }

        protected void ImportSolutionsInternalSync(IEnumerable<OrganizationConfig> orgConfigs, bool importAsync, bool importSelf, PostDeploymentConfig pdConfig)
        {
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    bool needsPublish = false;

                    // First install managed solutions (dependencies)
                    if (orgConfig.InternalSync != null && orgConfig.InternalSync.ImportManaged != null)
                    {
                        ImportSolutions(orgService, orgConfig.UniqueName, orgConfig.InternalSync.OverwriteUnmanaged ? SolutionImportMode.ManagedOverwrite : SolutionImportMode.Managed, importAsync, orgConfig.InternalSync.ImportManaged.ToArray());
                    }

                    // Then install unmanaged solutions
                    if (orgConfig.InternalSync != null && orgConfig.InternalSync.ImportUnmanaged != null)
                    {
                        foreach (string toImport in orgConfig.InternalSync.ImportUnmanaged)
                        {
                            needsPublish |= ImportSolutions(orgService, orgConfig.UniqueName, SolutionImportMode.Unmanaged, importAsync, toImport);

                            if (orgConfig.InternalSync.MergeUnmanaged)
                            {
                                // Ensure everything in imported unmanaged solution is in global unmanaged solution
                                SolutionHelper.SyncSolutionComponents(orgService, solutionNames[toImport], orgConfig.SolutionName);

                                // Delete imported solution (now we have copied everything from it)
                                SolutionHelper.DeleteSolution(orgService, solutionNames[toImport]);
                            }
                        }
                    }

                    // Install (optionally) own solution - used by Rebuild action
                    if (importSelf && unmanagedSolutions.ContainsKey(orgConfig.UniqueName))
                    {
                        Trace.WriteLine(string.Format("[Import Unmanaged Solution '{0}' into Organization '{1}']", orgConfig.SolutionName, orgConfig.UniqueName));
                        SolutionHelper.ImportSolution(orgService, unmanagedSolutions[orgConfig.UniqueName], importAsync: importAsync, overwriteUnmanaged: true, publishDedupeRules: true);
                        needsPublish = true;
                    }

                    // Create Dictionary of Security Role file paths for solution
                    Dictionary<string, string> solutionRoles = securityRoles.Where(q => q.Key.StartsWith(orgConfig.UniqueName + "_")).ToDictionary(k => k.Key.RightOfLast('_'), v => v.Value);

                    // Merge in Privileges that are defined in dependency solutions
                    List<string> toMergePaths = new List<string>();
                    foreach (string roleName in solutionRoles.Keys)
                    {
                        foreach (string dependency in orgConfig.InternalSync.ImportManaged.Union(orgConfig.InternalSync.ImportUnmanaged))
                        {
                            string key = string.Format("{0}_{1}", dependency, roleName);
                            if (securityRoles.ContainsKey(key))
                            {
                                toMergePaths.Add(securityRoles[key]);
                            }
                        }
                        MergePrivileges(solutionRoles[roleName], toMergePaths);
                    }

                    // Import Modified Security Roles
                    needsPublish |= SolutionHelper.ImportSecurityRoles(orgService, orgConfig.UniqueName, solutionRoles);

                    // Publish Customisations
                    if (needsPublish) SolutionHelper.PublishAllCustomisations(orgService);

                    // Perform Post Deployment Steps
                    PerformPostDeploymentSteps(orgService, pdConfig);
                }
            }
        }

        protected void PerformPostDeploymentSteps(IOrganizationService orgService, PostDeploymentConfig pdConfig)
        {
            CrmContext context = new CrmContext(orgService);
            bool needsPublish = false;

            if (pdConfig.DisableSystemBPFs)
            {
                // Disable OOB Process Flows
                OutputDivider("Disabling OOB Business Process Flows");

                QueryExpression query = new QueryExpression("workflow") { ColumnSet = new ColumnSet("name") };
                query.Criteria.AddCondition("category", ConditionOperator.Equal, 4);
                query.Criteria.AddCondition("type", ConditionOperator.Equal, 1);
                query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 1);
                query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, GetSystemSolutionId(orgService));
                foreach (Entity bpfEntity in orgService.RetrieveMultiple(query).Entities)
                {
                    Trace.WriteLine(string.Format("Disabling Process Flow '{0}'", bpfEntity.GetAttributeValue<string>("name")));
                    UpdateHelper.SetEntityState(orgService, bpfEntity.ToEntityReference(), 0, 1);
                }
            }

            if (pdConfig.DisableSystemForms)
            {
                OutputDivider("Disabling OOB Forms for Entities with replacement Forms");

                // Find Unmanaged Main Forms
                QueryExpression query = new QueryExpression("systemform") { ColumnSet = new ColumnSet("objecttypecode") };
                query.Criteria.AddCondition("type", ConditionOperator.Equal, (int)FormType.Main);
                query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
                query.Criteria.AddCondition("formactivationstate", ConditionOperator.Equal, 1);
                EntityCollection customForms = orgService.RetrieveMultiple(query);

                // Get list of Entities
                object[] entities = customForms.Entities.Select(q => q["objecttypecode"]).Distinct().OrderBy(q => q).ToArray();
                if (entities.Any())
                {
                    // Disable Managed forms for those Entities
                    query = new QueryExpression("systemform") { ColumnSet = new ColumnSet("name", "objecttypecode") };
                    query.Criteria.AddCondition("type", ConditionOperator.Equal, (int)FormType.Main);
                    query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, true);
                    query.Criteria.AddCondition("formactivationstate", ConditionOperator.Equal, 1);
                    query.Criteria.AddCondition("objecttypecode", ConditionOperator.In, entities);
                    foreach (Entity systemForm in orgService.RetrieveMultiple(query).Entities)
                    {
                        Trace.WriteLine(string.Format("Disabling {0} Form '{1}'", systemForm.GetAttributeValue<string>("objecttypecode"), systemForm.GetAttributeValue<string>("name")));
                        FormHelper.DeactivateForm(orgService, systemForm.Id);
                        needsPublish = true;
                    }
                }
            }

            if (pdConfig.DeleteSystemOptionSetValues.Any())
            {
                OutputDivider("Deleting unwanted OOB Option Set Values");
                foreach (SystemOptionSetValueConfig osValue in pdConfig.DeleteSystemOptionSetValues)
                {
                    EnumAttributeMetadata attrMeta = MetadataHelper.GetAttributeMetadata(context, osValue.EntityName, osValue.AttributeName) as EnumAttributeMetadata;
                    if (attrMeta == null) continue;
                    foreach (string unwantedOption in osValue.UnwantedOptions)
                    {
                        OptionMetadata optionToRemove = attrMeta.OptionSet.Options.SingleOrDefault(q => q.Label.UserLocalizedLabel.Label == unwantedOption);
                        if (optionToRemove != null)
                        {
                            Trace.WriteLine(string.Format("Removing '{0}' from {1}/{2}", unwantedOption, osValue.EntityName, osValue.AttributeName));
                            orgService.Execute(new DeleteOptionValueRequest { AttributeLogicalName = attrMeta.LogicalName, EntityLogicalName = attrMeta.EntityLogicalName, Value = optionToRemove.Value.Value });
                            needsPublish = true;
                        }
                    }
                }
            }

            if (pdConfig.CheckUnmanagedEntityPrivileges)
            {
                OutputDivider("Identifying inaccessible Unmanaged Entities");

                // Get All Privileges
                Dictionary<Guid, string> allPrivileges = SecurityHelper.RetrieveAllPrivileges(orgService);

                // Get Unmanaged Roles
                QueryExpression query = new QueryExpression("role") { ColumnSet = new ColumnSet("name") };
                query.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
                IEnumerable<string> unmanagedRoles = orgService.RetrieveMultiple(query).Entities.Select(q => q.GetAttributeValue<string>("name"));

                // Get Unmanaged Entities
                EntityMetadata[] allMeta = MetadataHelper.GetAllEntityMetadata(orgService, EntityFilters.Entity);
                Dictionary<string, bool> entityAccessibility = allMeta.Where(q => !q.IsManaged.Value && !q.IsIntersect.Value && !q.IsActivity.Value).Select(q => q.LogicalName).OrderBy(q => q).ToDictionary(k => k, v => false);

                // Loop through Unmanaged Roles
                foreach (string role in unmanagedRoles)
                {
                    // Get Privileges for those Roles
                    RolePrivilege[] rolePrivileges = SecurityHelper.GetRolePrivileges(orgService, role);

                    // Loop through Unmanaged Entities
                    foreach (string entityName in entityAccessibility.Keys.ToArray())
                    {
                        // Update if role has a privilege on that Entity
                        entityAccessibility[entityName] |= rolePrivileges.Any(q => allPrivileges[q.PrivilegeId].EndsWith(entityName));
                    }
                }

                IEnumerable<string> inaccessibleEntities = entityAccessibility.Where(q => !q.Value).Select(q => q.Key);
                if (inaccessibleEntities.Any())
                {
                    Trace.WriteLine("*** WARNING *** The security privileges for these Entities may not be set:");
                    Trace.WriteLine(string.Join(", ", inaccessibleEntities.ToArray()));
                }
            }

            if (pdConfig.DeleteDefaultSubject)
            {
                OutputDivider("Deleting Default Subject");
                QueryExpression query = new QueryExpression("subject");
                query.Criteria.AddCondition("title", ConditionOperator.Equal, "Default Subject");
                EntityCollection ec = orgService.RetrieveMultiple(query);
                if (ec.Entities.Count == 0)
                {
                    Trace.WriteLine("Default Subject not found.");
                }
                else
                {
                    Trace.WriteLine(string.Format("Deleting {0} Subject records.", ec.Entities.Count));
                }
            }

            if (needsPublish) SolutionHelper.PublishAllCustomisations(orgService);
        }

        protected bool SyncRibbons(OrganizationConfig orgConfig, SyncRibbonConfig syncRibbonConfig)
        {
            SolutionDefinition sd = new SolutionDefinition
            {
                PublisherId = syncRibbonConfig.PublisherId,
                UniqueName = "ribbonsync",
                FriendlyName = "Ribbon Sync"
            };

            bool solutionsCreated = false;

            using (OrganizationServiceProxy syncFromSvc = ServiceHelper.GetOrganizationServiceProxy(syncRibbonConfig.OrgConfig.CrmContext))
            using (OrganizationServiceProxy syncToSvc = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
            {
                Trace.WriteLine("Retrieving Metadata...");
                EntityMetadata[] fromMeta = MetadataHelper.GetAllEntityMetadata(syncFromSvc, EntityFilters.Entity);
                EntityMetadata[] toMeta = MetadataHelper.GetAllEntityMetadata(syncToSvc, EntityFilters.Entity);

                foreach (Entity sc in SolutionHelper.RetrieveSolutionComponents(syncToSvc, orgConfig.SolutionName, ComponentType.Entity).Entities)
                {
                    // Get the Entity Metadata in both Organizations
                    EntityMetadata toEntity = toMeta.Single(q => q.MetadataId.Value == sc.GetAttributeValue<Guid>("objectid"));
                    EntityMetadata fromEntity = fromMeta.Single(q => q.LogicalName == toEntity.LogicalName);

                    // Get the Entity Ribbon from both Organizations
                    XmlDocument fromRibbon = RibbonHelper.RetrieveEntityRibbonXml(syncFromSvc, fromEntity.LogicalName);
                    XmlDocument toRibbon = RibbonHelper.RetrieveEntityRibbonXml(syncToSvc, toEntity.LogicalName);

                    // If they are different
                    Trace.Write(string.Format("Comparing Entity '{0}'...", toEntity.LogicalName));
                    if (fromRibbon.OuterXml != toRibbon.OuterXml)
                    {
                        Trace.WriteLine("DIFFERENT");

                        // Clear or Create solutions
                        if (solutionsCreated)
                        {
                            SolutionHelper.RemoveAllSolutionComponents(syncFromSvc, sd.UniqueName);
                            SolutionHelper.RemoveAllSolutionComponents(syncToSvc, sd.UniqueName);
                        }
                        else
                        {
                            SolutionHelper.CreateSolution(syncFromSvc, sd);
                            SolutionHelper.CreateSolution(syncToSvc, sd);
                            solutionsCreated = true;
                        }

                        // Add Entity to each Solution
                        SolutionHelper.AddSolutionComponent(syncFromSvc, sd.UniqueName, fromEntity.MetadataId.Value, ComponentType.Entity);
                        SolutionHelper.AddSolutionComponent(syncToSvc, sd.UniqueName, toEntity.MetadataId.Value, ComponentType.Entity);

                        // Export Solutions from each Organization
                        string fromSolution = SolutionHelper.ExportSolution(syncFromSvc, sd.UniqueName, false, Path.Combine(syncRibbonConfig.ExportPath, "RibbonSync\\SyncFrom"));
                        string toSolution = SolutionHelper.ExportSolution(syncToSvc, sd.UniqueName, false, Path.Combine(syncRibbonConfig.ExportPath, "RibbonSync\\SyncTo"));

                        // Read the Ribbon from the "From" ("correct") Solution
                        string ribbonXml = null;
                        using (Package package = Package.Open(fromSolution))
                        {
                            PackagePart part = package.GetPart(new Uri("/customizations.xml", UriKind.Relative));
                            XmlDocument xd = new XmlDocument();
                            xd.Load(part.GetStream());
                            ribbonXml = xd.SelectSingleNode("//RibbonDiffXml").InnerXml;
                        }

                        // Update the "To" Solution with the same XML
                        using (Package package = Package.Open(toSolution))
                        {
                            PackagePart part = package.GetPart(new Uri("/customizations.xml", UriKind.Relative));
                            XmlDocument xd = new XmlDocument();
                            xd.Load(part.GetStream());
                            xd.SelectSingleNode("//RibbonDiffXml").InnerXml = ribbonXml;
                            xd.Save(part.GetStream());
                        }

                        // Import the Solution
                        SolutionHelper.ImportSolution(syncToSvc, toSolution, syncRibbonConfig.ImportSolutionsAsync);
                    }
                    else
                    {
                        Trace.WriteLine("SAME");
                    }
                }

                if (solutionsCreated)
                {
                    SolutionHelper.DeleteSolution(syncFromSvc, sd.UniqueName);
                    SolutionHelper.DeleteSolution(syncToSvc, sd.UniqueName);
                    return true;
                }
                return false;
            }
        }

        private int GetDepthValue(string depthName)
        {
            PrivilegeDepth depth = (PrivilegeDepth)Enum.Parse(typeof(PrivilegeDepth), depthName);
            return (int)depth;
        }

        private Guid GetSystemSolutionId(IOrganizationService orgService)
        {
            if (systemSolutionId == Guid.Empty)
            {
                QueryExpression query = new QueryExpression("solution");
                query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, "System");
                systemSolutionId = QueryHelper.RetrieveSingleEntity(orgService, query).Id;
            }
            return systemSolutionId;
        }

        private void MergePrivileges(string toUpdatePath, List<string> toMergePaths)
        {
            if (toMergePaths.Count == 0) return;

            Dictionary<string, int> merged = new Dictionary<string, int>();
            string headerRow = null;

            // Read "toUpdate" file
            using (CsvReader csvReader = new CsvReader(toUpdatePath))
            {
                headerRow = string.Join(",", csvReader.HeadingIndex.Keys.ToArray());
                while (!csvReader.EndOfData)
                {
                    string[] data = csvReader.ReadFields();
                    merged.Add(data[0], GetDepthValue(data[1]));
                }
            }

            // Add in privileges from each "toMerge" file
            foreach (string toMergePath in toMergePaths)
            {
                using (CsvReader csvReader = new CsvReader(toMergePath))
                {
                    while (!csvReader.EndOfData)
                    {
                        string[] data = csvReader.ReadFields();
                        string privName = data[0];
                        int depth = GetDepthValue(data[1]);
                        if (merged.ContainsKey(privName))
                        {
                            if (depth > merged[privName]) merged[privName] = depth;
                        }
                        else
                        {
                            merged.Add(privName, depth);
                        }
                    }
                }
            }

            // Write the combination of privileges back to the "toUpdate" file
            using (StreamWriter sw = File.CreateText(toUpdatePath))
            {
                sw.WriteLine(headerRow);
                foreach (KeyValuePair<string, int> privilege in merged.OrderBy(q => q.Key))
                {
                    sw.WriteLine(string.Format("{0},{1}", privilege.Key, (PrivilegeDepth)privilege.Value));
                }
            }
        }
    }
}