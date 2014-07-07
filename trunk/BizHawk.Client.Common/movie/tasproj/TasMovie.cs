using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public sealed partial class TasMovie : Bk2Movie
	{
		private readonly List<bool> LagLog = new List<bool>();

		public TasMovie(string path) : base(path) { }

		public TasMovie()
			: base()
		{
			Header[HeaderKeys.MOVIEVERSION] = "BizHawk v2.0 Tasproj v1.0"; 
		}

		public override string PreferredExtension
		{
			get { return Extension; }
		}

		public new const string Extension = "tasproj";

		public TasMovieRecord this[int index]
		{
			get
			{
				return new TasMovieRecord // TODO
				{
					State = null,
					LogEntry = "",
					Lagged = false
				};
			}
		}

		// _state = (byte[])Global.Emulator.SaveStateBinary().Clone();
	}
}
