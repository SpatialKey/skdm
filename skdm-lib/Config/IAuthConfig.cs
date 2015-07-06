using System;

namespace SpatialKey.DataManager.Lib.Config
{
	public interface IAuthConfig : ICloneable
	{
		string OrganizationUrl { get; set; }
		string UserApiKey { get; set; }
		string OrganizationApiKey { get; set; }
		string OrganizationSecretKey { get; set; }

		bool ProxyEnable { get; set; }
		string ProxyUrl { get; set; }
		string ProxyPort { get; set; }
		string ProxyUser { get; set; }
		string ProxyPassword { get; set; }
		string ProxyDomain { get; set; }
	}
}

