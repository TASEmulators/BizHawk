using System.Collections.Generic;
using System.Text;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie
	{
		protected readonly Bk2Header Header = new Bk2Header();
		private string _syncSettingsJson = "";

		public IDictionary<string, string> HeaderEntries => Header;

		public SubtitleList Subtitles { get; } = new SubtitleList();
		public IList<string> Comments { get; } = new List<string>();

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
				if (!Header.ContainsKey(HeaderKeys.Rerecords))
				{
					Header[HeaderKeys.Rerecords] = "0";
				}

				return ulong.Parse(Header[HeaderKeys.Rerecords]);
			}

			set
			{
				if (Header[HeaderKeys.Rerecords] != value.ToString())
				{
					Changes = true;
					Header[HeaderKeys.Rerecords] = value.ToString();
				}
			}
		}

		public bool StartsFromSavestate
		{
			get => Header.ContainsKey(HeaderKeys.StartsFromSavestate) && bool.Parse(Header[HeaderKeys.StartsFromSavestate]);
			set
			{
				if (value)
				{
					Header[HeaderKeys.StartsFromSavestate] = "True";
				}
				else
				{
					Header.Remove(HeaderKeys.StartsFromSavestate);
				}
			}
		}

		public bool StartsFromSaveRam
		{
			get => Header.ContainsKey(HeaderKeys.StartsFromSaveram) && bool.Parse(Header[HeaderKeys.StartsFromSaveram]);
			set
			{
				if (value)
				{
					if (!Header.ContainsKey(HeaderKeys.StartsFromSaveram))
					{
						Header.Add(HeaderKeys.StartsFromSaveram, "True");
					}
				}
				else
				{
					if (Header.ContainsKey(HeaderKeys.StartsFromSaveram))
					{
						Header.Remove(HeaderKeys.StartsFromSaveram);
					}
				}
			}
		}

		public string GameName
		{
			get => Header.ContainsKey(HeaderKeys.GameName) ? Header[HeaderKeys.GameName] : "";
			set
			{
				if (Header[HeaderKeys.GameName] != value)
				{
					Changes = true;
					Header[HeaderKeys.GameName] = value;
				}
			}
		}

		public string SystemID
		{
			get => Header.ContainsKey(HeaderKeys.Platform) ? Header[HeaderKeys.Platform] : "";
			set
			{
				if (Header[HeaderKeys.Platform] != value)
				{
					Changes = true;
					Header[HeaderKeys.Platform] = value;
				}
			}
		}

		public string Hash
		{
			get => Header[HeaderKeys.Sha1];
			set
			{
				if (Header[HeaderKeys.Sha1] != value)
				{
					Changes = true;
					Header[HeaderKeys.Sha1] = value;
				}
			}
		}

		public string Author
		{
			get => Header[HeaderKeys.Author];
			set
			{
				if (Header[HeaderKeys.Author] != value)
				{
					Changes = true;
					Header[HeaderKeys.Author] = value;
				}
			}
		}

		public string Core
		{
			get => Header[HeaderKeys.Core];
			set
			{
				if (Header[HeaderKeys.Core] != value)
				{
					Changes = true;
					Header[HeaderKeys.Core] = value;
				}
			}
		}

		public string BoardName
		{
			get => Header[HeaderKeys.BoardName];
			set
			{
				if (Header[HeaderKeys.BoardName] != value)
				{
					Changes = true;
					Header[HeaderKeys.BoardName] = value;
				}
			}
		}

		public string EmulatorVersion
		{
			get => Header[HeaderKeys.EmulationVersion];
			set
			{
				if (Header[HeaderKeys.EmulationVersion] != value)
				{
					Changes = true;
					Header[HeaderKeys.EmulationVersion] = value;
				}
			}
		}

		public string FirmwareHash
		{
			get => Header[HeaderKeys.FirmwareSha1];
			set
			{
				if (Header[HeaderKeys.FirmwareSha1] != value)
				{
					Changes = true;
					Header[HeaderKeys.FirmwareSha1] = value;
				}
			}
		}

		protected int? LoopOffset
		{
			get
			{
				var offsetStr = Header[HeaderKeys.LoopOffset];
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
					Header[HeaderKeys.LoopOffset] = value.ToString();
				}
				else
				{
					Header.Remove(HeaderKeys.LoopOffset);
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
