using System;
using System.Collections.Generic;

namespace Trains2Calendar
{
	public interface ISelectionView<T>
	{
		void ParseAndFillCalendar (string stringToParse, DateTime day);

		T GetSavedCalendarID ();
		void SaveSelectedCalendarID (T calendarID);

		IParser GetParser();
		ICalendar<T> GetCalendar();
	}
}

