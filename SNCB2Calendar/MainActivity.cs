using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;


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
		string[] calendarsProjection = {
				CalendarContract.Calendars.InterfaceConsts.Id,
				CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
				CalendarContract.Calendars.InterfaceConsts.AccountName,
				CalendarContract.Calendars.InterfaceConsts.CalendarColor
			};

		readonly StringComparison Comp = StringComparison.InvariantCulture;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			SetContentView (Resource.Layout.Main);

			var listView = FindViewById<ListView> (Resource.Id.calList);

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

			// parse
			var events = Parse (description);

			int calendarID = -1;//TODO: get from local settings

			if (calendarID == -1) {
				var cursor = ManagedQuery (CalendarContract.Calendars.ContentUri, calendarsProjection, null, null, null);
				SimpleCursorAdapter adapter = 
					new SimpleCursorAdapter (this, Resource.Layout.CalendarListItem, cursor, calendarsProjection, new int[] {
					Resource.Id.calId, 
					Resource.Id.calDisplayName, 
					Resource.Id.calAccountName,
					Resource.Id.calColor 
				});

				listView.Adapter = adapter;
				listView.ItemClick += (sender, e) => {
					cursor.MoveToPosition (e.Position);
					calendarID = cursor.GetInt (cursor.GetColumnIndex (calendarsProjection [0]));

					//TODO: store calendar id in local settings

					FillCalendar (calendarID, events);
				};
			} else {
				FillCalendar(calendarID, events);
			}
		}

		void FillCalendar (int calendarID, List<Event> events)
		{
			foreach (var evt in events) {
				AddToCalendar(evt, calendarID);
			}

			Toast.MakeText(this, Android.Resource.String.Ok, ToastLength.Short).Show();

			Finish();
		}

		Android.Net.Uri AddToCalendar (Event evt, int calendarID)
		{
			if (calendarID == -1)
				return null;

			ContentValues eventValues = new ContentValues ();

			eventValues.Put (CalendarContract.Events.InterfaceConsts.CalendarId, calendarID);
			eventValues.Put (CalendarContract.Events.InterfaceConsts.Title, evt.Title);
			//eventValues.Put (CalendarContract.Events.InterfaceConsts.Description, evt.ToString());
			eventValues.Put (CalendarContract.Events.InterfaceConsts.Dtstart, GetDateTimeMS (evt.Departure.Time));
			eventValues.Put (CalendarContract.Events.InterfaceConsts.Dtend, GetDateTimeMS (evt.Arrival.Time));
			eventValues.Put (CalendarContract.Events.InterfaceConsts.EventTimezone, "UTC");
			eventValues.Put (CalendarContract.Events.InterfaceConsts.EventEndTimezone, "UTC");
			eventValues.Put (CalendarContract.Events.InterfaceConsts.EventLocation, evt.Departure.Name);
			//TODO: add reminders
			//CalendarContract.Reminders.InterfaceConsts.EventId

			return ContentResolver.Insert (CalendarContract.Events.ContentUri, eventValues);
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


		const char LineBreak = '\n';
		const string TokenType  = ")  ";
		const string TokenDest  = "-> ";
		const string TokenStart = "Departure ";
		const string TokenStop  = "Arrival ";
		const string TokenPlat  = "Platf. ";
		const string TokenHour  = "HH:mm";
		const string TokenWalk  = "walk";

		readonly string[] TrainTypes = new [] { "IC", "IR", "P", "ICT", "City Rail", "L" };

		List<Event> Parse (string description)
		{
			var events = new List<Event> ();
			var lines = description.Split (LineBreak);

			Event evt = new Event ();
			events.Add (evt);

			int cnt = 0;
			foreach (string line in lines) {
				if (line.Length == 0) {
					evt = new Event ();
					events.Add (evt);
				} else if (line.Contains (TokenType)) {
					//TODO : regexp: 0-9) 
					evt.Name = line.Substring (line.IndexOf (TokenType, Comp) + TokenType.Length).Trim();
					//TODO : more types
					evt.Type = (evt.Name.Trim ().StartsWith (TokenWalk, Comp)) ? Types.Walk : Types.Train;
					events.Add (evt);
				}else if (TrainTypes.Any (line.Trim().StartsWith)) {
					evt.Name = line + " " + evt.Name ?? "";
					evt.Type = Types.Train;
				}else if (line.StartsWith (TokenDest, Comp)) {
					var dest = line.Substring (TokenDest.Length);
					evt.Name = dest + " " + evt.Name ?? "";
				}else if (line.Trim().StartsWith(TokenStart, Comp)) {
					evt.Departure = ParseActionLine(line, TokenStart);
				}else if (line.Trim().StartsWith(TokenStop, Comp)) {
					evt.Arrival = ParseActionLine(line, TokenStop);
				}
				cnt++;
			}
			return events;
		}

		Action ParseActionLine(string line, string actiontype) 
		{
			int timePos = line.IndexOf (actiontype, Comp) + actiontype.Length;
			int textPos = timePos + TokenHour.Length + 1;
			int commaPos = line.LastIndexOf(",", Comp);

			string timeSting = line.Substring(timePos, TokenHour.Length);
			DateTime time = DateTime.Now;
			try {
				time = DateTime.ParseExact (timeSting, TokenHour, CultureInfo.InvariantCulture);
			} finally {}

			var action = new Action(){
				Time = time,
			};

			if (commaPos == -1){
				action.Name = line.Substring(textPos);
			}else{
				action.Name = line.Substring(textPos, commaPos - textPos);
			}
			if (line.Contains(TokenPlat)){
				action.Platform = line.Substring(line.IndexOf(TokenPlat, Comp) + TokenPlat.Length);
			}
			return action;
		}


		class Event
		{
			public Types Type {get;set;}
			public string Name {get;set;}
			public Action Departure {get;set;}
			public Action Arrival {get;set;}

			public string Title {
				get { 
					return string.Format("{0}: {1} > {2}", Departure?.Platform ?? "?", Name ?? "?", Arrival?.Name ?? "?");
				}
			}

			public override string ToString ()
			{
				return string.Format ("[Event: Type={0}, Name={1}, Departure={2}, Arrival={3}]", Type, Name, Departure, Arrival);
			}
		}

		class Action
		{
			public string Name {get;set;}
			public string Platform {get;set;}
			public DateTime Time {get;set;}

			public override string ToString ()
			{
				return string.Format ("[Action: Name={0}, Platform={1}, Time={2}]", Name, Platform, Time);
			}
		}

		enum Types 
		{
			Train,
			Walk,
			Bus,
		}
	}
}


