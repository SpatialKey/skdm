using System;
using System.Xml;
using System.Collections.Generic;
using System.IO;

namespace skdm
{
	public class ConfigAction
	{

		#region Action XML constants

		// <actionType>
		public const string ACTION_SUGGEST = "suggest";
		public const string ACTION_IMPORT = "import";
		public const string ACTION_OVERWRITE = "overwrite";
		public const string ACTION_APPEND = "append";
		private static readonly List<string> VALID_ACTIONS = new List<string> {
			ACTION_SUGGEST,
			ACTION_IMPORT,
			ACTION_OVERWRITE,
			ACTION_APPEND
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

		#region properties
		public XmlNode xml;
		public ConfigAuth configAuth;

		// from the action xml
		public String actionName;
		public String actionType;
		public String[] pathDataArray;
		public String pathXML;
		public String datasetId { 
			get { return _datasetId; } 
			set 
			{ 
				_datasetId = value;
				if (_datasetId != null)
				{
					if (xml.SelectSingleNode("datasetId") == null)
						xml.AppendChild(xml.OwnerDocument.CreateElement("datasetId"));
					xml.SelectSingleNode("datasetId").InnerText = _datasetId;
					isUpdateDoc = true;
				}
			} 
		}
		private String _datasetId;
		public String dataType;

		// from pathXML if dataType is TYPE_INSURANCE
		public String locationId;
		public String policyId;

		// set when uploaded pathDataArray
		public String uploadId;

		public Boolean isUpdateDoc = false;

		#endregion

		public ConfigAction(XmlNode xml = null, ConfigAuth defaultConfigAuth = null)
		{
			ParseXML(xml);
		}

		public void ParseXML(XmlNode xml, ConfigAuth defaultConfigAuth = null)
		{
			this.xml = xml;
			if (xml == null)
				return;

			// Action override authentication info
			configAuth = new ConfigAuth(xml, defaultConfigAuth);

			// action configuration
			actionName = XMLUtils.GetInnerText(xml, "@name");
			actionType = XMLUtils.GetInnerText(xml, "actionType").ToLower();
			pathDataArray = XMLUtils.GetInnerTextList(xml, "pathData");
			pathXML = XMLUtils.GetInnerText(xml, "pathXML");
			_datasetId = XMLUtils.GetInnerText(xml, "datasetId");
			dataType = XMLUtils.GetInnerText(xml, "dataType").ToLower();

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
				if (datasetId == null || datasetId.Length == 0)
					actionType = ACTION_IMPORT;
			}
		}
	}
}

