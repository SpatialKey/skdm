/// <file>
/// <copyright>Copyright (c) 2013 SpatialKey</copyright>
/// <author>Robert Stehwien</author>
/// <datecreated>2013-04-01</datecreated>
/// </file>
using System;
using System.Xml;
using System.Net;

namespace skdm
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			string configFile = "SpatailKeyDataManagerConfig.xml";
			if (args.Length > 0)
				configFile = args[0];
			
			XmlDocument doc = new XmlDocument();
			doc.Load(configFile);

			// Default URL info
			String defaultOrganizationName = GetInnerText(doc, "/config/organizationName");
			String defaultClusterDomainUrl = GetInnerText(doc, "/config/clusterDomainUrl");

			// Default authentication info
			String defaultUserName = GetInnerText(doc, "/config/userName");
			String defaultPassword = GetInnerText(doc, "/config/password");
			String defaultApiKey = GetInnerText(doc, "/config/apiKey"); 
			String defaultUserId = GetInnerText(doc, "/config/userId"); 

			var actionNodes = doc.SelectNodes("/config/actions/action");

			// Last authenticate
			SpatialKeyDataManager skapi = null;

			foreach (XmlNode actionNode in actionNodes)
			{
				try
				{
					// Action override URL info
					String organizationName = GetInnerText(doc, "organizationName", defaultOrganizationName);
					String clusterDomainUrl = GetInnerText(doc, "clusterDomainUrl", defaultClusterDomainUrl);

					// Action override authentication info
					String userName = GetInnerText(doc, "userName", defaultUserName);
					String password = GetInnerText(doc, "password", defaultPassword);
					String apiKey = GetInnerText(doc, "apiKey", defaultApiKey); 
					String userId = GetInnerText(doc, "userId", defaultUserId); 

					// Action information
					String action = GetInnerText(actionNode, "action");
					String actionName = GetInnerText(actionNode, "@name");

					Log(String.Format("Running Action: {0}", actionName));

					if (skapi == null)
					{
						skapi = new SpatialKeyDataManager(organizationName, clusterDomainUrl, userName, password, apiKey, userId, Log);
					}
					else
					{
						skapi.Init(organizationName, clusterDomainUrl, userName, password, apiKey, userId);
					}
					
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
						String dataPath = GetInnerText(actionNode, "dataPath");
						skapi.UploadShape(dataPath, datasetName, datasetId);
					}

					Log(String.Format("Finished Action: {0}", actionName));
				}
				catch (Exception ex)
				{
					Log(String.Format("Error: {0}", ex.ToString()));
				}
			}
		}

		private static String GetInnerText(XmlNode node, String path, String defaultValue = "")
		{
			XmlNode value = node.SelectSingleNode(path);
			return value != null ? value.InnerText : defaultValue;
		}

		public static void Log(string message)
		{
			Console.WriteLine(message);
		}
	}
}
