using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class GitConfig
    {
        [XmlArrayItem("Repository")]
        public List<GitRepositoryConfig> Repositories { get; set; }
    }
}