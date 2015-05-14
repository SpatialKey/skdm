using System;
using System.Xml;

using SpatialKey.DataManager.Lib.Helpers;

namespace SpatialKey.DataManager.Lib.Config
{
	public class ConfigAuth
	{
		public const String PROXY_ENABLED = "true";

		#region Fields
		protected String _organizationURL = "";
		protected String _userAPIKey = "";
		protected String _organizationAPIKey = "";
		protected String _organizationSecretKey = "";

		protected String _proxyEnable = PROXY_ENABLED;
		protected String _proxyURL = "";
		protected String _proxyPort = "";
		protected String _proxyUser = "";
		protected String _proxyPassword = "";
		protected String _proxyDomain = "";
		#endregion

		#region Properties
		virtual public String organizationURL { get { return _organizationURL; } set { _organizationURL = value; } }
		virtual public String userAPIKey { get { return _userAPIKey; } set { _userAPIKey = value; } }
		virtual public String organizationAPIKey { get { return _organizationAPIKey; } set { _organizationAPIKey = value; } }
		virtual public String organizationSecretKey { get { return _organizationSecretKey; } set { _organizationSecretKey = value; } }

		virtual public String proxyEnable { get { return _proxyEnable; } set { _proxyEnable = value; } }
		virtual public String proxyURL { get { return _proxyURL; } set { _proxyURL = value; } }
		virtual public String proxyPort { get { return _proxyPort; } set { _proxyPort = value; } }
		virtual public String proxyUser { get { return _proxyUser; } set { _proxyUser = value; } }
		virtual public String proxyPassword { get { return _proxyPassword; } set { _proxyPassword = value; } }
		virtual public String proxyDomain { get { return _proxyDomain; } set { _proxyDomain = value; } }
		#endregion

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

