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
					Lagged = (index < LagLog.Count) ? LagLog[index] : (bool?)null
				};
			}
		}

		public void ClearChanges()
		{
			Changes = false;
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
				return adapter.GetFloat(buttonName).ToString();
			}

			return "!";
		}

		public void ToggleBoolState(int frame, string buttonName)
		{
			if (frame < _log.Count)
			{
				var adapter = GetInputState(frame) as Bk2ControllerAdapter;
				adapter[buttonName] = !adapter.IsPressed(buttonName);

				var lg = LogGeneratorInstance();
				lg.SetSource(adapter);
				_log[frame] = lg.GenerateLogEntry();
				Changes = true;
			}
		}

		public void SetBoolState(int frame, string buttonName, bool val)
		{
			if (frame < _log.Count)
			{
				var adapter = GetInputState(frame) as Bk2ControllerAdapter;
				var old = adapter[buttonName];
				adapter[buttonName] = val;

				var lg = LogGeneratorInstance();
				lg.SetSource(adapter);
				_log[frame] = lg.GenerateLogEntry();

				if (old != val)
				{
					Changes = true;
				}
			}
		}

		public void SetFloatState(int frame, string buttonName, float val)
		{
			if (frame < _log.Count)
			{
				var adapter = GetInputState(frame) as Bk2ControllerAdapter;
				var old = adapter.GetFloat(buttonName);
				adapter.SetFloat(buttonName, val);

				var lg = LogGeneratorInstance();
				lg.SetSource(adapter);
				_log[frame] = lg.GenerateLogEntry();

				if (old != val)
				{
					Changes = true;
				}
			}
		}

		public bool BoolIsPressed(int frame, string buttonName)
		{
			var adapter = GetInputState(frame) as Bk2ControllerAdapter;
			return adapter.IsPressed(buttonName);
		}

		public float GetFloatValue(int frame, string buttonName)
		{
			var adapter = GetInputState(frame) as Bk2ControllerAdapter;
			return adapter.GetFloat(buttonName);
		}

		public override string GetInput(int frame)
		{
			if (Global.Emulator.Frame == frame && !StateManager.HasState(frame))
			{
				StateManager.Capture();
			}

			return base.GetInput(frame);
		}

		public TasStateManager.ManagerSettings GreenzoneSettings
		{
			get { return StateManager.Settings;  }
		}
	}
}
