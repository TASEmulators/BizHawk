using System.Collections;
using System.Linq.Expressions;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// ZXHawk: Core Class
	/// * Misc Utilities *
	/// </summary>
	public partial class ZXSpectrum
	{
		/// <summary>
		/// Helper method that returns a single INT32 from a BitArray
		/// </summary>
		public static int GetIntFromBitArray(BitArray bitArray)
		{
			if (bitArray.Length > 32) throw new ArgumentException(message: "Argument length shall be at most 32 bits.", paramName: nameof(bitArray));
			int[] array = new int[1];
			bitArray.CopyTo(array, 0);
			return array[0];
		}

		/// <summary>
		/// POKEs a memory bus address
		/// </summary>
		public void PokeMemory(ushort addr, byte value)
		{
			_machine.WriteBus(addr, value);
		}

		/// <summary>
		/// Called by MainForm so that the core label can display a more detailed tooltip about the emulated spectrum model
		/// </summary>
		public string GetMachineType()
		{
			string m = "";
			switch (SyncSettings.MachineType)
			{
				case MachineType.ZXSpectrum16:
					m = "(Sinclair) ZX Spectrum 16K";
					break;
				case MachineType.ZXSpectrum48:
					m = "(Sinclair) ZX Spectrum 48K";
					break;
				case MachineType.ZXSpectrum128:
					m = "(Sinclair) ZX Spectrum 128K";
					break;
				case MachineType.ZXSpectrum128Plus2:
					m = "(Amstrad) ZX Spectrum 128K +2";
					break;
				case MachineType.ZXSpectrum128Plus2a:
					m = "(Amstrad) ZX Spectrum 128K +2a";
					break;
				case MachineType.ZXSpectrum128Plus3:
					m = "(Amstrad) ZX Spectrum 128K +3";
					break;
				case MachineType.Pentagon128:
					m = "(Clone) Pentagon 128K";
					break;
			}

			return m;
		}

		/// <summary>
		/// Called by MainForm - dumps a close approximation of the Spectaculator SZX snapshot format
		/// DEV use only - this is nowhere near accurate
		/// </summary>
		public byte[] GetSZXSnapshot()
		{
			return SZX.ExportSZX(_machine);
		}

		/// <summary>
		/// Utility method to get MemberName from an object
		/// </summary>
		public static string GetMemberName<T, TValue>(Expression<Func<T, TValue>> memberAccess)
		{
			return ((MemberExpression)memberAccess.Body).Member.Name;
		}
	}
}
