﻿using System;
using System.Collections.Generic;

namespace SNCB2Calendar
{
	public interface IParser
	{
		IList<Event> Parse (string descriptionToParse);
	}
}

