using Maximis.Toolkit.Xrm.Development.Customisation;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class XrmBuildConfig
    {
        [XmlArrayItem("EntityName")]
        public List<string> AuditedEntities { get; set; }

        public List<DataConfig> DataImportExport { get; set; }

        public List<EnvironmentConfig> Environments { get; set; }

        public PostDeploymentConfig PostDeployment { get; set; }
        public PublisherDefinition Publisher { get; set; }
        public RepairConfig Repair { get; set; }
        public SourceControlConfig SourceControl { get; set; }
    }
}