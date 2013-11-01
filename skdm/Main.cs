/// <file>
/// <copyright>Copyright (c) 2013 SpatialKey</copyright>
/// <author>Robert Stehwien</author>
/// <datecreated>2013-04-01</datecreated>
/// </file>
using System;
using System.Xml;
using System.Net;
using CMDLine;

namespace skdm
{
	class MainClass
	{
		private static string _helpPrefix = @"
skdm [options] [<actionName1> ... <actionNameN>]
  <actionNameX>  Optional action names  
                 Will only run given actions, default to running all
See http://support.spatialkey.com/dmapi for more information
";

		private const string PARAM_CONFIG = "/config";

		private static CMDLineParser cmdParser;

		public static void Main(string[] args)
		{
			cmdParser = new CMDLineParser(true, _helpPrefix);
			cmdParser.AddStringParameter(PARAM_CONFIG, "XML Configuration File", false, new string[] {"-config", "/c", "-c"});
			try
			{
				cmdParser.Parse(args);
			}
			catch (Exception ex)
			{
				Console.Write(cmdParser.HelpMessage());
				Console.WriteLine();
				Console.WriteLine("Error: " + ex.Message);
				Environment.Exit(1);
			}

			args = cmdParser.RemainingArgs();

			if (cmdParser.HelpOption.isMatched)
			{
				return;
			}

			bool isAction = false;
			if (ParseConfigXML())
				isAction = true;

			if (!isAction)
			{
				Console.Write(cmdParser.HelpMessage());
				Console.Write("\nWARNING: No operations performed\n");
			}
		}

		private static Boolean ParseConfigXML()
		{
			CMDLineParser.Option configOpt = cmdParser.FindOption(PARAM_CONFIG);
			if (configOpt == null || !configOpt.isMatched)
				return false;

			string configFile = configOpt.Value.ToString();
			string[] args = cmdParser.RemainingArgs();
			string[] actions = (args != null && args.Length > 0) ? args : null;

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
					String actionName = GetInnerText(actionNode, "@name");
					if (actions != null && Array.IndexOf(actions, actionName) < 0)
						continue;

					// Action override URL info
					String organizationName = GetInnerText(doc, "organizationName", defaultOrganizationName);
					String clusterDomainUrl = GetInnerText(doc, "clusterDomainUrl", defaultClusterDomainUrl);

					// Action override authentication info
					String userName = GetInnerText(doc, "userName", defaultUserName);
					String password = GetInnerText(doc, "password", defaultPassword);
					String apiKey = GetInnerText(doc, "apiKey", defaultApiKey); 
					String userId = GetInnerText(doc, "userId", defaultUserId); 

					// Action information
					String type = GetInnerText(actionNode, "type");

					Log(String.Format("Running Action: {0}", actionName));

					if (skapi == null)
					{
						skapi = new SpatialKeyDataManager(organizationName, clusterDomainUrl, userName, password, apiKey, userId, Log);
					}
					else
					{
						skapi.Init(organizationName, clusterDomainUrl, userName, password, apiKey, userId);
					}

					if (type.ToLower() == "overwrite" || type.ToLower() == "append")
					{
						String dataPath = GetInnerText(actionNode, "dataPath");
						String xmlPath = GetInnerText(actionNode, "xmlPath");
						Boolean runAsBackground = GetInnerText(actionNode, "runAsBackground", "true").ToLower() == "true";
						Boolean notifyByEmail = GetInnerText(actionNode, "notifyByEmail", "true").ToLower() == "true";
						Boolean addAllUsers = GetInnerText(actionNode, "addAllUsers", "false").ToLower() == "true";

						skapi.UploadData(dataPath, xmlPath, type, runAsBackground, notifyByEmail, addAllUsers);
					}
					else if (type.ToLower() == "poly")
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

			return true;
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
