/// <file>
/// <copyright>Copyright (c) 2013 SpatialKey</copyright>
/// <author>Robert Stehwien</author>
/// <datecreated>2013-04-01</datecreated>
/// </file>
using System;
using System.Xml;
using System.Net;
using System.Collections.Generic;

namespace skdm
{
	class MainClass
	{
		private static string HELP_DESCRIPTION = @"Command line tool to work with the data API or create oAuth tokens.
See http://support.spatialkey.com/dmapi for more information";
		private const string PARAM_TTL = "ttl";
		private const string ACTION_ALL = "all";
		private const string COMMAND_OAUTH = "oAuth";
		private const int ERROR_SUCCESS = 0;
		private const int ERROR_COMMAND_LINE = 1;
		private const int ERROR_NO_COMMANDS = 2;
		private const int ERROR_RUN_XML = 3;
		private const int ERROR_RUN_OAUTH = 4;
		private static CommandLineParser clp;
		private static int errorCode = ERROR_SUCCESS;

		public static void Main(string[] args)
		{
			try
			{
				clp = new CommandLineParser("skdm", HELP_DESCRIPTION);
				clp.AddOptionHelp();
				clp.AddCommandHelp();
				clp.AddOptionValue<int>(new string[] { PARAM_TTL, "-t" }, "oAuth token time to live in seconds (Default 60)", "TTL", 60);

				CommandLineParser oauthCLP = new CommandLineParser("oauth", "Get oAuth token for given keys", "ORG_API_KEY ORG_SECRET_KEY USER_API_KEY");
				clp.AddCommand(new string[] { "oauth" }, oauthCLP.Description, RunOAuthCommand, oauthCLP);
				
				CommandLineParser uploadCLP = new CommandLineParser("upload", "Upload dataset data", "COMMAND_FILE [[ACTION1] ... [ACTIONN]]");
				clp.AddCommand(new string[] { "upload" }, "Upload data over the API", RunUploadCommand, uploadCLP);

				clp.Parse(args);
			}
			catch (Exception ex)
			{
				Console.Write(clp.GetHelpMessage());
				Console.WriteLine();
				Console.WriteLine("Error: " + ex.Message);
				Environment.Exit(ERROR_COMMAND_LINE);
			}

			if (clp.HelpOption.IsMatched)
			{
				Console.Write(clp.GetHelpMessage());
				Environment.Exit(ERROR_SUCCESS);
			}

			if (!clp.RunCommands())
			{
				errorCode = ERROR_NO_COMMANDS;
				Console.WriteLine(clp.GetHelpMessage());
			}

			Environment.Exit(errorCode);
		}

		private static Boolean RunOAuthCommand(string command, Queue<string> args)
		{
			if (args.Count < 3)
				return false;

			string orgAPIKey = args.Dequeue();
			string orgSecretKey = args.Dequeue();
			string userAPIKey = args.Dequeue();

			Log(String.Format("oAuth for Org API Key: {0} Org Secret Key: {1} User API Key: {2}", orgAPIKey, orgSecretKey, userAPIKey));
			Log(OAuth.GetOAuthToken(userAPIKey, orgAPIKey, orgSecretKey, clp.FindOptionValue<int>(PARAM_TTL).Value));

			return true;
		}

		private static Boolean RunUploadCommand(string command, Queue<string> args)
		{
			if (args.Count < 1)
				return false;

			bool isRanAction = false;
			string configFile = args.Dequeue();
			Log(String.Format("Running XML '{0}'", configFile));

			List<string> actions = new List<string>(args);
			args.Clear();
			if (actions.Count < 1)
				actions.Add(ACTION_ALL);

			bool isUpdateDoc = false;
			XmlDocument doc = new XmlDocument();
			doc.Load(configFile);

			// Default URL info
			String defaultOrganizationURL = GetInnerText(doc, "/config/organizationURL");

			// Default authentication info
			String defaultUserAPIKey = GetInnerText(doc, "/config/userAPIKey");
			String defaultOrganizationAPIKey = GetInnerText(doc, "/config/organizationAPIKey");
			String defaultOrganizationSecretKey = GetInnerText(doc, "/config/organizationSecretKey"); 

			var actionNodes = doc.SelectNodes("/config/actions/action");

			// Last authenticate
			SpatialKeyDataManager skapi = new SpatialKeyDataManager(Log);

			foreach (XmlNode actionNode in actionNodes)
			{
				try
				{
					String actionName = GetInnerText(actionNode, "@name");
					if (!(actions == null || actions.Count == 0 || actions.Contains(actionName) || actions.FindIndex(x => x.Equals(ACTION_ALL, StringComparison.OrdinalIgnoreCase)) >= 0))
						continue;

					isRanAction = true;

					Log(String.Format("Running Action: {0}", actionName));

					// Action override URL info
					String organizationURL = GetInnerText(doc, "organizationURL", defaultOrganizationURL);

					// Action override authentication info
					String userAPIKey = GetInnerText(doc, "userAPIKey", defaultUserAPIKey); 
					String organizationAPIKey = GetInnerText(doc, "organizationAPIKey", defaultOrganizationAPIKey); 
					String organizationSecretKey = GetInnerText(doc, "organizationSecretKey", defaultOrganizationSecretKey); 

					skapi.Init(organizationURL, organizationAPIKey, organizationSecretKey, userAPIKey);

					// Action information
					String type = GetInnerText(actionNode, "type").ToLower();
					if (type == "import" || type == "overwrite" || type == "append")
					{
						String pathData = GetInnerText(actionNode, "pathData");
						String pathXML = GetInnerText(actionNode, "pathXML");
						String datasetId = GetInnerText(actionNode, "datasetId");
						string uploadId = null;

						if (type == "import" || datasetId == null || datasetId.Length == 0)
						{
							uploadId = skapi.Upload(pathData);
							if (skapi.Import(uploadId, pathXML))
							{
								skapi.WaitUploadComplete(uploadId);
							}
						}

					}
					else
					{
						Log(String.Format("ERROR Action '{0}' had an unknown type '{1}", actionName, type));
					}

					Log(String.Format("Finished Action: {0}", actionName));
				}
				catch (Exception ex)
				{
					Log(String.Format("Error: {0}", ex.ToString()));
					errorCode = ERROR_RUN_XML;
				}
			}
			skapi.Logout();

			if (isUpdateDoc)
				doc.Save(configFile);

			if (!isRanAction)
			{
				Log(String.Format("WARNING no upload actions run from {0}.  Check config file and specified actions '{1}'.", configFile, String.Join(", ", actions)));
			}

			return true;
		}

		private static String GetInnerText(XmlNode node, String path, String defaultValue = "")
		{
			if (defaultValue == null)
				defaultValue = "";

			XmlNode value = node.SelectSingleNode(path);
			return value != null ? value.InnerText : defaultValue;
		}

		public static void Log(string message)
		{
			Console.WriteLine(message);
		}
	}
}
