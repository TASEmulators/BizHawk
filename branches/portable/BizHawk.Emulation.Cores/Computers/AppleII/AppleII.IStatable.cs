using BizHawk.Emulation.Common;
using System.IO;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return true; } }

		[FeatureNotImplemented]
		public void SaveStateText(TextWriter writer)
		{

		}

		[FeatureNotImplemented]
		public void LoadStateText(TextReader reader)
		{

		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_machine.SaveState(writer);
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			_machine.LoadState(reader);
		}

		public byte[] SaveStateBinary()
		{
			if (_stateBuffer == null)
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				_stateBuffer = stream.ToArray();
				writer.Close();
				return _stateBuffer;
			}
			else
			{
				var stream = new MemoryStream(_stateBuffer);
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Close();
				return _stateBuffer;
			}
		}

		private byte[] _stateBuffer;
	}
}
