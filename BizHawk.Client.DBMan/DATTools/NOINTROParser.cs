using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;
using System.Windows.Forms;

namespace BizHawk.Client.DBMan
{
	public class NOINTROParser : DATParser
	{
		/// <summary>
		/// Required to generate a GameDB file
		/// </summary>
		public override SystemType SysType { get; set; }

		private List<XDocument> xmls = new List<XDocument>();

		public NOINTROParser(SystemType type)
		{
			SysType = type;
		}

		/// <summary>
		/// Parses multiple DAT files and returns a single GamesDB format csv string
		/// </summary>
		public override string ParseDAT(string[] filePath)
		{
			foreach (var s in filePath)
			{
				try
				{
					xmls.Add(XDocument.Load(s));
				}
				catch
				{
					var res = MessageBox.Show("Could not parse document as valid XML:\n\n" + s + "\n\nDo you wish to continue any other processing?", "Parsing Error", MessageBoxButtons.YesNo);
					if (res != DialogResult.Yes)
						return "";
				}				
			}

			int startIndex = 0;

			// actual tosec parsing
			foreach (var obj in xmls)
			{
				startIndex = Data.Count > 0 ? Data.Count - 1 : 0;
				// get header info
				var header = obj.Root.Descendants("header").First();
				var name = header.Element("name").Value;
				var version = header.Element("version").Value;
				var description = header.Element("description").Value + " - " + version;

				// start comment block
				List<string> comments = new List<string>
				{
					"Type:\tNO-INTRO",
					$"Source:\t{description}",
					$"FileGen:\t{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} (UTC)",
				};

				AddCommentBlock(comments.ToArray());

				// process each entry
				var query = obj.Root.Descendants("game");
				foreach (var g in query)
				{
					GameDB item = new GameDB();
					item.Name = g.Value;
					item.SHA1 = g.Elements("rom").First().Attribute("sha1").Value.ToUpper();
					item.MD5 = g.Elements("rom").First().Attribute("md5").Value.ToUpper();
					item.System = GameDB.GetSystemCode(SysType);

					ParseNOINTROFlags(item);

					Data.Add(item);
				}

				// add this file's data to the stringbuilder
				// first we will sort into various ROMSTATUS groups
				var working = Data.Skip(startIndex).ToList();

				var baddump = working.Where(st => st.Status == "B").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Bad Dumps");
				AppendCSVData(baddump);

				var hack = working.Where(st => st.Status == "H").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Hacks");
				AppendCSVData(hack);

				var over = working.Where(st => st.Status == "O").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Over Dumps");
				AppendCSVData(over);

				var trans = working.Where(st => st.Status == "T").OrderBy(na => na.Name).ToList();
				AddCommentBlock("Translated");
				AppendCSVData(trans);

				var good = working.Where(st => st.Status == "" || st.Status == null).OrderBy(na => na.Name).ToList();
				AddCommentBlock("Believed Good");
				AppendCSVData(good);
			}

			string result = sb.ToString();
			return sb.ToString();
		}

		/// <summary>
		/// Parses all the weird TOSEC flags within the game field
		/// Detailed info here: https://www.tosecdev.org/tosec-naming-convention
		/// Guts of this has been reused from here: https://github.com/Asnivor/MedLaunch/blob/master/MedLaunch/_Debug/DATDB/Platforms/TOSEC/StringConverterToSec.cs
		/// </summary>
		private void ParseNOINTROFlags(GameDB g)
		{
			string nameString = g.Name;

			// remove uninteresting options
			string a = RemoveUnneededOptions(nameString);

			// process data contained in ()
			string[] d = a.ToString().Split('(', ')');

			if (d.Length > 0)
			{
				// name field
			}

			if (d.Length > 1)
			{
				if (d[1].Length >= 3)
				{
					// country
					g.Region = d[1].Trim();
				}
			}

			if (d.Length > 2)
			{
				// parse all other () fields
				// because these are not mandatory this can be a confusing process
				for (int i = 4; i < d.Length; i++)
				{
					string f = d[i].Trim();

					// check for language
					if (IsLanguageFlag(f) == true)
					{
						g.Notes = f;
						continue;
					}

					// version - ignore

					// check development status (not currently implemented)
					if (IsDevelopmenttStatus(f) == true)
					{
						continue;
					}

					// check copyright status (not currently implemented)
					if (IsCopyrightStatus(f) == true)
					{
						continue;
					}

					// country flag(s)
					if (IsCountryFlag(f) == true)
					{
						g.Region = f;
						continue;
					}

					// language - if present add to notes
					if (IsLanguageFlag(f) == true)
					{
						g.Notes = f;
						continue;
					}

					// Media Type - ignore for now
					// Media Label - ignore for now
				}

				// process dump info flags and other info contained in []
				if (nameString.Contains("[") && nameString.Contains("]"))
				{
					var e = nameString.Split('[', ']')
						.Skip(1) // remove first entry (this is the bit before the [] entries start)
						.Where(s => !string.IsNullOrWhiteSpace(s)) // remove empty entries
						.Distinct()
						.ToList();

					if (e.Count > 0)
					{
						// bizhawk currently only has a few different RomStatus values (not as many as TOSEC anyway)
						// Parsing priority will be:
						//	RomStatus.BadDump
						//	RomStatus.Hack
						//	RomStatus.Overdump
						//	RomStatus.GoodDump
						//	RomStatus.TranslatedRom
						//	everything else
						// all tosec cr, h, t etc.. will fall under RomStatus.Hack

						if (e.Where(str => 
						// bad dump
						str == "b" || str.StartsWith("b ")).ToList().Count > 0)
						{
							// RomStatus.BadDump
							g.Status = "B";
						}							
						else if (e.Where(str => 
						// BIOS
						str == "BIOS" || str.StartsWith("BIOS ")).ToList().Count > 0)
						{
							// RomStatus.BIOS
							g.Status = "I";
						}
						else
						{
							g.Status = "";
						}
					}
				}
			}
		}

		public static bool IsDevelopmenttStatus(string s)
		{
			List<string> DS = new List<string>
			{
				"alpha", "beta", "preview", "pre-release", "proto"
			};

			bool b = DS.Any(s.Contains);
			return b;
		}

		public static bool IsCopyrightStatus(string s)
		{
			List<string> CS = new List<string>
			{
				"CW", "CW-R", "FW", "GW", "GW-R", "LW", "PD", "SW", "SW-R"
			};

			bool b = CS.Any(s.Contains);
			return b;
		}

		public static bool IsLanguageFlag(string s)
		{
			List<string> LC = new List<string>
			{
				"En", "Ja", "Fr", "De", "Es", "It", "Nl", "Pt", "Sv", "No", "Da", "Fi", "Zh", "Ko", "Pl"
			};

			bool b = false;

			if (!s.Contains("[") && !s.Contains("]"))
			{
				foreach (var x in LC)
				{
					if (s == x || s.StartsWith(x + ",") || s.EndsWith("," + x))
					{
						b = true;
						break;
					}
				}

				//b = LC.Any(s.Contains);
			}

			return b;
		}

		public static bool IsCountryFlag(string s)
		{
			List<string> CC = new List<string>
			{
				"World", "Australia", "Brazil", "Canada", "China", "France", "Germany", "Hong Kong", "Italy",
				"Japan", "Korea", "Netherlands", "Spain", "Sweden", "USA", "Europe", "Asia"
			};

			bool b = false;

			if (!s.Contains("[") && !s.Contains("]"))
			{
				foreach (var x in CC)
				{
					if (s == x || s.StartsWith(x) || s.EndsWith(x))
					{
						b = true;
						break;
					}
				}

				//b = CC.Any(s.Contains);
			}

			return b;
		}

		public static string RemoveUnneededOptions(string nameString)
		{
			// Remove unneeded entries
			string n = nameString
				.Replace(" (demo) ", " ")
				.Replace(" (demo-kiosk) ", " ")
				.Replace(" (demo-playable) ", " ")
				.Replace(" (demo-rolling) ", " ")
				.Replace(" (demo-slideshow) ", " ");

			return n;
		}
	}
}
