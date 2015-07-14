using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Android.Content;
using System.Text.RegularExpressions;

namespace Trains2Calendar
{
	public class NMBSNCBParser : IParser
	{
		const StringComparison Comp = StringComparison.InvariantCulture;

		const char LineBreak = '\n'; //Environment.NewLine;
		const string TokenType = ")\t";
		const string TokenDest = "-> ";
		const string TokenHour = "HH:mm";

		string tokenStart;
		string tokenStop;
		string tokenWalk;
		string tokenPlatform;

		readonly string[] TrainTypes = new [] { "IC", "IR", "P", "ICT", "City Rail", "L" };

		public NMBSNCBParser (Context context)
		{
			tokenStart = context.GetString (Resource.String.token_start);       // = "Departure ";
			tokenStop = context.GetString (Resource.String.token_stop);         // = "Arrival ";
			tokenWalk = context.GetString (Resource.String.token_walk);         // = "walk";
			tokenPlatform = context.GetString (Resource.String.token_platform); // = "Platf. ";
		}

		#region IParser implementation

		public IList<Event> Parse (string descriptionToParse, DateTime day)
		{
			var events = new List<Event> ();
			var lines = descriptionToParse.Split (LineBreak);

			Event evt = null;
			int cnt = 0;
			foreach (string line in lines) {
				if (line.Length == 0 || cnt == 0) {
					evt = new Event ();
					events.Add (evt);
				}

				Match stepMatch;
				if (line.Contains (TokenType) && (stepMatch = Regex.Match (line, "([0-9]{1,3})(?=" + Regex.Escape (TokenType) + ")")).Success) {
					evt.Step = Convert.ToUInt32(stepMatch.Value);

					evt.Name = line.Substring (line.IndexOf (TokenType, Comp) + TokenType.Length).Trim () + (evt.Name != null ? " " + evt?.Name : "");//.Trim();

					if (TrainTypes.Any (trainType => line.Trim ().ToLowerInvariant ().StartsWith (trainType.ToLowerInvariant (), Comp))) {
						evt.Name = line + (evt.Name != null ? " " + evt?.Name : "");//.Trim();
						evt.Type = TransportTypes.Train;
					} else {
						//TODO : support more types
						evt.Type = (evt.Name.Trim ().StartsWith (tokenWalk, Comp)) ? TransportTypes.Walk : TransportTypes.Train;
					}

					//events.Add (evt);
				}
				else if (TrainTypes.Any (trainType => line.Trim ().ToLowerInvariant ().StartsWith (trainType.ToLowerInvariant (), Comp))) 
				{
					evt.Name = line + (evt.Name != null ?  " " + evt?.Name : "");//.Trim();
					evt.Type = TransportTypes.Train;
					//events.Add (evt);
				}
				else if (line.StartsWith (TokenDest, Comp)) 
				{
					var dest = line.Substring (TokenDest.Length);
					evt.Name = (dest + " " + evt?.Name ?? "");//.Trim();
				}
				else if (line.Trim ().StartsWith (tokenStart, Comp)) 
				{
					evt.Departure = ParseActionLine (line, tokenStart, tokenPlatform, day);
				}
				else if (line.Trim ().StartsWith (tokenStop, Comp))
				{
					evt.Arrival = ParseActionLine (line, tokenStop, tokenPlatform, day);
				}

				cnt++;
			}
			return events;
		}

		#endregion

		Action ParseActionLine (string line, string actiontype, string tokenPlatform, DateTime day)
		{
			line = line.Trim();

			int timePos = line.IndexOf (actiontype, Comp) + actiontype.Length;
			int textPos = timePos + TokenHour.Length + 1;
			int commaPos = line.LastIndexOf (",", Comp);

			string timeSting = line.Substring (timePos, TokenHour.Length);
			DateTime dayAndTime = day;
			try {
				DateTime timeHour = DateTime.ParseExact (timeSting, TokenHour, CultureInfo.InvariantCulture);
				dayAndTime = day.Date.AddHours(timeHour.Hour).AddMinutes(timeHour.Minute);
			} catch(Exception) {}

			string name = null;
			if (commaPos == -1) {
				name = line.Substring (textPos).Trim();
			} else {
				name = line.Substring (textPos, commaPos - textPos).Trim();
			}

			string platform = null;
			if (line.Contains (tokenPlatform)) {
				platform = line.Substring (line.IndexOf (tokenPlatform, Comp) + tokenPlatform.Length);
				platform = platform.Trim();
			}

			return new Action {
				Time = dayAndTime,
				Name = name,
				Platform = platform,
			};
		}
	}
}

