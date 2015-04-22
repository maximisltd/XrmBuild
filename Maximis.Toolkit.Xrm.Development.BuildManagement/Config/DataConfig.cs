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

        public string EntityName { get; set; }
    }
}