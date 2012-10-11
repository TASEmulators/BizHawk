using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk
{
	public interface IEmulator : IDisposable
	{
		IVideoProvider VideoProvider { get; }
		/// <summary>
		/// sound provider for async operation.  this is optional, and is only required after StartAsyncSound() is called and returns true
		/// </summary>
		ISoundProvider SoundProvider { get; }
		/// <summary>
		/// sound provider for sync operation.  this is manditory
		/// </summary>
		ISyncSoundProvider SyncSoundProvider { get; }
		/// <summary>start async operation.  (on construct, sync operation is assumed).</summary>
		/// <returns>false if core doesn't support async sound; SyncSoundProvider will continue to be used in that case</returns>
		bool StartAsyncSound();
		/// <summary>
		/// end async operation, returning to sync operation.  after this, all sound requests will go to the SyncSoundProvider
		/// </summary>
		void EndAsyncSound();

		ControllerDefinition ControllerDefinition { get; }
		IController Controller { get; set; }

		// note that some? cores expect you to call SoundProvider.GetSamples() after each FrameAdvance()
		// please do this, even when rendersound = false
		void FrameAdvance(bool render, bool rendersound = true);

		int Frame { get; }
		int LagCount { get; set; }
		bool IsLagFrame { get; }
		string SystemId { get; }
		/// <summary>if you want to set this, look in the emulator's constructor or Load() method</summary>
		bool DeterministicEmulation { get; }

		/// <summary>
		/// return a copy of the saveram.  editing it won't do you any good unless you later call StoreSaveRam()
		/// </summary>
		byte[] ReadSaveRam();

		/// <summary>
		/// store new saveram to the emu core.  the data should be the same size as the return from ReadSaveRam()
		/// </summary>
		void StoreSaveRam(byte[] data);

		/// <summary>
		/// reset saveram to a standard initial state
		/// </summary>
		void ClearSaveRam();


		bool SaveRamModified { get; set; }

		void ResetFrameCounter();
		void SaveStateText(TextWriter writer);
		void LoadStateText(TextReader reader);
		void SaveStateBinary(BinaryWriter writer);
		void LoadStateBinary(BinaryReader reader);
		byte[] SaveStateBinary();

		// Arbitrary extensible core comm mechanism
		CoreInputComm CoreInputComm { get; set; }
		CoreOutputComm CoreOutputComm { get; }

		// ----- Client Debugging API stuff -----
		IList<MemoryDomain> MemoryDomains { get; }
		// this MUST BE the same as MemoryDomains[0], else DRAGONS
		MemoryDomain MainMemory { get; }
	}

	public class MemoryDomain
	{
		public readonly string Name;
		public readonly int Size;
		public readonly Endian Endian;

		public readonly Func<int, byte> PeekByte;
		public readonly Action<int, byte> PokeByte;
	
		public MemoryDomain(string name, int size, Endian endian, Func<int, byte> peekByte, Action<int, byte> pokeByte)
		{
			Name = name;
			Size = size;
			Endian = endian;
			PeekByte = peekByte;
			PokeByte = pokeByte;
		}

		public MemoryDomain()
		{
		}

		public MemoryDomain(MemoryDomain domain)
		{
			Name = domain.Name;
			Size = domain.Size;
			Endian = domain.Endian;
			PeekByte = domain.PeekByte;
			PokeByte = domain.PokeByte;
		}

		public override string ToString()
		{
			return Name;
		}
	}

	public enum Endian { Big, Little, Unknown }

	public enum DisplayType { NTSC, PAL }
}
