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
		private List<bool> LagLog = new List<bool>();
		private readonly TasStateManager StateManager = new TasStateManager();

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
				return new TasMovieRecord
				{
					State = StateManager[index],
					LogEntry = GetInput(index),
					Lagged = (index < LagLog.Count) ? LagLog[index] : false
				};
			}
		}

		public override void StartNewRecording()
		{
			LagLog.Clear();
			StateManager.Clear();
			base.StartNewRecording();
		}
	}
}
