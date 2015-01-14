using System;
using System.ComponentModel;
using System.Linq;

using LuaInterface;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;

namespace BizHawk.Client.Common
{
	[Description("Functions related specifically to Nes Cores")]
	public sealed class NesLuaLibrary : LuaLibraryBase
	{
		// TODO:  
		// perhaps with the new core config system, one could
		// automatically bring out all of the settings to a lua table, with names.  that
		// would be completely arbitrary and would remove the whole requirement for this mess
		public NesLuaLibrary(Lua lua)
			: base(lua) { }

		[OptionalService]
		private NES _neshawk { get; set; }
		[OptionalService]
		private QuickNES _quicknes { get; set; }

		private bool NESAvailable { get { return _neshawk != null || _quicknes != null; } }

		public NesLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name { get { return "nes"; } }

		[LuaMethodAttributes(
			"addgamegenie",
			"Adds the specified game genie code. If an NES game is not currently loaded or the code is not a valid game genie code, this will have no effect"
		)]
		public void AddGameGenie(string code)
		{
			if (NESAvailable)
			{
				var decoder = new NESGameGenieDecoder(code);
				var watch = Watch.GenerateWatch(
					Global.Emulator.AsMemoryDomains()["System Bus"],
					decoder.Address,
					Watch.WatchSize.Byte,
					Watch.DisplayType.Hex,
					code,
					false);

				Global.CheatList.Add(new Cheat(
					watch,
					decoder.Value,
					decoder.Compare));
			}
		}

		[LuaMethodAttributes(
			"getallowmorethaneightsprites",
			"Gets the NES setting 'Allow more than 8 sprites per scanline' value"
		)]
		public bool GetAllowMoreThanEightSprites()
		{
			if (_quicknes != null)
			{
				return _quicknes.GetSettings().NumSprites != 8;
			}
			if (_neshawk != null)
			{
				return _neshawk.GetSettings().AllowMoreThanEightSprites;
			}

			throw new InvalidOperationException();
		}

		[LuaMethodAttributes(
			"getbottomscanline",
			"Gets the current value for the bottom scanline value"
		)]
		public int GetBottomScanline(bool pal = false)
		{
			if (_quicknes != null)
			{
				return _quicknes.GetSettings().ClipTopAndBottom ? 231 : 239;
			}

			if (_neshawk != null)
			{
				return pal
					? _neshawk.GetSettings().PAL_BottomLine
					: _neshawk.GetSettings().NTSC_BottomLine;
			}

			throw new InvalidOperationException();
		}

		[LuaMethodAttributes(
			"getclipleftandright",
			"Gets the current value for the Clip Left and Right sides option"
		)]
		public bool GetClipLeftAndRight()
		{
			if (_quicknes != null)
			{
				return _quicknes.GetSettings().ClipLeftAndRight;
			}

			if (_neshawk != null)
			{
				return _neshawk.GetSettings().ClipLeftAndRight;
			}

			throw new InvalidOperationException();
		}

		[LuaMethodAttributes(
			"getdispbackground",
			"Indicates whether or not the bg layer is being displayed"
		)]
		public bool GetDisplayBackground()
		{
			if (_quicknes != null)
			{
				return true;
			}

			if (_neshawk != null)
			{
				return _neshawk.GetSettings().DispBackground;
			}

			throw new InvalidOperationException();
		}

		[LuaMethodAttributes(
			"getdispsprites",
			"Indicates whether or not sprites are being displayed"
		)]
		public bool GetDisplaySprites()
		{
			if (_quicknes != null)
			{
				return _quicknes.GetSettings().NumSprites > 0;
			}

			if (_neshawk != null)
			{
				return _neshawk.GetSettings().DispSprites;
			}

			throw new InvalidOperationException();
		}

		[LuaMethodAttributes(
			"gettopscanline",
			"Gets the current value for the top scanline value"
		)]
		public int GetTopScanline(bool pal = false)
		{
			if (_quicknes != null)
			{
				return _quicknes.GetSettings().ClipTopAndBottom ? 8 : 0;
			}

			if (_neshawk != null)
			{
				return pal
					? _neshawk.GetSettings().PAL_TopLine
					: _neshawk.GetSettings().NTSC_TopLine;
			}

			throw new InvalidOperationException();
		}

		[LuaMethodAttributes(
			"removegamegenie",
			"Removes the specified game genie code. If an NES game is not currently loaded or the code is not a valid game genie code, this will have no effect"
		)]
		public void RemoveGameGenie(string code)
		{
			if (NESAvailable)
			{
				var decoder = new NESGameGenieDecoder(code);
				Global.CheatList.RemoveRange(
					Global.CheatList.Where(x => x.Address == decoder.Address));
			}
		}

		[LuaMethodAttributes(
			"setallowmorethaneightsprites",
			"Sets the NES setting 'Allow more than 8 sprites per scanline'"
		)]
		public void SetAllowMoreThanEightSprites(bool allow)
		{
			if (_neshawk != null)
			{
				var s = _neshawk.GetSettings();
				s.AllowMoreThanEightSprites = allow;
				_neshawk.PutSettings(s);
			}
			else if (_quicknes != null)
			{
				var s = _quicknes.GetSettings();
				s.NumSprites = allow ? 64 : 8;
				_quicknes.PutSettings(s);

			}
		}

		[LuaMethodAttributes(
			"setclipleftandright",
			"Sets the Clip Left and Right sides option"
		)]
		public void SetClipLeftAndRight(bool leftandright)
		{
			if (_neshawk != null)
			{
				var s = _neshawk.GetSettings();
				s.ClipLeftAndRight = leftandright;
				_neshawk.PutSettings(s);
			}
			else if (_quicknes != null)
			{
				var s = _quicknes.GetSettings();
				s.ClipLeftAndRight = leftandright;
				_quicknes.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setdispbackground",
			"Sets whether or not the background layer will be displayed"
		)]
		public void SetDisplayBackground(bool show)
		{
			if (_neshawk != null)
			{
				var s = _neshawk.GetSettings();
				s.DispBackground = show;
				_neshawk.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setdispsprites",
			"Sets whether or not sprites will be displayed"
		)]
		public void SetDisplaySprites(bool show)
		{
			if (_neshawk != null)
			{
				var s = _neshawk.GetSettings();
				s.DispSprites = show;
				_neshawk.PutSettings(s);
			}
			else if (_quicknes != null)
			{
				var s = _quicknes.GetSettings();
				s.NumSprites = show ? 8 : 0;
				_quicknes.PutSettings(s);
			}
		}

		[LuaMethodAttributes(
			"setscanlines",
			"sets the top and bottom scanlines to be drawn (same values as in the graphics options dialog). Top must be in the range of 0 to 127, bottom must be between 128 and 239. Not supported in the Quick Nes core"
		)]
		public void SetScanlines(int top, int bottom, bool pal = false)
		{
			if (_neshawk != null)
			{
				if (top > 127)
				{
					top = 127;
				}
				else if (top < 0)
				{
					top = 0;
				}

				if (bottom > 239)
				{
					bottom = 239;
				}
				else if (bottom < 128)
				{
					bottom = 128;
				}

				var s = _neshawk.GetSettings();

				if (pal)
				{
					s.PAL_TopLine = top;
					s.PAL_BottomLine = bottom;
				}
				else
				{
					s.NTSC_TopLine = top;
					s.NTSC_BottomLine = bottom;
				}

				_neshawk.PutSettings(s);
			}
		}
	}
}
