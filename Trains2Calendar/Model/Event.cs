using System;

namespace Trains2Calendar
{
	public class Event
	{
		public uint Step = 0;

		public TransportTypes Type { get; set; }

		public string Name { get; set; }

		public Action Departure { get; set; }

		public Action Arrival { get; set; }

		public override string ToString () =>  string.Format ("[Event: Step={4}, Type={0}, Name={1}, Departure={2}, Arrival={3}]", Type, Name, Departure, Arrival, Step);
	}
}

