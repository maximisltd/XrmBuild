using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class DataConfig
    {
        [XmlArrayItem("Attribute")]
        public List<string> Attributes { get; set; }

        public List<ConditionExpression> Conditions { get; set; }

        [XmlAttribute]
        public string EntityName { get; set; }

        [XmlArrayItem("Attribute")]
        public List<string> ExistingMatch { get; set; }

        public List<OrderExpression> Orders { get; set; }

        [XmlArrayItem("EntityName")]
        public List<string> RelatedEntities { get; set; }
    }
}