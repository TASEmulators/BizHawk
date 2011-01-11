using System;
using System.Collections.Generic;
using System.Text;

namespace MC68000
{
	public interface IMemoryController
	{
		sbyte ReadB(int address);
		short ReadW(int address);
		int ReadL(int address);

		void WriteB(int address, sbyte value);
		void WriteW(int address, short value);
		void WriteL(int address, int value);
	}
}
