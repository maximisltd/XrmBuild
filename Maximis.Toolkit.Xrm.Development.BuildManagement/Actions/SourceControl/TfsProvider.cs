using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions.SourceControl
{
    public class TfsProvider : BaseSourceControlProvider
    {
        private TfsProjectCollectionConfig projectCollConfig;
        private VersionControlServer vcs;

        public TfsProvider(SourceControlConfig scConfig, TfsProjectCollectionConfig projectCollConfig)
            : base(scConfig)
        {
            this.projectCollConfig = projectCollConfig;
            TfsTeamProjectCollection projectColl = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(projectCollConfig.ProjectCollectionUri));
            this.vcs = projectColl.GetService<VersionControlServer>();
        }

        public override void CheckInFiles(CheckInOptions options)
        {
            // Get TFS Workspace
            string workspaceName = string.Format("SolutionManagement_{0}_{1}", options.EnvironmentName, Environment.MachineName);
            Workspace ws = vcs.TryGetWorkspace(options.LocalPath);

            // If Workspace exists but does not have correct name, delete
            if (ws != null && ws.Name != workspaceName)
            {
                ws.Delete();
                ws = null;
            }

            // Get Remote Path
            string remotePath = GetRemotePath(options.CrmComponentType);

            // If Workspace does not exist, create it
            if (ws == null)
            {
                ws = vcs.CreateWorkspace(new CreateWorkspaceParameters(workspaceName)
                {
                    Folders = new WorkingFolder[] { new WorkingFolder(remotePath, options.LocalPath) }
                });
            }

            // Set up Check In Note object
            CheckinNote checkInNote = null;
            if (scConfig.Tfs.CheckInNotes != null && scConfig.Tfs.CheckInNotes.Any())
            {
                checkInNote = new CheckinNote(scConfig.Tfs.CheckInNotes.Select(q => new CheckinNoteFieldValue(q.Key, q.Value)).ToArray());
            }

            // Get list of items currently in Workspace
            IEnumerable<string> itemsInWorkspace = vcs.GetItems(remotePath, VersionSpec.Latest, RecursionType.Full, DeletedState.NonDeleted, ItemType.File).Items.Select(q => ws.GetLocalItemForServerItem(q.ServerItem));

            // Get list of items currently in local folder
            IEnumerable<string> itemsInLocalFolder = Directory.EnumerateFiles(projectCollConfig.LocalPath, "*", SearchOption.AllDirectories);

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
                int changeSet = ws.CheckIn(new WorkspaceCheckInParameters(pendingChanges, options.Description) { CheckinNotes = checkInNote });
                Trace.WriteLine(string.Format("Done: changeset '{0}'.", changeSet));
            }
            else
            {
                Trace.WriteLine("Nothing to check in!");
            }
        }

        public override string DownloadAllFiles(DownloadOptions options)
        {
            string rootFolder = null;
            foreach (Item item in vcs.GetItems(GetRemotePath(options.CrmComponentType), RecursionType.Full).Items)
            {
                string itemLocalPath = Path.Combine(options.LocalPath, item.ServerItem.Substring(2).Replace("/", "\\"));
                switch (item.ItemType)
                {
                    case ItemType.Folder:
                        if (Directory.Exists(itemLocalPath)) Directory.Delete(itemLocalPath, true);
                        Directory.CreateDirectory(itemLocalPath);
                        if (string.IsNullOrEmpty(rootFolder)) rootFolder = itemLocalPath;
                        break;

                    case ItemType.File:
                        Trace.WriteLine(string.Format("Downloading file '{0}'", itemLocalPath));
                        item.DownloadFile(itemLocalPath);
                        break;
                }
            }
            return rootFolder;
        }

        public override string DownloadPluginAssembly(PluginAssemblyConfig pluginConfig, ref List<string> downloadedDependencies)
        {
            // Download Visual Studio Project and related files from TFS
            string localPath = GetPluginAssemblyLocalPath(pluginConfig);
            foreach (string dependencyRemotePath in pluginConfig.DependencyPaths)
            {
                if (!downloadedDependencies.Contains(dependencyRemotePath))
                {
                    downloadedDependencies.Add(dependencyRemotePath);
                    DownloadAllFiles(new DownloadOptions { RemotePath = dependencyRemotePath });
                }
            }
            return DownloadAllFiles(new DownloadOptions { RemotePath = pluginConfig.ProjectPath });
        }

        public override string GetPluginAssemblyLocalPath(PluginAssemblyConfig pluginConfig)
        {
            throw new NotImplementedException();
        }

        public override string GetSolutionLocalPath(string solutionName)
        {
            throw new NotImplementedException();
        }

        private string GetRemotePath(CrmComponentType crmComponentType)
        {
            switch (crmComponentType)
            {
                case CrmComponentType.None:
                    throw new Exception("CrmComponentType enum must be set.");

                case CrmComponentType.PluginAssembly:
                    return projectCollConfig.PluginAssembliesPath;

                case CrmComponentType.Report:
                    return projectCollConfig.ReportsPath;

                case CrmComponentType.Solution:
                    return projectCollConfig.SolutionsPath;
            }
            return null;
        }
    }
}