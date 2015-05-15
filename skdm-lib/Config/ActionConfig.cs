using System;
using System.Xml;
using System.Collections.Generic;
using System.IO;

using SpatialKey.DataManager.Lib.Message;
using SpatialKey.DataManager.Lib.Helpers;

namespace SpatialKey.DataManager.Lib.Config
{
	public class ConfigAction : BaseMessageClass
	{
		#region Action XML constants

		// <actionType>
		public const string ACTION_SUGGEST = "suggest";
		public const string ACTION_IMPORT = "import";
		public const string ACTION_OVERWRITE = "overwrite";
		public const string ACTION_APPEND = "append";
		private static readonly List<string> VALID_ACTIONS = new List<string> {
			ACTION_OVERWRITE,
			ACTION_APPEND,
			ACTION_IMPORT
		};
		// <dataType>
		public const string TYPE_CSV = "csv";
		public const string TYPE_SHAPE = "shapefile";
		public const string TYPE_INSURANCE = "insurance";
		private static readonly List<string> VALID_TYPES = new List<string> {
			TYPE_CSV,
			TYPE_SHAPE,
			TYPE_INSURANCE
		};

		#endregion

		#region Fields
		protected String _actionType;
		protected XmlNode _xml;
		protected AuthConfig _configAuth;
		protected String _actionName;
		protected String[] _pathDataArray;
		protected String _pathXML;
		protected String _id;
		protected String _dataType;
		protected String _locationId;
		protected String _policyId;
		protected String _uploadId;
		protected Boolean _isUpdateDoc = false;
		protected Boolean _isWaitUpdate = true;
		#endregion

		#region Properties

		virtual public XmlNode xml { get { return _xml; } set { _xml = value; } }
		virtual public AuthConfig configAuth { get { return _configAuth; } set { _configAuth = value; } }
		// from the action xml
		virtual public String actionName { get { return _actionName; } set { _actionName = value; } }
		virtual public String[] pathDataArray { get { return _pathDataArray; } set { _pathDataArray = value; } }
		virtual public String pathXML { get { return _pathXML; } set { _pathXML = value; } }

		virtual public String actionType { get { return _actionType; } set { _actionType = value.ToLower(); } }

		virtual public String id { 
			get { return _id; } 
			set { 
				_id = value;
				if (_id != null && xml != null)
				{
					string elementName = dataType == TYPE_INSURANCE ? "insuranceId" : "datasetId";
					if (xml.SelectSingleNode(elementName) == null)
						xml.AppendChild(xml.OwnerDocument.CreateElement(elementName));
					xml.SelectSingleNode(elementName).InnerText = _id;
					isUpdateDoc = true;
				}
			} 
		}

		virtual public String dataType { get { return _dataType; } set { _dataType = value.ToLower(); } }

		// from pathXML if dataType is TYPE_INSURANCE
		virtual public String locationId { get { return _locationId; } set { _locationId = value; } }
		virtual public String policyId { get { return _policyId; } set { _policyId = value; } }
		// set when uploaded pathDataArray
		virtual public String uploadId { get { return _uploadId; } set { _uploadId = value; } }
		virtual public Boolean isUpdateDoc { get { return _isUpdateDoc; } set { _isUpdateDoc = value; } }

		virtual public Boolean isWaitUpdate { get { return _isWaitUpdate; } set { _isWaitUpdate = value; } }
		#endregion

		public ConfigAction(Messager messenger = null, XmlNode xml = null, IAuthConfig defaultConfigAuth = null) : base(messenger)
		{

			ParseXML(xml, defaultConfigAuth);
		}

		#region Parse XML Configuration
		virtual public void ParseXML(XmlNode xml, IAuthConfig defaultConfigAuth = null)
		{
			this.xml = xml;
			if (xml == null)
				return;

			// Action override authentication info
			configAuth = new AuthConfig(xml, defaultConfigAuth);

			// action configuration
			actionName = XMLUtils.GetInnerText(xml, "@name");
			actionType = XMLUtils.GetInnerText(xml, "actionType");
			pathDataArray = XMLUtils.GetInnerTextList(xml, "pathData");
			pathXML = XMLUtils.GetInnerText(xml, "pathXML");
			dataType = XMLUtils.GetInnerText(xml, "dataType");

			_id = (dataType == TYPE_INSURANCE ? XMLUtils.GetInnerText(xml, "insuranceId") : XMLUtils.GetInnerText(xml, "datasetId"));

			// load insurance configuration ids if needed
			if (dataType == TYPE_INSURANCE && File.Exists(pathXML))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(pathXML);
				policyId = XMLUtils.GetInnerText(doc, "/insuranceImport/policyDataset/@id");
				locationId = XMLUtils.GetInnerText(doc, "/insuranceImport/locationDataset/@id");
			}

			Validate();
		}
		#endregion

		#region validate config
		private void Validate()
		{
			// make sure data type is valid
			if (!VALID_TYPES.Contains(dataType))
				throw new Exception(String.Format("Invalid dataType '{0}' must be one of '{1}'", dataType, String.Join(", ", VALID_TYPES)));

			// make sure the action type is valid
			if (!VALID_ACTIONS.Contains(actionType))
				throw new Exception(String.Format("Invalid actionType '{0}' must be one of '{1}'", actionType, String.Join(", ", VALID_ACTIONS)));

			// make sure have the right number of data files
			if (dataType == TYPE_INSURANCE && !(pathDataArray.Length == 2 || pathDataArray.Length == 0))
				throw new Exception(String.Format("Invalid configuration dataType '{0}' requires 0 or 2 pathData entries.", dataType));
			else if (dataType != TYPE_INSURANCE && pathDataArray.Length != 1)
				throw new Exception(String.Format("Invalid configuration dataType '{0}' requires 1 pathData entries.", dataType));

			// fix the action type
			if (dataType == TYPE_INSURANCE && pathDataArray.Length == 0)
				actionType = ACTION_IMPORT;

			if (actionType == ACTION_APPEND || actionType == ACTION_OVERWRITE)
			{
				if (id == null || id.Length == 0)
					actionType = ACTION_IMPORT;
			}
		}
		#endregion

		#region Trace Action Information
		private string FormatTraceValue(object value)
		{
			if (value is String)
			{
				String s = value as String;
				s.Trim();
				if (s.Length < 1)
					return "UNSET";
				else
					return s;
			}
			else if (value is String[])
				return String.Join(", ", (value as String[]));
			else
				return "UNSET";
		}

		public string TraceInfo()
		{
			if (dataType == TYPE_INSURANCE)
				return String.Format("actionName: '{0}' actionType: '{1}' dataType: '{2}' insuranceId: '{3}' policyId: '{4}' locationId: '{5}', pathXML: '{6}' pathData: '{7}'", 
					FormatTraceValue(actionName),
					FormatTraceValue(actionType), 
					FormatTraceValue(dataType), 
					FormatTraceValue(id), 
					FormatTraceValue(policyId), 
					FormatTraceValue(locationId), 
					FormatTraceValue(pathXML), 
					FormatTraceValue(pathDataArray));
			else
				return String.Format("actionName: '{0}' actionType: '{1}' dataType: '{2}' datasetId: '{3}' pathXML: '{4}' pathData: '{5}'", 
					FormatTraceValue(actionName), 
					FormatTraceValue(actionType), 
					FormatTraceValue(dataType), 
					FormatTraceValue(id), 
					FormatTraceValue(pathXML), 
					FormatTraceValue(pathDataArray));
		}
		#endregion

		#region RunAction
		public void Run(SpatialKeyDataManager skapi)
		{
			// initialize skapi
			if (skapi == null)
				skapi = new SpatialKeyDataManager(MyMessenger);
			skapi.Init(configAuth);

			// suggest actions should limit upload lengths
			if (actionType == ConfigAction.ACTION_SUGGEST)
				pathDataArray = CreateSuggestShortFiles();

			// Upload the data and wait for upload to finish
			uploadId = skapi.UploadAndWait(pathDataArray);

			switch (actionType)
			{
			case ACTION_SUGGEST:
				RunSuggest(skapi);
				break;
			case ACTION_IMPORT:
				RunImport(skapi);
				break;
			case ACTION_OVERWRITE:
				RunOverwrite(skapi);
				break;
			case ACTION_APPEND:
				RunAppend(skapi);
				break;
			default:
				throw new Exception("Unknown actionType: " + TraceInfo());
			}
		}

		private void RunSuggest(SpatialKeyDataManager skapi)
		{
			String method;
			if (dataType == ConfigAction.TYPE_CSV)
				method = "ImportCSV";
			else if (dataType == ConfigAction.TYPE_SHAPE)
				method = "ImportShapefile";
			else if (dataType == ConfigAction.TYPE_INSURANCE)
				method = "ImportInsurance";
			else
			{
				ShowMessage(MessageLevel.Error, String.Format("Unknown dataType '{1}", dataType));
				return;
			}

			string xml = skapi.GetSampleConfiguration(uploadId, method);
			if (xml != null)
			{
				string tmpPathXml = this.pathXML;
				if (File.Exists(tmpPathXml))
				{
					tmpPathXml = SpatialKeyDataManager.GetTempFile("xml", Path.GetFileNameWithoutExtension(tmpPathXml), Path.GetDirectoryName(tmpPathXml));
				}
				using (StreamWriter outfile = new StreamWriter(tmpPathXml))
				{
					outfile.Write(xml);
				}
				ShowMessage(MessageLevel.Status, String.Format("Wrote Suggested XML to '{0}'", tmpPathXml));
			}
		}

		private void RunImport(SpatialKeyDataManager skapi)
		{
			string uploadMessage;
			if (dataType == ConfigAction.TYPE_INSURANCE && pathDataArray.Length == 0)
			{
				uploadMessage = String.Format("Importing insurance using existing dataset ids in '{0}'", pathXML);
			}
			else
			{
				uploadMessage = String.Format("Importing '{0}' for {1}", String.Join(", ", pathDataArray), (dataType == ConfigAction.TYPE_INSURANCE ? "insurance" : "dataset"));
			}

			ShowMessage(MessageLevel.Status, uploadMessage);

			bool isSuccess;
			if (dataType == ConfigAction.TYPE_INSURANCE)
			{
				if (pathDataArray.Length == 0)
				{
					uploadId = skapi.InsuranceCreateExistingDatasets(pathXML);
					isSuccess = uploadId != null;
				}
				else
				{
					isSuccess = skapi.InsuranceCreate(uploadId, pathDataArray, pathXML);
				}
			}
			else
			{
				isSuccess = skapi.DatasetCreate(uploadId, pathXML);
			}

			if (!isSuccess)
				return;

			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
					return;
				}

				if (!ExtractDatasetIds(skapi, uploadMessage, uploadStausJson))
					return;

				ShowMessage(MessageLevel.Status, String.Format("{0} Complete", uploadMessage));
			}
			else
				ShowMessage(MessageLevel.Status, String.Format("{0} Not watiting for completion.", uploadMessage));
		}

		private void RunOverwrite(SpatialKeyDataManager skapi)
		{
			bool isSuccess;
			string uploadMessage;
			if (dataType == ConfigAction.TYPE_INSURANCE && pathDataArray.Length == 0)
			{
				ShowMessage(MessageLevel.Error, String.Format("Canot do an insurance overwrite with only ids '{0}'", pathXML));
				return;
			}
			else
			{
				uploadMessage = String.Format("Overwriting {0} '{1}' using csv '{2}' and config '{3}'", (dataType == ConfigAction.TYPE_INSURANCE ? "insurance" : "dataset"), id, String.Join(", ", pathDataArray), pathXML);
				ShowMessage(MessageLevel.Status, uploadMessage);
				if (dataType == ConfigAction.TYPE_INSURANCE)
					isSuccess = skapi.InsuranceOverwrite(uploadId, id, pathXML);
				else
					isSuccess = skapi.DatasetOverwrite(uploadId, id, pathXML);
			}

			if (!isSuccess)
				return;

			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
				}
				else
				{
					if (dataType == ConfigAction.TYPE_INSURANCE)
					{
						if (!ExtractDatasetIds(skapi, uploadMessage, uploadStausJson))
							return;
					}

					ShowMessage(MessageLevel.Status, String.Format("Finished {0}", uploadMessage));
				}
			}
			else
				ShowMessage(MessageLevel.Status, String.Format("Not waiting for completion. {0}", uploadMessage));
		}

		private void RunAppend(SpatialKeyDataManager skapi)
		{
			if (dataType == ConfigAction.TYPE_INSURANCE || dataType == ConfigAction.TYPE_SHAPE)
			{
				ShowMessage(MessageLevel.Error, String.Format("Canot append dataType '{0}'", dataType));
				return;
			}

			skapi.DatasetAppend(uploadId, id, pathXML);
			ShowMessage(MessageLevel.Status, String.Format("Appending '{0}'", String.Join(", ", pathDataArray)));
			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
					ShowMessage(MessageLevel.Error, String.Format("Append failed: {0}", MiniJson.Serialize(uploadStausJson)));
				else
					ShowMessage(MessageLevel.Status, String.Format("Appended '{0}'", String.Join(", ", pathDataArray)));
			}
			else
				ShowMessage(MessageLevel.Status, String.Format("Not waiting for append of '{0}' to complete", String.Join(", ", pathDataArray)));
		}
		#endregion
		#region RunAction Helpers
		private string[] CreateSuggestShortFiles()
		{
			List<string> list = new List<string>();
			foreach (string path in pathDataArray)
			{
				if (Path.GetExtension(path).ToLower() != ".csv")
				{
					list.Add(path);
					continue;
				}

				bool isUseTmp = false;
				string tmp = SpatialKeyDataManager.GetTempFile("csv", Path.GetFileNameWithoutExtension(path));
				using (StreamReader reader = new StreamReader(path))
				using (StreamWriter writer = new StreamWriter(tmp))
				{
					int numLines = 0;
					string line;
					while ((line = reader.ReadLine()) != null)
					{
						numLines++;
						if (numLines >= 500)
						{
							isUseTmp = true;
							break;
						}
						writer.WriteLine(line);
					}
				}
				if (isUseTmp)
					list.Add(tmp);
				else
					list.Add(path);
			}
			return list.ToArray();
		}

		private bool ExtractDatasetIds(SpatialKeyDataManager skapi, string uploadMessage, Dictionary<string, object> uploadStausJson)
		{
			Dictionary<string, string> ids = skapi.GetDatasetIDs(uploadStausJson);
			if (dataType == ConfigAction.TYPE_INSURANCE)
			{
				string policyId = null;
				string locationId = null;
				string insuranceId = null;
				if (ids != null)
				{
					foreach (KeyValuePair<string, string> cur in ids)
					{
						if (cur.Value.ToLower() == "policy_dataset")
							policyId = cur.Key;
						else if (cur.Value.ToLower() == "location_dataset")
							locationId = cur.Key;
						else if (cur.Value.ToLower() == "insurance")
							insuranceId = cur.Key;
					}
				}
				if (pathDataArray.Length == 2)
				{
					if (policyId == null || locationId == null || insuranceId == null)
					{
						ShowMessage(MessageLevel.Error, String.Format("{0} Failed.  Could not find policy, location, and insurance ids: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
						return false;
					}
					XmlDocument doc = new XmlDocument();
					doc.Load(pathXML);
					(doc.SelectSingleNode("/insuranceImport/policyDataset") as XmlElement).SetAttribute("id", policyId);
					(doc.SelectSingleNode("/insuranceImport/locationDataset") as XmlElement).SetAttribute("id", locationId);
					doc.Save(pathXML);
					ShowMessage(MessageLevel.Result, String.Format("UPDATE wrote policy and location ids to {0}", pathXML));
				}
				else if (pathDataArray.Length == 0)
				{
					if (insuranceId == null)
					{
						ShowMessage(MessageLevel.Error, String.Format("{0} Failed.  Could not find insurance id: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
						return false;
					}
				}
				else
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed. Incorrect number of ids: {0}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
					return false;
				}
				id = insuranceId;
			}
			else
			{
				id = ((ids != null && ids.Count == 1) ? (new List<string>(ids.Keys))[0] : null);
				if (id == null)
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed.  Could not find dataset Id: {0}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
					return false;
				}
			}
			return true;
		}
		#endregion

	}
}

