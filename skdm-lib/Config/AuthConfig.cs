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
		virtual public string OrganizationUrl { get { return _organizationURL; } set { _organizationURL = value; } }
		virtual public string UserApiKey { get { return _userAPIKey; } set { _userAPIKey = value; } }
		virtual public string OrganizationApiKey { get { return _organizationAPIKey; } set { _organizationAPIKey = value; } }
		virtual public string OrganizationSecretKey { get { return _organizationSecretKey; } set { _organizationSecretKey = value; } }

		virtual public bool ProxyEnable { get { return _proxyEnable; } set { _proxyEnable = value; } }
		virtual public string ProxyUrl { get { return _proxyURL; } set { _proxyURL = value; } }
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

			OrganizationUrl = XMLUtils.GetInnerText(xml, "./organizationURL", defaultConfig != null ? defaultConfig.OrganizationUrl : "");
			OrganizationApiKey = XMLUtils.GetInnerText(xml, "./organizationAPIKey", defaultConfig != null ? defaultConfig.OrganizationApiKey : "");
			OrganizationSecretKey = XMLUtils.GetInnerText(xml, "./organizationSecretKey", defaultConfig != null ? defaultConfig.OrganizationSecretKey : ""); 
			UserApiKey = XMLUtils.GetInnerText(xml, "./userAPIKey", defaultConfig != null ? defaultConfig.UserApiKey : "");

			string tmpEnabled = XMLUtils.GetInnerText(xml, "./proxyEnable", defaultConfig != null ? defaultConfig.ProxyEnable.ToString() : PROXY_ENABLED).ToLower().Trim(); 
			ProxyEnable = string.IsNullOrWhiteSpace(tmpEnabled) || Convert.ToBoolean(tmpEnabled);

			ProxyUrl = XMLUtils.GetInnerText(xml, "./proxyURL", defaultConfig != null ? defaultConfig.ProxyUrl : ""); 
			ProxyPort = XMLUtils.GetInnerText(xml, "./proxyPort", defaultConfig != null ? defaultConfig.ProxyPort : ""); 
			ProxyUser = XMLUtils.GetInnerText(xml, "./proxyUser", defaultConfig != null ? defaultConfig.ProxyUser : ""); 
			ProxyPassword = XMLUtils.GetInnerText(xml, "./proxyPassword", defaultConfig != null ? defaultConfig.ProxyPassword : ""); 
			ProxyDomain = XMLUtils.GetInnerText(xml, "./proxyDomain", defaultConfig != null ? defaultConfig.ProxyDomain : ""); 
		}

		public void Copy(IAuthConfig config)
		{
			if (config == null) return;

			OrganizationUrl = config.OrganizationUrl;
			UserApiKey = config.UserApiKey;
			OrganizationApiKey = config.OrganizationApiKey;
			OrganizationSecretKey = config.OrganizationSecretKey;

			ProxyEnable = config.ProxyEnable;
			ProxyUrl = config.ProxyUrl;
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
			    (OrganizationUrl == p.OrganizationUrl) &&
			    (UserApiKey == p.UserApiKey) &&
			    (OrganizationApiKey == p.OrganizationApiKey) &&
				(OrganizationSecretKey == p.OrganizationSecretKey) &&
				(ProxyUrl == p.ProxyUrl) &&
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
				hash = hash * 23 + OrganizationUrl.GetHashCode();
				hash = hash * 23 + UserApiKey.GetHashCode();
				hash = hash * 23 + OrganizationApiKey.GetHashCode();
				hash = hash * 23 + OrganizationSecretKey.GetHashCode();
				hash = hash * 23 + ProxyUrl.GetHashCode();
				hash = hash * 23 + ProxyPort.GetHashCode();
				hash = hash * 23 + ProxyUser.GetHashCode();
				hash = hash * 23 + ProxyPassword.GetHashCode();
				hash = hash * 23 + ProxyDomain.GetHashCode();
				return hash;
			}
		}

        public object Clone()
        {
            return new AuthConfig
            {
                OrganizationUrl = OrganizationUrl == null ? null : string.Copy(OrganizationUrl),
                UserApiKey = UserApiKey == null ? null : string.Copy(UserApiKey),
                OrganizationApiKey = OrganizationApiKey == null ? null : string.Copy(OrganizationApiKey),
                OrganizationSecretKey = OrganizationSecretKey == null ? null : string.Copy(OrganizationSecretKey),

                ProxyEnable = ProxyEnable,
                ProxyUrl = ProxyUrl == null ? null : string.Copy(ProxyUrl),
                ProxyPort = ProxyPort == null ? null : string.Copy(ProxyPort),
                ProxyUser = ProxyUser == null ? null : string.Copy(ProxyUser),
                ProxyPassword = ProxyPassword == null ? null : string.Copy(ProxyPassword),
                ProxyDomain = ProxyDomain == null ? null : string.Copy(ProxyDomain)
            };
        }
	}
}

