using System.IO;
using Newtonsoft.Json;

namespace ArcGISUserManagement.Logic
{
	public class Settings
	{
		private string SettingsJson { get; set; } = string.Empty;
		private dynamic SettingsObject { get; set; }

		public Settings()
		{
			SettingsJson = File.ReadAllText("config.json");
			SettingsObject = JsonConvert.DeserializeObject(SettingsJson);
		}

		public string ArcGISPortalURL
		{
			get
			{
				return SettingsObject?.ArcGISPortalURL?.Value as string;
			}
		}

		public string ArcGISUsername
		{
			get
			{
				return SettingsObject?.ArcGISUsername?.Value as string;
			}
		}

		public string ArcGISPassword
		{
			get
			{
				return SettingsObject?.ArcGISPassword?.Value as string;
			}
		}
	}
}