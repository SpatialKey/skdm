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

		#region Command/Option constants

		// general help description
		private static string HELP_DESCRIPTION = @"Command line tool to work with the data API or create oAuth tokens.
See http://support.spatialkey.com/dmapi for more information";
		// options
		private const string PARAM_CONFIG = "config";
		private const string PARAM_VERSION = "version";
		private const string PARAM_TRACE = "trace";
		private const string PARAM_TTL = "ttl";
		private const string PARAM_NO_WAIT = "no-wait";
		// commands
		private const string COMMAND_OAUTH = "oauth";
		private const string COMMAND_UPLOAD = "upload";
		private const string COMMAND_SUGGEST = ConfigAction.ACTION_SUGGEST;
		private const string COMMAND_LIST = "list";
		private const string COMMAND_DELETE = "delete";
		// defaults
		private const int TTL_MIN = 10;
		private const int TTL_MAX = 3600;

		#endregion

		#region return values

		private const int EXIT_SUCCESS = 0;
		private const int EXIT_WARNING = 1;
		private const int EXIT_BAD_COMMAND_LINE = 2;
		private const int EXIT_NO_COMMANDS = 3;
		private const int EXIT_ERROR = 4;

		#endregion

		private static int exitCode = EXIT_SUCCESS;
		private static CommandLineParser clp;
		private static CommandLineParser.OptionValue<int> optTrace;
		private static String configFile;
		private static ConfigAuth defaultConfigAuth;

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
				if (optTrace.Value < 0)
					optTrace.Value = 0;
			}
			catch (Exception ex)
			{
				ShowMessage(MessageLevel.Help, clp.GetHelpMessage());
				ShowMessage(MessageLevel.Help, "");
				ShowMessage(MessageLevel.Error, ex.Message);
				Environment.Exit(EXIT_BAD_COMMAND_LINE);
			}

			if (clp.HelpOption.IsMatched)
			{
				ShowMessage(MessageLevel.Help, clp.GetHelpMessage());
				Environment.Exit(EXIT_SUCCESS);
			}
			if (clp.FindOptionBoolean(PARAM_VERSION).IsMatched)
			{
				ShowMessage(MessageLevel.Result, "skdm version 2.0");
				Environment.Exit(EXIT_SUCCESS);
			}

			if (!clp.RunCommands())
			{
				if (exitCode == EXIT_SUCCESS)
					exitCode = EXIT_NO_COMMANDS;
				ShowMessage(MessageLevel.Help, clp.GetHelpMessage());
			}

			Environment.Exit(exitCode);
		}

		private static XmlDocument LoadConfig()
		{
			configFile = clp.FindOptionValue<string>(PARAM_CONFIG).Value;
			try
			{
				if (configFile == null || configFile.Length == 0 || !File.Exists(configFile))
				{
					ShowMessage(MessageLevel.Error, String.Format("XML configuration file '{0}' does not exist", configFile));
					return null;
				}

				XmlDocument doc = new XmlDocument();
				doc.Load(configFile);

				defaultConfigAuth = new ConfigAuth(doc);

				return doc;
			}
			catch (Exception ex)
			{
				ShowMessage(MessageLevel.Error, String.Format("Failed to loading XML configuration file '{0}': {1}", configFile, ex.Message));
				return null;
			}
		}

		private static Boolean RunOAuthCommand(string command, Queue<string> args)
		{
			XmlDocument doc = LoadConfig();
			if (doc == null)
				return false;

			string orgAPIKey = defaultConfigAuth.organizationAPIKey;
			string orgSecretKey = defaultConfigAuth.organizationSecretKey;
			string userAPIKey = defaultConfigAuth.userAPIKey;
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
			skapi.Init(defaultConfigAuth);
			List<Dictionary<string, string>> list = skapi.ListDatasets();
			list.AddRange(skapi.InsuranceList());
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
			skapi.Init(defaultConfigAuth);

			foreach (string id in ids)
			{
				try
				{
					skapi.DatasetDelete(id);
					// TODO need some way to delete insurance
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
				ShowMessage(MessageLevel.Error, String.Format("No actions defined in '{0}'", configFile));
				return false;
			}

			SpatialKeyDataManager skapi = new SpatialKeyDataManager(ShowMessage);

			foreach (XmlNode actionNode in actionNodes)
			{
				ConfigAction action = new ConfigAction(actionNode, defaultConfigAuth);
				if (!(actions.Count == 0 || actions.Contains(action.actionName)))
					continue;

				try
				{
					isRanAction = true;

					ShowMessage(MessageLevel.Status, String.Format("START {0}", action.TraceInfo()));

					skapi.Init(action.configAuth);

					// if the command is suggest, change action to suggest
					if (command.ToLower() == COMMAND_SUGGEST)
						action.actionType = ConfigAction.ACTION_SUGGEST;

					// suggest actions should limit upload lengths
					if (action.actionType == ConfigAction.ACTION_SUGGEST)
						action.pathDataArray = CreateSuggestShortFiles(action.pathDataArray);

					// Upload the data and wait for upload to finish
					UploadAndGetId(skapi, action);

					// perform the specified dataset action
					switch (action.actionType)
					{
					case ConfigAction.ACTION_SUGGEST:
						DoUploadSuggest(skapi, action);
						break;
					case ConfigAction.ACTION_IMPORT:
						DoUploadImport(skapi, action, isWaitUpdate);
						break;
					case ConfigAction.ACTION_OVERWRITE:
						DoUploadOverwrite(skapi, action, isWaitUpdate);
						break;
					case ConfigAction.ACTION_APPEND:
						DoUploadAppend(skapi, action, isWaitUpdate);
						break;
					}
				}
				catch (Exception ex)
				{
					ShowMessage(MessageLevel.Error, ex.ToString());
				}
				finally
				{
					if (action != null && action.isUpdateDoc)
						isUpdateDoc = true;

					try
					{
						if (action != null && action.uploadId != null && action.uploadId.Length > 0)
							skapi.CancelUpload(action.uploadId);
					}
					catch
					{
					}

					ShowMessage(MessageLevel.Result, String.Format("FINISH {0}", action.TraceInfo()));
				}
			}
			skapi.Logout();

			if (isUpdateDoc)
			{
				ShowMessage(MessageLevel.Result, String.Format("UPDATE datasetId(s) in {0}", configFile));
				doc.Save(configFile);
			}

			if (!isRanAction)
			{
				ShowMessage(MessageLevel.Warning, String.Format("No upload actions run from {0}.  Check config file and specified actions '{1}'.", configFile, (actions.Count > 0 ? String.Join(", ", actions) : "ALL")));
			}

			return true;
		}

		static void UploadAndGetId(SpatialKeyDataManager skapi, ConfigAction action)
		{
			string uploadMessage;
			if (action.dataType == ConfigAction.TYPE_INSURANCE && action.pathDataArray.Length == 0)
			{
				// return because when using existing datasets there is nothing to upload
				return;
			}
			else
			{
				uploadMessage = String.Format("Uploading '{0}' for {1}", String.Join(", ", action.pathDataArray), (action.dataType == ConfigAction.TYPE_INSURANCE ? "insurance" : "dataset"));
				ShowMessage(MessageLevel.Status, uploadMessage);
				action.uploadId = skapi.Upload(action.pathDataArray);
			}
			if (action.uploadId == null)
			{
				throw new Exception(uploadMessage + "  Error: No uploadId retrieved.");
			}
			Dictionary<string, object> uploadStausJson = skapi.WaitUploadComplete(action.uploadId);
			if (skapi.IsUploadStatusError(uploadStausJson))
			{
				throw new Exception(String.Format("{0} Error Message: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
			}
			ShowMessage(MessageLevel.Status, String.Format("Finished. {0}", uploadMessage));
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

		private static void DoUploadSuggest(SpatialKeyDataManager skapi, ConfigAction action)
		{
			String method;
			if (action.dataType == ConfigAction.TYPE_CSV)
				method = "ImportCSV";
			else if (action.dataType == ConfigAction.TYPE_SHAPE)
				method = "ImportShapefile";
			else if (action.dataType == ConfigAction.TYPE_INSURANCE)
				method = "ImportInsurance";
			else
			{
				ShowMessage(MessageLevel.Error, String.Format("Unknown dataType '{1}", action.dataType));
				return;
			}

			string xml = skapi.GetSampleConfiguration(action.uploadId, method);
			if (xml != null)
			{
				string pathXML = action.pathXML;
				if (File.Exists(pathXML))
				{
					pathXML = SpatialKeyDataManager.GetTempFile("xml", Path.GetFileNameWithoutExtension(pathXML), Path.GetDirectoryName(pathXML));
				}
				using (StreamWriter outfile = new StreamWriter(action.pathXML))
				{
					outfile.Write(xml);
				}
				ShowMessage(MessageLevel.Status, String.Format("Wrote Suggested XML to '{0}'", pathXML));
			}
		}

		static bool ExtractDatasetIds(SpatialKeyDataManager skapi, ConfigAction action, string uploadMessage, Dictionary<string, object> uploadStausJson)
		{
			Dictionary<string, string> ids = skapi.GetDatasetIDs(uploadStausJson);
			if (action.dataType == ConfigAction.TYPE_INSURANCE)
			{
				string policyId = null;
				string locationId = null;
				string insuranceId = null;
				if (ids != null)
				{
					foreach (KeyValuePair<string, string> cur in ids)
					{
						if (cur.Value.ToLower() == "policy_dataset")
							policyId = cur.Key;
						else if (cur.Value.ToLower() == "location_dataset")
							locationId = cur.Key;
						else if (cur.Value.ToLower() == "insurance")
							insuranceId = cur.Key;
					}
				}
				if (action.pathDataArray.Length == 2)
				{
					if (policyId == null || locationId == null || insuranceId == null)
					{
						ShowMessage(MessageLevel.Error, String.Format("{0} Failed.  Could not find policy, location, and insurance ids: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
						return false;
					}
					XmlDocument doc = new XmlDocument();
					doc.Load(action.pathXML);
					(doc.SelectSingleNode("/insuranceImport/policyDataset") as XmlElement).SetAttribute("id", policyId);
					(doc.SelectSingleNode("/insuranceImport/locationDataset") as XmlElement).SetAttribute("id", locationId);
					doc.Save(action.pathXML);
					ShowMessage(MessageLevel.Result, String.Format("UPDATE wrote policy and location ids to {0}", action.pathXML));
				}
				else if (action.pathDataArray.Length == 0)
				{
					if (insuranceId == null)
					{
						ShowMessage(MessageLevel.Error, String.Format("{0} Failed.  Could not find insurance id: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
						return false;
					}
				}
				else
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed. Incorrect number of ids: {0}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
					return false;
				}
				action.id = insuranceId;
			}
			else
			{
				action.id = ((ids != null && ids.Count == 1) ? (new List<string>(ids.Keys))[0] : null);
				if (action.id == null)
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed.  Could not find dataset Id: {0}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
					return false;
				}
			}
			return true;
		}

		private static void DoUploadImport(SpatialKeyDataManager skapi, ConfigAction action, bool isWaitUpdate)
		{
			string uploadMessage;
			if (action.dataType == ConfigAction.TYPE_INSURANCE && action.pathDataArray.Length == 0)
			{
				uploadMessage = String.Format("Importing insurance using existing dataset ids in '{0}'", action.pathXML);
			}
			else
			{
				uploadMessage = String.Format("Importing '{0}' for {1}", String.Join(", ", action.pathDataArray), (action.dataType == ConfigAction.TYPE_INSURANCE ? "insurance" : "dataset"));
			}

			ShowMessage(MessageLevel.Status, uploadMessage);

			bool isSuccess;
			if (action.dataType == ConfigAction.TYPE_INSURANCE)
			{
				if (action.pathDataArray.Length == 0)
				{
					action.uploadId = skapi.InsuranceCreateExistingDatasets(action.pathXML);
					isSuccess = action.uploadId != null;
				}
				else
				{
					isSuccess = skapi.InsuranceCreate(action.uploadId, action.pathDataArray, action.pathXML);
				}
			}
			else
			{
				isSuccess = skapi.DatasetCreate(action.uploadId, action.pathXML);
			}

			if (!isSuccess)
				return;
				
			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(action.uploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
					return;
				}

				if (!ExtractDatasetIds(skapi, action, uploadMessage, uploadStausJson))
					return;

				ShowMessage(MessageLevel.Status, String.Format("{0} Complete", uploadMessage));
			}
			else
				ShowMessage(MessageLevel.Status, String.Format("{0} Not watiting for completion.", uploadMessage));
		}

		private static void DoUploadAppend(SpatialKeyDataManager skapi, ConfigAction action, bool isWaitUpdate)
		{
			if (action.dataType == ConfigAction.TYPE_INSURANCE || action.dataType == ConfigAction.TYPE_SHAPE)
			{
				ShowMessage(MessageLevel.Error, String.Format("Canot append dataType '{0}'", action.dataType));
				return;
			}

			skapi.DatasetAppend(action.uploadId, action.id, action.pathXML);
			ShowMessage(MessageLevel.Status, String.Format("Appending '{0}'", String.Join(", ", action.pathDataArray)));
			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(action.uploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
				{
					ShowMessage(MessageLevel.Error, String.Format("Append failed: {0}", MiniJson.Serialize(uploadStausJson)));
				}
				else
				{
					ShowMessage(MessageLevel.Status, String.Format("Appended '{0}'", String.Join(", ", action.pathDataArray)));
				}
			}
			else
				ShowMessage(MessageLevel.Status, String.Format("Not waiting for append of '{0}' to complete", String.Join(", ", action.pathDataArray)));
		}

		private static void DoUploadOverwrite(SpatialKeyDataManager skapi, ConfigAction action, bool isWaitUpdate)
		{
			bool isSuccess;
			string uploadMessage;
			if (action.dataType == ConfigAction.TYPE_INSURANCE && action.pathDataArray.Length == 0)
			{
				ShowMessage(MessageLevel.Error, String.Format("Canot do an insurance overwrite with only ids '{0}'", action.pathXML));
				return;
			}
			else
			{
				uploadMessage = String.Format("Overwriting {0} '{1}' using csv '{2}' and config '{3}'", (action.dataType == ConfigAction.TYPE_INSURANCE ? "insurance" : "dataset"), action.id, String.Join(", ", action.pathDataArray), action.pathXML);
				ShowMessage(MessageLevel.Status, uploadMessage);
				if (action.dataType == ConfigAction.TYPE_INSURANCE)
					isSuccess = skapi.InsuranceOverwrite(action.uploadId, action.id, action.pathXML);
				else
					isSuccess = skapi.DatasetOverwrite(action.uploadId, action.id, action.pathXML);
			}

			if (!isSuccess)
				return;

			if (isWaitUpdate)
			{
				Dictionary<string,object> uploadStausJson = skapi.WaitUploadComplete(action.uploadId);
				if (skapi.IsUploadStatusError(uploadStausJson))
				{
					ShowMessage(MessageLevel.Error, String.Format("{0} Failed: {1}", uploadMessage, MiniJson.Serialize(uploadStausJson)));
				}
				else
				{
					if (action.dataType == ConfigAction.TYPE_INSURANCE)
					{
						if (!ExtractDatasetIds(skapi, action, uploadMessage, uploadStausJson))
							return;
					}

					ShowMessage(MessageLevel.Status, String.Format("Finished {0}", uploadMessage));
				}
			}
			else
				ShowMessage(MessageLevel.Status, String.Format("Not waiting for completion. {0}", uploadMessage));
		}

		private static readonly Dictionary<MessageLevel, int> messageLevelMinimumTrace = new Dictionary<MessageLevel, int> {
			{ MessageLevel.Result, 0 },
			{ MessageLevel.Error, 0 },
			{ MessageLevel.Help, 0 },
			{ MessageLevel.Status, 1 },
			{ MessageLevel.Warning, 1 },
			{ MessageLevel.Verbose, 2 }
		};
		private static readonly List<MessageLevel> messageLevelTag = new List<MessageLevel> {
			MessageLevel.Error,
			MessageLevel.Status,
			MessageLevel.Warning
		};

		public static void ShowMessage(MessageLevel level, string message)
		{
			if (level == MessageLevel.Error)
				exitCode = EXIT_ERROR;
			else if (level == MessageLevel.Warning && exitCode == EXIT_SUCCESS)
				exitCode = EXIT_WARNING;

			if (optTrace.Value < messageLevelMinimumTrace[level])
				return;

			StringBuilder str = new StringBuilder(message);
			if (messageLevelTag.Contains(level))
				str.Insert(0, String.Format("[{0} {1}] ", level.ToString().ToUpper(), DateTime.Now.ToString()));

			if (level == MessageLevel.Error || level == MessageLevel.Warning)
				Console.Error.WriteLine(str.ToString());
			else
				Console.WriteLine(str.ToString());
		}
	}
}
