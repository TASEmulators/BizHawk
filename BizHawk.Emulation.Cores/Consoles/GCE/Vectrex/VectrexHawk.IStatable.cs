using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Vectrex
{
	public partial class VectrexHawk : IStatable
	{
		public bool BinarySaveStatesPreferred => true;

		public void SaveStateText(TextWriter writer)
		{
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(new Serializer(br));
		}

		public byte[] SaveStateBinary()
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		// here is where all your savestated stuff will be called 
		// make sure every variable you make is savestated
		// also all the components with their own savestate functions need to be called form here
		// for normal single variables the format is:
		// ser.Sync("var_name", ref var_name);

		// for arrays, use:
		// ser.Sync("var_name", ref var_name, false);
		private void SyncState(Serializer ser)
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}
			cpu.SyncState(ser);
			mapper.SyncState(ser);
			ppu.SyncState(ser);
			serialport.SyncState(ser);
			audio.SyncState(ser);

			ser.BeginSection("VIC20");

			ser.Sync("RAM", ref RAM, false);

			// probably a better way to do this
			if (cart_RAM != null)
			{
				ser.Sync("cart_RAM", ref cart_RAM, false);
			}

			ser.EndSection();
		}
	}
}
