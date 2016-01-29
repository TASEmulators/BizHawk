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
			set
			{
				if (_syncSettingsJson != value)
				{
					Changes = true;
					_syncSettingsJson = value;
				}
			}
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
				if (Header[HeaderKeys.RERECORDS] != value.ToString())
				{
					Changes = true;
					Header[HeaderKeys.RERECORDS] = value.ToString();
				}
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
					Header[HeaderKeys.STARTSFROMSAVESTATE] = "True";
				}
				else
				{
					Header.Remove(HeaderKeys.STARTSFROMSAVESTATE);
				}
			}
		}

		public bool StartsFromSaveRam
		{
			get
			{
				if (Header.ContainsKey(HeaderKeys.STARTSFROMSAVERAM))
				{
					return bool.Parse(Header[HeaderKeys.STARTSFROMSAVERAM]);
				}

				return false;
			}

			set
			{
				if (value)
				{
					Header.Add(HeaderKeys.STARTSFROMSAVERAM, "True");
				}
				else
				{
					Header.Remove(HeaderKeys.STARTSFROMSAVERAM);
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
				if (Header[HeaderKeys.GAMENAME] != value)
				{
					Changes = true;
					Header[HeaderKeys.GAMENAME] = value;
				}
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
				if (Header[HeaderKeys.PLATFORM] != value)
				{
					Changes = true;
					Header[HeaderKeys.PLATFORM] = value;
				}
			}
		}

		public string Hash
		{
			get { return Header[HeaderKeys.SHA1]; }
			set
			{
				if (Header[HeaderKeys.SHA1] != value)
				{
					Changes = true;
					Header[HeaderKeys.SHA1] = value;
				}
			}
		}

		public string Author
		{
			get { return Header[HeaderKeys.AUTHOR]; }
			set
			{
				if (Header[HeaderKeys.AUTHOR] != value)
				{
					Changes = true;
					Header[HeaderKeys.AUTHOR] = value;
				}
			}
		}

		public string Core
		{
			get { return Header[HeaderKeys.CORE]; }
			set
			{
				if (Header[HeaderKeys.CORE] != value)
				{
					Changes = true;
					Header[HeaderKeys.CORE] = value;
				}
			}
		}

		public string BoardName
		{
			get { return Header[HeaderKeys.BOARDNAME]; }
			set
			{
				if (Header[HeaderKeys.BOARDNAME] != value)
				{
					Changes = true;
					Header[HeaderKeys.BOARDNAME] = value;
				}
			}
		}

		public string EmulatorVersion
		{
			get { return Header[HeaderKeys.EMULATIONVERSION]; }
			set
			{
				if (Header[HeaderKeys.EMULATIONVERSION] != value)
				{
					Changes = true;
					Header[HeaderKeys.EMULATIONVERSION] = value;
				}
			}
		}

		public string FirmwareHash
		{
			get { return Header[HeaderKeys.FIRMWARESHA1]; }
			set
			{
				if (Header[HeaderKeys.FIRMWARESHA1] != value)
				{
					Changes = true;
					Header[HeaderKeys.FIRMWARESHA1] = value;
				}
			}
		}

		protected int? LoopOffset
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
		public int[] SavestateFramebuffer { get; set; }
		public byte[] SaveRam { get; set; }
	}
}
