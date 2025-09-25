using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using BizHawk.Common.StringExtensions;

using LookupEntry = (string RegName, byte RegWidthBytes, byte ByteOffset);

namespace BizHawk.Emulation.Common
{
	public sealed class RegistersMemoryDomain : MemoryDomain
	{
		private const string ERR_FMT_STR_INVALID_ADDR = "invalid address, must be in 0..<{0}";

		public static ImmutableArray<LookupEntry> GenLookup(IDictionary<string, RegisterValue> regs)
		{
			List<LookupEntry> entries = new();
			foreach (var (name, val) in regs)
			{
				var widthBytes = unchecked((byte) (val.BitSize / 8));
				if (val.BitSize % 8 is not 0) widthBytes++;
				byte i = widthBytes;
				while (i > 0) entries.Add((RegName: name, RegWidthBytes: widthBytes, ByteOffset: --i));
			}
			while (entries.Count % sizeof(uint) is not 0) entries.Add((string.Empty, 0, 0)); // padding for Hex Editor
			return entries.ToImmutableArray();
		}

		private readonly IDebuggable _debuggableCore;

		private readonly ImmutableArray<LookupEntry> _lookup;

		public RegistersMemoryDomain(IDebuggable debuggableCore)
		{
			_debuggableCore = debuggableCore;
			var regs = _debuggableCore.GetCpuFlagsAndRegisters();
			_lookup = GenLookup(regs);
			EndianType = Endian.Big;
			var cpuName = regs.Keys.CommonPrefix();
			Name = cpuName.Length is 0 ? "CPU registers" : $"{new string(cpuName)/*trailing space*/}registers";
			Size = _lookup.Sum(static entry => entry.RegWidthBytes);
			WordSize = sizeof(byte);
			Writable = !debuggableCore.GetType().GetMethod("SetCpuRegister").CustomAttributes
				.Any(ad => ad.AttributeType == typeof(FeatureNotImplementedAttribute));
		}

		public override byte PeekByte(long addr)
		{
			if (addr < 0 || _lookup.Length <= addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_INVALID_ADDR, _lookup.Length));
			var (name, width, offset) = _lookup[unchecked((int) addr)];
			if (width is 0) return default; // padding for Hex Editor
			var toReturn = unchecked((uint) _debuggableCore.GetCpuFlagsAndRegisters()[name].Value);
			toReturn >>= 8 * offset;
			return unchecked((byte) toReturn);
		}

		public override void PokeByte(long addr, byte val)
		{
			if (addr < 0 || _lookup.Length <= addr) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: string.Format(ERR_FMT_STR_INVALID_ADDR, _lookup.Length));
			var (name, width, offset) = _lookup[unchecked((int) addr)];
			if (width is 0) return; // padding for Hex Editor
			switch (width)
			{
				case 8 or 7 or 6 or 5:
					throw new NotSupportedException($"{nameof(IDebuggable)}.{nameof(IDebuggable.SetCpuRegister)} does not support poking registers wider than 32 bits");
				case 4 or 3:
				{
					var toWrite = unchecked((uint) _debuggableCore.GetCpuFlagsAndRegisters()[name].Value);
					switch (offset)
					{
						case 3:
							toWrite &= 0x00FF_FFFFU;
							toWrite |= unchecked((ushort) (val << 24));
							break;
						case 2:
							toWrite &= 0xFF00_FFFFU;
							toWrite |= unchecked((ushort) (val << 16));
							break;
						case 1:
							toWrite &= 0xFFFF_00FFU;
							toWrite |= unchecked((ushort) (val << 8));
							break;
						default:
							toWrite &= 0xFFFF_FF00U;
							toWrite |= val;
							break;
					}
					_debuggableCore.SetCpuRegister(name, unchecked((int) toWrite));
					break;
				}
				case 2:
				{
					var toWrite = unchecked((ushort) _debuggableCore.GetCpuFlagsAndRegisters()[name].Value);
					if (offset is 1)
					{
						toWrite &= 0x00FF;
						toWrite |= unchecked((ushort) (val << 8));
					}
					else
					{
						toWrite &= 0xFF00;
						toWrite |= val;
					}
					_debuggableCore.SetCpuRegister(name, toWrite);
					break;
				}
				case 1:
					_debuggableCore.SetCpuRegister(name, val);
					break;
				default:
					throw new InvalidOperationException();
			}
		}
	}
}
