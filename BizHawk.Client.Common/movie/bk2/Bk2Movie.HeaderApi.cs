using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected readonly Bk2Header Header = new Bk2Header();
		private string _syncSettingsJson = "";

		public IDictionary<string, string> HeaderEntries => Header;

		public SubtitleList Subtitles { get; }
		public IList<string> Comments { get; }

		public string SyncSettingsJson
		{
			get => _syncSettingsJson;
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
					if (!Header.ContainsKey(HeaderKeys.STARTSFROMSAVERAM))
					{
						Header.Add(HeaderKeys.STARTSFROMSAVERAM, "True");
					}
				}
				else
				{
					if (Header.ContainsKey(HeaderKeys.STARTSFROMSAVERAM))
					{
						Header.Remove(HeaderKeys.STARTSFROMSAVERAM);
					}
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

				return "";
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

				return "";
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
			get => Header[HeaderKeys.SHA1];
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
			get => Header[HeaderKeys.AUTHOR];
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
			get => Header[HeaderKeys.CORE];
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
			get => Header[HeaderKeys.BOARDNAME];
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
			get => Header[HeaderKeys.EMULATIONVERSION];
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
			get => Header[HeaderKeys.FIRMWARESHA1];
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

			foreach (var comment in Comments)
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
