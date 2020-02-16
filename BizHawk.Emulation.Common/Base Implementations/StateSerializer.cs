using System;
using System.IO;
using BizHawk.Common;
namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// A generic implementation of <see cref="IStatable" /> that also
	/// implements <see cref="ITextStatable" /> using the <see cref="Serializer" /> class
	/// </summary>
	public class StateSerializer : ITextStatable
	{
		private readonly Action<Serializer> _syncState;

		public StateSerializer(Action<Serializer> syncState)
		{
			_syncState = syncState;
		}

		public void SaveStateText(TextWriter writer)
		{
			_syncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			_syncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			_syncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			_syncState(new Serializer(br));
		}

		public byte[] SaveStateBinary()
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}
	}
}
