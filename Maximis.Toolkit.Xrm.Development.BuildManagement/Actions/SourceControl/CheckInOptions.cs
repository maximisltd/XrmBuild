namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions.SourceControl
{
    public class CheckInOptions
    {
        public CrmComponentType CrmComponentType { get; set; }
        public string Description { get; set; }
        public string EnvironmentName { get; set; }
        public string LocalPath { get; set; }
    }
}