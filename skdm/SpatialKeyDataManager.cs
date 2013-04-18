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
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;

// Using http://www.icsharpcode.net/opensource/sharpziplib/

namespace skdm
{
	/// <summary>
	/// The SpatialKey Data Import API (DIAPI) allows developers to programmatically create or update SpatialKey datasets.
	/// </summary>
	/// <see cref="http://support.spatialkey.com/dmapi"/>
	public class SpatialKeyDataManager
	{
		#region parameters
		/// <summary>Logging delegate</summary>
		public delegate void Logger(string message);
		
		/// <summary>
		/// Name of the SpatialKey organization used to get cluster info in <see cref="ClusterLookup()"/>
		/// </summary>
		public string organizationName { get; private set; }
		
		/// <summary>
		/// Name of the SpatialKey organization url"/>
		/// </summary>
		public string clusterDomainUrl { get; private set; }
		
		/// <summary>
		/// Authentication username for <see cref="Authenticate()"/>
		/// </summary>
		public string userName { get; private set; }
		
		/// <summary>
		/// Authentication password for <see cref="Authenticate()"/>
		/// </summary>
		public string password { get; private set; }
		
		/// <summary>
		/// Authentication apiKey for <see cref="Authenticate()"/>
		/// </summary>
		public string apiKey { get; private set; }
		
		/// <summary>
		/// Authentication userId for <see cref="Authenticate()"/>
		/// </summary>
		public string userId { get; private set; }

		/// <summary>
		/// Gets or sets the logger.  Used by  for <see cref="Log(message)"/>
		/// </summary>
		public Logger logger { get; set; }
		#endregion
		
		#region dataMartAPI properties - set by calls
		/// <summary>
		/// Gets the cluster for the the given organizationName.
		/// </summary>
		public string clusterHost { get; private set; }
		
		/// <summary>
		/// The authenticated jsessionID retrieved by <see cref="Authenticate()"/>
		/// </summary>
		private Cookie _jsessionID;
		
		/// <summary>
		/// The protocol retrieved by <see cref="ClusterLookup()"/>
		/// </summary>
		private string _protocol;
		#endregion
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SpatialKey.SpatialKeyDataManager"/> class.
		/// </summary>
		/// <param name='organizationName'>
		/// Name of the SpatialKey organization used to get cluster info in <see cref="ClusterLookup()"/>
		/// </param>
		/// <param name='userName'>
		/// Authentication username for <see cref="Authenticate()"/>
		/// </param>
		/// <param name='password'>
		/// Authentication password for <see cref="Authenticate()"/>
		/// </param>
		/// </param>
		/// <param name='apiKey'>
		/// Authentication apiKey for <see cref="Authenticate()"/>
		/// </param>
		/// <param name='userId'>
		/// Authentication userId for <see cref="Authenticate()"/>
		/// </param>
		/// <param name='logger'>
		/// Used by for <see cref="Log(message)"/>
		/// </param>
		public SpatialKeyDataManager(string organizationName = null, string clusterDomainUrl = null, string userName = null, string password = null, String apiKey = null, String userId = null, Logger logger = null)
		{
			this.logger = logger;
			Init(organizationName, clusterDomainUrl, userName, password, apiKey, userId);
		}
		
		/// <summary>
		/// Initialize/Reset the DataMartImporter
		/// </summary>
		/// <param name='organizationName'>
		/// Name of the SpatialKey organization used to get cluster info in <see cref="ClusterLookup()"/>
		/// </param>
		/// <param name='userName'>
		/// Authentication username for <see cref="Authenticate()"/>
		/// </param>
		/// <param name='password'>
		/// Authentication password for <see cref="Authenticate()"/>
		/// </param>
		/// <param name='apiKey'>
		/// Authentication apiKey for <see cref="Authenticate()"/>
		/// </param>
		/// <param name='userId'>
		/// Authentication userId for <see cref="Authenticate()"/>
		/// </param>
		public void Init(string organizationName = null, string clusterDomainUrl = null, string userName = null, string password = null, String apiKey = null, String userId = null)
		{
			// if the setup is the same, bail
			if (this.organizationName == organizationName && 
				this.clusterDomainUrl == clusterDomainUrl &&
			    this.userName == userName &&
			    this.password == password &&
			    this.apiKey == apiKey &&
			    this.userId == userId)
			{
				return;
			}

			this.organizationName = organizationName;
			this.clusterDomainUrl = clusterDomainUrl;
			this.userName = userName;
			this.password = password;
			this.logger = logger;
			this.apiKey = apiKey;
			this.userId = userId;
			
			clusterHost = null;
			_jsessionID = null; 
			_protocol = null;

			if (clusterDomainUrl != null && clusterDomainUrl.Length > 0)
			{
				Uri uri = new Uri(clusterDomainUrl);
				_protocol = uri.Scheme == "http" ? "http://" : "https://";
				clusterHost = uri.Host;
			}
		}
		
		/// <summary>
		/// Log the specified message if the <see cref="logger"/> is set
		/// </summary>
		/// <param name='message'>
		/// Message to log
		/// </param>
		private void Log(string message)
		{
			if (logger != null)
				logger(message);
		}
		
		#region low level dataImportAPI calls
		/// <summary>
		/// Look up the cluster for the <see cref="organizationName"/> to get <see cref="clusterHost"/> and <see cref="_protocol"/>
		/// </summary>
		private void ClusterLookup()
		{
			if (clusterHost != null && clusterHost.Length > 0 && _protocol != null && _protocol.Length > 0)
				return;

			string url = String.Format("http://{0}.spatialkey.com/clusterlookup.cfm", organizationName);
			Log(String.Format("ClusterLookup: {0}", url));
			
			XmlDocument doc = new XmlDocument();
			doc.Load(url);
			Log(doc.InnerXml);
			
			clusterHost = doc.SelectSingleNode("/organization/cluster").InnerText;
			_protocol = doc.SelectSingleNode("/organization/protocol").InnerText;
			
			Log(String.Format("Cluster: {0}", clusterHost));
		}
		
		/// <summary>
		/// Authenticate to the dataImportAPI and get the <see cref="_jsessionID"/>
		/// </summary>
		private void Authenticate()
		{
			if (_jsessionID != null)
				return;

			ClusterLookup();

			string url = "";
			if (apiKey != null && apiKey.Length > 0 && userId != null && userId.Length > 0)
			{
				url = String.Format("{0}{1}/SpatialKeyFramework/dataImportAPI?action=login&orgName={2}&userId={3}&apiKey={4}", 
				                    _protocol, clusterHost, organizationName, HttpUtility.UrlEncode(userId), HttpUtility.UrlEncode(apiKey));
				Log(String.Format("Authenticate: {0}", url.Replace(apiKey, "XXX").Replace(userId, "XXX")));
			}
			else if (userName != null && userName.Length > 0 && this.password != null && this.password.Length > 0)
			{
				string password = HttpUtility.UrlEncode(this.password);
				url = String.Format("{0}{1}/SpatialKeyFramework/dataImportAPI?action=login&orgName={2}&user={3}&password={4}", 
				                     _protocol, clusterHost, organizationName, HttpUtility.UrlEncode(userName), password);
				Log(String.Format("Authenticate: {0}", url.Replace(password, "XXX")));
			}
			else
			{
				throw new ArgumentException("Must have userName and password or apiKey and userId.");
			}

			HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
			request.Method = "GET";
			CookieContainer cookieJar = new CookieContainer();
			request.CookieContainer = cookieJar;
			
			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse ())
			using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
			{
				if (response.StatusCode != HttpStatusCode.OK)
				{
					throw new SystemException("Authentication Failed");
				}
				
				_jsessionID = cookieJar.GetCookies(request.RequestUri)["JSESSIONID"];
				Log(streamReader.ReadToEnd());
			}
			
			Log(_jsessionID.ToString());
		}
		#endregion
		
		#region Upload
		/// <summary>
		/// Zips up the data and uploads it to the dataImportAPI
		/// </summary>
		/// <param name='dataPath'>
		/// The path to the data to import.
		/// </param>
		/// <param name='xmlPath'>
		/// The path to the xml description of the data being imported
		/// </param>
		/// <param name='action'>
		/// Import action.  Can be 'overwrite' or 'append'
		/// </param>
		/// <param name='runAsBackground'>
		/// determines if the SpatialKey server should return as soon as the data is uploaded 
		/// (thus freeing up your connection), or should it wait for the entire import process to complete
		/// </param>
		/// <param name='notifyByEmail'>
		/// if set as true the SpatialKey server will send an email to the authenticated user when the import is complete
		/// </param>
		/// <param name='addAllUsers'>
		/// if set as true the SpatialKey server will add the All Users group as a viewer of the dataset
		/// </param>
		public void UploadData(string dataPath, string xmlPath, string action = "overwrite", bool runAsBackground = true, bool notifyByEmail = false, bool addAllUsers = false)
		{
			Authenticate();
			
			Log(String.Format("UploadData: {0} {1}", dataPath, xmlPath));
			
			string zipPath = ZipData(new string[] {dataPath, xmlPath});
			
			try
			{
				UploadZip(zipPath, action, runAsBackground, notifyByEmail, addAllUsers);
			}
			finally
			{
				File.Delete(zipPath);
			}
			
			Log("UploadData: Complete");
		}
		
		/// <summary>
		/// Uploads the zip to the dataImportAPI
		/// </summary>
		/// <param name='zipPath'>
		/// Path to the zip file (containing data and xml configuration) to upload
		/// </param>
		/// <param name='action'>
		/// Import action.  Can be 'overwrite' or 'append'
		/// </param>
		/// <param name='runAsBackground'>
		/// determines if the SpatialKey server should return as soon as the data is uploaded 
		/// (thus freeing up your connection), or should it wait for the entire import process to complete
		/// </param>
		/// <param name='notifyByEmail'>
		/// if set as true the SpatialKey server will send an email to the authenticated user when the import is complete
		/// </param>
		/// <param name='addAllUsers'>
		/// if set as true the SpatialKey server will add the All Users group as a viewer of the dataset
		/// </param>
		private void UploadZip(string zipPath, string action = "overwrite", bool runAsBackground = true, bool notifyByEmail = false, bool addAllUsers = false)
		{
			string path = "/SpatialKeyFramework/dataImportAPI";
			
			string url = String.Format("{0}{1}{2}", 
			                            _protocol,
			                            clusterHost,
			                            path);
			
			Log(String.Format("UploadZip: {0} {1}", url, zipPath));
			
			// create the jsessionID cookie
			CookieContainer cookieJar = new CookieContainer();
			Cookie cookie = new Cookie(_jsessionID.Name, _jsessionID.Value);
			cookieJar.Add(new Uri(String.Format("{0}{1}", _protocol, clusterHost)), cookie);
			
			CustomWebClient client = new CustomWebClient(cookieJar);
			
			// add the query string
			NameValueCollection query = new NameValueCollection();
			query.Add("action", action);
			query.Add("runAsBackground", runAsBackground.ToString().ToLower());
			query.Add("notifyByEmail", notifyByEmail.ToString().ToLower());
			query.Add("addAllUsers", addAllUsers.ToString().ToLower());
			client.QueryString = query;
			Log(NameValueCollectionToString(query));

			byte[] response = client.UploadFile(url, zipPath);
			Log(Encoding.ASCII.GetString(response));

			Log("UploadZip: Complete");
		}
		#endregion

		#region Shape File Upload
		public void UploadShape(string dataPath, string datasetName, string datasetId)
		{
			Log(String.Format("UploadShape: {0}", dataPath));
			
			Authenticate();
			
			string path = "/SpatialKeyFramework/dataImportAPI";
			
			string url = String.Format("{0}{1}{2}", 
			                           _protocol,
			                           clusterHost,
			                           path);
			
			// create the jsessionID cookie
			CookieContainer cookieJar = new CookieContainer();
			Cookie cookie = new Cookie(_jsessionID.Name, _jsessionID.Value);
			cookieJar.Add(new Uri(String.Format("{0}{1}", _protocol, clusterHost)), cookie);
			
			CustomWebClient client = new CustomWebClient(cookieJar);
			
			// add the query string
			NameValueCollection query = new NameValueCollection();
			query.Add("action", "poly");
			if (datasetName != null && datasetName.Length > 0)
				query.Add("datasetName", datasetName);
			if (datasetId != null && datasetId.Length > 0)
				query.Add("datasetId", datasetId);
			client.QueryString = query;
			Log(NameValueCollectionToString(query));
			
			byte[] response = client.UploadFile(url, dataPath);
			Log(Encoding.ASCII.GetString(response));

			Log("UploadShape: Complete");
		}
		#endregion
		
		#region Zip Up Files
		/// <summary>
		/// Zips the given files into a temporary zip file
		/// </summary>
		/// <returns>
		/// The path to the zip file
		/// </returns>
		/// <param name='paths'>
		/// Array of file paths to include
		/// </param>
		private string ZipData(string[] paths)
		{
			Log(String.Format("ZipData: {0}", String.Join(", ", paths)));
			
			string zipPath = GetTempFile("zip");
			ZipOutputStream zipStream = new ZipOutputStream(File.Create(zipPath));
			
			foreach (string path in paths)
			{
				ZipAdd(zipStream, path);
			}
			
			zipStream.Finish();
			zipStream.IsStreamOwner = true;	// Makes the Close also Close the underlying stream
			zipStream.Close();
			
			Log(String.Format("ZipData: {0}", zipPath));
			return zipPath;
		}
		
		/// <summary>
		/// Adds the given file to the zip stream
		/// </summary>
		/// <param name='zipStream'>
		/// Zip stream.
		/// </param>
		/// <param name='fName'>
		/// File path to add to the zip
		/// </param>
		private void ZipAdd(ZipOutputStream zipStream, string fName)
		{
			Log(String.Format("ZipAdd: {0}", fName));
			FileInfo fi = new FileInfo(fName);
			
			// add the entry
			ZipEntry newEntry = new ZipEntry(fi.Name);
			newEntry.DateTime = fi.LastWriteTime;
			// To permit the zip to be unpacked by built-in extractor in WinXP and Server2003, WinZip 8, Java, and other older code,
			// you need to do one of the following: Specify UseZip64.Off, or set the Size.
			// If the file may be bigger than 4GB, or you do not need WinXP built-in compatibility, you do not need either,
			// but the zip will be in Zip64 format which not all utilities can understand.
			//   zipStream.UseZip64 = UseZip64.Off;
			newEntry.Size = fi.Length;
			
			zipStream.PutNextEntry(newEntry);
			
			// Zip the file in buffered chunks
			// the "using" will close the stream even if an exception occurs
			using (FileStream streamReader = File.OpenRead(fName))
			{
				streamReader.CopyTo(zipStream);
			}
			zipStream.CloseEntry();
			Log("ZipAdd: Complete");
		}
		
		#endregion
		
		#region helpers
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

