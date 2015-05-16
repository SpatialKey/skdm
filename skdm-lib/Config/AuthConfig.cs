using System;
using System.Xml;

using SpatialKey.DataManager.Lib.Helpers;

namespace SpatialKey.DataManager.Lib.Config
{
	public class AuthConfig : IAuthConfig
	{
		private const String PROXY_ENABLED = "true";

		#region Fields
		protected string _organizationURL = "";
		protected string _userAPIKey = "";
		protected string _organizationAPIKey = "";
		protected string _organizationSecretKey = "";

		protected bool _proxyEnable;
		protected string _proxyURL = "";
		protected string _proxyPort = "";
		protected string _proxyUser = "";
		protected string _proxyPassword = "";
		protected string _proxyDomain = "";
		#endregion

		#region Properties
		virtual public string OrganizationURL { get { return _organizationURL; } set { _organizationURL = value; } }
		virtual public string UserAPIKey { get { return _userAPIKey; } set { _userAPIKey = value; } }
		virtual public string OrganizationAPIKey { get { return _organizationAPIKey; } set { _organizationAPIKey = value; } }
		virtual public string OrganizationSecretKey { get { return _organizationSecretKey; } set { _organizationSecretKey = value; } }

		virtual public bool ProxyEnable { get { return _proxyEnable; } set { _proxyEnable = value; } }
		virtual public string ProxyURL { get { return _proxyURL; } set { _proxyURL = value; } }
		virtual public string ProxyPort { get { return _proxyPort; } set { _proxyPort = value; } }
		virtual public string ProxyUser { get { return _proxyUser; } set { _proxyUser = value; } }
		virtual public string ProxyPassword { get { return _proxyPassword; } set { _proxyPassword = value; } }
		virtual public string ProxyDomain { get { return _proxyDomain; } set { _proxyDomain = value; } }
		#endregion

		public AuthConfig(XmlNode xml = null, IAuthConfig defaultConfig = null)
		{
			ParseXML(xml, defaultConfig);
		}

		virtual public void ParseXML(XmlNode xml, IAuthConfig defaultConfig = null)
		{
			if (xml == null)
			{
				Copy(defaultConfig);
				return;
			}

			OrganizationURL = XMLUtils.GetInnerText(xml, "./organizationURL", defaultConfig != null ? defaultConfig.OrganizationURL : "");
			OrganizationAPIKey = XMLUtils.GetInnerText(xml, "./organizationAPIKey", defaultConfig != null ? defaultConfig.OrganizationAPIKey : "");
			OrganizationSecretKey = XMLUtils.GetInnerText(xml, "./organizationSecretKey", defaultConfig != null ? defaultConfig.OrganizationSecretKey : ""); 
			UserAPIKey = XMLUtils.GetInnerText(xml, "./userAPIKey", defaultConfig != null ? defaultConfig.UserAPIKey : "");

			string tmpEnabled = XMLUtils.GetInnerText(xml, "./proxyEnable", defaultConfig != null ? defaultConfig.ProxyEnable.ToString() : PROXY_ENABLED).ToLower().Trim(); 
			if (tmpEnabled == "") ProxyEnable = true;
			else ProxyEnable = Convert.ToBoolean(tmpEnabled);

			ProxyURL = XMLUtils.GetInnerText(xml, "./proxyURL", defaultConfig != null ? defaultConfig.ProxyURL : ""); 
			ProxyPort = XMLUtils.GetInnerText(xml, "./proxyPort", defaultConfig != null ? defaultConfig.ProxyPort : ""); 
			ProxyUser = XMLUtils.GetInnerText(xml, "./proxyUser", defaultConfig != null ? defaultConfig.ProxyUser : ""); 
			ProxyPassword = XMLUtils.GetInnerText(xml, "./proxyPassword", defaultConfig != null ? defaultConfig.ProxyPassword : ""); 
			ProxyDomain = XMLUtils.GetInnerText(xml, "./proxyDomain", defaultConfig != null ? defaultConfig.ProxyDomain : ""); 
		}

		public void Copy(IAuthConfig config)
		{
			if (config == null) return;

			OrganizationURL = config.OrganizationURL;
			UserAPIKey = config.UserAPIKey;
			OrganizationAPIKey = config.OrganizationAPIKey;
			OrganizationSecretKey = config.OrganizationSecretKey;

			ProxyEnable = config.ProxyEnable;
			ProxyURL = config.ProxyURL;
			ProxyPort = config.ProxyPort;
			ProxyUser = config.ProxyUser;
			ProxyPassword = config.ProxyPassword;
			ProxyDomain = config.ProxyDomain;
		}

		public override bool Equals(System.Object obj)
		{
			if (obj == null)
				return false;

			return this.Equals(obj as IAuthConfig);
		}

		public bool Equals(IAuthConfig p)
		{
			if (p == null)
				return false;

			return  (
			    (OrganizationURL == p.OrganizationURL) &&
			    (UserAPIKey == p.UserAPIKey) &&
			    (OrganizationAPIKey == p.OrganizationAPIKey) &&
				(OrganizationSecretKey == p.OrganizationSecretKey) &&
				(ProxyURL == p.ProxyURL) &&
				(ProxyPort == p.ProxyPort) &&
				(ProxyUser == p.ProxyUser) &&
				(ProxyPassword == p.ProxyPassword) &&
				(ProxyDomain == p.ProxyDomain)
			);
		}

		public override int GetHashCode()
		{
			unchecked // Overflow is fine, just wrap
			{
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + OrganizationURL.GetHashCode();
				hash = hash * 23 + UserAPIKey.GetHashCode();
				hash = hash * 23 + OrganizationAPIKey.GetHashCode();
				hash = hash * 23 + OrganizationSecretKey.GetHashCode();
				hash = hash * 23 + ProxyURL.GetHashCode();
				hash = hash * 23 + ProxyPort.GetHashCode();
				hash = hash * 23 + ProxyUser.GetHashCode();
				hash = hash * 23 + ProxyPassword.GetHashCode();
				hash = hash * 23 + ProxyDomain.GetHashCode();
				return hash;
			}
		}
	}
}

