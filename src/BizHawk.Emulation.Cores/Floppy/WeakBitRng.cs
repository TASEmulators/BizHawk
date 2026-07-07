namespace BizHawk.Emulation.Cores.Floppy
{
	/// <summary>
	/// Deterministic PRNG used to resolve weak/fuzzy cells (copy protection) so repeated reads of a weak sector
	/// vary. Unlike <see cref="System.Random"/>, its whole state is a single value that can be serialized, so
	/// weak reads replay identically across savestate/TAS load (the FDC syncs <see cref="State"/>). splitmix64.
	/// </summary>
	public sealed class WeakBitRng
	{
		private const ulong Gamma = 0x9E37_79B9_7F4A_7C15UL;
		private ulong _state;

		public WeakBitRng(ulong seed = 0) => _state = seed;

		/// <summary>The full RNG state - serialize this for deterministic replay.</summary>
		public ulong State { get => _state; set => _state = value; }

		/// <summary>Returns a value in [0, maxExclusive). Mirrors the System.Random.Next(int) signature.</summary>
		public int Next(int maxExclusive)
		{
			_state += Gamma;
			ulong z = _state;
			z = (z ^ (z >> 30)) * 0xBF58_476D_1CE4_E5B9UL;
			z = (z ^ (z >> 27)) * 0x94D0_49BB_1331_11EBUL;
			z ^= z >> 31;
			return maxExclusive <= 1 ? 0 : (int)(z % (ulong)maxExclusive);
		}
	}
}
