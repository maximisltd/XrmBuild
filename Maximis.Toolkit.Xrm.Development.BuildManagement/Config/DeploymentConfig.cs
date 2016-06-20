using Maximis.Toolkit.Xrm.Development.BuildManagement.Actions;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class DeploymentConfig
    {
        private bool publishDedupeRules = true;

        [XmlArrayItem("UniqueName")]
        public List<string> DeployFromDisk { get; set; }

        [XmlArrayItem("UniqueName")]
        public List<string> DeployFromOrgs { get; set; }

        [XmlAttribute]
        public SolutionImportMode Mode { get; set; }

        [XmlAttribute]
        public bool PublishDedupeRules
        {
            get { return publishDedupeRules; }
            set { publishDedupeRules = value; }
        }
    }
}