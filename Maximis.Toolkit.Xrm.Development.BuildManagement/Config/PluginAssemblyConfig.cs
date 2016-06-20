using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class PluginAssemblyConfig
    {
        public string AssemblyName { get; set; }

        [XmlArrayItem("Path")]
        public List<string> DependencyPaths { get; set; }

        public ILMergeConfig ILMerge { get; set; }

        public string LocationName { get; set; }
        public string ProjectName { get; set; }

        public string ProjectPath { get; set; }
    }
}