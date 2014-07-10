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
		private readonly TasMovieMarkerList Markers = new TasMovieMarkerList();

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
			Markers.Clear();
			base.StartNewRecording();
		}

		public void Marker(int frame, string message)
		{
			Markers.Add(frame, message);
		}

		public void DeleteMarker(int frame)
		{
			Markers.Remove(frame);
		}

		private readonly Bk2MnemonicConstants Mnemonics = new Bk2MnemonicConstants();
		/// <summary>
		/// Returns the mnemonic value for boolean buttons, and actual value for floats,
		/// for a given frame and button
		/// </summary>
		public string DisplayValue(int frame, string buttonName)
		{
			var adapter = GetInputState(frame);

			if (adapter.Type.BoolButtons.Contains(buttonName))
			{
				return adapter.IsPressed(buttonName) ?
					Mnemonics[buttonName].ToString() :
					string.Empty;
			}

			if (adapter.Type.FloatControls.Contains(buttonName))
			{
				adapter.GetFloat(buttonName);
			}

			return "!";
		}
	}
}
