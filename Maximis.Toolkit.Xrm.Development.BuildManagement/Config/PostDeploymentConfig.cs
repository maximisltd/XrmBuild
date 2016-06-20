using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class PostDeploymentConfig
    {
        public bool CheckUnmanagedEntityPrivileges { get; set; }
        public bool DeleteDefaultSubject { get; set; }

        [XmlArrayItem("OptionSet")]
        public List<SystemOptionSetValueConfig> DeleteSystemOptionSetValues { get; set; }

        public bool DisableSystemBPFs { get; set; }
        public bool DisableSystemForms { get; set; }
    }

    public class SystemOptionSetValueConfig
    {
        [XmlAttribute]
        public string AttributeName { get; set; }

        [XmlAttribute]
        public string EntityName { get; set; }

        [XmlArrayItem("OptionText")]
        public List<string> UnwantedOptions { get; set; }
    }
}