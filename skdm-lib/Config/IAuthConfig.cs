using System;

namespace SpatialKey.DataManager.Lib.Config
{
	public interface IAuthConfig
	{
		string organizationURL { get; set; }
		string userAPIKey { get; set; }
		string organizationAPIKey { get; set; }
		string organizationSecretKey { get; set; }

		bool proxyEnable { get; set; }
		string proxyURL { get; set; }
		string proxyPort { get; set; }
		string proxyUser { get; set; }
		string proxyPassword { get; set; }
		string proxyDomain { get; set; }
	}
}

