using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class ExportSolutionsAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            OutputDivider("Export Solutions");
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            ExportSolutions(envConfig.ExportPath, GetOrgConfigs(envConfig, orgUniqueNames));
        }
    }
}