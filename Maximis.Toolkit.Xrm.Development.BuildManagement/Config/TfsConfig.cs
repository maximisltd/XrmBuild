using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class TfsConfig
    {
        public CheckInNote[] CheckInNotes { get; set; }

        [XmlArrayItem("PluginAssembly")]
        public List<TfsPluginConfig> PluginAssemblies { get; set; }

        public string ProjectCollectionUri { get; set; }

        public string SolutionPackagerPath { get; set; }

        public string SolutionRoot { get; set; }
    }
}