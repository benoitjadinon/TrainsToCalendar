using System;

namespace Trains2Calendar
{
	public class Action
	{
		public string Name { get; set; }

		public string Platform { get; set; }

		public DateTime Time { get; set; }

		public override string ToString ()
		{
			return string.Format ("[Action: Name={0}, Platform={1}, Time={2}]", Name, Platform, Time);
		}
	}
}

