using System;
using NUnit.Framework;
using skdm;

namespace test
{
	[TestFixture()]
	public class CommandLineParserTest
	{
		[Test()]
		public void AddOptionValue()
		{
			CommandLineParser cmd = new CommandLineParser();
			cmd.AddOptionValue<String>("test");

			CommandLineParser.IOption[] options = cmd.Options;
			Assert.AreEqual(1, options.Length);
		}

		[Test()]
		public void AddOptionValueTwoOk()
		{
			CommandLineParser cmd = new CommandLineParser();
			cmd.AddOptionValue<String>("test");
			cmd.AddOptionValue<String>("foo");

			CommandLineParser.IOption[] options = cmd.Options;
			Assert.AreEqual(2, options.Length);
		}
		
		[Test()]
		[ExpectedException(typeof(CommandLineParser.OptionException))]
		public void AddOptionValueSameException()
		{
			CommandLineParser cmd = new CommandLineParser();
			cmd.AddOptionValue<String>(new String[] {"foo", "test"});
			cmd.AddOptionValue<int>("test");
			Assert.IsTrue(false, "never get here");
		}

		[Test()]
		public void AddOptionTwoFindPermutations()
		{
			CommandLineParser cmd = new CommandLineParser();
			cmd.AddOptionValue<String>(new String[]{"test", "/t"}, "TEST DESCRIPTION");
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");
			Assert.AreEqual("TEST DESCRIPTION", cmd.FindOption("test").Description);
			Assert.AreEqual("TEST DESCRIPTION", cmd.FindOption("/test").Description);
			Assert.AreEqual("TEST DESCRIPTION", cmd.FindOption("-test").Description);
			Assert.AreEqual("TEST DESCRIPTION", cmd.FindOption("/t").Description);
			Assert.AreEqual("OTHER DESCRIPTION", cmd.FindOption("foo").Description);
			Assert.AreEqual("OTHER DESCRIPTION", cmd.FindOption("/foo").Description);
			Assert.AreEqual("OTHER DESCRIPTION", cmd.FindOption("-foo").Description);
			Assert.IsNull(cmd.FindOption("bar"));
			Assert.IsNull(cmd.FindOption("/f"));
			Assert.IsNull(cmd.FindOption("-t"));
		}
		
		[Test()]
		public void AddOptionValueFindType()
		{
			CommandLineParser cmd = new CommandLineParser();
			cmd.AddOptionValue<String>("test", "TEST DESCRIPTION");
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");

			Assert.AreEqual("TEST DESCRIPTION", cmd.FindOptionValue<String>("test").Description);
			Assert.AreEqual("TEST DESCRIPTION", cmd.FindOptionValue<String>("/test").Description);
			Assert.AreEqual("TEST DESCRIPTION", cmd.FindOptionValue<String>("-test").Description);
			Assert.IsNull(cmd.FindOptionValue<String>("foo"));
			Assert.IsNull(cmd.FindOptionValue<String>("/foo"));
			Assert.IsNull(cmd.FindOptionValue<String>("-foo"));

			Assert.AreEqual("OTHER DESCRIPTION", cmd.FindOptionValue<int>("foo").Description);
			Assert.AreEqual("OTHER DESCRIPTION", cmd.FindOptionValue<int>("/foo").Description);
			Assert.AreEqual("OTHER DESCRIPTION", cmd.FindOptionValue<int>("-foo").Description);
			Assert.IsNull(cmd.FindOptionValue<int>("test"));
			Assert.IsNull(cmd.FindOptionValue<int>("/test"));
			Assert.IsNull(cmd.FindOptionValue<int>("-test"));
		}
		
		[Test()]
		public void ParseOptionValueTwo()
		{
			CommandLineParser cmd = new CommandLineParser();
			CommandLineParser.OptionValue<String> strOption = cmd.AddOptionValue<String>("test", "TEST DESCRIPTION");
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");

			Assert.AreEqual(0, cmd.FindOption("foo").GetValue<int>());
			Assert.IsNull(strOption.Value);

			cmd.Parse(new String[] { "-foo", "13", "/test", "some value" });
			Assert.AreEqual(13, cmd.FindOption("foo").GetValue<int>());
			Assert.AreEqual("some value", strOption.Value);
		}
		
		[Test()]
		public void ParseOptionValueMissingOne()
		{
			CommandLineParser cmd = new CommandLineParser();
			cmd.AddOptionValue<String>("test", "TEST DESCRIPTION", CommandLineParser.DEFAULT_PROMPT, "test value");
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");

			Assert.AreEqual("test value", cmd.FindOption("test").GetValue<string>());
			Assert.AreEqual(0, cmd.FindOption("foo").GetValue<int>());

			cmd.Parse(new String[] { "-foo", "13" });
			Assert.AreEqual(13, cmd.FindOption("foo").GetValue<int>());
			Assert.AreEqual("test value", cmd.FindOption("test").GetValue<string>());
		}

		[Test()]
		[ExpectedException(typeof(CommandLineParser.ParseException))]
		public void ParseOptionValueMissingRequired()
		{
			CommandLineParser cmd = new CommandLineParser();
			cmd.AddOptionValue<String>("test", "TEST DESCRIPTION", CommandLineParser.DEFAULT_PROMPT, "test value", true);
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");

			Assert.AreEqual("test value", cmd.FindOption("test").GetValue<string>());
			Assert.AreEqual(0, cmd.FindOption("foo").GetValue<int>());

			cmd.Parse(new String[] { "-foo", "13" });
			Assert.IsTrue(false, "never get here");
		}
		
		[Test()]
		public void ParseOptionValueTwoHasRemaining()
		{
			CommandLineParser cmd = new CommandLineParser();
			CommandLineParser.OptionValue<String> strOption = cmd.AddOptionValue<String>("test", "TEST DESCRIPTION");
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");

			Assert.AreEqual(0, cmd.FindOption("foo").GetValue<int>());
			Assert.IsNull(strOption.Value);

			cmd.Parse(new String[] { "/before", "value1", "-foo", "13", "/test", "some value", "some stuff after" });
			Assert.AreEqual(13, cmd.FindOption("foo").GetValue<int>());
			Assert.AreEqual("some value", strOption.Value);
			CollectionAssert.AreEqual(new String[] { "/before", "value1", "some stuff after" }, cmd.RemainingArgs);
		}
	}
}

