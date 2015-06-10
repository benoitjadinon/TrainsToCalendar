using System;
using System.Collections.Generic;

namespace Trains2Calendar
{
	public interface IParser
	{
		IList<Event> Parse (string descriptionToParse);
	}
}

