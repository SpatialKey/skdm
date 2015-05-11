using System;
using System.Xml;

namespace SpatialKey.DataManager.Lib
{
	public class ConfigAuth
	{
		public const String PROXY_ENABLED = "true";

		public String organizationURL = "";
		public String userAPIKey = "";
		public String organizationAPIKey = "";
		public String organizationSecretKey = "";

		public String proxyEnable = PROXY_ENABLED;
		public String proxyURL = "";
		public String proxyPort = "";
		public String proxyUser = "";
		public String proxyPassword = "";
		public String proxyDomain = "";

		public ConfigAuth(XmlNode xml = null, ConfigAuth defaultConfig = null)
		{
			ParseXML(xml, defaultConfig);
		}

		virtual public void ParseXML(XmlNode xml, ConfigAuth defaultConfig = null)
		{
			if (xml == null)
				return;

			organizationURL = XMLUtils.GetInnerText(xml, "./organizationURL", defaultConfig != null ? defaultConfig.organizationURL : "");
			organizationAPIKey = XMLUtils.GetInnerText(xml, "./organizationAPIKey", defaultConfig != null ? defaultConfig.organizationAPIKey : "");
			organizationSecretKey = XMLUtils.GetInnerText(xml, "./organizationSecretKey", defaultConfig != null ? defaultConfig.organizationSecretKey : ""); 
			userAPIKey = XMLUtils.GetInnerText(xml, "./userAPIKey", defaultConfig != null ? defaultConfig.userAPIKey : "");

			proxyEnable = XMLUtils.GetInnerText(xml, "./proxyEnable", defaultConfig != null ? defaultConfig.proxyEnable : PROXY_ENABLED).ToLower().Trim(); 
			if (proxyEnable == "") proxyEnable = PROXY_ENABLED;

			proxyURL = XMLUtils.GetInnerText(xml, "./proxyURL", defaultConfig != null ? defaultConfig.proxyURL : ""); 
			proxyPort = XMLUtils.GetInnerText(xml, "./proxyPort", defaultConfig != null ? defaultConfig.proxyPort : ""); 
			proxyUser = XMLUtils.GetInnerText(xml, "./proxyUser", defaultConfig != null ? defaultConfig.proxyUser : ""); 
			proxyPassword = XMLUtils.GetInnerText(xml, "./proxyPassword", defaultConfig != null ? defaultConfig.proxyPassword : ""); 
			proxyDomain = XMLUtils.GetInnerText(xml, "./proxyDomain", defaultConfig != null ? defaultConfig.proxyDomain : ""); 
		}

		public override bool Equals(System.Object obj)
		{
			if (obj == null)
				return false;

			return this.Equals(obj as ConfigAuth);
		}

		public bool Equals(ConfigAuth p)
		{
			if (p == null)
				return false;

			return  (
			    (organizationURL == p.organizationURL) &&
			    (userAPIKey == p.userAPIKey) &&
			    (organizationAPIKey == p.organizationAPIKey) &&
				(organizationSecretKey == p.organizationSecretKey) &&
				(proxyURL == p.proxyURL) &&
				(proxyPort == p.proxyPort) &&
				(proxyUser == p.proxyUser) &&
				(proxyPassword == p.proxyPassword) &&
				(proxyDomain == p.proxyDomain)
			);
		}

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + organizationURL.GetHashCode();
				hash = hash * 23 + userAPIKey.GetHashCode();
				hash = hash * 23 + organizationAPIKey.GetHashCode();
				hash = hash * 23 + organizationSecretKey.GetHashCode();
				hash = hash * 23 + proxyURL.GetHashCode();
				hash = hash * 23 + proxyPort.GetHashCode();
				hash = hash * 23 + proxyUser.GetHashCode();
				hash = hash * 23 + proxyPassword.GetHashCode();
				hash = hash * 23 + proxyDomain.GetHashCode();
				return hash;
			}
		}
	}
}

