using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;


namespace SNCB2Calendar
{
	[Activity (Label = "SNCB2Calendar", MainLauncher = true, Icon = "@drawable/icon")]
	[IntentFilter ( new[]{ Intent.ActionInsert }, 
		Categories = new[]{ Intent.CategoryDefault },
        DataScheme = "content",
        DataHost = "com.android.calendar",
        DataPath = "/events"
    )]

	[IntentFilter ( new[]{ Intent.ActionEdit, Intent.ActionInsert }, 
		Categories = new[]{ Intent.CategoryDefault },
		DataMimeType = "vnd.android.cursor.item/event"
    )]
	[IntentFilter ( new[]{ Intent.ActionEdit, Intent.ActionInsert }, 
		Categories = new[]{ Intent.CategoryDefault },
		DataMimeType = "vnd.android.cursor.dir/event"
    )]
	[IntentFilter ( new[]{ Intent.ActionEdit }, 
        Categories = new[]{ Intent.CategoryDefault },
        DataScheme = "content",
        DataHost = "com.android.calendar",
        DataPath = "/events"
    )]
    /*
	[IntentFilter ( new[]{ Intent.ActionView }, 
        Categories = new[]{ Intent.CategoryDefault },
        DataScheme = "content",
        DataHost = "com.android.calendar",
		DataMimeType = "time/epoch"
    )]
    */

	public class MainActivity : Activity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			string description = null;
			if (Intent.HasExtra ("description")) {
				description = Intent.Extras.GetString ("description");
			}
			#if DEBUG
			/*if (description == null) {
				description = "1)walk 4 minutes";
				description += "\n Departure 06:10 Namur";
				description += "\n Arrival 06:14 Gare";
				description += "\n";
				description += "\nIC 2127";
				description += "\n-> Brussel-Zuid / Bruxelles-Midi";
				description += "\n Departure 06:14 Namur, Platf. 9";
				description += "\n Arrival 07:27 Bruxelles-Zuid / Bruxelles-Midi, Platf. 14";
				description += "\n";
				description += "\nIC 428";
				description += "\n-> Kortrijk";
				description += "\n Departure 07:33 Bruxelles-Zuid / Bruxelles-Midi, Platf. 11";
				description += "\n Arrival 08:08 Gent-Sint-Pieters, Platf. 8";
			}*/
			#endif
			if (description == null) {
				return;
			}

			// get calendar
			var calendarsUri = CalendarContract.Calendars.ContentUri;
			string[] calendarsProjection = {
			    CalendarContract.Calendars.InterfaceConsts.Id,
			    CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
			    CalendarContract.Calendars.InterfaceConsts.AccountName
			};
			int calendarID = -1;
			string calendarName;
			using (var cursor = ManagedQuery (calendarsUri, calendarsProjection, null, null, null)) {
				for (int i = 0; i < cursor.Count; i++) {
					cursor.MoveToPosition(i);
					calendarID = cursor.GetInt(cursor.GetColumnIndex (calendarsProjection [0]));
					calendarName = cursor.GetString(cursor.GetColumnIndex (calendarsProjection [1]));
				}
			}

			// parse
			var events = Parse(description);

			// fill calendar
			foreach (var evt in events) {
				AddToCalendar(evt, calendarID);
			}
		}

		void AddToCalendar (Event evt, int calendarID)
		{
			if (calendarID == -1)
				return;

			ContentValues eventValues = new ContentValues ();

			eventValues.Put (CalendarContract.Events.InterfaceConsts.CalendarId, calendarID);
			eventValues.Put (CalendarContract.Events.InterfaceConsts.Title, string.Format("{0}) {1}", evt.Departure.Platform, evt.Departure.Name));
			//eventValues.Put (CalendarContract.Events.InterfaceConsts.Description, evt.ToString());
			eventValues.Put (CalendarContract.Events.InterfaceConsts.Dtstart, GetDateTimeMS (evt.Departure.Time));
			eventValues.Put (CalendarContract.Events.InterfaceConsts.Dtend, GetDateTimeMS (evt.Arrival.Time));
			eventValues.Put (CalendarContract.Events.InterfaceConsts.EventTimezone, "UTC");
			eventValues.Put (CalendarContract.Events.InterfaceConsts.EventEndTimezone, "UTC");

			var uri = ContentResolver.Insert (CalendarContract.Events.ContentUri, eventValues);
		}

		long GetDateTimeMS (DateTime date)
		{
			return GetDateTimeMS(date.Year, date.Month-1, date.Day, date.Hour, date.Minute);
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


		const string Destination = "->";
		const string Departure   = "Departure ";
		const string Arrival     = "Arrival ";
		const string Platform    = "Platf. ";
		const string HourFormat  = "HH:mm";

		List<Event> Parse (string description)
		{
			var events = new List<Event> ();
			var lines = description.Split ('\n');

			Event evt = new Event();
			int cnt = 0;
			foreach (string line in lines) {
				if (line.StartsWith (Destination)) {
					evt = new Event(){ Name = line.Substring(3) };
				}else if (line.Trim().StartsWith(Departure)) {
					evt.Departure = ParseAction(line, Departure);
				}else if (line.Trim().StartsWith(Arrival)) {
					evt.Arrival = ParseAction(line, Arrival);
					events.Add(evt);
				}
				cnt++;
			}
			return events;
		}

		Action ParseAction(string line, string actiontype) {
			int timePos = line.IndexOf(actiontype) + actiontype.Length;
			int textPos = timePos + HourFormat.Length + 1;
			int commaPos = line.LastIndexOf(",");

			string timeSting = line.Substring(timePos, HourFormat.Length);
			DateTime time = DateTime.Now;
			try {
				time = DateTime.ParseExact (timeSting, HourFormat, CultureInfo.InvariantCulture);
			} finally {}

			var action = new Action(){
				Time = time,
			};

			if (commaPos == -1){
				action.Name = line.Substring(textPos);
			}else{
				action.Name = line.Substring(textPos, commaPos - textPos);
			}
			if (line.Contains(Platform)){
				action.Platform = line.Substring(line.IndexOf(Platform) + Platform.Length);
			}
			return action;
		}


		class Event{
			public string Name {get;set;}
			public Action Departure {get;set;}
			public Action Arrival {get;set;}
		}
		class Action{
			public string Name {get;set;}
			public string Platform {get;set;}
			public DateTime Time {get;set;}
		}
	}
}


