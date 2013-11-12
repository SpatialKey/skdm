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
		private const string PARAM_CONFIG = "config";
		private const string PARAM_VERSION = "version";
		private const string PARAM_TRACE = "trace";
		private const string PARAM_TTL = "ttl";
		private const string PARAM_NO_WAIT = "no-wait";
		private const string COMMAND_OAUTH = "oauth";
		private const string COMMAND_UPLOAD = "upload";
		private const string COMMAND_SUGGEST = "suggest";
		private const string COMMAND_LIST = "list";
		private const string COMMAND_DELETE = "delete";
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
		private const int TTL_MIN = 10;
		private const int TTL_MAX = 3600;
		private static int errorCode = ERROR_SUCCESS;
		private static CommandLineParser clp;
		private static CommandLineParser.OptionValue<int> optTrace;
		private static String configFile;
		private static String defaultOrganizationURL;
		private static String defaultUserAPIKey;
		private static String defaultOrganizationAPIKey;
		private static String defaultOrganizationSecretKey;

		public static void Main(string[] args)
		{
			try
			{
				clp = new CommandLineParser("skdm", HELP_DESCRIPTION);
				clp.MyMessenger = ShowMessage;
				clp.AddOptionHelp();
				clp.AddCommandHelp();
				clp.AddOptionValue<string>(new string[] { PARAM_CONFIG, "c" }, "Data Manager Config XML file. (Default SpatialKeyDataManagerConfig.xml)", "CONFIG_XML", "SpatialKeyDataManagerConfig.xml");
				clp.AddOptionBoolean(new string[] { PARAM_VERSION }, "Get application version");
				optTrace = clp.AddOptionValue<int>(new string[] { PARAM_TRACE }, "Trace debug verbosity: 1 for status, 2 for debug (Default 0)", "LEVEL", 0);

				CommandLineParser.Command cmd;

				cmd = clp.AddCommand(new string[] { COMMAND_OAUTH }, "Get oAuth token for keys in XML configuration or by passing in values", "[ORG_API_KEY ORG_SECRET_KEY USER_API_KEY]", RunOAuthCommand);
				cmd.Parser.AddOptionValue<int>(new string[] { PARAM_TTL }, String.Format("oAuth token time to live in seconds. Min {0}, Max {1} (Default 60)", TTL_MIN, TTL_MAX), "TTL", 60);

				cmd = clp.AddCommand(new string[] { COMMAND_UPLOAD }, "Upload dataset data", "[[ACTION1] ... [ACTIONN]]", RunUploadCommand);
				cmd.Parser.AddOptionBoolean(new string[] { PARAM_NO_WAIT }, "Don't wait for import, overwrite, and append actions to complete.");

				clp.AddCommand(new string[] { COMMAND_SUGGEST }, "Get suggested config for data", "[[ACTION1] ... [ACTIONN]]", RunUploadCommand);
				clp.AddCommand(new string[] { COMMAND_LIST }, "List available datasets", "", RunListCommand);
				clp.AddCommand(new string[] { COMMAND_DELETE }, "Delete datasets by id", "ID [[ID] ... [ID]]", RunDeleteCommand);

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

		private static XmlDocument LoadConfig()
		{
			configFile = clp.FindOptionValue<string>(PARAM_CONFIG).Value;
			try
			{
				if (configFile == null || configFile.Length == 0 || !File.Exists(configFile))
				{
					ShowMessage(MessageLevel.Error, String.Format("ERROR XML configuration file '{0}' does not exist", configFile));
					return null;
				}

				XmlDocument doc = new XmlDocument();
				doc.Load(configFile);

				defaultOrganizationURL = GetInnerText(doc, "/config/organizationURL");
				defaultUserAPIKey = GetInnerText(doc, "/config/userAPIKey");
				defaultOrganizationAPIKey = GetInnerText(doc, "/config/organizationAPIKey");
				defaultOrganizationSecretKey = GetInnerText(doc, "/config/organizationSecretKey"); 

				return doc;
			}
			catch (Exception ex)
			{
				ShowMessage(MessageLevel.Error, String.Format("ERROR loading XML configuration file '{0}': {1}", configFile, ex.Message));
				return null;
			}
		}

		private static Boolean RunOAuthCommand(string command, Queue<string> args)
		{
			XmlDocument doc = LoadConfig();
			if (doc == null)
				return false;

			string orgAPIKey = defaultOrganizationAPIKey;
			string orgSecretKey = defaultOrganizationSecretKey;
			string userAPIKey = defaultUserAPIKey;
			int ttl = clp.FindCommand(COMMAND_OAUTH).Parser.FindOptionValue<int>(PARAM_TTL).Value;

			if (ttl < TTL_MIN)
				ttl = TTL_MIN;
			else if (ttl > TTL_MAX)
				ttl = 3600;

			if (args.Count >= 3)
			{
				orgAPIKey = args.Dequeue();
				orgSecretKey = args.Dequeue();
				userAPIKey = args.Dequeue();
			}

			StringBuilder text = new StringBuilder();
			text.AppendLine("oAuth Token For");
			text.AppendFormat("{0,24}: {1}", "Organization API Key", orgAPIKey);
			text.AppendLine();
			text.AppendFormat("{0,24}: {1}", "Organization Secret Key", orgSecretKey);
			text.AppendLine();
			text.AppendFormat("{0,24}: {1}", "User API Key", userAPIKey);
			text.AppendLine();
			text.AppendFormat("{0,24}: {1}", "TTL (seconds)", ttl);
			text.AppendLine();
			text.AppendLine("-----");

			ShowMessage(MessageLevel.Status, text.ToString());
			ShowMessage(MessageLevel.Result, OAuth.GetOAuthToken(userAPIKey, orgAPIKey, orgSecretKey, ttl));

			return true;
		}

		private static Boolean RunListCommand(string command, Queue<string> args)
		{
			XmlDocument doc = LoadConfig();
			if (doc == null)
				return false;

			SpatialKeyDataManager skapi = new SpatialKeyDataManager(ShowMessage);
			skapi.Init(defaultOrganizationURL, defaultOrganizationAPIKey, defaultOrganizationSecretKey, defaultUserAPIKey);
			List<Dictionary<string, string>> list = skapi.ListDatasets();
			if (list == null || list.Count < 1)
			{
				ShowMessage(MessageLevel.Result, "Dataset List Empty");
				return true;
			}
			StringBuilder text = new StringBuilder();
			foreach (Dictionary<string, string> item in list)
			{
				text.AppendLine("---");
				foreach (string key in item.Keys)
				{
					text.AppendFormat("{0,16}: {1}", key, item[key]);
					text.AppendLine();
				}
			}
			ShowMessage(MessageLevel.Result, text.ToString());

			skapi.Logout();

			return true;
		}

		private static Boolean RunDeleteCommand(string command, Queue<string> args)
		{
			XmlDocument doc = LoadConfig();
			if (doc == null || args.Count < 1)
				return false;

			List<string> ids = new List<string>(args);
			args.Clear();

			SpatialKeyDataManager skapi = new SpatialKeyDataManager(ShowMessage);
			skapi.Init(defaultOrganizationURL, defaultOrganizationAPIKey, defaultOrganizationSecretKey, defaultUserAPIKey);

			foreach (string id in ids)
			{
				try
				{
					skapi.DeleteDataset(id);
				}
				catch (Exception)
				{
				}
			}

			skapi.Logout();

			return true;
		}

		private static Boolean RunUploadCommand(string command, Queue<string> args)
		{
			XmlDocument doc = LoadConfig();
			if (doc == null)
				return false;

			List<string> actions = new List<string>(args);
			args.Clear();

			bool isWaitUpdate = true;

			if (command.ToLower() == "suggest")
			{
				ShowMessage(MessageLevel.Status, String.Format("Suggest XML '{0}'", configFile));
			}
			else
			{
				ShowMessage(MessageLevel.Status, String.Format("Running XML '{0}'", configFile));
				isWaitUpdate = !clp.FindCommand(COMMAND_UPLOAD).Parser.FindOptionBoolean(PARAM_NO_WAIT).Value;
			}

			bool isRanAction = false;
			bool isUpdateDoc = false;

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
					String[] pathDataArray = GetInnerTextList(actionNode, "pathData");
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
						if (actionType == ACTION_APPEND || actionType == ACTION_OVERWRITE)
						{
							if (datasetId == null || datasetId.Length == 0)
								actionType = ACTION_IMPORT;
						}
					}

					// suggest actions should limit upload lengths
					if (actionType == ACTION_SUGGEST)
						pathDataArray = CreateSuggestShortFiles(pathDataArray);

					// Upload the data and wait for upload to finish
					string uploadId = skapi.Upload(pathDataArray);
					if (uploadId == null)
					{
						// error logged in API
						continue;
					}
					Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
					if (skapi.IsUploadStatusError(uploadStausJson))
					{
						ShowMessage(MessageLevel.Error, String.Format("Error uploading: {0}", MiniJson.Serialize(uploadStausJson)));
						continue;
					}
					ShowMessage(MessageLevel.Result, String.Format("Uploaded '{0}'", String.Join(", ", pathDataArray)));

					// perform the specified dataset action
					switch (actionType)
					{
					case ACTION_SUGGEST:
						DoUploadSuggest(skapi, pathDataArray, pathXML, dataType, uploadId);
						break;
					case ACTION_IMPORT:
						datasetId = DoUploadImport(skapi, pathDataArray, pathXML, uploadId, isWaitUpdate);
						if (datasetId != null)
						{
							if (actionNode.SelectSingleNode("datasetId") == null)
								actionNode.AppendChild(doc.CreateElement("datasetId"));
							actionNode.SelectSingleNode("datasetId").InnerText = datasetId;
							isUpdateDoc = true;
						}
						break;
					case ACTION_OVERWRITE:
						DoUploadOverwrite(skapi, pathDataArray, pathXML, uploadId, datasetId, isWaitUpdate);
						break;
					case ACTION_APPEND:
						DoUploadAppend(skapi, pathDataArray, pathXML, uploadId, datasetId, isWaitUpdate);
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
			{
				ShowMessage(MessageLevel.Result, String.Format("Updating {0}", configFile));
				doc.Save(configFile);
			}

			if (!isRanAction)
			{
				ShowMessage(MessageLevel.Warning, String.Format("WARNING no upload actions run from {0}.  Check config file and specified actions '{1}'.", configFile, (actions.Count > 0 ? String.Join(", ", actions) : "ALL")));
			}

			return true;
		}

		private static string[] CreateSuggestShortFiles(string[] pathDataArray)
		{
			List<string> list = new List<string>();
			foreach (string path in pathDataArray)
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

		private static void DoUploadSuggest(SpatialKeyDataManager skapi, string[] pathDataArray, string pathXML, string dataType, string uploadId)
		{
			String method;
			if (dataType == "csv")
				method = "ImportCSV";
			else if (dataType == "shape" || dataType == "shapefile")
				method = "ImportShapefile";
			else if (dataType == "insurance")
				method = "ImportInsurance";
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
					//pathXML = SpatialKeyDataManager.GetTempFile("xml", Path.GetFileNameWithoutExtension(pathXML), Path.GetPathRoot(pathXML));
					pathXML = SpatialKeyDataManager.GetTempFile("xml", Path.GetFileNameWithoutExtension(pathXML), Path.GetDirectoryName(pathXML));
				}
				using (StreamWriter outfile = new StreamWriter(pathXML))
				{
					outfile.Write(xml);
				}
				ShowMessage(MessageLevel.Result, String.Format("Wrote Suggested XML to '{0}'", pathXML));
			}
			skapi.CancelUpload(uploadId);
		}

		private static string DoUploadImport(SpatialKeyDataManager skapi, string[] pathDataArray, string pathXML, string uploadId, bool isWaitUpdate)
		{
			String datasetId = null;

			if (skapi.Import(uploadId, pathXML))
			{
				if (isWaitUpdate)
				{
					Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
					datasetId = skapi.GetDatasetID(uploadStausJson);
					if (skapi.IsUploadStatusError(uploadStausJson) || datasetId == null)
					{
						ShowMessage(MessageLevel.Error, String.Format("Error Running Import: {0}", MiniJson.Serialize(uploadStausJson)));
					}
					else
					{
						datasetId = skapi.GetDatasetID(uploadStausJson);
						ShowMessage(MessageLevel.Result, String.Format("Imported '{0}'", String.Join(", ", pathDataArray)));
					}
				}
				else
					ShowMessage(MessageLevel.Result, String.Format("Not waiting for import of '{0}' to complete", String.Join(", ", pathDataArray)));
			}

			return datasetId;
		}

		private static void DoUploadAppend(SpatialKeyDataManager skapi, string[] pathDataArray, string pathXML, string uploadId, string datasetId, bool isWaitUpdate)
		{
			skapi.Append(uploadId, datasetId, pathXML);
			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
				{
					ShowMessage(MessageLevel.Error, String.Format("Error Running Append: {0}", MiniJson.Serialize(uploadStausJson)));
				}
				else
				{
					ShowMessage(MessageLevel.Result, String.Format("Appended '{0}'", String.Join(", ", pathDataArray)));
				}
			}
			else
				ShowMessage(MessageLevel.Result, String.Format("Not waiting for append of '{0}' to complete", String.Join(", ", pathDataArray)));
		}

		private static void DoUploadOverwrite(SpatialKeyDataManager skapi, string[] pathDataArray, string pathXML, string uploadId, string datasetId, bool isWaitUpdate)
		{
			skapi.Overwrite(uploadId, datasetId, pathXML);
			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(uploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
				{
					ShowMessage(MessageLevel.Error, String.Format("Error Running Overwrite: {0}", MiniJson.Serialize(uploadStausJson)));
				}
				else
				{
					ShowMessage(MessageLevel.Result, String.Format("Overwrote with '{0}'", String.Join(", ", pathDataArray)));
				}
			}
			else
				ShowMessage(MessageLevel.Result, String.Format("Not waiting for overwrite of '{0}' to complete", String.Join(", ", pathDataArray)));
		}

		private static String GetInnerText(XmlNode node, String path, String defaultValue = "")
		{
			if (defaultValue == null)
				defaultValue = "";

			XmlNode value = node.SelectSingleNode(path);
			return value != null ? value.InnerText : defaultValue;
		}

		private static String[] GetInnerTextList(XmlNode node, String path)
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

		public static void ShowMessage(MessageLevel level, string message)
		{
			if (optTrace.Value < (int)level)
				return;
			Console.WriteLine(message);
		}
	}
}
