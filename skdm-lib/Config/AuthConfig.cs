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
		virtual public string organizationURL { get { return _organizationURL; } set { _organizationURL = value; } }
		virtual public string userAPIKey { get { return _userAPIKey; } set { _userAPIKey = value; } }
		virtual public string organizationAPIKey { get { return _organizationAPIKey; } set { _organizationAPIKey = value; } }
		virtual public string organizationSecretKey { get { return _organizationSecretKey; } set { _organizationSecretKey = value; } }

		virtual public bool proxyEnable { get { return _proxyEnable; } set { _proxyEnable = value; } }
		virtual public string proxyURL { get { return _proxyURL; } set { _proxyURL = value; } }
		virtual public string proxyPort { get { return _proxyPort; } set { _proxyPort = value; } }
		virtual public string proxyUser { get { return _proxyUser; } set { _proxyUser = value; } }
		virtual public string proxyPassword { get { return _proxyPassword; } set { _proxyPassword = value; } }
		virtual public string proxyDomain { get { return _proxyDomain; } set { _proxyDomain = value; } }
		#endregion

		public AuthConfig(XmlNode xml = null, IAuthConfig defaultConfig = null)
		{
			ParseXML(xml, defaultConfig);
		}

		virtual public void ParseXML(XmlNode xml, IAuthConfig defaultConfig = null)
		{
			if (xml == null)
				return;

			organizationURL = XMLUtils.GetInnerText(xml, "./organizationURL", defaultConfig != null ? defaultConfig.organizationURL : "");
			organizationAPIKey = XMLUtils.GetInnerText(xml, "./organizationAPIKey", defaultConfig != null ? defaultConfig.organizationAPIKey : "");
			organizationSecretKey = XMLUtils.GetInnerText(xml, "./organizationSecretKey", defaultConfig != null ? defaultConfig.organizationSecretKey : ""); 
			userAPIKey = XMLUtils.GetInnerText(xml, "./userAPIKey", defaultConfig != null ? defaultConfig.userAPIKey : "");

			string tmpEnabled = XMLUtils.GetInnerText(xml, "./proxyEnable", defaultConfig != null ? defaultConfig.proxyEnable.ToString() : PROXY_ENABLED).ToLower().Trim(); 
			if (tmpEnabled == "") proxyEnable = true;
			else proxyEnable = Convert.ToBoolean(tmpEnabled);

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

			return this.Equals(obj as IAuthConfig);
		}

		public bool Equals(IAuthConfig p)
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

