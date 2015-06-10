using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Android.Content;

namespace Trains2Calendar
{
	public class NMBSNCBParser : IParser
	{
		const StringComparison Comp = StringComparison.InvariantCulture;

		const char LineBreak = '\n'; //Environment.NewLine;
		const string TokenType = ")  ";
		const string TokenDest = "-> ";
		const string TokenHour = "HH:mm";

		readonly string[] TrainTypes = new [] { "IC", "IR", "P", "ICT", "City Rail", "L" };

		Context context;

		public NMBSNCBParser (Context context)
		{
			this.context = context;
		}

		#region IParser implementation

		public IList<Event> Parse (string descriptionToParse)
		{
			//TODO: translate from nl/fr app
			string tokenStart = context.GetString(Resource.String.token_start);   // = "Departure ";
			string tokenStop = context.GetString(Resource.String.token_stop);     // = "Arrival ";
			string tokenPlat = context.GetString(Resource.String.token_platform); // = "Platf. ";
			string tokenWalk = context.GetString(Resource.String.token_walk);     // = "walk";
			string platformAbbr = context.GetString(Resource.String.platform_abbr);//= "Pf."

			var events = new List<Event> ();
			var lines = descriptionToParse.Split (LineBreak);

			Event evt = null;
			int cnt = 0;
			foreach (string line in lines) 
			{
				if (line.Length == 0 || cnt == 0) 
				{
					evt = new Event (platformAbbr);
					events.Add (evt);
				}

				if (line.Contains (TokenType)) 
				{
					//TODO : regexp: 0-9) 
					evt.Name = line.Substring (line.IndexOf (TokenType, Comp) + TokenType.Length).Trim () + (evt.Name != null ?  " " + evt?.Name : "");
					//TODO : more types
					evt.Type = (evt.Name.Trim ().StartsWith (tokenWalk, Comp)) ? Types.Walk : Types.Train;
					events.Add (evt);
				}
				else if (TrainTypes.Any (trainType => line.Trim ().ToLowerInvariant ().StartsWith (trainType.ToLowerInvariant (), Comp))) 
				{
					evt.Name = line + (evt.Name != null ?  " " + evt?.Name : "");
					evt.Type = Types.Train;
				}
				else if (line.StartsWith (TokenDest, Comp)) 
				{
					var dest = line.Substring (TokenDest.Length);
					evt.Name = dest + " " + evt?.Name ?? "";
				}
				else if (line.Trim ().StartsWith (tokenStart, Comp)) 
				{
					evt.Departure = ParseActionLine (line, tokenStart, tokenPlat);
				}
				else if (line.Trim ().StartsWith (tokenStop, Comp))
				{
					evt.Arrival = ParseActionLine (line, tokenStop, tokenPlat);
				}

				cnt++;
			}
			return events;
		}

		#endregion

		Action ParseActionLine (string line, string actiontype, string tokenPlat)
		{
			int timePos = line.IndexOf (actiontype, Comp) + actiontype.Length;
			int textPos = timePos + TokenHour.Length + 1;
			int commaPos = line.LastIndexOf (",", Comp);

			string timeSting = line.Substring (timePos, TokenHour.Length);
			DateTime time = DateTime.Now;
			try {
				time = DateTime.ParseExact (timeSting, TokenHour, CultureInfo.InvariantCulture);
			} finally {
			}

			var action = new Action () {
				Time = time,
			};

			if (commaPos == -1) {
				action.Name = line.Substring (textPos);
			} else {
				action.Name = line.Substring (textPos, commaPos - textPos);
			}
			if (line.Contains (tokenPlat)) {
				action.Platform = line.Substring (line.IndexOf (tokenPlat, Comp) + tokenPlat.Length);
			}
			return action;
		}
	}
}

