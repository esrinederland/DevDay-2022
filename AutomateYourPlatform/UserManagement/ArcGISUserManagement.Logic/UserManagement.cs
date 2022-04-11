using ArcGIS.Net.API;
using ArcGIS.Net.API.Data;
using log4net;
using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Threading;

namespace ArcGISUserManagement.Logic
{
	public class UserManagement
	{
		IArcGISClient Client { get; set; } = null;

		public void UpdateGroupsAndUsers(ILog logger)
		{
			logger.Info("Start connecting to ArcGIS.");
			Settings config = new Settings();
			Client = new ArcGISPortal(new Uri(config.ArcGISPortalURL), config.ArcGISUsername, config.ArcGISPassword);
			logger.Info("Connected to ArcGIS.");

			// Create a ArcGIS.Net.Api group object.
			RestGroups restgroup = new RestGroups(Client);

			// Create a active directory logic class, to get the information from the active directory.
			ActiveDirectory activeDirectoryLogic = new ActiveDirectory(logger);

			// Get all the ArcGIS Groups with the tag: activedirectory
			logger.Info("Reading managed groups from ArcGIS.");
			List<Group> managedGroups = restgroup.Search("tags: activedirectory_");
			if (managedGroups == null)
			{
				logger.Warn("No groups found to manage with active directory");
				return;
			}

			// For every group found in ArcGIS, search for the group in the active directory.
			logger.Info($"Start updating users in groups.");
			foreach (Group managedGroup in managedGroups)
			{
				string groupName = string.Empty;
				string groupNameTag = managedGroup.Tags.FirstOrDefault(item => item.StartsWith("activedirectory_"));
				if (!string.IsNullOrEmpty(groupNameTag))
				{
					groupName = groupNameTag[16..];
				}

				// Try to get the active directory user from the group.
				logger.Info($"Get the user from group: {managedGroup.Title}");
				List<UserPrincipal> adusers = activeDirectoryLogic.GetUsersFromGroup(groupName);
				if (adusers == null)
				{
					logger.Info($"No group or users found for {managedGroup.Title}");
					continue;
				}

				// Search for the user in the ArcGIS Group.
				GroupUserResponse usersResponse = restgroup.Users(managedGroup.Id);
				if (usersResponse == null)
				{
					logger.Info($"No users for the ArcGIS Group {managedGroup.Title}");
				}

				// Search for the ad users who aren't in the ArcGIS group. This users will be added if not found. 
				foreach (UserPrincipal aduser in adusers)
				{
					string accountname = $"{aduser.GivenName}_DemoUser";
					logger.Info($"Check if user {accountname} has rights in ArcGIS");

					if (!usersResponse.Users.Contains(accountname) &&
						!usersResponse.Owner.Contains(accountname) &&
						!usersResponse.Admins.Contains(accountname))
					{
						// The users isn't part of the group, add the user to the group. 
						// Get or create a the user. 
						UserResponse arcgisUser = GetOrCreateUser(logger, managedGroup, aduser, accountname);

						// If user isn't null, add the user to the group.
						if (arcgisUser != null && arcgisUser.Username != null)
						{
							//Add the user to the group
							if (restgroup.AddUser(managedGroup.Id, arcgisUser.Username))
							{
								logger.Info($"User {accountname} succesful added to group.");
							}
							else
							{
								logger.Error($"Failed to add {accountname} to {managedGroup.Title}.");
							}
						}
						else
						{
							logger.Error($"User {accountname} not found in ArcGIS");
						}
					}
					else
					{
						logger.Debug($"User {accountname} is available in ArcGIS group.");
					}

					// For demo purpose, slow down the log
					Thread.Sleep(1000);
				}
			}
		}

		/// <summary>
		/// Get or creates the user in ArcGIS.
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="managedGroup"></param>
		/// <param name="aduser"></param>
		/// <param name="accountname"></param>
		/// <returns></returns>
		private UserResponse GetOrCreateUser(ILog logger, Group managedGroup, UserPrincipal aduser, string accountname)
		{
			// User not in the group 
			logger.Info($"User {accountname} not found in group {managedGroup.Title}");

			// Check if the user is allready available in ArcGIS
			RestUser restUser = new RestUser(Client);
			UserResponse arcgisUser = restUser.GetUser(accountname);
			if (arcgisUser == null || arcgisUser.Id == null)
			{
				// User is isn't available in ArcGIS, so we need to create the user. 
				logger.Info($"User {accountname} not found ArcGIS, start creating user.");

				// We use tags within the group to determine what type of user to create.
				string arcGISRoleTag = managedGroup.Tags.FirstOrDefault(item => item.StartsWith("ArcGISUserRole_"));
				string arcGISUserTypeTag = managedGroup.Tags.FirstOrDefault(item => item.StartsWith("ArcGISUserType_"));

				// If no tag is set, use a default. 
				// See help page for types: https://developers.arcgis.com/rest/enterprise-administration/portal/create-user.htm
				string arcGISRole = "iAAAAAAAAAAAAAAA";
				if (!string.IsNullOrEmpty(arcGISRoleTag))
				{
					arcGISRole = arcGISRoleTag[15..];
				}
				string arcGISUserType = "viewerUT";
				if (!string.IsNullOrEmpty(arcGISUserTypeTag))
				{
					arcGISUserType = arcGISUserTypeTag[15..];
				}

				// Get a default email adress or the user's email adres. 
				string email = "developers@esri.nl";
				if (!string.IsNullOrWhiteSpace(aduser.EmailAddress))
				{
					email = aduser.EmailAddress;
				}

				// Get the fistname
				string firstname = " ";
				if (string.IsNullOrWhiteSpace(aduser.GivenName) && string.IsNullOrWhiteSpace(aduser.Surname))
				{
					firstname = accountname;
				}
				else if (!string.IsNullOrWhiteSpace(aduser.GivenName))
				{
					firstname = aduser.GivenName;
				}

				// Get the last name
				string lastname = " ";
				if (!string.IsNullOrWhiteSpace(aduser.Surname))
				{
					lastname = $"{aduser.Surname} {aduser.MiddleName}";
				}

				// Create the users in ArcGIS
				try
				{
					bool issucces = restUser.CreateEnterpriseUser(accountname,
																	firstname,
																	lastname,
																	email,
																	accountname,
																	arcGISUserType,
																	arcGISRole,
																	"Domain user generated by the active directory user management scripts.");

					if (issucces)
					{
						// Get the created user.
						logger.Info($"User {accountname} succesful created in ArcGIS.");
						arcgisUser = restUser.GetUser(accountname);
						if (arcgisUser == null)
						{
							logger.Error($"ArcGIS user created succesful but failed to retrieve {accountname} from ArcGIS.");
						}
					}
					else
					{
						logger.Error($"Failed to add {accountname} to ArcGIS.");
					}
				}
				catch (Exception ex)
				{
					logger.Error($"Error when creating user in ArcGIS {ex.Message}", ex);
				}
			}
			return arcgisUser;
		}
	}
}