using System;
using System.Collections.Generic;

namespace Trains2Calendar
{
	public interface ISelectionView<T>
	{
		void ParseAndFillCalendar (string stringToParse);

		T getSavedCalendarID ();
		void saveSelectedCalendarID (T calendarID);

		IParser GetParser();
		ICalendar<T> GetCalendar();
	}
}

