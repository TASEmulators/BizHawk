using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IMemoryApi : IExternalApi
	{
		string MainMemoryName { get; }

		void SetBigEndian(bool enabled = true);

		IReadOnlyCollection<string> GetMemoryDomainList();
		uint GetMemoryDomainSize(string name = "");
		string GetCurrentMemoryDomain();
		uint GetCurrentMemoryDomainSize();
		bool UseMemoryDomain(string domain);
		string HashRegion(long addr, int count, string domain = null);

		uint ReadByte(long addr, string domain = null);
		IReadOnlyList<byte> ReadByteRange(long addr, int length, string domain = null);
		float ReadFloat(long addr, string domain = null);

		int ReadS8(long addr, string domain = null);
		int ReadS16(long addr, string domain = null);
		int ReadS24(long addr, string domain = null);
		int ReadS32(long addr, string domain = null);

		uint ReadU8(long addr, string domain = null);
		uint ReadU16(long addr, string domain = null);
		uint ReadU24(long addr, string domain = null);
		uint ReadU32(long addr, string domain = null);

		void WriteByte(long addr, uint value, string domain = null);
		void WriteByteRange(long addr, IReadOnlyList<byte> memoryblock, string domain = null);
		void WriteFloat(long addr, float value, string domain = null);

		void WriteS8(long addr, int value, string domain = null);
		void WriteS16(long addr, int value, string domain = null);
		void WriteS24(long addr, int value, string domain = null);
		void WriteS32(long addr, int value, string domain = null);

		void WriteU8(long addr, uint value, string domain = null);
		void WriteU16(long addr, uint value, string domain = null);
		void WriteU24(long addr, uint value, string domain = null);
		void WriteU32(long addr, uint value, string domain = null);
	}
}
