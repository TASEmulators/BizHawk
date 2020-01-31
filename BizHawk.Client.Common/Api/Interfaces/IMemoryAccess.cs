using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IMemoryAccess : IExternalApi
	{
		bool BigEndian { get; set; }

		string SelectedDomainName { get; }

		uint SelectedDomainSize { get; }

		IList<string> MemoryDomainList { get; }

		uint GetMemoryDomainSize(string name = null);

		string HashRegion(long addr, int count, string domain = null);

		uint ReadByte(long addr, string domain = null);

		IList<byte> ReadByteRange(long addr, int length, string domain = null);

		float ReadFloat(long addr, string domain = null);

		int ReadS16(long addr, string domain = null);

		int ReadS24(long addr, string domain = null);

		int ReadS32(long addr, string domain = null);

		int ReadS8(long addr, string domain = null);

		uint ReadU16(long addr, string domain = null);

		uint ReadU24(long addr, string domain = null);

		uint ReadU32(long addr, string domain = null);

		uint ReadU8(long addr, string domain = null);

		bool UseMemoryDomain(string domain);

		void WriteByte(long addr, uint value, string domain = null);

		void WriteByteRange(long addr, IList<byte> memoryblock, string domain = null);

		void WriteFloat(long addr, double value, string domain = null);

		void WriteS16(long addr, int value, string domain = null);

		void WriteS24(long addr, int value, string domain = null);

		void WriteS32(long addr, int value, string domain = null);

		void WriteS8(long addr, int value, string domain = null);

		void WriteU16(long addr, uint value, string domain = null);

		void WriteU24(long addr, uint value, string domain = null);

		void WriteU32(long addr, uint value, string domain = null);

		void WriteU8(long addr, uint value, string domain = null);
	}
}
