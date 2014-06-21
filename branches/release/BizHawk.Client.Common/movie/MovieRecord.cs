using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Client.Common
{
	public class MovieRecord
	{
		private byte[] _state = new byte[0];

		public MovieRecord(string serializedInput, bool captureState)
		{
			SerializedInput = serializedInput;
			if (captureState)
			{
				CaptureSate();
			}
		}

		public string SerializedInput { get; private set; }
		public bool Lagged { get; private set; }

		public void ClearInput()
		{
			SerializedInput = string.Empty;
		}

		public void SetInput(string input)
		{
			SerializedInput = input ?? string.Empty;
		}

		public void CaptureSate()
		{
			Lagged = Global.Emulator.IsLagFrame;
			_state = (byte[])Global.Emulator.SaveStateBinary().Clone();
		}

		public IEnumerable<byte> State
		{
			get { return _state; }
		}

		public bool HasState
		{
			get { return State.Any(); }
		}

		public void ClearState()
		{
			_state = new byte[0];
		}
	}
}
