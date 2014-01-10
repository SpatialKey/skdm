using System;
using System.Xml;

namespace skdm
{
	public class ConfigAuth
	{
		public String organizationURL = "";
		public String userAPIKey = "";
		public String organizationAPIKey = "";
		public String organizationSecretKey = "";

		public ConfigAuth(XmlNode xml = null, ConfigAuth defaultConfig = null)
		{
			ParseXML(xml, defaultConfig);
		}

		public void ParseXML(XmlNode xml, ConfigAuth defaultConfig = null)
		{
			if (xml == null)
				return;
			organizationURL = XMLUtils.GetInnerText(xml, "/config/organizationURL", defaultConfig != null ? defaultConfig.organizationURL : "");
			userAPIKey = XMLUtils.GetInnerText(xml, "/config/userAPIKey", defaultConfig != null ? defaultConfig.userAPIKey : "");
			organizationAPIKey = XMLUtils.GetInnerText(xml, "/config/organizationAPIKey", defaultConfig != null ? defaultConfig.organizationAPIKey : "");
			organizationSecretKey = XMLUtils.GetInnerText(xml, "/config/organizationSecretKey", defaultConfig != null ? defaultConfig.organizationSecretKey : ""); 
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
			    (organizationSecretKey == p.organizationSecretKey)
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
				return hash;
			}
		}
	}
}

