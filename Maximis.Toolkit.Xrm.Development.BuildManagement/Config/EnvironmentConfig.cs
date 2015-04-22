using Microsoft.Xrm.Sdk.Client;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class EnvironmentConfig
    {
        public List<UserConfig> AdminUsers { get; set; }

        public AuthenticationProviderType AuthenticationProviderType { get; set; }

        public List<CleanUpFolder> CleanUp { get; set; }

        public string Domain { get; set; }

        public string ExportPath { get; set; }

        public string HostName { get; set; }

        public bool ImportSolutionsAsync { get; set; }

        public List<OrganizationConfig> Organizations { get; set; }

        public string Password { get; set; }

        public int Port { get; set; }

        public bool Secure { get; set; }

        public string SqlServerName { get; set; }

        public string SrsUrl { get; set; }

        [XmlAttribute]
        public string UniqueName { get; set; }

        public string Username { get; set; }

        public string Version { get; set; }
    }
}