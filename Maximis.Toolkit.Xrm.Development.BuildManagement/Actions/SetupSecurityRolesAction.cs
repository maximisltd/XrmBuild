using Maximis.Toolkit.Logging;
using Maximis.Toolkit.Xrm.Development.BuildManagement.Config;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Maximis.Toolkit.Xrm.Development.BuildManagement.Actions
{
    public class SetupSecurityRolesAction : BaseImportExportAction
    {
        protected override void PerformActionWorker(XrmBuildConfig config, string environmentName, params string[] orgUniqueNames)
        {
            EnvironmentConfig envConfig = config.Environments.SingleOrDefault(q => q.UniqueName == environmentName);
            foreach (OrganizationConfig orgConfig in GetOrgConfigs(envConfig, orgUniqueNames).Where(q => q.SecurityRoles.Any()))
            {
                OutputDivider(string.Format("Ensure Security Role Consistency - {0}", orgConfig.FriendlyName));

                using (OrganizationServiceProxy orgService = ServiceHelper.GetOrganizationServiceProxy(orgConfig.CrmContext))
                {
                    QueryExpression buQuery = new QueryExpression("businessunit");
                    buQuery.Criteria.AddCondition("parentbusinessunitid", ConditionOperator.Null);
                    Entity rootBU = QueryHelper.RetrieveSingleEntity(orgService, buQuery);

                    TidyRoles(orgService, orgConfig.SecurityRoles, rootBU);
                }
            }
        }

        private RolePrivilege[] MergePrivileges(RolePrivilege[] privileges, RolePrivilege[] parentPrivileges)
        {
            Dictionary<Guid, RolePrivilege> result = new Dictionary<Guid, RolePrivilege>();
            foreach (RolePrivilege privilege in privileges)
            {
                result.Add(privilege.PrivilegeId, new RolePrivilege((int)privilege.Depth, privilege.PrivilegeId));
            }

            if (parentPrivileges != null)
            {
                foreach (RolePrivilege parentPrivilege in parentPrivileges)
                {
                    if (result.ContainsKey(parentPrivilege.PrivilegeId))
                    {
                        int childDepth = (int)result[parentPrivilege.PrivilegeId].Depth;
                        int parentDepth = (int)parentPrivilege.Depth;
                        if (parentDepth > childDepth) result[parentPrivilege.PrivilegeId].Depth = (PrivilegeDepth)parentDepth;
                    }
                    else
                    {
                        result.Add(parentPrivilege.PrivilegeId, new RolePrivilege((int)parentPrivilege.Depth, parentPrivilege.PrivilegeId));
                    }
                }
            }
            return result.Values.ToArray();
        }

        private void TidyRoles(OrganizationServiceProxy orgService, List<SecurityRoleConfig> securityRoles, Entity rootBU, RolePrivilege[] parentPrivileges = null)
        {
            foreach (SecurityRoleConfig parentRole in securityRoles)
            {
                RolePrivilege[] privileges = null;

                using (TraceProgressReporter progress = new TraceProgressReporter(string.Format("Updating Role '{0}'", parentRole.Name)))
                {
                    // Get Root BU Role
                    QueryExpression roleQuery = new QueryExpression("role");
                    roleQuery.Criteria.AddCondition("name", ConditionOperator.Equal, parentRole.Name);
                    roleQuery.Criteria.AddCondition("businessunitid", ConditionOperator.Equal, rootBU.Id);
                    Entity rootBURole = QueryHelper.RetrieveSingleEntity(orgService, roleQuery);

                    // Retrieve Privileges for root BU Role
                    privileges = ((RetrieveRolePrivilegesRoleResponse)orgService.Execute(
                        new RetrieveRolePrivilegesRoleRequest { RoleId = rootBURole.Id })).RolePrivileges;

                    // Create merged set of privileges using parent privileges
                    privileges = MergePrivileges(privileges, parentPrivileges);

                    // Replace Privileges
                    orgService.Execute(new ReplacePrivilegesRoleRequest { Privileges = privileges, RoleId = rootBURole.Id });
                }

                // Recursive call
                TidyRoles(orgService, parentRole.ChildRoles, rootBU, privileges);
            }
        }
    }
}