using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Widget;
using Android.Database;
using Android.Views;
using Acr.Settings;
using BlueMarin;
using BlueMarin.Android;

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

		const string SettingSelectedCalendarID = "SelectedCalendarID";

		string[] calendarsProjection = {
			CalendarContract.Calendars.InterfaceConsts.Id,
			CalendarContract.Calendars.InterfaceConsts.CalendarDisplayName,
			CalendarContract.Calendars.InterfaceConsts.AccountName,
			CalendarContract.Calendars.InterfaceConsts.CalendarColor
		};

		ListView lvCalendars;
		CheckBox cbAlways;
		Button btOK;

		ISettings settings;

		CalendarsAdapter adapter;


		public int SelectedCalendarID { get; set; } = -1;


		public MainActivity ()
		{
			settings = Acr.Settings.Settings.Local;
		}

		ICursor cursor;

		protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// restore state, from bundle if exists OTHERWISE from saved setting
			SelectedCalendarID = savedInstanceState.GetValue (() => SelectedCalendarID, GetSavedCalendarID ());

			// get values from intent, if any

			string stringToParse = null;
			DateTime day = DateTime.Today;
			if (HasReceivedEvents) {
				if (Intent.Extras.ContainsKey (IntentExtraDescription))
					stringToParse = Intent.Extras.GetString (IntentExtraDescription);
				if (Intent.Extras.ContainsKey (IntentExtraBeginTime)) {
					long ms = Intent.Extras.GetLong (IntentExtraBeginTime);
					day = new DateTime (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds (ms).ToLocalTime ();
				}
			}

			// setup view

			SetContentView (Resource.Layout.Main);

			cbAlways = FindViewById<CheckBox> (Resource.Id.cbAlways);

			btOK = FindViewById<Button> (Resource.Id.btOK);
			btOK.Click += (sender, e) => ParseAndFillCalendar (stringToParse, day);
			btOK.Selected &= GetSavedCalendarID () != -1;

			var btCancel = FindViewById<Button> (Resource.Id.btCancel);
			btCancel.Click += (sender, e) => Finish ();

			var introTxt = FindViewById<TextView> (Resource.Id.txtIntro);
			if (HasReceivedEvents) {
				introTxt.Text = GetString (Resource.String.intro_create);
			} else {
				introTxt.Text = GetString (Resource.String.intro);
			}

			// calendars
			//TODO calendarID = get from settings -> preselect calendar
			cursor = ManagedQuery (CalendarContract.Calendars.ContentUri, calendarsProjection, null, null, null);
			if (cursor.Count > 0) {
				adapter = new CalendarsAdapter (this, cursor, calendarsProjection, () => SelectedCalendarID);
				UpdateState ();
				lvCalendars = FindViewById<ListView> (Resource.Id.lvCalendars);
				lvCalendars.Adapter = adapter;
				lvCalendars.ItemClick += (sender, e) => {
					var item = adapter.GetItem(e.Position);
					cursor.MoveToPosition(e.Position);
					SelectedCalendarID = cursor.GetInt (calendarsProjection.ToList ().IndexOf (CalendarContract.Calendars.InterfaceConsts.Id));
					lvCalendars.Invalidate ();
					adapter.NotifyDataSetChanged();
					UpdateState ();
				};
			} else {
				new AlertDialog.Builder(this)
					.SetTitle(Resource.String.error)
					.SetMessage(Resource.String.error_no_calendars)
					.SetNeutralButton(Android.Resource.String.Cancel, delegate{ 
						Finish();
					})
					.SetPositiveButton(Android.Resource.String.Ok, delegate{ 
						Finish();
						StartActivity(new Intent(Android.Provider.Settings.ActionSettings));
					})
					.Show();
			}
		}

		protected override void OnSaveInstanceState (Bundle outState)
		{
			base.OnSaveInstanceState (outState);
			//outState.PutInt(nameof(SelectedCalendarID), SelectedCalendarID);
			outState.PutValue(() => SelectedCalendarID);
		}

		bool HasReceivedEvents {
			get { 
				return (Intent != null && Intent.HasExtra (IntentExtraDescription) && Intent.HasExtra (IntentExtraBeginTime)); 
			}
		}

		void UpdateState ()
		{
			btOK.Enabled = SelectedCalendarID > -1;
		}

		#region IMainActivity implementation

		public int GetSavedCalendarID ()
		{
			return settings.Get(SettingSelectedCalendarID, -1);
		}

		public void SaveSelectedCalendarID (int calendarID)
		{
			settings.Set (SettingSelectedCalendarID, calendarID);
		}

		public void ParseAndFillCalendar (string stringToParse, DateTime day)
		{
			SaveSelectedCalendarID((cbAlways.Checked) ? SelectedCalendarID : -1);

			// just saving, not coming from an Intent
			if (stringToParse == null) {
				//Finish (Resource.String.description_parse_error);
				Finish ();
				return;
			}

			// parse
			var events = GetParser().Parse (stringToParse, day);

			// add
			var result = GetCalendar().AddEvents(events, SelectedCalendarID);

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


	class CalendarsAdapter : SimpleCursorAdapter 
	{
		readonly List<String> projection;

		readonly Func<int> getSelectedIdFunc;

		public CalendarsAdapter (Context context, ICursor c, string[] projection, Func<int> getSelected) 
			: base(context, Resource.Layout.CalendarListItem, c, projection, new int[] {
					Resource.Id.calId, 
					Resource.Id.calDisplayName, 
					Resource.Id.calAccountName,
					Resource.Id.calColor
				})
		{
			this.getSelectedIdFunc = getSelected;
			this.projection = projection.ToList();
		}

		public override void BindView (Android.Views.View view, Context context, ICursor cursor)
		{
			base.BindView (view, context, cursor);

			var color = cursor.GetInt (projection.IndexOf (CalendarContract.Calendars.InterfaceConsts.CalendarColor));
			var calId = cursor.GetInt (projection.IndexOf (CalendarContract.Calendars.InterfaceConsts.Id));

			var viewHolder = (CalendarViewHolder)view.Tag ?? new CalendarViewHolder {
				ColorSwatch = view.FindViewById<View>(Resource.Id.calColorSwatch),
				RadioButton = view.FindViewById<RadioButton>(Resource.Id.checkBox),
			};

			viewHolder.ColorSwatch.SetBackgroundColor(new Android.Graphics.Color(color));
			viewHolder.RadioButton.Checked = getSelectedIdFunc() == calId;
			//view.SetBackgroundColor(context.Resources.GetColor((getSelectedIndexFunc() == calId) ? Android.Resource.Color.PrimaryTextDark : Android.Resource.Color.Transparent));
		}

		internal class CalendarViewHolder : Java.Lang.Object {
			internal View ColorSwatch {get;set;}
			public RadioButton RadioButton {get;set;}
		}
	}
}


