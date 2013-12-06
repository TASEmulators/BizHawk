using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public class MovieRecord : IMovieRecord
	{
		private MnemonicsGenerator _mg;
		private byte[] _state;

		public string Input
		{
			get
			{
				return _mg.GetControllersAsMnemonic();
			}
		}

		public bool Lagged { get; private set; }
		public IEnumerable<byte> State
		{
			get { return _state; }
		}

		public bool IsPressed(int player, string mnemonic)
		{
			return _mg[player, mnemonic];
		}

		public void SetInput(MnemonicsGenerator mg)
		{
			_mg = mg;
		}

		public void ClearInput()
		{
			_mg = new MnemonicsGenerator();
		}

		public MovieRecord(MnemonicsGenerator mg, bool captureState)
		{
			_mg = mg;
			if (captureState)
			{
				Lagged = Global.Emulator.IsLagFrame;
				_state = Global.Emulator.SaveStateBinary();
			}
		}

		public bool HasState
		{
			get
			{
				return State.Count() > 0;
			}
		}

		public override string ToString()
		{
			//TODO: consider the fileformat of binary and lagged data
			return Input;
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
