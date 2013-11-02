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
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(
				new string[]{"/test"}, "some description", "PROMPT", false, null);
			String[] keys = option.Keys;
			CollectionAssert.AreEquivalent(new String[] {"/test"}, keys);
		}

		[Test()]
		public void TestOptionValueKeyExpand()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(
				new string[]{"test"}, "some description", "PROMPT", false, null);
			String[] keys = option.Keys;
			CollectionAssert.AreEquivalent(new String[] {"-test", "/test"}, keys);
		}
		
		[Test()]
		public void TestOptionValueKeyExist()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(
				new string[]{"test", "-t", "/test"}, "some description", "PROMPT", false, null);
			String[] keys = option.Keys;
			CollectionAssert.AreEquivalent(new String[] {"-test", "/test", "-t"}, keys);
		}

		[Test()]
		public void TestOptionValueDefault()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(
				new string[]{"/test"}, "some description", "PROMPT", false, null);
			Assert.IsNull(option.Value);
			Assert.IsNull(option.GetValue<String>());
			Assert.IsFalse(option.IsMatched);
		}
		
		[Test()]
		public void TestOptionValueSetDefault()
		{
			CommandLineParser.OptionValue<String> option = new CommandLineParser.OptionValue<String>(
				new string[]{"/test"}, "some description", "PROMPT", false, "some default");
			StringAssert.AreEqualIgnoringCase("some default", option.Value);
			StringAssert.AreEqualIgnoringCase("some default", option.GetValue<String>());
			Assert.IsFalse(option.IsMatched);
		}

		[Test()]
		[ExpectedException(typeof(CommandLineParser.OptionException))]
		public void TestOptionValueKeyWithBlank()
		{
			//Assert.Throws<CommandLineParser.OptionException>(
			//	() => new CommandLineParser.OptionValue<String>(new string[]{"bad key"}, "some description", "PROMPT", false, null));
			new CommandLineParser.OptionValue<String>(
				new string[]{"good", "bad key"}, "some description", "PROMPT", false, "some default");
		}


	}
}

