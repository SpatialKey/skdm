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
		public const string API_VERSION = "v2";
		// 1 min * 60 sec * 1000 msec = 60000 msec
		private const int HTTP_TIMEOUT_SHORT = 60000;
		// 15 min * 60 sec * 1000 msec = 900000 msec
		private const int HTTP_TIMEOUT_MED = 900000;
		// 60 min * 60 sec * 1000 msec = 3600000 msec
		private const int HTTP_TIMEOUT_LONG = 3600000;

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

			Logout();

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

		public string Login()
		{
			if (IsLoginTokenValid())
				return _accessToken;

			_accessToken = null;

			Log("START LOGIN: " + OrganizationURL);

			// add the query string
			NameValueCollection bodyParam = new NameValueCollection();
			bodyParam["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";
			bodyParam["assertion"] = GetOAuthToken();

			using (HttpWebResponse response = HttpPost(BuildUrl("oauth.json"), null, bodyParam))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					Dictionary<string,object> json = HttpResponseToJSON(response);
					_accessToken = json["access_token"] as String;
					return _accessToken;
				}
				else
				{
					return null;
				}
			}
		}

		public bool IsLoginTokenValid()
		{
			if (_accessToken == null || _accessToken.Length == 0)
				return false;

			Log("VALIDATE TOKEN: " + _accessToken);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpGet(BuildUrl("oauth.json"), query))
			{
				HttpResponseToJSON(response);
				return response.StatusCode == HttpStatusCode.OK;
			}
		}

		public void Logout()
		{
			if (_accessToken == null || _accessToken.Length == 0)
				return;

			Log("LOGOUT TOKEN: " + _accessToken);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpGet(BuildUrl("oauth.json"), query, "DELETE"))
			{
				HttpResponseToJSON(response);
				_accessToken = null;
			}
		}

		public string Upload(string path)
		{
			if (Login() == null)
				return null;

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;
			using (HttpWebResponse response = HttpUploadFile(BuildUrl("upload.json"), path, "file", "application/octet-stream", query, null))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					Dictionary<string,object> json = HttpResponseToJSON(response);
					string uploadId = (json["upload"] as Dictionary<string,object>)["uploadId"] as String;
					return uploadId;
				}
				else
				{
					return null;
				}
			}
		}

		public string GetUploadStatus(string uploadId)
		{
			if (Login() == null)
				return null;

			Log("UPLOAD STATUS: " + uploadId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("upload/{0}.json", uploadId)), query))
			{
				Dictionary<string,object> json = HttpResponseToJSON(response);
				return json["status"] as String;
			}
		}

		public void WaitUploadComplete(string uploadId)
		{
			DateTime start = DateTime.Now;
			while (IsUploadStatusWorking(GetUploadStatus(uploadId)))
			{
				if (DateTime.Now.Subtract(start).TotalMilliseconds > HTTP_TIMEOUT_MED)
					throw new Exception("Timed out waiting for upload to complete");

				System.Threading.Thread.Sleep(10000);
			}
		}

		private static readonly List<string> UPLOAD_IDLE_STATUSES = new List<string> {
			"UPLOAD_IDLE",
			"UPLOAD_CANCELED",
			"IMPORT_COMPLETE_CLEAN",
			"IMPORT_COMPLETE_WARNING"
		};

		private bool IsUploadStatusWorking(string status)
		{
			return !(status.IndexOf("ERROR_") == 0 || UPLOAD_IDLE_STATUSES.IndexOf(status) >= 0);
		}

		public void GetSampleImportConfiguration()
		{
		}

		public bool Import(string uploadId, string pathConfig)
		{
			if (Login() == null)
				return false;

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpPostXml(BuildUrl(string.Format("upload/{0}/dataset.json", uploadId)), pathConfig, query))
			{
				HttpResponseToJSON(response);
				return response.StatusCode == HttpStatusCode.OK;
			}
		}

		public bool Append(string uploadId, string datasetId, string pathConfig)
		{
			return AppendOrOverwrite(uploadId, datasetId, pathConfig, "Append");
		}

		public bool Overwrite(string uploadId, string datasetId, string pathConfig)
		{
			return AppendOrOverwrite(uploadId, datasetId, pathConfig, "Overwrite");
		}

		private bool AppendOrOverwrite(string uploadId, string datasetId, string pathConfig, string method)
		{
			if (Login() == null)
				return false;

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;
			query["method"] = method;

			using (HttpWebResponse response = HttpPostXml(BuildUrl(string.Format("upload/{0}/dataset/{1}.json", uploadId, datasetId)), pathConfig, query))
			{
				HttpResponseToJSON(response);
				return response.StatusCode == HttpStatusCode.OK;
			}
		}

		public void GetImportStatus()
		{
		}

		public void CancelUpload(string uploadId)
		{
			if (Login() == null)
				return;

			Log("UPLOAD STATUS: " + uploadId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("upload/{0}.json", uploadId)), query, "DELETE"))
			{
				HttpResponseToJSON(response);
			}
		}

		#endregion

		#region helpers

		private string BuildUrl(string command, NameValueCollection query = null)
		{
			UriBuilder uri = new UriBuilder(OrganizationURL);
			uri.Path = String.Format("/SpatialKeyFramework/api/{0}/{1}", API_VERSION, command);
			return uri.ToString() + ToQueryString(query);
		}

		private string ToQueryString(NameValueCollection nvc)
		{
			if (nvc == null || nvc.Count < 1)
				return "";

			List<string> list = new List<string>();
			foreach (string key in nvc)
			{
				list.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key])));
			}
			return "?" + string.Join("&", list);
		}

		private Dictionary<string, object> HttpResponseToJSON(HttpWebResponse response)
		{
			Dictionary<string, object> json;
			using (response)
			{
				StreamReader reader = new StreamReader(response.GetResponseStream());
				string result = reader.ReadToEnd();
				Log("RESULT: " + result);
				json = MiniJson.Deserialize(result) as Dictionary<string, object>;
			}
			return json;
		}

		private HttpWebResponse HttpGet(string url, NameValueCollection queryParam = null, string method = "GET", int timeout = HTTP_TIMEOUT_SHORT)
		{
			url = url + ToQueryString(queryParam);
			Log(String.Format("HTTP GET: {0}", url));

			HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
			request.Method = method;
			request.Timeout = timeout;
			try
			{
				return request.GetResponse() as HttpWebResponse;
			}
			catch (WebException we)
			{
				var response = we.Response as HttpWebResponse;
				if (response == null)
					throw;
				Log(String.Format("STATUS CODE ERROR: {0}", response.StatusCode.ToString()));
				return response;
			}

		}

		private HttpWebResponse HttpPost(string url, NameValueCollection queryParam = null, NameValueCollection bodyParam = null, string method = "POST", int timeout = HTTP_TIMEOUT_SHORT)
		{
			url = url + ToQueryString(queryParam);
			HttpWebRequest request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
			request.Method = method;  
			request.Timeout = timeout;
			request.ContentType = "application/x-www-form-urlencoded";

			// get the query string and trim off the starting "?"
			string body = ToQueryString(bodyParam).Remove(0, 1);

			Log(String.Format("HTTP POST URL: {0} PARAM: {1}", url, body));

			// Encode the parameters as form data:
			byte[] formData = UTF8Encoding.UTF8.GetBytes(body);
			request.ContentLength = formData.Length;

			// Send the request:
			using (Stream post = request.GetRequestStream())
			{  
				post.Write(formData, 0, formData.Length);  
			}

			try
			{
				return request.GetResponse() as HttpWebResponse;
			}
			catch (WebException we)
			{
				var response = we.Response as HttpWebResponse;
				if (response == null)
					throw;
				Log(String.Format("STATUS CODE ERROR: {0}", response.StatusCode.ToString()));
				return response;
			}
		}

		private HttpWebResponse HttpPostXml(string url, string pathXML, NameValueCollection queryParam = null, string method = "POST", int timeout = HTTP_TIMEOUT_MED)
		{
			url = url + ToQueryString(queryParam);
			HttpWebRequest request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
			request.Method = method;  
			request.Timeout = timeout;
			request.ContentType = "application/xml";

			Log(String.Format("HTTP POST URL: {0} XML: {1}", url, pathXML));

			// Send the xml:
			using (Stream rs = request.GetRequestStream())
			{  
				FileStream fileStream = new FileStream(pathXML, FileMode.Open, FileAccess.Read);
				byte[] buffer = new byte[4096];
				int bytesRead = 0;
				while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
				{
					WriteUploadBytes(rs, buffer, bytesRead, false);
				}
				fileStream.Close();
			}

			try
			{
				return request.GetResponse() as HttpWebResponse;
			}
			catch (WebException we)
			{
				var response = we.Response as HttpWebResponse;
				if (response == null)
					throw;
				Log(String.Format("STATUS CODE ERROR: {0}", response.StatusCode.ToString()));
				return response;
			}
		}

		private HttpWebResponse HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection queryParam, NameValueCollection bodyParam, string method = "POST", int timeout = HTTP_TIMEOUT_LONG)
		{
			url = url + ToQueryString(queryParam);
			Log(string.Format("HTTP UPLOAD {0} to {1}", file, url));
			string boundary = String.Format("-----------{0:N}", Guid.NewGuid());
			byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.ContentType = "multipart/form-data; boundary=" + boundary;
			request.Method = method;
			request.KeepAlive = true;
			request.Timeout = timeout;
			//request.CookieContainer = new CookieContainer();

			Log("Content-Type: " + request.ContentType);
			long bytes = 0;
			using (Stream rs = request.GetRequestStream())
			{
				// Write NVP
				if (bodyParam != null)
				{
					string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
					foreach (string key in bodyParam.Keys)
					{
						bytes += WriteUploadBytes(rs, boundarybytes);

						string formitem = string.Format(formdataTemplate, key, bodyParam[key]);
						bytes += WriteUploadBytes(rs, System.Text.Encoding.UTF8.GetBytes(formitem));
					}
				}

				// Write File Header
				bytes += WriteUploadBytes(rs, boundarybytes);
				string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
				string header = string.Format(headerTemplate, paramName, file, contentType);
				bytes += WriteUploadBytes(rs, System.Text.Encoding.UTF8.GetBytes(header));

				// Write File
				FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
				byte[] buffer = new byte[4096];
				int bytesRead = 0;
				while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
				{
					bytes += WriteUploadBytes(rs, buffer, bytesRead, false);
				}
				fileStream.Close();
				Log("FILE INSERTED HERE");

				byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
				bytes += WriteUploadBytes(rs, trailer);
			}

			try
			{
				return request.GetResponse() as HttpWebResponse;
			}
			catch (WebException we)
			{
				var response = we.Response as HttpWebResponse;
				if (response == null)
					throw;
				Log(String.Format("STATUS CODE ERROR: {0}", response.StatusCode.ToString()));
				return response;
			}
		}

		private int WriteUploadBytes(Stream rs, byte[] bytes, int length = -1, bool isLog = true)
		{
			if (length < 0)
				length = bytes.Length;

			rs.Write(bytes, 0, length);
			if (isLog)
				Log(Encoding.UTF8.GetString(bytes));

			return length;
		}

		/// <summary>
		/// Custom <see cref="System.Net.WebClient"/> that allows setting of cookies
		/// </summary>
		/// <see cref="http://www.codeproject.com/Articles/72232/C-File-Upload-with-form-fields-cookies-and-headers"/>
		private class CustomWebClient : WebClient
		{
			private CookieContainer _cookies;
			private int _timeout;

			public CustomWebClient(CookieContainer cookies = null, int timeout = HTTP_TIMEOUT_LONG)
			{
				_cookies = cookies;
				_timeout = timeout;
			}

			protected override WebRequest GetWebRequest(Uri address)
			{
				HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
				if (_cookies != null)
					request.CookieContainer = _cookies;
				request.Timeout = _timeout;
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
