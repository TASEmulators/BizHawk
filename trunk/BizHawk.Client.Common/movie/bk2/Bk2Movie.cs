using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public partial class Bk2Movie : IMovie
	{
		private readonly PlatformFrameRates _frameRates = new PlatformFrameRates();
		private bool _makeBackup = true;

		public Bk2Movie(string filename, bool startsFromSavestate = false)
			: this(startsFromSavestate)
		{
			Subtitles = new SubtitleList();
			Comments = new List<string>();

			Rerecords = 0;
			Filename = filename;
		}

		public Bk2Movie(bool startsFromSavestate = false)
		{
			Filename = string.Empty;
			StartsFromSavestate = startsFromSavestate;
			
			IsCountingRerecords = true;
			_mode = Moviemode.Inactive;
			_makeBackup = true;
		}

		#region Implementation

		public string PreferredExtension { get { return "bk2"; } }
		public bool IsCountingRerecords { get; set; }

		public bool Changes { get; private set; }

		public double FrameCount
		{
			get { throw new NotImplementedException(); }
		}

		public double Fps
		{
			get { throw new NotImplementedException(); }
		}

		public TimeSpan Time
		{
			get { throw new NotImplementedException(); }
		}

		public int InputLogLength
		{
			get { throw new NotImplementedException(); }
		}

		public string Filename
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public void AppendFrame(IController source)
		{
			throw new NotImplementedException();
		}

		public void RecordFrame(int frame, IController source)
		{
			throw new NotImplementedException();
		}

		public void Truncate(int frame)
		{
			throw new NotImplementedException();
		}

		public string GetInput(int frame)
		{
			throw new NotImplementedException();
		}

		// Probably won't support
		public void PokeFrame(int frame, IController source)
		{
			throw new NotImplementedException();
		}

		public void ClearFrame(int frame)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
