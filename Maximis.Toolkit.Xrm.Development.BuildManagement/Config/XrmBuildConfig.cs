using Maximis.Toolkit.Xrm.Development.Customisation;
using System.Collections.Generic;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class XrmBuildConfig
    {
        public List<EnvironmentConfig> Environments { get; set; }

        public PublisherDefinition Publisher { get; set; }

        public TfsConfig TfsConfig { get; set; }
    }
}