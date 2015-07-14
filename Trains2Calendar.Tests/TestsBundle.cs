using System;
using NUnit.Framework;
using Android.OS;
using BlueMarin;

namespace Trains2Calendar.Tests
{
	[TestFixture]
	public class TestsBundle
	{
		public string PropString { get; set; }
		public string PropStringDef { get; set; } = "default_value";

		Bundle bundle;

		[SetUp]
		public void Setup ()
		{
			bundle = new Bundle();
		}

		
		[TearDown]
		public void Tear ()
		{
		}

		[Test]
		public void TestString ()
		{
			bundle.PutValue(() => PropStringDef);
			Assert.AreEqual (PropStringDef, bundle.GetString(() => PropStringDef));
		}

		[Test]
		public void Fail ()
		{
			Assert.False (true);
		}

		[Test]
		[Ignore ("another time")]
		public void Ignore ()
		{
			Assert.True (false);
		}

		[Test]
		public void Inconclusive ()
		{
			Assert.Inconclusive ("Inconclusive");
		}
	}
}

