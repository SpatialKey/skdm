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
		private static string _helpPrefix = @"
skdm [options] <command> [<args>]

The available commands are:
  help    Show general or command specific help
  oauth   Get an oAuth token.
  upload  Upload data

See http://support.spatialkey.com/dmapi for more information
";
		private const string PARAM_XML = "xml";
		private const string PARAM_USERAPI = "user";
		private const string PARAM_ORGAPI = "org";
		private const string PARAM_ORGSECRET = "secret";
		private const string PARAM_ACTIONS = "actions";
		private const string PARAM_COMMAND = "command";
		private const string PARAM_TTL = "ttl";

		private const string ACTION_ALL = "all";
		private const string COMMAND_OAUTH = "oAuth";

		private const int ERROR_SUCCESS = 0;
		private const int ERROR_COMMAND_LINE = 1;
		private const int ERROR_NO_COMMANDS = 2;
		private const int ERROR_RUN_XML = 3;
		private const int ERROR_RUN_OAUTH = 4;

		private static CommandLineParser cmdParser;
		private static String clUserAPIKey;
		private static String clOrgAPIKey;
		private static String clOrgSecretKey;

		private static int errorCode = ERROR_SUCCESS;

		public static void Main(string[] args)
		{
			try
			{
				cmdParser = new CommandLineParser(_helpPrefix, true);
				cmdParser.AddOptionValue<string>(new string[] { PARAM_COMMAND  , "c" }, "Command to run: oAuth returns oAuth token and requires the user, org, and secret key", "COMMAND");
				cmdParser.AddOptionValue<string>(new string[] { PARAM_USERAPI  , "u" }, "User API Key", "KEY", "");
				cmdParser.AddOptionValue<string>(new string[] { PARAM_ORGAPI   , "o" }, "Organization API Key", "KEY", "");
				cmdParser.AddOptionValue<string>(new string[] { PARAM_ORGSECRET, "s" }, "Organization Secret Key", "KEY", "");
				cmdParser.AddOptionValue<int>   (new string[] { PARAM_TTL      , "t" }, "oAuth token time to live in seconds", "TTL", 60);
				cmdParser.AddOptionValue<string>(new string[] { PARAM_XML      , "x" }, "XML Configuration File", "FILE");
				cmdParser.AddOptionList<string> (new string[] { PARAM_ACTIONS  , "a" }, "Actions to perform from XML configuration file", "A1[,A2,...]", new List<string>{ACTION_ALL});
				cmdParser.Parse(args);

				clUserAPIKey = cmdParser.FindOptionValue<String>(PARAM_USERAPI).Value;
				clOrgAPIKey = cmdParser.FindOptionValue<String>(PARAM_ORGAPI).Value;
				clOrgSecretKey = cmdParser.FindOptionValue<String>(PARAM_ORGSECRET).Value;
			}
			catch (Exception ex)
			{
				Console.Write(cmdParser.GetHelpMessage());
				Console.WriteLine();
				Console.WriteLine("Error: " + ex.Message);
				Environment.Exit(ERROR_COMMAND_LINE);
			}

			if (cmdParser.HelpOption.IsMatched)
			{
				Console.Write(cmdParser.GetHelpMessage());
				Environment.Exit(ERROR_SUCCESS);
			}

			bool isRanCommand = false;
			if (GetOAuth())
				isRanCommand = true;
			if (ParseConfigXML())
				isRanCommand = true;

			if (!isRanCommand)
			{
				errorCode = ERROR_NO_COMMANDS;
				Console.Write(cmdParser.GetHelpMessage());
				Console.Write("\nWARNING: No commands performed\n");
			}

			Environment.Exit(errorCode);
		}

		private static void ProcessCommands()
		{
			Stack<string> commands = new Stack<string>(cmdParser.RemainingArgs);

			while (commands.Count > 0)
			{
				string command = commands.Pop().ToLower();
				switch (command)
				{
				case "help":
					break;
				case "oauth":
					break;
				case "upload":
					break;
				default:
					Log(String.Format("ERROR Unknown Command '{0}'", command));
					break;
				}
			}
		}

		private static Boolean GetOAuth()
		{
			if (!COMMAND_OAUTH.Equals(cmdParser.FindOptionValue<string>(PARAM_COMMAND).Value, StringComparison.OrdinalIgnoreCase))
				return false;

			if (clUserAPIKey == null || clUserAPIKey.Length == 0 || clOrgAPIKey == null || clOrgAPIKey.Length == 0 || clOrgSecretKey == null || clOrgSecretKey.Length == 0)
			{
				// TODO need better way of calling out required commands
				Log(String.Format("ERROR: oAuth command requires setting the following parameters: {0}, {1}, and {2}. Optionally set: {3}",
				                  "/"+PARAM_USERAPI, "/"+PARAM_ORGAPI, "/"+PARAM_ORGSECRET, "/"+PARAM_TTL));
				errorCode = ERROR_RUN_OAUTH;
				return true;
			}

			Log("oAuth Key:");
			Log(OAuth.GetOAuthToken(clUserAPIKey, clOrgAPIKey, clOrgSecretKey, cmdParser.FindOptionValue<int>(PARAM_TTL).Value));

			return true;
		}

		private static Boolean ParseConfigXML()
		{
			bool isRanAction = false;
			CommandLineParser.OptionValue<string> configOpt = cmdParser.FindOptionValue<string>(PARAM_XML);
			if (configOpt == null || !configOpt.IsMatched)
				return isRanAction;

			Log(String.Format("Running XML '{0}'", configOpt.Value));

			string configFile = configOpt.Value;
			List<string> actions = cmdParser.FindOptionList<string>(PARAM_ACTIONS).Value;

			bool isUpdateDoc = false;
			XmlDocument doc = new XmlDocument();
			doc.Load(configFile);

			// Default URL info
			String defaultOrganizationURL = GetInnerText(doc, "/config/organizationURL");

			// Default authentication info
			String defaultUserAPIKey = GetInnerText(doc, "/config/userAPIKey", clUserAPIKey);
			String defaultOrganizationAPIKey = GetInnerText(doc, "/config/organizationAPIKey", clOrgAPIKey);
			String defaultOrganizationSecretKey = GetInnerText(doc, "/config/organizationSecretKey", clOrgSecretKey); 

			var actionNodes = doc.SelectNodes("/config/actions/action");

			// Last authenticate
			SpatialKeyDataManager skapi = new SpatialKeyDataManager(Log);

			foreach (XmlNode actionNode in actionNodes)
			{
				try
				{
					String actionName = GetInnerText(actionNode, "@name");
					if (!(actions == null || actions.Count == 0 || actions.Contains(actionName) || actions.FindIndex(x => x.Equals(ACTION_ALL, StringComparison.OrdinalIgnoreCase) ) >= 0))
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

			return isRanAction;
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
