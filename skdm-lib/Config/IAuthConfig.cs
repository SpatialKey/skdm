using System;

namespace SpatialKey.DataManager.Lib.Config
{
	public interface IAuthConfig
	{
		string OrganizationURL { get; set; }
		string UserAPIKey { get; set; }
		string OrganizationAPIKey { get; set; }
		string OrganizationSecretKey { get; set; }

		bool ProxyEnable { get; set; }
		string ProxyURL { get; set; }
		string ProxyPort { get; set; }
		string ProxyUser { get; set; }
		string ProxyPassword { get; set; }
		string ProxyDomain { get; set; }
	}
}

