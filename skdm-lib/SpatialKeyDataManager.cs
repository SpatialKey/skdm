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

using SpatialKey.DataManager.Lib.Message;
using SpatialKey.DataManager.Lib.Config;
using SpatialKey.DataManager.Lib.Helpers;

// Using http://www.icsharpcode.net/opensource/sharpziplib/
namespace SpatialKey.DataManager.Lib
{
	/// <summary>
	/// The SpatialKey Data Import API (DIAPI) allows developers to programmatically create or update SpatialKey datasets.
	/// </summary>
	/// <see cref="http://support.spatialkey.com/dmapi"/>
	public class SpatialKeyDataManager : BaseMessageClass, IDisposable
	{

		#region constants

		public const string VERSION = "3.0.0";

		public const string API_VERSION = "v2";
		// 60 min * 60 sec * 1000 msec = 3600000 msec
		private const int HTTP_TIMEOUT_MED = 3600000;
		// 4 hrs * 60 min * 60 sec * 1000 msec = 14400000 msec
        private const int HTTP_TIMEOUT_LONG = 14400000;
		private const string LOG_SEPARATOR = "----------------------------------------";
		private const string ROUTEID = "ROUTEID";

		#endregion

		#region parameters

		/// <summary>Authenticaton Configuration</summary>
		public IAuthConfig MyConfigAuth { get; private set; }
        private int _tokenTimeout = 1800;
        public int TokenTimeout 
        { 
            get { return _tokenTimeout; }
            set { _tokenTimeout = value; }
        }

        private string _userAgent = string.Format("SpatialKeyDataManager/{0}", VERSION);
        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

		#endregion

		#region dataMartAPI properties - set by calls

		private String _accessToken;
		private String _routeId;

		#endregion

		public SpatialKeyDataManager(Messager messenger = null) : base(messenger)
		{
		}

		static public String GetVersion()
		{
			return VERSION;
		}

		/// <summary>
		/// Init the manager.  If the organizationURL, organizationAPIKey, organizationSecretKey or userAPIKey
		/// have changed the application will logout.
		/// </summary>
		public void Init(IAuthConfig configAuth)
		{
			// if the setup is the same, bail
			if (configAuth == MyConfigAuth)
				return;

			Logout();

			_accessToken = null;
			_routeId = null;

            MyConfigAuth = configAuth == null ? null : configAuth.Clone() as IAuthConfig;

            if (MyConfigAuth != null && 
                MyConfigAuth.OrganizationUrl != null &&
                !MyConfigAuth.OrganizationUrl.Contains("://"))
            {
                MyConfigAuth.OrganizationUrl = "https://"+MyConfigAuth.OrganizationUrl;
            }
		}

        public void Dispose()
        {
            Logout();
            _accessToken = null;
            _routeId = null;
            MyConfigAuth = null;
        }

		#region API calls - Common

		/// <summary>
		/// If don't have a valid login token, login to the API and get one.
		/// </summary>
		public string Login(string routeId = null)
		{
			if (IsLoginTokenValid())
				return _accessToken;

			ShowMessage(MessageLevel.Status, "START LOGIN: " + MyConfigAuth.OrganizationUrl);
			_accessToken = null;

            // use passed in routeId or keep the current routeId just in case we are logging in because IsLoginTokenValid() failed
            _routeId = string.IsNullOrWhiteSpace(routeId) ? _routeId : routeId; 

			string oauth = OAuth.GetOAuthToken(MyConfigAuth.UserApiKey, MyConfigAuth.OrganizationApiKey, MyConfigAuth.OrganizationSecretKey, TokenTimeout);
			// add the query string
			NameValueCollection bodyParam = new NameValueCollection();
			bodyParam["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer";
			bodyParam["assertion"] = oauth;

			try
			{
				using (HttpWebResponse response = HttpPost(BuildUrl("oauth.json"), null, bodyParam))
				{
                    if (response.Cookies != null && response.Cookies[ROUTEID] != null)
                    {
                        _routeId = response.Cookies[ROUTEID].Value;
                    }

					Dictionary<string,object> json = HttpResponseToJSON(response);
					_accessToken = JsonGetPath<string>(json, "access_token");
					if (string.IsNullOrWhiteSpace(_accessToken))
						throw new Exception(String.Format("JSON does not contian 'access_token': {0}", MiniJson.Serialize(json)));
					return _accessToken;
				}
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("Unable to login to '{0}' using oAuth token: {1}", MyConfigAuth.OrganizationUrl, FormatException(ex)), ex);
			}
		}

		/// <summary>
		/// Return true if the _accessToken is valid
		/// </summary>
		public bool IsLoginTokenValid()
		{
			if (string.IsNullOrWhiteSpace(_accessToken))
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
				ShowMessage(MessageLevel.Warning, String.Format("Failed to validate token '{0}': {1}", _accessToken, ex.Message));
				return false;
			}
		}

		/// <summary>
		/// Logout and clear the _accessToken
		/// </summary>
		public void Logout()
		{
            if (string.IsNullOrWhiteSpace(_accessToken))
            {
                return;
            }

            if (!IsLoginTokenValid())
            {
                _accessToken = null;
                _routeId = null;
                return;
            }

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
				_routeId = null;
			}
		}

        /// <summary>
        /// Relogin
        /// </summary>
        public void Relog()
        {
            string routeId = _routeId;
            Logout();
            Login(routeId);
            if (!string.IsNullOrWhiteSpace(routeId) && routeId != _routeId)
            {
                throw new Exception(string.Format("Error: New ROUTEID {0} not the same as old {1}", _routeId, routeId));
            }
        }

        /// <summary>
        /// Get information about the organization
        /// </summary>
        public Dictionary<string, object> GetOrganizationInformation()
	    {
            Login();

	        using (new MessageTimeTaken(MyMessenger, "Get Organization Information"))
	        {
                NameValueCollection query = new NameValueCollection();
                query["token"] = _accessToken;

                try
                {
                    using (HttpWebResponse response = HttpGet(BuildUrl("organization"), query))
                    {
                        return HttpResponseToJSON(response);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(String.Format("Failed to get organization information. {0}", FormatException(ex)), ex);
                }
            }

	    }

        /// <summary>
		/// Upload the given file and return the uploadId
		/// </summary>
		public string Upload(string[] paths)
		{
			Login();

			ShowMessage(MessageLevel.Status, "UPLOAD: " + String.Join(", ", paths));

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpUploadFiles(BuildUrl("upload.json"), paths, "file", "application/octet-stream", query, null))
				{
					Dictionary<string,object> json = HttpResponseToJSON(response);
					string uploadId = JsonGetPath<string>(json, "upload/uploadId");
					if (string.IsNullOrWhiteSpace(uploadId))
						throw new Exception(String.Format("JSON does not contian '{0}': {1}", "upload/uploadId", MiniJson.Serialize(json)));
					return uploadId;
				}
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("Failed to upload '{0}'. {1}", paths, FormatException(ex)), ex);
			}
		}

		/// <summary>
		/// Return the json status for the given uploadId
		/// </summary>
		public Dictionary<string,object> GetUploadStatus(string uploadId)
		{
			Login();

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
				throw new Exception(String.Format("Failed to get upload status for '{0}'. {1}", uploadId, FormatException(ex)), ex);
			}
		}

		/// <summary>
		/// Gets the sample import configuration.
		/// </summary>
		public string GetSampleConfiguration(string uploadId, string method)
		{
			Login();

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
				throw new Exception(String.Format("Failed to get sample import config '{0}'. {1}", uploadId, FormatException(ex)), ex);
			}
		}

		/// <summary>
		/// Cancel the given uploadId
		/// </summary>
		public void CancelUpload(string uploadId)
		{
            Login();

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

		#endregion // API calls - Common

		#region API calls - Dataset

		/// <summary>
		/// Get information about a dataset
		/// </summary>
		public Dictionary<string,object> GetDatasetInfo(string datasetId)
		{
			Login();
			if (string.IsNullOrWhiteSpace(datasetId))
				throw new Exception("Need to give datasetId to get info.");

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
				throw new Exception(String.Format("Failed to get dataset information for '{0}'. {1}", datasetId, FormatException(ex)), ex);
			}
		}

		/// <summary>
		/// Import the uploadId as a new dataset with the given config
		/// </summary>
		public bool DatasetCreate(string uploadId, string pathConfig)
		{
			Login();

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
				ShowException(String.Format("Failed to import dataset {0} with config '{1}'", uploadId, pathConfig), ex);
				return false;
			}
		}

		/// <summary>
		/// Append the uploadId to the datasetId
		/// </summary>
		public bool DatasetAppend(string uploadId, string datasetId, string pathConfig)
		{
			return DatasetAppendOrOverwrite(uploadId, datasetId, pathConfig, "Append");
		}

		/// <summary>
		/// Overwrite the datasetId with datasetId
		/// </summary>
		public bool DatasetOverwrite(string uploadId, string datasetId, string pathConfig)
		{
			return DatasetAppendOrOverwrite(uploadId, datasetId, pathConfig, "Overwrite");
		}

		/// <summary>
		/// Append or overwrite datasetId with uploadId
		/// </summary>
		private bool DatasetAppendOrOverwrite(string uploadId, string datasetId, string pathConfig, string method)
		{
            if (string.IsNullOrWhiteSpace(Login())) return false;

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
		/// Get a list of all the datasets the authenticated user has access to
		/// </summary>
		public List<Dictionary<string, string>> DatasetList()
		{
            Login();

			ShowMessage(MessageLevel.Status, "LIST DATASETS");

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl("dataset.json"), query))
				{
					Dictionary<string, object> json = HttpResponseToJSON(response);
					return ParseListJson(json, "Dataset");
				}
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("Error Processing Dataset List", FormatException(ex)), ex);
			}
		}

		/// <summary>
		/// Deletes the datasetId
		/// </summary>
		public void DatasetDelete(string datasetId)
		{
            Login();

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

		#endregion // API calls - Dataset

		#region API calls - Insurance

		/// <summary>
		/// Create uploadId for insurance from existing datasets
		/// </summary>
		public string InsuranceCreateExistingDatasets(string pathConfig)
		{
            Login();

			ShowMessage(MessageLevel.Status, "INSURANCE CREATE FROM DATASETS: " + pathConfig);

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpPostXml(BuildUrl("upload/insurance.json"), pathConfig, query))
				{
					Dictionary<string,object> json = HttpResponseToJSON(response);
					string uploadId = JsonGetPath<string>(json, "upload/uploadId");
					if (string.IsNullOrWhiteSpace(uploadId))
						throw new Exception(String.Format("JSON does not contian '{0}': {1}", "upload/uploadId", MiniJson.Serialize(json)));
					return uploadId;
				}
			}
			catch (Exception ex)
			{
				throw new Exception(String.Format("Failed create from datasets '{0}'. {1}", pathConfig, FormatException(ex)), ex);
			}
		}

		/// <summary>
		/// Import the uploadId as a new dataset with the given config
		/// </summary>
		public bool InsuranceCreate(string uploadId, string[] pathDataArray, string pathConfig)
		{
            Login();

			ShowMessage(MessageLevel.Status, String.Format("IMPORT {0} with config'{1}'", uploadId, pathConfig));

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpPostXml(BuildUrl(string.Format("upload/{0}/insurance.json", uploadId)), pathConfig, query))
				{
					HttpResponseToJSON(response);  // TODO should the json be examined for success?
					return true; 
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed to import insurance {0} with config '{1}'", uploadId, pathConfig), ex);
				return false;
			}
		}

		public bool InsuranceOverwrite(string uploadId, string datasetId, string pathConfig)
		{
            Login();
            
            ShowMessage(MessageLevel.Status, String.Format("Overwrite uploadId:{0} datasetId: {1} config: '{2}'", uploadId, datasetId, pathConfig));

			// add the query string
			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpPostXml(BuildUrl(string.Format("upload/{0}/insurance/overwrite.json", uploadId, datasetId)), pathConfig, query))
				{
					HttpResponseToJSON(response);  // TODO should the json be examined for success?
					return true;
				}
			}
			catch (Exception ex)
			{
				ShowException(String.Format("Failed overwrite uploadId:{0} datasetId: {1} config: '{2}'", uploadId, datasetId, pathConfig), ex);
				return false;
			}
		}

		/// <summary>
		/// Get a list of all the insurance datasets the authenticated user has access to
		/// </summary>
		public List<Dictionary<string, string>> InsuranceList()
		{
			Login();

			ShowMessage(MessageLevel.Status, "LIST INSURANCE");

			NameValueCollection query = new NameValueCollection();
			query["token"] = _accessToken;

			try
			{
				using (HttpWebResponse response = HttpGet(BuildUrl("insurance.json"), query))
				{
					Dictionary<string, object> json = HttpResponseToJSON(response);
					return ParseListJson(json, "Insurance");
				}
			}
			catch (Exception ex)
			{
				ShowException("Error Processing Insurance List", ex);
				return null;
			}
		}

		#endregion // API calls - Insurance

		#region API calls - Composite

		/// <summary>
		/// Waits the upload complete and returns the final status json
		/// </summary>
		public Dictionary<string,object> WaitUploadComplete(string uploadId)
		{
			ShowMessage(MessageLevel.Status, "WAIT UPLOAD COMPLETE: " + uploadId);

            Dictionary<string,object> json = null;
            do
            {
                System.Threading.Thread.Sleep(10000);
                json = GetUploadStatus(uploadId);
            } while (IsUploadStatusWorking(json));

            return json;
		}

		public string UploadAndWait(string[] paths)
		{
			if (paths == null || paths.Length < 1)
				return null;

			string uploadId = Upload(paths);
			Dictionary<string, object> uploadStausJson = WaitUploadComplete(uploadId);
			if (IsUploadStatusError(uploadStausJson))
				throw new Exception(String.Format("Upload failed: {1}", MiniJson.Serialize(uploadStausJson)));
			return uploadId;
		}

		#endregion // API calls - Composite

		#region JSON helpers

		/// <summary>
		/// Parse value list of datasets or insurance
		/// </summary>
		private List<Dictionary<string, string>> ParseListJson(Dictionary<string, object> json, string type)
		{
			List<object> items = JsonGetPath<List<object>>(json, "value");
			if (items == null)
				throw new Exception(String.Format("Dataset List JSON did not contain value array: {0}", json == null ? "null" : MiniJson.Serialize(json)));

			List<Dictionary<string, string>> retList = new List<Dictionary<string, string>>();
			foreach (Dictionary<string, object> item in items)
			{
				if (!item.ContainsKey("id") || !item.ContainsKey("label"))
				{
					ShowJSON(MessageLevel.Error, "List dataset found item with incorrect data", json);
					continue;
				}
				Dictionary<string, string> cur = new Dictionary<string, string>();
				cur["ID"] = item["id"].ToString();
				cur["Label"] = item["label"].ToString();
				//if (item.ContainsKey("description"))
				//	cur["Description"] = item["description"].ToString();
				if (item.ContainsKey("created"))
					cur["Created"] = FromUnixTime(Convert.ToInt64(item["created"].ToString())).ToString();
				if (item.ContainsKey("modified"))
					cur["Modified"] = FromUnixTime(Convert.ToInt64(item["modified"].ToString())).ToString();
				if (item.ContainsKey("geometryType"))
					cur["Geometry Type"] = item["geometryType"].ToString();
				if (item.ContainsKey("totalRows"))
					cur["Total Rows"] = item["totalRows"].ToString();
				cur["Type"] = type;
				retList.Add(cur);
			}
			return retList;
		}

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
		public Dictionary<string,string> GetDatasetIDs(Dictionary<string,object> json)
		{
			List<object> createdResources = JsonGetPath<List<object>>(json, "createdResources");
			if (createdResources == null)
			{
				throw new Exception(String.Format("Upload status JSON did not contain 'createdResources': {0}", json == null ? "null" : MiniJson.Serialize(json)));
			}
			Dictionary<string,string> dict = new Dictionary<string,string>();
			foreach (Dictionary<string, object> item in createdResources)
			{
				string id = JsonGetPath<string>(item, "id");
				string type = JsonGetPath<string>(item, "type");
				if (id == null)
					continue;
				if (type == null)
					type = "unknown";
				dict[id] = type;
			}
			return dict;
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

		#endregion // JSON helpers

		#region HTTP/Web helpers

		private static DateTime FromUnixTime(long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddMilliseconds(unixTime).ToLocalTime();
		}

		private string BuildUrl(string command, NameValueCollection query = null)
		{
			UriBuilder uri = new UriBuilder(MyConfigAuth.OrganizationUrl);
			uri.Path = String.Format("/SpatialKeyFramework/api/{0}/{1}", API_VERSION, command);
			return uri.ToString() + ToQueryString(query);
		}

		private string ToQueryString(NameValueCollection nvc)
		{
			List<string> list = new List<string>();

            // add _routeId
            if (!string.IsNullOrWhiteSpace(_routeId))
            {
                list.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(ROUTEID), HttpUtility.UrlEncode(_routeId)));
            }

            if (nvc != null && nvc.Count > 0)
            {
                foreach (string key in nvc)
                {
                    list.Add(string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(nvc[key])));
                }
            }

            return list.Count > 0 ? "?" + string.Join("&", list) : "";
		}

		private string GetResponseString(HttpWebResponse response)
		{
		    StreamReader reader = new StreamReader(response.GetResponseStream());
		    string result = reader.ReadToEnd().Trim();
		    ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
		    ShowMessage(MessageLevel.Verbose, "RESULT: " + result);
		    ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
		    return result;
		}

		private void ShowException(string message, Exception ex)
		{
			ShowMessage(MessageLevel.Error, String.Format("{0}: {1}", message, FormatException(ex)));
		}

		private string FormatException(Exception ex)
		{
			if (ex is WebException)
			{
				WebException we = ex as WebException;
			    using (HttpWebResponse response = we != null ? we.Response as HttpWebResponse : null)
			    {
			        if (response == null)
			        {
			            return ex.Message;
			        }
			        else
			        {
			            string responseString = GetResponseString(response);
			            Dictionary<string, object> json = MiniJson.Deserialize(responseString) as Dictionary<string, object>;
			            if (json != null && json.ContainsKey("message") && json["message"] is string)
			            {
			                responseString = json["message"] as string;
			            }
			            return String.Format("StatusCode:  {0}; {1}", response.StatusCode, responseString);
			        }
			    }
			}
			else
				return ex.Message;
		}

		private HttpWebRequest CreateWebRequest(string url, string method, int timeout)
		{
			HttpWebRequest request = WebRequest.Create(new Uri(url)) as HttpWebRequest;
		    request.UserAgent = UserAgent;

			CookieContainer cookieJar = new CookieContainer();
			request.CookieContainer = cookieJar;

			IWebProxy proxy = CreateProxy();
			if (proxy != null) 
			{
				request.Proxy = proxy;
			}

			request.Method = method;  
			request.Timeout = timeout;

			return request;
		}

		private IWebProxy CreateProxy()
		{
			if (!MyConfigAuth.ProxyEnable) 
			{
				return null;
			}

			IWebProxy proxy;

			if (!string.IsNullOrWhiteSpace(MyConfigAuth.ProxyUrl) && !string.IsNullOrWhiteSpace(MyConfigAuth.ProxyPort)) 
			{
				proxy = new WebProxy(MyConfigAuth.ProxyUrl, Convert.ToInt32(MyConfigAuth.ProxyPort));
				if (!string.IsNullOrWhiteSpace(MyConfigAuth.ProxyUser) && !string.IsNullOrWhiteSpace(MyConfigAuth.ProxyPassword) && !string.IsNullOrWhiteSpace(MyConfigAuth.ProxyDomain))
				{
					proxy.Credentials = new NetworkCredential(MyConfigAuth.ProxyUser, MyConfigAuth.ProxyPassword, MyConfigAuth.ProxyDomain);
				}
				else if (!string.IsNullOrWhiteSpace(MyConfigAuth.ProxyUser) && !string.IsNullOrWhiteSpace(MyConfigAuth.ProxyPassword))
				{
					proxy.Credentials = new NetworkCredential(MyConfigAuth.ProxyUser, MyConfigAuth.ProxyPassword);
				}
			} 
			else
			{
				proxy = WebRequest.GetSystemWebProxy();
				proxy.Credentials = CredentialCache.DefaultCredentials;
			}	

			return proxy;
		}

		private HttpWebResponse HttpGet(string url, NameValueCollection queryParam = null, string method = "GET", int timeout = HTTP_TIMEOUT_MED)
		{
			try
			{
				url = url + ToQueryString(queryParam);
				ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
				ShowMessage(MessageLevel.Verbose, String.Format("HTTP GET: {0}", url));

				HttpWebRequest request = CreateWebRequest(url, method, timeout);
				return request.GetResponse() as HttpWebResponse;
			}
			finally
			{
				ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			}
		}

		private HttpWebResponse HttpPost(string url, NameValueCollection queryParam = null, NameValueCollection bodyParam = null, string method = "POST", int timeout = HTTP_TIMEOUT_MED)
		{
			try
			{
				url = url + ToQueryString(queryParam);
				HttpWebRequest request = CreateWebRequest(url, method, timeout);
                request.ContentType = "application/x-www-form-urlencoded";

				// get the query string and trim off the starting "?"
				string body = ToQueryString(bodyParam);
				body = body.Remove(0, 1);

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
			finally
			{
				ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			}
		}

		private HttpWebResponse HttpPostXml(string url, string pathXML, NameValueCollection queryParam = null, string method = "POST", int timeout = HTTP_TIMEOUT_MED)
		{
			if (!File.Exists(pathXML))
				throw new Exception(String.Format("File '{0}' does not exist.", pathXML));

			try
			{
				url = url + ToQueryString(queryParam);
				HttpWebRequest request = CreateWebRequest(url, method, timeout);
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
			finally
			{
				ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
			}
		}

		private HttpWebResponse HttpUploadFiles(string url, string[] files, string paramName, string contentType, NameValueCollection queryParam, NameValueCollection bodyParam, string method = "POST", int timeout = HTTP_TIMEOUT_LONG)
		{
			if (files == null || files.Length < 1)
				throw new Exception("No files to upload.");

			foreach (string file in files)
			{
				if (!File.Exists(file))
					throw new Exception(String.Format("File '{0}' does not exist.", file));
			}

			try
			{
				url = url + ToQueryString(queryParam);
				ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
				ShowMessage(MessageLevel.Verbose, string.Format("HTTP UPLOAD {0} to {1}", String.Join(", ", files), url));
				string boundary = String.Format("-----------{0:N}", Guid.NewGuid());
				byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

				HttpWebRequest request = CreateWebRequest(url, method, timeout);
				request.ContentType = "multipart/form-data; boundary=" + boundary;
				request.KeepAlive = true;
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
			finally
			{
				ShowMessage(MessageLevel.Verbose, LOG_SEPARATOR);
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

		#endregion //  HTTP/Web helpers

		#region General Helpers

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

		#endregion //  General Helpers

	}
}
