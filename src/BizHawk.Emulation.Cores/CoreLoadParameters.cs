using System;
using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores
{
	public interface IRomAsset
	{
		byte[] RomData { get; }
		byte[] FileData { get; }
		string Extension { get; }
		/// <summary>
		/// GameInfo for this individual asset.  Doesn't make sense a lot of the time;
		/// only use this if your individual rom assets are full proper games when considered alone.
		/// Not guaranteed to be set in any other situation.
		/// </summary>
		GameInfo Game { get; }
	}
	public interface IDiscAsset
	{
		Disc DiscData { get; }
		DiscType DiscType { get; }
		public string DiscName { get; set; }
	}
	internal interface ICoreLoadParameters<in TSettiing, in TSync>
	{
		CoreComm Comm { set; }
		GameInfo Game { set; }
		List<IRomAsset> Roms { get; }
		bool DeterministicEmulationRequested { set; }
		void PutSettings(object settings, object syncSettings);
	}
	public class CoreLoadParameters<TSettiing, TSync> : ICoreLoadParameters<TSettiing, TSync>
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
		public List<IRomAsset> Roms { get; set; } = new List<IRomAsset>();
		/// <summary>
		/// All discs that should be loaded as part of this core load.
		/// Order may be significant.
		/// </summary>
		/// <value></value>
		public List<IDiscAsset> Discs { get; set; } = new List<IDiscAsset>();
		public bool DeterministicEmulationRequested { get; set; }
		void ICoreLoadParameters<TSettiing, TSync>.PutSettings(object settings, object syncSettings)
		{
			if (!(settings is TSettiing typedSettings)) throw new ArgumentException("type does not match type param", nameof(settings));
			if (!(syncSettings is TSync typedSyncSettings)) throw new ArgumentException("type does not match type param", nameof(syncSettings));
			Settings = typedSettings;
			SyncSettings = typedSyncSettings;
		}
	}
}
