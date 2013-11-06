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

namespace skdm
{
	class MainClass
	{
		private static string HELP_DESCRIPTION = @"Command line tool to work with the data API or create oAuth tokens.
See http://support.spatialkey.com/dmapi for more information";
		private const string PARAM_TTL = "ttl";
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

				clp.AddCommand(new string[] { "oauth" },"Get oAuth token for given keys", "ORG_API_KEY ORG_SECRET_KEY USER_API_KEY", RunOAuthCommand);
				clp.AddCommand(new string[] { "upload" }, "Upload dataset data", "COMMAND_FILE [[ACTION1] ... [ACTIONN]]", RunUploadCommand);
				clp.AddCommand(new string[] { "suggest" }, "Get suggested config for data file. Type is 'csv' or 'shape'", "COMMAND_FILE ACTION TYPE", RunUploadCommand);

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

			// TODO need different levels of loggin
			//Log(String.Format("oAuth for{0}  Org API Key:    {1}{0}  Org Secret Key: {2}{0}  User API Key:   {3}{0}-----", Environment.NewLine, orgAPIKey, orgSecretKey, userAPIKey));
			Console.WriteLine(OAuth.GetOAuthToken(userAPIKey, orgAPIKey, orgSecretKey, clp.FindOptionValue<int>(PARAM_TTL).Value));

			return true;
		}

		private static Boolean RunUploadCommand(string command, Queue<string> args)
		{
			string configFile;
			List<string> actions;
			string method = "";

			if (command.ToLower() == "suggest")
			{
				if (args.Count < 3)
					return false;

				configFile = args.Dequeue();
				actions = new List<string>(new string[]{args.Dequeue()});

				method = args.Dequeue();
				if (method.ToLower() == "csv")
					method = "ImportCSV";
				else if (method.ToLower() == "shape")
					method = "ImportShapefile";
				else
					return false;

				Log(String.Format("Suggest XML '{0}' for '{1}'", configFile, actions[0]));
			}
			else
			{
				if (args.Count < 1)
					return false;

				configFile = args.Dequeue();
				actions = new List<string>(args);
				args.Clear();
				Log(String.Format("Running XML '{0}'", configFile));
			}

			// TODO use TTL argument

			bool isRanAction = false;
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
					if (!(actions.Count == 0 || actions.Contains(actionName)))
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

					if (command.ToLower() == "suggest")
					{
						String pathData = GetInnerText(actionNode, "pathData");
						String pathXML = GetInnerText(actionNode, "pathXML");

						// TODO only need the first 500 rows

						string uploadId = skapi.Upload(pathData);
						skapi.WaitUploadComplete(uploadId);
						string xml = skapi.GetSampleImportConfiguration(uploadId, method);
						if (xml != null)
						{
							if (File.Exists(pathXML))
							{
								pathXML = SpatialKeyDataManager.GetTempFile("xml", Path.GetFileNameWithoutExtension(pathData)+"_", Path.GetPathRoot(pathXML));
							}
							using (StreamWriter outfile = new StreamWriter(pathXML))
							{
								outfile.Write(xml);
							}
							Log(String.Format("Wrote Sample XML to '{0}'", pathXML));
						}
						skapi.CancelUpload(uploadId);
					}
					else
					{
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
				Log(String.Format("WARNING no upload actions run from {0}.  Check config file and specified actions '{1}'.", configFile, (actions.Count > 0 ? String.Join(", ", actions) : "ALL")));
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
