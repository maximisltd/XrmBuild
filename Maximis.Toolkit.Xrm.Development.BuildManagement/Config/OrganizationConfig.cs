using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public enum RepairRestoreOption { None, BeforeSync, AfterSync }

    public class OrganizationConfig
    {
        private string friendlyName;
        private string solutionFriendlyName;
        private string solutionName;

        [XmlIgnore()]
        public CrmConnectionInfo CrmContext { get; set; }

        public DeploymentConfig Deployment { get; set; }

        public bool EnableAuditing { get; set; }

        [XmlArrayItem("UniqueName")]
        public List<string> EnvSyncIn { get; set; }

        [XmlArrayItem("UniqueName")]
        public List<string> EnvSyncOut { get; set; }

        public bool ExportData { get; set; }

        public List<FormConfig> ExportForms { get; set; }

        public string FriendlyName
        {
            get
            {
                if (string.IsNullOrEmpty(friendlyName)) friendlyName = this.FriendlyName;
                return friendlyName;
            }
            set { friendlyName = value; }
        }

        public bool ImportData { get; set; }

        public bool ImportForms { get; set; }

        public InternalSyncConfig InternalSync { get; set; }

        [XmlArrayItem("FullPath")]
        public List<string> PreRequisiteSolutions { get; set; }

        public RepairRestoreOption RepairRestore { get; set; }

        public List<SecurityRoleConfig> SecurityRoles { get; set; }

        public string SolutionFriendlyName
        {
            get
            {
                if (string.IsNullOrEmpty(solutionFriendlyName)) solutionFriendlyName = this.FriendlyName;
                return solutionFriendlyName;
            }
            set { solutionFriendlyName = value; }
        }

        public string SolutionName
        {
            get
            {
                if (string.IsNullOrEmpty(solutionName)) solutionName = this.UniqueName;
                return solutionName;
            }
            set { solutionName = value; }
        }

        public OrgSourceControlConfig SourceControl { get; set; }

        [XmlAttribute]
        public string SyncRibbonsWith { get; set; }

        [XmlAttribute]
        public string UniqueName { get; set; }
    }
}