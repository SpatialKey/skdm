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

		private String _id;
		public String id { 
			get { return _id; } 
			set { 
				_id = value;
				if (_id != null)
				{
					string elementName = dataType == TYPE_INSURANCE ? "insuranceId" : "datasetId";
					if (xml.SelectSingleNode(elementName) == null)
						xml.AppendChild(xml.OwnerDocument.CreateElement(elementName));
					xml.SelectSingleNode(elementName).InnerText = _id;
					isUpdateDoc = true;
				}
			} 
		}

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
			dataType = XMLUtils.GetInnerText(xml, "dataType").ToLower();

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
	}
}

