using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Libretro
{
	public class RetroDescription
	{
		/// <summary>
		/// String containing a friendly display name for the core, but we probably shouldn't use this. I decided it's better to get the user used to using filenames as core 'codenames' instead.
		/// </summary>
		public string LibraryName;

		/// <summary>
		/// String containing a friendly version number for the core library
		/// </summary>
		public string LibraryVersion;

		/// <summary>
		/// List of extensions as "sfc|smc|fig" which this core accepts.
		/// </summary>
		public string ValidExtensions;

		/// <summary>
		/// Whether the core needs roms to be specified as paths (can't take rom data buffersS)
		/// </summary>
		public bool NeedsRomAsPath;

		/// <summary>
		/// Whether the core needs roms stored as archives (e.g. arcade roms). We probably shouldn't employ the dearchiver prompts when opening roms for these cores.
		/// </summary>
		public bool NeedsArchives;

		/// <summary>
		/// Whether the core can be run without a game provided (e.g. stand-alone games, like 2048)
		/// </summary>
		public bool SupportsNoGame;

		/// <summary>
		/// Variables defined by the core
		/// </summary>
		public Dictionary<string, VariableDescription> Variables = new Dictionary<string, VariableDescription>();
	}

	public class VariableDescription
	{
		public string Name;
		public string Description;
		public string[] Options;
		public string DefaultOption => Options[0];

		public override string ToString() => $"{Name} ({Description}) = ({string.Join("|", Options)})";
	}
}