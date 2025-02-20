using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal static class BkmMnemonicConstants
	{
		public static readonly IReadOnlyDictionary<string, ControllerDefinition> ControllerDefinitions = new Dictionary<string, ControllerDefinition>
		{
			[VSystemID.Raw.GB] = new BkmControllerDefinition("Gameboy Controller")
			{
				BoolButtons = new[] { "Up", "Down", "Left", "Right", "Select", "Start", "B", "A" }.Select(b => $"P1 {b}")
					.Append("Power")
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.GBA] = new BkmControllerDefinition("GBA Controller")
			{
				BoolButtons = new[] { "Up", "Down", "Left", "Right", "Select", "Start", "B", "A", "L", "R" }.Select(b => $"P1 {b}")
					.Append("Power")
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.GEN] = new BkmControllerDefinition("GPGX Genesis Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "A", "B", "C", "Start", "X", "Y", "Z", "Mode" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Reset", "Power" ])
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.NES] = new BkmControllerDefinition("NES Controller")
			{
				BoolButtons = Enumerable.Range(1, 4)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "Select", "Start", "B", "A" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Reset", "Power", "FDS Eject", "FDS Insert 0", "FDS Insert 1", "FDS Insert 2", "FDS Insert 3", "VS Coin 1", "VS Coin 2" ])
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.SNES] = new BkmControllerDefinition("SNES Controller")
			{
				BoolButtons = Enumerable.Range(1, 4)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "Select", "Start", "B", "A", "X", "Y", "L", "R" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Reset", "Power" ])
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.PCE] = new BkmControllerDefinition("PC Engine Controller")
			{
				BoolButtons = Enumerable.Range(1, 5)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "Select", "Run", "B2", "B1" }
						.Select(b => $"P{i} {b}"))
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.SMS] = new BkmControllerDefinition("SMS Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "B1", "B2" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Pause", "Reset" ])
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.TI83] = new BkmControllerDefinition("TI83 Controller")
			{
				BoolButtons = new[] {
					"0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "DOT", "ON", "ENTER",
					"UP", "DOWN", "LEFT", "RIGHT", "PLUS", "MINUS", "MULTIPLY", "DIVIDE",
					"CLEAR", "EXP", "DASH", "PARAOPEN", "PARACLOSE", "TAN", "VARS", "COS",
					"PRGM", "STAT", "MATRIX", "X", "STO", "LN", "LOG", "SQUARED", "NEG1",
					"MATH", "ALPHA", "GRAPH", "TRACE", "ZOOM", "WINDOW", "Y", "2ND", "MODE"
				}.Select(b => $"P1 {b}")
				.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.A26] = new BkmControllerDefinition("Atari 2600 Basic Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "Button" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Reset", "Select" ])
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.A78] = new BkmControllerDefinition("Atari 7800 ProLine Joystick Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "Trigger", "Trigger 2" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Reset", "Power", "Select", "Pause" ])
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.C64] = new BkmControllerDefinition("Commodore 64 Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "Button" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Key F1", "Key F3", "Key F5", "Key F7", "Key Left Arrow", "Key 1",
						"Key 2", "Key 3", "Key 4", "Key 5", "Key 6", "Key 7", "Key 8", "Key 9", "Key 0", "Key Plus",
						"Key Minus", "Key Pound", "Key Clear/Home", "Key Insert/Delete", "Key Control", "Key Q", "Key W", "Key E",
						"Key R", "Key T", "Key Y", "Key U", "Key I", "Key O", "Key P", "Key At", "Key Asterisk", "Key Up Arrow",
						"Key Restore", "Key Run/Stop", "Key Lck", "Key A", "Key S", "Key D", "Key F", "Key G", "Key H", "Key J",
						"Key K", "Key L", "Key Colon", "Key Semicolon", "Key Equal", "Key Return", "Key Commodore", "Key Left Shift",
						"Key Z", "Key X", "Key C", "Key V", "Key B", "Key N", "Key M", "Key Comma", "Key Period",
						"Key Slash", "Key Right Shift", "Key Cursor Up/Down", "Key Cursor Left/Right", "Key Space"
					])
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.Coleco] = new BkmControllerDefinition("ColecoVision Basic Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "L", "R",
						"Key1", "Key2", "Key3", "Key4", "Key5", "Key6",
						"Key7", "Key8", "Key9", "Star", "Key0", "Pound"
					}.Select(b => $"P{i} {b}"))
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.N64] = new BkmControllerDefinition("Nintento 64 Controller")
			{
				BoolButtons = Enumerable.Range(1, 4)
					.SelectMany(i => new[] {
						"DPad U", "DPad D", "DPad L", "DPad R",
						"B", "A", "Z", "Start", "L", "R",
						"C Up", "C Down", "C Left", "C Right"
					}.Select(b => $"P{i} {b}"))
					.Concat([ "Reset", "Power" ])
					.ToArray()
			}.AddXYPair("P1 {0} Axis", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0)
			.AddXYPair("P2 {0} Axis", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0)
			.AddXYPair("P3 {0} Axis", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0)
			.AddXYPair("P4 {0} Axis", AxisPairOrientation.RightAndUp, (-128).RangeTo(127), 0)
			.MakeImmutable(),
			[VSystemID.Raw.SAT] = new BkmControllerDefinition("Saturn Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "Up", "Down", "Left", "Right", "Start", "X", "Y", "Z", "A", "B", "C", "L", "R" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Reset", "Power" ])
					.ToArray()
			}.MakeImmutable(),
			["DGB"] = new BkmControllerDefinition("Dual Gameboy Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "Power", "Up", "Down", "Left", "Right", "Select", "Start", "B", "A" }
						.Select(b => $"P{i} {b}"))
					.ToArray()
			}.MakeImmutable(),
			[VSystemID.Raw.WSWAN] = new BkmControllerDefinition("WonderSwan Controller")
			{
				BoolButtons = Enumerable.Range(1, 2)
					.SelectMany(i => new[] { "X1", "X3", "X4", "X2", "Y1", "Y3", "Y4", "Y2", "Start", "B", "A" }
						.Select(b => $"P{i} {b}"))
					.Concat([ "Power", "Rotate" ])
					.ToArray()
			}.MakeImmutable()
		};
	}
}
