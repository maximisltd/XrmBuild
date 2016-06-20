using System;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public class SyncRibbonConfig
    {
        public string ExportPath { get; set; }
        public bool ImportSolutionsAsync { get; set; }
        public OrganizationConfig OrgConfig { get; set; }
        public Guid PublisherId { get; set; }
    }
}