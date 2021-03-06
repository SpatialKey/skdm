using System;
using System.Collections.Generic;
using NUnit.Framework;
using SpatialKey.DataManager.App;

namespace test
{
	[TestFixture()]
	public class CommandLineParserTest
	{
		[Test()]
		public void AddOptionValue()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionValue<String>("test");

			CommandLineParser.IOption[] options = cmd.Options;
			Assert.AreEqual(1, options.Length);
		}

		[Test()]
		public void AddOptionValueTwoOk()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionValue<String>("test");
			cmd.AddOptionValue<String>("foo");

			CommandLineParser.IOption[] options = cmd.Options;
			Assert.AreEqual(2, options.Length);
		}

		[Test()]
		public void AddOptionValueSameException()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionValue<String>(new String[] { "foo", "test" });
            Assert.Throws<CommandLineParser.ExceptionOption>(() => cmd.AddOptionValue<int>("test"));
			Assert.IsTrue(false, "never get here");
		}

		[Test()]
		public void AddOptionTwoFindPermutations()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionValue<String>(new String[] { "test", "/t" }, "TEST DESCRIPTION");
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
			CommandLineParser cmd = new CommandLineParser("test");
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
			CommandLineParser cmd = new CommandLineParser("test");
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
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionValue<String>("test", "TEST DESCRIPTION", CommandLineParser.DEFAULT_PROMPT, "test value");
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");

			Assert.AreEqual("test value", cmd.FindOption("test").GetValue<string>());
			Assert.AreEqual(0, cmd.FindOption("foo").GetValue<int>());

			cmd.Parse(new String[] { "-foo", "13" });
			Assert.AreEqual(13, cmd.FindOption("foo").GetValue<int>());
			Assert.AreEqual("test value", cmd.FindOption("test").GetValue<string>());

			Assert.IsTrue(cmd.FindOption("foo").IsMatched);
			Assert.IsFalse(cmd.FindOption("test").IsMatched);
		}

		[Test()]
		public void ParseOptionValueMissingRequired()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionValue<String>("test", "TEST DESCRIPTION", CommandLineParser.DEFAULT_PROMPT, "test value", true);
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");

			Assert.AreEqual("test value", cmd.FindOption("test").GetValue<string>());
			Assert.AreEqual(0, cmd.FindOption("foo").GetValue<int>());

			Assert.Throws<CommandLineParser.ExceptionParse>(() => cmd.Parse(new String[] { "-foo", "13" }));
			Assert.IsTrue(false, "never get here");
		}

		[Test()]
		public void ParseOptionValueTwoHasRemaining()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			CommandLineParser.OptionValue<String> strOption = cmd.AddOptionValue<String>("test", "TEST DESCRIPTION");
			cmd.AddOptionValue<int>("foo", "OTHER DESCRIPTION");

			Assert.AreEqual(0, cmd.FindOption("foo").GetValue<int>());
			Assert.IsNull(strOption.Value);

			cmd.Parse(new String[] { "/before", "value1", "-foo", "13", "/test", "some value", "some stuff after" });
			Assert.AreEqual(13, cmd.FindOption("foo").GetValue<int>());
			Assert.AreEqual("some value", strOption.Value);
			CollectionAssert.AreEqual(new String[] { "/before", "value1", "some stuff after" }, cmd.RemainingArgs);
		}

		[Test()]
		public void ExceptionOverwritingHelp()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionHelp();
			Assert.Throws<CommandLineParser.ExceptionOption>(() => cmd.AddOptionValue<String>(new String[] { "?", "test" }));
			Assert.IsTrue(false, "never get here");
		}

		[Test()]
		public void AddHelpTwice()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionHelp();
			cmd.AddOptionHelp();

			CommandLineParser.IOption[] options = cmd.Options;
			Assert.AreEqual(1, options.Length);
			Assert.AreSame(cmd.HelpOption, cmd.FindOption("help"));
		}

		[Test()]
		public void ParseWithBooleanOption()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			CommandLineParser.OptionValue<String> strOption = cmd.AddOptionValue<String>("test", "TEST DESCRIPTION");
			cmd.AddOptionBoolean("b");

			Assert.IsNull(strOption.Value);
			Assert.AreEqual(false, cmd.FindOption("b").GetValue<Boolean>());

			cmd.Parse(new String[] { "-foo", "13", "-b", "/test", "some value" });
			Assert.AreEqual("some value", strOption.Value);
			Assert.AreEqual(true, cmd.FindOption("b").GetValue<Boolean>());
			Assert.IsTrue(cmd.FindOption("b").IsMatched);
			CollectionAssert.AreEqual(new String[] { "-foo", "13" }, cmd.RemainingArgs);
		}

		[Test()]
		public void ParseWithBooleanOptionMissed()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			CommandLineParser.OptionValue<String> strOption = cmd.AddOptionValue<String>("test", "TEST DESCRIPTION");
			cmd.AddOptionBoolean("b");

			Assert.IsNull(strOption.Value);
			Assert.AreEqual(false, cmd.FindOption("b").GetValue<Boolean>());

			cmd.Parse(new String[] { "-foo", "13", "/test", "some value" });
			Assert.AreEqual("some value", strOption.Value);
			Assert.AreEqual(false, cmd.FindOption("b").GetValue<Boolean>());
			Assert.IsFalse(cmd.FindOption("b").IsMatched);
			CollectionAssert.AreEqual(new String[] { "-foo", "13" }, cmd.RemainingArgs);
		}

		[Test()]
		public void ParseHelp()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionHelp();
			cmd.AddOptionValue<String>("test", "TEST DESCRIPTION");
			cmd.AddOptionBoolean("b");

			Assert.IsFalse(cmd.HelpOption.IsMatched);
			Assert.IsFalse(cmd.HelpOption.Value);

			cmd.Parse(new String[] { "-foo", "13", "/?", "/test", "some value" });
			Assert.IsTrue(cmd.HelpOption.IsMatched);
			Assert.IsTrue(cmd.HelpOption.Value);
			CollectionAssert.AreEqual(new String[] { "-foo", "13" }, cmd.RemainingArgs);
		}

		[Test()]
		public void ParseList()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionList<String>("list");
			CollectionAssert.IsEmpty(cmd.FindOptionList<String>("list").Value);
			cmd.Parse(new String[] { "-list", "foo", "/list", "some, value" });
			CollectionAssert.AreEqual(new String[] { "foo", "some", "value" }, cmd.FindOptionList<String>("list").Value);
		}

		[Test()]
		public void ParseListDefault()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionList<String>("list", "desc", "LIST", new List<String> { "default list" });
			CollectionAssert.AreEqual(new String[] { "default list" }, cmd.FindOptionList<String>("list").Value);
			cmd.Parse(new String[] { "-list", "foo", "/list", "some, value" });
			CollectionAssert.AreEqual(new String[] { "foo", "some", "value" }, cmd.FindOptionList<String>("list").Value);
		}

		[Test()]
		public void ParseCount()
		{
			CommandLineParser cmd = new CommandLineParser("test");
			cmd.AddOptionCount("b");
			Assert.AreEqual(0, cmd.FindOptionCount("b").Value);

			cmd.Parse(new String[] { "/b" });
			Assert.AreEqual(1, cmd.FindOptionCount("b").Value);

			cmd.Reset();
			cmd.Parse(new String[] { "/b", "-b", "/b" });
			Assert.AreEqual(3, cmd.FindOptionCount("b").Value);
		}
	}
}

