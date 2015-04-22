using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class CleanUpFolder
    {
        [XmlAttribute]
        public int FileAgeDays { get; set; }

        [XmlAttribute]
        public string Path { get; set; }
    }
}