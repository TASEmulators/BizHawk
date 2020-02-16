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
		private readonly bool _bufferStates;
		private byte[] _stateBuffer;

		/// <summary>
		/// Instantiates a new instance of the <see cref="StateSerializer" /> class
		/// </summary>
		/// <param name="syncState">The callback that will be called on save and load methods </param>
		/// <param name="bufferStates">
		/// Whether or not to keep an allocated array for
		/// the byte array version of the SaveStateBinary method,
		/// should be true unless a core can have savestates of varying sizes per instance
		/// </param>
		public StateSerializer(Action<Serializer> syncState, bool bufferStates = true)
		{
			_bufferStates = bufferStates;
			_syncState = syncState;
		}

		/// <summary>
		/// If provided, will be called after a loadstate call
		/// </summary>
		public Action LoadStateCallback { get; set; }

		public void SaveStateText(TextWriter writer)
		{
			_syncState(Serializer.CreateTextWriter(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			_syncState(Serializer.CreateTextReader(reader));
			LoadStateCallback?.Invoke();
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			_syncState(Serializer.CreateBinaryWriter(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			_syncState(Serializer.CreateBinaryReader(br));
			LoadStateCallback?.Invoke();
		}

		public byte[] SaveStateBinary()
		{
			if (_bufferStates && _stateBuffer != null)
			{
				using var stream = new MemoryStream(_stateBuffer);
				using var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Flush();
				writer.Close();
				return _stateBuffer;
			}

			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			_stateBuffer = ms.ToArray();
			bw.Close();
			return _stateBuffer;
		}
	}
}
