using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace skdm
{
	public class CommandLineParser
	{
		public const String DEFAULT_PROMPT = "[...]";
		public static readonly String[] DEFAULT_HELP_KEYS = new string[] {"help","h", "?"};
		public const String DEFAULT_HELP_DESCRIPTION = "Show help message";

		private List<IOption> _options = new List<IOption>();

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
			RemainingArgs = new List<String>();

			// Loop through argments
			int i = 0;
			while (args != null && i < args.Length)
			{
				// Give each option a try at the current argument until find match
				Boolean isFound = false;
				foreach (IOption option in _options)
				{
					int increment = option.Parse(args, i);
					if (increment > 0)
					{
						i += increment;
						isFound = true;
						break;
					}
				}

				// if none of the options parsed current, bump up by one
				if (!isFound)
				{
					RemainingArgs.Add(args[i]);
					i++;
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

		#region IOption Interface

		public interface IOption
		{
			String [] Keys { get; }

			String Description { get; set; }

			String Prompt { get; set; }

			Boolean IsRequired { get; set; }

			Boolean IsMatched { get; }

			T GetValue<T>();

			Boolean IsKeyMatch(String key);

			Boolean IsKeyMatch(String[] keys);

			Boolean IsKeyMatch(IOption option);

			int Parse(String[] tokens, int index);

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

			virtual public int Parse(String[] tokens, int index)
			{
				int keyIdx = index;
				int valueIdx = index + 1;

				// if enough tokens for key & value and the first is one of our keys
				if (tokens == null || tokens.Length <= valueIdx || !IsKeyMatch(tokens[keyIdx]))
					return 0;

				if (IsMatched)
					throw new ExceptionParse(String.Format("Option '{0}' already set.", String.Join(", ", _keys)));

				IsMatched = true;

				Value = (T)Convert.ChangeType(tokens[valueIdx], typeof(T));

				return 2;
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

			override public int Parse(String[] tokens, int index)
			{
				// if enough tokens for key & value and the first is one of our keys
				if (tokens == null || tokens.Length <= index || !IsKeyMatch(tokens[index]))
					return 0;

				if (IsMatched)
					throw new ExceptionParse(String.Format("Option '{0}' already set.", String.Join(", ", _keys)));

				IsMatched = true;
				Value = true;

				return 1;
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

			override public int Parse(String[] tokens, int index)
			{
				// if enough tokens for key & value and the first is one of our keys
				if (tokens == null || tokens.Length <= index || !IsKeyMatch(tokens[index]))
					return 0;

				IsMatched = true;
				Value++;

				return 1;
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

			override public int Parse(String[] tokens, int index)
			{
				int keyIdx = index;
				int valueIdx = index + 1;

				// if enough tokens for key & value and the first is one of our keys
				if (tokens == null || tokens.Length <= valueIdx || !IsKeyMatch(tokens[keyIdx]))
					return 0;

				if (!IsMatched)
					Value.Clear();

				IsMatched = true;

				foreach (String tok in Regex.Split(tokens[valueIdx], SeparatorPattern))
				{
					Value.Add((T)Convert.ChangeType(tok, typeof(T)));
				}

				return 2;
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

