using Maximis.Toolkit.Csv;
using Maximis.Toolkit.Tfs;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public abstract class BaseImportExportAction : BaseAction
    {
        protected Dictionary<string, string> managedSolutions = new Dictionary<string, string>();
        protected Dictionary<string, string> securityRoles = new Dictionary<string, string>();
        protected Dictionary<string, string> solutionVersions = new Dictionary<string, string>();
        protected Dictionary<string, string> unmanagedSolutions = new Dictionary<string, string>();

        protected void CheckSolutionsIntoSourceControl(XrmBuildConfig config, string environmentName, IEnumerable<OrganizationConfig> orgConfigs)
        {
            if (config.TfsConfig == null || string.IsNullOrEmpty(config.TfsConfig.ProjectCollectionUri) || string.IsNullOrEmpty(config.TfsConfig.SolutionRoot)) return;
            if (!orgConfigs.Any(q => q.TfsSync != null && q.TfsSync.CheckInSolution)) return;

            // Get Local Path where Solutions are extracted. Delete and re-create it.
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            string extractPath = Path.Combine(envConfig.ExportPath, "TFS\\Solutions");
            if (Directory.Exists(extractPath))
            {
                try
                {
                    Directory.Delete(extractPath, true);
                    Directory.CreateDirectory(extractPath);
                }
                catch { }
            }

            // Get TFS Workspace
            VersionControlServer vcs = VersionControlHelper.GetVersionControlServer(config.TfsConfig.ProjectCollectionUri);
            Workspace ws = VersionControlHelper.GetOrCreateWorkspace(vcs, string.Format("SolutionManagement_{0}_{1}", environmentName, Environment.MachineName), config.TfsConfig.SolutionRoot, extractPath);

            // Set up Check In Note object
            CheckinNote checkInNote = null;
            if (config.TfsConfig.CheckInNotes != null && config.TfsConfig.CheckInNotes.Any())
            {
                checkInNote = new CheckinNote(config.TfsConfig.CheckInNotes.Select(q => new CheckinNoteFieldValue(q.Key, q.Value)).ToArray());
            }

            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                // Make sure Solution is supposed to be checked in
                if (!unmanagedSolutions.ContainsKey(orgConfig.UniqueName) || orgConfig.TfsSync == null || !orgConfig.TfsSync.CheckInSolution) continue;

                Trace.WriteLine(string.Format("Checking solution '{0}' into source control...", orgConfig.SolutionName));

                // Define location to extract solution
                string outputPath = Path.Combine(extractPath, orgConfig.UniqueName);

                // Use SDK Solution Packager to extract solution
                Process p = new Process();
                p.StartInfo.FileName = config.TfsConfig.SolutionPackagerPath;
                p.StartInfo.Arguments = string.Format("/action:Extract /zipfile:\"{0}\" /folder:\"{1}\" /allowDelete:Yes /clobber /nologo", unmanagedSolutions[orgConfig.UniqueName], outputPath);
                p.StartInfo.UseShellExecute = false;
                p.Start();
                p.WaitForExit();

                // Get list of items currently in Workspace
                string tfsPath = string.Format("{0}/{1}", config.TfsConfig.SolutionRoot, orgConfig.UniqueName);
                IEnumerable<string> itemsInWorkspace = vcs.GetItems(tfsPath, VersionSpec.Latest, RecursionType.Full, DeletedState.NonDeleted, ItemType.File).Items.Select(q => ws.GetLocalItemForServerItem(q.ServerItem));

                // Get list of items currently in local folder
                IEnumerable<string> itemsInLocalFolder = Directory.EnumerateFiles(outputPath, "*", SearchOption.AllDirectories);

                // Identify Adds, Edits and Deletes
                string[] deletedItems = itemsInWorkspace.Except(itemsInLocalFolder).ToArray();
                if (deletedItems.Length > 0) ws.PendDelete(deletedItems);
                string[] addedItems = itemsInLocalFolder.Except(itemsInWorkspace).ToArray();
                if (addedItems.Length > 0) ws.PendAdd(addedItems);
                string[] updatedItems = itemsInLocalFolder.Except(addedItems).ToArray();
                if (updatedItems.Length > 0) ws.PendEdit(updatedItems);

                // Commit Pending Changes
                PendingChange[] pendingChanges = ws.GetPendingChanges();
                if (pendingChanges.Length > 0)
                {
                    Trace.Write("Checking in...");
                    string description = string.Format("Solution '{0}' Version '{1}'", orgConfig.SolutionName, GetSolutionVersion(orgConfig.UniqueName));
                    int changeSet = ws.CheckIn(new WorkspaceCheckInParameters(pendingChanges, description) { CheckinNotes = checkInNote });
                    Trace.WriteLine(string.Format("Done: changeset '{0}'.", changeSet));
                }
                else
                {
                    Trace.WriteLine("Nothing to check in!");
                }
            }
        }

        protected void ExportSolutions(string exportFolderPath, IEnumerable<OrganizationConfig> orgConfigs, bool incrementVersion = true, string targetVersion = null, bool exportUnmanaged = true, bool exportManaged = true)
        {
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                // Skip if  solutions have already been exported
                bool unmanagedExported = unmanagedSolutions.ContainsKey(orgConfig.UniqueName);
                bool managedExported = managedSolutions.ContainsKey(orgConfig.UniqueName);
                if (unmanagedExported && (managedExported || !exportManaged)) continue;
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
                    if (exportUnmanaged && !unmanagedSolutions.ContainsKey(orgConfig.UniqueName))
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

        protected void ImportManagedSolutions(OrganizationServiceProxy orgService, IEnumerable<string> solutionsToImport, string importOrgUniqueName, bool overwriteUnmanaged, bool importAsync)
        {
            foreach (string toImport in solutionsToImport)
            {
                if (!managedSolutions.ContainsKey(toImport)) continue;

                Trace.WriteLine(string.Format("[Import Managed Solution '{0}' into Organization '{1}']", solutionNames[toImport], importOrgUniqueName));
                SolutionHelper.ImportSolution(orgService, managedSolutions[toImport], importAsync: importAsync, overwriteUnmanaged: overwriteUnmanaged, publishDedupeRules: true);
            }
        }

        protected void ImportSolutionsInternalSync(IEnumerable<OrganizationConfig> orgConfigs, bool importAsync, bool importSelf = false)
        {
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    bool needsPublish = false;

                    // First install managed solutions (dependencies)
                    if (orgConfig.InternalSync != null && orgConfig.InternalSync.ImportManaged != null)
                    {
                        ImportManagedSolutions(orgService, orgConfig.InternalSync.ImportManaged, orgConfig.UniqueName, orgConfig.InternalSync.OverwriteUnmanaged, importAsync);
                    }

                    // Then install unmanaged solutions
                    if (orgConfig.InternalSync != null && orgConfig.InternalSync.ImportUnmanaged != null)
                    {
                        foreach (string toImport in orgConfig.InternalSync.ImportUnmanaged)
                        {
                            if (!unmanagedSolutions.ContainsKey(toImport)) continue;

                            Trace.WriteLine(string.Format("[Import Unmanaged Solution '{0}' into Organization '{1}']", solutionNames[toImport], orgConfig.UniqueName));
                            SolutionHelper.ImportSolution(orgService, unmanagedSolutions[toImport], importAsync: importAsync, overwriteUnmanaged: true, publishDedupeRules: true);

                            if (orgConfig.InternalSync.MergeUnmanaged)
                            {
                                // Ensure everything in imported unmanaged solution is in global unmanaged solution
                                SolutionHelper.SyncSolutionComponents(orgService, solutionNames[toImport], orgConfig.SolutionName);
                                needsPublish = true;

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
                }
            }
        }

        private int GetDepthValue(string depthName)
        {
            PrivilegeDepth depth = (PrivilegeDepth)Enum.Parse(typeof(PrivilegeDepth), depthName);
            return (int)depth;
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