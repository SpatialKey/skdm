using System;
using System.Xml;
using System.Collections.Generic;

namespace SpatialKey.DataManager.Lib
{
	public class XMLUtils
	{
		public static String GetInnerText(XmlNode node, String path, String defaultValue = "")
		{
			if (defaultValue == null)
				defaultValue = "";

			XmlNode value = node.SelectSingleNode(path);
			return value != null ? value.InnerText : defaultValue;
		}

		public static String[] GetInnerTextList(XmlNode node, String path)
		{
			List<string> retList = new List<string>();
			XmlNodeList valueList = node.SelectNodes(path);
			if (valueList != null)
			{
				foreach (XmlNode value in valueList)
				{
					retList.Add(value.InnerText);
				}
			}
			return retList.ToArray();
		}
	}
}

