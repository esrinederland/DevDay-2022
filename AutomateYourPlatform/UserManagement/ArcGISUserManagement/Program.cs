using ArcGISUserManagement.Logic;
using log4net;
using log4net.Config;
using System.IO;
using System.Reflection;

namespace ArcGISUserManagement
{
	class Program
	{
		static void Main(string[] args)
		{
			// Load log4Net configuration
			var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
			XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

			// create logger
			ILog logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
			logger.Debug("Start active directory users with groups update script.");

			UserManagement um = new UserManagement();
			um.UpdateGroupsAndUsers(logger);

			logger.Debug("Script completed succesful.");
		}
	}
}