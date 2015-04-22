using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public abstract class BaseAction
    {
        protected Dictionary<string, string> solutionNames = new Dictionary<string, string>();

        public void PerformAction(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            foreach (EnvironmentConfig envConfig in config.Environments)
            {
                foreach (OrganizationConfig orgConfig in envConfig.Organizations)
                {
                    orgConfig.CrmContext = new CrmContext
                    {
                        AuthenticationProviderType = envConfig.AuthenticationProviderType,
                        Domain = envConfig.Domain,
                        HostName = envConfig.HostName,
                        Organization = orgConfig.UniqueName,
                        Password = envConfig.Password,
                        Port = envConfig.Port,
                        Secure = envConfig.Secure,
                        TimeoutSeconds = 3600,
                        Username = envConfig.Username,
                        Version = envConfig.Version
                    };

                    solutionNames.Add(orgConfig.UniqueName, orgConfig.SolutionName);
                }
            }

#if DEBUG
            PerformActionWorker(config, environmentName, orgUniqueNames);
#else

            try
            {
                PerformActionWorker(config, environmentName, orgUniqueNames);
            }
            catch (Exception ex)
            {
                TraceException(ex, 1);
                Trace.WriteLine(string.Empty);
                Trace.WriteLine(string.Empty);
                throw;
            }
#endif
        }

        protected IEnumerable<OrganizationConfig> GetOrgConfigs(EnvironmentConfig envConfig, params string[] orgUniqueNames)
        {
            if (orgUniqueNames.Any())
            {
                IEnumerable<OrganizationConfig> orgConfigs = envConfig.Organizations.Where(q => orgUniqueNames.Contains(q.UniqueName));

                string[] unknownOrgs = orgUniqueNames.Except(orgConfigs.Select(q => q.UniqueName)).ToArray();
                if (unknownOrgs.Length > 0)
                {
                    throw new ArgumentException(string.Format("Unknown Organization(s): {0}", string.Join(", ", unknownOrgs)));
                }

                return orgConfigs;
            }
            else
                return envConfig.Organizations;
        }

        protected void OutputDivider(string title)
        {
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(title.ToUpper());
            Trace.WriteLine("----------------------------------------------------------------------");
        }

        protected abstract void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames);

        private void TraceException(Exception ex, int level)
        {
            if (level >= 10) return;

            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Empty);
            Trace.WriteLine(string.Format("EXCEPTION - DEPTH LEVEL {0}", level));
            Trace.WriteLine(ex.ToString());
            if (ex.InnerException != null)
            {
                TraceException(ex.InnerException, level + 1);
            }
        }
    }
}