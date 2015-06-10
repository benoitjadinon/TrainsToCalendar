using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

//TODO: rename to Trains2Calendar
namespace SNCB2Calendar
{
	[Activity (Label = "Trains2Calendar", MainLauncher = true, Icon = "@drawable/icon")]

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
    //TODO: remove more, and find a way to limit more to sncb/nmbs app
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
		const string IntentExtraDescription = "description";

		string[] calendarsProjection = {
				CalendarContract.Calendars.InterfaceConsts.Id,
				CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
				CalendarContract.Calendars.InterfaceConsts.AccountName,
				CalendarContract.Calendars.InterfaceConsts.CalendarColor
			};

		readonly StringComparison Comp = StringComparison.InvariantCulture;

		ListView lvCalendars;
		CheckBox cbAlways;
		Button btOK;

		string description;

		int calendarID;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			SetContentView (Resource.Layout.Main);

			lvCalendars = FindViewById<ListView> (Resource.Id.lvCalendars);
			cbAlways = FindViewById<CheckBox> (Resource.Id.cbAlways);

			btOK = FindViewById<Button> (Resource.Id.btOK);
			btOK.Click += (sender, e) => FillCalendar();

			var btCancel = FindViewById<Button> (Resource.Id.btCancel);
			btCancel.Click += (sender, e) => Finish();

			// calendars
			//TODO calendarID = get from settings
			var cursor = ManagedQuery (CalendarContract.Calendars.ContentUri, calendarsProjection, null, null, null);
			SimpleCursorAdapter adapter = 
				new SimpleCursorAdapter (this, Resource.Layout.CalendarListItem, cursor, calendarsProjection, new int[] {
				Resource.Id.calId, 
				Resource.Id.calDisplayName, 
				Resource.Id.calAccountName,
				Resource.Id.calColor 
			});
			if (calendarID != -1){
				//TODO: pre-select with calendarID
				UpdateState();
			}
			lvCalendars.Adapter = adapter;
			lvCalendars.ItemClick += (sender, e) => {
				cursor.MoveToPosition (e.Position);
				calendarID = cursor.GetInt (cursor.GetColumnIndex (calendarsProjection [0]));

				//TODO: store calendar id in local settings

				UpdateState();
			};
		}

		void UpdateState ()
		{
			btOK.Enabled = calendarID > -1;
		}

		void FillCalendar ()
		{
			// parse
			var events = Parse (description);

			//TODO: add check if opened from drawer or from share intent
			if (Intent.HasExtra (IntentExtraDescription)) {
				description = Intent.Extras.GetString (IntentExtraDescription);
			} else {
				Finish(Resource.String.description_parse_error);
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
				Finish(Resource.String.description_parse_error);
				return;
			}

			FillCalendar (events);
		}

		void FillCalendar (List<Event> events)
		{
			bool result = false;
			foreach (var evt in events) {
				result |= AddToCalendar(evt, calendarID);
			}

			Finish(result ? Resource.String.events_added : Resource.String.events_add_error);
		}

		void Finish (int message)
		{
			Finish(GetString(message));
		}
		void Finish (string message)
		{
			Toast.MakeText(this, message, ToastLength.Long).Show();

			Finish();
		}

		bool AddToCalendar (Event evt)
		{
			if (calendarID == -1)
				return false;

			try {
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
				
				Android.Net.Uri uri = ContentResolver.Insert (CalendarContract.Events.ContentUri, eventValues);
			} catch (Exception ex) {
				return false;
			}

			return true;
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


		const char LineBreak = '\n';//Environment.NewLine;
		const string TokenType  = ")  ";
		const string TokenDest  = "-> ";
		const string TokenStart = "Departure ";
		const string TokenStop  = "Arrival ";
		const string TokenPlat  = "Platf. ";
		const string TokenHour  = "HH:mm";
		const string TokenWalk  = "walk";

		readonly string[] TrainTypes = new [] { "IC", "IR", "P", "ICT", "City Rail", "L" };

		List<Event> Parse (string desc)
		{
			var events = new List<Event> ();
			var lines = desc.Split (LineBreak);

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


