/// <file>
/// <copyright>Copyright (c) 2013 SpatialKey</copyright>
/// <author>Robert Stehwien</author>
/// <datecreated>2013-04-01</datecreated>
/// </file>
using System;
using System.Xml;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace skdm
{
	class MainClass
	{
		private static string HELP_DESCRIPTION = @"Command line tool to work with the data API or create oAuth tokens.
See http://support.spatialkey.com/dmapi for more information";
		private const string PARAM_VERSION = "version";
		private const string PARAM_TRACE = "trace";
		private const string PARAM_TTL = "ttl";
		private const string PARAM_WAIT = "wait";
		private const string COMMAND_OAUTH = "oauth";
		private const string COMMAND_UPLOAD = "upload";
		private const string COMMAND_SUGGEST = "suggest";
		private const string COMMAND_LIST = "list";
		private const string ACTION_SUGGEST = COMMAND_SUGGEST;
		private const string ACTION_IMPORT = "import";
		private const string ACTION_OVERWRITE = "overwrite";
		private const string ACTION_APPEND = "append";
		private static readonly List<string> VALID_ACTIONS = new List<string> {
			ACTION_SUGGEST,
			ACTION_IMPORT,
			ACTION_OVERWRITE,
			ACTION_APPEND
		};
		private const int ERROR_SUCCESS = 0;
		private const int ERROR_COMMAND_LINE = 1;
		private const int ERROR_NO_COMMANDS = 2;
		private const int ERROR_RUN_XML = 3;
		private const int ERROR_RUN_OAUTH = 4;
		private static int errorCode = ERROR_SUCCESS;
		private static CommandLineParser clp;
		private static CommandLineParser.OptionValue<int> optTrace;

		public static void Main(string[] args)
		{
			try
			{
				clp = new CommandLineParser("skdm", HELP_DESCRIPTION);
				clp.MyMessenger = ShowMessage;
				clp.AddOptionHelp();
				clp.AddCommandHelp();
				clp.AddOptionBoolean(new string[] { PARAM_VERSION }, "Get application version");
				optTrace = clp.AddOptionValue<int>(new string[] { PARAM_TRACE }, "Trace debug verbosity: 1 for status, 2 for debug (default 0)", "LEVEL", 0);

				CommandLineParser.Command cmd;

				cmd = clp.AddCommand(new string[] { COMMAND_OAUTH }, "Get oAuth token for given keys", "ORG_API_KEY ORG_SECRET_KEY USER_API_KEY", RunOAuthCommand);
				cmd.Parser.AddOptionValue<int>(new string[] { PARAM_TTL }, "oAuth token time to live in seconds (Default 60)", "TTL", 60);

				cmd = clp.AddCommand(new string[] { COMMAND_UPLOAD }, "Upload dataset data", "COMMAND_FILE [[ACTION1] ... [ACTIONN]]", RunUploadCommand);
				cmd.Parser.AddOptionBoolean(new string[] { PARAM_WAIT }, "Wait for overwrite and append actions to complete.");

				clp.AddCommand(new string[] { COMMAND_SUGGEST }, "Get suggested config for data", "COMMAND_FILE [[ACTION1] ... [ACTIONN]]", RunUploadCommand);
				clp.AddCommand(new string[] { COMMAND_LIST }, "List available datasets", "COMMAND_FILE", RunListCommand);

				clp.Parse(args);
			}
			catch (Exception ex)
			{
				ShowMessage(MessageLevel.Help, clp.GetHelpMessage());
				ShowMessage(MessageLevel.Help, "");
				ShowMessage(MessageLevel.Error, "Error: " + ex.Message);
				Environment.Exit(ERROR_COMMAND_LINE);
			}

			if (clp.HelpOption.IsMatched)
			{
				ShowMessage(MessageLevel.Help, clp.GetHelpMessage());
				Environment.Exit(ERROR_SUCCESS);
			}
			if (clp.FindOptionBoolean(PARAM_VERSION).IsMatched)
			{
				ShowMessage(MessageLevel.Result, "skdm version 2.0");
				Environment.Exit(ERROR_SUCCESS);
			}

			if (!clp.RunCommands())
			{
				errorCode = ERROR_NO_COMMANDS;
				ShowMessage(MessageLevel.Help, clp.GetHelpMessage());
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
			int ttl = clp.FindCommand(COMMAND_OAUTH).Parser.FindOptionValue<int>(PARAM_TTL).Value;

			//Log(String.Format("oAuth for{0}  Org API Key:    {1}{0}  Org Secret Key: {2}{0}  User API Key:   {3}{0}-----", Environment.NewLine, orgAPIKey, orgSecretKey, userAPIKey));
			ShowMessage(MessageLevel.Result, OAuth.GetOAuthToken(userAPIKey, orgAPIKey, orgSecretKey, ttl));

			return true;
		}

		private static Boolean RunListCommand(string command, Queue<string> args)
		{
			if (args.Count < 1)
				return false;

			string configFile = args.Dequeue();
			XmlDocument doc = new XmlDocument();
			doc.Load(configFile);

			// Default authentication info
			String organizationURL = GetInnerText(doc, "/config/organizationURL");
			String userAPIKey = GetInnerText(doc, "/config/userAPIKey");
			String organizationAPIKey = GetInnerText(doc, "/config/organizationAPIKey");
			String organizationSecretKey = GetInnerText(doc, "/config/organizationSecretKey"); 

			SpatialKeyDataManager skapi = new SpatialKeyDataManager(ShowMessage);
			skapi.Init(organizationURL, organizationAPIKey, organizationSecretKey, userAPIKey);
			List<Dictionary<string, string>> list = skapi.ListDatasets();
			StringBuilder text = new StringBuilder();
			foreach (Dictionary<string, string> item in list)
			{
				text.AppendLine("---");
				foreach (string key in item.Keys)
				{
					text.AppendFormat("{0,16}: {1}", key, item[key]);
					text.AppendLine();
				}
				text.AppendLine("---");
			}
			ShowMessage(MessageLevel.Result, text.ToString());

			skapi.Logout();

			return true;
		}

		private static Boolean RunUploadCommand(string command, Queue<string> args)
		{
			if (args.Count < 1)
				return false;

			string configFile = args.Dequeue();
			List<string> actions = new List<string>(args);
			args.Clear();

			bool isWaitUpdate = false;

			if (command.ToLower() == "suggest")
			{
				ShowMessage(MessageLevel.Status, String.Format("Suggest XML '{0}'", configFile));
			}
			else
			{
				ShowMessage(MessageLevel.Status, String.Format("Running XML '{0}'", configFile));
				isWaitUpdate = clp.FindCommand(COMMAND_UPLOAD).Parser.FindOptionBoolean(PARAM_WAIT).Value;
			}

			bool isRanAction = false;
			bool isUpdateDoc = false;

			XmlDocument doc = new XmlDocument();
			doc.Load(configFile);

			XmlNodeList actionNodes = doc.SelectNodes("/config/actions/action");
			if (actionNodes == null || actionNodes.Count < 1)
			{
				ShowMessage(MessageLevel.Error, String.Format("ERROR: No actions defined in '{0}'", configFile));
				return false;
			}

			// Default authentication info
			String defaultOrganizationURL = GetInnerText(doc, "/config/organizationURL");
			String defaultUserAPIKey = GetInnerText(doc, "/config/userAPIKey");
			String defaultOrganizationAPIKey = GetInnerText(doc, "/config/organizationAPIKey");
			String defaultOrganizationSecretKey = GetInnerText(doc, "/config/organizationSecretKey"); 

			SpatialKeyDataManager skapi = new SpatialKeyDataManager(ShowMessage);

			foreach (XmlNode actionNode in actionNodes)
			{
				try
				{
					String actionName = GetInnerText(actionNode, "@name");
					if (!(actions.Count == 0 || actions.Contains(actionName)))
						continue;

					isRanAction = true;

					ShowMessage(MessageLevel.Status, String.Format("Running Action: {0}", actionName));

					// Action override authentication info
					String organizationURL = GetInnerText(doc, "organizationURL", defaultOrganizationURL);
					String userAPIKey = GetInnerText(doc, "userAPIKey", defaultUserAPIKey); 
					String organizationAPIKey = GetInnerText(doc, "organizationAPIKey", defaultOrganizationAPIKey); 
					String organizationSecretKey = GetInnerText(doc, "organizationSecretKey", defaultOrganizationSecretKey); 

					skapi.Init(organizationURL, organizationAPIKey, organizationSecretKey, userAPIKey);

					// common data
					String pathData = GetInnerText(actionNode, "pathData");
					String pathXML = GetInnerText(actionNode, "pathXML");
					String datasetId = GetInnerText(actionNode, "datasetId");
					String dataType = GetInnerText(actionNode, "dataType").ToLower();

					// get the action type
					String actionType;
					if (command.ToLower() == COMMAND_SUGGEST)
					{
						actionType = ACTION_SUGGEST;
					}
					else
					{
						actionType = GetInnerText(actionNode, "actionType").ToLower();
						if (!VALID_ACTIONS.Contains(actionType))
						{
							ShowMessage(MessageLevel.Error, String.Format("ERROR invalid actionType: {0}", actionType));
							continue;
						}
						if (datasetId == null || datasetId.Length == 0)
							actionType = ACTION_IMPORT;
					}

					// TODO if action is ACTION_SUGGEST only need the first 500 lines

					// Upload the data and wait for upload to finish
					string uploadId = skapi.Upload(pathData);
					if (uploadId == null)
					{
						// TODO  neeed better error
						ShowMessage(MessageLevel.Error, "ERROR Upload Failed");
						continue;
					}
					Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
					if (SpatialKeyDataManager.IsUploadStatusError(uploadStausJson))
					{
						// TODO show uploadStatusJson error
						ShowMessage(MessageLevel.Error, "ERROR uploading");
						continue;
					}

					// perform the specified dataset action
					switch (actionType)
					{
					case ACTION_SUGGEST:
						DoUploadSuggest(skapi, pathXML, pathData, dataType, uploadId);
						break;
					case ACTION_IMPORT:
						datasetId = DoUploadImport(skapi, pathXML, uploadId);
						if (datasetId != null)
						{
							if (actionNode.SelectSingleNode("datasetId") == null)
								actionNode.AppendChild(doc.CreateElement("datasetId"));
							actionNode.SelectSingleNode("datasetId").InnerText = datasetId;
							isUpdateDoc = true;
						}
						break;
					case ACTION_OVERWRITE:
						DoUploadOverwrite(skapi, pathXML, uploadId, datasetId, isWaitUpdate);
						break;
					case ACTION_APPEND:
						DoUploadAppend(skapi, pathXML, uploadId, datasetId, isWaitUpdate);
						break;
					}

					ShowMessage(MessageLevel.Status, String.Format("Finished Action: {0}", actionName));
				}
				catch (Exception ex)
				{
					ShowMessage(MessageLevel.Error, String.Format("Error: {0}", ex.ToString()));
					errorCode = ERROR_RUN_XML;
				}
			}
			skapi.Logout();

			if (isUpdateDoc)
				doc.Save(configFile);

			if (!isRanAction)
			{
				ShowMessage(MessageLevel.Warning, String.Format("WARNING no upload actions run from {0}.  Check config file and specified actions '{1}'.", configFile, (actions.Count > 0 ? String.Join(", ", actions) : "ALL")));
			}

			return true;
		}

		private static void DoUploadSuggest(SpatialKeyDataManager skapi, string pathXML, string pathData, string dataType, string uploadId)
		{
			String method;
			if (dataType == "csv")
				method = "ImportCSV";
			else if (dataType == "shape" || dataType == "shapefile")
				method = "ImportShapefile";
			else
			{
				ShowMessage(MessageLevel.Error, String.Format("ERROR Unknown dataType '{1}", dataType));
				return;
			}

			string xml = skapi.GetSampleImportConfiguration(uploadId, method);
			if (xml != null)
			{
				if (File.Exists(pathXML))
				{
					pathXML = SpatialKeyDataManager.GetTempFile("xml", Path.GetFileNameWithoutExtension(pathData) + "_", Path.GetPathRoot(pathXML));
				}
				using (StreamWriter outfile = new StreamWriter(pathXML))
				{
					outfile.Write(xml);
				}
				ShowMessage(MessageLevel.Result, String.Format("Wrote Suggested XML to '{0}'", pathXML));
			}
			skapi.CancelUpload(uploadId);
		}

		private static string DoUploadImport(SpatialKeyDataManager skapi, string pathXML, string uploadId)
		{
			String datasetId = null;

			if (skapi.Import(uploadId, pathXML))
			{
				// must wait on the import status to get the datasetid
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
				datasetId = skapi.GetDatasetID(uploadStausJson);

			}

			return datasetId;
		}

		private static void DoUploadAppend(SpatialKeyDataManager skapi, string pathXML, string uploadId, string datasetId, bool isWaitUpdate)
		{
			skapi.Append(uploadId, datasetId, pathXML);
			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
				if (SpatialKeyDataManager.IsUploadStatusError(uploadStausJson))
				{
					// TODO show uploadStatusJson error
					ShowMessage(MessageLevel.Error, "ERROR Running Append");
				}
			}
		}

		private static void DoUploadOverwrite(SpatialKeyDataManager skapi, string pathXML, string uploadId, string datasetId, bool isWaitUpdate)
		{
			skapi.Overwrite(uploadId, datasetId, pathXML);
			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
				if (SpatialKeyDataManager.IsUploadStatusError(uploadStausJson))
				{
					// TODO show uploadStatusJson error
					ShowMessage(MessageLevel.Error, "ERROR Running Overwrite");
				}
			}
		}

		private static String GetInnerText(XmlNode node, String path, String defaultValue = "")
		{
			if (defaultValue == null)
				defaultValue = "";

			XmlNode value = node.SelectSingleNode(path);
			return value != null ? value.InnerText : defaultValue;
		}

		public static void ShowMessage(MessageLevel level, string message)
		{
			if (optTrace.Value < (int)level)
				return;
			Console.WriteLine(message);
		}
	}
}
