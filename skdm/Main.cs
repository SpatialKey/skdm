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

using SpatialKey.DataManager.Lib;
using SpatialKey.DataManager.Lib.Config;
using SpatialKey.DataManager.Lib.Message;

namespace SpatialKey.DataManager.App
{
	class MainClass
	{

		#region Command/Option constants

		// general help description
		private static string HELP_DESCRIPTION = @"Command line tool to work with the data API or create oAuth tokens.";
		// options
		private const string PARAM_CONFIG = "config";
		private const string PARAM_VERSION = "version";
		private const string PARAM_TRACE = "trace";
		private const string PARAM_TTL = "ttl";
		private const string PARAM_NO_WAIT = "no-wait";
		private const string PARAM_KEEP_UPLOADID = "keep-uploadid";
		private const string PARAM_NO_ZIP = "no-zip";
		// commands
		private const string COMMAND_OAUTH = "oauth";
		private const string COMMAND_UPLOAD = "upload";
		private const string COMMAND_SUGGEST = ActionConfig.ACTION_SUGGEST;
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
		private static AuthConfig defaultConfigAuth;

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

				cmd = clp.AddCommand(new string[] { COMMAND_UPLOAD }, "Upload dataset data", "[[ACTION1] ... [ACTIONN]]", RunActions);
				cmd.Parser.AddOptionBoolean(new string[] { PARAM_NO_WAIT }, "Don't wait for import, overwrite, and append actions to complete.");
				cmd.Parser.AddOptionBoolean(new string[] { PARAM_KEEP_UPLOADID }, "Don't cancel the upload id when finished.");
				cmd.Parser.AddOptionBoolean(new string[] { PARAM_NO_ZIP }, "Don't zip uploaded files.");

				clp.AddCommand(new string[] { COMMAND_SUGGEST }, "Get suggested config for data", "[[ACTION1] ... [ACTIONN]]", RunActions);
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
				ShowMessage(MessageLevel.Result, "skdm version " + SpatialKeyDataManager.GetVersion());
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

				defaultConfigAuth = new AuthConfig(doc.SelectSingleNode("/config"));

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

			string orgAPIKey = defaultConfigAuth.OrganizationApiKey;
			string orgSecretKey = defaultConfigAuth.OrganizationSecretKey;
			string userAPIKey = defaultConfigAuth.UserApiKey;
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

            using (SpatialKeyDataManager skapi = new SpatialKeyDataManager(ShowMessage))
            {
                skapi.Init(defaultConfigAuth);
                List<Dictionary<string, string>> list = skapi.DatasetList();
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
            }

			return true;
		}

		private static Boolean RunDeleteCommand(string command, Queue<string> args)
		{
			XmlDocument doc = LoadConfig();
			if (doc == null || args.Count < 1)
				return false;

			List<string> ids = new List<string>(args);
			args.Clear();

            using (SpatialKeyDataManager skapi = new SpatialKeyDataManager(ShowMessage))
            {
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
            }

			return true;
		}

		private static Boolean RunActions(string command, Queue<string> args)
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
			bool isCancelUpload = !clp.FindCommand(COMMAND_UPLOAD).Parser.FindOptionBoolean(PARAM_KEEP_UPLOADID).Value;
			bool isZipUploads = !clp.FindCommand(COMMAND_UPLOAD).Parser.FindOptionBoolean(PARAM_NO_ZIP).Value;

			XmlNodeList actionNodes = doc.SelectNodes("/config/actions/action");
			if (actionNodes == null || actionNodes.Count < 1)
			{
				ShowMessage(MessageLevel.Error, String.Format("No actions defined in '{0}'", configFile));
				return false;
			}

            using (SpatialKeyDataManager skapi = new SpatialKeyDataManager(ShowMessage))
            {
				skapi.IsZipUploads = isZipUploads;

                foreach (XmlNode actionNode in actionNodes)
                {
                    ActionConfig action = new ActionConfig(ShowMessage, actionNode, defaultConfigAuth);
                    action.IsWaitUpdate = isWaitUpdate;
                    if (!(actions.Count == 0 || actions.Contains(action.ActionName)))
                        continue;

                    try
                    {
                        isRanAction = true;

                        ShowMessage(MessageLevel.Status, String.Format("START {0}", action.TraceInfo()));

                        action.Run(skapi);
                    }
                    catch (Exception ex)
                    {
                        ShowMessage(MessageLevel.Error, ex.Message);
                        ShowMessage(MessageLevel.Verbose, ex.Source);
                        ShowMessage(MessageLevel.Verbose, ex.StackTrace);
                    }
                    finally
                    {
                        if (action != null && action.IsUpdateDoc)
                            isUpdateDoc = true;

                        try
                        {
                            if (isCancelUpload && action != null && action.UploadId != null && action.UploadId.Length > 0)
                                skapi.CancelUpload(action.UploadId);
                        }
                        catch
                        {
                        }

                        ShowMessage(MessageLevel.Result, String.Format("FINISH {0}", action.TraceInfo()));
                    }
                }
                skapi.Logout();
            }

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
