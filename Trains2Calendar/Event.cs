using System;

namespace Trains2Calendar
{
	public class Event
	{
		public static string TrackLocalizedName;

		public Event (string str)
		{
			TrackLocalizedName = str;
		}

		public Types Type { get; set; }

		public string Name { get; set; }

		public Action Departure { get; set; }

		public Action Arrival { get; set; }

		public string Title {
			get { 
				return string.Format ("{3}{0}: '{1}' -> {2}", Departure?.Platform ?? "", Name ?? "", Arrival?.Name ?? "?", TrackLocalizedName);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[Event: Type={0}, Name={1}, Departure={2}, Arrival={3}]", Type, Name, Departure, Arrival);
		}
	}
}

