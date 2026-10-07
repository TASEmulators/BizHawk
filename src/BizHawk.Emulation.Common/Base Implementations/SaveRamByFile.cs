using System.IO;

namespace BizHawk.Emulation.Common
{
	public class SaveRamByFile : ISaveRam
	{
		public bool SaveRamModified => false;

		public byte[]? CloneSaveRam(bool clearDirty = true) => File.ReadAllBytes(file);
		public void StoreSaveRam(byte[] data) => throw new NotImplementedException();

		public readonly string file;

		public SaveRamByFile(string file)
			=> this.file = file;
	}
}
