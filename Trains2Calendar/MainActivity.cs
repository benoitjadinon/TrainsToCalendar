﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;

namespace Trains2Calendar
{
	[Activity (Label = "Trains2Calendar", MainLauncher = true, Icon = "@drawable/icon")]

	[IntentFilter (new[]{ Intent.ActionInsert }, 
		Categories = new[]{ Intent.CategoryDefault },
		DataScheme = "content",
		DataHost = "com.android.calendar",
		DataPath = "/events"
	)]

	[IntentFilter (new[]{ Intent.ActionEdit, Intent.ActionInsert }, 
		Categories = new[]{ Intent.CategoryDefault },
		DataMimeType = "vnd.android.cursor.item/event"
	)]
	[IntentFilter (new[]{ Intent.ActionEdit, Intent.ActionInsert }, 
		Categories = new[]{ Intent.CategoryDefault },
		DataMimeType = "vnd.android.cursor.dir/event"
	)]
	[IntentFilter (new[]{ Intent.ActionEdit }, 
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


	public class MainActivity : Activity, ISelectionView<int>
	{
		const string IntentExtraDescription = "description";
		const string IntentExtraBeginTime = "beginTime";

		string[] calendarsProjection = {
			CalendarContract.Calendars.InterfaceConsts.Id,
			CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
			CalendarContract.Calendars.InterfaceConsts.AccountName,
			CalendarContract.Calendars.InterfaceConsts.CalendarColor
		};

		ListView lvCalendars;
		CheckBox cbAlways;
		Button btOK;

		int selectedCalendarID;


		public MainActivity ()
		{
			#if DEBUG
			/*if (stringToParse == null) {
				stringToParse = "1)walk 4 minutes";
				stringToParse += "\n Departure 06:10 Namur";
				stringToParse += "\n Arrival 06:14 Gare";
				stringToParse += "\n";
				stringToParse += "\nIC 2127";
				stringToParse += "\n-> Brussel-Zuid / Bruxelles-Midi";
				stringToParse += "\n Departure 06:14 Namur, Platf. 9";
				stringToParse += "\n Arrival 07:27 Bruxelles-Zuid / Bruxelles-Midi, Platf. 14";
				stringToParse += "\n";
				stringToParse += "\nIC 428";
				stringToParse += "\n-> Kortrijk";
				stringToParse += "\n Departure 07:33 Bruxelles-Zuid / Bruxelles-Midi, Platf. 11";
				stringToParse += "\n Arrival 08:08 Gent-Sint-Pieters, Platf. 8";
			}*/
			#endif
		}

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// get values from intent

			string stringToParse = null;
			DateTime day = DateTime.Today;
			if (Intent.HasExtra (IntentExtraDescription)) {
				if (Intent.Extras.ContainsKey(IntentExtraDescription)) 
					stringToParse = Intent.Extras.GetString (IntentExtraDescription);
				if (Intent.Extras.ContainsKey(IntentExtraBeginTime)) {
					long ms = Intent.Extras.GetLong (IntentExtraBeginTime);
					day = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds (ms).ToLocalTime ();
	            }
			}

			// setup view

			SetContentView (Resource.Layout.Main);

			lvCalendars = FindViewById<ListView> (Resource.Id.lvCalendars);
			cbAlways = FindViewById<CheckBox> (Resource.Id.cbAlways);

			btOK = FindViewById<Button> (Resource.Id.btOK);
			btOK.Click += (sender, e) => ParseAndFillCalendar (stringToParse, day);

			var btCancel = FindViewById<Button> (Resource.Id.btCancel);
			btCancel.Click += (sender, e) => Finish ();

			// calendars
			//TODO calendarID = get from settings -> preselect calendar
			var cursor = ManagedQuery (CalendarContract.Calendars.ContentUri, calendarsProjection, null, null, null);
			SimpleCursorAdapter adapter = 
				new SimpleCursorAdapter (this, Resource.Layout.CalendarListItem, cursor, calendarsProjection, new int[] {
					Resource.Id.calId, 
					Resource.Id.calDisplayName, 
					Resource.Id.calAccountName,
					Resource.Id.calColor 
				}
			);

			selectedCalendarID = GetSavedCalendarID();
			if (selectedCalendarID != -1) {
				//TODO: pre-select with calendarID
				UpdateState ();
			}
			lvCalendars.Adapter = adapter;
			lvCalendars.ItemClick += (sender, e) => {
				cursor.MoveToPosition (e.Position);
				selectedCalendarID = cursor.GetInt (cursor.GetColumnIndex (calendarsProjection [0]));

				UpdateState ();
			};
		}

		void UpdateState ()
		{
			btOK.Enabled = selectedCalendarID > -1;
		}

		#region IMainActivity implementation

		public int GetSavedCalendarID ()
		{
			//TODO, get from settings
			//throw new NotImplementedException ();
			return -1;
		}

		public void SaveSelectedCalendarID (int calendarID)
		{
			//TODO: store calendar id in local settings
		}

		public void ParseAndFillCalendar (string stringToParse, DateTime day)
		{
			if (cbAlways.Checked) 
				SaveSelectedCalendarID(selectedCalendarID);

			if (stringToParse == null) {
				//Finish (Resource.String.description_parse_error);
				return;
			}

			// parse
			var events = GetParser().Parse (stringToParse, day);

			// add
			var result = GetCalendar().AddEvents(events, selectedCalendarID);

			//
			Finish (result ? Resource.String.events_added : Resource.String.events_add_error);
		}

		IParser parser;
		public IParser GetParser ()
		{
			return parser ?? (parser = new NMBSNCBParser(this));
		}

		ICalendar<int> calendar;
		public ICalendar<int> GetCalendar ()
		{
			return calendar ?? (calendar = new AndroidCalendar(this));
		}

		#endregion

		void Finish (int message)
		{
			Finish (GetString (message));
		}

		void Finish (string message)
		{
			Toast.MakeText (Application.Context, message, ToastLength.Long).Show ();

			Finish ();
		}
	}
}


