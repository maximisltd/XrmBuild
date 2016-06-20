using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions.SourceControl
{
    public class GitProvider : BaseSourceControlProvider
    {
        private CredentialsHandler credHandler;
        private GitRepositoryConfig repoConfig;
        private Signature signature;

        public GitProvider(SourceControlConfig scConfig, GitRepositoryConfig repoConfig)
            : base(scConfig)
        {
            this.repoConfig = repoConfig;

            // Set up Credentials Handler
            UsernamePasswordCredentials upc = new UsernamePasswordCredentials() { Username = repoConfig.Username, Password = repoConfig.Password };
            this.credHandler = new CredentialsHandler((url, user, cred) => upc);

            // Ensure the Repo is cloned and local directory exists before any operations take place
            GetRepository();
        }

        public override void CheckInFiles(CheckInOptions options)
        {
            using (Repository repo = GetRepository())
            {
                // If there is something to commit...
                RepositoryStatus status = repo.RetrieveStatus();
                if (status.IsDirty)
                {
                    // Build a list of changed files
                    List<string> filePaths = new List<string>();
                    filePaths.AddRange(status.Added.Select(q => q.FilePath));
                    filePaths.AddRange(status.Missing.Select(q => q.FilePath));
                    filePaths.AddRange(status.Modified.Select(q => q.FilePath));
                    filePaths.AddRange(status.Removed.Select(q => q.FilePath));
                    filePaths.AddRange(status.RenamedInIndex.Select(q => q.FilePath));
                    filePaths.AddRange(status.RenamedInWorkDir.Select(q => q.FilePath));
                    filePaths.AddRange(status.Untracked.Select(q => q.FilePath));

                    // Stage
                    repo.Stage(filePaths);

                    // Commit
                    Signature sig = new Signature(repoConfig.Username, repoConfig.EmailAddress, DateTimeOffset.Now);
                    repo.Commit(options.Description, sig, sig,
                       new CommitOptions { AllowEmptyCommit = true, PrettifyMessage = true });

                    // Push
                    repo.Network.Push(repo.Network.Remotes["origin"], repo.Branches[repoConfig.BranchName].CanonicalName, new PushOptions { CredentialsProvider = credHandler });
                }
            }
        }

        public override string DownloadAllFiles(DownloadOptions options)
        {
            // Pull to get latest version of files
            // Unlike TFS Provider, options.LocalPath and options.RemotePath are ignored, as all we can do with Git is pull the whole repo.

            using (Repository repo = GetRepository())
            {
                repo.Network.Pull(new Signature(repoConfig.Username, repoConfig.EmailAddress, DateTimeOffset.Now),
                    new PullOptions { FetchOptions = new FetchOptions { CredentialsProvider = credHandler } });
            }

            // Return path for compatibility with generic calling code that also supports TFS.
            return options == null ? null : options.LocalPath;
        }

        public override string DownloadPluginAssembly(PluginAssemblyConfig pluginConfig, ref List<string> downloadedDependencies)
        {
            // Unlike TFS, we can only download the whole Repo, not just the code for the plugin assembly

            DownloadAllFiles(null);
            return Path.Combine(repoConfig.LocalPath, pluginConfig.ProjectPath);
        }

        public override string GetPluginAssemblyLocalPath(PluginAssemblyConfig pluginConfig)
        {
            return Path.Combine(repoConfig.LocalPath, pluginConfig.ProjectPath);
        }

        public override string GetSolutionLocalPath(string solutionName)
        {
            return Path.Combine(repoConfig.LocalPath, "Solutions", solutionName);
        }

        private Repository GetRepository()
        {
            Repository repo = null;

            try
            {
                // Try to get Repository
                repo = new Repository(repoConfig.LocalPath);
            }
            catch (RepositoryNotFoundException)
            {
                // If Repository not found, clone, then attempt to return again
                if (Directory.Exists(repoConfig.LocalPath)) Directory.Delete(repoConfig.LocalPath, true);
                Repository.Clone(repoConfig.Url, repoConfig.LocalPath, new CloneOptions { CredentialsProvider = credHandler, BranchName = repoConfig.BranchName });
                repo = new Repository(repoConfig.LocalPath);
            }

            return repo;
        }
    }
}