using System;
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

			string stringToParse = null;
			//TODO: add check if opened from drawer or from share intent
			if (Intent.HasExtra (IntentExtraDescription)) {
				stringToParse = Intent.Extras.GetString (IntentExtraDescription);
			} else {
				Finish (Resource.String.description_parse_error);
			}
			if (stringToParse == null) {
				Finish (Resource.String.description_parse_error);
				return;
			}

			SetContentView (Resource.Layout.Main);

			lvCalendars = FindViewById<ListView> (Resource.Id.lvCalendars);
			cbAlways = FindViewById<CheckBox> (Resource.Id.cbAlways);

			btOK = FindViewById<Button> (Resource.Id.btOK);
			btOK.Click += (sender, e) => ParseAndFillCalendar (stringToParse);

			var btCancel = FindViewById<Button> (Resource.Id.btCancel);
			btCancel.Click += (sender, e) => Finish ();

			// calendars
			//TODO calendarID = get from settings
			var cursor = ManagedQuery (CalendarContract.Calendars.ContentUri, calendarsProjection, null, null, null);
			SimpleCursorAdapter adapter = 
				new SimpleCursorAdapter (this, Resource.Layout.CalendarListItem, cursor, calendarsProjection, new int[] {
					Resource.Id.calId, 
					Resource.Id.calDisplayName, 
					Resource.Id.calAccountName,
					Resource.Id.calColor 
				}
			);

			selectedCalendarID = getSavedCalendarID();
			if (selectedCalendarID != -1) {
				//TODO: pre-select with calendarID
				UpdateState ();
			}
			lvCalendars.Adapter = adapter;
			lvCalendars.ItemClick += (sender, e) => {
				cursor.MoveToPosition (e.Position);
				selectedCalendarID = cursor.GetInt (cursor.GetColumnIndex (calendarsProjection [0]));

				//TODO: store calendar id in local settings
				saveSelectedCalendarID(selectedCalendarID);

				UpdateState ();
			};
		}

		void UpdateState ()
		{
			btOK.Enabled = selectedCalendarID > -1;
		}

		#region IMainActivity implementation

		public int getSavedCalendarID ()
		{
			//TODO, get from settings
			//throw new NotImplementedException ();
			return -1;
		}

		public void saveSelectedCalendarID (int calendarID)
		{
			//TODO
		}

		public void ParseAndFillCalendar (string stringToParse)
		{
			// parse
			var events = GetParser().Parse (stringToParse);

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
			return calendar ?? (calendar = new AndroidCalendar(ContentResolver));
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


