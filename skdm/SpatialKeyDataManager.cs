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
		private const string LOG_SEPARATOR = "----------------------------------------";

		#region parameters

		/// <summary>Logging delegate</summary>
		public delegate void Messager(MessageLevel level, string message);

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
		public Messager MyMessenger { get; set; }

		#endregion

		#region dataMartAPI properties - set by calls

		private String _accessToken;

		#endregion

		public SpatialKeyDataManager(Messager messenger = null)
		{
			this.MyMessenger = messenger;
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
		private void ShowMessage(MessageLevel level, string message)
		{
			if (MyMessenger != null)
				MyMessenger(level, message);
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

			ShowMessage(MessageLevel.Status, "START LOGIN: " + OrganizationURL);

			_accessToken = null;


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

			ShowMessage(MessageLevel.Status, "VALIDATE TOKEN: " + _accessToken);

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

			ShowMessage(MessageLevel.Status, "LOGOUT TOKEN: " + _accessToken);

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

			ShowMessage(MessageLevel.Status, "UPLOAD: " + path);

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

		public Dictionary<string,object> GetUploadStatus(string uploadId)
		{
			if (Login() == null)
				return null;

			ShowMessage(MessageLevel.Status, "UPLOAD STATUS: " + uploadId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("upload/{0}.json", uploadId)), query))
			{
				return HttpResponseToJSON(response);
			}
		}

		public Dictionary<string,object> WaitUploadComplete(string uploadId)
		{
			ShowMessage(MessageLevel.Status, "WAIT UPLOAD COMPLETE: " + uploadId);

			DateTime start = DateTime.Now;
			Dictionary<string,object> json = GetUploadStatus(uploadId);

			while (IsUploadStatusWorking(json))
			{
				if (DateTime.Now.Subtract(start).TotalMilliseconds > HTTP_TIMEOUT_MED)
					throw new Exception("Timed out waiting for upload to complete");

				System.Threading.Thread.Sleep(10000);
				json = GetUploadStatus(uploadId);
			}
			return json;
		}

		private static readonly List<string> UPLOAD_IDLE_STATUSES = new List<string> {
			"UPLOAD_IDLE",
			"UPLOAD_CANCELED",
			"IMPORT_COMPLETE_CLEAN",
			"IMPORT_COMPLETE_WARNING"
		};

		private bool IsUploadStatusWorking(Dictionary<string,object> json)
		{
			string status = null;
			try
			{
				status = (json == null ? null : json["status"] as string);
			}
			catch (Exception)
			{
				status = null;
			}
			return !(status == null || status.IndexOf("ERROR_") == 0 || UPLOAD_IDLE_STATUSES.IndexOf(status) >= 0);
		}

		public static bool IsUploadStatusError(Dictionary<string,object> json)
		{
			string status = null;
			try
			{
				status = (json == null ? null : json["status"] as string);
			}
			catch (Exception)
			{
				status = null;
			}
			return (status == null || status.IndexOf("ERROR_") == 0);
		}

		public string GetDatasetID(Dictionary<string,object> json)
		{
			try
			{
				List<object> createdResources = json["createdResources"] as List<object>;
				foreach (Dictionary<string, object> item in createdResources)
				{
					if (item.ContainsKey("id"))
						return item["id"] as String;
				}
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		public string GetSampleImportConfiguration(string uploadId, string method)
		{
			if (Login() == null)
				return null;

			ShowMessage(MessageLevel.Status, "GET SAMPLE IMPORT CONFIG: " + uploadId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("upload/{0}/construct/{1}.xml", uploadId, method)), query))
			{
				StreamReader reader = new StreamReader(response.GetResponseStream());
				string result = reader.ReadToEnd();
				if (response.StatusCode == HttpStatusCode.OK)
				{
					return result;
				}
				else
				{
					ShowMessage(MessageLevel.Error, result);
					return null;
				}

			}
		}

		public bool Import(string uploadId, string pathConfig)
		{
			if (Login() == null)
				return false;

			ShowMessage(MessageLevel.Status, String.Format("IMPORT {0} '{1}'", uploadId, pathConfig));

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
			ShowMessage(MessageLevel.Status, String.Format("{0} uploadId:{1} datasetId: {2} config: '{3}'", method.ToUpper(), uploadId, datasetId, pathConfig));

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

		public void CancelUpload(string uploadId)
		{
			if (Login() == null)
				return;

			ShowMessage(MessageLevel.Status, "CANCEL UPLOAD: " + uploadId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("upload/{0}.json", uploadId)), query, "DELETE"))
			{
				HttpResponseToJSON(response);
			}
		}

		public List<Dictionary<string, string>> ListDatasets()
		{
			if (Login() == null)
				return null;
			
			ShowMessage(MessageLevel.Status, "LIST DATASETS");

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			using (HttpWebResponse response = HttpGet(BuildUrl("dataset.json"), query))
			{
				if (response.StatusCode == HttpStatusCode.OK)
				{
					Dictionary<string, object> json = HttpResponseToJSON(response);
					List<object> items = json["value"] as List<object>;
					List<Dictionary<string, string>> retList = new List<Dictionary<string, string>>();
					foreach (Dictionary<string, object> item in items)
					{
						Dictionary<string, string> cur = new Dictionary<string, string>();
						cur["ID"] = item["id"].ToString();
						cur["Label"] = item["label"].ToString();
						//cur["Description"] = item["description"].ToString();
						cur["Created"] = FromUnixTime(Convert.ToInt64(item["created"].ToString())).ToString();
						cur["Modified"] = FromUnixTime(Convert.ToInt64(item["modified"].ToString())).ToString();
						cur["Geometry Type"] = item["geometryType"].ToString();
						cur["Total Rows"] = item["totalRows"].ToString();
						retList.Add(cur);
					}
					return retList;
				}
				else
					return null;
			}
		}

		private static DateTime FromUnixTime(long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddMilliseconds(unixTime);
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
				string result = reader.ReadToEnd().Trim();
				if (!result.StartsWith("{") && !result.EndsWith("}"))
					result = "{\"value\": " + result + "}";
				ShowMessage(MessageLevel.Verbose, "RESULT: " + result);
				json = MiniJson.Deserialize(result) as Dictionary<string, object>;
			}
			return json;
		}

		private HttpWebResponse HttpGet(string url, NameValueCollection queryParam = null, string method = "GET", int timeout = HTTP_TIMEOUT_SHORT)
		{
			url = url + ToQueryString(queryParam);
			ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			ShowMessage(MessageLevel.Verbose, String.Format("HTTP GET: {0}", url));

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
				ShowMessage(MessageLevel.Verbose, String.Format("STATUS CODE ERROR: {0}", response.StatusCode.ToString()));
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

			ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			ShowMessage(MessageLevel.Verbose, String.Format("HTTP POST URL: {0} PARAM: {1}", url, body));

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
				ShowMessage(MessageLevel.Verbose, String.Format("STATUS CODE ERROR: {0}", response.StatusCode.ToString()));
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

			ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			ShowMessage(MessageLevel.Verbose, String.Format("HTTP POST URL: {0} XML: {1}", url, pathXML));

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
				ShowMessage(MessageLevel.Verbose, String.Format("STATUS CODE ERROR: {0}", response.StatusCode.ToString()));
				return response;
			}
		}

		private HttpWebResponse HttpUploadFile(string url, string file, string paramName, string contentType, NameValueCollection queryParam, NameValueCollection bodyParam, string method = "POST", int timeout = HTTP_TIMEOUT_LONG)
		{
			url = url + ToQueryString(queryParam);
			ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			ShowMessage(MessageLevel.Verbose, string.Format("HTTP UPLOAD {0} to {1}", file, url));
			string boundary = String.Format("-----------{0:N}", Guid.NewGuid());
			byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
			request.ContentType = "multipart/form-data; boundary=" + boundary;
			request.Method = method;
			request.KeepAlive = true;
			request.Timeout = timeout;
			//request.CookieContainer = new CookieContainer();

			ShowMessage(MessageLevel.Verbose, "Content-Type: " + request.ContentType);
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
				ShowMessage(MessageLevel.Verbose, "FILE INSERTED HERE");

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
				ShowMessage(MessageLevel.Verbose, String.Format("STATUS CODE ERROR: {0}", response.StatusCode.ToString()));
				return response;
			}
		}

		private int WriteUploadBytes(Stream rs, byte[] bytes, int length = -1, bool isLog = true)
		{
			if (length < 0)
				length = bytes.Length;

			rs.Write(bytes, 0, length);
			if (isLog)
				ShowMessage(MessageLevel.Verbose, Encoding.UTF8.GetString(bytes));

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
		public static string GetTempFile(string fileExtension, string prefix = "", string path = null)
		{
			if (path == null)
				path = System.IO.Path.GetTempPath();
			string filename = string.Empty;
			while (true)
			{
				filename = string.Format("{0}{1}.{2}", prefix, Guid.NewGuid().ToString(), fileExtension);
				filename = System.IO.Path.Combine(path, filename);
				if (!System.IO.File.Exists(filename))
				{
					try
					{
						System.IO.FileStream s = System.IO.File.Create(filename);
						s.Close();
						break;
					}
					catch (Exception)
					{
						
					}
				}
			}
			return filename;
		}

		#endregion

	}
}
