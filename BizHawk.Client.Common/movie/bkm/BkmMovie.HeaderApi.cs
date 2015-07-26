using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public partial class BkmMovie
	{
		public IDictionary<string, string> HeaderEntries
		{
			get
			{
				return Header;
			}
		}
		
		public SubtitleList Subtitles
		{
			get { return Header.Subtitles; }
		}

		public IList<string> Comments
		{
			get { return Header.Comments; }
		}

		public string SyncSettingsJson
		{
			get { return Header[HeaderKeys.SYNCSETTINGS]; }
			set { Header[HeaderKeys.SYNCSETTINGS] = value; }
		}

		public ulong Rerecords
		{
			get { return Header.Rerecords; }
			set { Header.Rerecords = value; }
		}

		public bool StartsFromSavestate
		{
			get { return Header.StartsFromSavestate; }
			set { Header.StartsFromSavestate = value; }
		}

		// Bkm doesn't support saveram anchored movies
		public bool StartsFromSaveRam { get { return false; } set { } }

		public string GameName
		{
			get { return Header.GameName; }
			set { Header.GameName = value; }
		}

		public string SystemID
		{
			get { return Header.SystemID; }
			set { Header.SystemID = value; }
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

		public string TextSavestate { get; set; }
		public byte[] BinarySavestate { get; set; }
		public int[] SavestateFramebuffer { get { return null; } set { } } // eat and ignore framebuffers
		public byte[] SaveRam { get { return null; } set {  } } // Bkm does not support Saveram anchored movies
	}
}
