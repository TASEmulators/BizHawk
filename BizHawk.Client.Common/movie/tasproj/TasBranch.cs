using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Client.Common
{
	public class TasBranch
	{
		public int Frame { get; set; }
		public byte[] CoreData { get; set; }
		public List<string> InputLog { get; set; }
		public byte[] OSDFrameBuffer { get; set; }
	}

	public class TasBranchCollection : List<TasBranch>
	{
		private List<TasBranch> Branches = new List<TasBranch>();

		public void Save(BinaryWriter bw)
		{

		}

		public void Load(BinaryReader br, long length)
		{

		}
	}
}
