using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieRecord : IMovieRecord
	{
		Dictionary<string, bool> BoolButtons = new Dictionary<string, bool>();
		private byte[] _state;

		public string Input
		{
			get
			{
				SimpleController controller = new SimpleController { Type = new ControllerDefinition() };
				foreach (var kvp in BoolButtons)
				{
					controller["P1 " + kvp.Key] = kvp.Value; // TODO: multi-player, all cores
				}

				controller.Type.Name = Global.Emulator.ControllerDefinition.Name;
				MnemonicsGenerator mg = new MnemonicsGenerator();
				mg.SetSource(controller);
				return mg.GetControllersAsMnemonic();
			}
		}

		public bool Lagged { get; private set; }
		public IEnumerable<byte> State
		{
			get { return _state; }
		}

		public bool IsPressed(int player, string mnemonic)
		{
			return BoolButtons[mnemonic]; // TODO: player
		}


		public void SetInput(IController controller)
		{
			var mnemonics = MnemonicConstants.BUTTONS[Global.Emulator.Controller.Type.Name].Select(x => x.Value);
			foreach (var mnemonic in mnemonics)
			{
				BoolButtons[mnemonic] = controller["P1 " + mnemonic]; // TODO: doesn't work on every core, can't do multiplayer
			}
		}

		public void ClearInput()
		{
			foreach (var key in BoolButtons.Keys)
			{
				BoolButtons[key] = false;
			}
		}

		public MovieRecord(IController controller, bool captureState)
		{
			SetInput(controller);

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
