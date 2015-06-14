using System;
using Android.Content;
using Android.Provider;

namespace Trains2Calendar
{
	public class AndroidCalendar : ICalendar<int>
	{
		readonly ContentResolver contentResolver;
		readonly string platformAbbr;

		public AndroidCalendar (Context context)
		{
			this.contentResolver = context.ContentResolver;
			this.platformAbbr = context.GetString (Resource.String.platform_abbr);   //= "Pf."
		}

		#region ICalendar implementation

		public bool AddEvents (System.Collections.Generic.IList<Event> events, int calendarID)
		{
			bool result = false;
			foreach (var evt in events) {
				result |= AddEvent (evt, calendarID);
			}
			return result;
		}

		public bool AddEvent (Event evt, int calendarID)
		{
			if (calendarID == -1)
				return false;

			try {
				ContentValues eventValues = new ContentValues ();
				
				eventValues.Put (CalendarContract.Events.InterfaceConsts.CalendarId, calendarID);
				eventValues.Put (CalendarContract.Events.InterfaceConsts.Title, GetTitle(evt));
				//eventValues.Put (CalendarContract.Events.InterfaceConsts.Description, evt.ToString());
				eventValues.Put (CalendarContract.Events.InterfaceConsts.Dtstart, GetDateTimeMS (evt.Departure.Time));
				eventValues.Put (CalendarContract.Events.InterfaceConsts.Dtend, GetDateTimeMS (evt.Arrival.Time));
				eventValues.Put (CalendarContract.Events.InterfaceConsts.EventTimezone, "UTC");
				eventValues.Put (CalendarContract.Events.InterfaceConsts.EventEndTimezone, "UTC");
				eventValues.Put (CalendarContract.Events.InterfaceConsts.EventLocation, evt.Departure.Name);
				//TODO: add reminders
				//CalendarContract.Reminders.InterfaceConsts.EventId
				
				contentResolver.Insert (CalendarContract.Events.ContentUri, eventValues);
			} catch (Exception) {
				return false;
			}

			return true;
		}

		long GetDateTimeMS (DateTime date)
		{
			return GetDateTimeMS (date.Year, date.Month - 1, date.Day, date.Hour, date.Minute);
		}

		long GetDateTimeMS (int yr, int monthZeroBased, int dayOfMonth, int hr, int min)
		{
			Java.Util.Calendar c = Java.Util.Calendar.GetInstance (Java.Util.TimeZone.Default);

			c.Set (Java.Util.CalendarField.DayOfMonth, dayOfMonth);
			c.Set (Java.Util.CalendarField.HourOfDay, hr);
			c.Set (Java.Util.CalendarField.Minute, min);
			c.Set (Java.Util.CalendarField.Month, monthZeroBased);
			c.Set (Java.Util.CalendarField.Year, yr);

			return c.TimeInMillis;
		}

		public string GetTitle (Event e)
		{
			return string.Format ("{0}{1}-{2} -> {3}", platformAbbr, e.Departure?.Platform ?? "", e.Name ?? "", e.Arrival?.Name ?? "?");
		}

		#endregion
	}
}

