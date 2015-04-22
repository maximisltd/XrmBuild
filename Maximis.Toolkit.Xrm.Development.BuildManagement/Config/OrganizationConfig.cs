using System.Collections.Generic;
using System.Xml.Serialization;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Config
{
    public enum RepairRestoreOption { None, BeforeSync, AfterSync }

    public class OrganizationConfig
    {
        private string solutionName;

        [XmlIgnore()]
        public CrmContext CrmContext { get; set; }

        public DeploymentConfig Deployment { get; set; }

        [XmlArrayItem("UniqueName")]
        public List<string> EnvSyncIn { get; set; }

        [XmlArrayItem("UniqueName")]
        public List<string> EnvSyncOut { get; set; }

        public List<DataConfig> ExportData { get; set; }

        public List<FormConfig> ExportForms { get; set; }

        public string FriendlyName { get; set; }

        public bool ImportData { get; set; }

        public bool ImportForms { get; set; }

        public InternalSyncConfig InternalSync { get; set; }

        public RepairRestoreOption RepairRestore { get; set; }

        public List<SecurityRoleConfig> SecurityRoles { get; set; }

        public string SolutionName
        {
            get
            {
                if (string.IsNullOrEmpty(solutionName)) solutionName = this.UniqueName;
                return solutionName;
            }
            set { solutionName = value; }
        }

        public TfsSyncConfig TfsSync { get; set; }

        [XmlAttribute]
        public string UniqueName { get; set; }
    }
}