using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class SecurityRoleConfig
    {
        public List<SecurityRoleConfig> ChildRoles { get; set; }

        [XmlAttribute]
        public string Name { get; set; }
    }
}