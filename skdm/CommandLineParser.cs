using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using SpatialKey.DataManager.Lib.Config;
using SpatialKey.DataManager.Lib.Message;

namespace SpatialKey.DataManager.App
{
	public class CommandLineParser
	{
		public const String DEFAULT_PROMPT = "VALUE";
		public static readonly String[] DEFAULT_HELP_KEYS = new string[] { "help", "h", "?" };
		public const String DEFAULT_HELP_DESCRIPTION = "Show help message";

		public delegate Boolean RunCommand(string command, Queue<string> args);
		public delegate void Messager(MessageLevel level, string message);

		private List<IOption> _options = new List<IOption>();
		private List<Command> _commands = new List<Command>();

		#region Public Members

		public  OptionBoolean HelpOption { get; protected set; }

		public List<String> RemainingArgs { get; protected set; }

		public String Name { get; set; }

		public String Description { get; set; }

		public String CommandArguments { get; set; }

		public IOption[] Options { get { return _options.ToArray(); } }

		public Messager MyMessenger { get; set; }

		#endregion

		#region Constructor

		public CommandLineParser(string name, string description = null, string commandArguments = "")
		{
			Name = name;
			Description = description;
			CommandArguments = commandArguments;
		}

		private void ShowMessage(MessageLevel level, string message)
		{
			if (MyMessenger != null)
				MyMessenger(level, message);
			else
				ShowMessage(MessageLevel.Help, message);
		}

		#endregion

		public Command AddCommand(String[] keys, string description, string commandArguments, RunCommand run, CommandLineParser parser = null)
		{
			Command cmd = FindCommand(keys);
			if (cmd != null)
				throw new ExceptionCommand(String.Format("The command '{0}' already exists", string.Join(", ", keys)));

			if (parser == null)
			{
				parser = new CommandLineParser(keys[0], description);
				// TODO dangerous to add options after creation
			}
			parser.CommandArguments = commandArguments;
			parser.MyMessenger = MyMessenger; // TODO should update all commands when setting MyMessenger

			// make sure command options not in options
			foreach (IOption option in parser.Options)
			{
				ThrowIfOptionExisting(option);
			}


			cmd = new Command(keys, description, run, parser);
			_commands.Add(cmd);

			return cmd;
		}

		public void AddCommandHelp()
		{
			AddCommand(new string[] { "help" }, "Show help for specific command.  Try '/help' for general help.", "<command>", RunHelpCommand);
		}

		private Boolean RunHelpCommand(string command, Queue<string> args)
		{
			if (args.Count > 0)
			{
				string key = args.Dequeue();
				Command cmd = FindCommand(key);
				if (cmd != null && cmd.Parser != null)
					ShowMessage(MessageLevel.Help, cmd.Parser.GetHelpMessage());
				else
					ShowMessage(MessageLevel.Help, String.Format("No help available for command '{0}'", key));
			}
			else
			{
				return false; // this will have RunCommands() show the help for for the help command
			}
			return true;
		}

		public Command FindCommand(IOption command)
		{
			foreach (Command cur in _commands)
			{
				if (cur.IsKeyMatch(command))
					return cur;
			}
			return null;
		}

		public Command FindCommand(String[] keys)
		{
			foreach (String key in keys)
			{
				Command match = FindCommand(key);
				if (match != null)
					return match;
			}
			return null;
		}

		public Command FindCommand(String key)
		{
			foreach (Command cur in _commands)
			{
				if (cur.IsKeyMatch(key))
					return cur;
			}
			return null;
		}

		#region IOption Methods

		public void AddOption(IOption option)
		{
			ThrowIfOptionExisting(option);
			_options.Add(option);
		}

		protected void ThrowIfOptionExisting(IOption option)
		{
			IOption existing = FindOption(option);

			// make sure options not in commands
			if (existing == null)
			{
				foreach (Command command in _commands)
				{
					if (command.Parser != null && (existing = FindOption(option)) != null)
					{
						break;
					}
				}
			}
			if (existing != null)
			{
				throw new ExceptionOption(String.Format("The option '{0}' exists as '{1}'", String.Join(", ", option.Keys), String.Join(", ", existing.Keys)));
			}
		}

		public IOption FindOption(IOption option)
		{
			foreach (IOption cur in _options)
			{
				if (cur.IsKeyMatch(option))
					return cur;
			}
			return null;
		}

		public IOption FindOption(String[] keys)
		{
			foreach (String key in keys)
			{
				IOption match = FindOption(key);
				if (match != null)
					return match;
			}
			return null;
		}

		public IOption FindOption(String key)
		{
			String[] keys = KeyPermutations(key);
			foreach (IOption cur in _options)
			{
				if (cur.IsKeyMatch(keys))
					return cur;
			}
			return null;
		}

		protected IOption FindOptionExact(String key)
		{
			foreach (IOption cur in _options)
			{
				if (cur.IsKeyMatch(key))
					return cur;
			}
			return null;
		}

		public void Reset()
		{
			foreach (IOption cur in _options)
			{
				cur.Reset();
			}
		}

		#endregion

		#region OptionValue Methods

		public OptionValue<T> AddOptionValue<T>(String key, String description = null, String prompt = DEFAULT_PROMPT, T defaultValue = default(T), Boolean isRequired = false)
		{
			return AddOptionValue<T>(new String [] { key }, description, prompt, defaultValue, isRequired);
		}

		public OptionValue<T> AddOptionValue<T>(String[] keys, String description = null, String prompt = DEFAULT_PROMPT, T defaultValue = default(T), Boolean isRequired = false)
		{
			OptionValue<T> option = new OptionValue<T>(keys, description, prompt, defaultValue, isRequired);
			AddOption(option);
			return option;
		}

		public OptionValue<T> FindOptionValue<T>(String[] keys)
		{
			try
			{
				return (OptionValue<T>)Convert.ChangeType(FindOption(keys), typeof(OptionValue<T>));
			}
			catch
			{
				return null;
			}
		}

		public OptionValue<T> FindOptionValue<T>(String key)
		{
			try
			{
				return (OptionValue<T>)Convert.ChangeType(FindOption(key), typeof(OptionValue<T>));
			}
			catch
			{
				return null;
			}
		}

		#endregion

		#region OptionBoolean Methods

		public OptionBoolean AddOptionBoolean(String key, String description = null, Boolean isRequired = false)
		{
			return AddOptionBoolean(new String[] { key }, description, isRequired);
		}

		public OptionBoolean AddOptionBoolean(String[] keys, String description = null, Boolean isRequired = false)
		{
			OptionBoolean option = new OptionBoolean(keys, description, isRequired);
			AddOption(option);
			return option;
		}

		public OptionBoolean AddOptionHelp(String[] keys = null, String description = DEFAULT_HELP_DESCRIPTION)
		{
			if (keys == null)
				keys = DEFAULT_HELP_KEYS;

			if (HelpOption != null)
				_options.Remove(HelpOption);

			HelpOption = AddOptionBoolean(keys, description, false);
			return HelpOption;
		}

		public OptionBoolean FindOptionBoolean(String[] keys)
		{
			try
			{
				return (OptionBoolean)Convert.ChangeType(FindOption(keys), typeof(OptionBoolean));
			}
			catch
			{
				return null;
			}
		}

		public OptionBoolean FindOptionBoolean(String key)
		{
			try
			{
				return (OptionBoolean)Convert.ChangeType(FindOption(key), typeof(OptionBoolean));
			}
			catch
			{
				return null;
			}
		}

		#endregion

		#region OptionList Methods

		public OptionList<T> AddOptionList<T>(String key, String description = null, String prompt = DEFAULT_PROMPT, List<T> defaultValue = default(List<T>), Boolean isRequired = false)
		{
			return AddOptionList<T>(new String [] { key }, description, prompt, defaultValue, isRequired);
		}

		public OptionList<T> AddOptionList<T>(String[] keys, String description = null, String prompt = DEFAULT_PROMPT, List<T> defaultValue = default(List<T>), Boolean isRequired = false)
		{
			OptionList<T> option = new OptionList<T>(keys, description, prompt, defaultValue, isRequired);
			AddOption(option);
			return option;
		}

		public OptionList<T> FindOptionList<T>(String[] keys)
		{
			try
			{
				return (OptionList<T>)Convert.ChangeType(FindOption(keys), typeof(OptionList<T>));
			}
			catch
			{
				return null;
			}
		}

		public OptionList<T> FindOptionList<T>(String key)
		{
			try
			{
				return (OptionList<T>)Convert.ChangeType(FindOption(key), typeof(OptionList<T>));
			}
			catch
			{
				return null;
			}
		}

		#endregion

		#region OptionCount Methods

		public OptionCount AddOptionCount(String key, String description = null, Boolean isRequired = false)
		{
			return AddOptionCount(new String[] { key }, description, isRequired);
		}

		public OptionCount AddOptionCount(String[] keys, String description = null, Boolean isRequired = false)
		{
			OptionCount option = new OptionCount(keys, description, isRequired);
			AddOption(option);
			return option;
		}

		public OptionCount FindOptionCount(String[] keys)
		{
			try
			{
				return (OptionCount)Convert.ChangeType(FindOption(keys), typeof(OptionCount));
			}
			catch
			{
				return null;
			}
		}

		public OptionCount FindOptionCount(String key)
		{
			try
			{
				return (OptionCount)Convert.ChangeType(FindOption(key), typeof(OptionCount));
			}
			catch
			{
				return null;
			}
		}

		#endregion

		#region Parse Methods

		public void Parse()
		{
			Parse(Environment.GetCommandLineArgs());
		}

		public void Parse(String[] args)
		{
			Queue<string> queue = new Queue<string>(args);
			RemainingArgs = new List<String>();

			// Loop through argments
			while (queue.Count > 0)
			{
				string current = queue.Dequeue();
				IOption option = FindOptionExact(current);  // want exact option
				if (option != null)
					option.Parse(current, queue);
				else
					RemainingArgs.Add(current);
			}

			// make sure all the required options have been read
			List<String> missingArgs = new List<String>();
			foreach (IOption option in _options)
			{
				if (option.IsRequired && !option.IsMatched)
				{
					missingArgs.Add(option.Keys[0]);
				}
			}
			if (missingArgs.Count > 0)
				throw new ExceptionParse(String.Format("The following required options were not set '{0}'", String.Join(", ", missingArgs)));

		}

		public bool RunCommands()
		{
			Queue<string> queue = new Queue<string>(RemainingArgs);
			RemainingArgs = new List<String>();

			bool isRunCommand = false;

			// Loop through argments
			while (queue.Count > 0)
			{
				string current = queue.Dequeue();
				Command command = FindCommand(current);
				if (command != null)
				{
					isRunCommand = true;

					// if command has a parser, have it parse what remains in the queue
					if (command.Parser != null)
					{
						command.Parser.Parse(queue.ToArray());
						queue = new Queue<string>(command.Parser.RemainingArgs);
					}

					// try running the command
					if (!command.Run(current, queue))
					{
						if (command.Parser != null)
							ShowMessage(MessageLevel.Help, command.Parser.GetHelpMessage());
						else
							ShowMessage(MessageLevel.Help, String.Format("Failed to run command {0} with arguments '{1}'", current, string.Join(" ", queue)));
					}
				}
				else
					RemainingArgs.Add(current);
			}

			return isRunCommand;
		}

		#endregion

		#region HelpMessage

		protected String WrapString(string str, int maxLength = -1, int firstIndent = 0, int secondIndent = -1)
		{
			// Return empty list of strings if the text was empty
			if (str.Length == 0)
				return "";

			if (maxLength < 0)
			{
				try
				{
					maxLength = Console.BufferWidth; // Exception running outside terminal
				}
				catch(Exception)
				{
					maxLength = 80;
				}
			}
			if (secondIndent < 0)
				secondIndent = firstIndent;

			string firstIndentText = firstIndent < 0 ? "" : new String(' ', firstIndent);
			string secondIndentText = secondIndent < 1 ? "" : new String(' ', secondIndent);
			List<string> lines = new List<string>();

			StringReader strReader = new StringReader(str);
			String text;
			while ((text = strReader.ReadLine()) != null)
			{

				string[] words = text.Split(' ');
				StringBuilder currentLine = new StringBuilder(firstIndentText);

				foreach (var currentWord in words)
				{

					if ((currentLine.Length > maxLength) ||
					    ((currentLine.Length + currentWord.Length) > maxLength))
					{
						lines.Add(currentLine.ToString());
						currentLine = new StringBuilder(secondIndentText);
					}

					if (currentLine.Length > 0)
						currentLine.AppendFormat(" {0}", currentWord);
					else
						currentLine.Append(currentWord);
				}
		
				if (currentLine.Length > 0)
					lines.Add(currentLine.ToString());
			}

			return string.Join(Environment.NewLine, lines);
		}

		public String GetUsageMessage()
		{
			StringBuilder line;
			line = new StringBuilder(Name);
			if (_options.Count > 0)
			{
				foreach (IOption option in _options)
				{
					string key = option.Keys[0];
					line.Append(" ");
					if (!option.IsRequired)
						line.Append("[");
					line.Append(key);
					if (option.IsNeedsValue)
						line.AppendFormat(" {0}", (option.Prompt != null && option.Prompt.Length > 0 ? option.Prompt : DEFAULT_PROMPT));
					if (!option.IsRequired)
						line.Append("]");
				}
			}
			if (_commands.Count > 0)
			{
				line.Append(" <command> [<args>]");
			}
			else if (CommandArguments != null && CommandArguments.Length > 0)
				line.AppendFormat(" {0}", CommandArguments);

			return line.ToString();
		}

		public String GetHelpMessage()
		{
			StringBuilder help = new StringBuilder();

			// show usage
			help.Append("USAGE: ");
			help.AppendLine(WrapString(GetUsageMessage()));

			// show description
			if (Description != null && Description.Length > 0)
			{
				help.AppendLine(); 
				help.AppendLine("DESCRIPTION");
				help.AppendLine();
				help.AppendLine(WrapString(Description));
			}

			// show options
			if (_options.Count > 0)
			{
				help.AppendLine(); 
				help.AppendLine("OPTIONS");
				foreach (IOption option in _options)
				{
					help.AppendLine();
					string[] keys = option.Keys;
					if (option.IsNeedsValue)
						keys[0] = String.Format("{0} {1}", keys[0], (option.Prompt != null && option.Prompt.Length > 0 ? option.Prompt : DEFAULT_PROMPT));
					help.AppendLine(WrapString(String.Join(", ", keys), -1, 2));
					help.AppendLine(WrapString(option.Description, -1, 4));
				}
			}

			// show commands
			if (_commands.Count > 0)
			{
				help.AppendLine(); 
				help.AppendLine("COMMANDS");
				foreach (Command command in _commands)
				{
					help.AppendLine();
					if (command.Parser != null)
					{
						help.AppendLine(WrapString(command.Parser.GetUsageMessage(), -1, 2));
						if (command.Keys.Length > 1)
							help.AppendLine(WrapString(String.Join(", ", command.Keys), -1, 2));
					}
					else
						help.AppendLine(WrapString(String.Join(", ", command.Keys), -1, 2));
					help.AppendLine(WrapString(command.Description, -1, 4));
				}
			}

			return help.ToString();
		}

		#endregion

		#region Static Methods

		protected static Boolean IsKey(String key)
		{
			return ((key.StartsWith("-") || key.StartsWith("/")));
		}

		protected static String[] KeyPermutations(String key)
		{
			if (IsKey(key))
				return new String[] { key };
			else
				return new String[] { "/" + key, "-" + key };
		}

		#endregion

		#region Command

		public class Command
		{
			public RunCommand Run { get; protected set; }

			public String Description { get; set; }

			public String [] Keys { get { return _keys.ToArray(); } }

			protected List<String> _keys = new List<String>();

			public CommandLineParser Parser { get; protected set; }

			public Command(String[] keys, string description, RunCommand run, CommandLineParser parser)
			{
				Run = run;
				Parser = parser;
				Description = description;
				SetKeys(keys);
			}

			protected void SetKeys(String[] keys)
			{
				foreach (String cur in keys)
				{
					if (Regex.IsMatch(cur, "\\s"))
						throw new ExceptionOption(String.Format("Key '{0}' contains spaces.", cur));
					if (!IsKeyMatch(cur))
						_keys.Add(cur);
				}
				if (_keys.Count < 1)
					throw new ExceptionOption(String.Format("No keys set for '{0}'", Description));
			}

			public Boolean IsKeyMatch(String key)
			{
				foreach (String cur in _keys)
				{
					if (cur.ToLower() == key.ToLower())
						return true;
				}
				return false;
			}

			public Boolean IsKeyMatch(String[] keys)
			{
				foreach (String cur in keys)
				{
					if (IsKeyMatch(cur))
						return true;
				}
				return false;
			}

			public Boolean IsKeyMatch(IOption option)
			{
				return IsKeyMatch(option.Keys);
			}
		}

		#endregion

		#region IOption Interface

		public interface IOption
		{
			String [] Keys { get; }

			String Description { get; set; }

			String Prompt { get; set; }

			Boolean IsRequired { get; set; }

			Boolean IsMatched { get; }

			Boolean IsNeedsValue { get; }

			T GetValue<T>();

			Boolean IsKeyMatch(String key);

			Boolean IsKeyMatch(String[] keys);

			Boolean IsKeyMatch(IOption option);

			Boolean Parse(string key, Queue<string> args);

			void Reset();
		}

		#endregion

		#region OptionValue<T> Class

		public class OptionValue<T> : IOption
		{
			public OptionValue(String[] keys, String description = null, String prompt = DEFAULT_PROMPT, T defaultValue = default(T), Boolean isRequired = false)
			{
				DefaultValue = defaultValue;
				Description = description;
				Prompt = prompt;
				IsRequired = isRequired;
				SetKeys(keys);
				Reset();
			}

			public T Value { get; set; }

			public T DefaultValue { get; set; }

			protected List<String> _keys = new List<String>();

			protected void SetKeys(String[] keys)
			{
				foreach (String cur in keys)
				{
					foreach (String key in CommandLineParser.KeyPermutations(cur))
					{
						if (Regex.IsMatch(cur, "\\s"))
							throw new ExceptionOption(String.Format("Key '{0}' contains spaces.", cur));
						if (!IsKeyMatch(cur))
							_keys.Add(key);
					}
				}
				if (_keys.Count < 1)
					throw new ExceptionOption(String.Format("No keys set for '{0}'", Description));
			}

			#region IOption

			public String [] Keys { get { return _keys.ToArray(); } }

			public String Description { get; set; }

			public String Prompt { get; set; }

			public Boolean IsMatched { get; protected set; }

			public Boolean IsRequired { get; set; }

			virtual public Boolean IsNeedsValue { get { return true; } }

			public Y GetValue<Y>()
			{
				return (Y)Convert.ChangeType(Value, typeof(Y));
			}

			public Boolean IsKeyMatch(String key)
			{
				foreach (String cur in _keys)
				{
					if (cur.ToLower() == key.ToLower())
						return true;
				}
				return false;
			}

			public Boolean IsKeyMatch(String[] keys)
			{
				foreach (String cur in keys)
				{
					if (IsKeyMatch(cur))
						return true;
				}
				return false;
			}

			public Boolean IsKeyMatch(IOption option)
			{
				return IsKeyMatch(option.Keys);
			}

			virtual public Boolean Parse(string key, Queue<string> args)
			{
				if (args == null || args.Count < 1 || !IsKeyMatch(key))
					return false;

				if (IsMatched)
					throw new ExceptionParse(String.Format("Option '{0}' already set.", String.Join(", ", _keys)));

				IsMatched = true;

				Value = (T)Convert.ChangeType(args.Dequeue(), typeof(T));

				return true;
			}

			virtual public void Reset()
			{
				IsMatched = false;
				Value = DefaultValue;
			}

			#endregion

		}

		#endregion

		#region OptionBoolean Class

		public class OptionBoolean : OptionValue<Boolean>
		{
			public OptionBoolean(String[] keys, String description = null, Boolean isRequired = false)
				:base(keys, description, null, false, isRequired)
			{
			}

			#region IOption

			override public Boolean IsNeedsValue { get { return false; } }

			override public Boolean Parse(string key, Queue<string> args)
			{
				if (!IsKeyMatch(key))
					return false;

				if (IsMatched)
					throw new ExceptionParse(String.Format("Option '{0}' already set.", String.Join(", ", _keys)));

				IsMatched = true;
				Value = true;

				return true;
			}

			#endregion

		}

		#endregion

		#region OptionBoolean Class

		public class OptionCount : OptionValue<int>
		{
			public OptionCount(String[] keys, String description = null, Boolean isRequired = false)
				:base(keys, description, null, 0, isRequired)
			{
			}

			#region IOption

			override public Boolean IsNeedsValue { get { return false; } }

			override public Boolean Parse(string key, Queue<string> args)
			{
				if (!IsKeyMatch(key))
					return false;

				IsMatched = true;
				Value++;

				return true;
			}

			#endregion

		}

		#endregion

		#region OptionList<T> Class

		public class OptionList<T> : OptionValue<List<T>>
		{
			public OptionList(String[] keys, String description = null, String prompt = DEFAULT_PROMPT, List<T> defaultValue = default(List<T>), Boolean isRequired = false, String separatorPattern = @"\s*,\s*")
				:base(keys, description, prompt, (defaultValue != null ? defaultValue : new List<T>()), isRequired)
			{
				SeparatorPattern = separatorPattern;
			}

			public String SeparatorPattern { get; set; }

			#region IOption

			override public Boolean Parse(string key, Queue<string> args)
			{
				if (args == null || args.Count < 1 || !IsKeyMatch(key))
					return false;

				if (!IsMatched)
					Value.Clear();

				IsMatched = true;

				foreach (String tok in Regex.Split(args.Dequeue(), SeparatorPattern))
				{
					Value.Add((T)Convert.ChangeType(tok, typeof(T)));
				}

				return true;
			}

			#endregion

		}

		#endregion

		#region Exceptions

		public class ExceptionCommandLineParser : Exception
		{
			public ExceptionCommandLineParser(String message)
				: base(message)
			{
			}
		}

		public class ExceptionOption : ExceptionCommandLineParser
		{
			public ExceptionOption(String message)
				: base(message)
			{
			}
		}

		public class ExceptionCommand : ExceptionCommandLineParser
		{
			public ExceptionCommand(String message)
				: base(message)
			{
			}
		}

		public class ExceptionParse : ExceptionCommandLineParser
		{
			public ExceptionParse(String message)
				: base(message)
			{
			}
		}

		#endregion

	}
}

