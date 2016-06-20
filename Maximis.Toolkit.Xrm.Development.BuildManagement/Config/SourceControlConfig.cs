using System.Collections.Generic;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class SourceControlConfig
    {
        public GitConfig Git { get; set; }

        public string SolutionPackagerPath { get; set; }

        public TfsConfig Tfs { get; set; }
        public List<PluginAssemblyConfig> PluginAssemblies { get; set; }
    }
}