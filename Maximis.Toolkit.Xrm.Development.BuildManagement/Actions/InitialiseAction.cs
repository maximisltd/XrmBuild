using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class InitialiseAction : BaseAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            // STEP 1: Delete Organizations
            new DeleteOrgsAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 2: Create Organizations
            new CreateOrgsAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 3: Setup Admin Users
            new SetupAdminUsersAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 4: Setup Publisher and Solutions
            new SetupSolutionsAction().PerformAction(config, environmentName, orgUniqueNames);

            // STEP 5: Install Pre-Requisite Solutions
            new InstallPreReqsAction().PerformAction(config, environmentName, orgUniqueNames);
        }
    }
}