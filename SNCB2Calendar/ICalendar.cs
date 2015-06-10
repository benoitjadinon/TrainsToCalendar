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
	public interface ICalendar<T>
	{
		bool AddEvent (Event evt, T calendarID);
		bool AddEvents(IList<Event> events, T calendarID);
	}

}


