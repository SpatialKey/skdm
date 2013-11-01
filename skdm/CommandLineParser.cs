using System;
using System.Collections.Generic;

namespace skdm
{
	public class CommandLineParser
	{
		private List<IOption> _options = new List<IOption>();
		private OptionSwitch _helpOption;

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

		#region Public Methods

		public CommandLineParser()
		{
		}

		public OptionValue<T> AddOptionValue<T>()
		{
			OptionValue<T> option = new OptionValue<T>();
			AddOption(option);
			return option;
		}

		public OptionList<T> AddOptionList<T>()
		{
			OptionList<T> option = new OptionList<T>();
			AddOption(option);
			return option;
		}

		public OptionSwitch AddOptionSwitch()
		{
			OptionSwitch option = new OptionSwitch();
			AddOption(option);
			return option;
		}

		public OptionSwitch AddOptionHelp()
		{
			// TODO need help options
			_helpOption = new OptionSwitch();
			AddOption(_helpOption);
			return _helpOption;
		}

		public void AddOption(IOption option)
		{
			IOption existing = FindOption(option);
			if (existing != null)
			{
				throw new OptionException(String.Format("The option '{0}' exists as '{1}'", String.Join(", ", option.Keys), String.Join(", ", existing.Keys)));
			}
			_options.Add(option);
		}

		public OptionValue<T> FindOptionValue<T>(String key)
		{
			IOption option = FindOption(key);
			if (typeof(OptionValue<T>).Equals(option))
				return (OptionValue<T>)option;
			else
				return null;
		}

		public OptionList<T> FindOptionList<T>(String key)
		{
			IOption option = FindOption(key);
			if (typeof(OptionList<T>).Equals(option))
				return (OptionList<T>)option;
			else
				return null;
		}

		public OptionSwitch FindOptionSwitch(String key)
		{
			IOption option = FindOption(key);
			if (typeof(OptionSwitch).Equals(option))
				return (OptionSwitch)option;
			else
				return null;
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

		#region IOption Interface

		public interface IOption
		{
			String [] Keys { get; }

			int Parse(string key, string value);

			Boolean IsKeyMatch(object o);

			Boolean IsMatched { get; }

			Boolean IsRequired { get; }

			Boolean IsValue { get; }

			T GetValue<T>();
		}

		#endregion

		#region OptionValue<T> Class

		public class OptionValue<T> : IOption
		{
			public T Value {
				get;
				protected set;
			}

			public OptionValue()
			{
				// TODO do this check
				//if (key.Contains(" "))
				//	throw new OptionException(String.Format("Key '{0}' contains spaces.",key));
			}

			#region IOption

			virtual public int Parse(string key, string value)
			{
				if (!IsKeyMatch(key))
					return 0;

				Value = (T)Convert.ChangeType(value, typeof(T));
				IsMatched = true;

				return 2;
			}

			public Boolean IsKeyMatch(object o)
			{
				if (o is IOption)
					return IsKeyMatch((IOption)o);
				else if (o is List<String>)
					return IsKeyMatch(((List<String>)o).ToArray());
				else if (o is String[])
					return IsKeyMatch((String[])o);
				else if (o is String)
					return IsKeyMatch((String)o);
				else
					return false;
			}

			public Boolean IsKeyMatch(IOption option)
			{
				return IsKeyMatch(option.Keys);
			}

			public Boolean IsKeyMatch(String [] keys)
			{
				foreach (String cur in keys)
				{
					if (IsKeyMatch(cur))
						return true;
				}
				return false;
			}

			public Boolean IsKeyMatch(String key)
			{
				foreach (String cur in Keys)
				{
					if (cur.ToLower().CompareTo(key.ToLower()) == 0)
						return true;
				}
				return false;
			}

			public Boolean IsMatched {
				get;
				internal set;
			}

			public Boolean IsRequired {
				get;
				internal set;
			}

			public Boolean IsValue {
				get;
				internal set;
			}

			public Y GetValue<Y>()
			{
				return (Y)Convert.ChangeType(Value, typeof(Y));
			}

			private List<String> _keys = new List<String>();

			public String [] Keys {
				get { return _keys.ToArray(); }
			}

			#endregion

		}

		#endregion

		#region OptionSwitch<T> Class

		public class OptionSwitch : OptionValue<Boolean>
		{
			public OptionSwitch()
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

		#endregion

		#region OptionList<T> Class

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

