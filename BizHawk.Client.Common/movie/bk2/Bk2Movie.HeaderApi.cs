using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected readonly Bk2Header Header = new Bk2Header();
		private string _syncSettingsJson = string.Empty;

		public IDictionary<string, string> HeaderEntries
		{
			get { return Header; }
		}

		public SubtitleList Subtitles { get; private set; }
		public IList<string> Comments { get; private set; }

		public string SyncSettingsJson
		{
			get { return _syncSettingsJson; }
			set { _syncSettingsJson = value; }
		}

		public ulong Rerecords
		{
			get
			{
				if (!Header.ContainsKey(HeaderKeys.RERECORDS))
				{
					Header[HeaderKeys.RERECORDS] = "0";
				}

				return ulong.Parse(Header[HeaderKeys.RERECORDS]);
			}

			set
			{
				Header[HeaderKeys.RERECORDS] = value.ToString();
			}
		}

		public bool StartsFromSavestate
		{
			get
			{
				if (Header.ContainsKey(HeaderKeys.STARTSFROMSAVESTATE))
				{
					return bool.Parse(Header[HeaderKeys.STARTSFROMSAVESTATE]);
				}

				return false;
			}

			set
			{
				if (value)
				{
					Header.Add(HeaderKeys.STARTSFROMSAVESTATE, "True");
				}
				else
				{
					Header.Remove(HeaderKeys.STARTSFROMSAVESTATE);
				}
			}
		}

		public string GameName
		{
			get
			{
				if (Header.ContainsKey(HeaderKeys.GAMENAME))
				{
					return Header[HeaderKeys.GAMENAME];
				}

				return string.Empty;
			}

			set
			{
				Header[HeaderKeys.GAMENAME] = value;
			}
		}

		public string SystemID
		{
			get
			{
				if (Header.ContainsKey(HeaderKeys.PLATFORM))
				{
					return Header[HeaderKeys.PLATFORM];
				}

				return string.Empty;
			}

			set
			{
				Header[HeaderKeys.PLATFORM] = value;
			}
		}

		public string Hash
		{
			get { return Header[HeaderKeys.SHA1]; }
			set { Header[HeaderKeys.SHA1] = value; }
		}

		public string Author
		{
			get { return Header[HeaderKeys.AUTHOR]; }
			set { Header[HeaderKeys.AUTHOR] = value; }
		}

		public string Core
		{
			get { return Header[HeaderKeys.CORE]; }
			set { Header[HeaderKeys.CORE] = value; }
		}

		public string Platform
		{
			get { return Header[HeaderKeys.PLATFORM]; }
			set { Header[HeaderKeys.PLATFORM] = value; }
		}

		public string BoardName
		{
			get { return Header[HeaderKeys.BOARDNAME]; }
			set { Header[HeaderKeys.BOARDNAME] = value; }
		}

		public string EmulatorVersion
		{
			get { return Header[HeaderKeys.EMULATIONVERSION]; }
			set { Header[HeaderKeys.EMULATIONVERSION] = value; }
		}

		public string FirmwareHash
		{
			get { return Header[HeaderKeys.FIRMWARESHA1]; }
			set { Header[HeaderKeys.FIRMWARESHA1] = value; }
		}

		private int? LoopOffset
		{
			get
			{
				var offsetStr = Header[HeaderKeys.LOOPOFFSET];
				if (!string.IsNullOrWhiteSpace(offsetStr))
				{
					return int.Parse(offsetStr);
				}

				return null;
			}

			set
			{
				if (value.HasValue)
				{
					Header[HeaderKeys.LOOPOFFSET] = value.ToString();
				}
				else
				{
					Header.Remove(HeaderKeys.LOOPOFFSET);
				}
			}
		}

		protected string CommentsString()
		{
			var sb = new StringBuilder();

			foreach(var comment in Comments)
			{
				sb.AppendLine(comment);
			}

			return sb.ToString();
		}

		public string TextSavestate { get; set; }
		public byte[] BinarySavestate { get; set; }
	}
}
