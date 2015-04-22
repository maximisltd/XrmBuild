using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class FormConfig
    {
        [XmlAttribute("EntityName")]
        public string EntityName { get; set; }

        [XmlAttribute("FormType")]
        public int FormType { get; set; }
    }
}