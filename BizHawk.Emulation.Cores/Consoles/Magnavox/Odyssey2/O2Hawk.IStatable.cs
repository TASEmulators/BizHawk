using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.O2Hawk
{
	public partial class O2Hawk : IStatable
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

			ser.BeginSection("Odyssey2");
			ser.Sync(nameof(core), ref core, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			_controllerDeck.SyncState(ser);

			ser.Sync(nameof(controller_state), ref controller_state);
			ser.Sync(nameof(in_vblank), ref in_vblank);
			ser.Sync(nameof(in_vblank_old), ref in_vblank_old);	
			ser.Sync(nameof(vblank_rise), ref vblank_rise);
			ser.Sync(nameof(input_register), ref input_register);

			// memory domains
			ser.Sync(nameof(RAM), ref RAM, false);
			ser.Sync(nameof(VRAM), ref VRAM, false);
			ser.Sync(nameof(OAM), ref OAM, false);
			ser.Sync(nameof(_bios), ref _bios, false);
			ser.Sync(nameof(RAM_Bank), ref RAM_Bank);
			ser.Sync(nameof(addr_access), ref addr_access);

			ser.Sync(nameof(frame_buffer), ref frame_buffer, false);
			ser.Sync(nameof(_vidbuffer), ref _vidbuffer, false);

			// probably a better way to do this
			if (cart_RAM != null)
			{
				ser.Sync(nameof(cart_RAM), ref cart_RAM, false);
			}

			ser.EndSection();
		}
	}
}
