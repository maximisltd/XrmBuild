using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class RepairStepConfig
    {
        [XmlAttribute]
        public bool Managed { get; set; }

        [XmlAttribute]
        public int Order { get; set; }

        [XmlAttribute]
        public string Organization { get; set; }

        [XmlAttribute]
        public string SolutionToImport { get; set; }
    }
}