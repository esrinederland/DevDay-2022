using log4net;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace ArcGISUserManagement.Logic
{
    public class ActiveDirectory
    {
        private ILog Logger { get; set; }

        public ActiveDirectory(ILog logger)
        {
            Logger = logger;
        }

        /// <summary>
        /// Gets all the groups for the current user in the domain.
        /// </summary>
        /// <returns></returns>
        public List<GroupPrincipal> GetGroups()
        {
            try
            {
                Logger.Debug($"Connection to active directory {Environment.UserDomainName} with user {Environment.UserName}");

                using (PrincipalContext context = new PrincipalContext(ContextType.Domain, Environment.UserDomainName))
                {
                    using (UserPrincipal user = UserPrincipal.FindByIdentity(context, Environment.UserName))
                    {
                        if (user != null)
                        {
                            PrincipalSearchResult<Principal> groups = user.GetGroups();
                            return groups.Select(item => item as GroupPrincipal).ToList();
                        }
                        else
                        {
                            Logger.Warn($"User not found {Environment.UserName}");
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.Error("Error with AD", ex);
            }

            return null;
        }

        /// <summary>
        /// Gets all the users within a given domain group
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public List<UserPrincipal> GetUsersFromGroup(string groupName)
        {
            try
            {
                Logger.Debug($"Connection to active directory {Environment.UserDomainName}, reading users from group {groupName}");

                PrincipalContext context = new PrincipalContext(ContextType.Domain, Environment.UserDomainName);
                if (context != null)
                {
                    GroupPrincipal group = GroupPrincipal.FindByIdentity(context, IdentityType.Name, groupName);
                    if (group != null)
                    {
                        PrincipalSearchResult<Principal> usersearch = group.GetMembers(true);
                        if (usersearch != null)
                        {
                            return usersearch.Select(item => item as UserPrincipal).ToList();
                        }
                        else
                        {
                            Logger.Warn($"Users not found for grous {groupName}");
                        }
                    }
                    else
                    {
                        Logger.Warn($"Group {groupName} not found in domain.");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error with AD", ex);
            }

            return null;
        }
    }
} 