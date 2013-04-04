/// <file>
/// <copyright>Copyright (c) 2013 SpatialKey</copyright>
/// <author>Robert Stehwien</author>
/// <datecreated>2013-04-01</datecreated>
/// </file>

using System;
using System.Xml;

namespace skdm
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			string configFile = "SpatailKeyDataManagerConfig.xml";
			if (args.Length > 0) configFile = args[0];
			
			XmlDocument doc = new XmlDocument ();
			doc.Load (configFile);

			String defaultOrganizationName = GetInnerText(doc, "/config/organizationName");
			String defaultUserName = GetInnerText(doc, "/config/userName");
			String defaultPassword = GetInnerText(doc, "/config/password");
			String defaultApiKey = GetInnerText(doc, "/config/apiKey"); 

			var actionNodes = doc.SelectNodes("/config/actions/action");
			foreach (XmlNode actionNode in actionNodes) {
				String organizationName = GetInnerText(doc, "organizationName", defaultOrganizationName);
				String userName = GetInnerText(doc, "userName", defaultUserName);
				String password = GetInnerText(doc, "password", defaultPassword);
				String apiKey = GetInnerText(doc, "apiKey", defaultApiKey); 
				String action = GetInnerText(actionNode, "action");
				String actionName = GetInnerText(actionNode, "@name");


				SpatialKeyDataManager skapi = new SpatialKeyDataManager(organizationName, userName, password, apiKey, Log);
				
				if (action.ToLower() == "overwrite" || action.ToLower() == "append")
				{
					String dataPath = GetInnerText(actionNode, "dataPath");
					String xmlPath = GetInnerText(actionNode, "xmlPath");
					Boolean runAsBackground = GetInnerText(actionNode, "runAsBackground", "true").ToLower() == "true";
					Boolean notifyByEmail = GetInnerText(actionNode, "notifyByEmail", "true").ToLower() == "true";
					Boolean addAllUsers = GetInnerText(actionNode, "addAllUsers", "false").ToLower() == "true";

					skapi.UploadData(dataPath, xmlPath, action, runAsBackground, notifyByEmail, addAllUsers);
				}
				else if (action.ToLower() == "poly")
				{
					// TODO handle poly
					String datasetName = GetInnerText(actionNode, "datasetName");
					String datasetId = GetInnerText(actionNode, "datasetId");
				}
			}
		}

		private static String GetInnerText(XmlNode node, String path, String defaultValue = "")
		{
			XmlNode value = node.SelectSingleNode (path);
			return value != null ? value.InnerText : defaultValue;
		}

		public static void Log(string message)
		{
			Console.WriteLine(message);
		}
	}
}
