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

		#region constants

		public const string API_VERSION = "v2";
		// 1 min * 60 sec * 1000 msec = 60000 msec
		private const int HTTP_TIMEOUT_SHORT = 60000;
		// 15 min * 60 sec * 1000 msec = 900000 msec
		private const int HTTP_TIMEOUT_MED = 900000;
		// 60 min * 60 sec * 1000 msec = 3600000 msec
		private const int HTTP_TIMEOUT_LONG = 3600000;
		private const string LOG_SEPARATOR = "----------------------------------------";
		private const int TOKEN_TIMEOUT = 1800;

		#endregion

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

		/// <summary>
		/// Init the manager.  If the organizationURL, organizationAPIKey, organizationSecretKey or userAPIKey
		/// have changed the application will logout.
		/// </summary>
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
		/// Log the specified message if the <see cref="MyMessenger"/> is set
		/// </summary>
		private void ShowMessage(MessageLevel level, string message)
		{
			if (MyMessenger != null)
				MyMessenger(level, message);
		}

		#region API calls

		/// <summary>
		/// If don't have a valid login token, login to the API and get one.
		/// </summary>
		public string Login()
		{
			if (IsLoginTokenValid())
				return _accessToken;

			ShowMessage(MessageLevel.Status, "START LOGIN: " + OrganizationURL);
			_accessToken = null;

			string oauth = OAuth.GetOAuthToken(UserAPIKey, OrganizationAPIKey, OrganizationSecretKey, TOKEN_TIMEOUT);
			// add the query string
			NameValueCollection bodyParam = new NameValueCollection();
			bodyParam["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";
			bodyParam["assertion"] = oauth;

			try
			{
				using (HttpWebResponse response = HttpPost(BuildUrl("oauth.json"), null, bodyParam))
				{
					Dictionary<string,object> json = HttpResponseToJSON(response);
					_accessToken = JsonGetPath<string>(json, "access_token");
					if (_accessToken == null)
						throw new Exception(String.Format("JSON does not contian 'access_token': {0}", MiniJson.Serialize(json)));
					return _accessToken;
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Unable to login to '{0}' using oAuth token '{1}'", OrganizationURL, oauth), ex);
				return null;
			}
		}

		/// <summary>
		/// Return true if the _accessToken is valid
		/// </summary>
		public bool IsLoginTokenValid()
		{
			if (_accessToken == null || _accessToken.Length == 0)
				return false;

			ShowMessage(MessageLevel.Status, "VALIDATE TOKEN: " + _accessToken);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl("oauth.json"), query))
				{
					Dictionary<string,object> json = HttpResponseToJSON(response);
					return json != null; // TODO check what valid json should contain as this throws an error for invalid response or json already
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to validate token '{0}'", _accessToken), ex);
				return false;
			}
		}

		/// <summary>
		/// Logout and clear the _accessToken
		/// </summary>
		public void Logout()
		{
			if (_accessToken == null || _accessToken.Length == 0)
				return;

			ShowMessage(MessageLevel.Status, "LOGOUT TOKEN: " + _accessToken);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl("oauth.json"), query, "DELETE"))
				{
					HttpResponseToJSON(response);
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to logout with token '{0}'", _accessToken), ex);
			}
			finally
			{
				_accessToken = null;
			}
		}

		/// <summary>
		/// Upload the given file and return the uploadId
		/// </summary>
		public string Upload(string[] paths)
		{
			if (Login() == null)
				return null;

			ShowMessage(MessageLevel.Status, "UPLOAD: " + paths);

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpUploadFiles(BuildUrl("upload.json"), paths, "file", "application/octet-stream", query, null))
				{
					Dictionary<string,object> json = HttpResponseToJSON(response);
					string uploadId = JsonGetPath<string>(json, "upload/uploadId");
					if (uploadId == null)
						throw new Exception(String.Format("JSON does not contian '{0}': {1}", "upload/uploadId", MiniJson.Serialize(json)));
					return uploadId;
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to upload '{0}'", paths), ex);
				return null;
			}
		}

		/// <summary>
		/// Return the json status for the given uploadId
		/// </summary>
		public Dictionary<string,object> GetUploadStatus(string uploadId)
		{
			if (Login() == null)
				return null;

			ShowMessage(MessageLevel.Status, "UPLOAD STATUS: " + uploadId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("upload/{0}.json", uploadId)), query))
				{
					return HttpResponseToJSON(response);
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to get upload status for '{0}'", uploadId), ex);
				return null;
			}
		}

		/// <summary>
		/// Get information about a dataset
		/// </summary>
		public Dictionary<string,object> GetDatasetInfo(string datasetId)
		{
			if (Login() == null || datasetId == null || datasetId.Length < 1)
				return null;

			ShowMessage(MessageLevel.Status, "Dataset Info: " + datasetId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("dataset/{0}.json", datasetId)), query))
				{
					return HttpResponseToJSON(response);
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to get dataset information for '{0}'", datasetId), ex);
				return null;
			}
		}

		/// <summary>
		/// Waits the upload complete and returns the final status json
		/// </summary>
		public Dictionary<string,object> WaitUploadComplete(string uploadId)
		{
			ShowMessage(MessageLevel.Status, "WAIT UPLOAD COMPLETE: " + uploadId);

			DateTime start = DateTime.Now;
			Dictionary<string,object> json = GetUploadStatus(uploadId);

			while (IsUploadStatusWorking(json))
			{
				if (DateTime.Now.Subtract(start).TotalMilliseconds > HTTP_TIMEOUT_MED)
					throw new Exception(String.Format("Timed out waiting for upload '{0}' to complete", uploadId));

				System.Threading.Thread.Sleep(10000);
				json = GetUploadStatus(uploadId);
			}
			return json;
		}

		/// <summary>
		/// Gets the sample import configuration.
		/// </summary>
		public string GetSampleImportConfiguration(string uploadId, string method)
		{
			if (Login() == null)
				return null;

			ShowMessage(MessageLevel.Status, "GET SAMPLE IMPORT CONFIG: " + uploadId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("upload/{0}/construct/{1}.xml", uploadId, method)), query))
				{
					return GetResponseString(response);
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to get sample import config '{0}'", uploadId), ex);
				return null;
			}
		}

		/// <summary>
		/// Import the uploadId as a new dataset with the given config
		/// </summary>
		public bool Import(string uploadId, string pathConfig)
		{
			if (Login() == null)
				return false;

			ShowMessage(MessageLevel.Status, String.Format("IMPORT {0} with config'{1}'", uploadId, pathConfig));

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpPostXml(BuildUrl(string.Format("upload/{0}/dataset.json", uploadId)), pathConfig, query))
				{
					HttpResponseToJSON(response);  // TODO should the json be examined for success?
					return true; 
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to import {0} with config '{1}'", uploadId, pathConfig), ex);
				return false;
			}
		}

		/// <summary>
		/// Append the uploadId to the datasetId
		/// </summary>
		public bool Append(string uploadId, string datasetId, string pathConfig)
		{
			return AppendOrOverwrite(uploadId, datasetId, pathConfig, "Append");
		}

		/// <summary>
		/// Overwrite the datasetId with datasetId
		/// </summary>
		public bool Overwrite(string uploadId, string datasetId, string pathConfig)
		{
			return AppendOrOverwrite(uploadId, datasetId, pathConfig, "Overwrite");
		}

		/// <summary>
		/// Append or overwrite datasetId with uploadId
		/// </summary>
		private bool AppendOrOverwrite(string uploadId, string datasetId, string pathConfig, string method)
		{
			if (Login() == null)
				return false;
			ShowMessage(MessageLevel.Status, String.Format("{0} uploadId:{1} datasetId: {2} config: '{3}'", method.ToUpper(), uploadId, datasetId, pathConfig));

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;
			query["method"] = method;

			try
			{
				using (HttpWebResponse response = HttpPostXml(BuildUrl(string.Format("upload/{0}/dataset/{1}.json", uploadId, datasetId)), pathConfig, query))
				{
					HttpResponseToJSON(response);  // TODO should the json be examined for success?
					return true;
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed {0} uploadId:{1} datasetId: {2} config: '{3}'", method.ToUpper(), uploadId, datasetId, pathConfig), ex);
				return false;
			}
		}

		/// <summary>
		/// Cancel the given uploadId
		/// </summary>
		public void CancelUpload(string uploadId)
		{
			if (Login() == null)
				return;

			ShowMessage(MessageLevel.Status, "CANCEL UPLOAD: " + uploadId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("upload/{0}.json", uploadId)), query, "DELETE"))
				{
					HttpResponseToJSON(response);  // TODO should the json be examined for success?
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to cancel upload {0}" + uploadId), ex);
			}
		}

		/// <summary>
		/// Get a list of all the datasets the authenticated user has access to
		/// </summary>
		public List<Dictionary<string, string>> ListDatasets()
		{
			if (Login() == null)
				return null;
			
			ShowMessage(MessageLevel.Status, "LIST DATASETS");

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl("dataset.json"), query))
				{
					Dictionary<string, object> json = HttpResponseToJSON(response);
					List<object> items = JsonGetPath<List<object>>(json, "value");
					if (items == null)
						throw new Exception(String.Format("Dataset List JSON did not contain value array: {0}", json == null ? "null" : MiniJson.Serialize(json)));

					List<Dictionary<string, string>> retList = new List<Dictionary<string, string>>();
					foreach (Dictionary<string, object> item in items)
					{
						if (!item.ContainsKey("id") ||
						    !item.ContainsKey("label") ||
						    !item.ContainsKey("created") ||
						    !item.ContainsKey("modified") ||
						    !item.ContainsKey("geometryType") ||
						    !item.ContainsKey("totalRows"))
						{
							ShowJSON(MessageLevel.Error, "List dataset found item with incorrect data", json);
							continue;
						}
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
			}
			catch (Exception ex)
			{
				ShowException("Error Processing Dataset List", ex);
				return null;
			}
		}

		/// <summary>
		/// Deletes the datasetId
		/// </summary>
		public void DeleteDataset(string datasetId)
		{
			if (Login() == null)
				return;

			ShowMessage(MessageLevel.Status, "DELETE DATASET: " + datasetId);

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl(string.Format("dataset/{0}.json", datasetId)), query, "DELETE"))
				{
					HttpResponseToJSON(response);  // TODO should the json be examined for success?
				}
			}
			catch (Exception ex)
			{
				ShowException("Failed to delete dataset " + datasetId, ex);
				return;
			}
		}

		#endregion

		#region JSON helpers

		/// <summary>
		/// Return true if the upload json status is currently in progress.  False if done or error.
		/// </summary>
		public bool IsUploadStatusWorking(Dictionary<string,object> json)
		{
			// TODO should this throw error?
			string status = JsonGetPath<string>(json, "status");
			if (status == null)
				ShowMessage(MessageLevel.Error, String.Format("Upload status JSON not valid: {0}", json == null ? "null" : MiniJson.Serialize(json)));
			return !(status == null || status.IndexOf("ERROR_") == 0 || UPLOAD_IDLE_STATUSES.IndexOf(status) >= 0);
		}

		private static readonly List<string> UPLOAD_IDLE_STATUSES = new List<string> {
			"UPLOAD_IDLE",
			"UPLOAD_CANCELED",
			"IMPORT_COMPLETE_CLEAN",
			"IMPORT_COMPLETE_WARNING"
		};

		/// <summary>
		/// Return true if the upload json status is an error
		/// </summary>
		public bool IsUploadStatusError(Dictionary<string,object> json)
		{
			// TODO should this throw error?
			string status = JsonGetPath<string>(json, "status");
			if (status == null)
				ShowMessage(MessageLevel.Error, String.Format("Upload status JSON not valid: {0}", json == null ? "null" : MiniJson.Serialize(json)));
			return (status == null || status.IndexOf("ERROR_") == 0);
		}

		/// <summary>
		/// Finds the datasetId for the first createdResources that has an id in the given JSON
		/// </summary>
		public string GetDatasetID(Dictionary<string,object> json)
		{
			// TODO should this throw error?
			List<object> createdResources = JsonGetPath<List<object>>(json, "createdResources");
			if (createdResources == null)
			{
				ShowMessage(MessageLevel.Error, String.Format("Upload status JSON did not contain 'createdResources': {0}", json == null ? "null" : MiniJson.Serialize(json)));
				return null;
			}
			foreach (Dictionary<string, object> item in createdResources)
			{
				string id = JsonGetPath<string>(item, "id");
				if (id != null)
					return id;
			}
			return null;
		}

		/// <summary>
		/// Get json value at given path.  Null if it doesn't exist or can't be converted to type
		/// </summary>
		private static T JsonGetPath<T>(Dictionary<string,object> json, string path)
		{
			return JsonGetPath<T>(json, path != null ? path.Split('/') : null);
		}

		private static T JsonGetPath<T>(Dictionary<string,object> json, string[] path)
		{
			if (json == null || path == null || path.Length < 1)
				return default(T);

			Queue<string> queue = new Queue<string>(path);
			string key = queue.Dequeue();
			if (!json.ContainsKey(key))
				return default(T);

			if (queue.Count < 1)
				return (T)Convert.ChangeType(json[key], typeof(T));
			else
				return JsonGetPath<T>(json[key] as Dictionary<string,object>, queue.ToArray());
		}

		/// <summary>
		/// Get json value at given path.  Null if it doesn't exist or can't be converted to type
		/// </summary>
		private void ShowJSON(MessageLevel level, string message, Dictionary<string,object> json)
		{
			ShowMessage(level, String.Format("{0}: {1}", message, json != null ? MiniJson.Serialize(json) : "null"));
		}

		/// <summary>
		/// Convert http response to json or throw error if fail
		/// </summary>
		private Dictionary<string, object> HttpResponseToJSON(HttpWebResponse response)
		{
			string result = GetResponseString(response);
			Dictionary<string, object> json = MiniJson.Deserialize(result) as Dictionary<string, object>;
			if (json == null)
				throw new Exception(String.Format("Unable to parse JSON: {0}", result));
			return json;
		}

		#endregion

		#region HTTP/Web helpers

		private static DateTime FromUnixTime(long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddMilliseconds(unixTime).ToLocalTime();
		}

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

		private string GetResponseString(HttpWebResponse response)
		{
			using (response)
			{
				StreamReader reader = new StreamReader(response.GetResponseStream());
				string result = reader.ReadToEnd().Trim();
				ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
				ShowMessage(MessageLevel.Verbose, "RESULT: " + result);
				return result;
			}
		}

		private void ShowException(string message, Exception ex)
		{
			if (ex is WebException)
			{
				WebException we = ex as WebException;
				HttpWebResponse response = we != null ? we.Response as HttpWebResponse : null;
				if (response == null)
					ShowMessage(MessageLevel.Error, String.Format("{0}: {1}", message, ex.Message));
				else
					ShowMessage(MessageLevel.Error, String.Format("{0}: StatusCode:  {1}; {2}", message, response.StatusCode, GetResponseString(response)));
			}
			else
				ShowMessage(MessageLevel.Error, String.Format("{0}: {1}", message, ex.Message));
		}

		private HttpWebResponse HttpGet(string url, NameValueCollection queryParam = null, string method = "GET", int timeout = HTTP_TIMEOUT_SHORT)
		{
			url = url + ToQueryString(queryParam);
			ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			ShowMessage(MessageLevel.Verbose, String.Format("HTTP GET: {0}", url));

			HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
			request.Method = method;
			request.Timeout = timeout;
			return request.GetResponse() as HttpWebResponse;
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

			return request.GetResponse() as HttpWebResponse;
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

			return request.GetResponse() as HttpWebResponse;
		}

		private HttpWebResponse HttpUploadFiles(string url, string[] files, string paramName, string contentType, NameValueCollection queryParam, NameValueCollection bodyParam, string method = "POST", int timeout = HTTP_TIMEOUT_LONG)
		{
			url = url + ToQueryString(queryParam);
			ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			ShowMessage(MessageLevel.Verbose, string.Format("HTTP UPLOAD {0} to {1}", files, url));
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

				foreach (string file in files)
				{
					// Write File Header
					bytes += WriteUploadBytes(rs, boundarybytes);
					string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
					string header = string.Format(headerTemplate, paramName, Path.GetFileName(file), contentType);
					bytes += WriteUploadBytes(rs, System.Text.Encoding.UTF8.GetBytes(header));

					// Write File
					using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
					{
						byte[] buffer = new byte[4096];
						int bytesRead = 0;
						while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
						{
							bytes += WriteUploadBytes(rs, buffer, bytesRead, false);
						}
					}

					ShowMessage(MessageLevel.Verbose, "FILE INSERTED HERE");
				}

				byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
				bytes += WriteUploadBytes(rs, trailer);
			}

			return request.GetResponse() as HttpWebResponse;
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

		#endregion

		#region Helpers

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
				filename = string.Format("{0}{1}.{2}", prefix, Guid.NewGuid().GetHashCode().ToString(), fileExtension);
				filename = Path.Combine(path, filename);
				if (!File.Exists(filename))
				{
					try
					{
						using (FileStream stream = File.Create(filename))
						{
						}
						FileInfo fileInfo = new FileInfo(filename);
						fileInfo.Attributes = FileAttributes.Temporary;
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
