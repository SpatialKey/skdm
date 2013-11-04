using System;
using NUnit.Framework;
using skdm;

namespace tests
{
	[TestFixture()]
	public class CommandLineOptionTest
	{
		[Test()]
		public void TestOptionValueKeyExact()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"/test"});
			String[] keys = option.Keys;
			CollectionAssert.AreEquivalent(new String[] {"/test"}, keys);
		}

		[Test()]
		public void TestOptionValueKeyExpand()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test"});
			String[] keys = option.Keys;
			CollectionAssert.AreEquivalent(new String[] {"-test", "/test"}, keys);
		}
		
		[Test()]
		public void TestOptionValueKeyExist()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test", "-t", "/test"});
			String[] keys = option.Keys;
			CollectionAssert.AreEquivalent(new String[] {"-test", "/test", "-t"}, keys);
		}

		[Test()]
		public void TestOptionValueDefault()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"/test"});
			Assert.IsNull(option.Value);
			Assert.IsNull(option.GetValue<String>());
			Assert.IsFalse(option.IsMatched);
		}
		
		[Test()]
		public void TestOptionValueSetDefault()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(
				new String[]{"/test"}, "some description", "PROMPT", "some default");
			StringAssert.AreEqualIgnoringCase("some default", option.Value);
			StringAssert.AreEqualIgnoringCase("some default", option.GetValue<String>());
			Assert.IsFalse(option.IsMatched);
		}

		[Test()]
		[ExpectedException(typeof(CommandLineParser.ExceptionOption))]
		public void TestOptionValueKeyWithBlank()
		{
			//Assert.Throws<CommandLineParser.OptionException>(
			//	() => new CommandLineParser.OptionValue<String>(new String[]{"bad key"}, "some description", "PROMPT", false, null));
			new CommandLineParser.OptionValue<String>(
				new String[]{"good", "bad key"}, "some description", "PROMPT", "some default");
			Assert.IsTrue(false, "never get here");
		}

		[Test()]
		public void TestOptionValueDefaultInt()
		{
			CommandLineParser.OptionValue<int> option = new CommandLineParser.OptionValue<int>(new String[]{"yarg"});
			Assert.AreEqual(0, option.Value);

			option = new CommandLineParser.OptionValue<int>(new String[]{"yarg"}, "some desc", "NUM", 44);
			Assert.AreEqual(44, option.Value);
		}

		[Test()]
		public void TestOptionKeyMatch()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test", "-t", "/test"});
			Assert.IsTrue(option.IsKeyMatch("/test"));
			Assert.IsTrue(option.IsKeyMatch("-t"));
			Assert.IsTrue(option.IsKeyMatch("/TEST"));
			Assert.IsTrue(option.IsKeyMatch("-T"));
			Assert.IsTrue(option.IsKeyMatch(new String[]{"/foo", "-t"}));
			Assert.IsTrue(option.IsKeyMatch(new String[]{"/foo", "-TeSt"}));
			Assert.IsFalse(option.IsKeyMatch("test"));
			Assert.IsFalse(option.IsKeyMatch("/foo"));
			Assert.IsFalse(option.IsKeyMatch(new String[]{"/foo", "-bar"}));

			CommandLineParser.OptionValue<int> option2 = new CommandLineParser.OptionValue<int>(new String[]{"-t", "bar"});
			Assert.IsTrue(option.IsKeyMatch(option2));

			CommandLineParser.OptionValue<int> option3 = new CommandLineParser.OptionValue<int>(new String[]{"foo"});
			Assert.IsFalse(option.IsKeyMatch(option3));
		}

		[Test()]
		public void TestOptionValueParseStringSuccess()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test"}, "desc", "VAL", "original value");
			Assert.AreEqual(2, option.Parse(new String[] { "/test", "some value" }, 0));
			Assert.AreEqual("some value", option.Value);
		}

		[Test()]
		public void TestOptionValueParseStringSuccessIndex()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test"}, "desc", "VAL", "original value");
			Assert.AreEqual(2, option.Parse(new String[] { "/foo", "/test", "another value" }, 1));
			Assert.AreEqual("another value", option.Value);
		}

		[Test()]
		public void TestOptionValueParseStringFail()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test"}, "desc", "VAL", "original value");
			Assert.AreEqual(0, option.Parse(new String[] { "/foo", "/test", "third value" }, 0));
			Assert.AreEqual(0, option.Parse(new String[] { "/test" }, 0));
		}

		[Test()]
		public void TestOptionValueParseStringFailThenSuccess()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test"}, "desc", "VAL", "original value");
			Assert.AreEqual(0, option.Parse(new String[] { "/foo", "third value" }, 0));
			Assert.AreEqual(2, option.Parse(new String[] { "/test", "some value" }, 0));
			Assert.AreEqual("some value", option.Value);
		}

		[Test()]
		[ExpectedException(typeof(CommandLineParser.ExceptionParse))]
		public void TestOptionValueParseStringTwiceException()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test"}, "desc", "VAL", "original value");
			Assert.AreEqual(2, option.Parse(new String[] { "/test", "some value" }, 0));
			Assert.AreEqual("some value", option.Value);
			//Assert.Throws<CommandLineParser.ParseException>(
			//	() => option.Parse(new String[] { "/test", "some value" }, 0);
			Assert.AreEqual(2, option.Parse(new String[] { "/test", "some value" }, 0));
			Assert.IsTrue(false, "never get here");
		}

		[Test()]
		public void TestOptionValueParseIntSuccess()
		{
			CommandLineParser.OptionValue<int> option = new CommandLineParser.OptionValue<int>(new String[]{"test"});
			Assert.AreEqual(2, option.Parse(new String[] { "/test", "99" }, 0));
			Assert.AreEqual(99, option.Value);
		}
				
		[Test()]
		public void TestOptionValueParseDoubleSuccess()
		{
			CommandLineParser.OptionValue<double> option = new CommandLineParser.OptionValue<double>(new String[]{"test"});
			Assert.AreEqual(2, option.Parse(new String[] { "/test", "-123.456" }, 0));
			Assert.AreEqual(-123.456, option.Value);
		}

		[Test()]
		public void TestOptionValueParseTwiceAfterReset()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(new String[]{"test"}, "desc", "VAL", "original value");
			Assert.AreEqual(2, option.Parse(new String[] { "/test", "some value" }, 0));
			Assert.AreEqual("some value", option.Value);
			option.Reset();
			Assert.AreEqual("original value", option.Value);
			Assert.AreEqual(2, option.Parse(new String[] { "/test", "another value" }, 0));
			Assert.AreEqual("another value", option.Value);
		}
		
		[Test()]
		public void TestOptionBoolean()
		{
			CommandLineParser.OptionBoolean option = new CommandLineParser.OptionBoolean(new String[] { "bool" });
			Assert.IsFalse(option.Value);
			Assert.IsFalse(option.IsMatched);
			Assert.AreEqual(1, option.Parse(new String[] { "/bool" }, 0));
			Assert.IsTrue(option.Value);
			Assert.IsTrue(option.IsMatched);
		}
		
		
		[Test()]
		[ExpectedException(typeof(CommandLineParser.ExceptionParse))]
		public void TestOptionBooleanTwice()
		{
			CommandLineParser.OptionBoolean option = new CommandLineParser.OptionBoolean(new String[] { "bool" });
			Assert.AreEqual(1, option.Parse(new String[] { "-bool" }, 0));
			option.Parse(new String[] { "/bool" }, 0);
			Assert.IsTrue(false, "never get here");
		}

		[Test()]
		public void TestOptionListOneItem()
		{
			CommandLineParser.OptionList<String> option = new CommandLineParser.OptionList<String>(new String[] { "l" });
			CollectionAssert.IsEmpty(option.Value);
			Assert.AreEqual(2, option.Parse(new String[] { "/l", "foo" }, 0));
			CollectionAssert.AreEqual(new String[] {"foo"},option.Value);
		}
		
		[Test()]
		public void TestOptionListList()
		{
			CommandLineParser.OptionList<String> option = new CommandLineParser.OptionList<String>(new String[] { "l" });
			CollectionAssert.IsEmpty(option.Value);
			Assert.AreEqual(2, option.Parse(new String[] { "/l", "foo, bar ,baz" }, 0));
			CollectionAssert.AreEqual(new String[] {"foo", "bar", "baz"},option.Value);
		}
		
		[Test()]
		public void TestOptionListTwice()
		{
			CommandLineParser.OptionList<String> option = new CommandLineParser.OptionList<String>(new String[] { "l" });
			CollectionAssert.IsEmpty(option.Value);
			Assert.AreEqual(2, option.Parse(new String[] { "/l", "foo" }, 0));
			Assert.AreEqual(2, option.Parse(new String[] { "/l", "bar ,baz" }, 0));
			CollectionAssert.AreEqual(new String[] {"foo", "bar", "baz"},option.Value);
		}

		[Test()]
		public void TestOptionCount()
		{
			CommandLineParser.OptionCount option = new CommandLineParser.OptionCount(new String[] { "count" });
			Assert.AreEqual(0, option.Value);
			Assert.IsFalse(option.IsMatched);
			Assert.AreEqual(1, option.Parse(new String[] { "/count" }, 0));
			Assert.AreEqual(1, option.Value);
			Assert.IsTrue(option.IsMatched);
		}

		[Test()]
		public void TestOptionCountTwice()
		{
			CommandLineParser.OptionCount option = new CommandLineParser.OptionCount(new String[] { "count" });
			Assert.AreEqual(0, option.Value);
			Assert.IsFalse(option.IsMatched);

			Assert.AreEqual(1, option.Parse(new String[] { "/count" }, 0));
			Assert.AreEqual(1, option.Value);
			Assert.IsTrue(option.IsMatched);

			Assert.AreEqual(1, option.Parse(new String[] { "/count" }, 0));
			Assert.AreEqual(2, option.Value);
			Assert.IsTrue(option.IsMatched);
		}

	}
}

