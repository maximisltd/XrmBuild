using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using System.Collections.Generic;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions.SourceControl
{
    public enum CrmComponentType { None, Solution, Report, PluginAssembly }

    public abstract class BaseSourceControlProvider
    {
        protected SourceControlConfig scConfig;

        public BaseSourceControlProvider(SourceControlConfig scConfig)
        {
            this.scConfig = scConfig;
        }

        public abstract void CheckInFiles(CheckInOptions options);

        public abstract string DownloadAllFiles(DownloadOptions options);

        public abstract string DownloadPluginAssembly(PluginAssemblyConfig pluginConfig, ref List<string> downloadedDependencies);

        public abstract string GetPluginAssemblyLocalPath(PluginAssemblyConfig pluginConfig);

        public abstract string GetSolutionLocalPath(string solutionName);
    }
}