using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores
{
	public interface IRomGame
	{
		byte[] RomData { get; }
		byte[] FileData { get; }
		string Extension { get; }
	}
	public interface IDiscGame
	{
		Disc DiscData { get; }
		DiscType DiscType { get; }
		public string DiscName { get; set; }
	}
	public class CoreLoadParameters<TSettiing, TSync>
	{
		public CoreComm Comm { get; set; }
		public GameInfo Game { get; set; }
		/// <summary>
		/// Settings previously returned from the core.  May be null.
		/// </summary>
		public TSettiing Settings { get; set; }
		/// <summary>
		/// Sync Settings previously returned from the core.  May be null.
		/// </summary>
		public TSync SyncSettings { get; set; }
		/// <summary>
		/// All roms that should be loaded as part of this core load.
		/// Order may be significant.  Does not include firmwares or other general resources.
		/// </summary>
		public List<IRomGame> Roms { get; set; } = new List<IRomGame>();
		/// <summary>
		/// All discs that should be loaded as part of this core load.
		/// Order may be significant.
		/// </summary>
		/// <value></value>
		public List<IDiscGame> Discs { get; set; } = new List<IDiscGame>();
		public bool DeterministicEmulationRequested { get; set; }
	}
}
