using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public partial class BkmMovie : IMovie
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

		public string SavestateBinaryBase64Blob
		{
			get { return Header.SavestateBinaryBase64Blob; }
			set { Header.SavestateBinaryBase64Blob = value; }
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
	}
}
