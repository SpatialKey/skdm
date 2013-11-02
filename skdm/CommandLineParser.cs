using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace skdm
{
	public class CommandLineParser
	{
		private const String DEFAULT_PROMPT = "[...]";

		private List<IOption> _options = new List<IOption>();
		//private OptionToggle _helpOption;

		#region Public Members

		public List<String> RemainingArgs {
			get;
			protected set;
		}

		public List<String> InvalidArgs {
			get;
			protected set;
		}

		#endregion

		#region Constructor

		public CommandLineParser()
		{
		}

		#endregion

		#region IOption Methods
		public void AddOption(IOption option)
		{
			IOption existing = FindOption(option);
			if (existing != null)
			{
				throw new OptionException(String.Format("The option '{0}' exists as '{1}'", String.Join(", ", option.Keys), String.Join(", ", existing.Keys)));
			}
			_options.Add(option);
		}

		public IOption FindOption(String key)
		{
			foreach (IOption cur in _options)
			{
				if (cur.IsKeyMatch(key))
					return cur;
			}
			return null;
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
		#endregion

		#region OptionValue Methods
		public OptionValue<T> AddOptionValue<T>(string key, String description, String prompt = DEFAULT_PROMPT, Boolean isRequired = false, T defaultValue = default(T))
		{
			return AddOptionValue<T>(new string [] { key }, description, prompt, isRequired, defaultValue);
		}

		public OptionValue<T> AddOptionValue<T>(String [] keys, String description, String prompt, Boolean isRequired = false, T defaultValue = default(T))
		{
			OptionValue<T> option = new OptionValue<T>(keys, description, prompt, isRequired, defaultValue);
			AddOption(option);
			return option;
		}

		public OptionValue<T> FindOptionValue<T>(String key)
		{
			IOption option = FindOption(key);
			if (typeof(OptionValue<T>).Equals(option))
				return (OptionValue<T>)option;
			else
				return null;
		}
		#endregion

		#region OptionList Methods
		/*
		public OptionList<T> AddOptionList<T>()
		{
			OptionList<T> option = new OptionList<T>();
			AddOption(option);
			return option;
		}

		public OptionList<T> FindOptionList<T>(String key)
		{
			IOption option = FindOption(key);
			if (typeof(OptionList<T>).Equals(option))
				return (OptionList<T>)option;
			else
				return null;
		}
		*/
		#endregion

		#region OptionToggle Methods	
		/*	
		public OptionToggle AddOptionToggle()
		{
			OptionToggle option = new OptionToggle();
			AddOption(option);
			return option;
		}

		public OptionToggle AddOptionHelp()
		{
			// TODO need help options
			_helpOption = new OptionToggle();
			AddOption(_helpOption);
			return _helpOption;
		}

		public OptionToggle FindOptionToggle(String key)
		{
			IOption option = FindOption(key);
			if (typeof(OptionToggle).Equals(option))
				return (OptionToggle)option;
			else
				return null;
		}
		*/
		#endregion

		#region Parse Methods
		public void Parse()
		{
			Parse(Environment.GetCommandLineArgs());
		}

		public void Parse(String[] args)
		{
			RemainingArgs = new List<String>();
			InvalidArgs = new List<String>();
		}
		#endregion

		#region Static Methods
		protected static Boolean IsKey(string key)
		{
			return ((key.StartsWith("-") || key.StartsWith("/")));
		}

		protected static string[] KeyPermutations(string key)
		{
			if (IsKey(key))
				return new string[] { key };
			else
				return new string[] { "/"+key, "-"+key };
		}
		#endregion

		#region IOption Interface

		public interface IOption
		{
			String [] Keys { get; }

			String Description { get; }

			String Prompt { get; }

			Boolean IsMatched { get; }

			Boolean IsRequired { get; }

			T GetValue<T>();

			Boolean IsKeyMatch(String key);
			Boolean IsKeyMatch(String[] keys);
			Boolean IsKeyMatch(IOption option);

			int Parse(string key, string value);
		}

		#endregion

		#region OptionValue<T> Class

		public class OptionValue<T> : IOption
		{
			public OptionValue(String [] keys, String description, String prompt = DEFAULT_PROMPT, Boolean isRequired = false, T defaultValue = default(T))
			{
				AddKeys(keys);
				Description = description;
				Prompt = prompt;
				IsMatched = false;
				IsRequired = isRequired;
				Value = defaultValue;
			}

			public T Value {
				get;
				protected set;
			}

			public void AddKeys(String [] keys)
			{
				foreach (String cur in keys)
				{
					foreach (String key in CommandLineParser.KeyPermutations(cur))
					{
						if (Regex.IsMatch(cur, "\\s"))
							throw new OptionException(String.Format("Key '{0}' contains spaces.",cur));
						if (!IsKeyMatch(cur))
							_keys.Add(key);
					}
				}
			}

			#region IOption

			private List<String> _keys = new List<String>();

			public String [] Keys {
				get { return _keys.ToArray(); }
			}

			public String Description { get; protected set; }

			public String Prompt { get; set; }

			public Boolean IsMatched {
				get;
				internal set;
			}

			public Boolean IsRequired {
				get;
				internal set;
			}

			public Y GetValue<Y>()
			{
				return (Y)Convert.ChangeType(Value, typeof(Y));
			}

			virtual public int Parse(string key, string value)
			{
				if (!IsKeyMatch(key))
					return 0;

				Value = (T)Convert.ChangeType(value, typeof(T));
				IsMatched = true;

				return 2;
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
			public Boolean IsKeyMatch(String [] keys)
			{
				foreach (String cur in _keys)
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

			#endregion


		}

		#endregion

		#region OptionToggle Class
		/*
		public class OptionToggle : OptionValue<Boolean>
		{
			public OptionToggle()
			{
				Value = false;
			}

			#region IOption

			override public int Parse(string key, string value)
			{
				if (!IsKeyMatch(key))
					return 0;

				Value = !Value;
				IsMatched = true;

				return 1;
			}

			#endregion

		}
		*/
		#endregion

		#region OptionList<T> Class
		/*
		public class OptionList<T> : OptionValue<List<T>>
		{

			#region IOption

			override public int Parse(string key, string value)
			{
				if (!IsKeyMatch(key))
					return 0;

				String[] split = value.Split(new char[] { ',' });
				foreach (String cur in split)
				{
					Value.Add((T)Convert.ChangeType(cur, typeof(T)));
				}
				IsMatched = true;

				return 2;
			}

			#endregion

		}
		*/
		#endregion

		public class CommandLineParserException : Exception
		{
			public CommandLineParserException(string message)
				: base(message)
			{
			}
		}

		public class OptionException : CommandLineParserException
		{
			public OptionException(string message)
				: base(message)
			{
			}
		}

	}
}

