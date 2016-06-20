using ILMerging;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Actions.SourceControl;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Maximis.Toolkit.Xrm.Development.Customisation;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class UpdatePluginAssembliesAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            OutputDivider("Update Plugins: Downloading from Source Control");

            // Get the Organizations which are configured to sync Plugins with Source Control
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            IEnumerable<OrganizationConfig> orgConfigs = GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.SourceControl != null && q.SourceControl.PluginAssemblies.Any());

            Dictionary<string, string> pluginContent = new Dictionary<string, string>();
            List<string> downloadedDependencies = new List<string>();
            foreach (string pluginName in orgConfigs.SelectMany(q => q.SourceControl.PluginAssemblies).Distinct())
            {
                PluginAssemblyConfig pluginConfig = config.SourceControl.PluginAssemblies.SingleOrDefault(q => q.AssemblyName == pluginName);
                if (pluginConfig == null) continue;

                if (pluginContent.ContainsKey(pluginConfig.AssemblyName)) continue;

                BaseSourceControlProvider srcControl = GetSourceControlProvider(config.SourceControl, pluginConfig.LocationName);
                string localProjectPath = srcControl.DownloadPluginAssembly(pluginConfig, ref downloadedDependencies);

                // Build Visual Studio Project
                string localProjectFullPath = Path.Combine(localProjectPath, pluginConfig.ProjectName);
                Dictionary<string, string> props = new Dictionary<string, string>();
                props.Add("Configuration", "Release");
                props.Add("PreBuildEvent", string.Empty);
                props.Add("PostBuildEvent", string.Empty);

                BuildRequestData buildRequest = new BuildRequestData(projectFullPath: localProjectFullPath,
                    globalProperties: props, toolsVersion: "4.0", targetsToBuild: new string[] { "Build" }, hostServices: null);
                BuildParameters bp = new BuildParameters(new ProjectCollection()) { Loggers = new ILogger[] { new ConsoleLogger(LoggerVerbosity.Detailed) } };
                BuildResult buildResult = BuildManager.DefaultBuildManager.Build(bp, buildRequest);

                if (buildResult.OverallResult != BuildResultCode.Success)
                {
                    continue;
                }

                // Make sure Assembly exists
                string buildOutputPath = Path.Combine(localProjectPath, "bin\\Release");
                string assemblyPath = Path.Combine(buildOutputPath, string.Format("{0}.dll", pluginConfig.AssemblyName));
                if (!File.Exists(assemblyPath))
                {
                    Trace.WriteLine(string.Format("ERROR :: Cannot locate Assembly '{0}'", assemblyPath));
                    continue;
                }

                // Use ILMerge if necessary
                if (pluginConfig.ILMerge != null)
                {
                    List<string> inputAssemblies = new List<string>();
                    inputAssemblies.Add(assemblyPath);
                    foreach (string assemblyName in pluginConfig.ILMerge.MergeAssemblies)
                    {
                        inputAssemblies.Add(Path.Combine(buildOutputPath, assemblyName));
                    }

                    FileInfo mergedAssemblyFile = new FileInfo(Path.Combine(buildOutputPath, "Merged", string.Format("{0}.dll", pluginConfig.AssemblyName)));
                    if (!mergedAssemblyFile.Directory.Exists) mergedAssemblyFile.Directory.Create();

                    ILMerge ilMerge = new ILMerge
                    {
                        KeyFile = Path.Combine(localProjectPath, pluginConfig.ILMerge.KeyFile),
                        OutputFile = mergedAssemblyFile.FullName
                    };

                    ilMerge.SetInputAssemblies(inputAssemblies.ToArray());

                    try
                    {
                        ilMerge.Merge();
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(string.Format("ERROR :: '{0}'", ex.Message));
                        continue;
                    }
                    assemblyPath = mergedAssemblyFile.FullName;
                }

                // Add Assembly Content to Dictionary
                pluginContent.Add(pluginConfig.AssemblyName, Convert.ToBase64String(File.ReadAllBytes(assemblyPath)));
            }

            // Loop through each Organization and egister plugins
            foreach (OrganizationConfig orgConfig in orgConfigs)
            {
                OutputDivider("Update Plugins: " + orgConfig.FriendlyName);

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    foreach (string pluginName in orgConfig.SourceControl.PluginAssemblies)
                    {
                        if (pluginContent.ContainsKey(pluginName))
                        {
                            PluginHelper.RegisterAssemblyFromBase64String(orgService, pluginName, pluginContent[pluginName]);
                            PluginHelper.AddAssemblyAndStepsToSolution(orgService, pluginName, orgConfig.SolutionName);
                        }
                    }
                }
            }
        }
    }
}