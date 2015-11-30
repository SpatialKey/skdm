using System;
using System.Xml;
using System.Collections.Generic;
using System.IO;

using SpatialKey.DataManager.Lib.Message;
using SpatialKey.DataManager.Lib.Helpers;

namespace SpatialKey.DataManager.Lib.Config
{
	public class ActionConfig : BaseMessageClass
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
		virtual public AuthConfig ConfigAuth { get { return _configAuth; } set { _configAuth = value; } }
		// from the action xml
		virtual public String ActionName { get { return _actionName; } set { _actionName = value; } }
		virtual public String[] PathDataArray { get { return _pathDataArray; } set { _pathDataArray = value; } }
		virtual public String PathXml { get { return _pathXML; } set { _pathXML = value; } }

		virtual public String ActionType { get { return _actionType; } set { _actionType = value.ToLower(); } }

		virtual public String id { 
			get { return _id; } 
			set { 
				_id = value;
				if (_id != null && xml != null)
				{
					string elementName = DataType == TYPE_INSURANCE ? "insuranceId" : "datasetId";
					if (xml.SelectSingleNode(elementName) == null)
						xml.AppendChild(xml.OwnerDocument.CreateElement(elementName));
					xml.SelectSingleNode(elementName).InnerText = _id;
					IsUpdateDoc = true;
				}
			} 
		}

		virtual public String DataType { get { return _dataType; } set { _dataType = value.ToLower(); } }

		// from pathXML if dataType is TYPE_INSURANCE
		virtual public String LocationId { get { return _locationId; } set { _locationId = value; } }
		virtual public String PolicyId { get { return _policyId; } set { _policyId = value; } }
		// set when uploaded pathDataArray
		virtual public String UploadId { get { return _uploadId; } set { _uploadId = value; } }
		virtual public Boolean IsUpdateDoc { get { return _isUpdateDoc; } set { _isUpdateDoc = value; } }

		virtual public Boolean IsWaitUpdate { get { return _isWaitUpdate; } set { _isWaitUpdate = value; } }
		#endregion

		public ActionConfig(Messager messenger = null, XmlNode xml = null, IAuthConfig defaultConfigAuth = null) : base(messenger)
		{
			ParseXML(xml, defaultConfigAuth);
		}

		#region Parse XML Configuration
		virtual public void ParseXML(XmlNode xml, IAuthConfig defaultConfigAuth = null)
		{
			this.xml = xml;

			// Action override authentication info
			ConfigAuth = new AuthConfig(xml, defaultConfigAuth);

			if (xml == null)
			{
				return;
			}

			// action configuration
			ActionName = XMLUtils.GetInnerText(xml, "@name");
			ActionType = XMLUtils.GetInnerText(xml, "actionType");
			PathDataArray = XMLUtils.GetInnerTextList(xml, "pathData");
			PathXml = XMLUtils.GetInnerText(xml, "pathXML");
			DataType = XMLUtils.GetInnerText(xml, "dataType");

			_id = (DataType == TYPE_INSURANCE ? XMLUtils.GetInnerText(xml, "insuranceId") : XMLUtils.GetInnerText(xml, "datasetId"));

			// load insurance configuration ids if needed
			if (DataType == TYPE_INSURANCE && File.Exists(PathXml))
			{
				XmlDocument doc = new XmlDocument();
				doc.Load(PathXml);
				PolicyId = XMLUtils.GetInnerText(doc, "/insuranceImport/policyDataset/@id");
				LocationId = XMLUtils.GetInnerText(doc, "/insuranceImport/locationDataset/@id");
			}

			Validate();
		}
		#endregion

		#region validate config
		private void Validate()
		{
			// make sure data type is valid
			if (!VALID_TYPES.Contains(DataType))
				throw new Exception(String.Format("Invalid dataType '{0}' must be one of '{1}'", DataType, String.Join(", ", VALID_TYPES)));

			// make sure the action type is valid
			if (!VALID_ACTIONS.Contains(ActionType))
				throw new Exception(String.Format("Invalid actionType '{0}' must be one of '{1}'", ActionType, String.Join(", ", VALID_ACTIONS)));

			// make sure have the right number of data files
			if (DataType == TYPE_INSURANCE && !(PathDataArray.Length == 2 || PathDataArray.Length == 0))
				throw new Exception(String.Format("Invalid configuration dataType '{0}' requires 0 or 2 pathData entries.", DataType));
			else if (DataType != TYPE_INSURANCE && PathDataArray.Length != 1)
				throw new Exception(String.Format("Invalid configuration dataType '{0}' requires 1 pathData entries.", DataType));

			// fix the action type
			if (DataType == TYPE_INSURANCE && PathDataArray.Length == 0)
				ActionType = ACTION_IMPORT;

			if (ActionType == ACTION_APPEND || ActionType == ACTION_OVERWRITE)
			{
				if (id == null || id.Length == 0)
					ActionType = ACTION_IMPORT;
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
			if (DataType == TYPE_INSURANCE)
				return String.Format("actionName: '{0}' actionType: '{1}' dataType: '{2}' insuranceId: '{3}' policyId: '{4}' locationId: '{5}', pathXML: '{6}' pathData: '{7}'", 
					FormatTraceValue(ActionName),
					FormatTraceValue(ActionType), 
					FormatTraceValue(DataType), 
					FormatTraceValue(id), 
					FormatTraceValue(PolicyId), 
					FormatTraceValue(LocationId), 
					FormatTraceValue(PathXml), 
					FormatTraceValue(PathDataArray));
			else
				return String.Format("actionName: '{0}' actionType: '{1}' dataType: '{2}' datasetId: '{3}' pathXML: '{4}' pathData: '{5}'", 
					FormatTraceValue(ActionName), 
					FormatTraceValue(ActionType), 
					FormatTraceValue(DataType), 
					FormatTraceValue(id), 
					FormatTraceValue(PathXml), 
					FormatTraceValue(PathDataArray));
		}
		#endregion

		#region RunAction
		public void Run(SpatialKeyDataManager skapi)
		{
			// initialize skapi
			if (skapi == null)
				skapi = new SpatialKeyDataManager(MyMessenger);
			skapi.Init(ConfigAuth);

			// suggest actions should limit upload lengths
			if (ActionType == ActionConfig.ACTION_SUGGEST)
				PathDataArray = CreateSuggestShortFiles();

			// Upload the data and wait for upload to finish
			UploadId = skapi.UploadAndWait(PathDataArray);

			switch (ActionType)
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
			if (DataType == ActionConfig.TYPE_CSV)
				method = "ImportCSV";
			else if (DataType == ActionConfig.TYPE_SHAPE)
				method = "ImportShapefile";
			else if (DataType == ActionConfig.TYPE_INSURANCE)
				method = "ImportInsurance";
			else
			{
				ShowMessage(MessageLevel.Error, String.Format("Unknown dataType '{1}", DataType));
				return;
			}

			string xml = skapi.GetSampleConfiguration(UploadId, method);
			if (xml != null)
			{
				string tmpPathXml = this.PathXml;
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
			if (DataType == ActionConfig.TYPE_INSURANCE && PathDataArray.Length == 0)
			{
				uploadMessage = String.Format("Importing insurance using existing dataset ids in '{0}'", PathXml);
			}
			else
			{
				uploadMessage = String.Format("Importing '{0}' for {1}", String.Join(", ", PathDataArray), (DataType == ActionConfig.TYPE_INSURANCE ? "insurance" : "dataset"));
			}

			ShowMessage(MessageLevel.Status, uploadMessage);

			bool isSuccess;
			if (DataType == ActionConfig.TYPE_INSURANCE)
			{
				if (PathDataArray.Length == 0)
				{
					UploadId = skapi.InsuranceCreateExistingDatasets(PathXml);
					isSuccess = UploadId != null;
				}
				else
				{
					isSuccess = skapi.InsuranceCreate(UploadId, PathDataArray, PathXml);
				}
			}
			else
			{
				isSuccess = skapi.DatasetCreate(UploadId, PathXml);
			}

			if (!isSuccess)
				return;

			if (IsWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(UploadId);
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
			if (DataType == ActionConfig.TYPE_INSURANCE && PathDataArray.Length == 0)
			{
				ShowMessage(MessageLevel.Error, String.Format("Canot do an insurance overwrite with only ids '{0}'", PathXml));
				return;
			}
			else
			{
				uploadMessage = String.Format("Overwriting {0} '{1}' using csv '{2}' and config '{3}'", (DataType == ActionConfig.TYPE_INSURANCE ? "insurance" : "dataset"), id, String.Join(", ", PathDataArray), PathXml);
				ShowMessage(MessageLevel.Status, uploadMessage);
				if (DataType == ActionConfig.TYPE_INSURANCE)
					isSuccess = skapi.InsuranceOverwrite(UploadId, id, PathXml);
				else
					isSuccess = skapi.DatasetOverwrite(UploadId, id, PathXml);
			}

			if (!isSuccess)
				return;

			if (IsWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(UploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
				}
				else
				{
					if (DataType == ActionConfig.TYPE_INSURANCE)
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
			if (DataType == ActionConfig.TYPE_INSURANCE || DataType == ActionConfig.TYPE_SHAPE)
			{
				ShowMessage(MessageLevel.Error, String.Format("Canot append dataType '{0}'", DataType));
				return;
			}

			skapi.DatasetAppend(UploadId, id, PathXml);
			ShowMessage(MessageLevel.Status, String.Format("Appending '{0}'", String.Join(", ", PathDataArray)));
			if (IsWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(UploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
					ShowMessage(MessageLevel.Error, String.Format("Append failed: {0}", MiniJson.Serialize(uploadStausJson)));
				else
					ShowMessage(MessageLevel.Status, String.Format("Appended '{0}'", String.Join(", ", PathDataArray)));
			}
			else
				ShowMessage(MessageLevel.Status, String.Format("Not waiting for append of '{0}' to complete", String.Join(", ", PathDataArray)));
		}
		#endregion
		#region RunAction Helpers
		private string[] CreateSuggestShortFiles()
		{
			List<string> list = new List<string>();
			foreach (string path in PathDataArray)
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
			if (DataType == ActionConfig.TYPE_INSURANCE)
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
				if (PathDataArray.Length == 2)
				{
					if (policyId == null || locationId == null || insuranceId == null)
					{
						ShowMessage(MessageLevel.Error, String.Format("{0} Failed.  Could not find policy, location, and insurance ids: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
						return false;
					}
					XmlDocument doc = new XmlDocument();
					doc.Load(PathXml);
					(doc.SelectSingleNode("/insuranceImport/policyDataset") as XmlElement).SetAttribute("id", policyId);
					(doc.SelectSingleNode("/insuranceImport/locationDataset") as XmlElement).SetAttribute("id", locationId);
					doc.Save(PathXml);
					ShowMessage(MessageLevel.Result, String.Format("UPDATE wrote policy and location ids to {0}", PathXml));
				}
				else if (PathDataArray.Length == 0)
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

