#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class MemoryManager;

	class Mapper
	{
	public:

		Mapper()
		{

		}

		uint32_t* 

		virtual uint8_t ReadMemory(uint32_t addr)
		{
			return 0;
		}

		virtual uint8_t PeekMemory(uint32_t addr)
		{
			return 0;
		}

		virtual void WriteMemory(uint32_t addr, uint8_t value)
		{
		}

		virtual void PokeMemory(uint32_t addr, uint8_t value)
		{
		}

		virtual uint8_t* SaveState(uint8_t* saver)
		{
			return nullptr;
		}

		virtual uint8_t* LoadState(uint8_t* loader)
		{
			return nullptr;
		}

		virtual void Dispose()
		{
		}

		virtual void Reset()
		{
		}

		virtual void Mapper_Tick()
		{
		}

		virtual void RTC_Get(int value, int index)
		{
		}
		/*
		virtual void MapCDL(uint32_t addr, LR35902.eCDLogMemFlags flags)
		{
		}

		protected void SetCDLROM(LR35902.eCDLogMemFlags flags, int cdladdr)
		{
			Core.SetCDL(flags, "ROM", cdladdr);
		}

		protected void SetCDLRAM(LR35902.eCDLogMemFlags flags, int cdladdr)
		{
			Core.SetCDL(flags, "CartRAM", cdladdr);
		}
		*/

	};
}
