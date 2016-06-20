using Maximis.Toolkit.Xrm.Development.Customisation;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class FormConfig
    {
        [XmlAttribute("EntityName")]
        public string EntityName { get; set; }

        [XmlAttribute("FormType")]
        public FormType FormType { get; set; }
    }
}