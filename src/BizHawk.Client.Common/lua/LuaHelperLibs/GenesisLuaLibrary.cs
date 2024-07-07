using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace BizHawk.Client.Common
{
	[Description("Functions specific to GenesisHawk (functions may not run when an Genesis game is not loaded)")]
	public sealed class GenesisLuaLibrary : LuaLibraryBase
	{
		public GenesisLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "genesis";

		[RequiredService]
		private GPGX gpgx { get; set; }

		private GPGX.GPGXSettings Settings
		{
			get => APIs.Emulation.GetSettings() as GPGX.GPGXSettings ?? new GPGX.GPGXSettings();
			set => APIs.Emulation.PutSettings(value);
		}
	
		[LuaMethodExample("if ( genesis.getlayer_bga( ) ) then\r\n\tconsole.log( \"Returns whether the bg layer A is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_bga", "Returns whether the bg layer A is displayed")]
		public bool GetLayerBgA()
			=> Settings.DrawBGA;

		[LuaMethodExample("if ( genesis.getlayer_bgb( ) ) then\r\n\tconsole.log( \"Returns whether the bg layer B is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_bgb", "Returns whether the bg layer B is displayed")]
		public bool GetLayerBgB()
			=> Settings.DrawBGB;

		[LuaMethodExample("if ( genesis.getlayer_bgw( ) ) then\r\n\tconsole.log( \"Returns whether the bg layer W is displayed\" );\r\nend;")]
		[LuaMethod("getlayer_bgw", "Returns whether the bg layer W is displayed")]
		public bool GetLayerBgW()
			=> Settings.DrawBGW;

		[LuaMethodExample("genesis.setlayer_bga( true );")]
		[LuaMethod("setlayer_bga", "Sets whether the bg layer A is displayed")]
		public void SetLayerBgA(bool value)
		{
			var s = Settings;
			s.DrawBGA = value;
			Settings = s;
		}

		[LuaMethodExample("genesis.setlayer_bgb( true );")]
		[LuaMethod("setlayer_bgb", "Sets whether the bg layer B is displayed")]
		public void SetLayerBgB(bool value)
		{
			var s = Settings;
			s.DrawBGB = value;
			Settings = s;
		}

		[LuaMethodExample("genesis.setlayer_bgw( true );")]
		[LuaMethod("setlayer_bgw", "Sets whether the bg layer W is displayed")]
		public void SetLayerBgW(bool value)
		{
			var s = Settings;
			s.DrawBGW = value;
			Settings = s;
		}

		[LuaMethodExample("genesis.add_deepfreeze_value( 0xFF00, 0x01 );")]
		[LuaMethod("add_deepfreeze_value", "Adds an address to deepfreeze to a given value. The value will not change at any point during emulation.")]
		public int AddDeepFreezeValue(int address, byte value)
		{
			return gpgx.AddDeepFreezeValue(address, value);
		}

		[LuaMethodExample("genesis.clear_deepfreeze_list();")]
		[LuaMethod("clear_deepfreeze_list", "Clears the list of deep frozen variables")]
		public void ClearDeepFreezeList()
		{
			gpgx.ClearDeepFreezeList();
		}
	}
}
