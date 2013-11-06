using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace skdm
{
	public class CommandLineParser
	{
		public const String DEFAULT_PROMPT = "[...]";
		public static readonly String[] DEFAULT_HELP_KEYS = new string[] { "help", "h", "?" };
		public const String DEFAULT_HELP_DESCRIPTION = "Show help message";

		public delegate void ParseCommand(Stack<string> args);
		public delegate void RunCommand(Stack<string> args);

		private List<IOption> _options = new List<IOption>();
		private List<Command> _commands = new List<Command>();

		#region Public Members

		public  OptionBoolean HelpOption { get; protected set; }

		public List<String> RemainingArgs { get; protected set; }

		public String HelpPrefix { get; set; }

		public IOption[] Options { get { return _options.ToArray(); } }

		#endregion

		#region Constructor

		public CommandLineParser(string helpPrefix = null, bool isAddHelp = true)
		{
			HelpPrefix = helpPrefix;
			if (isAddHelp)
				AddOptionHelp();
		}

		#endregion

		public void AddCommand(string name, ParseCommand parse, string description, string help)
		{
			foreach (Command cmd in _commands)
			{
				if (cmd.Name.ToLower() == name.ToLower())
					throw new ExceptionCommand(String.Format("The command '{0}' already exists",name));
			}

			Command newCmd = new Command();
			newCmd.Description = description;
			newCmd.Name = name;
			newCmd.Parse = parse;
			newCmd.Help = help;
		
			_commands.Add(newCmd);
		}

		#region IOption Methods

		public void AddOption(IOption option)
		{
			IOption existing = FindOption(option);
			if (existing != null)
			{
				throw new ExceptionOption(String.Format("The option '{0}' exists as '{1}'", String.Join(", ", option.Keys), String.Join(", ", existing.Keys)));
			}
			_options.Add(option);
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
				// give each command a try
				foreach (Command cmd in _commands)
				{
					// TODO handle command
				}

				// Give each option a try at the current argument until find match
				Boolean isFound = false;
				foreach (IOption option in _options)
				{
					if (option.Parse(queue))
					{
						isFound = true;
						break;
					}
				}

				// if none of the options parsed current, bump up by one
				if (!isFound)
				{
					RemainingArgs.Add(queue.Dequeue());
				}
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

		#endregion

		#region HelpMessage

		public String GetHelpMessage()
		{
			// TODO need nicer formatting for alternate commands
			// TODO need description text wrapping 

			const string indent = "  ";
			int ind = indent.Length;
			const int spc = 3;

			// help header
			string help = HelpPrefix != null ? HelpPrefix : "";
			help += "\nCommand line options are:\n\n";

			// get the max length of options
			int len = 0;
			foreach (IOption option in _options)
			{
				foreach (string key in option.Keys)
				{
					int nlen = key.Length;
					if (option.IsNeedsValue)
						nlen += option.Prompt.Length + 1;
					len = Math.Max(len, nlen);
				}
			}

			// add each option to help
			bool req = false;
			foreach (IOption option in _options)
			{
				string[] keys = option.Keys;

				for (int i = 0; i < keys.Length; i++)
				{
					string line = indent + keys[i];
					if (option.IsNeedsValue)
						line += " " + option.Prompt;
					if (i == 0)
					{
						while (line.Length < len + spc + ind)
							line += " ";
						if (option.IsRequired)
						{
							line += "(*) ";
							req = true;
						}
						line += option.Description;
					}

					help += line + "\n";
				}

				help += "\n";
			}
			if (req)
				help += "(*) Required.\n";

			return help;
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
		private class Command
		{
			public ParseCommand Parse { get; set; }
			public string Name { get; set; }
			public string Description { get; set; }
			public string Help { get; set; }
			public Queue<string> args;
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

			Boolean Parse(Queue<string> args);

			void Reset();
		}

		#endregion

		#region OptionValue<T> Class

		public class OptionValue<T> : IOption
		{
			public OptionValue(String[] keys, String description = null, String prompt = DEFAULT_PROMPT, T defaultValue = default(T), Boolean isRequired = false)
			{
				SetKeys(keys);
				DefaultValue = defaultValue;
				Description = description;
				Prompt = prompt;
				IsRequired = isRequired;
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

			virtual public Boolean Parse(Queue<string> args)
			{
				if (args == null || args.Count < 2 || !IsKeyMatch(args.Peek()))
					return false;

				// remove the key
				args.Dequeue();

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

			override public Boolean Parse(Queue<string> args)
			{
				if (args == null || args.Count < 1 || !IsKeyMatch(args.Peek()))
					return false;

				// remove the key
				args.Dequeue();

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

			override public Boolean Parse(Queue<string> args)
			{
				if (args == null || args.Count < 1 || !IsKeyMatch(args.Peek()))
					return false;

				// remove the key
				args.Dequeue();

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

			override public Boolean Parse(Queue<string> args)
			{
				if (args == null || args.Count < 2 || !IsKeyMatch(args.Peek()))
					return false;

				// remove the key
				args.Dequeue();

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

