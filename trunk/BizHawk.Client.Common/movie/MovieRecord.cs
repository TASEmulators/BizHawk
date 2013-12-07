using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieRecord
	{
		// TODO: pass in ActivePlayers
		// TODO: pass in IController source, probably wasteful though

		private NewMnemonicsGenerator _mg;
		private Dictionary<string, bool> _boolButtons = new Dictionary<string, bool>();
		private byte[] _state;

		public MovieRecord()
		{
			_mg = new NewMnemonicsGenerator(Global.MovieOutputHardpoint);
		}

		public MovieRecord(IController source, bool captureState)
		{
			_mg = new NewMnemonicsGenerator(source);
			SetInput();
			if (captureState)
			{
				Lagged = Global.Emulator.IsLagFrame;
				_state = Global.Emulator.SaveStateBinary();
			}
		}

		public List<string> ActivePlayers
		{
			get
			{
				return _mg.ActivePlayers;
			}
			set
			{
				_mg.ActivePlayers = value;
			}
		}

		public string Input
		{
			get
			{
				return _mg.MnemonicString;
			}
		}

		public bool Lagged { get; private set; }
		public IEnumerable<byte> State
		{
			get { return _state; }
		}

		public bool IsPressed(string buttonName)
		{
			return _boolButtons[buttonName];
		}


		public void SetInput()
		{
			_boolButtons.Clear();
			_boolButtons = _mg.GetBoolButtons();
		}

		public void ClearInput()
		{
			_boolButtons.Clear();
		}

		public bool HasState
		{
			get { return State.Count() > 0; }
		}

		public override string ToString()
		{
			return Input; // TODO: consider the fileformat of binary and lagged data
		}
	}

	public class MovieRecordList : List<MovieRecord>
	{
		public MovieRecordList()
			: base()
		{

		}

		public override string ToString()
		{
			var sb = new StringBuilder();
			sb
				.AppendLine("[Input]")

				.Append("Frame ")
				.Append(Global.Emulator.Frame)
				.AppendLine();

			foreach (var record in this)
			{
				sb.AppendLine(record.ToString());
			}
			sb.AppendLine("[/Input]");
			return sb.ToString();
		}

		public void Truncate(int index)
		{
			if (index < Count)
			{
				RemoveRange(index, Count - index);
			}
		}
	}
}
