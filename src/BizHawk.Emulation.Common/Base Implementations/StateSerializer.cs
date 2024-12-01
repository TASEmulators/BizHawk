#nullable disable

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

		/// <summary>
		/// Instantiates a new instance of the <see cref="StateSerializer" /> class
		/// </summary>
		/// <param name="syncState">The callback that will be called on save and load methods </param>
		public StateSerializer(Action<Serializer> syncState)
		{
			_syncState = syncState;
		}

		/// <summary>
		/// If provided, will be called after a loadstate call
		/// </summary>
		public Action LoadStateCallback { get; set; }

		public bool AvoidRewind => false;

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
	}
}
