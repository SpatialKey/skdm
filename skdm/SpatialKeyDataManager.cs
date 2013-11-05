/// <file>
/// <copyright>Copyright (c) 2013 SpatialKey</copyright>
/// <author>Robert Stehwien</author>
/// <datecreated>2013-04-01</datecreated
/// </file>
using System;
using System.Xml;
using System.Web;
using System.Net;
using System.IO;
using System.Text;
using System.Collections.Specialized;
using System.Collections.Generic;

// Using http://www.icsharpcode.net/opensource/sharpziplib/
namespace skdm
{
	/// <summary>
	/// The SpatialKey Data Import API (DIAPI) allows developers to programmatically create or update SpatialKey datasets.
	/// </summary>
	/// <see cref="http://support.spatialkey.com/dmapi"/>
	public class SpatialKeyDataManager
	{
		public const string API_VERSION = "v1";

		#region parameters

		/// <summary>Logging delegate</summary>
		public delegate void Logger(string message);

		/// <summary>
		/// Name of the SpatialKey organization url"/>
		/// </summary>
		public string OrganizationURL { get; private set; }

		/// <summary>
		/// Authentication organizationAPIKey for <see cref="Authenticate()"/>
		/// </summary>
		public string OrganizationAPIKey { get; private set; }

		/// <summary>
		/// Authentication organizationSecretKey for <see cref="Authenticate()"/>
		/// </summary>
		public string OrganizationSecretKey { get; private set; }

		/// <summary>
		/// Authentication userAPIKey for <see cref="Authenticate()"/>
		/// </summary>
		public string UserAPIKey { get; private set; }

		/// <summary>
		/// Gets or sets the logger.  Used by  for <see cref="Log(message)"/>
		/// </summary>
		public Logger MyLogger { get; set; }

		#endregion

		#region dataMartAPI properties - set by calls

		private CookieCollection _cookies;
		private String _accessToken;

		#endregion

		public SpatialKeyDataManager(Logger logger = null)
		{
			this.MyLogger = logger;
		}

		public void Init(string organizationURL, string organizationAPIKey, string organizationSecretKey, string userAPIKey)
		{
			// if the setup is the same, bail
			if (this.OrganizationURL == organizationURL &&
			    this.OrganizationAPIKey == organizationAPIKey &&
			    this.OrganizationSecretKey == organizationSecretKey &&
			    this.UserAPIKey == userAPIKey)
			{
				return;
			}

			this.OrganizationURL = organizationURL;
			this.OrganizationAPIKey = organizationAPIKey;
			this.OrganizationSecretKey = organizationSecretKey;
			this.UserAPIKey = userAPIKey;
		}

		/// <summary>
		/// Log the specified message if the <see cref="logger"/> is set
		/// </summary>
		/// <param name='message'>
		/// Message to log
		/// </param>
		private void Log(string message)
		{
			if (MyLogger != null)
				MyLogger(message);
		}

		#region API calls

		public string GetOAuthToken()
		{
			return OAuth.GetOAuthToken(UserAPIKey, OrganizationAPIKey, OrganizationSecretKey, 1800);
		}

		public void Login()
		{
			if (IsLoginTokenValid())
				return;

			string url = BuildUrl("oauth.json");
			Log("START LOGIN: " + url);

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";
			query["assertion"] = GetOAuthToken();
			Log("QUERY PARAMS: " + NameValueCollectionToString(query));

			CustomWebClient client = CreateCustomWebClient();
			byte[] response = client.UploadValues(url, "POST", query);
			Console.WriteLine(Encoding.UTF8.GetString(response));
			Dictionary<string,object> json = MiniJson.Deserialize(Encoding.UTF8.GetString(response)) as Dictionary<string,object>;
			_accessToken = json["access_token"] as String;
		}

		public bool IsLoginTokenValid()
		{
			if (_accessToken == null || _accessToken.Length == 0)
				return false;

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;
			string url = BuildUrl("oauth.json", query);
			Log("VALIDATE TOKEN: " + url);

			HttpWebRequest req = WebRequest.Create(url) as HttpWebRequest;
			string result = null;
			using (HttpWebResponse resp = req.GetResponse() as HttpWebResponse)
			{
				StreamReader reader = new StreamReader(resp.GetResponseStream());
				result = reader.ReadToEnd();
				Console.WriteLine(result);
				return resp.StatusCode == HttpStatusCode.OK;
			}
		}

		public void Logout()
		{
		}

		public void Upload()
		{
		}

		public void GetUploadStatus()
		{
		}

		public void GetSampleImportConfiguration()
		{
		}

		public void Import()
		{
		}

		public void GetImportStatus()
		{
		}

		#endregion

		#region helpers

		private string BuildUrl(string path, NameValueCollection query = null)
		{
			UriBuilder uri = new UriBuilder(OrganizationURL);
			uri.Path = String.Format("/SpatialKeyFramework/api/{0}/{1}", API_VERSION, path);
			return uri.ToString() + ToQueryString(query);
		}

		private string ToQueryString(NameValueCollection nvc)
		{
			if (nvc == null || nvc.Count < 1)
				return "";

			List<string> list = new List<string>();
			foreach (string key in nvc) {
				list.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key])));
			}
			return "?" + string.Join("&", list);
		}

		private CustomWebClient CreateCustomWebClient()
		{
			CookieContainer cookieJar = new CookieContainer();
			if (_cookies != null)
			{
				foreach (Cookie c in _cookies)
				{
					Cookie cookie = new Cookie(c.Name, c.Value);
					cookieJar.Add(new Uri(OrganizationURL), cookie);
				}
			}
			return new CustomWebClient(cookieJar);
		}

		private void AddCookiesToQuery(NameValueCollection query)
		{
			foreach (Cookie c in _cookies)
			{
				query.Add(c.Name, c.Value);
			}
		}

		/// <summary>
		/// Custom <see cref="System.Net.WebClient"/> that allows setting of cookies
		/// </summary>
		/// <see cref="http://www.codeproject.com/Articles/72232/C-File-Upload-with-form-fields-cookies-and-headers"/>
		private class CustomWebClient : WebClient
		{
			private CookieContainer _cookies;

			public CustomWebClient(CookieContainer cookies)
			{
				_cookies = cookies;
			}

			protected override WebRequest GetWebRequest(Uri address)
			{
				HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
				request.CookieContainer = _cookies;
				request.Timeout = 3600000; // 60 min * 60 sec * 1000 msec = 3600000 msec
				return request;
			}
		}

		/// <summary>
		/// Gets a string representation of a NameValueCollection
		/// </summary>
		/// <returns>
		/// The value collection as a string
		/// </returns>
		/// <param name='c'>
		/// The NameValueCollection
		/// </param>
		private static string NameValueCollectionToString(NameValueCollection c)
		{
			string[] d = new string[c.Count];
			int i = 0;
			foreach (string key in c)
			{
				d[i] = string.Format("{0}={1}", key, c[key]);
				i++;
			}
			return string.Join(", ", d);
		}

		/// <summary>
		/// Gets a temporary file path with a given extension
		/// </summary>
		/// <returns>
		/// The temp file path
		/// </returns>
		/// <param name='fileExtension'>
		/// File extension.
		/// </param>
		private string GetTempFile(string fileExtension)
		{
			string temp = System.IO.Path.GetTempPath();
			string res = string.Empty;
			while (true)
			{
				res = string.Format("{0}.{1}", Guid.NewGuid().ToString(), fileExtension);
				res = System.IO.Path.Combine(temp, res);
				if (!System.IO.File.Exists(res))
				{
					try
					{
						System.IO.FileStream s = System.IO.File.Create(res);
						s.Close();
						break;
					}
					catch (Exception)
					{
						
					}
				}
			}
			return res;
		}

		#endregion

	}
}

