using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Waterbox
{
	/// <summary>
	/// a simple grow-only fixed max size heap
	/// </summary>
	internal sealed class Heap : IBinaryStateable, IDisposable
	{
		public MemoryBlock Memory { get; private set; }
		/// <summary>
		/// name, used in identifying errors
		/// </summary>
		public string Name { get; private set; }
		/// <summary>
		/// total number of bytes used
		/// </summary>
		public ulong Used { get; private set; }

		/// <summary>
		/// true if the heap has been sealed, preventing further changes
		/// </summary>
		public bool Sealed { get; private set; }

		private byte[] _hash;

		public Heap(ulong start, ulong size, string name)
		{
			Memory = MemoryBlock.PlatformConstructor(start, size);
			Used = 0;
			Name = name;
			Console.WriteLine("Created heap `{1}` at {0:x16}:{2:x16}", start, name, start + size);
		}

		private ulong EnsureAlignment(int align)
		{
			if (align > 1)
			{
				ulong newused = ((Used - 1) | (ulong)(align - 1)) + 1;
				if (newused > Memory.Size)
				{
					throw new InvalidOperationException(string.Format("Failed to meet alignment {0} on heap {1}", align, Name));
				}
				return newused;
			}
			return Used;
		}

		public ulong Allocate(ulong size, int align)
		{
			if (Sealed)
				throw new InvalidOperationException(string.Format("Attempt made to allocate from sealed heap {0}", Name));

			ulong allocstart = EnsureAlignment(align);
			ulong newused = allocstart + size;
			if (newused > Memory.Size)
			{
				throw new InvalidOperationException(string.Format("Failed to allocate {0} bytes from heap {1}", size, Name));
			}
			ulong ret = Memory.Start + allocstart;
			Memory.Protect(Memory.Start + Used, newused - Used, MemoryBlock.Protection.RW);
			Used = newused;
			Console.WriteLine($"Allocated {size} bytes on {Name}, utilization {Used}/{Memory.Size} ({100.0 * Used / Memory.Size:0.#}%)");
			return ret;
		}

		public void Seal()
		{
			if (!Sealed)
			{
				Memory.Protect(Memory.Start, Used, MemoryBlock.Protection.R);
				_hash = WaterboxUtils.Hash(Memory.GetStream(Memory.Start, Used, false));
				Sealed = true;
			}
			else
			{
				throw new InvalidOperationException(string.Format("Attempt to reseal heap {0}", Name));
			}
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			bw.Write(Name);
			bw.Write(Used);
			if (!Sealed)
			{
				bw.Write(Memory.XorHash);
				var ms = Memory.GetXorStream(Memory.Start, WaterboxUtils.AlignUp(Used), false);
				ms.CopyTo(bw.BaseStream);
			}
			else
			{
				bw.Write(_hash);
			}
		}

		public void LoadStateBinary(BinaryReader br)
		{
			var name = br.ReadString();
			if (name != Name)
				// probable cause: internal error
				throw new InvalidOperationException(string.Format("Name did not match for heap {0}", Name));
			var used = br.ReadUInt64();
			if (used > Memory.Size)
				throw new InvalidOperationException(string.Format("Heap {0} used {1} larger than available {2}", Name, used, Memory.Size));
			if (!Sealed)
			{
				var hash = br.ReadBytes(Memory.XorHash.Length);
				if (!hash.SequenceEqual(Memory.XorHash))
				{
					throw new InvalidOperationException(string.Format("Hash did not match for heap {0}.  Is this the same rom with the same SyncSettings?", Name));
				}
				var usedAligned = WaterboxUtils.AlignUp(used);

				Memory.Protect(Memory.Start, Memory.Size, MemoryBlock.Protection.None);
				Memory.Protect(Memory.Start, used, MemoryBlock.Protection.RW);
				var ms = Memory.GetXorStream(Memory.Start, usedAligned, true);
				WaterboxUtils.CopySome(br.BaseStream, ms, (long)usedAligned);
				Used = used;
			}
			else
			{
				var hash = br.ReadBytes(_hash.Length);
				if (!hash.SequenceEqual(_hash))
				{
					throw new InvalidOperationException(string.Format("Hash did not match for heap {0}.  Is this the same rom with the same SyncSettings?", Name));
				}
			}
		}

		public void Dispose()
		{
			if (Memory != null)
			{
				Memory.Dispose();
				Memory = null;
			}
		}
	}
}
