#include <cstdio>
#include <time.h>
#include <cstdlib>
#include <cstring>
#include <algorithm>

#include <stdint.h>
#include <limits.h>
#include <math.h>

#define LSB_FIRST
#ifdef SPEEDHAX
#error NO SPEEDHAX
#endif
#define HAVE_HLE_BIOS

#include "port.h"

#include "instance.h"

#include "sound_blargg.h"

#include "constarrays.h"

#include "newstate.h"

#define INLINE

class Gigazoid
{

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// BEGIN MEMORY.CPP
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

/*============================================================
	FLASH
============================================================ */


#define FLASH_READ_ARRAY         0
#define FLASH_CMD_1              1
#define FLASH_CMD_2              2
#define FLASH_AUTOSELECT         3
#define FLASH_CMD_3              4
#define FLASH_CMD_4              5
#define FLASH_CMD_5              6
#define FLASH_ERASE_COMPLETE     7
#define FLASH_PROGRAM            8
#define FLASH_SETBANK            9

uint8_t flashSaveMemory[FLASH_128K_SZ];

int flashState; // = FLASH_READ_ARRAY;
int flashReadState; // = FLASH_READ_ARRAY;
int flashSize; // = 0x10000;
int flashDeviceID; // = 0x1b;
int flashManufacturerID; // = 0x32;
int flashBank; // = 0;

void flashInit (void)
{
	memset(flashSaveMemory, 0xff, sizeof(flashSaveMemory));
}

void flashReset()
{
	flashState = FLASH_READ_ARRAY;
	flashReadState = FLASH_READ_ARRAY;
	flashBank = 0;
}

uint8_t flashRead(uint32_t address)
{
	address &= 0xFFFF;

	switch(flashReadState) {
		case FLASH_READ_ARRAY:
			return flashSaveMemory[(flashBank << 16) + address];
		case FLASH_AUTOSELECT:
			switch(address & 0xFF)
			{
				case 0:
					// manufacturer ID
					return flashManufacturerID;
				case 1:
					// device ID
					return flashDeviceID;
			}
			break;
		case FLASH_ERASE_COMPLETE:
			flashState = FLASH_READ_ARRAY;
			flashReadState = FLASH_READ_ARRAY;
			return 0xFF;
	};
	return 0;
}

void flashSaveDecide(uint32_t address, uint8_t byte)
{
	if (address == 0x0e005555)
		cpuSaveGameFunc = &Gigazoid::flashWrite;
	else
		cpuSaveGameFunc = &Gigazoid::sramWrite;

	(this->*cpuSaveGameFunc)(address, byte);
}

void flashWrite(uint32_t address, uint8_t byte)
{
	address &= 0xFFFF;
	switch(flashState) {
		case FLASH_READ_ARRAY:
			if(address == 0x5555 && byte == 0xAA)
				flashState = FLASH_CMD_1;
			break;
		case FLASH_CMD_1:
			if(address == 0x2AAA && byte == 0x55)
				flashState = FLASH_CMD_2;
			else
				flashState = FLASH_READ_ARRAY;
			break;
		case FLASH_CMD_2:
			if(address == 0x5555) {
				if(byte == 0x90) {
					flashState = FLASH_AUTOSELECT;
					flashReadState = FLASH_AUTOSELECT;
				} else if(byte == 0x80) {
					flashState = FLASH_CMD_3;
				} else if(byte == 0xF0) {
					flashState = FLASH_READ_ARRAY;
					flashReadState = FLASH_READ_ARRAY;
				} else if(byte == 0xA0) {
					flashState = FLASH_PROGRAM;
				} else if(byte == 0xB0 && flashSize == 0x20000) {
					flashState = FLASH_SETBANK;
				} else {
					flashState = FLASH_READ_ARRAY;
					flashReadState = FLASH_READ_ARRAY;
				}
			} else {
				flashState = FLASH_READ_ARRAY;
				flashReadState = FLASH_READ_ARRAY;
			}
			break;
		case FLASH_CMD_3:
			if(address == 0x5555 && byte == 0xAA) {
				flashState = FLASH_CMD_4;
			} else {
				flashState = FLASH_READ_ARRAY;
				flashReadState = FLASH_READ_ARRAY;
			}
			break;
		case FLASH_CMD_4:
			if(address == 0x2AAA && byte == 0x55) {
				flashState = FLASH_CMD_5;
			} else {
				flashState = FLASH_READ_ARRAY;
				flashReadState = FLASH_READ_ARRAY;
			}
			break;
		case FLASH_CMD_5:
			if(byte == 0x30) {
				// SECTOR ERASE
				memset(&flashSaveMemory[(flashBank << 16) + (address & 0xF000)],
						0,
						0x1000);
				flashReadState = FLASH_ERASE_COMPLETE;
			} else if(byte == 0x10) {
				// CHIP ERASE
				memset(flashSaveMemory, 0, flashSize);
				flashReadState = FLASH_ERASE_COMPLETE;
			} else {
				flashState = FLASH_READ_ARRAY;
				flashReadState = FLASH_READ_ARRAY;
			}
			break;
		case FLASH_AUTOSELECT:
			if(byte == 0xF0) {
				flashState = FLASH_READ_ARRAY;
				flashReadState = FLASH_READ_ARRAY;
			} else if(address == 0x5555 && byte == 0xAA)
				flashState = FLASH_CMD_1;
			else {
				flashState = FLASH_READ_ARRAY;
				flashReadState = FLASH_READ_ARRAY;
			}
			break;
		case FLASH_PROGRAM:
			flashSaveMemory[(flashBank<<16)+address] = byte;
			flashState = FLASH_READ_ARRAY;
			flashReadState = FLASH_READ_ARRAY;
			break;
		case FLASH_SETBANK:
			if(address == 0) {
				flashBank = (byte & 1);
			}
			flashState = FLASH_READ_ARRAY;
			flashReadState = FLASH_READ_ARRAY;
			break;
	}
}

/*============================================================
	EEPROM
============================================================ */
int eepromMode; // = EEPROM_IDLE;
int eepromByte; // = 0;
int eepromBits; // = 0;
int eepromAddress; // = 0;

u8 eepromData[0x2000];

u8 eepromBuffer[16];
bool eepromInUse; // = false;
int eepromSize; // = 512;

void eepromInit (void)
{
	memset(eepromData, 255, sizeof(eepromData));
}

void eepromReset (void)
{
	eepromMode = EEPROM_IDLE;
	eepromByte = 0;
	eepromBits = 0;
	eepromAddress = 0;
	eepromInUse = false;
	eepromSize = 512;
}

int eepromRead (void)
{
	switch(eepromMode)
	{
		case EEPROM_IDLE:
		case EEPROM_READADDRESS:
		case EEPROM_WRITEDATA:
			return 1;
		case EEPROM_READDATA:
			{
				eepromBits++;
				if(eepromBits == 4) {
					eepromMode = EEPROM_READDATA2;
					eepromBits = 0;
					eepromByte = 0;
				}
				return 0;
			}
		case EEPROM_READDATA2:
			{
				int data = 0;
				int address = eepromAddress << 3;
				int mask = 1 << (7 - (eepromBits & 7));
				data = (eepromData[address+eepromByte] & mask) ? 1 : 0;
				eepromBits++;
				if((eepromBits & 7) == 0)
					eepromByte++;
				if(eepromBits == 0x40)
					eepromMode = EEPROM_IDLE;
				return data;
			}
		default:
			return 0;
	}
	return 1;
}

void eepromWrite(u8 value)
{
	if(cpuDmaCount == 0)
		return;
	int bit = value & 1;
	switch(eepromMode) {
		case EEPROM_IDLE:
			eepromByte = 0;
			eepromBits = 1;
			eepromBuffer[eepromByte] = bit;
			eepromMode = EEPROM_READADDRESS;
			break;
		case EEPROM_READADDRESS:
			eepromBuffer[eepromByte] <<= 1;
			eepromBuffer[eepromByte] |= bit;
			eepromBits++;
			if((eepromBits & 7) == 0) {
				eepromByte++;
			}
			if(cpuDmaCount == 0x11 || cpuDmaCount == 0x51) {
				if(eepromBits == 0x11) {
					eepromInUse = true;
					eepromSize = 0x2000;
					eepromAddress = ((eepromBuffer[0] & 0x3F) << 8) |
						((eepromBuffer[1] & 0xFF));
					if(!(eepromBuffer[0] & 0x40)) {
						eepromBuffer[0] = bit;
						eepromBits = 1;
						eepromByte = 0;
						eepromMode = EEPROM_WRITEDATA;
					} else {
						eepromMode = EEPROM_READDATA;
						eepromByte = 0;
						eepromBits = 0;
					}
				}
			} else {
				if(eepromBits == 9) {
					eepromInUse = true;
					eepromAddress = (eepromBuffer[0] & 0x3F);
					if(!(eepromBuffer[0] & 0x40)) {
						eepromBuffer[0] = bit;
						eepromBits = 1;
						eepromByte = 0;
						eepromMode = EEPROM_WRITEDATA;
					} else {
						eepromMode = EEPROM_READDATA;
						eepromByte = 0;
						eepromBits = 0;
					}
				}
			}
			break;
		case EEPROM_READDATA:
		case EEPROM_READDATA2:
			// should we reset here?
			eepromMode = EEPROM_IDLE;
			break;
		case EEPROM_WRITEDATA:
			eepromBuffer[eepromByte] <<= 1;
			eepromBuffer[eepromByte] |= bit;
			eepromBits++;
			if((eepromBits & 7) == 0)
				eepromByte++;
			if(eepromBits == 0x40)
			{
				eepromInUse = true;
				// write data;
				for(int i = 0; i < 8; i++)
					eepromData[(eepromAddress << 3) + i] = eepromBuffer[i];
			}
			else if(eepromBits == 0x41)
			{
				eepromMode = EEPROM_IDLE;
				eepromByte = 0;
				eepromBits = 0;
			}
			break;
	}
}

/*============================================================
	SRAM
============================================================ */

u8 sramRead(u32 address)
{
	return flashSaveMemory[address & 0xFFFF];
}

void sramWrite(u32 address, u8 byte)
{
	flashSaveMemory[address & 0xFFFF] = byte;
}

void dummyWrite(u32 address, u8 byte)
{
}

/*============================================================
	RTC
============================================================ */

#define IDLE		0
#define COMMAND		1
#define DATA		2
#define READDATA	3

typedef struct
{
	u8 byte0;
	u8 byte1;
	u8 byte2;
	u8 command;
	int dataLen;
	int bits;
	int state;
	u8 data[12];
} RTCCLOCKDATA;

RTCCLOCKDATA rtcClockData;
bool rtcEnabled; // = false;

u16 rtcRead(u32 address)
{
	switch(address)
	{
		case 0x80000c8:
			return rtcClockData.byte2;
		case 0x80000c6:
			return rtcClockData.byte1;
		case 0x80000c4:
			return rtcClockData.byte0;
		default:
			return 0;
	}
}

static u8 toBCD(u8 value)
{
	value = value % 100;
	int l = value % 10;
	int h = value / 10;
	return h * 16 + l;
}

bool rtcWrite(u32 address, u16 value)
{
	if(!rtcEnabled)
		return false;

	if(address == 0x80000c8)
		rtcClockData.byte2 = (u8)value; // enable ?
	else if(address == 0x80000c6)
		rtcClockData.byte1 = (u8)value; // read/write
	else if(address == 0x80000c4)
	{
		if(rtcClockData.byte2 & 1) // enable
		{
			if(rtcClockData.state == IDLE && rtcClockData.byte0 == 1 && value == 5)
			{
				rtcClockData.state = COMMAND;
				rtcClockData.bits = 0;
				rtcClockData.command = 0;
			}
			else if(!(rtcClockData.byte0 & 1) && (value & 1))
			{ // bit transfer
				rtcClockData.byte0 = (u8)value;
				switch(rtcClockData.state)
				{
					case COMMAND:
						rtcClockData.command |= ((value & 2) >> 1) << (7-rtcClockData.bits);
						rtcClockData.bits++;
						if(rtcClockData.bits == 8)
						{
							rtcClockData.bits = 0;
							switch(rtcClockData.command)
							{
								case 0x60:
									// not sure what this command does but it doesn't take parameters
									// maybe it is a reset or stop
									rtcClockData.state = IDLE;
									rtcClockData.bits = 0;
									break;
								case 0x62:
									// this sets the control state but not sure what those values are
									rtcClockData.state = READDATA;
									rtcClockData.dataLen = 1;
									break;
								case 0x63:
									rtcClockData.dataLen = 1;
									rtcClockData.data[0] = 0x40;
									rtcClockData.state = DATA;
									break;
								case 0x64:
									break;
								case 0x65:
									{
										tm newtime;
										GetTime(newtime);
										rtcClockData.dataLen = 7;
										rtcClockData.data[0] = toBCD(newtime.tm_year);
										rtcClockData.data[1] = toBCD(newtime.tm_mon+1);
										rtcClockData.data[2] = toBCD(newtime.tm_mday);
										rtcClockData.data[3] = toBCD(newtime.tm_wday);
										rtcClockData.data[4] = toBCD(newtime.tm_hour);
										rtcClockData.data[5] = toBCD(newtime.tm_min);
										rtcClockData.data[6] = toBCD(newtime.tm_sec);
										rtcClockData.state = DATA;
									}
									break;
								case 0x67:
									{
										tm newtime;
										GetTime(newtime);
										rtcClockData.dataLen = 3;
										rtcClockData.data[0] = toBCD(newtime.tm_hour);
										rtcClockData.data[1] = toBCD(newtime.tm_min);
										rtcClockData.data[2] = toBCD(newtime.tm_sec);
										rtcClockData.state = DATA;
									}
									break;
								default:
									//systemMessage(0, "Unknown RTC command %02x", rtcClockData.command);
									rtcClockData.state = IDLE;
									break;
							}
						}
						break;
					case DATA:
						if(rtcClockData.byte1 & 2)
						{
						}
						else
						{
							rtcClockData.byte0 = (rtcClockData.byte0 & ~2) |
								((rtcClockData.data[rtcClockData.bits >> 3] >>
								  (rtcClockData.bits & 7)) & 1)*2;
							rtcClockData.bits++;
							if(rtcClockData.bits == 8*rtcClockData.dataLen)
							{
								rtcClockData.bits = 0;
								rtcClockData.state = IDLE;
							}
						}
						break;
					case READDATA:
						if(!(rtcClockData.byte1 & 2)) {
						} else {
							rtcClockData.data[rtcClockData.bits >> 3] =
								(rtcClockData.data[rtcClockData.bits >> 3] >> 1) |
								((value << 6) & 128);
							rtcClockData.bits++;
							if(rtcClockData.bits == 8*rtcClockData.dataLen) {
								rtcClockData.bits = 0;
								rtcClockData.state = IDLE;
							}
						}
						break;
					default:
						break;
				}
			} else
				rtcClockData.byte0 = (u8)value;
		}
	}
	return true;
}

void rtcReset (void)
{
	memset(&rtcClockData, 0, sizeof(rtcClockData));

	rtcClockData.byte0 = 0;
	rtcClockData.byte1 = 0;
	rtcClockData.byte2 = 0;
	rtcClockData.command = 0;
	rtcClockData.dataLen = 0;
	rtcClockData.bits = 0;
	rtcClockData.state = IDLE;
}

// guarantees predictable results regardless of stdlib
// could be modified later to better match internal quirks of
// the RTC chip actually used
struct
{
	int year; // 00..99
	int month; // 00..11
	int mday; // 01..31
	int wday; // 00..06
	int hour; // 00..23
	int min; // 00..59
	int sec; // 00..59

	template<bool isReader>void SyncState(NewState *ns)
	{
		NSS(year);
		NSS(month);
		NSS(mday);
		NSS(wday);
		NSS(hour);
		NSS(min);
		NSS(sec);
	}

private:
	int DaysInMonth()
	{
		// gba rtc doesn't understand 100/400 exceptions
		int result = daysinmonth[month];
		if (month == 1 && year % 4 == 0)
			result++;
		return result;
	}

public:
	void Increment()
	{
		sec++;
		if (sec >= 60)
		{
			sec = 0;
			min++;
			if (min >= 60)
			{
				min = 0;
				hour++;
				if (hour >= 24)
				{
					hour = 0;
					wday++;
					if (wday >= 7)
						wday = 0;
					mday++;
					if (mday >= DaysInMonth())
					{
						mday = 1;
						month++;
						if (month >= 12)
						{
							month = 0;
							year++;
							if (year >= 100)
								year = 0;
						}
					}
				}
			}
		}
	}

} rtcInternalTime;

void GetTime(tm &times)
{
	if (RTCUseRealTime)
	{
		time_t t = time(nullptr);
		#if defined _MSC_VER
		gmtime_s(&times, &t);
		#elif defined __MINGW32__
		tm *tmp = gmtime(&t);
		times = *tmp;		
		#elif defined __GNUC__
		gmtime_r(&t, &times);
		#endif
	}
	else
	{
		times.tm_hour = rtcInternalTime.hour;
		times.tm_mday = rtcInternalTime.mday;
		times.tm_min = rtcInternalTime.min;
		times.tm_mon = rtcInternalTime.month;
		times.tm_sec = rtcInternalTime.sec;
		times.tm_wday = rtcInternalTime.wday;
		times.tm_year = rtcInternalTime.year;
	}
}

int RTCTicks;
bool RTCUseRealTime;

void AdvanceRTC(int ticks)
{
	RTCTicks += ticks;
	while (RTCTicks >= 16777216)
	{
		RTCTicks -= 16777216;
		rtcInternalTime.Increment();
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// END MEMORY.CPP
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// BEGIN SOUND.CPP
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#define NR10 0x60
#define NR11 0x62
#define NR12 0x63
#define NR13 0x64
#define NR14 0x65
#define NR21 0x68
#define NR22 0x69
#define NR23 0x6c
#define NR24 0x6d
#define NR30 0x70
#define NR31 0x72
#define NR32 0x73
#define NR33 0x74
#define NR34 0x75
#define NR41 0x78
#define NR42 0x79
#define NR43 0x7c
#define NR44 0x7d
#define NR50 0x80
#define NR51 0x81
#define NR52 0x84

/* 1/100th of a second */
//#define SOUND_CLOCK_TICKS_ 167772 
#define SOUNDVOLUME 0.5f
#define SOUNDVOLUME_ -1

/*============================================================
	CLASS DECLS
============================================================ */

class Blip_Buffer
{
	public:
	template<bool isReader>void SyncState(NewState *ns)
	{
		NSS(clock_rate_);
		NSS(length_);
		NSS(sample_rate_);
		NSS(factor_);
		NSS(offset_);
		// int32_t *buffer_; shouldn't need to save
		NSS(buffer_size_);
		NSS(reader_accum_);

	}

		Blip_Buffer::Blip_Buffer()
		{
		   factor_       = INT_MAX;
		   buffer_       = 0;
		   buffer_size_  = 0;
		   sample_rate_  = 0;
		   clock_rate_   = 0;
		   length_       = 0;

		   clear();
		}

		Blip_Buffer::~Blip_Buffer()
		{
			free(buffer_);
		}

		void Blip_Buffer::clear( void)
		{
		   offset_       = 0;
		   reader_accum_ = 0;
		   if (buffer_)
			  memset( buffer_, 0, (buffer_size_ + BLIP_BUFFER_EXTRA_) * sizeof (int32_t) );
		}

		const char * Blip_Buffer::set_sample_rate( long new_rate, int msec )
		{
		   /* start with maximum length that resampled time can represent*/
		   long new_size = (ULONG_MAX >> BLIP_BUFFER_ACCURACY) - BLIP_BUFFER_EXTRA_ - 64;
		   if ( msec != 0)
		   {
			  long s = (new_rate * (msec + 1) + 999) / 1000;
			  if ( s < new_size )
				 new_size = s;
		   }

		   if ( buffer_size_ != new_size )
		   {
			  void* p = realloc( buffer_, (new_size + BLIP_BUFFER_EXTRA_) * sizeof *buffer_ );
			  if ( !p )
				 return "Out of memory";
			  buffer_ = (int32_t *) p;
		   }

		   buffer_size_ = new_size;

		   /* update things based on the sample rate*/
		   sample_rate_ = new_rate;
		   length_ = new_size * 1000 / new_rate - 1;

		   /* update these since they depend on sample rate*/
		   if ( clock_rate_ )
			  factor_ = clock_rate_factor( clock_rate_);

		   clear();

		   return 0;
		}

		/* Sets number of source time units per second */

		uint32_t Blip_Buffer::clock_rate_factor( long rate ) const
		{
		   double ratio = (double) sample_rate_ / rate;
		   int32_t factor = (int32_t) floor( ratio * (1L << BLIP_BUFFER_ACCURACY) + 0.5 );
		   return (uint32_t) factor;
		}
		long clock_rate_;
		int length_;		/* Length of buffer in milliseconds*/
		long sample_rate_;	/* Current output sample rate*/
		uint32_t factor_;
		uint32_t offset_;
		int32_t *buffer_;
		int32_t buffer_size_;
		int32_t reader_accum_;
	private:
		Blip_Buffer( const Blip_Buffer& );
		Blip_Buffer& operator = ( const Blip_Buffer& );
};

class Blip_Synth
{
	public:
	int delta_factor;

	template<bool isReader>void SyncState(NewState *ns)
	{
		NSS(delta_factor);
	}

	void volume( double v ) { delta_factor = int ((v * 1.0) * (1L << BLIP_SAMPLE_BITS) + 0.5); }
	INLINE void Blip_Synth::offset_resampled( uint32_t time, int delta, Blip_Buffer* blip_buf ) const
	{
		int32_t left, right, phase;
		int32_t *buf;

		delta *= delta_factor;
		buf = blip_buf->buffer_ + (time >> BLIP_BUFFER_ACCURACY);
		phase = (int) (time >> (BLIP_BUFFER_ACCURACY - BLIP_PHASE_BITS) & BLIP_RES_MIN_ONE);

		left = buf [0] + delta;

		right = (delta >> BLIP_PHASE_BITS) * phase;

		left  -= right;
		right += buf [1];

		buf [0] = left;
		buf [1] = right;
	}

	INLINE void Blip_Synth::offset( int32_t t, int delta, Blip_Buffer* buf ) const
	{
			offset_resampled( t * buf->factor_ + buf->offset_, delta, buf );
	}
	void offset_inline( int32_t t, int delta, Blip_Buffer* buf ) const {
		offset_resampled( t * buf->factor_ + buf->offset_, delta, buf );
	}
};

#define TRIGGER_MASK 0x80
#define LENGTH_ENABLED 0x40

#define VOLUME_SHIFT_PLUS_FOUR	6
#define SIZE20_MASK 0x20



#define reload_sweep_timer() \
        sweep_delay = (regs [0] & PERIOD_MASK) >> 4; \
        if ( !sweep_delay ) \
                sweep_delay = 8;

class Gb_Osc
{
	public:
	Blip_Buffer* outputs [4];	/* NULL, right, left, center*/
	Blip_Buffer* output;		/* where to output sound*/
	uint8_t * regs;			/* osc's 5 registers*/
	int mode;			/* mode_dmg, mode_cgb, mode_agb*/
	int dac_off_amp;		/* amplitude when DAC is off*/
	int last_amp;			/* current amplitude in Blip_Buffer*/
	Blip_Synth const* good_synth;
	Blip_Synth  const* med_synth;

	int delay;			/* clocks until frequency timer expires*/
	int length_ctr;			/* length counter*/
	unsigned phase;			/* waveform phase (or equivalent)*/
	bool enabled;			/* internal enabled flag*/

	template<bool isReader>void SyncState(NewState *ns)
	{
		EBS(output, -1);
		EVS(output, outputs[0], 0);
		EVS(output, outputs[1], 1);
		EVS(output, outputs[2], 2);
		EVS(output, outputs[3], 3);
		EES(output, nullptr);

		NSS(mode);
		NSS(dac_off_amp);
		NSS(last_amp);

		NSS(delay);
		NSS(length_ctr);
		NSS(phase);
		NSS(enabled);
	}

	void Gb_Osc::clock_length()
	{
			if ( (regs [4] & LENGTH_ENABLED) && length_ctr )
			{
					if ( --length_ctr <= 0 )
							enabled = false;
			}
	}
	void Gb_Osc::reset()
	{
			output   = 0;
			last_amp = 0;
			delay    = 0;
			phase    = 0;
			enabled  = false;
	}
	protected:
	INLINE void Gb_Osc::update_amp( int32_t time, int new_amp )
	{
		int delta = new_amp - last_amp;
			if ( delta )
			{
					last_amp = new_amp;
					med_synth->offset( time, delta, output );
			}
	}
	int Gb_Osc::write_trig( int frame_phase, int max_len, int old_data )
	{
			int data = regs [4];

			if ( (frame_phase & 1) && !(old_data & LENGTH_ENABLED) && length_ctr )
			{
					if ( (data & LENGTH_ENABLED))
							length_ctr--;
			}

			if ( data & TRIGGER_MASK )
			{
					enabled = true;
					if ( !length_ctr )
					{
							length_ctr = max_len;
							if ( (frame_phase & 1) && (data & LENGTH_ENABLED) )
									length_ctr--;
					}
			}

			if ( !length_ctr )
					enabled = false;

			return data & TRIGGER_MASK;
	}
};

class Gb_Env : public Gb_Osc
{
	public:
	int  env_delay;
	int  volume;
	bool env_enabled;

	template<bool isReader>void SyncState(NewState *ns)
	{
		Gb_Osc::SyncState<isReader>(ns);
		NSS(env_delay);
		NSS(volume);
		NSS(env_enabled);
	}

	void Gb_Env::clock_envelope()
	{
			if ( env_enabled && --env_delay <= 0 && reload_env_timer() )
			{
					int v = volume + (regs [2] & 0x08 ? +1 : -1);
					if ( 0 <= v && v <= 15 )
							volume = v;
					else
							env_enabled = false;
			}
	}
	bool Gb_Env::write_register( int frame_phase, int reg, int old, int data )
	{
			int const max_len = 64;

			switch ( reg )
		{
			case 1:
				length_ctr = max_len - (data & (max_len - 1));
				break;

			case 2:
				if ( !GB_ENV_DAC_ENABLED() )
					enabled = false;

				zombie_volume( old, data );

				if ( (data & 7) && env_delay == 8 )
				{
					env_delay = 1;
					clock_envelope(); // TODO: really happens at next length clock
				}
				break;

			case 4:
				if ( write_trig( frame_phase, max_len, old ) )
				{
					volume = regs [2] >> 4;
					reload_env_timer();
					env_enabled = true;
					if ( frame_phase == 7 )
						env_delay++;
					if ( !GB_ENV_DAC_ENABLED() )
						enabled = false;
					return true;
				}
		}
			return false;
	}

	void reset()
	{
		env_delay = 0;
		volume    = 0;
		Gb_Osc::reset();
	}
	private:
	INLINE void Gb_Env::zombie_volume( int old, int data )
	{
		int v = volume;

		// CGB-05 behavior, very close to AGB behavior as well
		if ( (old ^ data) & 8 )
		{
			if ( !(old & 8) )
			{
				v++;
				if ( old & 7 )
					v++;
			}

			v = 16 - v;
		}
		else if ( (old & 0x0F) == 8 )
			v++;
		volume = v & 0x0F;
	}
	INLINE int Gb_Env::reload_env_timer()
	{
			int raw = regs [2] & 7;
			env_delay = (raw ? raw : 8);
			return raw;
	}
};

class Gb_Square : public Gb_Env
{
public:
	template<bool isReader>void SyncState(NewState *ns)
	{
		Gb_Env::SyncState<isReader>(ns);
	}

	bool Gb_Square::write_register( int frame_phase, int reg, int old_data, int data )
	{
			bool result = Gb_Env::write_register( frame_phase, reg, old_data, data );
			if ( result )
					delay = (delay & (CLK_MUL_MUL_4 - 1)) + period();
			return result;
	}
	void Gb_Square::run( int32_t time, int32_t end_time )
	{
			/* Calc duty and phase*/
			static unsigned char const duty_offsets [4] = { 1, 1, 3, 7 };
			static unsigned char const duties       [4] = { 1, 2, 4, 6 };
			int const duty_code = regs [1] >> 6;
			int32_t duty_offset = duty_offsets [duty_code];
			int32_t duty = duties [duty_code];
		/* AGB uses inverted duty*/
		duty_offset -= duty;
		duty = 8 - duty;
			int ph = (phase + duty_offset) & 7;

			/* Determine what will be generated*/
			int vol = 0;
			Blip_Buffer* const out = output;
			if ( out )
			{
					int amp = dac_off_amp;
					if ( GB_ENV_DAC_ENABLED() )
					{
							if ( enabled )
									vol = volume;

				amp = -(vol >> 1);

							/* Play inaudible frequencies as constant amplitude*/
							if ( GB_OSC_FREQUENCY() >= 0x7FA && delay < CLK_MUL_MUL_32 )
							{
									amp += (vol * duty) >> 3;
									vol = 0;
							}

							if ( ph < duty )
							{
									amp += vol;
									vol = -vol;
							}
					}
					update_amp( time, amp );
			}

			/* Generate wave*/
			time += delay;
			if ( time < end_time )
			{
					int const per = period();
					if ( !vol )
					{
							/* Maintain phase when not playing*/
							int count = (end_time - time + per - 1) / per;
							ph += count; /* will be masked below*/
							time += (int32_t) count * per;
					}
					else
					{
							/* Output amplitude transitions*/
							int delta = vol;
							do
							{
									ph = (ph + 1) & 7;
									if ( ph == 0 || ph == duty )
									{
											good_synth->offset_inline( time, delta, out );
											delta = -delta;
									}
									time += per;
							}
							while ( time < end_time );

							if ( delta != vol )
									last_amp -= delta;
					}
					phase = (ph - duty_offset) & 7;
			}
			delay = time - end_time;
	}

	void reset()
	{
		Gb_Env::reset();
		delay = 0x40000000; /* TODO: something less hacky (never clocked until first trigger)*/
	}
	private:
	/* Frequency timer period*/
	int period() const { return (2048 - GB_OSC_FREQUENCY()) * (CLK_MUL_MUL_4); }
};

class Gb_Sweep_Square : public Gb_Square
{
	public:
	int  sweep_freq;
	int  sweep_delay;
	bool sweep_enabled;
	bool sweep_neg;

	template<bool isReader>void SyncState(NewState *ns)
	{
		Gb_Square::SyncState<isReader>(ns);
		NSS(sweep_freq);
		NSS(sweep_delay);
		NSS(sweep_enabled);
		NSS(sweep_neg);
	}

	void Gb_Sweep_Square::clock_sweep()
	{
			if ( --sweep_delay <= 0 )
			{
					reload_sweep_timer();
					if ( sweep_enabled && (regs [0] & PERIOD_MASK) )
					{
							calc_sweep( true  );
							calc_sweep( false );
					}
			}
	}
	INLINE void Gb_Sweep_Square::write_register( int frame_phase, int reg, int old_data, int data )
	{
			if ( reg == 0 && sweep_enabled && sweep_neg && !(data & 0x08) )
					enabled = false; // sweep negate disabled after used

			if ( Gb_Square::write_register( frame_phase, reg, old_data, data ) )
			{
					sweep_freq = GB_OSC_FREQUENCY();
					sweep_neg = false;
					reload_sweep_timer();
					sweep_enabled = (regs [0] & (PERIOD_MASK | SHIFT_MASK)) != 0;
					if ( regs [0] & SHIFT_MASK )
							calc_sweep( false );
			}
	}

	void reset()
	{
		sweep_freq    = 0;
		sweep_delay   = 0;
		sweep_enabled = false;
		sweep_neg     = false;
		Gb_Square::reset();
	}
	private:
	void Gb_Sweep_Square::calc_sweep( bool update )
	{
		int shift, delta, freq;

			shift = regs [0] & SHIFT_MASK;
			delta = sweep_freq >> shift;
			sweep_neg = (regs [0] & 0x08) != 0;
			freq = sweep_freq + (sweep_neg ? -delta : delta);

			if ( freq > 0x7FF )
					enabled = false;
			else if ( shift && update )
			{
					sweep_freq = freq;

					regs [3] = freq & 0xFF;
					regs [4] = (regs [4] & ~0x07) | (freq >> 8 & 0x07);
			}
	}
};

class Gb_Noise : public Gb_Env
{
	public:
	int divider; /* noise has more complex frequency divider setup*/

	template<bool isReader>void SyncState(NewState *ns)
	{
		Gb_Env::SyncState<isReader>(ns);
		NSS(divider);
	}

	/* Quickly runs LFSR for a large number of clocks. For use when noise is generating*/
	/* no sound.*/
	unsigned run_lfsr( unsigned s, unsigned mask, int count )
	{
		/* optimization used in several places:*/
		/* ((s & (1 << b)) << n) ^ ((s & (1 << b)) << (n + 1)) = (s & (1 << b)) * (3 << n)*/

		if ( mask == 0x4000 )
		{
			if ( count >= 32767 )
				count %= 32767;

			/* Convert from Fibonacci to Galois configuration,*/
			/* shifted left 1 bit*/
			s ^= (s & 1) * 0x8000;

			/* Each iteration is equivalent to clocking LFSR 255 times*/
			while ( (count -= 255) > 0 )
				s ^= ((s & 0xE) << 12) ^ ((s & 0xE) << 11) ^ (s >> 3);
			count += 255;

			/* Each iteration is equivalent to clocking LFSR 15 times*/
			/* (interesting similarity to single clocking below)*/
			while ( (count -= 15) > 0 )
				s ^= ((s & 2) * (3 << 13)) ^ (s >> 1);
			count += 15;

			/* Remaining singles*/
			do{
				--count;
				s = ((s & 2) * (3 << 13)) ^ (s >> 1);
			}while(count >= 0);

			/* Convert back to Fibonacci configuration*/
			s &= 0x7FFF;
		}
		else if ( count < 8)
		{
			/* won't fully replace upper 8 bits, so have to do the unoptimized way*/
			do{
				--count;
				s = (s >> 1 | mask) ^ (mask & -((s - 1) & 2));
			}while(count >= 0);
		}
		else
		{
			if ( count > 127 )
			{
				count %= 127;
				if ( !count )
					count = 127; /* must run at least once*/
			}

			/* Need to keep one extra bit of history*/
			s = s << 1 & 0xFF;

			/* Convert from Fibonacci to Galois configuration,*/
			/* shifted left 2 bits*/
			s ^= (s & 2) << 7;

			/* Each iteration is equivalent to clocking LFSR 7 times*/
			/* (interesting similarity to single clocking below)*/
			while ( (count -= 7) > 0 )
				s ^= ((s & 4) * (3 << 5)) ^ (s >> 1);
			count += 7;

			/* Remaining singles*/
			while ( --count >= 0 )
				s = ((s & 4) * (3 << 5)) ^ (s >> 1);

			/* Convert back to Fibonacci configuration and*/
			/* repeat last 8 bits above significant 7*/
			s = (s << 7 & 0x7F80) | (s >> 1 & 0x7F);
		}

		return s;
	}

	void Gb_Noise::run( int32_t time, int32_t end_time )
	{
			/* Determine what will be generated*/
			int vol = 0;
			Blip_Buffer* const out = output;
			if ( out )
			{
					int amp = dac_off_amp;
					if ( GB_ENV_DAC_ENABLED() )
					{
							if ( enabled )
									vol = volume;

				amp = -(vol >> 1);

							if ( !(phase & 1) )
							{
									amp += vol;
									vol = -vol;
							}
					}

					/* AGB negates final output*/
			vol = -vol;
			amp    = -amp;

					update_amp( time, amp );
			}

			/* Run timer and calculate time of next LFSR clock*/
			static unsigned char const period1s [8] = { 1, 2, 4, 6, 8, 10, 12, 14 };
			int const period1 = period1s [regs [3] & 7] * CLK_MUL;
			{
					int extra = (end_time - time) - delay;
					int const per2 = GB_NOISE_PERIOD2(8);
					time += delay + ((divider ^ (per2 >> 1)) & (per2 - 1)) * period1;

					int count = (extra < 0 ? 0 : (extra + period1 - 1) / period1);
					divider = (divider - count) & PERIOD2_MASK;
					delay = count * period1 - extra;
			}

			/* Generate wave*/
			if ( time < end_time )
			{
					unsigned const mask = GB_NOISE_LFSR_MASK();
					unsigned bits = phase;

					int per = GB_NOISE_PERIOD2( period1 * 8 );
					if ( GB_NOISE_PERIOD2_INDEX() >= 0xE )
					{
							time = end_time;
					}
					else if ( !vol )
					{
							/* Maintain phase when not playing*/
							int count = (end_time - time + per - 1) / per;
							time += (int32_t) count * per;
							bits = run_lfsr( bits, ~mask, count );
					}
					else
					{
							/* Output amplitude transitions*/
							int delta = -vol;
							do
							{
									unsigned changed = bits + 1;
									bits = bits >> 1 & mask;
									if ( changed & 2 )
									{
											bits |= ~mask;
											delta = -delta;
											med_synth->offset_inline( time, delta, out );
									}
									time += per;
							}
							while ( time < end_time );

							if ( delta == vol )
									last_amp += delta;
					}
					phase = bits;
			}
	}
	INLINE void Gb_Noise::write_register( int frame_phase, int reg, int old_data, int data )
	{
			if ( Gb_Env::write_register( frame_phase, reg, old_data, data ) )
			{
					phase = 0x7FFF;
					delay += CLK_MUL_MUL_8;
			}
	}

	void reset()
	{
		divider = 0;
		Gb_Env::reset();
		delay = CLK_MUL_MUL_4; /* TODO: remove?*/
	}
};

class Gb_Wave : public Gb_Osc
{
	public:
	int sample_buf;		/* last wave RAM byte read (hardware has this as well)*/
	int agb_mask;		/* 0xFF if AGB features enabled, 0 otherwise*/
	uint8_t* wave_ram;	/* 32 bytes (64 nybbles), stored in APU*/

	template<bool isReader>void SyncState(NewState *ns)
	{
		Gb_Osc::SyncState<isReader>(ns);
		NSS(sample_buf);
		NSS(agb_mask);
	}

	INLINE void Gb_Wave::write_register( int frame_phase, int reg, int old_data, int data )
	{
			switch ( reg )
		{
			case 0:
				if ( !GB_WAVE_DAC_ENABLED() )
					enabled = false;
				break;

			case 1:
				length_ctr = 256 - data;
				break;

			case 4:
				bool was_enabled = enabled;
				if ( write_trig( frame_phase, 256, old_data ) )
				{
					if ( !GB_WAVE_DAC_ENABLED() )
						enabled = false;
					phase = 0;
					delay    = period() + CLK_MUL_MUL_6;
				}
		}
	}
	void Gb_Wave::run( int32_t time, int32_t end_time )
	{
			/* Calc volume*/
			static unsigned char const volumes [8] = { 0, 4, 2, 1, 3, 3, 3, 3 };
			int const volume_idx = regs [2] >> 5 & (agb_mask | 3); /* 2 bits on DMG/CGB, 3 on AGB*/
			int const volume_mul = volumes [volume_idx];

			/* Determine what will be generated*/
			int playing = false;
			Blip_Buffer* const out = output;
			if ( out )
			{
					int amp = dac_off_amp;
					if ( GB_WAVE_DAC_ENABLED() )
					{
							/* Play inaudible frequencies as constant amplitude*/
							amp = 128; /* really depends on average of all samples in wave*/

							/* if delay is larger, constant amplitude won't start yet*/
							if ( GB_OSC_FREQUENCY() <= 0x7FB || delay > CLK_MUL_MUL_15 )
							{
									if ( volume_mul )
											playing = (int) enabled;

									amp = (sample_buf << (phase << 2 & 4) & 0xF0) * playing;
							}

							amp = ((amp * volume_mul) >> VOLUME_SHIFT_PLUS_FOUR) - DAC_BIAS;
					}
					update_amp( time, amp );
			}

			/* Generate wave*/
			time += delay;
			if ( time < end_time )
			{
					unsigned char const* wave = wave_ram;

					/* wave size and bank*/
					int const flags = regs [0] & agb_mask;
					int const wave_mask = (flags & SIZE20_MASK) | 0x1F;
					int swap_banks = 0;
					if ( flags & BANK40_MASK)
					{
							swap_banks = flags & SIZE20_MASK;
							wave += BANK_SIZE_DIV_TWO - (swap_banks >> 1);
					}

					int ph = phase ^ swap_banks;
					ph = (ph + 1) & wave_mask; /* pre-advance*/

					int const per = period();
					if ( !playing )
					{
							/* Maintain phase when not playing*/
							int count = (end_time - time + per - 1) / per;
							ph += count; /* will be masked below*/
							time += (int32_t) count * per;
					}
					else
					{
							/* Output amplitude transitions*/
							int lamp = last_amp + DAC_BIAS;
							do
							{
									/* Extract nybble*/
									int nybble = wave [ph >> 1] << (ph << 2 & 4) & 0xF0;
									ph = (ph + 1) & wave_mask;

									/* Scale by volume*/
									int amp = (nybble * volume_mul) >> VOLUME_SHIFT_PLUS_FOUR;

									int delta = amp - lamp;
									if ( delta )
									{
											lamp = amp;
											med_synth->offset_inline( time, delta, out );
									}
									time += per;
							}
							while ( time < end_time );
							last_amp = lamp - DAC_BIAS;
					}
					ph = (ph - 1) & wave_mask; /* undo pre-advance and mask position*/

					/* Keep track of last byte read*/
					if ( enabled )
							sample_buf = wave [ph >> 1];

					phase = ph ^ swap_banks; /* undo swapped banks*/
			}
			delay = time - end_time;
	}

	/* Reads/writes wave RAM*/
	INLINE int Gb_Wave::read( unsigned addr ) const
	{
		int index;

		if(enabled)
			index = access( addr );
		else
			index = addr & 0x0F;
	
		unsigned char const * wave_bank = &wave_ram[(~regs[0] & BANK40_MASK) >> 2 & agb_mask];

		return (index < 0 ? 0xFF : wave_bank[index]);
	}

	INLINE void Gb_Wave::write( unsigned addr, int data )
	{
		int index;

		if(enabled)
			index = access( addr );
		else
			index = addr & 0x0F;
	
		unsigned char * wave_bank = &wave_ram[(~regs[0] & BANK40_MASK) >> 2 & agb_mask];

		if ( index >= 0 )
			wave_bank[index] = data;;
	}


	void reset()
	{
		sample_buf = 0;
		Gb_Osc::reset();
	}

	private:
	friend class Gb_Apu;

	/* Frequency timer period*/
	int period() const { return (2048 - GB_OSC_FREQUENCY()) * (CLK_MUL_MUL_2); }

	void Gb_Wave::corrupt_wave()
	{
			int pos = ((phase + 1) & BANK_SIZE_MIN_ONE) >> 1;
			if ( pos < 4 )
					wave_ram [0] = wave_ram [pos];
			else
					for ( int i = 4; --i >= 0; )
							wave_ram [i] = wave_ram [(pos & ~3) + i];
	}

	/* Wave index that would be accessed, or -1 if no access would occur*/
	int Gb_Wave::access( unsigned addr ) const
	{
		//if ( mode != MODE_AGB )
		//{
		//	addr = (phase & BANK_SIZE_MIN_ONE) >> 1;
		//}
		return addr & 0x0F;
	}
};

/*============================================================
	INLINE CLASS FUNCS
============================================================ */

int16_t   soundFinalWave [2048];
static const long  soundSampleRate = 44100; //    = 22050;
//int   SOUND_CLOCK_TICKS; //  = SOUND_CLOCK_TICKS_;
//int   soundTicks; //         = SOUND_CLOCK_TICKS_;
int soundTicksUp; // counts up from 0 being the last time the blips were emptied

int soundEnableFlag; //   = 0x3ff; /* emulator channels enabled*/

struct gba_pcm_t
{
	int last_amp;
	int last_time;
	int shift;
	Blip_Buffer* output;

	template<bool isReader>void SyncState(NewState *ns, Gigazoid *g)
	{
		NSS(last_amp);
		NSS(last_time);
		NSS(shift);

		// tricky
		EBS(output, -1);
		EVS(output, &g->bufs_buffer[0], 0);
		EVS(output, &g->bufs_buffer[1], 1);
		EVS(output, &g->bufs_buffer[2], 2);
		EES(output, nullptr);
	}
};

struct gba_pcm_fifo_t
{
	bool enabled;
	uint8_t   fifo [32];
	int  count;
	int  dac;
	int  readIndex;
	int  writeIndex;
	int     which;
	int  timer;
	gba_pcm_t pcm;

	template<bool isReader>void SyncState(NewState *ns, Gigazoid *g)
	{
		NSS(enabled);
		NSS(fifo);
		NSS(count);
		NSS(dac);
		NSS(readIndex);
		NSS(writeIndex);
		NSS(which);
		NSS(timer);
		SSS_HACKY(pcm, g);
	}
};

gba_pcm_fifo_t   pcm [2];


Blip_Synth pcm_synth; // 32 kHz, 16 kHz, 8 kHz

Blip_Buffer bufs_buffer [BUFS_SIZE];
int mixer_samples_read;

void gba_pcm_init (void)
{
	pcm[0].pcm.output    = 0;
	pcm[0].pcm.last_time = 0;
	pcm[0].pcm.last_amp  = 0;
	pcm[0].pcm.shift     = 0;

	pcm[1].pcm.output    = 0;
	pcm[1].pcm.last_time = 0;
	pcm[1].pcm.last_amp  = 0;
	pcm[1].pcm.shift     = 0;
}

void gba_pcm_apply_control( int pcm_idx, int idx )
{
	int ch = 0;
	pcm[pcm_idx].pcm.shift = ~ioMem [SGCNT0_H] >> (2 + idx) & 1;

	if ( (ioMem [NR52] & 0x80) )
		ch = ioMem [SGCNT0_H+1] >> (idx << 2) & 3;

	Blip_Buffer* out = 0;
	switch ( ch )
	{
		case 1:
			out = &bufs_buffer[1];
			break;
		case 2:
			out = &bufs_buffer[0];
			break;
		case 3:
			out = &bufs_buffer[2];
			break;
	}

	if ( pcm[pcm_idx].pcm.output != out )
	{
		if ( pcm[pcm_idx].pcm.output )
			pcm_synth.offset( soundTicksUp, -pcm[pcm_idx].pcm.last_amp, pcm[pcm_idx].pcm.output );
		pcm[pcm_idx].pcm.last_amp = 0;
		pcm[pcm_idx].pcm.output = out;
	}
}

/*============================================================
	GB APU
============================================================ */

/* 0: Square 1, 1: Square 2, 2: Wave, 3: Noise */
#define OSC_COUNT 4

/* Resets hardware to initial power on state BEFORE boot ROM runs. Mode selects*/
/* sound hardware. Additional AGB wave features are enabled separately.*/
#define MODE_AGB	2

#define START_ADDR	0xFF10
#define END_ADDR	0xFF3F

/* Reads and writes must be within the START_ADDR to END_ADDR range, inclusive.*/
/* Addresses outside this range are not mapped to the sound hardware.*/
#define REGISTER_COUNT	48
#define REGS_SIZE 64

/* Clock rate that sound hardware runs at.
 * formula: 4194304 * 4 
 * */
#define CLOCK_RATE 16777216

struct gb_apu_t
{
	bool		reduce_clicks_;
	uint8_t		regs[REGS_SIZE]; // last values written to registers
	int32_t		last_time;	// time sound emulator has been run to
	int32_t		frame_time;	// time of next frame sequencer action
	int32_t		frame_period;       // clocks between each frame sequencer step
	int32_t         frame_phase;    // phase of next frame sequencer step
	double		volume_;
	Gb_Osc*		oscs [OSC_COUNT];
	Gb_Sweep_Square square1;
	Gb_Square       square2;
	Gb_Wave         wave;
	Gb_Noise        noise;
	Blip_Synth	good_synth;
	Blip_Synth	med_synth;

	template<bool isReader>void SyncState(NewState *ns)
	{
		NSS(reduce_clicks_);
		NSS(regs);
		NSS(last_time);
		NSS(frame_time);
		NSS(frame_period);
		NSS(frame_phase);
		NSS(volume_);
		SSS(square1);
		SSS(square2);
		SSS(wave);
		SSS(noise);

		SSS(good_synth);
		SSS(med_synth);
	}
} gb_apu;

#define VOL_REG 0xFF24
#define STEREO_REG 0xFF25
#define STATUS_REG 0xFF26
#define WAVE_RAM 0xFF30
#define POWER_MASK 0x80

#define OSC_COUNT 4

void gb_apu_reduce_clicks( bool reduce )
{
	gb_apu.reduce_clicks_ = reduce;

	/* Click reduction makes DAC off generate same output as volume 0*/
	int dac_off_amp = 0;

	gb_apu.oscs[0]->dac_off_amp = dac_off_amp;
	gb_apu.oscs[1]->dac_off_amp = dac_off_amp;
	gb_apu.oscs[2]->dac_off_amp = dac_off_amp;
	gb_apu.oscs[3]->dac_off_amp = dac_off_amp;

	/* AGB always eliminates clicks on wave channel using same method*/
	gb_apu.wave.dac_off_amp = -DAC_BIAS;
}

void gb_apu_synth_volume( int iv )
{
	double v = gb_apu.volume_ * 0.60 / OSC_COUNT / 15 /*steps*/ / 8 /*master vol range*/ * iv;
	gb_apu.good_synth.volume( v );
	gb_apu.med_synth .volume( v );
}

void gb_apu_apply_volume (void)
{
	int data, left, right, vol_tmp;
	data  = gb_apu.regs [VOL_REG - START_ADDR];
	left  = data >> 4 & 7;
	right = data & 7;
	vol_tmp = left < right ? right : left;
	gb_apu_synth_volume( vol_tmp + 1 );
}

void gb_apu_silence_osc( Gb_Osc& o )
{
	int delta;

	delta = -o.last_amp;
	if ( delta )
	{
		o.last_amp = 0;
		if ( o.output )
		{
			gb_apu.med_synth.offset( gb_apu.last_time, delta, o.output );
		}
	}
}

void gb_apu_run_until_( int32_t end_time )
{
	int32_t time;

	do{
		/* run oscillators*/
		time = end_time;
		if ( time > gb_apu.frame_time )
			time = gb_apu.frame_time;

		gb_apu.square1.run( gb_apu.last_time, time );
		gb_apu.square2.run( gb_apu.last_time, time );
		gb_apu.wave   .run( gb_apu.last_time, time );
		gb_apu.noise  .run( gb_apu.last_time, time );
		gb_apu.last_time = time;

		if ( time == end_time )
			break;

		/* run frame sequencer*/
		gb_apu.frame_time += gb_apu.frame_period * CLK_MUL;
		switch ( gb_apu.frame_phase++ )
		{
			case 2:
			case 6:
				/* 128 Hz*/
				gb_apu.square1.clock_sweep();
			case 0:
			case 4:
				/* 256 Hz*/
				gb_apu.square1.clock_length();
				gb_apu.square2.clock_length();
				gb_apu.wave   .clock_length();
				gb_apu.noise  .clock_length();
				break;

			case 7:
				/* 64 Hz*/
				gb_apu.frame_phase = 0;
				gb_apu.square1.clock_envelope();
				gb_apu.square2.clock_envelope();
				gb_apu.noise  .clock_envelope();
		}
	}while(1);
}

void gb_apu_write_osc( int index, int reg, int old_data, int data )
{
        reg -= index * 5;
        switch ( index )
	{
		case 0:
			gb_apu.square1.write_register( gb_apu.frame_phase, reg, old_data, data );
			break;
		case 1:
			gb_apu.square2.write_register( gb_apu.frame_phase, reg, old_data, data );
			break;
		case 2:
			gb_apu.wave.write_register( gb_apu.frame_phase, reg, old_data, data );
			break;
		case 3:
			gb_apu.noise.write_register( gb_apu.frame_phase, reg, old_data, data );
			break;
	}
}

INLINE int gb_apu_calc_output( int osc )
{
	int bits = gb_apu.regs [STEREO_REG - START_ADDR] >> osc;
	return (bits >> 3 & 2) | (bits & 1);
}

void gb_apu_write_register( int32_t time, unsigned addr, int data )
{
	int reg = addr - START_ADDR;
	if ( (unsigned) reg >= REGISTER_COUNT )
		return;

	if ( addr < STATUS_REG && !(gb_apu.regs [STATUS_REG - START_ADDR] & POWER_MASK) )
		return;	/* Power is off*/

	if ( time > gb_apu.last_time )
		gb_apu_run_until_( time );

	if ( addr >= WAVE_RAM )
	{
		gb_apu.wave.write( addr, data );
	}
	else
	{
		int old_data = gb_apu.regs [reg];
		gb_apu.regs [reg] = data;

		if ( addr < VOL_REG )
			gb_apu_write_osc( reg / 5, reg, old_data, data );	/* Oscillator*/
		else if ( addr == VOL_REG && data != old_data )
		{
			/* Master volume*/
			for ( int i = OSC_COUNT; --i >= 0; )
				gb_apu_silence_osc( *gb_apu.oscs [i] );

			gb_apu_apply_volume();
		}
		else if ( addr == STEREO_REG )
		{
			/* Stereo panning*/
			for ( int i = OSC_COUNT; --i >= 0; )
			{
				Gb_Osc& o = *gb_apu.oscs [i];
				Blip_Buffer* out = o.outputs [gb_apu_calc_output( i )];
				if ( o.output != out )
				{
					gb_apu_silence_osc( o );
					o.output = out;
				}
			} }
		else if ( addr == STATUS_REG && (data ^ old_data) & POWER_MASK )
		{
			/* Power control*/
			gb_apu.frame_phase = 0;
			for ( int i = OSC_COUNT; --i >= 0; )
				gb_apu_silence_osc( *gb_apu.oscs [i] );

			for ( int i = 0; i < 32; i++ )
				gb_apu.regs [i] = 0;

			gb_apu.square1.reset();
			gb_apu.square2.reset();
			gb_apu.wave   .reset();
			gb_apu.noise  .reset();

			gb_apu_apply_volume();

			gb_apu.square1.length_ctr = 64;
			gb_apu.square2.length_ctr = 64;
			gb_apu.wave   .length_ctr = 256;
			gb_apu.noise  .length_ctr = 64;

			gb_apu.regs [STATUS_REG - START_ADDR] = data;
		}
	}
}

void gb_apu_reset( uint32_t mode, bool agb_wave )
{
	/* Hardware mode*/
	mode = MODE_AGB; /* using AGB wave features implies AGB hardware*/
	gb_apu.wave.agb_mask = 0xFF;
	gb_apu.oscs [0]->mode = mode;
	gb_apu.oscs [1]->mode = mode;
	gb_apu.oscs [2]->mode = mode;
	gb_apu.oscs [3]->mode = mode;
	gb_apu_reduce_clicks( gb_apu.reduce_clicks_ );

	/* Reset state*/
	gb_apu.frame_time  = 0;
	gb_apu.last_time   = 0;
	gb_apu.frame_phase = 0;

	for ( int i = 0; i < 32; i++ )
		gb_apu.regs [i] = 0;

	gb_apu.square1.reset();
	gb_apu.square2.reset();
	gb_apu.wave   .reset();
	gb_apu.noise  .reset();

	gb_apu_apply_volume();

	gb_apu.square1.length_ctr = 64;
	gb_apu.square2.length_ctr = 64;
	gb_apu.wave   .length_ctr = 256;
	gb_apu.noise  .length_ctr = 64;

	/* Load initial wave RAM*/
	static unsigned char const initial_wave [2] [16] = {
		{0x84,0x40,0x43,0xAA,0x2D,0x78,0x92,0x3C,0x60,0x59,0x59,0xB0,0x34,0xB8,0x2E,0xDA},
		{0x00,0xFF,0x00,0xFF,0x00,0xFF,0x00,0xFF,0x00,0xFF,0x00,0xFF,0x00,0xFF,0x00,0xFF},
	};
	for ( int b = 2; --b >= 0; )
	{
		/* Init both banks (does nothing if not in AGB mode)*/
		gb_apu_write_register( 0, 0xFF1A, b * 0x40 );
		for ( unsigned i = 0; i < sizeof initial_wave [0]; i++ )
			gb_apu_write_register( 0, i + WAVE_RAM, initial_wave [1] [i] );
	}
}

void gb_apu_new(void)
{
	int i;

	gb_apu.wave.wave_ram = &gb_apu.regs [WAVE_RAM - START_ADDR];

	gb_apu.oscs [0] = &gb_apu.square1;
	gb_apu.oscs [1] = &gb_apu.square2;
	gb_apu.oscs [2] = &gb_apu.wave;
	gb_apu.oscs [3] = &gb_apu.noise;

	for ( i = OSC_COUNT; --i >= 0; )
	{
		Gb_Osc& o = *gb_apu.oscs [i];
		o.regs        = &gb_apu.regs [i * 5];
		o.output      = 0;
		o.outputs [0] = 0;
		o.outputs [1] = 0;
		o.outputs [2] = 0;
		o.outputs [3] = 0;
		o.good_synth  = &gb_apu.good_synth;
		o.med_synth   = &gb_apu.med_synth;
	}

	gb_apu.reduce_clicks_ = false;
	gb_apu.frame_period = 4194304 / 512; /* 512 Hz*/

	gb_apu.volume_ = 1.0;
	gb_apu_reset(MODE_AGB, false);
}



void gb_apu_set_output( Blip_Buffer* center, Blip_Buffer* left, Blip_Buffer* right, int osc )
{
	int i;

	i = osc;
	do
	{
		Gb_Osc& o = *gb_apu.oscs [i];
		o.outputs [1] = right;
		o.outputs [2] = left;
		o.outputs [3] = center;
		o.output = o.outputs [gb_apu_calc_output( i )];
		++i;
	}
	while ( i < osc );
}

void gb_apu_volume( double v )
{
	if ( gb_apu.volume_ != v )
	{
		gb_apu.volume_ = v;
		gb_apu_apply_volume();
	}
}

void gb_apu_apply_stereo (void)
{
	int i;

	for ( i = OSC_COUNT; --i >= 0; )
	{
		Gb_Osc& o = *gb_apu.oscs [i];
		Blip_Buffer* out = o.outputs [gb_apu_calc_output( i )];
		if ( o.output != out )
		{
			gb_apu_silence_osc( o );
			o.output = out;
		}
	}
}


/*============================================================
	GB OSCS
============================================================ */


/*============================================================
	BLIP BUFFER
============================================================ */

/* Blip_Buffer 0.4.1. http://www.slack.net/~ant */

#define FIXED_SHIFT 12
#define SAL_FIXED_SHIFT 4096
#define TO_FIXED( f )   int ((f) * SAL_FIXED_SHIFT)
#define FROM_FIXED( f ) ((f) >> FIXED_SHIFT)



/*============================================================
	STEREO BUFFER
============================================================ */

/* Uses three buffers (one for center) and outputs stereo sample pairs. */

#define STEREO_BUFFER_SAMPLES_AVAILABLE() ((long)(bufs_buffer[0].offset_ -  mixer_samples_read) << 1)
#define stereo_buffer_samples_avail() ((((bufs_buffer [0].offset_ >> BLIP_BUFFER_ACCURACY) - mixer_samples_read) << 1))


const char * stereo_buffer_set_sample_rate( long rate, int msec )
{
        mixer_samples_read = 0;
        for ( int i = BUFS_SIZE; --i >= 0; )
                RETURN_ERR( bufs_buffer [i].set_sample_rate( rate, msec ) );
        return 0; 
}

void stereo_buffer_clock_rate( long rate )
{
	bufs_buffer[2].factor_ = bufs_buffer [2].clock_rate_factor( rate );
	bufs_buffer[1].factor_ = bufs_buffer [1].clock_rate_factor( rate );
	bufs_buffer[0].factor_ = bufs_buffer [0].clock_rate_factor( rate );
}

void stereo_buffer_clear (void)
{
        mixer_samples_read = 0;
	bufs_buffer [2].clear();
	bufs_buffer [1].clear();
	bufs_buffer [0].clear();
}

/* mixers use a single index value to improve performance on register-challenged processors
 * offset goes from negative to zero*/

INLINE void stereo_buffer_mixer_read_pairs( int16_t* out, int count )
{
	/* TODO: if caller never marks buffers as modified, uses mono*/
	/* except that buffer isn't cleared, so caller can encounter*/
	/* subtle problems and not realize the cause.*/
	mixer_samples_read += count;
	int16_t* outtemp = out + count * STEREO;

	/* do left + center and right + center separately to reduce register load*/
	Blip_Buffer* buf = &bufs_buffer [2];
	{
		--buf;
		--outtemp;

		BLIP_READER_BEGIN( side,   *buf );
		BLIP_READER_BEGIN( center, bufs_buffer[2] );

		BLIP_READER_ADJ_( side,   mixer_samples_read );
		BLIP_READER_ADJ_( center, mixer_samples_read );

		int offset = -count;
		do
		{
			int s = (center_reader_accum + side_reader_accum) >> 14;
			BLIP_READER_NEXT_IDX_( side,   offset );
			BLIP_READER_NEXT_IDX_( center, offset );
			BLIP_CLAMP( s, s );

			++offset; /* before write since out is decremented to slightly before end*/
			outtemp [offset * STEREO] = (int16_t) s;
		}while ( offset );

		BLIP_READER_END( side,   *buf );
	}
	{
		--buf;
		--outtemp;

		BLIP_READER_BEGIN( side,   *buf );
		BLIP_READER_BEGIN( center, bufs_buffer[2] );

		BLIP_READER_ADJ_( side,   mixer_samples_read );
		BLIP_READER_ADJ_( center, mixer_samples_read );

		int offset = -count;
		do
		{
			int s = (center_reader_accum + side_reader_accum) >> 14;
			BLIP_READER_NEXT_IDX_( side,   offset );
			BLIP_READER_NEXT_IDX_( center, offset );
			BLIP_CLAMP( s, s );

			++offset; /* before write since out is decremented to slightly before end*/
			outtemp [offset * STEREO] = (int16_t) s;
		}while ( offset );

		BLIP_READER_END( side,   *buf );

		/* only end center once*/
		BLIP_READER_END( center, bufs_buffer[2] );
	}
}

void blip_buffer_remove_all_samples( long count )
{
	uint32_t new_offset = (uint32_t)count << BLIP_BUFFER_ACCURACY;
	/* BLIP BUFFER #1 */
	bufs_buffer[0].offset_ -= new_offset;
	bufs_buffer[1].offset_ -= new_offset;
	bufs_buffer[2].offset_ -= new_offset;

	/* copy remaining samples to beginning and clear old samples*/
	long remain = (bufs_buffer[0].offset_ >> BLIP_BUFFER_ACCURACY) + BLIP_BUFFER_EXTRA_;
	memmove( bufs_buffer[0].buffer_, bufs_buffer[0].buffer_ + count, remain * sizeof *bufs_buffer[0].buffer_ );
	memset( bufs_buffer[0].buffer_ + remain, 0, count * sizeof(*bufs_buffer[0].buffer_));

	remain = (bufs_buffer[1].offset_ >> BLIP_BUFFER_ACCURACY) + BLIP_BUFFER_EXTRA_;
	memmove( bufs_buffer[1].buffer_, bufs_buffer[1].buffer_ + count, remain * sizeof *bufs_buffer[1].buffer_ );
	memset( bufs_buffer[1].buffer_ + remain, 0, count * sizeof(*bufs_buffer[1].buffer_));

	remain = (bufs_buffer[2].offset_ >> BLIP_BUFFER_ACCURACY) + BLIP_BUFFER_EXTRA_;
	memmove( bufs_buffer[2].buffer_, bufs_buffer[2].buffer_ + count, remain * sizeof *bufs_buffer[2].buffer_ );
	memset( bufs_buffer[2].buffer_ + remain, 0, count * sizeof(*bufs_buffer[2].buffer_));
}

long stereo_buffer_read_samples( int16_t * out, long out_size )
{
	int pair_count;

        out_size = (STEREO_BUFFER_SAMPLES_AVAILABLE() < out_size) ? STEREO_BUFFER_SAMPLES_AVAILABLE() : out_size;

        pair_count = int (out_size >> 1);
        if ( pair_count )
	{
		stereo_buffer_mixer_read_pairs( out, pair_count );
		blip_buffer_remove_all_samples( mixer_samples_read );
		mixer_samples_read = 0;
	}
        return out_size;
}

void gba_to_gb_sound_parallel( int * __restrict addr, int * __restrict addr2 )
{
	uint32_t addr1_table = *addr - 0x60;
	uint32_t addr2_table = *addr2 - 0x60;
	*addr = table [addr1_table];
	*addr2 = table [addr2_table];
}

void pcm_fifo_write_control( int data, int data2)
{
	pcm[0].enabled = (data & 0x0300) ? true : false;
	pcm[0].timer   = (data & 0x0400) ? 1 : 0;

	if ( data & 0x0800 )
	{
		// Reset
		pcm[0].writeIndex = 0;
		pcm[0].readIndex  = 0;
		pcm[0].count      = 0;
		pcm[0].dac        = 0;
		memset(pcm[0].fifo, 0, sizeof(pcm[0].fifo));
	}

	gba_pcm_apply_control( 0, pcm[0].which );

	if(pcm[0].pcm.output)
	{
		int time = soundTicksUp;

		pcm[0].dac = (int8_t)pcm[0].dac >> pcm[0].pcm.shift;
		int delta = pcm[0].dac - pcm[0].pcm.last_amp;
		if ( delta )
		{
			pcm[0].pcm.last_amp = pcm[0].dac;
			pcm_synth.offset( time, delta, pcm[0].pcm.output );
		}
		pcm[0].pcm.last_time = time;
	}

	pcm[1].enabled = (data2 & 0x0300) ? true : false;
	pcm[1].timer   = (data2 & 0x0400) ? 1 : 0;

	if ( data2 & 0x0800 )
	{
		// Reset
		pcm[1].writeIndex = 0;
		pcm[1].readIndex  = 0;
		pcm[1].count      = 0;
		pcm[1].dac        = 0;
		memset( pcm[1].fifo, 0, sizeof(pcm[1].fifo));
	}

	gba_pcm_apply_control( 1, pcm[1].which );

	if(pcm[1].pcm.output)
	{
		int time = soundTicksUp;

		pcm[1].dac = (int8_t)pcm[1].dac >> pcm[1].pcm.shift;
		int delta = pcm[1].dac - pcm[1].pcm.last_amp;
		if ( delta )
		{
			pcm[1].pcm.last_amp = pcm[1].dac;
			pcm_synth.offset( time, delta, pcm[1].pcm.output );
		}
		pcm[1].pcm.last_time = time;
	}
}

void soundEvent_u16_parallel(uint32_t address[])
{
	for(int i = 0; i < 8; i++)
	{
		switch ( address[i] )
		{
			case SGCNT0_H:
				//Begin of Write SGCNT0_H
				WRITE16LE( &ioMem [SGCNT0_H], 0 & 0x770F );
				pcm_fifo_write_control(0, 0);

				gb_apu_volume( apu_vols [ioMem [SGCNT0_H] & 3] );
				//End of SGCNT0_H
				break;

			case FIFOA_L:
			case FIFOA_H:
				pcm[0].fifo [pcm[0].writeIndex  ] = 0;
				pcm[0].fifo [pcm[0].writeIndex+1] = 0;
				pcm[0].count += 2;
				pcm[0].writeIndex = (pcm[0].writeIndex + 2) & 31;
				WRITE16LE( &ioMem[address[i]], 0 );
				break;

			case FIFOB_L:
			case FIFOB_H:
				pcm[1].fifo [pcm[1].writeIndex  ] = 0;
				pcm[1].fifo [pcm[1].writeIndex+1] = 0;
				pcm[1].count += 2;
				pcm[1].writeIndex = (pcm[1].writeIndex + 2) & 31;
				WRITE16LE( &ioMem[address[i]], 0 );
				break;

			case 0x88:
				WRITE16LE( &ioMem[address[i]], 0 );
				break;

			default:
				{
					int gb_addr[2]	= {address[i] & ~1, address[i] | 1};
					uint32_t address_array[2] = {address[i] & ~ 1, address[i] | 1};
					uint8_t data_array[2] = {0};
					gba_to_gb_sound_parallel(&gb_addr[0], &gb_addr[1]);
					soundEvent_u8_parallel(gb_addr, address_array, data_array);
					break;
				}
		}
	}
}

void gba_pcm_fifo_timer_overflowed( unsigned pcm_idx )
{
	if ( pcm[pcm_idx].count <= 16 )
	{
		// Need to fill FIFO
		CPUCheckDMA( 3, pcm[pcm_idx].which ? 4 : 2 );

		if ( pcm[pcm_idx].count <= 16 )
		{
			// Not filled by DMA, so fill with 16 bytes of silence
			int reg = pcm[pcm_idx].which ? FIFOB_L : FIFOA_L;

			uint32_t address_array[8] = {reg, reg+2, reg, reg+2, reg, reg+2, reg, reg+2};
			soundEvent_u16_parallel(address_array);
		}
	}

	// Read next sample from FIFO
	pcm[pcm_idx].count--;
	pcm[pcm_idx].dac = pcm[pcm_idx].fifo [pcm[pcm_idx].readIndex];
	pcm[pcm_idx].readIndex = (pcm[pcm_idx].readIndex + 1) & 31;

	if(pcm[pcm_idx].pcm.output)
	{
		int time = soundTicksUp;

		pcm[pcm_idx].dac = (int8_t)pcm[pcm_idx].dac >> pcm[pcm_idx].pcm.shift;
		int delta = pcm[pcm_idx].dac - pcm[pcm_idx].pcm.last_amp;
		if ( delta )
		{
			pcm[pcm_idx].pcm.last_amp = pcm[pcm_idx].dac;
			pcm_synth.offset( time, delta, pcm[pcm_idx].pcm.output );
		}
		pcm[pcm_idx].pcm.last_time = time;
	}
}

void soundEvent_u8_parallel(int gb_addr[], uint32_t address[], uint8_t data[])
{
	for(uint32_t i = 0; i < 2; i++)
	{
		ioMem[address[i]] = data[i];
		gb_apu_write_register( soundTicksUp, gb_addr[i], data[i] );

		if ( address[i] == NR52 )
		{
			gba_pcm_apply_control(0, 0 );
			gba_pcm_apply_control(1, 1 );
		}
		// TODO: what about byte writes to SGCNT0_H etc.?
	}
}

void soundEvent_u8(int gb_addr, uint32_t address, uint8_t data)
{
	ioMem[address] = data;
	gb_apu_write_register( soundTicksUp, gb_addr, data );

	if ( address == NR52 )
	{
		gba_pcm_apply_control(0, 0 );
		gba_pcm_apply_control(1, 1 );
	}
	// TODO: what about byte writes to SGCNT0_H etc.?
}


void soundEvent_u16(uint32_t address, uint16_t data)
{
	switch ( address )
	{
		case SGCNT0_H:
			//Begin of Write SGCNT0_H
			WRITE16LE( &ioMem [SGCNT0_H], data & 0x770F );
			pcm_fifo_write_control( data, data >> 4);

			gb_apu_volume( apu_vols [ioMem [SGCNT0_H] & 3] );
			//End of SGCNT0_H
			break;

		case FIFOA_L:
		case FIFOA_H:
			pcm[0].fifo [pcm[0].writeIndex  ] = data & 0xFF;
			pcm[0].fifo [pcm[0].writeIndex+1] = data >> 8;
			pcm[0].count += 2;
			pcm[0].writeIndex = (pcm[0].writeIndex + 2) & 31;
			WRITE16LE( &ioMem[address], data );
			break;

		case FIFOB_L:
		case FIFOB_H:
			pcm[1].fifo [pcm[1].writeIndex  ] = data & 0xFF;
			pcm[1].fifo [pcm[1].writeIndex+1] = data >> 8;
			pcm[1].count += 2;
			pcm[1].writeIndex = (pcm[1].writeIndex + 2) & 31;
			WRITE16LE( &ioMem[address], data );
			break;

		case 0x88:
			data &= 0xC3FF;
			WRITE16LE( &ioMem[address], data );
			break;

		default:
			{
				int gb_addr[2]	= {address & ~1, address | 1};
				uint32_t address_array[2] = {address & ~ 1, address | 1};
				uint8_t data_array[2] = {(uint8_t)data, (uint8_t)(data >> 8)};
				gba_to_gb_sound_parallel(&gb_addr[0], &gb_addr[1]);
				soundEvent_u8_parallel(gb_addr, address_array, data_array);
				break;
			}
	}
}

void soundTimerOverflow(int timer)
{
	if ( timer == pcm[0].timer && pcm[0].enabled )
		gba_pcm_fifo_timer_overflowed(0);
	if ( timer == pcm[1].timer && pcm[1].enabled )
		gba_pcm_fifo_timer_overflowed(1);
}

void process_sound_tick_fn (void)
{
	// Run sound hardware to present
	pcm[0].pcm.last_time -= soundTicksUp;
	if ( pcm[0].pcm.last_time < -2048 )
		pcm[0].pcm.last_time = -2048;

	pcm[1].pcm.last_time -= soundTicksUp;
	if ( pcm[1].pcm.last_time < -2048 )
		pcm[1].pcm.last_time = -2048;

	/* Emulates sound hardware up to a specified time, ends current time
	frame, then starts a new frame at time 0 */

	if(soundTicksUp > gb_apu.last_time)
		gb_apu_run_until_( soundTicksUp );

	gb_apu.frame_time -= soundTicksUp;
	gb_apu.last_time -= soundTicksUp;

	bufs_buffer[2].offset_ += soundTicksUp * bufs_buffer[2].factor_;
	bufs_buffer[1].offset_ += soundTicksUp * bufs_buffer[1].factor_;
	bufs_buffer[0].offset_ += soundTicksUp * bufs_buffer[0].factor_;


	// dump all the samples available
	// VBA will only ever store 1 frame worth of samples
	int numSamples = stereo_buffer_read_samples( (int16_t*) soundFinalWave, stereo_buffer_samples_avail());
	systemOnWriteDataToSoundBuffer(soundFinalWave, numSamples);

	soundTicksUp = 0;
}

void apply_muting (void)
{
	// PCM
	gba_pcm_apply_control(1, 0 );
	gba_pcm_apply_control(1, 1 );

	// APU
	gb_apu_set_output( &bufs_buffer[2], &bufs_buffer[0], &bufs_buffer[1], 0 );
	gb_apu_set_output( &bufs_buffer[2], &bufs_buffer[0], &bufs_buffer[1], 1 );
	gb_apu_set_output( &bufs_buffer[2], &bufs_buffer[0], &bufs_buffer[1], 2 );
	gb_apu_set_output( &bufs_buffer[2], &bufs_buffer[0], &bufs_buffer[1], 3 );
}


void remake_stereo_buffer (void)
{
	if ( !ioMem )
		return;

	// Clears pointers kept to old stereo_buffer
	gba_pcm_init();

	// Stereo_Buffer

        mixer_samples_read = 0;
	stereo_buffer_set_sample_rate( soundSampleRate, BLIP_DEFAULT_LENGTH );
	stereo_buffer_clock_rate( CLOCK_RATE );

	// PCM
	pcm [0].which = 0;
	pcm [1].which = 1;

	// APU
	gb_apu_new();
	gb_apu_reset( MODE_AGB, true );

	stereo_buffer_clear();

	soundTicksUp = 0;

	apply_muting();

	gb_apu_volume(apu_vols [ioMem [SGCNT0_H] & 3] );

	pcm_synth.volume( 0.66 / 256 * SOUNDVOLUME_ );
}

void soundReset (void)
{
	remake_stereo_buffer();
	//Begin of Reset APU
	gb_apu_reset( MODE_AGB, true );

	stereo_buffer_clear();

	soundTicksUp = 0;

	// Sound Event (NR52)
	int gb_addr = table[NR52 - 0x60];
	if ( gb_addr )
	{
		ioMem[NR52] = 0x80;
		gb_apu_write_register( soundTicksUp, gb_addr, 0x80 );

		gba_pcm_apply_control(0, 0 );
		gba_pcm_apply_control(1, 1 );
	}

	// TODO: what about byte writes to SGCNT0_H etc.?
	// End of Sound Event (NR52)
}
/*
void soundSetSampleRate(long sampleRate)
{
	if ( soundSampleRate != sampleRate )
	{
		soundSampleRate      = sampleRate;
		remake_stereo_buffer();
	}
}
*/

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// END SOUND.CPP
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// BEGIN GBA.CPP
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/*============================================================
	GBA INLINE
============================================================ */

#define UPDATE_REG(address, value)	WRITE16LE(((u16 *)&ioMem[address]),value);
#define ARM_PREFETCH_NEXT		cpuPrefetch[1] = CPUReadMemoryQuick(bus.armNextPC+4);
#define THUMB_PREFETCH_NEXT		cpuPrefetch[1] = CPUReadHalfWordQuick(bus.armNextPC+2);

#define ARM_PREFETCH \
  {\
    cpuPrefetch[0] = CPUReadMemoryQuick(bus.armNextPC);\
    cpuPrefetch[1] = CPUReadMemoryQuick(bus.armNextPC+4);\
  }

#define THUMB_PREFETCH \
  {\
    cpuPrefetch[0] = CPUReadHalfWordQuick(bus.armNextPC);\
    cpuPrefetch[1] = CPUReadHalfWordQuick(bus.armNextPC+2);\
  }
 
int cpuNextEvent; // = 0;
bool holdState; // = false;
uint32_t cpuPrefetch[2];
int cpuTotalTicks; // = 0;
uint8_t memoryWait[16];
uint8_t memoryWaitSeq[16];
uint8_t memoryWait32[16];
uint8_t memoryWaitSeq32[16];

uint8_t biosProtected[4];
uint8_t cpuBitsSet[256];

bool N_FLAG; // = 0;
bool C_FLAG; // = 0;
bool Z_FLAG; // = 0;
bool V_FLAG; // = 0;
bool armState; // = true;
bool armIrqEnable; // = true;
int armMode; // = 0x1f;

typedef enum
{
  REG_DISPCNT = 0x000,
  REG_DISPSTAT = 0x002,
  REG_VCOUNT = 0x003,
  REG_BG0CNT = 0x004,
  REG_BG1CNT = 0x005,
  REG_BG2CNT = 0x006,
  REG_BG3CNT = 0x007,
  REG_BG0HOFS = 0x08,
  REG_BG0VOFS = 0x09,
  REG_BG1HOFS = 0x0A,
  REG_BG1VOFS = 0x0B,
  REG_BG2HOFS = 0x0C,
  REG_BG2VOFS = 0x0D,
  REG_BG3HOFS = 0x0E,
  REG_BG3VOFS = 0x0F,
  REG_BG2PA = 0x10,
  REG_BG2PB = 0x11,
  REG_BG2PC = 0x12,
  REG_BG2PD = 0x13,
  REG_BG2X_L = 0x14,
  REG_BG2X_H = 0x15,
  REG_BG2Y_L = 0x16,
  REG_BG2Y_H = 0x17,
  REG_BG3PA = 0x18,
  REG_BG3PB = 0x19,
  REG_BG3PC = 0x1A,
  REG_BG3PD = 0x1B,
  REG_BG3X_L = 0x1C,
  REG_BG3X_H = 0x1D,
  REG_BG3Y_L = 0x1E,
  REG_BG3Y_H = 0x1F,
  REG_WIN0H = 0x20,
  REG_WIN1H = 0x21,
  REG_WIN0V = 0x22,
  REG_WIN1V = 0x23,
  REG_WININ = 0x24,
  REG_WINOUT = 0x25,
  REG_BLDCNT = 0x28,
  REG_BLDALPHA = 0x29,
  REG_BLDY = 0x2A,
  REG_TM0D = 0x80,
  REG_TM0CNT = 0x81,
  REG_TM1D = 0x82,
  REG_TM1CNT = 0x83,
  REG_TM2D = 0x84,
  REG_TM2CNT = 0x85,
  REG_TM3D = 0x86,
  REG_TM3CNT = 0x87,
  REG_P1 = 0x098,
  REG_P1CNT = 0x099,
  REG_RCNT = 0x9A,
  REG_IE = 0x100,
  REG_IF = 0x101,
  REG_IME = 0x104,
  REG_HALTCNT = 0x180
} hardware_register;

uint16_t io_registers[1024 * 16];

u16 MOSAIC;

uint16_t BG2X_L   ;
uint16_t BG2X_H   ;
uint16_t BG2Y_L   ;
uint16_t BG2Y_H   ;
uint16_t BG3X_L   ;
uint16_t BG3X_H   ;
uint16_t BG3Y_L   ;
uint16_t BG3Y_H   ;
uint16_t BLDMOD   ;
uint16_t COLEV    ;
uint16_t COLY     ;
uint16_t DM0SAD_L ;
uint16_t DM0SAD_H ;
uint16_t DM0DAD_L ;
uint16_t DM0DAD_H ;
uint16_t DM0CNT_L ;
uint16_t DM0CNT_H ;
uint16_t DM1SAD_L ;
uint16_t DM1SAD_H ;
uint16_t DM1DAD_L ;
uint16_t DM1DAD_H ;
uint16_t DM1CNT_L ;
uint16_t DM1CNT_H ;
uint16_t DM2SAD_L ;
uint16_t DM2SAD_H ;
uint16_t DM2DAD_L ;
uint16_t DM2DAD_H ;
uint16_t DM2CNT_L ;
uint16_t DM2CNT_H ;
uint16_t DM3SAD_L ;
uint16_t DM3SAD_H ;
uint16_t DM3DAD_L ;
uint16_t DM3DAD_H ;
uint16_t DM3CNT_L ;
uint16_t DM3CNT_H ;

uint8_t timerOnOffDelay ;
uint16_t timer0Value ;
uint32_t dma0Source ;
uint32_t dma0Dest ;
uint32_t dma1Source ;
uint32_t dma1Dest ;
uint32_t dma2Source ;
uint32_t dma2Dest ;
uint32_t dma3Source ;
uint32_t dma3Dest ;
void (Gigazoid::*cpuSaveGameFunc)(uint32_t,uint8_t);
bool fxOn ;
bool windowOn ;

int cpuDmaTicksToUpdate;

int IRQTicks;
bool intState;

bus_t bus;
graphics_t graphics;

memoryMap map[256];
int clockTicks;

int romSize; // = 0x2000000;
uint32_t line[6][240];
bool gfxInWin[2][240];
int lineOBJpixleft[128];
int joy;

int gfxBG2Changed;
int gfxBG3Changed;

int gfxBG2X;
int gfxBG2Y;
int gfxBG3X;
int gfxBG3Y;

bool ioReadable[0x400];

//int gfxLastVCOUNT = 0;

// Waitstates when accessing data

#define DATATICKS_ACCESS_BUS_PREFETCH(address, value) \
	int addr = (address >> 24) & 15; \
	if ((addr>=0x08) || (addr < 0x02)) \
	{ \
		bus.busPrefetchCount=0; \
		bus.busPrefetch=false; \
	} \
	else if (bus.busPrefetch) \
	{ \
		int waitState = value; \
		waitState = (1 & ~waitState) | (waitState & waitState); \
		bus.busPrefetchCount = ((bus.busPrefetchCount+1)<<waitState) - 1; \
	}

/* Waitstates when accessing data */

#define DATATICKS_ACCESS_32BIT(address)  (memoryWait32[(address >> 24) & 15])
#define DATATICKS_ACCESS_32BIT_SEQ(address) (memoryWaitSeq32[(address >> 24) & 15])
#define DATATICKS_ACCESS_16BIT(address) (memoryWait[(address >> 24) & 15])
#define DATATICKS_ACCESS_16BIT_SEQ(address) (memoryWaitSeq[(address >> 24) & 15])

// Waitstates when executing opcode
INLINE int codeTicksAccess(u32 address, u8 bit32) // THUMB NON SEQ
{
	int addr, ret;

	addr = (address>>24) & 15;

	if (unsigned(addr - 0x08) <= 5)
	{
		if (bus.busPrefetchCount&0x1)
		{
			if (bus.busPrefetchCount&0x2)
			{
				bus.busPrefetchCount = ((bus.busPrefetchCount&0xFF)>>2) | (bus.busPrefetchCount&0xFFFFFF00);
				return 0;
			}
			bus.busPrefetchCount = ((bus.busPrefetchCount&0xFF)>>1) | (bus.busPrefetchCount&0xFFFFFF00);
			return memoryWaitSeq[addr]-1;
		}
	}
	bus.busPrefetchCount = 0;

	if(bit32)		/* ARM NON SEQ */
		ret = memoryWait32[addr];
	else			/* THUMB NON SEQ */
		ret = memoryWait[addr];

	return ret;
}

INLINE int codeTicksAccessSeq16(u32 address) // THUMB SEQ
{
	int addr = (address>>24) & 15;

	if (unsigned(addr - 0x08) <= 5)
	{
		if (bus.busPrefetchCount&0x1)
		{
			bus.busPrefetchCount = ((bus.busPrefetchCount&0xFF)>>1) | (bus.busPrefetchCount&0xFFFFFF00);
			return 0;
		}
		else if (bus.busPrefetchCount>0xFF)
		{
			bus.busPrefetchCount=0;
			return memoryWait[addr];
		}
	}
	else
		bus.busPrefetchCount = 0;

	return memoryWaitSeq[addr];
}

INLINE int codeTicksAccessSeq32(u32 address) // ARM SEQ
{
	int addr = (address>>24)&15;

	if (unsigned(addr - 0x08) <= 5)
	{
		if (bus.busPrefetchCount&0x1)
		{
			if (bus.busPrefetchCount&0x2)
			{
				bus.busPrefetchCount = ((bus.busPrefetchCount&0xFF)>>2) | (bus.busPrefetchCount&0xFFFFFF00);
				return 0;
			}
			bus.busPrefetchCount = ((bus.busPrefetchCount&0xFF)>>1) | (bus.busPrefetchCount&0xFFFFFF00);
			return memoryWaitSeq[addr];
		}
		else if (bus.busPrefetchCount > 0xFF)
		{
			bus.busPrefetchCount=0;
			return memoryWait32[addr];
		}
	}
	return memoryWaitSeq32[addr];
}

#define CPUReadByteQuick(addr)		map[(addr)>>24].address[(addr) & map[(addr)>>24].mask]
#define CPUReadHalfWordQuick(addr)	READ16LE(((u16*)&map[(addr)>>24].address[(addr) & map[(addr)>>24].mask]))
#define CPUReadMemoryQuick(addr)	READ32LE(((u32*)&map[(addr)>>24].address[(addr) & map[(addr)>>24].mask]))

bool stopState;
#ifdef USE_MOTION_SENSOR
extern bool cpuEEPROMSensorEnabled;
#endif
bool timer0On ;
int timer0Ticks ;
int timer0Reload ;
int timer0ClockReload  ;
uint16_t timer1Value ;
bool timer1On ;
int timer1Ticks ;
int timer1Reload ;
int timer1ClockReload  ;
uint16_t timer2Value ;
bool timer2On ;
int timer2Ticks ;
int timer2Reload ;
int timer2ClockReload  ;
uint16_t timer3Value ;
bool timer3On ;
int timer3Ticks ;
int timer3Reload ;
int timer3ClockReload  ;

INLINE u32 CPUReadMemory(u32 address)
{
	if (readCallback)
		readCallback(address);

	u32 value;
	switch(address >> 24)
	{
		case 0:
			/* BIOS */
			if(bus.reg[15].I >> 24)
			{
				if(address < 0x4000)
					value = READ32LE(((u32 *)&biosProtected));
				else goto unreadable;
			}
			else
				value = READ32LE(((u32 *)&bios[address & 0x3FFC]));
			break;
		case 0x02:
			/* external work RAM */
			value = READ32LE(((u32 *)&workRAM[address & 0x3FFFC]));
			break;
		case 0x03:
			/* internal work RAM */
			value = READ32LE(((u32 *)&internalRAM[address & 0x7ffC]));
			break;
		case 0x04:
			/* I/O registers */
			if (address == 0x4000130)
			{
				if (padCallback)
					padCallback();
				lagged = false;
			}
			if((address < 0x4000400) && ioReadable[address & 0x3fc])
			{
				if(ioReadable[(address & 0x3fc) + 2])
					value = READ32LE(((u32 *)&ioMem[address & 0x3fC]));
				else
					value = READ16LE(((u16 *)&ioMem[address & 0x3fc]));
			}
			else
				goto unreadable;
			break;
		case 0x05:
			/* palette RAM */
			value = READ32LE(((u32 *)&graphics.paletteRAM[address & 0x3fC]));
			break;
		case 0x06:
			/* VRAM */
			address = (address & 0x1fffc);
			if (((io_registers[REG_DISPCNT] & 7) >2) && ((address & 0x1C000) == 0x18000))
			{
				value = 0;
				break;
			}
			if ((address & 0x18000) == 0x18000)
				address &= 0x17fff;
			value = READ32LE(((u32 *)&vram[address]));
			break;
		case 0x07:
			/* OAM RAM */
			value = READ32LE(((u32 *)&oam[address & 0x3FC]));
			break;
		case 0x08:
		case 0x09:
		case 0x0A:
		case 0x0B: 
		case 0x0C: 
			/* gamepak ROM */
			value = READ32LE(((u32 *)&rom[address&0x1FFFFFC]));
			break;
		case 0x0D:
         value = eepromRead();
         break;
		case 14:
      case 15:
				value = flashRead(address) * 0x01010101;
            break;
		default:
unreadable:
			if(armState)
				value = CPUReadHalfWordQuick(bus.reg[15].I + (address & 2));
			else
				value = CPUReadHalfWordQuick(bus.reg[15].I);
	}

	if(address & 3) {
		int shift = (address & 3) << 3;
		value = (value >> shift) | (value << (32 - shift));
	}
	return value;
}

INLINE u32 CPUReadHalfWord(u32 address)
{
	if (readCallback)
		readCallback(address);

	u32 value;
	switch(address >> 24)
	{
		case 0:
			if (bus.reg[15].I >> 24)
			{
				if(address < 0x4000)
					value = READ16LE(((u16 *)&biosProtected[address&2]));
				else
					goto unreadable;
			}
			else
				value = READ16LE(((u16 *)&bios[address & 0x3FFE]));
			break;
		case 2:
			value = READ16LE(((u16 *)&workRAM[address & 0x3FFFE]));
			break;
		case 3:
			value = READ16LE(((u16 *)&internalRAM[address & 0x7ffe]));
			break;
		case 4:
			if (address == 0x4000130)
			{
				if (padCallback)
					padCallback();
				lagged = false;
			}

			if((address < 0x4000400) && ioReadable[address & 0x3fe])
			{
				value =  READ16LE(((u16 *)&ioMem[address & 0x3fe]));
				if (((address & 0x3fe)>0xFF) && ((address & 0x3fe)<0x10E))
				{
					if (((address & 0x3fe) == 0x100) && timer0On)
						value = 0xFFFF - ((timer0Ticks-cpuTotalTicks) >> timer0ClockReload);
					else
						if (((address & 0x3fe) == 0x104) && timer1On && !(io_registers[REG_TM1CNT] & 4))
							value = 0xFFFF - ((timer1Ticks-cpuTotalTicks) >> timer1ClockReload);
						else
							if (((address & 0x3fe) == 0x108) && timer2On && !(io_registers[REG_TM2CNT] & 4))
								value = 0xFFFF - ((timer2Ticks-cpuTotalTicks) >> timer2ClockReload);
							else
								if (((address & 0x3fe) == 0x10C) && timer3On && !(io_registers[REG_TM3CNT] & 4))
									value = 0xFFFF - ((timer3Ticks-cpuTotalTicks) >> timer3ClockReload);
				}
			}
			else goto unreadable;
			break;
		case 5:
			value = READ16LE(((u16 *)&graphics.paletteRAM[address & 0x3fe]));
			break;
		case 6:
			address = (address & 0x1fffe);
			if (((io_registers[REG_DISPCNT] & 7) >2) && ((address & 0x1C000) == 0x18000))
			{
				value = 0;
				break;
			}
			if ((address & 0x18000) == 0x18000)
				address &= 0x17fff;
			value = READ16LE(((u16 *)&vram[address]));
			break;
		case 7:
			value = READ16LE(((u16 *)&oam[address & 0x3fe]));
			break;
		case 8:
		case 9:
		case 10:
		case 11:
		case 12:
			if(rtcEnabled && (address == 0x80000c4 || address == 0x80000c6 || address == 0x80000c8))
				value = rtcRead(address);
			else
				value = READ16LE(((u16 *)&rom[address & 0x1FFFFFE]));
			break;
		case 13:
         value =  eepromRead();
         break;
		case 14:
         value = flashRead(address) * 0x0101;
         break;
		default:
unreadable:
			{
				int param = bus.reg[15].I;
				if(armState)
					param += (address & 2);
				value = CPUReadHalfWordQuick(param);
			}
			break;
	}

	if(address & 1)
		value = (value >> 8) | (value << 24);

	return value;
}

INLINE u16 CPUReadHalfWordSigned(u32 address)
{
	u16 value = CPUReadHalfWord(address);
	if((address & 1))
		value = (s8)value;
	return value;
}

INLINE u8 CPUReadByte(u32 address)
{
	if (readCallback)
		readCallback(address);

	switch(address >> 24)
	{
		case 0:
			if (bus.reg[15].I >> 24)
			{
				if(address < 0x4000)
					return biosProtected[address & 3];
				else
					goto unreadable;
			}
			return bios[address & 0x3FFF];
		case 2:
			return workRAM[address & 0x3FFFF];
		case 3:
			return internalRAM[address & 0x7fff];
		case 4:
			if (address == 0x4000130 || address == 0x4000131)
			{
				if (padCallback)
					padCallback();
				lagged = false;
			}

			if((address < 0x4000400) && ioReadable[address & 0x3ff])
				return ioMem[address & 0x3ff];
			else goto unreadable;
		case 5:
			return graphics.paletteRAM[address & 0x3ff];
		case 6:
			address = (address & 0x1ffff);
			if (((io_registers[REG_DISPCNT] & 7) >2) && ((address & 0x1C000) == 0x18000))
				return 0;
			if ((address & 0x18000) == 0x18000)
				address &= 0x17fff;
			return vram[address];
		case 7:
			return oam[address & 0x3ff];
		case 8:
		case 9:
		case 10:
		case 11:
		case 12:
			return rom[address & 0x1FFFFFF];
		case 13:
         return eepromRead();
		case 14:
#ifdef USE_MOTION_SENSOR
			if(cpuEEPROMSensorEnabled)
         {
				switch(address & 0x00008f00)
            {
					case 0x8200:
						return systemGetSensorX() & 255;
					case 0x8300:
						return (systemGetSensorX() >> 8)|0x80;
					case 0x8400:
						return systemGetSensorY() & 255;
					case 0x8500:
						return systemGetSensorY() >> 8;
				}
			}
#endif
         return flashRead(address);
		default:
unreadable:
			if(armState)
				return CPUReadByteQuick(bus.reg[15].I+(address & 3));
			else
				return CPUReadByteQuick(bus.reg[15].I+(address & 1));
	}
}

INLINE void CPUWriteMemory(u32 address, u32 value)
{
	if (writeCallback)
		writeCallback(address);

	switch(address >> 24)
	{
		case 0x02:
			WRITE32LE(((u32 *)&workRAM[address & 0x3FFFC]), value);
			break;
		case 0x03:
			WRITE32LE(((u32 *)&internalRAM[address & 0x7ffC]), value);
			break;
		case 0x04:
			if(address < 0x4000400)
			{
				CPUUpdateRegister((address & 0x3FC), value & 0xFFFF);
				CPUUpdateRegister((address & 0x3FC) + 2, (value >> 16));
			}
			break;
		case 0x05:
			WRITE32LE(((u32 *)&graphics.paletteRAM[address & 0x3FC]), value);
			break;
		case 0x06:
			address = (address & 0x1fffc);
			if (((io_registers[REG_DISPCNT] & 7) >2) && ((address & 0x1C000) == 0x18000))
				return;
			if ((address & 0x18000) == 0x18000)
				address &= 0x17fff;


			WRITE32LE(((u32 *)&vram[address]), value);
			break;
		case 0x07:
			WRITE32LE(((u32 *)&oam[address & 0x3fc]), value);
			break;
		case 0x0D:
			if (cpuEEPROMEnabled)
				eepromWrite(value);
			break;
		case 0x0E:
			(this->*cpuSaveGameFunc)(address, (u8)value);
			break;
		default:
			break;
	}
}

INLINE void CPUWriteHalfWord(u32 address, u16 value)
{
	if (writeCallback)
		writeCallback(address);

	switch(address >> 24)
	{
		case 2:
			WRITE16LE(((u16 *)&workRAM[address & 0x3FFFE]),value);
			break;
		case 3:
			WRITE16LE(((u16 *)&internalRAM[address & 0x7ffe]), value);
			break;
		case 4:
			if(address < 0x4000400)
				CPUUpdateRegister(address & 0x3fe, value);
			break;
		case 5:
			WRITE16LE(((u16 *)&graphics.paletteRAM[address & 0x3fe]), value);
			break;
		case 6:
			address = (address & 0x1fffe);
			if (((io_registers[REG_DISPCNT] & 7) >2) && ((address & 0x1C000) == 0x18000))
				return;
			if ((address & 0x18000) == 0x18000)
				address &= 0x17fff;
			WRITE16LE(((u16 *)&vram[address]), value);
			break;
		case 7:
			WRITE16LE(((u16 *)&oam[address & 0x3fe]), value);
			break;
		case 8:
		case 9:
			if(address == 0x80000c4 || address == 0x80000c6 || address == 0x80000c8)
				if(!rtcWrite(address, value))
					break;
			break;
		case 13:
			if(cpuEEPROMEnabled)
				eepromWrite((u8)value);
			break;
		case 14:
			(this->*cpuSaveGameFunc)(address, (u8)value);
			break;
		default:
			break;
	}
}

INLINE void CPUWriteByte(u32 address, u8 b)
{
	if (writeCallback)
		writeCallback(address);

	switch(address >> 24)
	{
		case 2:
			workRAM[address & 0x3FFFF] = b;
			break;
		case 3:
			internalRAM[address & 0x7fff] = b;
			break;
		case 4:
			if(address < 0x4000400)
			{
				switch(address & 0x3FF)
				{
					case 0x60:
					case 0x61:
					case 0x62:
					case 0x63:
					case 0x64:
					case 0x65:
					case 0x68:
					case 0x69:
					case 0x6c:
					case 0x6d:
					case 0x70:
					case 0x71:
					case 0x72:
					case 0x73:
					case 0x74:
					case 0x75:
					case 0x78:
					case 0x79:
					case 0x7c:
					case 0x7d:
					case 0x80:
					case 0x81:
					case 0x84:
					case 0x85:
					case 0x90:
					case 0x91:
					case 0x92:
					case 0x93:
					case 0x94:
					case 0x95:
					case 0x96:
					case 0x97:
					case 0x98:
					case 0x99:
					case 0x9a:
					case 0x9b:
					case 0x9c:
					case 0x9d:
					case 0x9e:
					case 0x9f:
						{
							int gb_addr = table[(address & 0xFF) - 0x60];
							soundEvent_u8(gb_addr, address&0xFF, b);
						}
						break;
					case 0x301: // HALTCNT, undocumented
						if(b == 0x80)
							stopState = true;
						holdState = 1;
						cpuNextEvent = cpuTotalTicks;
						break;
					default: // every other register
						{
							u32 lowerBits = address & 0x3fe;
							uint16_t param;
							if(address & 1)
								param = (READ16LE(&ioMem[lowerBits]) & 0x00FF) | (b << 8);
							else
								param = (READ16LE(&ioMem[lowerBits]) & 0xFF00) | b;

							CPUUpdateRegister(lowerBits, param);
						}
					break;
				}
			}
			break;
		case 5:
			// no need to switch
			*((u16 *)&graphics.paletteRAM[address & 0x3FE]) = (b << 8) | b;
			break;
		case 6:
			address = (address & 0x1fffe);
			if (((io_registers[REG_DISPCNT] & 7) >2) && ((address & 0x1C000) == 0x18000))
				return;
			if ((address & 0x18000) == 0x18000)
				address &= 0x17fff;

			// no need to switch
			// byte writes to OBJ VRAM are ignored
			if ((address) < objTilesAddress[((io_registers[REG_DISPCNT] & 7)+1)>>2])
				*((u16 *)&vram[address]) = (b << 8) | b;
			break;
		case 7:
			// no need to switch
			// byte writes to OAM are ignored
			//    *((u16 *)&oam[address & 0x3FE]) = (b << 8) | b;
			break;
		case 13:
			if(cpuEEPROMEnabled)
				eepromWrite(b);
			break;
		case 14:
			(this->*cpuSaveGameFunc)(address, b);
			break;
		default:
			break;
	}
}


/*============================================================
	BIOS
============================================================ */

void BIOS_RegisterRamReset(u32 flags)
{
	// no need to trace here. this is only called directly from GBA.cpp
	// to emulate bios initialization

	CPUUpdateRegister(0x0, 0x80);

	if(flags)
	{
		if(flags & 0x01)
			memset(workRAM, 0, 0x40000);		// clear work RAM

		if(flags & 0x02)
			memset(internalRAM, 0, 0x7e00);		// don't clear 0x7e00-0x7fff, clear internal RAM

		if(flags & 0x04)
			memset(graphics.paletteRAM, 0, 0x400);	// clear palette RAM

		if(flags & 0x08)
			memset(vram, 0, 0x18000);		// clear VRAM

		if(flags & 0x10)
			memset(oam, 0, 0x400);			// clean OAM

		if(flags & 0x80) {
			int i;
			for(i = 0; i < 0x10; i++)
				CPUUpdateRegister(0x200+i*2, 0);

			for(i = 0; i < 0xF; i++)
				CPUUpdateRegister(0x4+i*2, 0);

			for(i = 0; i < 0x20; i++)
				CPUUpdateRegister(0x20+i*2, 0);

			for(i = 0; i < 0x18; i++)
				CPUUpdateRegister(0xb0+i*2, 0);

			CPUUpdateRegister(0x130, 0);
			CPUUpdateRegister(0x20, 0x100);
			CPUUpdateRegister(0x30, 0x100);
			CPUUpdateRegister(0x26, 0x100);
			CPUUpdateRegister(0x36, 0x100);
		}

		if(flags & 0x20) {
			int i;
			for(i = 0; i < 8; i++)
				CPUUpdateRegister(0x110+i*2, 0);
			CPUUpdateRegister(0x134, 0x8000);
			for(i = 0; i < 7; i++)
				CPUUpdateRegister(0x140+i*2, 0);
		}

		if(flags & 0x40) {
			int i;
			CPUWriteByte(0x4000084, 0);
			CPUWriteByte(0x4000084, 0x80);
			CPUWriteMemory(0x4000080, 0x880e0000);
			CPUUpdateRegister(0x88, CPUReadHalfWord(0x4000088)&0x3ff);
			CPUWriteByte(0x4000070, 0x70);
			for(i = 0; i < 8; i++)
				CPUUpdateRegister(0x90+i*2, 0);
			CPUWriteByte(0x4000070, 0);
			for(i = 0; i < 8; i++)
				CPUUpdateRegister(0x90+i*2, 0);
			CPUWriteByte(0x4000084, 0);
		}
	}
}

void BIOS_SoftReset (void)
{
	armState = true;
	armMode = 0x1F;
	armIrqEnable = false;
	C_FLAG = V_FLAG = N_FLAG = Z_FLAG = false;
	bus.reg[13].I = 0x03007F00;
	bus.reg[14].I = 0x00000000;
	bus.reg[16].I = 0x00000000;
	bus.reg[R13_IRQ].I = 0x03007FA0;
	bus.reg[R14_IRQ].I = 0x00000000;
	bus.reg[SPSR_IRQ].I = 0x00000000;
	bus.reg[R13_SVC].I = 0x03007FE0;
	bus.reg[R14_SVC].I = 0x00000000;
	bus.reg[SPSR_SVC].I = 0x00000000;
	u8 b = internalRAM[0x7ffa];

	memset(&internalRAM[0x7e00], 0, 0x200);

	if(b) {
		bus.armNextPC = 0x02000000;
		bus.reg[15].I = 0x02000004;
	} else {
		bus.armNextPC = 0x08000000;
		bus.reg[15].I = 0x08000004;
	}
}

#define BIOS_REGISTER_RAM_RESET() BIOS_RegisterRamReset(bus.reg[0].I);

#define CPU_UPDATE_CPSR() \
{ \
	uint32_t CPSR; \
	CPSR = bus.reg[16].I & 0x40; \
	if(N_FLAG) \
		CPSR |= 0x80000000; \
	if(Z_FLAG) \
		CPSR |= 0x40000000; \
	if(C_FLAG) \
		CPSR |= 0x20000000; \
	if(V_FLAG) \
		CPSR |= 0x10000000; \
	if(!armState) \
		CPSR |= 0x00000020; \
	if(!armIrqEnable) \
		CPSR |= 0x80; \
	CPSR |= (armMode & 0x1F); \
	bus.reg[16].I = CPSR; \
}

#define CPU_SOFTWARE_INTERRUPT() \
{ \
  uint32_t PC = bus.reg[15].I; \
  bool savedArmState = armState; \
  if(armMode != 0x13) \
    CPUSwitchMode(0x13, true, false); \
  bus.reg[14].I = PC - (savedArmState ? 4 : 2); \
  bus.reg[15].I = 0x08; \
  armState = true; \
  armIrqEnable = false; \
  bus.armNextPC = 0x08; \
  ARM_PREFETCH; \
  bus.reg[15].I += 4; \
}

void CPUUpdateFlags(bool breakLoop)
{
	uint32_t CPSR = bus.reg[16].I;

	N_FLAG = (CPSR & 0x80000000) ? true: false;
	Z_FLAG = (CPSR & 0x40000000) ? true: false;
	C_FLAG = (CPSR & 0x20000000) ? true: false;
	V_FLAG = (CPSR & 0x10000000) ? true: false;
	armState = (CPSR & 0x20) ? false : true;
	armIrqEnable = (CPSR & 0x80) ? false : true;
	if (breakLoop && armIrqEnable && (io_registers[REG_IF] & io_registers[REG_IE]) && (io_registers[REG_IME] & 1))
		cpuNextEvent = cpuTotalTicks;
}

void CPUSoftwareInterrupt(int comment)
{
	if(armState)
		comment >>= 16;

	CPU_SOFTWARE_INTERRUPT();
}


/*============================================================
	GBA ARM CORE
============================================================ */

#ifdef _MSC_VER
 // Disable "empty statement" warnings
 #pragma warning(disable: 4390)
 // Visual C's inline assembler treats "offset" as a reserved word, so we
 // tell it otherwise.  If you want to use it, write "OFFSET" in capitals.
 #define offset offset_
#endif

void armUnknownInsn(u32 opcode)
{
	u32 PC = bus.reg[15].I;
	bool savedArmState = armState;
	if(armMode != 0x1b )
		CPUSwitchMode(0x1b, true, false);
	bus.reg[14].I = PC - (savedArmState ? 4 : 2);
	bus.reg[15].I = 0x04;
	armState = true;
	armIrqEnable = false;
	bus.armNextPC = 0x04;
	ARM_PREFETCH;
	bus.reg[15].I += 4;
}

// Common macros //////////////////////////////////////////////////////////

#define NEG(i) ((i) >> 31)
#define POS(i) ((~(i)) >> 31)

// The following macros are used for optimization; any not defined for a
// particular compiler/CPU combination default to the C core versions.
//
//    ALU_INIT_C:   Used at the beginning of ALU instructions (AND/EOR/...).
//    (ALU_INIT_NC) Can consist of variable declarations, like the C core,
//                  or the start of a continued assembly block, like the
//                  x86-optimized version.  The _C version is used when the
//                  carry flag from the shift operation is needed (logical
//                  operations that set condition codes, like ANDS); the
//                  _NC version is used when the carry result is ignored.
//    VALUE_XXX: Retrieve the second operand's value for an ALU instruction.
//               The _C and _NC versions are used the same way as ALU_INIT.
//    OP_XXX: ALU operations.  XXX is the instruction name.
//    SETCOND_NONE: Used in multiply instructions in place of SETCOND_MUL
//                  when the condition codes are not set.  Usually empty.
//    SETCOND_MUL: Used in multiply instructions to set the condition codes.
//    ROR_IMM_MSR: Used to rotate the immediate operand for MSR.
//    ROR_OFFSET: Used to rotate the `offset' parameter for LDR and STR
//                instructions.
//    RRX_OFFSET: Used to rotate (RRX) the `offset' parameter for LDR and
//                STR instructions.

// C core

#define C_SETCOND_LOGICAL \
    N_FLAG = ((s32)res < 0) ? true : false;             \
    Z_FLAG = (res == 0) ? true : false;                 \
    C_FLAG = C_OUT;
#define C_SETCOND_ADD \
    N_FLAG = ((s32)res < 0) ? true : false;             \
    Z_FLAG = (res == 0) ? true : false;                 \
    V_FLAG = ((NEG(lhs) & NEG(rhs) & POS(res)) |        \
              (POS(lhs) & POS(rhs) & NEG(res))) ? true : false;\
    C_FLAG = ((NEG(lhs) & NEG(rhs)) |                   \
              (NEG(lhs) & POS(res)) |                   \
              (NEG(rhs) & POS(res))) ? true : false;
#define C_SETCOND_SUB \
    N_FLAG = ((s32)res < 0) ? true : false;             \
    Z_FLAG = (res == 0) ? true : false;                 \
    V_FLAG = ((NEG(lhs) & POS(rhs) & POS(res)) |        \
              (POS(lhs) & NEG(rhs) & NEG(res))) ? true : false;\
    C_FLAG = ((NEG(lhs) & POS(rhs)) |                   \
              (NEG(lhs) & POS(res)) |                   \
              (POS(rhs) & POS(res))) ? true : false;

#ifndef ALU_INIT_C
 #define ALU_INIT_C \
    int dest = (opcode>>12) & 15;                       \
    bool C_OUT = C_FLAG;                                \
    u32 value;
#endif
// OP Rd,Rb,Rm LSL #
#ifndef VALUE_LSL_IMM_C
 #define VALUE_LSL_IMM_C \
    unsigned int shift = (opcode >> 7) & 0x1F;          \
    if (!shift) {  /* LSL #0 most common? */    \
        value = bus.reg[opcode & 0x0F].I;                   \
    } else {                                            \
        u32 v = bus.reg[opcode & 0x0F].I;                   \
        C_OUT = (v >> (32 - shift)) & 1 ? true : false; \
        value = v << shift;                             \
    }
#endif
// OP Rd,Rb,Rm LSL Rs
#ifndef VALUE_LSL_REG_C
 #define VALUE_LSL_REG_C \
    unsigned int shift = bus.reg[(opcode >> 8)&15].B.B0;    \
    if (shift) {                                \
        if (shift == 32) {                              \
            value = 0;                                  \
            C_OUT = (bus.reg[opcode & 0x0F].I & 1 ? true : false);\
        } else if (shift < 32) {                \
            u32 v = bus.reg[opcode & 0x0F].I;               \
            C_OUT = (v >> (32 - shift)) & 1 ? true : false;\
            value = v << shift;                         \
        } else {                                        \
            value = 0;                                  \
            C_OUT = false;                              \
        }                                               \
    } else {                                            \
        value = bus.reg[opcode & 0x0F].I;                   \
    }
#endif
// OP Rd,Rb,Rm LSR #
#ifndef VALUE_LSR_IMM_C
 #define VALUE_LSR_IMM_C \
    unsigned int shift = (opcode >> 7) & 0x1F;          \
    if (shift) {                                \
        u32 v = bus.reg[opcode & 0x0F].I;                   \
        C_OUT = (v >> (shift - 1)) & 1 ? true : false;  \
        value = v >> shift;                             \
    } else {                                            \
        value = 0;                                      \
        C_OUT = (bus.reg[opcode & 0x0F].I & 0x80000000) ? true : false;\
    }
#endif
// OP Rd,Rb,Rm LSR Rs
#ifndef VALUE_LSR_REG_C
 #define VALUE_LSR_REG_C \
    unsigned int shift = bus.reg[(opcode >> 8)&15].B.B0;    \
    if (shift) {                                \
        if (shift == 32) {                              \
            value = 0;                                  \
            C_OUT = (bus.reg[opcode & 0x0F].I & 0x80000000 ? true : false);\
        } else if (shift < 32) {                \
            u32 v = bus.reg[opcode & 0x0F].I;               \
            C_OUT = (v >> (shift - 1)) & 1 ? true : false;\
            value = v >> shift;                         \
        } else {                                        \
            value = 0;                                  \
            C_OUT = false;                              \
        }                                               \
    } else {                                            \
        value = bus.reg[opcode & 0x0F].I;                   \
    }
#endif
// OP Rd,Rb,Rm ASR #
#ifndef VALUE_ASR_IMM_C
 #define VALUE_ASR_IMM_C \
    unsigned int shift = (opcode >> 7) & 0x1F;          \
    if (shift) {                                \
        s32 v = bus.reg[opcode & 0x0F].I;                   \
        C_OUT = (v >> (int)(shift - 1)) & 1 ? true : false;\
        value = v >> (int)shift;                        \
    } else {                                            \
        if (bus.reg[opcode & 0x0F].I & 0x80000000) {        \
            value = 0xFFFFFFFF;                         \
            C_OUT = true;                               \
        } else {                                        \
            value = 0;                                  \
            C_OUT = false;                              \
        }                                               \
    }
#endif
// OP Rd,Rb,Rm ASR Rs
#ifndef VALUE_ASR_REG_C
 #define VALUE_ASR_REG_C \
    unsigned int shift = bus.reg[(opcode >> 8)&15].B.B0;    \
    if (shift < 32) {                           \
        if (shift) {                            \
            s32 v = bus.reg[opcode & 0x0F].I;               \
            C_OUT = (v >> (int)(shift - 1)) & 1 ? true : false;\
            value = v >> (int)shift;                    \
        } else {                                        \
            value = bus.reg[opcode & 0x0F].I;               \
        }                                               \
    } else {                                            \
        if (bus.reg[opcode & 0x0F].I & 0x80000000) {        \
            value = 0xFFFFFFFF;                         \
            C_OUT = true;                               \
        } else {                                        \
            value = 0;                                  \
            C_OUT = false;                              \
        }                                               \
    }
#endif
// OP Rd,Rb,Rm ROR #
#ifndef VALUE_ROR_IMM_C
 #define VALUE_ROR_IMM_C \
    unsigned int shift = (opcode >> 7) & 0x1F;          \
    if (shift) {                                \
        u32 v = bus.reg[opcode & 0x0F].I;                   \
        C_OUT = (v >> (shift - 1)) & 1 ? true : false;  \
        value = ((v << (32 - shift)) |                  \
                 (v >> shift));                         \
    } else {                                            \
        u32 v = bus.reg[opcode & 0x0F].I;                   \
        C_OUT = (v & 1) ? true : false;                 \
        value = ((v >> 1) |                             \
                 (C_FLAG << 31));                       \
    }
#endif
// OP Rd,Rb,Rm ROR Rs
#ifndef VALUE_ROR_REG_C
 #define VALUE_ROR_REG_C \
    unsigned int shift = bus.reg[(opcode >> 8)&15].B.B0;    \
    if (shift & 0x1F) {                         \
        u32 v = bus.reg[opcode & 0x0F].I;                   \
        C_OUT = (v >> (shift - 1)) & 1 ? true : false;  \
        value = ((v << (32 - shift)) |                  \
                 (v >> shift));                         \
    } else {                                            \
        value = bus.reg[opcode & 0x0F].I;                   \
        if (shift)                                      \
            C_OUT = (value & 0x80000000 ? true : false);\
    }
#endif
// OP Rd,Rb,# ROR #
#ifndef VALUE_IMM_C
 #define VALUE_IMM_C \
    int shift = (opcode & 0xF00) >> 7;                  \
    if (shift) {                              \
        u32 v = opcode & 0xFF;                          \
        C_OUT = (v >> (shift - 1)) & 1 ? true : false;  \
        value = ((v << (32 - shift)) |                  \
                 (v >> shift));                         \
    } else {                                            \
        value = opcode & 0xFF;                          \
    }
#endif

// Make the non-carry versions default to the carry versions
// (this is fine for C--the compiler will optimize the dead code out)
#ifndef ALU_INIT_NC
 #define ALU_INIT_NC ALU_INIT_C
#endif
#ifndef VALUE_LSL_IMM_NC
 #define VALUE_LSL_IMM_NC VALUE_LSL_IMM_C
#endif
#ifndef VALUE_LSL_REG_NC
 #define VALUE_LSL_REG_NC VALUE_LSL_REG_C
#endif
#ifndef VALUE_LSR_IMM_NC
 #define VALUE_LSR_IMM_NC VALUE_LSR_IMM_C
#endif
#ifndef VALUE_LSR_REG_NC
 #define VALUE_LSR_REG_NC VALUE_LSR_REG_C
#endif
#ifndef VALUE_ASR_IMM_NC
 #define VALUE_ASR_IMM_NC VALUE_ASR_IMM_C
#endif
#ifndef VALUE_ASR_REG_NC
 #define VALUE_ASR_REG_NC VALUE_ASR_REG_C
#endif
#ifndef VALUE_ROR_IMM_NC
 #define VALUE_ROR_IMM_NC VALUE_ROR_IMM_C
#endif
#ifndef VALUE_ROR_REG_NC
 #define VALUE_ROR_REG_NC VALUE_ROR_REG_C
#endif
#ifndef VALUE_IMM_NC
 #define VALUE_IMM_NC VALUE_IMM_C
#endif

#define C_CHECK_PC(SETCOND) if (dest != 15) { SETCOND }
#ifndef OP_AND
 #define OP_AND \
    u32 res = bus.reg[(opcode>>16)&15].I & value;           \
    bus.reg[dest].I = res;
#endif
#ifndef OP_ANDS
 #define OP_ANDS   OP_AND C_CHECK_PC(C_SETCOND_LOGICAL)
#endif
#ifndef OP_EOR
 #define OP_EOR \
    u32 res = bus.reg[(opcode>>16)&15].I ^ value;           \
    bus.reg[dest].I = res;
#endif
#ifndef OP_EORS
 #define OP_EORS   OP_EOR C_CHECK_PC(C_SETCOND_LOGICAL)
#endif
#ifndef OP_SUB
 #define OP_SUB \
    u32 lhs = bus.reg[(opcode>>16)&15].I;                   \
    u32 rhs = value;                                    \
    u32 res = lhs - rhs;                                \
    bus.reg[dest].I = res;
#endif
#ifndef OP_SUBS
 #define OP_SUBS   OP_SUB C_CHECK_PC(C_SETCOND_SUB)
#endif
#ifndef OP_RSB
 #define OP_RSB \
    u32 lhs = bus.reg[(opcode>>16)&15].I;                   \
    u32 rhs = value;                                    \
    u32 res = rhs - lhs;                                \
    bus.reg[dest].I = res;
#endif
#ifndef OP_RSBS
 #define OP_RSBS   OP_RSB C_CHECK_PC(C_SETCOND_SUB)
#endif
#ifndef OP_ADD
 #define OP_ADD \
    u32 lhs = bus.reg[(opcode>>16)&15].I;                   \
    u32 rhs = value;                                    \
    u32 res = lhs + rhs;                                \
    bus.reg[dest].I = res;
#endif
#ifndef OP_ADDS
 #define OP_ADDS   OP_ADD C_CHECK_PC(C_SETCOND_ADD)
#endif
#ifndef OP_ADC
 #define OP_ADC \
    u32 lhs = bus.reg[(opcode>>16)&15].I;                   \
    u32 rhs = value;                                    \
    u32 res = lhs + rhs + (u32)C_FLAG;                  \
    bus.reg[dest].I = res;
#endif
#ifndef OP_ADCS
 #define OP_ADCS   OP_ADC C_CHECK_PC(C_SETCOND_ADD)
#endif
#ifndef OP_SBC
 #define OP_SBC \
    u32 lhs = bus.reg[(opcode>>16)&15].I;                   \
    u32 rhs = value;                                    \
    u32 res = lhs - rhs - !((u32)C_FLAG);               \
    bus.reg[dest].I = res;
#endif
#ifndef OP_SBCS
 #define OP_SBCS   OP_SBC C_CHECK_PC(C_SETCOND_SUB)
#endif
#ifndef OP_RSC
 #define OP_RSC \
    u32 lhs = bus.reg[(opcode>>16)&15].I;                   \
    u32 rhs = value;                                    \
    u32 res = rhs - lhs - !((u32)C_FLAG);               \
    bus.reg[dest].I = res;
#endif
#ifndef OP_RSCS
 #define OP_RSCS   OP_RSC C_CHECK_PC(C_SETCOND_SUB)
#endif
#ifndef OP_TST
 #define OP_TST \
    u32 res = bus.reg[(opcode >> 16) & 0x0F].I & value;     \
    C_SETCOND_LOGICAL;
#endif
#ifndef OP_TEQ
 #define OP_TEQ \
    u32 res = bus.reg[(opcode >> 16) & 0x0F].I ^ value;     \
    C_SETCOND_LOGICAL;
#endif
#ifndef OP_CMP
 #define OP_CMP \
    u32 lhs = bus.reg[(opcode>>16)&15].I;                   \
    u32 rhs = value;                                    \
    u32 res = lhs - rhs;                                \
    C_SETCOND_SUB;
#endif
#ifndef OP_CMN
 #define OP_CMN \
    u32 lhs = bus.reg[(opcode>>16)&15].I;                   \
    u32 rhs = value;                                    \
    u32 res = lhs + rhs;                                \
    C_SETCOND_ADD;
#endif
#ifndef OP_ORR
 #define OP_ORR \
    u32 res = bus.reg[(opcode >> 16) & 0x0F].I | value;     \
    bus.reg[dest].I = res;
#endif
#ifndef OP_ORRS
 #define OP_ORRS   OP_ORR C_CHECK_PC(C_SETCOND_LOGICAL)
#endif
#ifndef OP_MOV
 #define OP_MOV \
    u32 res = value;                                    \
    bus.reg[dest].I = res;
#endif
#ifndef OP_MOVS
 #define OP_MOVS   OP_MOV C_CHECK_PC(C_SETCOND_LOGICAL)
#endif
#ifndef OP_BIC
 #define OP_BIC \
    u32 res = bus.reg[(opcode >> 16) & 0x0F].I & (~value);  \
    bus.reg[dest].I = res;
#endif
#ifndef OP_BICS
 #define OP_BICS   OP_BIC C_CHECK_PC(C_SETCOND_LOGICAL)
#endif
#ifndef OP_MVN
 #define OP_MVN \
    u32 res = ~value;                                   \
    bus.reg[dest].I = res;
#endif
#ifndef OP_MVNS
 #define OP_MVNS   OP_MVN C_CHECK_PC(C_SETCOND_LOGICAL)
#endif

#ifndef SETCOND_NONE
 #define SETCOND_NONE /*nothing*/
#endif
#ifndef SETCOND_MUL
 #define SETCOND_MUL \
     N_FLAG = ((s32)bus.reg[dest].I < 0) ? true : false;    \
     Z_FLAG = bus.reg[dest].I ? false : true;
#endif
#ifndef SETCOND_MULL
 #define SETCOND_MULL \
     N_FLAG = (bus.reg[dest].I & 0x80000000) ? true : false;\
     Z_FLAG = bus.reg[dest].I || bus.reg[acc].I ? false : true;
#endif

#ifndef ROR_IMM_MSR
 #define ROR_IMM_MSR \
    u32 v = opcode & 0xff;                              \
    value = ((v << (32 - shift)) | (v >> shift));
#endif
#ifndef ROR_OFFSET
 #define ROR_OFFSET \
    offset = ((offset << (32 - shift)) | (offset >> shift));
#endif
#ifndef RRX_OFFSET
 #define RRX_OFFSET \
    offset = ((offset >> 1) | ((int)C_FLAG << 31));
#endif

// ALU ops (except multiply) //////////////////////////////////////////////

// ALU_INIT: init code (ALU_INIT_C or ALU_INIT_NC)
// GETVALUE: load value and shift/rotate (VALUE_XXX)
// OP: ALU operation (OP_XXX)
// MODECHANGE: MODECHANGE_NO or MODECHANGE_YES
// ISREGSHIFT: 1 for insns of the form ...,Rn LSL/etc Rs; 0 otherwise
// ALU_INIT, GETVALUE and OP are concatenated in order.
#define ALU_INSN(ALU_INIT, GETVALUE, OP, MODECHANGE, ISREGSHIFT) \
    ALU_INIT GETVALUE OP;                            \
    if ((opcode & 0x0000F000) != 0x0000F000) {          \
        clockTicks = 1 + ISREGSHIFT                             \
                       + codeTicksAccessSeq32(bus.armNextPC);       \
    } else {                                                    \
        MODECHANGE;                                             \
        if (armState) {                                         \
            bus.reg[15].I &= 0xFFFFFFFC;                            \
            bus.armNextPC = bus.reg[15].I;                              \
            bus.reg[15].I += 4;                                     \
            ARM_PREFETCH;                                       \
        } else {                                                \
            bus.reg[15].I &= 0xFFFFFFFE;                            \
            bus.armNextPC = bus.reg[15].I;                              \
            bus.reg[15].I += 2;                                     \
            THUMB_PREFETCH;                                     \
        }                                                       \
        clockTicks = 3 + ISREGSHIFT                             \
                       + codeTicksAccess(bus.armNextPC, BITS_32)           \
                       + ((codeTicksAccessSeq32(bus.armNextPC)) << 1);       \
    }

#define MODECHANGE_NO  /*nothing*/
#define MODECHANGE_YES if(armMode != (bus.reg[17].I & 0x1f)) CPUSwitchMode(bus.reg[17].I & 0x1f, false, true);

#define DEFINE_ALU_INSN_C(CODE1, CODE2, OP, MODECHANGE) \
   void arm##CODE1##0(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_LSL_IMM_C, OP_##OP, MODECHANGE_##MODECHANGE, 0); }\
   void arm##CODE1##1(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_LSL_REG_C, OP_##OP, MODECHANGE_##MODECHANGE, 1); }\
   void arm##CODE1##2(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_LSR_IMM_C, OP_##OP, MODECHANGE_##MODECHANGE, 0); }\
   void arm##CODE1##3(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_LSR_REG_C, OP_##OP, MODECHANGE_##MODECHANGE, 1); }\
   void arm##CODE1##4(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_ASR_IMM_C, OP_##OP, MODECHANGE_##MODECHANGE, 0); }\
   void arm##CODE1##5(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_ASR_REG_C, OP_##OP, MODECHANGE_##MODECHANGE, 1); }\
   void arm##CODE1##6(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_ROR_IMM_C, OP_##OP, MODECHANGE_##MODECHANGE, 0); }\
   void arm##CODE1##7(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_ROR_REG_C, OP_##OP, MODECHANGE_##MODECHANGE, 1); }\
   void arm##CODE2##0(u32 opcode) { ALU_INSN(ALU_INIT_C, VALUE_IMM_C,     OP_##OP, MODECHANGE_##MODECHANGE, 0); }
#define DEFINE_ALU_INSN_NC(CODE1, CODE2, OP, MODECHANGE) \
   void arm##CODE1##0(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_LSL_IMM_NC, OP_##OP, MODECHANGE_##MODECHANGE, 0); }\
   void arm##CODE1##1(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_LSL_REG_NC, OP_##OP, MODECHANGE_##MODECHANGE, 1); }\
   void arm##CODE1##2(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_LSR_IMM_NC, OP_##OP, MODECHANGE_##MODECHANGE, 0); }\
   void arm##CODE1##3(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_LSR_REG_NC, OP_##OP, MODECHANGE_##MODECHANGE, 1); }\
   void arm##CODE1##4(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_ASR_IMM_NC, OP_##OP, MODECHANGE_##MODECHANGE, 0); }\
   void arm##CODE1##5(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_ASR_REG_NC, OP_##OP, MODECHANGE_##MODECHANGE, 1); }\
   void arm##CODE1##6(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_ROR_IMM_NC, OP_##OP, MODECHANGE_##MODECHANGE, 0); }\
   void arm##CODE1##7(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_ROR_REG_NC, OP_##OP, MODECHANGE_##MODECHANGE, 1); }\
   void arm##CODE2##0(u32 opcode) { ALU_INSN(ALU_INIT_NC, VALUE_IMM_NC,     OP_##OP, MODECHANGE_##MODECHANGE, 0); }

// AND
DEFINE_ALU_INSN_NC(00, 20, AND,  NO)
// ANDS
DEFINE_ALU_INSN_C (01, 21, ANDS, YES)

// EOR
DEFINE_ALU_INSN_NC(02, 22, EOR,  NO)
// EORS
DEFINE_ALU_INSN_C (03, 23, EORS, YES)

// SUB
DEFINE_ALU_INSN_NC(04, 24, SUB,  NO)
// SUBS
DEFINE_ALU_INSN_NC(05, 25, SUBS, YES)

// RSB
DEFINE_ALU_INSN_NC(06, 26, RSB,  NO)
// RSBS
DEFINE_ALU_INSN_NC(07, 27, RSBS, YES)

// ADD
DEFINE_ALU_INSN_NC(08, 28, ADD,  NO)
// ADDS
DEFINE_ALU_INSN_NC(09, 29, ADDS, YES)

// ADC
DEFINE_ALU_INSN_NC(0A, 2A, ADC,  NO)
// ADCS
DEFINE_ALU_INSN_NC(0B, 2B, ADCS, YES)

// SBC
DEFINE_ALU_INSN_NC(0C, 2C, SBC,  NO)
// SBCS
DEFINE_ALU_INSN_NC(0D, 2D, SBCS, YES)

// RSC
DEFINE_ALU_INSN_NC(0E, 2E, RSC,  NO)
// RSCS
DEFINE_ALU_INSN_NC(0F, 2F, RSCS, YES)

// TST
DEFINE_ALU_INSN_C (11, 31, TST,  NO)

// TEQ
DEFINE_ALU_INSN_C (13, 33, TEQ,  NO)

// CMP
DEFINE_ALU_INSN_NC(15, 35, CMP,  NO)

// CMN
DEFINE_ALU_INSN_NC(17, 37, CMN,  NO)

// ORR
DEFINE_ALU_INSN_NC(18, 38, ORR,  NO)
// ORRS
DEFINE_ALU_INSN_C (19, 39, ORRS, YES)

// MOV
DEFINE_ALU_INSN_NC(1A, 3A, MOV,  NO)
// MOVS
DEFINE_ALU_INSN_C (1B, 3B, MOVS, YES)

// BIC
DEFINE_ALU_INSN_NC(1C, 3C, BIC,  NO)
// BICS
DEFINE_ALU_INSN_C (1D, 3D, BICS, YES)

// MVN
DEFINE_ALU_INSN_NC(1E, 3E, MVN,  NO)
// MVNS
DEFINE_ALU_INSN_C (1F, 3F, MVNS, YES)

// Multiply instructions //////////////////////////////////////////////////

// OP: OP_MUL, OP_MLA etc.
// SETCOND: SETCOND_NONE, SETCOND_MUL, or SETCOND_MULL
// CYCLES: base cycle count (1, 2, or 3)
#define MUL_INSN(OP, SETCOND, CYCLES) \
    int mult = (opcode & 0x0F);                         \
    u32 rs = bus.reg[(opcode >> 8) & 0x0F].I;               \
    int acc = (opcode >> 12) & 0x0F;   /* or destLo */  \
    int dest = (opcode >> 16) & 0x0F;  /* or destHi */  \
    OP;                                                 \
    SETCOND;                                            \
    if ((s32)rs < 0)                                    \
        rs = ~rs;                                       \
    if ((rs & 0xFFFF0000) == 0)                         \
        clockTicks += 1;                                \
    else if ((rs & 0xFF000000) == 0)                    \
        clockTicks += 2;                                \
    else                                                \
        clockTicks += 3;                                \
    if (bus.busPrefetchCount == 0)                          \
        bus.busPrefetchCount = ((bus.busPrefetchCount+1)<<clockTicks) - 1; \
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);

#define OP_MUL \
    bus.reg[dest].I = bus.reg[mult].I * rs;
#define OP_MLA \
    bus.reg[dest].I = bus.reg[mult].I * rs + bus.reg[acc].I;
#define OP_MULL(SIGN) \
    SIGN##64 res = (SIGN##64)(SIGN##32)bus.reg[mult].I      \
                 * (SIGN##64)(SIGN##32)rs;              \
    bus.reg[acc].I = (u32)res;                              \
    bus.reg[dest].I = (u32)(res >> 32);
#define OP_MLAL(SIGN) \
    SIGN##64 res = ((SIGN##64)bus.reg[dest].I<<32 | bus.reg[acc].I)\
                 + ((SIGN##64)(SIGN##32)bus.reg[mult].I     \
                    * (SIGN##64)(SIGN##32)rs);          \
    bus.reg[acc].I = (u32)res;                              \
    bus.reg[dest].I = (u32)(res >> 32);
#define OP_UMULL OP_MULL(u)
#define OP_UMLAL OP_MLAL(u)
#define OP_SMULL OP_MULL(s)
#define OP_SMLAL OP_MLAL(s)

// MUL Rd, Rm, Rs
 void arm009(u32 opcode) { MUL_INSN(OP_MUL, SETCOND_NONE, 1); }
// MULS Rd, Rm, Rs
 void arm019(u32 opcode) { MUL_INSN(OP_MUL, SETCOND_MUL, 1); }

// MLA Rd, Rm, Rs, Rn
 void arm029(u32 opcode) { MUL_INSN(OP_MLA, SETCOND_NONE, 2); }
// MLAS Rd, Rm, Rs, Rn
 void arm039(u32 opcode) { MUL_INSN(OP_MLA, SETCOND_MUL, 2); }

// UMULL RdLo, RdHi, Rn, Rs
 void arm089(u32 opcode) { MUL_INSN(OP_UMULL, SETCOND_NONE, 2); }
// UMULLS RdLo, RdHi, Rn, Rs
 void arm099(u32 opcode) { MUL_INSN(OP_UMULL, SETCOND_MULL, 2); }

// UMLAL RdLo, RdHi, Rn, Rs
 void arm0A9(u32 opcode) { MUL_INSN(OP_UMLAL, SETCOND_NONE, 3); }
// UMLALS RdLo, RdHi, Rn, Rs
 void arm0B9(u32 opcode) { MUL_INSN(OP_UMLAL, SETCOND_MULL, 3); }

// SMULL RdLo, RdHi, Rm, Rs
 void arm0C9(u32 opcode) { MUL_INSN(OP_SMULL, SETCOND_NONE, 2); }
// SMULLS RdLo, RdHi, Rm, Rs
 void arm0D9(u32 opcode) { MUL_INSN(OP_SMULL, SETCOND_MULL, 2); }

// SMLAL RdLo, RdHi, Rm, Rs
 void arm0E9(u32 opcode) { MUL_INSN(OP_SMLAL, SETCOND_NONE, 3); }
// SMLALS RdLo, RdHi, Rm, Rs
 void arm0F9(u32 opcode) { MUL_INSN(OP_SMLAL, SETCOND_MULL, 3); }

// Misc instructions //////////////////////////////////////////////////////

// SWP Rd, Rm, [Rn]
 void arm109(u32 opcode)
{
	u32 address = bus.reg[(opcode >> 16) & 15].I;
	u32 temp = CPUReadMemory(address);
	CPUWriteMemory(address, bus.reg[opcode&15].I);
	bus.reg[(opcode >> 12) & 15].I = temp;
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 4 + (dataticks_value << 1) + codeTicksAccess(bus.armNextPC, BITS_32);
}

// SWPB Rd, Rm, [Rn]
 void arm149(u32 opcode)
{
	u32 address = bus.reg[(opcode >> 16) & 15].I;
	u32 temp = CPUReadByte(address);
	CPUWriteByte(address, bus.reg[opcode&15].B.B0);
	bus.reg[(opcode>>12)&15].I = temp;
	u32 dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 4 + (dataticks_value << 1) + codeTicksAccess(bus.armNextPC, BITS_32);
}

// MRS Rd, CPSR
 void arm100(u32 opcode)
{
	if ((opcode & 0x0FFF0FFF) == 0x010F0000)
	{
		CPU_UPDATE_CPSR();
		bus.reg[(opcode >> 12) & 0x0F].I = bus.reg[16].I;
	}
	else
		armUnknownInsn(opcode);
}

// MRS Rd, SPSR
 void arm140(u32 opcode)
{
	if ((opcode & 0x0FFF0FFF) == 0x014F0000)
		bus.reg[(opcode >> 12) & 0x0F].I = bus.reg[17].I;
	else
		armUnknownInsn(opcode);
}

// MSR CPSR_fields, Rm
 void arm120(u32 opcode)
{
    if ((opcode & 0x0FF0FFF0) == 0x0120F000)
    {
	    CPU_UPDATE_CPSR();
	    u32 value = bus.reg[opcode & 15].I;
	    u32 newValue = bus.reg[16].I;
	    if (armMode > 0x10) {
		    if (opcode & 0x00010000)
			    newValue = (newValue & 0xFFFFFF00) | (value & 0x000000FF);
		    if (opcode & 0x00020000)
			    newValue = (newValue & 0xFFFF00FF) | (value & 0x0000FF00);
		    if (opcode & 0x00040000)
			    newValue = (newValue & 0xFF00FFFF) | (value & 0x00FF0000);
	    }
	    if (opcode & 0x00080000)
		    newValue = (newValue & 0x00FFFFFF) | (value & 0xFF000000);
	    newValue |= 0x10;
	    if(armMode != (newValue & 0x1F))
		    CPUSwitchMode(newValue & 0x1F, false, true);
	    bus.reg[16].I = newValue;
	    CPUUpdateFlags(1);
	    if (!armState) {  // this should not be allowed, but it seems to work
		    THUMB_PREFETCH;
		    bus.reg[15].I = bus.armNextPC + 2;
	    }
    }
    else
	    armUnknownInsn(opcode);
}

// MSR SPSR_fields, Rm
 void arm160(u32 opcode)
{
	if ((opcode & 0x0FF0FFF0) == 0x0160F000)
	{
		u32 value = bus.reg[opcode & 15].I;
		if (armMode > 0x10 && armMode < 0x1F)
		{
			if (opcode & 0x00010000)
				bus.reg[17].I = (bus.reg[17].I & 0xFFFFFF00) | (value & 0x000000FF);
			if (opcode & 0x00020000)
				bus.reg[17].I = (bus.reg[17].I & 0xFFFF00FF) | (value & 0x0000FF00);
			if (opcode & 0x00040000)
				bus.reg[17].I = (bus.reg[17].I & 0xFF00FFFF) | (value & 0x00FF0000);
			if (opcode & 0x00080000)
				bus.reg[17].I = (bus.reg[17].I & 0x00FFFFFF) | (value & 0xFF000000);
		}
	}
	else
		armUnknownInsn(opcode);
}

// MSR CPSR_fields, #
 void arm320(u32 opcode)
{
	if ((opcode & 0x0FF0F000) == 0x0320F000)
	{
		CPU_UPDATE_CPSR();
		u32 value = opcode & 0xFF;
		int shift = (opcode & 0xF00) >> 7;
		if (shift) {
			ROR_IMM_MSR;
		}
		u32 newValue = bus.reg[16].I;
		if (armMode > 0x10) {
			if (opcode & 0x00010000)
				newValue = (newValue & 0xFFFFFF00) | (value & 0x000000FF);
			if (opcode & 0x00020000)
				newValue = (newValue & 0xFFFF00FF) | (value & 0x0000FF00);
			if (opcode & 0x00040000)
				newValue = (newValue & 0xFF00FFFF) | (value & 0x00FF0000);
		}
		if (opcode & 0x00080000)
			newValue = (newValue & 0x00FFFFFF) | (value & 0xFF000000);

		newValue |= 0x10;

		if(armMode != (newValue & 0x1F))
			CPUSwitchMode(newValue & 0x1F, false, true);
		bus.reg[16].I = newValue;
		CPUUpdateFlags(1);
		if (!armState) {  // this should not be allowed, but it seems to work
			THUMB_PREFETCH;
			bus.reg[15].I = bus.armNextPC + 2;
		}
	}
	else
		armUnknownInsn(opcode);
}

// MSR SPSR_fields, #
 void arm360(u32 opcode)
{
	if ((opcode & 0x0FF0F000) == 0x0360F000) {
		if (armMode > 0x10 && armMode < 0x1F) {
			u32 value = opcode & 0xFF;
			int shift = (opcode & 0xF00) >> 7;
			if (shift) {
				ROR_IMM_MSR;
			}
			if (opcode & 0x00010000)
				bus.reg[17].I = (bus.reg[17].I & 0xFFFFFF00) | (value & 0x000000FF);
			if (opcode & 0x00020000)
				bus.reg[17].I = (bus.reg[17].I & 0xFFFF00FF) | (value & 0x0000FF00);
			if (opcode & 0x00040000)
				bus.reg[17].I = (bus.reg[17].I & 0xFF00FFFF) | (value & 0x00FF0000);
			if (opcode & 0x00080000)
				bus.reg[17].I = (bus.reg[17].I & 0x00FFFFFF) | (value & 0xFF000000);
		}
	}
	else
		armUnknownInsn(opcode);
}

// BX Rm
 void arm121(u32 opcode)
{
	if ((opcode & 0x0FFFFFF0) == 0x012FFF10) {
		int base = opcode & 0x0F;
		bus.busPrefetchCount = 0;
		armState = bus.reg[base].I & 1 ? false : true;
		if (armState) {
			bus.reg[15].I = bus.reg[base].I & 0xFFFFFFFC;
			bus.armNextPC = bus.reg[15].I;
			bus.reg[15].I += 4;
			ARM_PREFETCH;
			clockTicks = 3 + (codeTicksAccessSeq32(bus.armNextPC)<<1)
				+ codeTicksAccess(bus.armNextPC, BITS_32);
		} else {
			bus.reg[15].I = bus.reg[base].I & 0xFFFFFFFE;
			bus.armNextPC = bus.reg[15].I;
			bus.reg[15].I += 2;
			THUMB_PREFETCH;
			clockTicks = 3 + (codeTicksAccessSeq16(bus.armNextPC)<<1)
				+ codeTicksAccess(bus.armNextPC, BITS_16);
		}
	}
	else
		armUnknownInsn(opcode);
}

// Load/store /////////////////////////////////////////////////////////////

#define OFFSET_IMM \
    int offset = opcode & 0xFFF;
#define OFFSET_IMM8 \
    int offset = ((opcode & 0x0F) | ((opcode>>4) & 0xF0));
#define OFFSET_REG \
    int offset = bus.reg[opcode & 15].I;
#define OFFSET_LSL \
    int offset = bus.reg[opcode & 15].I << ((opcode>>7) & 31);
#define OFFSET_LSR \
    int shift = (opcode >> 7) & 31;                     \
    int offset = shift ? bus.reg[opcode & 15].I >> shift : 0;
#define OFFSET_ASR \
    int shift = (opcode >> 7) & 31;                     \
    int offset;                                         \
    if (shift)                                          \
        offset = (int)((s32)bus.reg[opcode & 15].I >> shift);\
    else if (bus.reg[opcode & 15].I & 0x80000000)           \
        offset = 0xFFFFFFFF;                            \
    else                                                \
        offset = 0;
#define OFFSET_ROR \
    int shift = (opcode >> 7) & 31;                     \
    u32 offset = bus.reg[opcode & 15].I;                    \
    if (shift) {                                        \
        ROR_OFFSET;                                     \
    } else {                                            \
        RRX_OFFSET;                                     \
    }

#define ADDRESS_POST (bus.reg[base].I)
#define ADDRESS_PREDEC (bus.reg[base].I - offset)
#define ADDRESS_PREINC (bus.reg[base].I + offset)

#define OP_STR    CPUWriteMemory(address, bus.reg[dest].I)
#define OP_STRH   CPUWriteHalfWord(address, bus.reg[dest].W.W0)
#define OP_STRB   CPUWriteByte(address, bus.reg[dest].B.B0)
#define OP_LDR    bus.reg[dest].I = CPUReadMemory(address)
#define OP_LDRH   bus.reg[dest].I = CPUReadHalfWord(address)
#define OP_LDRB   bus.reg[dest].I = CPUReadByte(address)
#define OP_LDRSH  bus.reg[dest].I = (s16)CPUReadHalfWordSigned(address)
#define OP_LDRSB  bus.reg[dest].I = (s8)CPUReadByte(address)

#define WRITEBACK_NONE     /*nothing*/
#define WRITEBACK_PRE      bus.reg[base].I = address
#define WRITEBACK_POSTDEC  bus.reg[base].I = address - offset
#define WRITEBACK_POSTINC  bus.reg[base].I = address + offset

#define LDRSTR_INIT(CALC_OFFSET, CALC_ADDRESS) \
    if (bus.busPrefetchCount == 0)                          \
        bus.busPrefetch = bus.busPrefetchEnable;                \
    int dest = (opcode >> 12) & 15;                     \
    int base = (opcode >> 16) & 15;                     \
    CALC_OFFSET;                                        \
    u32 address = CALC_ADDRESS;

#define STR(CALC_OFFSET, CALC_ADDRESS, STORE_DATA, WRITEBACK1, WRITEBACK2, SIZE) \
    LDRSTR_INIT(CALC_OFFSET, CALC_ADDRESS);             \
    WRITEBACK1;                                         \
    STORE_DATA;                                         \
    WRITEBACK2;                                         \
    int dataticks_val;					\
    if(SIZE == 32) \
       dataticks_val = DATATICKS_ACCESS_32BIT(address);	\
    else \
       dataticks_val = DATATICKS_ACCESS_16BIT(address); \
    DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_val); \
    clockTicks = 2 + dataticks_val + codeTicksAccess(bus.armNextPC, BITS_32); 

#define LDR(CALC_OFFSET, CALC_ADDRESS, LOAD_DATA, WRITEBACK, SIZE) \
    LDRSTR_INIT(CALC_OFFSET, CALC_ADDRESS);             \
    LOAD_DATA;                                          \
    if (dest != base)                                   \
    {                                                   \
        WRITEBACK;                                      \
    }                                                   \
    clockTicks = 0;                                     \
    int dataticks_value; \
    if (dest == 15) {                                   \
        bus.reg[15].I &= 0xFFFFFFFC;                        \
        bus.armNextPC = bus.reg[15].I;                          \
        bus.reg[15].I += 4;                                 \
        ARM_PREFETCH;                                   \
	dataticks_value = DATATICKS_ACCESS_32BIT_SEQ(address); \
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
        clockTicks += 2 + (dataticks_value << 1);\
    }                                                   \
    if(SIZE == 32)					\
    dataticks_value = DATATICKS_ACCESS_32BIT(address); \
    else \
    dataticks_value = DATATICKS_ACCESS_16BIT(address); \
    DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
    clockTicks += 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_32);
#define STR_POSTDEC(CALC_OFFSET, STORE_DATA, SIZE) \
  STR(CALC_OFFSET, ADDRESS_POST, STORE_DATA, WRITEBACK_NONE, WRITEBACK_POSTDEC, SIZE)
#define STR_POSTINC(CALC_OFFSET, STORE_DATA, SIZE) \
  STR(CALC_OFFSET, ADDRESS_POST, STORE_DATA, WRITEBACK_NONE, WRITEBACK_POSTINC, SIZE)
#define STR_PREDEC(CALC_OFFSET, STORE_DATA, SIZE) \
  STR(CALC_OFFSET, ADDRESS_PREDEC, STORE_DATA, WRITEBACK_NONE, WRITEBACK_NONE, SIZE)
#define STR_PREDEC_WB(CALC_OFFSET, STORE_DATA, SIZE) \
  STR(CALC_OFFSET, ADDRESS_PREDEC, STORE_DATA, WRITEBACK_PRE, WRITEBACK_NONE, SIZE)
#define STR_PREINC(CALC_OFFSET, STORE_DATA, SIZE) \
  STR(CALC_OFFSET, ADDRESS_PREINC, STORE_DATA, WRITEBACK_NONE, WRITEBACK_NONE, SIZE)
#define STR_PREINC_WB(CALC_OFFSET, STORE_DATA, SIZE) \
  STR(CALC_OFFSET, ADDRESS_PREINC, STORE_DATA, WRITEBACK_PRE, WRITEBACK_NONE, SIZE)
#define LDR_POSTDEC(CALC_OFFSET, LOAD_DATA, SIZE) \
  LDR(CALC_OFFSET, ADDRESS_POST, LOAD_DATA, WRITEBACK_POSTDEC, SIZE)
#define LDR_POSTINC(CALC_OFFSET, LOAD_DATA, SIZE) \
  LDR(CALC_OFFSET, ADDRESS_POST, LOAD_DATA, WRITEBACK_POSTINC, SIZE)
#define LDR_PREDEC(CALC_OFFSET, LOAD_DATA, SIZE) \
  LDR(CALC_OFFSET, ADDRESS_PREDEC, LOAD_DATA, WRITEBACK_NONE, SIZE)
#define LDR_PREDEC_WB(CALC_OFFSET, LOAD_DATA, SIZE) \
  LDR(CALC_OFFSET, ADDRESS_PREDEC, LOAD_DATA, WRITEBACK_PRE, SIZE)
#define LDR_PREINC(CALC_OFFSET, LOAD_DATA, SIZE) \
  LDR(CALC_OFFSET, ADDRESS_PREINC, LOAD_DATA, WRITEBACK_NONE, SIZE)
#define LDR_PREINC_WB(CALC_OFFSET, LOAD_DATA, SIZE) \
  LDR(CALC_OFFSET, ADDRESS_PREINC, LOAD_DATA, WRITEBACK_PRE, SIZE)

// STRH Rd, [Rn], -Rm
 void arm00B(u32 opcode) { STR_POSTDEC(OFFSET_REG, OP_STRH, 16); }
// STRH Rd, [Rn], #-offset
 void arm04B(u32 opcode) { STR_POSTDEC(OFFSET_IMM8, OP_STRH, 16); }
// STRH Rd, [Rn], Rm
 void arm08B(u32 opcode) { STR_POSTINC(OFFSET_REG, OP_STRH, 16); }
// STRH Rd, [Rn], #offset
 void arm0CB(u32 opcode) { STR_POSTINC(OFFSET_IMM8, OP_STRH, 16); }
// STRH Rd, [Rn, -Rm]
 void arm10B(u32 opcode) { STR_PREDEC(OFFSET_REG, OP_STRH, 16); }
// STRH Rd, [Rn, -Rm]!
 void arm12B(u32 opcode) { STR_PREDEC_WB(OFFSET_REG, OP_STRH, 16); }
// STRH Rd, [Rn, -#offset]
 void arm14B(u32 opcode) { STR_PREDEC(OFFSET_IMM8, OP_STRH, 16); }
// STRH Rd, [Rn, -#offset]!
 void arm16B(u32 opcode) { STR_PREDEC_WB(OFFSET_IMM8, OP_STRH, 16); }
// STRH Rd, [Rn, Rm]
 void arm18B(u32 opcode) { STR_PREINC(OFFSET_REG, OP_STRH, 16); }
// STRH Rd, [Rn, Rm]!
 void arm1AB(u32 opcode) { STR_PREINC_WB(OFFSET_REG, OP_STRH, 16); }
// STRH Rd, [Rn, #offset]
 void arm1CB(u32 opcode) { STR_PREINC(OFFSET_IMM8, OP_STRH, 16); }
// STRH Rd, [Rn, #offset]!
 void arm1EB(u32 opcode) { STR_PREINC_WB(OFFSET_IMM8, OP_STRH, 16); }

// LDRH Rd, [Rn], -Rm
 void arm01B(u32 opcode) { LDR_POSTDEC(OFFSET_REG, OP_LDRH, 16); }
// LDRH Rd, [Rn], #-offset
 void arm05B(u32 opcode) { LDR_POSTDEC(OFFSET_IMM8, OP_LDRH, 16); }
// LDRH Rd, [Rn], Rm
 void arm09B(u32 opcode) { LDR_POSTINC(OFFSET_REG, OP_LDRH, 16); }
// LDRH Rd, [Rn], #offset
 void arm0DB(u32 opcode) { LDR_POSTINC(OFFSET_IMM8, OP_LDRH, 16); }
// LDRH Rd, [Rn, -Rm]
 void arm11B(u32 opcode) { LDR_PREDEC(OFFSET_REG, OP_LDRH, 16); }
// LDRH Rd, [Rn, -Rm]!
 void arm13B(u32 opcode) { LDR_PREDEC_WB(OFFSET_REG, OP_LDRH, 16); }
// LDRH Rd, [Rn, -#offset]
 void arm15B(u32 opcode) { LDR_PREDEC(OFFSET_IMM8, OP_LDRH, 16); }
// LDRH Rd, [Rn, -#offset]!
 void arm17B(u32 opcode) { LDR_PREDEC_WB(OFFSET_IMM8, OP_LDRH, 16); }
// LDRH Rd, [Rn, Rm]
 void arm19B(u32 opcode) { LDR_PREINC(OFFSET_REG, OP_LDRH, 16); }
// LDRH Rd, [Rn, Rm]!
 void arm1BB(u32 opcode) { LDR_PREINC_WB(OFFSET_REG, OP_LDRH, 16); }
// LDRH Rd, [Rn, #offset]
 void arm1DB(u32 opcode) { LDR_PREINC(OFFSET_IMM8, OP_LDRH, 16); }
// LDRH Rd, [Rn, #offset]!
 void arm1FB(u32 opcode) { LDR_PREINC_WB(OFFSET_IMM8, OP_LDRH, 16); }

// LDRSB Rd, [Rn], -Rm
 void arm01D(u32 opcode) { LDR_POSTDEC(OFFSET_REG, OP_LDRSB, 16); }
// LDRSB Rd, [Rn], #-offset
 void arm05D(u32 opcode) { LDR_POSTDEC(OFFSET_IMM8, OP_LDRSB, 16); }
// LDRSB Rd, [Rn], Rm
 void arm09D(u32 opcode) { LDR_POSTINC(OFFSET_REG, OP_LDRSB, 16); }
// LDRSB Rd, [Rn], #offset
 void arm0DD(u32 opcode) { LDR_POSTINC(OFFSET_IMM8, OP_LDRSB, 16); }
// LDRSB Rd, [Rn, -Rm]
 void arm11D(u32 opcode) { LDR_PREDEC(OFFSET_REG, OP_LDRSB, 16); }
// LDRSB Rd, [Rn, -Rm]!
 void arm13D(u32 opcode) { LDR_PREDEC_WB(OFFSET_REG, OP_LDRSB, 16); }
// LDRSB Rd, [Rn, -#offset]
 void arm15D(u32 opcode) { LDR_PREDEC(OFFSET_IMM8, OP_LDRSB, 16); }
// LDRSB Rd, [Rn, -#offset]!
 void arm17D(u32 opcode) { LDR_PREDEC_WB(OFFSET_IMM8, OP_LDRSB, 16); }
// LDRSB Rd, [Rn, Rm]
 void arm19D(u32 opcode) { LDR_PREINC(OFFSET_REG, OP_LDRSB, 16); }
// LDRSB Rd, [Rn, Rm]!
 void arm1BD(u32 opcode) { LDR_PREINC_WB(OFFSET_REG, OP_LDRSB, 16); }
// LDRSB Rd, [Rn, #offset]
 void arm1DD(u32 opcode) { LDR_PREINC(OFFSET_IMM8, OP_LDRSB, 16); }
// LDRSB Rd, [Rn, #offset]!
 void arm1FD(u32 opcode) { LDR_PREINC_WB(OFFSET_IMM8, OP_LDRSB, 16); }

// LDRSH Rd, [Rn], -Rm
 void arm01F(u32 opcode) { LDR_POSTDEC(OFFSET_REG, OP_LDRSH, 16); }
// LDRSH Rd, [Rn], #-offset
 void arm05F(u32 opcode) { LDR_POSTDEC(OFFSET_IMM8, OP_LDRSH, 16); }
// LDRSH Rd, [Rn], Rm
 void arm09F(u32 opcode) { LDR_POSTINC(OFFSET_REG, OP_LDRSH, 16); }
// LDRSH Rd, [Rn], #offset
 void arm0DF(u32 opcode) { LDR_POSTINC(OFFSET_IMM8, OP_LDRSH, 16); }
// LDRSH Rd, [Rn, -Rm]
 void arm11F(u32 opcode) { LDR_PREDEC(OFFSET_REG, OP_LDRSH, 16); }
// LDRSH Rd, [Rn, -Rm]!
 void arm13F(u32 opcode) { LDR_PREDEC_WB(OFFSET_REG, OP_LDRSH, 16); }
// LDRSH Rd, [Rn, -#offset]
 void arm15F(u32 opcode) { LDR_PREDEC(OFFSET_IMM8, OP_LDRSH, 16); }
// LDRSH Rd, [Rn, -#offset]!
 void arm17F(u32 opcode) { LDR_PREDEC_WB(OFFSET_IMM8, OP_LDRSH, 16); }
// LDRSH Rd, [Rn, Rm]
 void arm19F(u32 opcode) { LDR_PREINC(OFFSET_REG, OP_LDRSH, 16); }
// LDRSH Rd, [Rn, Rm]!
 void arm1BF(u32 opcode) { LDR_PREINC_WB(OFFSET_REG, OP_LDRSH, 16); }
// LDRSH Rd, [Rn, #offset]
 void arm1DF(u32 opcode) { LDR_PREINC(OFFSET_IMM8, OP_LDRSH, 16); }
// LDRSH Rd, [Rn, #offset]!
 void arm1FF(u32 opcode) { LDR_PREINC_WB(OFFSET_IMM8, OP_LDRSH, 16); }

// STR[T] Rd, [Rn], -#
// Note: STR and STRT do the same thing on the GBA (likewise for LDR/LDRT etc)
 void arm400(u32 opcode) { STR_POSTDEC(OFFSET_IMM, OP_STR, 32); }
// LDR[T] Rd, [Rn], -#
 void arm410(u32 opcode) { LDR_POSTDEC(OFFSET_IMM, OP_LDR, 32); }
// STRB[T] Rd, [Rn], -#
 void arm440(u32 opcode) { STR_POSTDEC(OFFSET_IMM, OP_STRB, 16); }
// LDRB[T] Rd, [Rn], -#
 void arm450(u32 opcode) { LDR_POSTDEC(OFFSET_IMM, OP_LDRB, 16); }
// STR[T] Rd, [Rn], #
 void arm480(u32 opcode) { STR_POSTINC(OFFSET_IMM, OP_STR, 32); }
// LDR Rd, [Rn], #
 void arm490(u32 opcode) { LDR_POSTINC(OFFSET_IMM, OP_LDR, 32); }
// STRB[T] Rd, [Rn], #
 void arm4C0(u32 opcode) { STR_POSTINC(OFFSET_IMM, OP_STRB, 16); }
// LDRB[T] Rd, [Rn], #
 void arm4D0(u32 opcode) { LDR_POSTINC(OFFSET_IMM, OP_LDRB, 16); }
// STR Rd, [Rn, -#]
 void arm500(u32 opcode) { STR_PREDEC(OFFSET_IMM, OP_STR, 32); }
// LDR Rd, [Rn, -#]
 void arm510(u32 opcode) { LDR_PREDEC(OFFSET_IMM, OP_LDR, 32); }
// STR Rd, [Rn, -#]!
 void arm520(u32 opcode) { STR_PREDEC_WB(OFFSET_IMM, OP_STR, 32); }
// LDR Rd, [Rn, -#]!
 void arm530(u32 opcode) { LDR_PREDEC_WB(OFFSET_IMM, OP_LDR, 32); }
// STRB Rd, [Rn, -#]
 void arm540(u32 opcode) { STR_PREDEC(OFFSET_IMM, OP_STRB, 16); }
// LDRB Rd, [Rn, -#]
 void arm550(u32 opcode) { LDR_PREDEC(OFFSET_IMM, OP_LDRB, 16); }
// STRB Rd, [Rn, -#]!
 void arm560(u32 opcode) { STR_PREDEC_WB(OFFSET_IMM, OP_STRB, 16); }
// LDRB Rd, [Rn, -#]!
 void arm570(u32 opcode) { LDR_PREDEC_WB(OFFSET_IMM, OP_LDRB, 16); }
// STR Rd, [Rn, #]
 void arm580(u32 opcode) { STR_PREINC(OFFSET_IMM, OP_STR, 32); }
// LDR Rd, [Rn, #]
 void arm590(u32 opcode) { LDR_PREINC(OFFSET_IMM, OP_LDR, 32); }
// STR Rd, [Rn, #]!
 void arm5A0(u32 opcode) { STR_PREINC_WB(OFFSET_IMM, OP_STR, 32); }
// LDR Rd, [Rn, #]!
 void arm5B0(u32 opcode) { LDR_PREINC_WB(OFFSET_IMM, OP_LDR, 32); }
// STRB Rd, [Rn, #]
 void arm5C0(u32 opcode) { STR_PREINC(OFFSET_IMM, OP_STRB, 16); }
// LDRB Rd, [Rn, #]
 void arm5D0(u32 opcode) { LDR_PREINC(OFFSET_IMM, OP_LDRB, 16); }
// STRB Rd, [Rn, #]!
 void arm5E0(u32 opcode) { STR_PREINC_WB(OFFSET_IMM, OP_STRB, 16); }
// LDRB Rd, [Rn, #]!
 void arm5F0(u32 opcode) { LDR_PREINC_WB(OFFSET_IMM, OP_LDRB, 16); }

// STR[T] Rd, [Rn], -Rm, LSL #
 void arm600(u32 opcode) { STR_POSTDEC(OFFSET_LSL, OP_STR, 32); }
// STR[T] Rd, [Rn], -Rm, LSR #
 void arm602(u32 opcode) { STR_POSTDEC(OFFSET_LSR, OP_STR, 32); }
// STR[T] Rd, [Rn], -Rm, ASR #
 void arm604(u32 opcode) { STR_POSTDEC(OFFSET_ASR, OP_STR, 32); }
// STR[T] Rd, [Rn], -Rm, ROR #
 void arm606(u32 opcode) { STR_POSTDEC(OFFSET_ROR, OP_STR, 32); }
// LDR[T] Rd, [Rn], -Rm, LSL #
 void arm610(u32 opcode) { LDR_POSTDEC(OFFSET_LSL, OP_LDR, 32); }
// LDR[T] Rd, [Rn], -Rm, LSR #
 void arm612(u32 opcode) { LDR_POSTDEC(OFFSET_LSR, OP_LDR, 32); }
// LDR[T] Rd, [Rn], -Rm, ASR #
 void arm614(u32 opcode) { LDR_POSTDEC(OFFSET_ASR, OP_LDR, 32); }
// LDR[T] Rd, [Rn], -Rm, ROR #
 void arm616(u32 opcode) { LDR_POSTDEC(OFFSET_ROR, OP_LDR, 32); }
// STRB[T] Rd, [Rn], -Rm, LSL #
 void arm640(u32 opcode) { STR_POSTDEC(OFFSET_LSL, OP_STRB, 16); }
// STRB[T] Rd, [Rn], -Rm, LSR #
 void arm642(u32 opcode) { STR_POSTDEC(OFFSET_LSR, OP_STRB, 16); }
// STRB[T] Rd, [Rn], -Rm, ASR #
 void arm644(u32 opcode) { STR_POSTDEC(OFFSET_ASR, OP_STRB, 16); }
// STRB[T] Rd, [Rn], -Rm, ROR #
 void arm646(u32 opcode) { STR_POSTDEC(OFFSET_ROR, OP_STRB, 16); }
// LDRB[T] Rd, [Rn], -Rm, LSL #
 void arm650(u32 opcode) { LDR_POSTDEC(OFFSET_LSL, OP_LDRB, 16); }
// LDRB[T] Rd, [Rn], -Rm, LSR #
 void arm652(u32 opcode) { LDR_POSTDEC(OFFSET_LSR, OP_LDRB, 16); }
// LDRB[T] Rd, [Rn], -Rm, ASR #
 void arm654(u32 opcode) { LDR_POSTDEC(OFFSET_ASR, OP_LDRB, 16); }
// LDRB Rd, [Rn], -Rm, ROR #
 void arm656(u32 opcode) { LDR_POSTDEC(OFFSET_ROR, OP_LDRB, 16); }
// STR[T] Rd, [Rn], Rm, LSL #
 void arm680(u32 opcode) { STR_POSTINC(OFFSET_LSL, OP_STR, 32); }
// STR[T] Rd, [Rn], Rm, LSR #
 void arm682(u32 opcode) { STR_POSTINC(OFFSET_LSR, OP_STR, 32); }
// STR[T] Rd, [Rn], Rm, ASR #
 void arm684(u32 opcode) { STR_POSTINC(OFFSET_ASR, OP_STR, 32); }
// STR[T] Rd, [Rn], Rm, ROR #
 void arm686(u32 opcode) { STR_POSTINC(OFFSET_ROR, OP_STR, 32); }
// LDR[T] Rd, [Rn], Rm, LSL #
 void arm690(u32 opcode) { LDR_POSTINC(OFFSET_LSL, OP_LDR, 32); }
// LDR[T] Rd, [Rn], Rm, LSR #
 void arm692(u32 opcode) { LDR_POSTINC(OFFSET_LSR, OP_LDR, 32); }
// LDR[T] Rd, [Rn], Rm, ASR #
 void arm694(u32 opcode) { LDR_POSTINC(OFFSET_ASR, OP_LDR, 32); }
// LDR[T] Rd, [Rn], Rm, ROR #
 void arm696(u32 opcode) { LDR_POSTINC(OFFSET_ROR, OP_LDR, 32); }
// STRB[T] Rd, [Rn], Rm, LSL #
 void arm6C0(u32 opcode) { STR_POSTINC(OFFSET_LSL, OP_STRB, 16); }
// STRB[T] Rd, [Rn], Rm, LSR #
 void arm6C2(u32 opcode) { STR_POSTINC(OFFSET_LSR, OP_STRB, 16); }
// STRB[T] Rd, [Rn], Rm, ASR #
 void arm6C4(u32 opcode) { STR_POSTINC(OFFSET_ASR, OP_STRB, 16); }
// STRB[T] Rd, [Rn], Rm, ROR #
 void arm6C6(u32 opcode) { STR_POSTINC(OFFSET_ROR, OP_STRB, 16); }
// LDRB[T] Rd, [Rn], Rm, LSL #
 void arm6D0(u32 opcode) { LDR_POSTINC(OFFSET_LSL, OP_LDRB, 16); }
// LDRB[T] Rd, [Rn], Rm, LSR #
 void arm6D2(u32 opcode) { LDR_POSTINC(OFFSET_LSR, OP_LDRB, 16); }
// LDRB[T] Rd, [Rn], Rm, ASR #
 void arm6D4(u32 opcode) { LDR_POSTINC(OFFSET_ASR, OP_LDRB, 16); }
// LDRB[T] Rd, [Rn], Rm, ROR #
 void arm6D6(u32 opcode) { LDR_POSTINC(OFFSET_ROR, OP_LDRB, 16); }
// STR Rd, [Rn, -Rm, LSL #]
 void arm700(u32 opcode) { STR_PREDEC(OFFSET_LSL, OP_STR, 32); }
// STR Rd, [Rn, -Rm, LSR #]
 void arm702(u32 opcode) { STR_PREDEC(OFFSET_LSR, OP_STR, 32); }
// STR Rd, [Rn, -Rm, ASR #]
 void arm704(u32 opcode) { STR_PREDEC(OFFSET_ASR, OP_STR, 32); }
// STR Rd, [Rn, -Rm, ROR #]
 void arm706(u32 opcode) { STR_PREDEC(OFFSET_ROR, OP_STR, 32); }
// LDR Rd, [Rn, -Rm, LSL #]
 void arm710(u32 opcode) { LDR_PREDEC(OFFSET_LSL, OP_LDR, 32); }
// LDR Rd, [Rn, -Rm, LSR #]
 void arm712(u32 opcode) { LDR_PREDEC(OFFSET_LSR, OP_LDR, 32); }
// LDR Rd, [Rn, -Rm, ASR #]
 void arm714(u32 opcode) { LDR_PREDEC(OFFSET_ASR, OP_LDR, 32); }
// LDR Rd, [Rn, -Rm, ROR #]
 void arm716(u32 opcode) { LDR_PREDEC(OFFSET_ROR, OP_LDR, 32); }
// STR Rd, [Rn, -Rm, LSL #]!
 void arm720(u32 opcode) { STR_PREDEC_WB(OFFSET_LSL, OP_STR, 32); }
// STR Rd, [Rn, -Rm, LSR #]!
 void arm722(u32 opcode) { STR_PREDEC_WB(OFFSET_LSR, OP_STR, 32); }
// STR Rd, [Rn, -Rm, ASR #]!
 void arm724(u32 opcode) { STR_PREDEC_WB(OFFSET_ASR, OP_STR, 32); }
// STR Rd, [Rn, -Rm, ROR #]!
 void arm726(u32 opcode) { STR_PREDEC_WB(OFFSET_ROR, OP_STR, 32); }
// LDR Rd, [Rn, -Rm, LSL #]!
 void arm730(u32 opcode) { LDR_PREDEC_WB(OFFSET_LSL, OP_LDR, 32); }
// LDR Rd, [Rn, -Rm, LSR #]!
 void arm732(u32 opcode) { LDR_PREDEC_WB(OFFSET_LSR, OP_LDR, 32); }
// LDR Rd, [Rn, -Rm, ASR #]!
 void arm734(u32 opcode) { LDR_PREDEC_WB(OFFSET_ASR, OP_LDR, 32); }
// LDR Rd, [Rn, -Rm, ROR #]!
 void arm736(u32 opcode) { LDR_PREDEC_WB(OFFSET_ROR, OP_LDR, 32); }
// STRB Rd, [Rn, -Rm, LSL #]
 void arm740(u32 opcode) { STR_PREDEC(OFFSET_LSL, OP_STRB, 16); }
// STRB Rd, [Rn, -Rm, LSR #]
 void arm742(u32 opcode) { STR_PREDEC(OFFSET_LSR, OP_STRB, 16); }
// STRB Rd, [Rn, -Rm, ASR #]
 void arm744(u32 opcode) { STR_PREDEC(OFFSET_ASR, OP_STRB, 16); }
// STRB Rd, [Rn, -Rm, ROR #]
 void arm746(u32 opcode) { STR_PREDEC(OFFSET_ROR, OP_STRB, 16); }
// LDRB Rd, [Rn, -Rm, LSL #]
 void arm750(u32 opcode) { LDR_PREDEC(OFFSET_LSL, OP_LDRB, 16); }
// LDRB Rd, [Rn, -Rm, LSR #]
 void arm752(u32 opcode) { LDR_PREDEC(OFFSET_LSR, OP_LDRB, 16); }
// LDRB Rd, [Rn, -Rm, ASR #]
 void arm754(u32 opcode) { LDR_PREDEC(OFFSET_ASR, OP_LDRB, 16); }
// LDRB Rd, [Rn, -Rm, ROR #]
 void arm756(u32 opcode) { LDR_PREDEC(OFFSET_ROR, OP_LDRB, 16); }
// STRB Rd, [Rn, -Rm, LSL #]!
 void arm760(u32 opcode) { STR_PREDEC_WB(OFFSET_LSL, OP_STRB, 16); }
// STRB Rd, [Rn, -Rm, LSR #]!
 void arm762(u32 opcode) { STR_PREDEC_WB(OFFSET_LSR, OP_STRB, 16); }
// STRB Rd, [Rn, -Rm, ASR #]!
 void arm764(u32 opcode) { STR_PREDEC_WB(OFFSET_ASR, OP_STRB, 16); }
// STRB Rd, [Rn, -Rm, ROR #]!
 void arm766(u32 opcode) { STR_PREDEC_WB(OFFSET_ROR, OP_STRB, 16); }
// LDRB Rd, [Rn, -Rm, LSL #]!
 void arm770(u32 opcode) { LDR_PREDEC_WB(OFFSET_LSL, OP_LDRB, 16); }
// LDRB Rd, [Rn, -Rm, LSR #]!
 void arm772(u32 opcode) { LDR_PREDEC_WB(OFFSET_LSR, OP_LDRB, 16); }
// LDRB Rd, [Rn, -Rm, ASR #]!
 void arm774(u32 opcode) { LDR_PREDEC_WB(OFFSET_ASR, OP_LDRB, 16); }
// LDRB Rd, [Rn, -Rm, ROR #]!
 void arm776(u32 opcode) { LDR_PREDEC_WB(OFFSET_ROR, OP_LDRB, 16); }
// STR Rd, [Rn, Rm, LSL #]
 void arm780(u32 opcode) { STR_PREINC(OFFSET_LSL, OP_STR, 32); }
// STR Rd, [Rn, Rm, LSR #]
 void arm782(u32 opcode) { STR_PREINC(OFFSET_LSR, OP_STR, 32); }
// STR Rd, [Rn, Rm, ASR #]
 void arm784(u32 opcode) { STR_PREINC(OFFSET_ASR, OP_STR, 32); }
// STR Rd, [Rn, Rm, ROR #]
 void arm786(u32 opcode) { STR_PREINC(OFFSET_ROR, OP_STR, 32); }
// LDR Rd, [Rn, Rm, LSL #]
 void arm790(u32 opcode) { LDR_PREINC(OFFSET_LSL, OP_LDR, 32); }
// LDR Rd, [Rn, Rm, LSR #]
 void arm792(u32 opcode) { LDR_PREINC(OFFSET_LSR, OP_LDR, 32); }
// LDR Rd, [Rn, Rm, ASR #]
 void arm794(u32 opcode) { LDR_PREINC(OFFSET_ASR, OP_LDR, 32); }
// LDR Rd, [Rn, Rm, ROR #]
 void arm796(u32 opcode) { LDR_PREINC(OFFSET_ROR, OP_LDR, 32); }
// STR Rd, [Rn, Rm, LSL #]!
 void arm7A0(u32 opcode) { STR_PREINC_WB(OFFSET_LSL, OP_STR, 32); }
// STR Rd, [Rn, Rm, LSR #]!
 void arm7A2(u32 opcode) { STR_PREINC_WB(OFFSET_LSR, OP_STR, 32); }
// STR Rd, [Rn, Rm, ASR #]!
 void arm7A4(u32 opcode) { STR_PREINC_WB(OFFSET_ASR, OP_STR, 32); }
// STR Rd, [Rn, Rm, ROR #]!
 void arm7A6(u32 opcode) { STR_PREINC_WB(OFFSET_ROR, OP_STR, 32); }
// LDR Rd, [Rn, Rm, LSL #]!
 void arm7B0(u32 opcode) { LDR_PREINC_WB(OFFSET_LSL, OP_LDR, 32); }
// LDR Rd, [Rn, Rm, LSR #]!
 void arm7B2(u32 opcode) { LDR_PREINC_WB(OFFSET_LSR, OP_LDR, 32); }
// LDR Rd, [Rn, Rm, ASR #]!
 void arm7B4(u32 opcode) { LDR_PREINC_WB(OFFSET_ASR, OP_LDR, 32); }
// LDR Rd, [Rn, Rm, ROR #]!
 void arm7B6(u32 opcode) { LDR_PREINC_WB(OFFSET_ROR, OP_LDR, 32); }
// STRB Rd, [Rn, Rm, LSL #]
 void arm7C0(u32 opcode) { STR_PREINC(OFFSET_LSL, OP_STRB, 16); }
// STRB Rd, [Rn, Rm, LSR #]
 void arm7C2(u32 opcode) { STR_PREINC(OFFSET_LSR, OP_STRB, 16); }
// STRB Rd, [Rn, Rm, ASR #]
 void arm7C4(u32 opcode) { STR_PREINC(OFFSET_ASR, OP_STRB, 16); }
// STRB Rd, [Rn, Rm, ROR #]
 void arm7C6(u32 opcode) { STR_PREINC(OFFSET_ROR, OP_STRB, 16); }
// LDRB Rd, [Rn, Rm, LSL #]
 void arm7D0(u32 opcode) { LDR_PREINC(OFFSET_LSL, OP_LDRB, 16); }
// LDRB Rd, [Rn, Rm, LSR #]
 void arm7D2(u32 opcode) { LDR_PREINC(OFFSET_LSR, OP_LDRB, 16); }
// LDRB Rd, [Rn, Rm, ASR #]
 void arm7D4(u32 opcode) { LDR_PREINC(OFFSET_ASR, OP_LDRB, 16); }
// LDRB Rd, [Rn, Rm, ROR #]
 void arm7D6(u32 opcode) { LDR_PREINC(OFFSET_ROR, OP_LDRB, 16); }
// STRB Rd, [Rn, Rm, LSL #]!
 void arm7E0(u32 opcode) { STR_PREINC_WB(OFFSET_LSL, OP_STRB, 16); }
// STRB Rd, [Rn, Rm, LSR #]!
 void arm7E2(u32 opcode) { STR_PREINC_WB(OFFSET_LSR, OP_STRB, 16); }
// STRB Rd, [Rn, Rm, ASR #]!
 void arm7E4(u32 opcode) { STR_PREINC_WB(OFFSET_ASR, OP_STRB, 16); }
// STRB Rd, [Rn, Rm, ROR #]!
 void arm7E6(u32 opcode) { STR_PREINC_WB(OFFSET_ROR, OP_STRB, 16); }
// LDRB Rd, [Rn, Rm, LSL #]!
 void arm7F0(u32 opcode) { LDR_PREINC_WB(OFFSET_LSL, OP_LDRB, 16); }
// LDRB Rd, [Rn, Rm, LSR #]!
 void arm7F2(u32 opcode) { LDR_PREINC_WB(OFFSET_LSR, OP_LDRB, 16); }
// LDRB Rd, [Rn, Rm, ASR #]!
 void arm7F4(u32 opcode) { LDR_PREINC_WB(OFFSET_ASR, OP_LDRB, 16); }
// LDRB Rd, [Rn, Rm, ROR #]!
 void arm7F6(u32 opcode) { LDR_PREINC_WB(OFFSET_ROR, OP_LDRB, 16); }

// STM/LDM ////////////////////////////////////////////////////////////////

#define STM_REG(bit,num) \
    if (opcode & (1U<<(bit))) {                         \
        CPUWriteMemory(address, bus.reg[(num)].I);          \
	int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
	clockTicks += 1 + dataticks_value; \
        count++;                                        \
        address += 4;                                   \
    }
#define STMW_REG(bit,num) \
    if (opcode & (1U<<(bit))) {                         \
        CPUWriteMemory(address, bus.reg[(num)].I);          \
	int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
	clockTicks += 1 + dataticks_value; \
        bus.reg[base].I = temp;                             \
        count++;                                        \
        address += 4;                                   \
    }
#define LDM_REG(bit,num) \
    if (opcode & (1U<<(bit))) {                         \
	int dataticks_value; \
        bus.reg[(num)].I = CPUReadMemory(address); \
	dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
	clockTicks += 1 + dataticks_value; \
        count++;                                        \
        address += 4;                                   \
    }
#define STM_LOW(STORE_REG) \
    STORE_REG(0, 0);                                    \
    STORE_REG(1, 1);                                    \
    STORE_REG(2, 2);                                    \
    STORE_REG(3, 3);                                    \
    STORE_REG(4, 4);                                    \
    STORE_REG(5, 5);                                    \
    STORE_REG(6, 6);                                    \
    STORE_REG(7, 7);
#define STM_HIGH(STORE_REG) \
    STORE_REG(8, 8);                                    \
    STORE_REG(9, 9);                                    \
    STORE_REG(10, 10);                                  \
    STORE_REG(11, 11);                                  \
    STORE_REG(12, 12);                                  \
    STORE_REG(13, 13);                                  \
    STORE_REG(14, 14);
#define STM_HIGH_2(STORE_REG) \
    if (armMode == 0x11) {                              \
        STORE_REG(8, R8_FIQ);                           \
        STORE_REG(9, R9_FIQ);                           \
        STORE_REG(10, R10_FIQ);                         \
        STORE_REG(11, R11_FIQ);                         \
        STORE_REG(12, R12_FIQ);                         \
    } else {                                            \
        STORE_REG(8, 8);                                \
        STORE_REG(9, 9);                                \
        STORE_REG(10, 10);                              \
        STORE_REG(11, 11);                              \
        STORE_REG(12, 12);                              \
    }                                                   \
    if (armMode != 0x10 && armMode != 0x1F) {           \
        STORE_REG(13, R13_USR);                         \
        STORE_REG(14, R14_USR);                         \
    } else {                                            \
        STORE_REG(13, 13);                              \
        STORE_REG(14, 14);                              \
    }
#define STM_PC \
    if (opcode & (1U<<15)) {                            \
        CPUWriteMemory(address, bus.reg[15].I+4);           \
	int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
	clockTicks += 1 + dataticks_value; \
        count++;                                        \
    }
#define STMW_PC \
    if (opcode & (1U<<15)) {                            \
        CPUWriteMemory(address, bus.reg[15].I+4);           \
	int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
	clockTicks += 1 + dataticks_value; \
        bus.reg[base].I = temp;                             \
        count++;                                        \
    }
#define LDM_LOW \
    LDM_REG(0, 0);                                      \
    LDM_REG(1, 1);                                      \
    LDM_REG(2, 2);                                      \
    LDM_REG(3, 3);                                      \
    LDM_REG(4, 4);                                      \
    LDM_REG(5, 5);                                      \
    LDM_REG(6, 6);                                      \
    LDM_REG(7, 7);
#define LDM_HIGH \
    LDM_REG(8, 8);                                      \
    LDM_REG(9, 9);                                      \
    LDM_REG(10, 10);                                    \
    LDM_REG(11, 11);                                    \
    LDM_REG(12, 12);                                    \
    LDM_REG(13, 13);                                    \
    LDM_REG(14, 14);
#define LDM_HIGH_2 \
    if (armMode == 0x11) {                              \
        LDM_REG(8, R8_FIQ);                             \
        LDM_REG(9, R9_FIQ);                             \
        LDM_REG(10, R10_FIQ);                           \
        LDM_REG(11, R11_FIQ);                           \
        LDM_REG(12, R12_FIQ);                           \
    } else {                                            \
        LDM_REG(8, 8);                                  \
        LDM_REG(9, 9);                                  \
        LDM_REG(10, 10);                                \
        LDM_REG(11, 11);                                \
        LDM_REG(12, 12);                                \
    }                                                   \
    if (armMode != 0x10 && armMode != 0x1F) {           \
        LDM_REG(13, R13_USR);                           \
        LDM_REG(14, R14_USR);                           \
    } else {                                            \
        LDM_REG(13, 13);                                \
        LDM_REG(14, 14);                                \
    }
#define STM_ALL \
    STM_LOW(STM_REG);                                   \
    STM_HIGH(STM_REG);                                  \
    STM_PC;
#define STMW_ALL \
    STM_LOW(STMW_REG);                                  \
    STM_HIGH(STMW_REG);                                 \
    STMW_PC;
#define LDM_ALL \
    LDM_LOW;                                            \
    LDM_HIGH;                                           \
    if (opcode & (1U<<15)) {                            \
        bus.reg[15].I = CPUReadMemory(address);             \
	int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
	clockTicks += 1 + dataticks_value; \
        count++;                                        \
    }                                                   \
    if (opcode & (1U<<15)) {                            \
        bus.armNextPC = bus.reg[15].I;                          \
        bus.reg[15].I += 4;                                 \
        ARM_PREFETCH;                                   \
        clockTicks += 1 + codeTicksAccessSeq32(bus.armNextPC);\
    }
#define STM_ALL_2 \
    STM_LOW(STM_REG);                                   \
    STM_HIGH_2(STM_REG);                                \
    STM_PC;
#define STMW_ALL_2 \
    STM_LOW(STMW_REG);                                  \
    STM_HIGH_2(STMW_REG);                               \
    STMW_PC;
#define LDM_ALL_2 \
    LDM_LOW;                                            \
    if (opcode & (1U<<15)) {                            \
        LDM_HIGH;                                       \
        bus.reg[15].I = CPUReadMemory(address);             \
	int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
	clockTicks += 1 + dataticks_value; \
        count++;                                        \
    } else {                                            \
        LDM_HIGH_2;                                     \
    }
#define LDM_ALL_2B \
    if (opcode & (1U<<15)) {                            \
	if(armMode != (bus.reg[17].I & 0x1F)) \
	    CPUSwitchMode(bus.reg[17].I & 0x1F, false, true);   \
        if (armState) {                                 \
            bus.armNextPC = bus.reg[15].I & 0xFFFFFFFC;         \
            bus.reg[15].I = bus.armNextPC + 4;                  \
            ARM_PREFETCH;                               \
        } else {                                        \
            bus.armNextPC = bus.reg[15].I & 0xFFFFFFFE;         \
            bus.reg[15].I = bus.armNextPC + 2;                  \
            THUMB_PREFETCH;                             \
        }                                               \
        clockTicks += 1 + codeTicksAccessSeq32(bus.armNextPC);\
    }


// STMDA Rn, {Rlist}
 void arm800(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (temp + 4) & 0xFFFFFFFC;
    int count = 0;
    STM_ALL;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMDA Rn, {Rlist}
 void arm810(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (temp + 4) & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMDA Rn!, {Rlist}
 void arm820(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (temp+4) & 0xFFFFFFFC;
    int count = 0;
    STMW_ALL;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMDA Rn!, {Rlist}
 void arm830(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (temp + 4) & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
    if (!(opcode & (1U << base)))
        bus.reg[base].I = temp;
}

// STMDA Rn, {Rlist}^
 void arm840(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (temp+4) & 0xFFFFFFFC;
    int count = 0;
    STM_ALL_2;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMDA Rn, {Rlist}^
 void arm850(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (temp + 4) & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL_2;
    LDM_ALL_2B;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMDA Rn!, {Rlist}^
 void arm860(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (temp+4) & 0xFFFFFFFC;
    int count = 0;
    STMW_ALL_2;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMDA Rn!, {Rlist}^
 void arm870(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (temp + 4) & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL_2;
    if (!(opcode & (1U << base)))
        bus.reg[base].I = temp;
    LDM_ALL_2B;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMIA Rn, {Rlist}
 void arm880(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = bus.reg[base].I & 0xFFFFFFFC;
    int count = 0;
    STM_ALL;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMIA Rn, {Rlist}
 void arm890(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = bus.reg[base].I & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMIA Rn!, {Rlist}
 void arm8A0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = bus.reg[base].I & 0xFFFFFFFC;
    int count = 0;
    u32 temp = bus.reg[base].I +
        4 * (cpuBitsSet[opcode & 0xFF] + cpuBitsSet[(opcode >> 8) & 255]);
    STMW_ALL;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMIA Rn!, {Rlist}
 void arm8B0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I +
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = bus.reg[base].I & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
    if (!(opcode & (1U << base)))
        bus.reg[base].I = temp;
}

// STMIA Rn, {Rlist}^
 void arm8C0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = bus.reg[base].I & 0xFFFFFFFC;
    int count = 0;
    STM_ALL_2;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMIA Rn, {Rlist}^
 void arm8D0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = bus.reg[base].I & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL_2;
    LDM_ALL_2B;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMIA Rn!, {Rlist}^
 void arm8E0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = bus.reg[base].I & 0xFFFFFFFC;
    int count = 0;
    u32 temp = bus.reg[base].I +
        4 * (cpuBitsSet[opcode & 0xFF] + cpuBitsSet[(opcode >> 8) & 255]);
    STMW_ALL_2;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMIA Rn!, {Rlist}^
 void arm8F0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I +
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = bus.reg[base].I & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL_2;
    if (!(opcode & (1U << base)))
        bus.reg[base].I = temp;
    LDM_ALL_2B;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMDB Rn, {Rlist}
 void arm900(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = temp & 0xFFFFFFFC;
    int count = 0;
    STM_ALL;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMDB Rn, {Rlist}
 void arm910(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = temp & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMDB Rn!, {Rlist}
 void arm920(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = temp & 0xFFFFFFFC;
    int count = 0;
    STMW_ALL;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMDB Rn!, {Rlist}
 void arm930(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = temp & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
    if (!(opcode & (1U << base)))
        bus.reg[base].I = temp;
}

// STMDB Rn, {Rlist}^
 void arm940(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = temp & 0xFFFFFFFC;
    int count = 0;
    STM_ALL_2;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMDB Rn, {Rlist}^
 void arm950(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = temp & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL_2;
    LDM_ALL_2B;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMDB Rn!, {Rlist}^
 void arm960(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = temp & 0xFFFFFFFC;
    int count = 0;
    STMW_ALL_2;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMDB Rn!, {Rlist}^
 void arm970(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I -
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = temp & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL_2;
    if (!(opcode & (1U << base)))
        bus.reg[base].I = temp;
    LDM_ALL_2B;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMIB Rn, {Rlist}
 void arm980(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = (bus.reg[base].I+4) & 0xFFFFFFFC;
    int count = 0;
    STM_ALL;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMIB Rn, {Rlist}
 void arm990(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = (bus.reg[base].I+4) & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMIB Rn!, {Rlist}
 void arm9A0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = (bus.reg[base].I+4) & 0xFFFFFFFC;
    int count = 0;
    u32 temp = bus.reg[base].I +
        4 * (cpuBitsSet[opcode & 0xFF] + cpuBitsSet[(opcode >> 8) & 255]);
    STMW_ALL;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMIB Rn!, {Rlist}
 void arm9B0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 temp = bus.reg[base].I +
        4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
    u32 address = (bus.reg[base].I+4) & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
    if (!(opcode & (1U << base)))
        bus.reg[base].I = temp;
}

// STMIB Rn, {Rlist}^
 void arm9C0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = (bus.reg[base].I+4) & 0xFFFFFFFC;
    int count = 0;
    STM_ALL_2;
    clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMIB Rn, {Rlist}^
 void arm9D0(u32 opcode)
{
    if (bus.busPrefetchCount == 0)
        bus.busPrefetch = bus.busPrefetchEnable;
    int base = (opcode & 0x000F0000) >> 16;
    u32 address = (bus.reg[base].I+4) & 0xFFFFFFFC;
    int count = 0;
    LDM_ALL_2;
    LDM_ALL_2B;
    clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// STMIB Rn!, {Rlist}^
 void arm9E0(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	int base = (opcode & 0x000F0000) >> 16;
	u32 address = (bus.reg[base].I+4) & 0xFFFFFFFC;
	int count = 0;
	u32 temp = bus.reg[base].I +
		4 * (cpuBitsSet[opcode & 0xFF] + cpuBitsSet[(opcode >> 8) & 255]);
	STMW_ALL_2;
	clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// LDMIB Rn!, {Rlist}^
 void arm9F0(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	int base = (opcode & 0x000F0000) >> 16;
	u32 temp = bus.reg[base].I +
		4 * (cpuBitsSet[opcode & 255] + cpuBitsSet[(opcode >> 8) & 255]);
	u32 address = (bus.reg[base].I+4) & 0xFFFFFFFC;
	int count = 0;
	LDM_ALL_2;
	if (!(opcode & (1U << base)))
		bus.reg[base].I = temp;
	LDM_ALL_2B;
	clockTicks += 2 + codeTicksAccess(bus.armNextPC, BITS_32);
}

// B/BL/SWI and (unimplemented) coproc support ////////////////////////////

// B <offset>
 void armA00(u32 opcode)
{
	int codeTicksVal = 0;
	int ct = 0;
	int offset = opcode & 0x00FFFFFF;
	if (offset & 0x00800000)
		offset |= 0xFF000000;  // negative offset
	bus.reg[15].I += offset<<2;
	bus.armNextPC = bus.reg[15].I;
	bus.reg[15].I += 4;
	ARM_PREFETCH;

	codeTicksVal = codeTicksAccessSeq32(bus.armNextPC);
	ct = codeTicksVal + 3;
	ct += 2 + codeTicksAccess(bus.armNextPC, BITS_32) + codeTicksVal;

	bus.busPrefetchCount = 0;
	clockTicks = ct;
}

// BL <offset>
 void armB00(u32 opcode)
{
	int codeTicksVal = 0;
	int ct = 0;

	int offset = opcode & 0x00FFFFFF;
	if (offset & 0x00800000)
		offset |= 0xFF000000;  // negative offset
	bus.reg[14].I = bus.reg[15].I - 4;
	bus.reg[15].I += offset<<2;
	bus.armNextPC = bus.reg[15].I;
	bus.reg[15].I += 4;
	ARM_PREFETCH;

	codeTicksVal = codeTicksAccessSeq32(bus.armNextPC);
	ct = codeTicksVal + 3;
	ct += 2 + codeTicksAccess(bus.armNextPC, BITS_32) + codeTicksVal;

	bus.busPrefetchCount = 0;
	clockTicks = ct;
}

#define armE01 armUnknownInsn

// SWI <comment>
 void armF00(u32 opcode)
{
	int codeTicksVal = 0;
	int ct = 0;

	codeTicksVal = codeTicksAccessSeq32(bus.armNextPC);
	ct = codeTicksVal + 3;
	ct += 2 + codeTicksAccess(bus.armNextPC, BITS_32) + codeTicksVal;

	bus.busPrefetchCount = 0;

	clockTicks = ct;
	CPUSoftwareInterrupt(opcode & 0x00FFFFFF);

}

// Instruction table //////////////////////////////////////////////////////

static void (Gigazoid::*const armInsnTable[4096])(u32 opcode);

// Wrapper routine (execution loop) ///////////////////////////////////////
int armExecute (void)
{
	CACHE_PREFETCH(clockTicks);

	u32 cond1;
	u32 cond2;

	int ct = 0;

	do
	{

		clockTicks = 0;

		if ((bus.armNextPC & 0x0803FFFF) == 0x08020000)
			bus.busPrefetchCount = 0x100;

		u32 opcode = cpuPrefetch[0];
		cpuPrefetch[0] = cpuPrefetch[1];

		bus.busPrefetch = false;
		int32_t busprefetch_mask = ((bus.busPrefetchCount & 0xFFFFFE00) | -(bus.busPrefetchCount & 0xFFFFFE00)) >> 31;
		bus.busPrefetchCount = (0x100 | (bus.busPrefetchCount & 0xFF) & busprefetch_mask) | (bus.busPrefetchCount & ~busprefetch_mask);
#if 0
		if (bus.busPrefetchCount & 0xFFFFFE00)
			bus.busPrefetchCount = 0x100 | (bus.busPrefetchCount & 0xFF);
#endif


		int oldArmNextPC = bus.armNextPC;

		bus.armNextPC = bus.reg[15].I;
		if (traceCallback)
			traceCallback(bus.armNextPC, opcode);
		if (fetchCallback)
			fetchCallback(bus.armNextPC);
		bus.reg[15].I += 4;
		ARM_PREFETCH_NEXT;

		int cond = opcode >> 28;
		bool cond_res = true;
		if (cond != 0x0E) {  // most opcodes are AL (always)
			switch(cond) {
				case 0x00: // EQ
					cond_res = Z_FLAG;
					break;
				case 0x01: // NE
					cond_res = !Z_FLAG;
					break;
				case 0x02: // CS
					cond_res = C_FLAG;
					break;
				case 0x03: // CC
					cond_res = !C_FLAG;
					break;
				case 0x04: // MI
					cond_res = N_FLAG;
					break;
				case 0x05: // PL
					cond_res = !N_FLAG;
					break;
				case 0x06: // VS
					cond_res = V_FLAG;
					break;
				case 0x07: // VC
					cond_res = !V_FLAG;
					break;
				case 0x08: // HI
					cond_res = C_FLAG && !Z_FLAG;
					break;
				case 0x09: // LS
					cond_res = !C_FLAG || Z_FLAG;
					break;
				case 0x0A: // GE
					cond_res = N_FLAG == V_FLAG;
					break;
				case 0x0B: // LT
					cond_res = N_FLAG != V_FLAG;
					break;
				case 0x0C: // GT
					cond_res = !Z_FLAG &&(N_FLAG == V_FLAG);
					break;
				case 0x0D: // LE
					cond_res = Z_FLAG || (N_FLAG != V_FLAG);
					break;
				case 0x0E: // AL (impossible, checked above)
					cond_res = true;
					break;
				case 0x0F:
				default:
					// ???
					cond_res = false;
					break;
			}
		}

		if (cond_res)
		{
			cond1 = (opcode>>16)&0xFF0;
			cond2 = (opcode>>4)&0x0F;

			(this->*armInsnTable[(cond1| cond2)])(opcode);

		}
		ct = clockTicks;

		if (ct < 0)
			return 0;

		/// better pipelining

		if (ct == 0)
			clockTicks = 1 + codeTicksAccessSeq32(oldArmNextPC);

		cpuTotalTicks += clockTicks;

} while ((cpuTotalTicks < cpuNextEvent) & armState & ~holdState);
	return 1;
}


/*============================================================
	GBA THUMB CORE
============================================================ */

 void thumbUnknownInsn(u32 opcode)
{
	u32 PC = bus.reg[15].I;
	bool savedArmState = armState;
	if(armMode != 0x1b)
		CPUSwitchMode(0x1b, true, false);
	bus.reg[14].I = PC - (savedArmState ? 4 : 2);
	bus.reg[15].I = 0x04;
	armState = true;
	armIrqEnable = false;
	bus.armNextPC = 0x04;
	ARM_PREFETCH;
	bus.reg[15].I += 4;
}

#define NEG(i) ((i) >> 31)
#define POS(i) ((~(i)) >> 31)

// C core
#ifndef ADDCARRY
 #define ADDCARRY(a, b, c) \
  C_FLAG = ((NEG(a) & NEG(b)) |\
            (NEG(a) & POS(c)) |\
            (NEG(b) & POS(c))) ? true : false;
#endif

#ifndef ADDOVERFLOW
 #define ADDOVERFLOW(a, b, c) \
  V_FLAG = ((NEG(a) & NEG(b) & POS(c)) |\
            (POS(a) & POS(b) & NEG(c))) ? true : false;
#endif

#ifndef SUBCARRY
 #define SUBCARRY(a, b, c) \
  C_FLAG = ((NEG(a) & POS(b)) |\
            (NEG(a) & POS(c)) |\
            (POS(b) & POS(c))) ? true : false;
#endif

#ifndef SUBOVERFLOW
 #define SUBOVERFLOW(a, b, c)\
  V_FLAG = ((NEG(a) & POS(b) & POS(c)) |\
            (POS(a) & NEG(b) & NEG(c))) ? true : false;
#endif

#ifndef ADD_RD_RS_RN
 #define ADD_RD_RS_RN(N) \
   {\
     u32 lhs = bus.reg[source].I;\
     u32 rhs = bus.reg[N].I;\
     u32 res = lhs + rhs;\
     bus.reg[dest].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     ADDCARRY(lhs, rhs, res);\
     ADDOVERFLOW(lhs, rhs, res);\
   }
#endif

#ifndef ADD_RD_RS_O3
 #define ADD_RD_RS_O3(N) \
   {\
     u32 lhs = bus.reg[source].I;\
     u32 rhs = N;\
     u32 res = lhs + rhs;\
     bus.reg[dest].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     ADDCARRY(lhs, rhs, res);\
     ADDOVERFLOW(lhs, rhs, res);\
   }
#endif

#ifndef ADD_RD_RS_O3_0
# define ADD_RD_RS_O3_0 ADD_RD_RS_O3
#endif

#ifndef ADD_RN_O8
 #define ADD_RN_O8(d) \
   {\
     u32 lhs = bus.reg[(d)].I;\
     u32 rhs = (opcode & 255);\
     u32 res = lhs + rhs;\
     bus.reg[(d)].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     ADDCARRY(lhs, rhs, res);\
     ADDOVERFLOW(lhs, rhs, res);\
   }
#endif

#ifndef CMN_RD_RS
 #define CMN_RD_RS \
   {\
     u32 lhs = bus.reg[dest].I;\
     u32 rhs = value;\
     u32 res = lhs + rhs;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     ADDCARRY(lhs, rhs, res);\
     ADDOVERFLOW(lhs, rhs, res);\
   }
#endif

#ifndef ADC_RD_RS
 #define ADC_RD_RS \
   {\
     u32 lhs = bus.reg[dest].I;\
     u32 rhs = value;\
     u32 res = lhs + rhs + (u32)C_FLAG;\
     bus.reg[dest].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     ADDCARRY(lhs, rhs, res);\
     ADDOVERFLOW(lhs, rhs, res);\
   }
#endif

#ifndef SUB_RD_RS_RN
 #define SUB_RD_RS_RN(N) \
   {\
     u32 lhs = bus.reg[source].I;\
     u32 rhs = bus.reg[N].I;\
     u32 res = lhs - rhs;\
     bus.reg[dest].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     SUBCARRY(lhs, rhs, res);\
     SUBOVERFLOW(lhs, rhs, res);\
   }
#endif

#ifndef SUB_RD_RS_O3
 #define SUB_RD_RS_O3(N) \
   {\
     u32 lhs = bus.reg[source].I;\
     u32 rhs = N;\
     u32 res = lhs - rhs;\
     bus.reg[dest].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     SUBCARRY(lhs, rhs, res);\
     SUBOVERFLOW(lhs, rhs, res);\
   }
#endif

#ifndef SUB_RD_RS_O3_0
# define SUB_RD_RS_O3_0 SUB_RD_RS_O3
#endif
#ifndef SUB_RN_O8
 #define SUB_RN_O8(d) \
   {\
     u32 lhs = bus.reg[(d)].I;\
     u32 rhs = (opcode & 255);\
     u32 res = lhs - rhs;\
     bus.reg[(d)].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     SUBCARRY(lhs, rhs, res);\
     SUBOVERFLOW(lhs, rhs, res);\
   }
#endif
#ifndef MOV_RN_O8
 #define MOV_RN_O8(d) \
   {\
     u32 val;\
	 val = (opcode & 255);\
     bus.reg[d].I = val;\
     N_FLAG = false;\
     Z_FLAG = (val ? false : true);\
   }
#endif
#ifndef CMP_RN_O8
 #define CMP_RN_O8(d) \
   {\
     u32 lhs = bus.reg[(d)].I;\
     u32 rhs = (opcode & 255);\
     u32 res = lhs - rhs;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     SUBCARRY(lhs, rhs, res);\
     SUBOVERFLOW(lhs, rhs, res);\
   }
#endif
#ifndef SBC_RD_RS
 #define SBC_RD_RS \
   {\
     u32 lhs = bus.reg[dest].I;\
     u32 rhs = value;\
     u32 res = lhs - rhs - !((u32)C_FLAG);\
     bus.reg[dest].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     SUBCARRY(lhs, rhs, res);\
     SUBOVERFLOW(lhs, rhs, res);\
   }
#endif
#ifndef LSL_RD_RM_I5
 #define LSL_RD_RM_I5 \
   {\
     C_FLAG = (bus.reg[source].I >> (32 - shift)) & 1 ? true : false;\
     value = bus.reg[source].I << shift;\
   }
#endif
#ifndef LSL_RD_RS
 #define LSL_RD_RS \
   {\
     C_FLAG = (bus.reg[dest].I >> (32 - value)) & 1 ? true : false;\
     value = bus.reg[dest].I << value;\
   }
#endif
#ifndef LSR_RD_RM_I5
 #define LSR_RD_RM_I5 \
   {\
     C_FLAG = (bus.reg[source].I >> (shift - 1)) & 1 ? true : false;\
     value = bus.reg[source].I >> shift;\
   }
#endif
#ifndef LSR_RD_RS
 #define LSR_RD_RS \
   {\
     C_FLAG = (bus.reg[dest].I >> (value - 1)) & 1 ? true : false;\
     value = bus.reg[dest].I >> value;\
   }
#endif
#ifndef ASR_RD_RM_I5
 #define ASR_RD_RM_I5 \
   {\
     C_FLAG = ((s32)bus.reg[source].I >> (int)(shift - 1)) & 1 ? true : false;\
     value = (s32)bus.reg[source].I >> (int)shift;\
   }
#endif
#ifndef ASR_RD_RS
 #define ASR_RD_RS \
   {\
     C_FLAG = ((s32)bus.reg[dest].I >> (int)(value - 1)) & 1 ? true : false;\
     value = (s32)bus.reg[dest].I >> (int)value;\
   }
#endif
#ifndef ROR_RD_RS
 #define ROR_RD_RS \
   {\
     C_FLAG = (bus.reg[dest].I >> (value - 1)) & 1 ? true : false;\
     value = ((bus.reg[dest].I << (32 - value)) |\
              (bus.reg[dest].I >> value));\
   }
#endif
#ifndef NEG_RD_RS
 #define NEG_RD_RS \
   {\
     u32 lhs = bus.reg[source].I;\
     u32 rhs = 0;\
     u32 res = rhs - lhs;\
     bus.reg[dest].I = res;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     SUBCARRY(rhs, lhs, res);\
     SUBOVERFLOW(rhs, lhs, res);\
   }
#endif
#ifndef CMP_RD_RS
 #define CMP_RD_RS \
   {\
     u32 lhs = bus.reg[dest].I;\
     u32 rhs = value;\
     u32 res = lhs - rhs;\
     Z_FLAG = (res == 0) ? true : false;\
     N_FLAG = NEG(res) ? true : false;\
     SUBCARRY(lhs, rhs, res);\
     SUBOVERFLOW(lhs, rhs, res);\
   }
#endif
#ifndef IMM5_INSN
 #define IMM5_INSN(OP,N) \
  int dest = opcode & 0x07;\
  int source = (opcode >> 3) & 0x07;\
  u32 value;\
  OP(N);\
  bus.reg[dest].I = value;\
  N_FLAG = (value & 0x80000000 ? true : false);\
  Z_FLAG = (value ? false : true);
 #define IMM5_INSN_0(OP) \
  int dest = opcode & 0x07;\
  int source = (opcode >> 3) & 0x07;\
  u32 value;\
  OP;\
  bus.reg[dest].I = value;\
  N_FLAG = (value & 0x80000000 ? true : false);\
  Z_FLAG = (value ? false : true);
 #define IMM5_LSL(N) \
  int shift = N;\
  LSL_RD_RM_I5;
 #define IMM5_LSL_0 \
  value = bus.reg[source].I;
 #define IMM5_LSR(N) \
  int shift = N;\
  LSR_RD_RM_I5;
 #define IMM5_LSR_0 \
  C_FLAG = bus.reg[source].I & 0x80000000 ? true : false;\
  value = 0;
 #define IMM5_ASR(N) \
  int shift = N;\
  ASR_RD_RM_I5;
 #define IMM5_ASR_0 \
  if(bus.reg[source].I & 0x80000000) {\
    value = 0xFFFFFFFF;\
    C_FLAG = true;\
  } else {\
    value = 0;\
    C_FLAG = false;\
  }
#endif
#ifndef THREEARG_INSN
 #define THREEARG_INSN(OP,N) \
  int dest = opcode & 0x07;          \
  int source = (opcode >> 3) & 0x07; \
  OP(N);
#endif

// Shift instructions /////////////////////////////////////////////////////

#define DEFINE_IMM5_INSN(OP,BASE) \
   void thumb##BASE##_00(u32 opcode) { IMM5_INSN_0(OP##_0); } \
   void thumb##BASE##_01(u32 opcode) { IMM5_INSN(OP, 1); } \
   void thumb##BASE##_02(u32 opcode) { IMM5_INSN(OP, 2); } \
   void thumb##BASE##_03(u32 opcode) { IMM5_INSN(OP, 3); } \
   void thumb##BASE##_04(u32 opcode) { IMM5_INSN(OP, 4); } \
   void thumb##BASE##_05(u32 opcode) { IMM5_INSN(OP, 5); } \
   void thumb##BASE##_06(u32 opcode) { IMM5_INSN(OP, 6); } \
   void thumb##BASE##_07(u32 opcode) { IMM5_INSN(OP, 7); } \
   void thumb##BASE##_08(u32 opcode) { IMM5_INSN(OP, 8); } \
   void thumb##BASE##_09(u32 opcode) { IMM5_INSN(OP, 9); } \
   void thumb##BASE##_0A(u32 opcode) { IMM5_INSN(OP,10); } \
   void thumb##BASE##_0B(u32 opcode) { IMM5_INSN(OP,11); } \
   void thumb##BASE##_0C(u32 opcode) { IMM5_INSN(OP,12); } \
   void thumb##BASE##_0D(u32 opcode) { IMM5_INSN(OP,13); } \
   void thumb##BASE##_0E(u32 opcode) { IMM5_INSN(OP,14); } \
   void thumb##BASE##_0F(u32 opcode) { IMM5_INSN(OP,15); } \
   void thumb##BASE##_10(u32 opcode) { IMM5_INSN(OP,16); } \
   void thumb##BASE##_11(u32 opcode) { IMM5_INSN(OP,17); } \
   void thumb##BASE##_12(u32 opcode) { IMM5_INSN(OP,18); } \
   void thumb##BASE##_13(u32 opcode) { IMM5_INSN(OP,19); } \
   void thumb##BASE##_14(u32 opcode) { IMM5_INSN(OP,20); } \
   void thumb##BASE##_15(u32 opcode) { IMM5_INSN(OP,21); } \
   void thumb##BASE##_16(u32 opcode) { IMM5_INSN(OP,22); } \
   void thumb##BASE##_17(u32 opcode) { IMM5_INSN(OP,23); } \
   void thumb##BASE##_18(u32 opcode) { IMM5_INSN(OP,24); } \
   void thumb##BASE##_19(u32 opcode) { IMM5_INSN(OP,25); } \
   void thumb##BASE##_1A(u32 opcode) { IMM5_INSN(OP,26); } \
   void thumb##BASE##_1B(u32 opcode) { IMM5_INSN(OP,27); } \
   void thumb##BASE##_1C(u32 opcode) { IMM5_INSN(OP,28); } \
   void thumb##BASE##_1D(u32 opcode) { IMM5_INSN(OP,29); } \
   void thumb##BASE##_1E(u32 opcode) { IMM5_INSN(OP,30); } \
   void thumb##BASE##_1F(u32 opcode) { IMM5_INSN(OP,31); }

// LSL Rd, Rm, #Imm 5
DEFINE_IMM5_INSN(IMM5_LSL,00)
// LSR Rd, Rm, #Imm 5
DEFINE_IMM5_INSN(IMM5_LSR,08)
// ASR Rd, Rm, #Imm 5
DEFINE_IMM5_INSN(IMM5_ASR,10)

// 3-argument ADD/SUB /////////////////////////////////////////////////////

#define DEFINE_REG3_INSN(OP,BASE) \
   void thumb##BASE##_0(u32 opcode) { THREEARG_INSN(OP,0); } \
   void thumb##BASE##_1(u32 opcode) { THREEARG_INSN(OP,1); } \
   void thumb##BASE##_2(u32 opcode) { THREEARG_INSN(OP,2); } \
   void thumb##BASE##_3(u32 opcode) { THREEARG_INSN(OP,3); } \
   void thumb##BASE##_4(u32 opcode) { THREEARG_INSN(OP,4); } \
   void thumb##BASE##_5(u32 opcode) { THREEARG_INSN(OP,5); } \
   void thumb##BASE##_6(u32 opcode) { THREEARG_INSN(OP,6); } \
   void thumb##BASE##_7(u32 opcode) { THREEARG_INSN(OP,7); }

#define DEFINE_IMM3_INSN(OP,BASE) \
   void thumb##BASE##_0(u32 opcode) { THREEARG_INSN(OP##_0,0); } \
   void thumb##BASE##_1(u32 opcode) { THREEARG_INSN(OP,1); } \
   void thumb##BASE##_2(u32 opcode) { THREEARG_INSN(OP,2); } \
   void thumb##BASE##_3(u32 opcode) { THREEARG_INSN(OP,3); } \
   void thumb##BASE##_4(u32 opcode) { THREEARG_INSN(OP,4); } \
   void thumb##BASE##_5(u32 opcode) { THREEARG_INSN(OP,5); } \
   void thumb##BASE##_6(u32 opcode) { THREEARG_INSN(OP,6); } \
   void thumb##BASE##_7(u32 opcode) { THREEARG_INSN(OP,7); }

// ADD Rd, Rs, Rn
DEFINE_REG3_INSN(ADD_RD_RS_RN,18)
// SUB Rd, Rs, Rn
DEFINE_REG3_INSN(SUB_RD_RS_RN,1A)
// ADD Rd, Rs, #Offset3
DEFINE_IMM3_INSN(ADD_RD_RS_O3,1C)
// SUB Rd, Rs, #Offset3
DEFINE_IMM3_INSN(SUB_RD_RS_O3,1E)

// MOV/CMP/ADD/SUB immediate //////////////////////////////////////////////

// MOV R0, #Offset8
 void thumb20(u32 opcode) { MOV_RN_O8(0); }
// MOV R1, #Offset8
 void thumb21(u32 opcode) { MOV_RN_O8(1); }
// MOV R2, #Offset8
 void thumb22(u32 opcode) { MOV_RN_O8(2); }
// MOV R3, #Offset8
 void thumb23(u32 opcode) { MOV_RN_O8(3); }
// MOV R4, #Offset8
 void thumb24(u32 opcode) { MOV_RN_O8(4); }
// MOV R5, #Offset8
 void thumb25(u32 opcode) { MOV_RN_O8(5); }
// MOV R6, #Offset8
 void thumb26(u32 opcode) { MOV_RN_O8(6); }
// MOV R7, #Offset8
 void thumb27(u32 opcode) { MOV_RN_O8(7); }

// CMP R0, #Offset8
 void thumb28(u32 opcode) { CMP_RN_O8(0); }
// CMP R1, #Offset8
 void thumb29(u32 opcode) { CMP_RN_O8(1); }
// CMP R2, #Offset8
 void thumb2A(u32 opcode) { CMP_RN_O8(2); }
// CMP R3, #Offset8
 void thumb2B(u32 opcode) { CMP_RN_O8(3); }
// CMP R4, #Offset8
 void thumb2C(u32 opcode) { CMP_RN_O8(4); }
// CMP R5, #Offset8
 void thumb2D(u32 opcode) { CMP_RN_O8(5); }
// CMP R6, #Offset8
 void thumb2E(u32 opcode) { CMP_RN_O8(6); }
// CMP R7, #Offset8
 void thumb2F(u32 opcode) { CMP_RN_O8(7); }

// ADD R0,#Offset8
 void thumb30(u32 opcode) { ADD_RN_O8(0); }
// ADD R1,#Offset8
 void thumb31(u32 opcode) { ADD_RN_O8(1); }
// ADD R2,#Offset8
 void thumb32(u32 opcode) { ADD_RN_O8(2); }
// ADD R3,#Offset8
 void thumb33(u32 opcode) { ADD_RN_O8(3); }
// ADD R4,#Offset8
 void thumb34(u32 opcode) { ADD_RN_O8(4); }
// ADD R5,#Offset8
 void thumb35(u32 opcode) { ADD_RN_O8(5); }
// ADD R6,#Offset8
 void thumb36(u32 opcode) { ADD_RN_O8(6); }
// ADD R7,#Offset8
 void thumb37(u32 opcode) { ADD_RN_O8(7); }

// SUB R0,#Offset8
 void thumb38(u32 opcode) { SUB_RN_O8(0); }
// SUB R1,#Offset8
 void thumb39(u32 opcode) { SUB_RN_O8(1); }
// SUB R2,#Offset8
 void thumb3A(u32 opcode) { SUB_RN_O8(2); }
// SUB R3,#Offset8
 void thumb3B(u32 opcode) { SUB_RN_O8(3); }
// SUB R4,#Offset8
 void thumb3C(u32 opcode) { SUB_RN_O8(4); }
// SUB R5,#Offset8
 void thumb3D(u32 opcode) { SUB_RN_O8(5); }
// SUB R6,#Offset8
 void thumb3E(u32 opcode) { SUB_RN_O8(6); }
// SUB R7,#Offset8
 void thumb3F(u32 opcode) { SUB_RN_O8(7); }

// ALU operations /////////////////////////////////////////////////////////

// AND Rd, Rs
 void thumb40_0(u32 opcode)
{
  int dest = opcode & 7;
  u32 val = (bus.reg[dest].I & bus.reg[(opcode >> 3)&7].I);
  
  //bus.reg[dest].I &= bus.reg[(opcode >> 3)&7].I;
  N_FLAG = val & 0x80000000 ? true : false;
  Z_FLAG = val ? false : true;

  bus.reg[dest].I = val;

}

// EOR Rd, Rs
 void thumb40_1(u32 opcode)
{
  int dest = opcode & 7;
  bus.reg[dest].I ^= bus.reg[(opcode >> 3)&7].I;
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
  Z_FLAG = bus.reg[dest].I ? false : true;
}

// LSL Rd, Rs
 void thumb40_2(u32 opcode)
{
  int dest = opcode & 7;
  u32 value = bus.reg[(opcode >> 3)&7].B.B0;
  u32 val = value;
  if(val) {
    if(val == 32) {
      value = 0;
      C_FLAG = (bus.reg[dest].I & 1 ? true : false);
    } else if(val < 32) {
      LSL_RD_RS;
    } else {
      value = 0;
      C_FLAG = false;
    }
    bus.reg[dest].I = value;
  }
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
  Z_FLAG = bus.reg[dest].I ? false : true;
  clockTicks = codeTicksAccess(bus.armNextPC, BITS_16)+2;
}

// LSR Rd, Rs
 void thumb40_3(u32 opcode)
{
  int dest = opcode & 7;
  u32 value = bus.reg[(opcode >> 3)&7].B.B0;
  u32 val = value;
  if(val) {
    if(val == 32) {
      value = 0;
      C_FLAG = (bus.reg[dest].I & 0x80000000 ? true : false);
    } else if(val < 32) {
      LSR_RD_RS;
    } else {
      value = 0;
      C_FLAG = false;
    }
    bus.reg[dest].I = value;
  }
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
  Z_FLAG = bus.reg[dest].I ? false : true;
  clockTicks = codeTicksAccess(bus.armNextPC, BITS_16)+2;
}

// ASR Rd, Rs
 void thumb41_0(u32 opcode)
{
  int dest = opcode & 7;
  u32 value = bus.reg[(opcode >> 3)&7].B.B0;
  
  if(value) {
    if(value < 32) {
      ASR_RD_RS;
      bus.reg[dest].I = value;
    } else {
      if(bus.reg[dest].I & 0x80000000){
        bus.reg[dest].I = 0xFFFFFFFF;
        C_FLAG = true;
      } else {
        bus.reg[dest].I = 0x00000000;
        C_FLAG = false;
      }
    }
  }
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
  Z_FLAG = bus.reg[dest].I ? false : true;
  clockTicks = codeTicksAccess(bus.armNextPC, BITS_16)+2;
}

// ADC Rd, Rs
 void thumb41_1(u32 opcode)
{
  int dest = opcode & 0x07;
  u32 value = bus.reg[(opcode >> 3)&7].I;
  ADC_RD_RS;
}

// SBC Rd, Rs
 void thumb41_2(u32 opcode)
{
  int dest = opcode & 0x07;
  u32 value = bus.reg[(opcode >> 3)&7].I;
  SBC_RD_RS;
}

// ROR Rd, Rs
 void thumb41_3(u32 opcode)
{
  int dest = opcode & 7;
  u32 value = bus.reg[(opcode >> 3)&7].B.B0;
  u32 val = value;
  if(val) {
    value = value & 0x1f;
    if(val == 0) {
      C_FLAG = (bus.reg[dest].I & 0x80000000 ? true : false);
    } else {
      ROR_RD_RS;
      bus.reg[dest].I = value;
    }
  }
  clockTicks = codeTicksAccess(bus.armNextPC, BITS_16)+2;
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
  Z_FLAG = bus.reg[dest].I ? false : true;
}

// TST Rd, Rs
 void thumb42_0(u32 opcode)
{
  u32 value = bus.reg[opcode & 7].I & bus.reg[(opcode >> 3) & 7].I;
  N_FLAG = value & 0x80000000 ? true : false;
  Z_FLAG = value ? false : true;
}

// NEG Rd, Rs
 void thumb42_1(u32 opcode)
{
  int dest = opcode & 7;
  int source = (opcode >> 3) & 7;
  NEG_RD_RS;
}

// CMP Rd, Rs
 void thumb42_2(u32 opcode)
{
  int dest = opcode & 7;
  u32 value = bus.reg[(opcode >> 3)&7].I;
  CMP_RD_RS;
}

// CMN Rd, Rs
 void thumb42_3(u32 opcode)
{
  int dest = opcode & 7;
  u32 value = bus.reg[(opcode >> 3)&7].I;
  CMN_RD_RS;
}

// ORR Rd, Rs
 void thumb43_0(u32 opcode)
{
  int dest = opcode & 7;
  bus.reg[dest].I |= bus.reg[(opcode >> 3) & 7].I;
  Z_FLAG = bus.reg[dest].I ? false : true;
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
}

// MUL Rd, Rs
 void thumb43_1(u32 opcode)
{
  clockTicks = 1;
  int dest = opcode & 7;
  u32 rm = bus.reg[dest].I;
  bus.reg[dest].I = bus.reg[(opcode >> 3) & 7].I * rm;
  if (((s32)rm) < 0)
    rm = ~rm;
  if ((rm & 0xFFFF0000) == 0)
    clockTicks += 1;
  else if ((rm & 0xFF000000) == 0)
    clockTicks += 2;
  else
    clockTicks += 3;
  bus.busPrefetchCount = (bus.busPrefetchCount<<clockTicks) | (0xFF>>(8-clockTicks));
  clockTicks += codeTicksAccess(bus.armNextPC, BITS_16) + 1;
  Z_FLAG = bus.reg[dest].I ? false : true;
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
}

// BIC Rd, Rs
 void thumb43_2(u32 opcode)
{
  int dest = opcode & 7;
  bus.reg[dest].I &= (~bus.reg[(opcode >> 3) & 7].I);
  Z_FLAG = bus.reg[dest].I ? false : true;
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
}

// MVN Rd, Rs
 void thumb43_3(u32 opcode)
{
  int dest = opcode & 7;
  bus.reg[dest].I = ~bus.reg[(opcode >> 3) & 7].I;
  Z_FLAG = bus.reg[dest].I ? false : true;
  N_FLAG = bus.reg[dest].I & 0x80000000 ? true : false;
}

// High-register instructions and BX //////////////////////////////////////

// ADD Rd, Hs
 void thumb44_1(u32 opcode)
{
  bus.reg[opcode&7].I += bus.reg[((opcode>>3)&7)+8].I;
}

// ADD Hd, Rs
 void thumb44_2(u32 opcode)
{
  bus.reg[(opcode&7)+8].I += bus.reg[(opcode>>3)&7].I;
  if((opcode&7) == 7) {
    bus.reg[15].I &= 0xFFFFFFFE;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = codeTicksAccessSeq16(bus.armNextPC)<<1
        + codeTicksAccess(bus.armNextPC, BITS_16) + 3;
  }
}

// ADD Hd, Hs
 void thumb44_3(u32 opcode)
{
  bus.reg[(opcode&7)+8].I += bus.reg[((opcode>>3)&7)+8].I;
  if((opcode&7) == 7) {
    bus.reg[15].I &= 0xFFFFFFFE;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = codeTicksAccessSeq16(bus.armNextPC)<<1
        + codeTicksAccess(bus.armNextPC, BITS_16) + 3;
  }
}

// CMP Rd, Hs
 void thumb45_1(u32 opcode)
{
  int dest = opcode & 7;
  u32 value = bus.reg[((opcode>>3)&7)+8].I;
  CMP_RD_RS;
}

// CMP Hd, Rs
 void thumb45_2(u32 opcode)
{
  int dest = (opcode & 7) + 8;
  u32 value = bus.reg[(opcode>>3)&7].I;
  CMP_RD_RS;
}

// CMP Hd, Hs
 void thumb45_3(u32 opcode)
{
  int dest = (opcode & 7) + 8;
  u32 value = bus.reg[((opcode>>3)&7)+8].I;
  CMP_RD_RS;
}

// MOV Rd, Hs
 void thumb46_1(u32 opcode)
{
  bus.reg[opcode&7].I = bus.reg[((opcode>>3)&7)+8].I;
}

// MOV Hd, Rs
 void thumb46_2(u32 opcode)
{
  bus.reg[(opcode&7)+8].I = bus.reg[(opcode>>3)&7].I;
  if((opcode&7) == 7) {
    bus.reg[15].I &= 0xFFFFFFFE;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = codeTicksAccessSeq16(bus.armNextPC)<<1
        + codeTicksAccess(bus.armNextPC, BITS_16) + 3;
  }
}

// MOV Hd, Hs
 void thumb46_3(u32 opcode)
{
  bus.reg[(opcode&7)+8].I = bus.reg[((opcode>>3)&7)+8].I;
  if((opcode&7) == 7) {
    bus.reg[15].I &= 0xFFFFFFFE;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = codeTicksAccessSeq16(bus.armNextPC)<<1
        + codeTicksAccess(bus.armNextPC, BITS_16) + 3;
  }
}


// BX Rs
 void thumb47(u32 opcode)
{
	int base = (opcode >> 3) & 15;
	bus.busPrefetchCount=0;
	bus.reg[15].I = bus.reg[base].I;
	if(bus.reg[base].I & 1) {
		armState = false;
		bus.reg[15].I &= 0xFFFFFFFE;
		bus.armNextPC = bus.reg[15].I;
		bus.reg[15].I += 2;
		THUMB_PREFETCH;
		clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1)
			+ codeTicksAccess(bus.armNextPC, BITS_16) + 3;
	} else {
		armState = true;
		bus.reg[15].I &= 0xFFFFFFFC;
		bus.armNextPC = bus.reg[15].I;
		bus.reg[15].I += 4;
		ARM_PREFETCH;
		clockTicks = ((codeTicksAccessSeq32(bus.armNextPC)) << 1) 
			+ codeTicksAccess(bus.armNextPC, BITS_32) + 3;
	}
}

// Load/store instructions ////////////////////////////////////////////////

// LDR R0~R7,[PC, #Imm]
 void thumb48(u32 opcode)
{
	u8 regist = (opcode >> 8) & 7;
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = (bus.reg[15].I & 0xFFFFFFFC) + ((opcode & 0xFF) << 2);
	// why quick?
	// bus.reg[regist].I = CPUReadMemoryQuick(address);
	bus.reg[regist].I = CPUReadMemory(address);
	bus.busPrefetchCount=0;
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// STR Rd, [Rs, Rn]
 void thumb50(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + bus.reg[(opcode>>6)&7].I;
	CPUWriteMemory(address, bus.reg[opcode & 7].I);
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16) + 2;
}

// STRH Rd, [Rs, Rn]
 void thumb52(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + bus.reg[(opcode>>6)&7].I;
	CPUWriteHalfWord(address, bus.reg[opcode&7].W.W0);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16) + 2;
}

// STRB Rd, [Rs, Rn]
 void thumb54(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + bus.reg[(opcode >>6)&7].I;
	CPUWriteByte(address, bus.reg[opcode & 7].B.B0);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16) + 2;
}

// LDSB Rd, [Rs, Rn]
 void thumb56(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + bus.reg[(opcode>>6)&7].I;
	bus.reg[opcode&7].I = (s8)CPUReadByte(address);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// LDR Rd, [Rs, Rn]
 void thumb58(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + bus.reg[(opcode>>6)&7].I;
	bus.reg[opcode&7].I = CPUReadMemory(address);
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// LDRH Rd, [Rs, Rn]
 void thumb5A(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + bus.reg[(opcode>>6)&7].I;
	bus.reg[opcode&7].I = CPUReadHalfWord(address);
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// LDRB Rd, [Rs, Rn]
 void thumb5C(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + bus.reg[(opcode>>6)&7].I;
	bus.reg[opcode&7].I = CPUReadByte(address);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// LDSH Rd, [Rs, Rn]
 void thumb5E(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + bus.reg[(opcode>>6)&7].I;
	bus.reg[opcode&7].I = (s16)CPUReadHalfWordSigned(address);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// STR Rd, [Rs, #Imm]
 void thumb60(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + (((opcode>>6)&31)<<2);
	CPUWriteMemory(address, bus.reg[opcode&7].I);
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16) + 2;
}

// LDR Rd, [Rs, #Imm]
 void thumb68(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + (((opcode>>6)&31)<<2);
	bus.reg[opcode&7].I = CPUReadMemory(address);
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// STRB Rd, [Rs, #Imm]
 void thumb70(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + (((opcode>>6)&31));
	CPUWriteByte(address, bus.reg[opcode&7].B.B0);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16) + 2;
}

// LDRB Rd, [Rs, #Imm]
 void thumb78(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + (((opcode>>6)&31));
	bus.reg[opcode&7].I = CPUReadByte(address);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// STRH Rd, [Rs, #Imm]
 void thumb80(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + (((opcode>>6)&31)<<1);
	CPUWriteHalfWord(address, bus.reg[opcode&7].W.W0);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16) + 2;
}

// LDRH Rd, [Rs, #Imm]
 void thumb88(u32 opcode)
{
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[(opcode>>3)&7].I + (((opcode>>6)&31)<<1);
	bus.reg[opcode&7].I = CPUReadHalfWord(address);
	int dataticks_value = DATATICKS_ACCESS_16BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// STR R0~R7, [SP, #Imm]
 void thumb90(u32 opcode)
{
	u8 regist = (opcode >> 8) & 7;
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[13].I + ((opcode&255)<<2);
	CPUWriteMemory(address, bus.reg[regist].I);
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16) + 2;
}

// LDR R0~R7, [SP, #Imm]
 void thumb98(u32 opcode)
{
	u8 regist = (opcode >> 8) & 7;
	if (bus.busPrefetchCount == 0)
		bus.busPrefetch = bus.busPrefetchEnable;
	u32 address = bus.reg[13].I + ((opcode&255)<<2);
	// why quick?
	// bus.reg[regist].I = CPUReadMemoryQuick(address);
	bus.reg[regist].I = CPUReadMemory(address);
	int dataticks_value = DATATICKS_ACCESS_32BIT(address);
	DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
	clockTicks = 3 + dataticks_value + codeTicksAccess(bus.armNextPC, BITS_16);
}

// PC/stack-related ///////////////////////////////////////////////////////

// ADD R0~R7, PC, Imm
 void thumbA0(u32 opcode)
{
  u8 regist = (opcode >> 8) & 7;
  bus.reg[regist].I = (bus.reg[15].I & 0xFFFFFFFC) + ((opcode&255)<<2);
}

// ADD R0~R7, SP, Imm
 void thumbA8(u32 opcode)
{
  u8 regist = (opcode >> 8) & 7;
  bus.reg[regist].I = bus.reg[13].I + ((opcode&255)<<2);
}

// ADD SP, Imm
 void thumbB0(u32 opcode)
{
  int offset = (opcode & 127) << 2;
  if(opcode & 0x80)
    offset = -offset;
  bus.reg[13].I += offset;
}

// Push and pop ///////////////////////////////////////////////////////////

#define PUSH_REG(val, r)                                    \
  if (opcode & (val)) {                                     \
    CPUWriteMemory(address, bus.reg[(r)].I);                    \
    int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
    DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
    clockTicks += 1 + dataticks_value;				\
    count++;                                                \
    address += 4;                                           \
  }

#define POP_REG(val, r)                                     \
  if (opcode & (val)) {                                     \
    bus.reg[(r)].I = CPUReadMemory(address);                    \
int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
    DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value); \
    clockTicks += 1 + dataticks_value; \
    count++;                                                \
    address += 4;                                           \
  }

// PUSH {Rlist}
 void thumbB4(u32 opcode)
{
  if (bus.busPrefetchCount == 0)
    bus.busPrefetch = bus.busPrefetchEnable;
  int count = 0;
  u32 temp = bus.reg[13].I - 4 * cpuBitsSet[opcode & 0xff];
  u32 address = temp & 0xFFFFFFFC;
  PUSH_REG(1, 0);
  PUSH_REG(2, 1);
  PUSH_REG(4, 2);
  PUSH_REG(8, 3);
  PUSH_REG(16, 4);
  PUSH_REG(32, 5);
  PUSH_REG(64, 6);
  PUSH_REG(128, 7);
  clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_16);
  bus.reg[13].I = temp;
}

// PUSH {Rlist, LR}
 void thumbB5(u32 opcode)
{
  if (bus.busPrefetchCount == 0)
    bus.busPrefetch = bus.busPrefetchEnable;
  int count = 0;
  u32 temp = bus.reg[13].I - 4 - 4 * cpuBitsSet[opcode & 0xff];
  u32 address = temp & 0xFFFFFFFC;
  PUSH_REG(1, 0);
  PUSH_REG(2, 1);
  PUSH_REG(4, 2);
  PUSH_REG(8, 3);
  PUSH_REG(16, 4);
  PUSH_REG(32, 5);
  PUSH_REG(64, 6);
  PUSH_REG(128, 7);
  PUSH_REG(256, 14);
  clockTicks += 1 + codeTicksAccess(bus.armNextPC, BITS_16);
  bus.reg[13].I = temp;
}

// POP {Rlist}
 void thumbBC(u32 opcode)
{
  if (bus.busPrefetchCount == 0)
    bus.busPrefetch = bus.busPrefetchEnable;
  int count = 0;
  u32 address = bus.reg[13].I & 0xFFFFFFFC;
  u32 temp = bus.reg[13].I + 4*cpuBitsSet[opcode & 0xFF];
  POP_REG(1, 0);
  POP_REG(2, 1);
  POP_REG(4, 2);
  POP_REG(8, 3);
  POP_REG(16, 4);
  POP_REG(32, 5);
  POP_REG(64, 6);
  POP_REG(128, 7);
  bus.reg[13].I = temp;
  clockTicks = 2 + codeTicksAccess(bus.armNextPC, BITS_16);
}

// POP {Rlist, PC}
 void thumbBD(u32 opcode)
{
  if (bus.busPrefetchCount == 0)
    bus.busPrefetch = bus.busPrefetchEnable;
  int count = 0;
  u32 address = bus.reg[13].I & 0xFFFFFFFC;
  u32 temp = bus.reg[13].I + 4 + 4*cpuBitsSet[opcode & 0xFF];
  POP_REG(1, 0);
  POP_REG(2, 1);
  POP_REG(4, 2);
  POP_REG(8, 3);
  POP_REG(16, 4);
  POP_REG(32, 5);
  POP_REG(64, 6);
  POP_REG(128, 7);
  bus.reg[15].I = (CPUReadMemory(address) & 0xFFFFFFFE);
  int dataticks_value = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address);
  DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_value);
  clockTicks += 1 + dataticks_value;
  count++;
  bus.armNextPC = bus.reg[15].I;
  bus.reg[15].I += 2;
  bus.reg[13].I = temp;
  THUMB_PREFETCH;
  bus.busPrefetchCount = 0;
  clockTicks += 3 + ((codeTicksAccess(bus.armNextPC, BITS_16)) << 1);
}

// Load/store multiple ////////////////////////////////////////////////////

#define THUMB_STM_REG(val,r,b)                              \
  if(opcode & (val)) {                                      \
    CPUWriteMemory(address, bus.reg[(r)].I);                    \
    bus.reg[(b)].I = temp;                                      \
    int dataticks_val = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
    DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_val); \
    clockTicks += 1 + dataticks_val; \
    count++;                                                \
    address += 4;                                           \
  }

#define THUMB_LDM_REG(val,r)                                \
  if(opcode & (val)) {                                      \
    bus.reg[(r)].I = CPUReadMemory(address);                    \
    int dataticks_val = count ? DATATICKS_ACCESS_32BIT_SEQ(address) : DATATICKS_ACCESS_32BIT(address); \
    DATATICKS_ACCESS_BUS_PREFETCH(address, dataticks_val); \
    clockTicks += 1 + dataticks_val; \
    count++;                                                \
    address += 4;                                           \
  }

// STM R0~7!, {Rlist}
 void thumbC0(u32 opcode)
{
  u8 regist = (opcode >> 8) & 7;
  if (bus.busPrefetchCount == 0)
    bus.busPrefetch = bus.busPrefetchEnable;
  u32 address = bus.reg[regist].I & 0xFFFFFFFC;
  u32 temp = bus.reg[regist].I + 4*cpuBitsSet[opcode & 0xff];
  int count = 0;
  // store
  THUMB_STM_REG(1, 0, regist);
  THUMB_STM_REG(2, 1, regist);
  THUMB_STM_REG(4, 2, regist);
  THUMB_STM_REG(8, 3, regist);
  THUMB_STM_REG(16, 4, regist);
  THUMB_STM_REG(32, 5, regist);
  THUMB_STM_REG(64, 6, regist);
  THUMB_STM_REG(128, 7, regist);
  clockTicks = 1 + codeTicksAccess(bus.armNextPC, BITS_16);
}

// LDM R0~R7!, {Rlist}
 void thumbC8(u32 opcode)
{
  u8 regist = (opcode >> 8) & 7;
  if (bus.busPrefetchCount == 0)
    bus.busPrefetch = bus.busPrefetchEnable;
  u32 address = bus.reg[regist].I & 0xFFFFFFFC;
  u32 temp = bus.reg[regist].I + 4*cpuBitsSet[opcode & 0xFF];
  int count = 0;
  // load
  THUMB_LDM_REG(1, 0);
  THUMB_LDM_REG(2, 1);
  THUMB_LDM_REG(4, 2);
  THUMB_LDM_REG(8, 3);
  THUMB_LDM_REG(16, 4);
  THUMB_LDM_REG(32, 5);
  THUMB_LDM_REG(64, 6);
  THUMB_LDM_REG(128, 7);
  clockTicks = 2 + codeTicksAccess(bus.armNextPC, BITS_16);
  if(!(opcode & (1<<regist)))
    bus.reg[regist].I = temp;
}

// Conditional branches ///////////////////////////////////////////////////

// BEQ offset
 void thumbD0(u32 opcode)
{
	if(Z_FLAG)
	{
		bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
		bus.armNextPC = bus.reg[15].I;
		bus.reg[15].I += 2;
		THUMB_PREFETCH;
#if defined (SPEEDHAX)
		clockTicks = 30;
#else
		clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
			codeTicksAccess(bus.armNextPC, BITS_16)+3;
#endif
		bus.busPrefetchCount=0;
	}
}

// BNE offset
 void thumbD1(u32 opcode)
{
  if(!Z_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BCS offset
 void thumbD2(u32 opcode)
{
  if(C_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BCC offset
 void thumbD3(u32 opcode)
{
  if(!C_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BMI offset
 void thumbD4(u32 opcode)
{
  if(N_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BPL offset
 void thumbD5(u32 opcode)
{
  if(!N_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BVS offset
 void thumbD6(u32 opcode)
{
  if(V_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BVC offset
 void thumbD7(u32 opcode)
{
  if(!V_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BHI offset
 void thumbD8(u32 opcode)
{
  if(C_FLAG && !Z_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BLS offset
 void thumbD9(u32 opcode)
{
  if(!C_FLAG || Z_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BGE offset
 void thumbDA(u32 opcode)
{
  if(N_FLAG == V_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BLT offset
 void thumbDB(u32 opcode)
{
  if(N_FLAG != V_FLAG) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BGT offset
 void thumbDC(u32 opcode)
{
  if(!Z_FLAG && (N_FLAG == V_FLAG)) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = (codeTicksAccessSeq16(bus.armNextPC) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// BLE offset
 void thumbDD(u32 opcode)
{
  if(Z_FLAG || (N_FLAG != V_FLAG)) {
    bus.reg[15].I += ((s8)(opcode & 0xFF)) << 1;
    bus.armNextPC = bus.reg[15].I;
    bus.reg[15].I += 2;
    THUMB_PREFETCH;
    clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
        codeTicksAccess(bus.armNextPC, BITS_16)+3;
    bus.busPrefetchCount=0;
  }
}

// SWI, B, BL /////////////////////////////////////////////////////////////

// SWI #comment
 void thumbDF(u32 opcode)
{
  u32 address = 0;
  clockTicks = ((codeTicksAccessSeq16(address)) << 1) +
      codeTicksAccess(address, BITS_16)+3;
  bus.busPrefetchCount=0;
  CPUSoftwareInterrupt(opcode & 0xFF);
}

// B offset
 void thumbE0(u32 opcode)
{
  int offset = (opcode & 0x3FF) << 1;
  if(opcode & 0x0400)
    offset |= 0xFFFFF800;
  bus.reg[15].I += offset;
  bus.armNextPC = bus.reg[15].I;
  bus.reg[15].I += 2;
  THUMB_PREFETCH;
  clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
      codeTicksAccess(bus.armNextPC, BITS_16) + 3;
  bus.busPrefetchCount=0;
}

// BLL #offset (forward)
 void thumbF0(u32 opcode)
{
  int offset = (opcode & 0x7FF);
  bus.reg[14].I = bus.reg[15].I + (offset << 12);
  clockTicks = codeTicksAccessSeq16(bus.armNextPC) + 1;
}

// BLL #offset (backward)
 void thumbF4(u32 opcode)
{
  int offset = (opcode & 0x7FF);
  bus.reg[14].I = bus.reg[15].I + ((offset << 12) | 0xFF800000);
  clockTicks = codeTicksAccessSeq16(bus.armNextPC) + 1;
}

// BLH #offset
 void thumbF8(u32 opcode)
{
  int offset = (opcode & 0x7FF);
  u32 temp = bus.reg[15].I-2;
  bus.reg[15].I = (bus.reg[14].I + (offset<<1))&0xFFFFFFFE;
  bus.armNextPC = bus.reg[15].I;
  bus.reg[15].I += 2;
  bus.reg[14].I = temp|1;
  THUMB_PREFETCH;
  clockTicks = ((codeTicksAccessSeq16(bus.armNextPC)) << 1) +
      codeTicksAccess(bus.armNextPC, BITS_16) + 3;
  bus.busPrefetchCount = 0;
}

// Instruction table //////////////////////////////////////////////////////

static void (Gigazoid::*const thumbInsnTable[1024])(u32 opcode);

// Wrapper routine (execution loop) ///////////////////////////////////////


int thumbExecute (void)
{
	CACHE_PREFETCH(clockTicks);

	int ct = 0;

	do {

		clockTicks = 0;

#if 0
		if ((bus.armNextPC & 0x0803FFFF) == 0x08020000)
		   bus.busPrefetchCount=0x100;
#endif

		u32 opcode = cpuPrefetch[0];
		cpuPrefetch[0] = cpuPrefetch[1];

		bus.busPrefetch = false;
#if 0
		if (bus.busPrefetchCount & 0xFFFFFF00)
			bus.busPrefetchCount = 0x100 | (bus.busPrefetchCount & 0xFF);
#endif

		u32 oldArmNextPC = bus.armNextPC;

		bus.armNextPC = bus.reg[15].I;
		if (traceCallback) // low bit of addr is set on callback to indicate thumb mode
			traceCallback(bus.armNextPC | 1, opcode);
		if (fetchCallback)
			fetchCallback(bus.armNextPC);

		bus.reg[15].I += 2;
		THUMB_PREFETCH_NEXT;

		(this->*thumbInsnTable[opcode>>6])(opcode);

		ct = clockTicks;

		if (ct < 0)
			return 0;

		/// better pipelining
		if (ct==0)
			clockTicks = codeTicksAccessSeq16(oldArmNextPC) + 1;

		cpuTotalTicks += clockTicks;

} while ((cpuTotalTicks < cpuNextEvent) & ~armState & ~holdState);
	return 1;
}


/*============================================================
	GBA GFX
============================================================ */

#ifdef TILED_RENDERING
#ifdef _MSC_VER
union u8h
{
   __pragma( pack(push, 1));
   struct
#ifdef LSB_FIRST
   {
      /* 0*/	unsigned char lo:4;
      /* 4*/	unsigned char hi:4;
#else
   {
      /* 4*/	unsigned char hi:4;
      /* 0*/	unsigned char lo:4;
#endif
   }
   __pragma(pack(pop));
   u8 val;
};
#else
union u8h
{
   struct
#ifdef LSB_FIRST
   {
      /* 0*/	unsigned char lo:4;
      /* 4*/	unsigned char hi:4;
#else
   {
      /* 4*/	unsigned char hi:4;
      /* 0*/	unsigned char lo:4;
#endif
   } __attribute__ ((packed));
   u8 val;
};
#endif

union TileEntry
{
#ifdef LSB_FIRST
   struct
   {
      /* 0*/	unsigned tileNum:10;
      /*12*/	unsigned hFlip:1;
      /*13*/	unsigned vFlip:1;
      /*14*/	unsigned palette:4;
   };
#else
   struct
   {
      /*14*/	unsigned palette:4;
      /*13*/	unsigned vFlip:1;
      /*12*/	unsigned hFlip:1;
      /* 0*/	unsigned tileNum:10;
   };
#endif
   u16 val;
};

struct TileLine
{
   u32 pixels[8];
};

typedef const TileLine (*TileReader) (const u16 *, const int, const u8 *, u16 *, const u32);

static inline void gfxDrawPixel(u32 *dest, const u8 color, const u16 *palette, const u32 prio)
{
   *dest = color ? (READ16LE(&palette[color]) | prio): 0x80000000;
}

static inline const TileLine gfxReadTile(const u16 *screenSource, const int yyy, const u8 *charBase, u16 *palette, const u32 prio)
{
   TileEntry tile;
   tile.val = READ16LE(screenSource);

   int tileY = yyy & 7;
   if (tile.vFlip) tileY = 7 - tileY;
   TileLine tileLine;

   const u8 *tileBase = &charBase[tile.tileNum * 64 + tileY * 8];

   if (!tile.hFlip)
   {
      gfxDrawPixel(&tileLine.pixels[0], tileBase[0], palette, prio);
      gfxDrawPixel(&tileLine.pixels[1], tileBase[1], palette, prio);
      gfxDrawPixel(&tileLine.pixels[2], tileBase[2], palette, prio);
      gfxDrawPixel(&tileLine.pixels[3], tileBase[3], palette, prio);
      gfxDrawPixel(&tileLine.pixels[4], tileBase[4], palette, prio);
      gfxDrawPixel(&tileLine.pixels[5], tileBase[5], palette, prio);
      gfxDrawPixel(&tileLine.pixels[6], tileBase[6], palette, prio);
      gfxDrawPixel(&tileLine.pixels[7], tileBase[7], palette, prio);
   }
   else
   {
      gfxDrawPixel(&tileLine.pixels[0], tileBase[7], palette, prio);
      gfxDrawPixel(&tileLine.pixels[1], tileBase[6], palette, prio);
      gfxDrawPixel(&tileLine.pixels[2], tileBase[5], palette, prio);
      gfxDrawPixel(&tileLine.pixels[3], tileBase[4], palette, prio);
      gfxDrawPixel(&tileLine.pixels[4], tileBase[3], palette, prio);
      gfxDrawPixel(&tileLine.pixels[5], tileBase[2], palette, prio);
      gfxDrawPixel(&tileLine.pixels[6], tileBase[1], palette, prio);
      gfxDrawPixel(&tileLine.pixels[7], tileBase[0], palette, prio);
   }

   return tileLine;
}

static inline const TileLine gfxReadTilePal(const u16 *screenSource, const int yyy, const u8 *charBase, u16 *palette, const u32 prio)
{
   TileEntry tile;
   tile.val = READ16LE(screenSource);

   int tileY = yyy & 7;
   if (tile.vFlip) tileY = 7 - tileY;
   palette += tile.palette * 16;
   TileLine tileLine;

   const u8h *tileBase = (u8h*) &charBase[tile.tileNum * 32 + tileY * 4];

   if (!tile.hFlip)
   {
      gfxDrawPixel(&tileLine.pixels[0], tileBase[0].lo, palette, prio);
      gfxDrawPixel(&tileLine.pixels[1], tileBase[0].hi, palette, prio);
      gfxDrawPixel(&tileLine.pixels[2], tileBase[1].lo, palette, prio);
      gfxDrawPixel(&tileLine.pixels[3], tileBase[1].hi, palette, prio);
      gfxDrawPixel(&tileLine.pixels[4], tileBase[2].lo, palette, prio);
      gfxDrawPixel(&tileLine.pixels[5], tileBase[2].hi, palette, prio);
      gfxDrawPixel(&tileLine.pixels[6], tileBase[3].lo, palette, prio);
      gfxDrawPixel(&tileLine.pixels[7], tileBase[3].hi, palette, prio);
   }
   else
   {
      gfxDrawPixel(&tileLine.pixels[0], tileBase[3].hi, palette, prio);
      gfxDrawPixel(&tileLine.pixels[1], tileBase[3].lo, palette, prio);
      gfxDrawPixel(&tileLine.pixels[2], tileBase[2].hi, palette, prio);
      gfxDrawPixel(&tileLine.pixels[3], tileBase[2].lo, palette, prio);
      gfxDrawPixel(&tileLine.pixels[4], tileBase[1].hi, palette, prio);
      gfxDrawPixel(&tileLine.pixels[5], tileBase[1].lo, palette, prio);
      gfxDrawPixel(&tileLine.pixels[6], tileBase[0].hi, palette, prio);
      gfxDrawPixel(&tileLine.pixels[7], tileBase[0].lo, palette, prio);
   }

   return tileLine;
}

static inline void gfxDrawTile(const TileLine &tileLine, u32 *line)
{
   memcpy(line, tileLine.pixels, sizeof(tileLine.pixels));
}

static inline void gfxDrawTileClipped(const TileLine &tileLine, u32 *line, const int start, int w)
{
   memcpy(line, tileLine.pixels + start, w * sizeof(u32));
}

template<TileReader readTile>
void gfxDrawTextScreen(u16 control, u16 hofs, u16 vofs,
                       u32 *line)
{
   u16 *palette = (u16 *)graphics.paletteRAM;
   u8 *charBase = &vram[((control >> 2) & 0x03) * 0x4000];
   u16 *screenBase = (u16 *)&vram[((control >> 8) & 0x1f) * 0x800];
   u32 prio = ((control & 3)<<25) + 0x1000000;
   int sizeX = 256;
   int sizeY = 256;
   switch ((control >> 14) & 3)
   {
      case 0:
         break;
      case 1:
         sizeX = 512;
         break;
      case 2:
         sizeY = 512;
         break;
      case 3:
         sizeX = 512;
         sizeY = 512;
         break;
   }

   int maskX = sizeX-1;
   int maskY = sizeY-1;

   bool mosaicOn = (control & 0x40) ? true : false;

   int xxx = hofs & maskX;
   int yyy = (vofs + io_registers[REG_VCOUNT]) & maskY;
   int mosaicX = (MOSAIC & 0x000F)+1;
   int mosaicY = ((MOSAIC & 0x00F0)>>4)+1;

   if (mosaicOn)
   {
      if ((io_registers[REG_VCOUNT] % mosaicY) != 0)
      {
         mosaicY = io_registers[REG_VCOUNT] - (io_registers[REG_VCOUNT] % mosaicY);
         yyy = (vofs + mosaicY) & maskY;
      }
   }

   if (yyy > 255 && sizeY > 256)
   {
      yyy &= 255;
      screenBase += 0x400;
      if (sizeX > 256)
         screenBase += 0x400;
   }

   int yshift = ((yyy>>3)<<5);

   u16 *screenSource = screenBase + 0x400 * (xxx>>8) + ((xxx & 255)>>3) + yshift;
   int x = 0;
   const int firstTileX = xxx & 7;

   // First tile, if clipped
   if (firstTileX)
   {
      gfxDrawTileClipped(readTile(screenSource, yyy, charBase, palette, prio), &line[x], firstTileX, 8 - firstTileX);
      screenSource++;
      x += 8 - firstTileX;
      xxx += 8 - firstTileX;

      if (xxx == 256 && sizeX > 256)
      {
         screenSource = screenBase + 0x400 + yshift;
      }
      else if (xxx >= sizeX)
      {
         xxx = 0;
         screenSource = screenBase + yshift;
      }
   }

   // Middle tiles, full
   while (x < 240 - firstTileX)
   {
      gfxDrawTile(readTile(screenSource, yyy, charBase, palette, prio), &line[x]);
      screenSource++;
      xxx += 8;
      x += 8;

      if (xxx == 256 && sizeX > 256)
      {
         screenSource = screenBase + 0x400 + yshift;
      }
      else if (xxx >= sizeX)
      {
         xxx = 0;
         screenSource = screenBase + yshift;
      }
   }

   // Last tile, if clipped
   if (firstTileX)
   {
      gfxDrawTileClipped(readTile(screenSource, yyy, charBase, palette, prio), &line[x], 0, firstTileX);
   }

   if (mosaicOn)
   {
      if (mosaicX > 1)
      {
         int m = 1;
         for (int i = 0; i < 239; i++)
         {
            line[i+1] = line[i];
            m++;
            if (m == mosaicX)
            {
               m = 1;
               i++;
            }
         }
      }
   }
}

void gfxDrawTextScreen(u16 control, u16 hofs, u16 vofs, u32 *line)
{
   if (control & 0x80) // 1 pal / 256 col
      gfxDrawTextScreen<gfxReadTile>(control, hofs, vofs, line);
   else // 16 pal / 16 col
      gfxDrawTextScreen<gfxReadTilePal>(control, hofs, vofs, line);
}
#else
inline void gfxDrawTextScreen(u16 control, u16 hofs, u16 vofs,
				     u32 *line)
{
  u16 *palette = (u16 *)graphics.paletteRAM;
  u8 *charBase = &vram[((control >> 2) & 0x03) * 0x4000];
  u16 *screenBase = (u16 *)&vram[((control >> 8) & 0x1f) * 0x800];
  u32 prio = ((control & 3)<<25) + 0x1000000;
  int sizeX = 256;
  int sizeY = 256;
  switch((control >> 14) & 3) {
  case 0:
    break;
  case 1:
    sizeX = 512;
    break;
  case 2:
    sizeY = 512;
    break;
  case 3:
    sizeX = 512;
    sizeY = 512;
    break;
  }

  int maskX = sizeX-1;
  int maskY = sizeY-1;

  bool mosaicOn = (control & 0x40) ? true : false;

  int xxx = hofs & maskX;
  int yyy = (vofs + io_registers[REG_VCOUNT]) & maskY;
  int mosaicX = (MOSAIC & 0x000F)+1;
  int mosaicY = ((MOSAIC & 0x00F0)>>4)+1;

  if(mosaicOn) {
    if((io_registers[REG_VCOUNT] % mosaicY) != 0) {
      mosaicY = io_registers[REG_VCOUNT] - (io_registers[REG_VCOUNT] % mosaicY);
      yyy = (vofs + mosaicY) & maskY;
    }
  }

  if(yyy > 255 && sizeY > 256) {
    yyy &= 255;
    screenBase += 0x400;
    if(sizeX > 256)
      screenBase += 0x400;
  }

  int yshift = ((yyy>>3)<<5);
  if((control) & 0x80) {
    u16 *screenSource = screenBase + 0x400 * (xxx>>8) + ((xxx & 255)>>3) + yshift;
    for(int x = 0; x < 240; x++) {
      u16 data = READ16LE(screenSource);

      int tile = data & 0x3FF;
      int tileX = (xxx & 7);
      int tileY = yyy & 7;

      if(tileX == 7)
        screenSource++;

      if(data & 0x0400)
        tileX = 7 - tileX;
      if(data & 0x0800)
        tileY = 7 - tileY;

      u8 color = charBase[tile * 64 + tileY * 8 + tileX];

      line[x] = color ? (READ16LE(&palette[color]) | prio): 0x80000000;

      xxx++;
      if(xxx == 256) {
        if(sizeX > 256)
          screenSource = screenBase + 0x400 + yshift;
        else {
          screenSource = screenBase + yshift;
          xxx = 0;
        }
      } else if(xxx >= sizeX) {
        xxx = 0;
        screenSource = screenBase + yshift;
      }
    }
  } else {
    u16 *screenSource = screenBase + 0x400*(xxx>>8)+((xxx&255)>>3) +
      yshift;
    for(int x = 0; x < 240; x++) {
      u16 data = READ16LE(screenSource);

      int tile = data & 0x3FF;
      int tileX = (xxx & 7);
      int tileY = yyy & 7;

      if(tileX == 7)
        screenSource++;

      if(data & 0x0400)
        tileX = 7 - tileX;
      if(data & 0x0800)
        tileY = 7 - tileY;

      u8 color = charBase[(tile<<5) + (tileY<<2) + (tileX>>1)];

      if(tileX & 1) {
        color = (color >> 4);
      } else {
        color &= 0x0F;
      }

      int pal = (data>>8) & 0xF0;
      line[x] = color ? (READ16LE(&palette[pal + color])|prio): 0x80000000;

      xxx++;
      if(xxx == 256) {
        if(sizeX > 256)
          screenSource = screenBase + 0x400 + yshift;
        else {
          screenSource = screenBase + yshift;
          xxx = 0;
        }
      } else if(xxx >= sizeX) {
        xxx = 0;
        screenSource = screenBase + yshift;
      }
    }
  }
  if(mosaicOn) {
    if(mosaicX > 1) {
      int m = 1;
      for(int i = 0; i < 239; i++) {
        line[i+1] = line[i];
        m++;
        if(m == mosaicX) {
          m = 1;
          i++;
        }
      }
    }
  }
}
#endif

INLINE void gfxDrawRotScreen(u16 control, u16 x_l, u16 x_h, u16 y_l, u16 y_h,
u16 pa,  u16 pb, u16 pc,  u16 pd, int& currentX, int& currentY, int changed, u32 *line)
{
	u16 *palette = (u16 *)graphics.paletteRAM;
	u8 *charBase = &vram[((control >> 2) & 0x03) << 14];
	u8 *screenBase = (u8 *)&vram[((control >> 8) & 0x1f) << 11];
	int prio = ((control & 3) << 25) + 0x1000000;

	u32 map_size = (control >> 14) & 3;
	u32 sizeX = map_sizes_rot[map_size];
	u32 sizeY = map_sizes_rot[map_size];

	int maskX = sizeX-1;
	int maskY = sizeY-1;

	int yshift = ((control >> 14) & 3)+4;

#ifdef BRANCHLESS_GBA_GFX
	int dx = pa & 0x7FFF;
	int dmx = pb & 0x7FFF;
	int dy = pc & 0x7FFF;
	int dmy = pd & 0x7FFF;

	dx |= isel(-(pa & 0x8000), 0, 0xFFFF8000);

	dmx |= isel(-(pb & 0x8000), 0, 0xFFFF8000);

	dy |= isel(-(pc & 0x8000), 0, 0xFFFF8000);

	dmy |= isel(-(pd & 0x8000), 0, 0xFFFF8000);
#else
	int dx = pa & 0x7FFF;
	if(pa & 0x8000)
		dx |= 0xFFFF8000;
	int dmx = pb & 0x7FFF;
	if(pb & 0x8000)
		dmx |= 0xFFFF8000;
	int dy = pc & 0x7FFF;
	if(pc & 0x8000)
		dy |= 0xFFFF8000;
	int dmy = pd & 0x7FFF;
	if(pd & 0x8000)
		dmy |= 0xFFFF8000;
#endif

	if(io_registers[REG_VCOUNT] == 0)
		changed = 3;

	currentX += dmx;
	currentY += dmy;

	if(changed & 1)
	{
		currentX = (x_l) | ((x_h & 0x07FF)<<16);
		if(x_h & 0x0800)
			currentX |= 0xF8000000;
	}

	if(changed & 2)
	{
		currentY = (y_l) | ((y_h & 0x07FF)<<16);
		if(y_h & 0x0800)
			currentY |= 0xF8000000;
	}

	int realX = currentX;
	int realY = currentY;

	if(control & 0x40)
	{
		int mosaicY = ((MOSAIC & 0xF0)>>4) + 1;
		int y = (io_registers[REG_VCOUNT] % mosaicY);
		realX -= y*dmx;
		realY -= y*dmy;
	}

	memset(line, -1, 240 * sizeof(u32));
	if(control & 0x2000)
	{
		for(u32 x = 0; x < 240u; ++x)
		{
			int xxx = (realX >> 8) & maskX;
			int yyy = (realY >> 8) & maskY;

			int tile = screenBase[(xxx>>3) + ((yyy>>3)<<yshift)];

			int tileX = (xxx & 7);
			int tileY = yyy & 7;

			u8 color = charBase[(tile<<6) + (tileY<<3) + tileX];

			if(color)
				line[x] = (READ16LE(&palette[color])|prio);

			realX += dx;
			realY += dy;
		}
	}
	else
	{
		for(u32 x = 0; x < 240u; ++x)
		{
			unsigned xxx = (realX >> 8);
			unsigned yyy = (realY >> 8);

			if(xxx < sizeX && yyy < sizeY)
			{
				int tile = screenBase[(xxx>>3) + ((yyy>>3)<<yshift)];

				int tileX = (xxx & 7);
				int tileY = yyy & 7;

				u8 color = charBase[(tile<<6) + (tileY<<3) + tileX];

				if(color)
					line[x] = (READ16LE(&palette[color])|prio);
			}

			realX += dx;
			realY += dy;
		}
	}

	if(control & 0x40)
	{
		int mosaicX = (MOSAIC & 0xF) + 1;
		if(mosaicX > 1)
		{
			int m = 1;
			for(u32 i = 0; i < 239u; ++i)
			{
				line[i+1] = line[i];
				if(++m == mosaicX)
				{
					m = 1;
					++i;
				}
			}
		}
	}
}

INLINE void gfxDrawRotScreen16Bit( int& currentX,  int& currentY, int changed)
{
	u16 *screenBase = (u16 *)&vram[0];
	int prio = ((io_registers[REG_BG2CNT] & 3) << 25) + 0x1000000;

	u32 sizeX = 240;
	u32 sizeY = 160;

	int startX = (BG2X_L) | ((BG2X_H & 0x07FF)<<16);
	if(BG2X_H & 0x0800)
		startX |= 0xF8000000;
	int startY = (BG2Y_L) | ((BG2Y_H & 0x07FF)<<16);
	if(BG2Y_H & 0x0800)
		startY |= 0xF8000000;

#ifdef BRANCHLESS_GBA_GFX
	int dx = io_registers[REG_BG2PA] & 0x7FFF;
	dx |= isel(-(io_registers[REG_BG2PA] & 0x8000), 0, 0xFFFF8000);

	int dmx = io_registers[REG_BG2PB] & 0x7FFF;
	dmx |= isel(-(io_registers[REG_BG2PB] & 0x8000), 0, 0xFFFF8000);

	int dy = io_registers[REG_BG2PC] & 0x7FFF;
	dy |= isel(-(io_registers[REG_BG2PC] & 0x8000), 0, 0xFFFF8000);

	int dmy = io_registers[REG_BG2PD] & 0x7FFF;
	dmy |= isel(-(io_registers[REG_BG2PD] & 0x8000), 0, 0xFFFF8000);
#else
	int dx = io_registers[REG_BG2PA] & 0x7FFF;
	if(io_registers[REG_BG2PA] & 0x8000)
		dx |= 0xFFFF8000;
	int dmx = io_registers[REG_BG2PB] & 0x7FFF;
	if(io_registers[REG_BG2PB] & 0x8000)
		dmx |= 0xFFFF8000;
	int dy = io_registers[REG_BG2PC] & 0x7FFF;
	if(io_registers[REG_BG2PC] & 0x8000)
		dy |= 0xFFFF8000;
	int dmy = io_registers[REG_BG2PD] & 0x7FFF;
	if(io_registers[REG_BG2PD] & 0x8000)
		dmy |= 0xFFFF8000;
#endif

	if(io_registers[REG_VCOUNT] == 0)
		changed = 3;

	currentX += dmx;
	currentY += dmy;

	if(changed & 1)
	{
		currentX = (BG2X_L) | ((BG2X_H & 0x07FF)<<16);
		if(BG2X_H & 0x0800)
			currentX |= 0xF8000000;
	}

	if(changed & 2)
	{
		currentY = (BG2Y_L) | ((BG2Y_H & 0x07FF)<<16);
		if(BG2Y_H & 0x0800)
			currentY |= 0xF8000000;
	}

	int realX = currentX;
	int realY = currentY;

	if(io_registers[REG_BG2CNT] & 0x40) {
		int mosaicY = ((MOSAIC & 0xF0)>>4) + 1;
		int y = (io_registers[REG_VCOUNT] % mosaicY);
		realX -= y*dmx;
		realY -= y*dmy;
	}

	unsigned xxx = (realX >> 8);
	unsigned yyy = (realY >> 8);

	memset(line[2], -1, 240 * sizeof(u32));
	for(u32 x = 0; x < 240u; ++x)
	{
		if(xxx < sizeX && yyy < sizeY)
			line[2][x] = (READ16LE(&screenBase[yyy * sizeX + xxx]) | prio);

		realX += dx;
		realY += dy;

		xxx = (realX >> 8);
		yyy = (realY >> 8);
	}

	if(io_registers[REG_BG2CNT] & 0x40) {
		int mosaicX = (MOSAIC & 0xF) + 1;
		if(mosaicX > 1) {
			int m = 1;
			for(u32 i = 0; i < 239u; ++i)
			{
				line[2][i+1] = line[2][i];
				if(++m == mosaicX)
				{
					m = 1;
					++i;
				}
			}
		}
	}
}

INLINE void gfxDrawRotScreen256(int &currentX, int& currentY, int changed)
{
	u16 *palette = (u16 *)graphics.paletteRAM;
	u8 *screenBase = (io_registers[REG_DISPCNT] & 0x0010) ? &vram[0xA000] : &vram[0x0000];
	int prio = ((io_registers[REG_BG2CNT] & 3) << 25) + 0x1000000;
	u32 sizeX = 240;
	u32 sizeY = 160;

	int startX = (BG2X_L) | ((BG2X_H & 0x07FF)<<16);
	if(BG2X_H & 0x0800)
		startX |= 0xF8000000;
	int startY = (BG2Y_L) | ((BG2Y_H & 0x07FF)<<16);
	if(BG2Y_H & 0x0800)
		startY |= 0xF8000000;

#ifdef BRANCHLESS_GBA_GFX
	int dx = io_registers[REG_BG2PA] & 0x7FFF;
	dx |= isel(-(io_registers[REG_BG2PA] & 0x8000), 0, 0xFFFF8000);

	int dmx = io_registers[REG_BG2PB] & 0x7FFF;
	dmx |= isel(-(io_registers[REG_BG2PB] & 0x8000), 0, 0xFFFF8000);

	int dy = io_registers[REG_BG2PC] & 0x7FFF;
	dy |= isel(-(io_registers[REG_BG2PC] & 0x8000), 0, 0xFFFF8000);

	int dmy = io_registers[REG_BG2PD] & 0x7FFF;
	dmy |= isel(-(io_registers[REG_BG2PD] & 0x8000), 0, 0xFFFF8000);
#else
	int dx = io_registers[REG_BG2PA] & 0x7FFF;
	if(io_registers[REG_BG2PA] & 0x8000)
		dx |= 0xFFFF8000;
	int dmx = io_registers[REG_BG2PB] & 0x7FFF;
	if(io_registers[REG_BG2PB] & 0x8000)
		dmx |= 0xFFFF8000;
	int dy = io_registers[REG_BG2PC] & 0x7FFF;
	if(io_registers[REG_BG2PC] & 0x8000)
		dy |= 0xFFFF8000;
	int dmy = io_registers[REG_BG2PD] & 0x7FFF;
	if(io_registers[REG_BG2PD] & 0x8000)
		dmy |= 0xFFFF8000;
#endif

	if(io_registers[REG_VCOUNT] == 0)
		changed = 3;

	currentX += dmx;
	currentY += dmy;

	if(changed & 1)
	{
		currentX = (BG2X_L) | ((BG2X_H & 0x07FF)<<16);
		if(BG2X_H & 0x0800)
			currentX |= 0xF8000000;
	}

	if(changed & 2)
	{
		currentY = (BG2Y_L) | ((BG2Y_H & 0x07FF)<<16);
		if(BG2Y_H & 0x0800)
			currentY |= 0xF8000000;
	}

	int realX = currentX;
	int realY = currentY;

	if(io_registers[REG_BG2CNT] & 0x40) {
		int mosaicY = ((MOSAIC & 0xF0)>>4) + 1;
		int y = io_registers[REG_VCOUNT] - (io_registers[REG_VCOUNT] % mosaicY);
		realX = startX + y*dmx;
		realY = startY + y*dmy;
	}

	int xxx = (realX >> 8);
	int yyy = (realY >> 8);

	memset(line[2], -1, 240 * sizeof(u32));
	for(u32 x = 0; x < 240; ++x)
	{
		u8 color = screenBase[yyy * 240 + xxx];
		if(unsigned(xxx) < sizeX && unsigned(yyy) < sizeY && color)
			line[2][x] = (READ16LE(&palette[color])|prio);
		realX += dx;
		realY += dy;

		xxx = (realX >> 8);
		yyy = (realY >> 8);
	}

	if(io_registers[REG_BG2CNT] & 0x40)
	{
		int mosaicX = (MOSAIC & 0xF) + 1;
		if(mosaicX > 1)
		{
			int m = 1;
			for(u32 i = 0; i < 239u; ++i)
			{
				line[2][i+1] = line[2][i];
				if(++m == mosaicX)
				{
					m = 1;
					++i;
				}
			}
		}
	}
}

INLINE void gfxDrawRotScreen16Bit160(int& currentX, int& currentY, int changed)
{
	u16 *screenBase = (io_registers[REG_DISPCNT] & 0x0010) ? (u16 *)&vram[0xa000] :
		(u16 *)&vram[0];
	int prio = ((io_registers[REG_BG2CNT] & 3) << 25) + 0x1000000;
	u32 sizeX = 160;
	u32 sizeY = 128;

	int startX = (BG2X_L) | ((BG2X_H & 0x07FF)<<16);
	if(BG2X_H & 0x0800)
		startX |= 0xF8000000;
	int startY = (BG2Y_L) | ((BG2Y_H & 0x07FF)<<16);
	if(BG2Y_H & 0x0800)
		startY |= 0xF8000000;

#ifdef BRANCHLESS_GBA_GFX
	int dx = io_registers[REG_BG2PA] & 0x7FFF;
	dx |= isel(-(io_registers[REG_BG2PA] & 0x8000), 0, 0xFFFF8000);

	int dmx = io_registers[REG_BG2PB] & 0x7FFF;
	dmx |= isel(-(io_registers[REG_BG2PB] & 0x8000), 0, 0xFFFF8000);

	int dy = io_registers[REG_BG2PC] & 0x7FFF;
	dy |= isel(-(io_registers[REG_BG2PC] & 0x8000), 0, 0xFFFF8000);

	int dmy = io_registers[REG_BG2PD] & 0x7FFF;
	dmy |= isel(-(io_registers[REG_BG2PD] & 0x8000), 0, 0xFFFF8000);
#else
	int dx = io_registers[REG_BG2PA] & 0x7FFF;
	if(io_registers[REG_BG2PA] & 0x8000)
		dx |= 0xFFFF8000;
	int dmx = io_registers[REG_BG2PB] & 0x7FFF;
	if(io_registers[REG_BG2PB] & 0x8000)
		dmx |= 0xFFFF8000;
	int dy = io_registers[REG_BG2PC] & 0x7FFF;
	if(io_registers[REG_BG2PC] & 0x8000)
		dy |= 0xFFFF8000;
	int dmy = io_registers[REG_BG2PD] & 0x7FFF;
	if(io_registers[REG_BG2PD] & 0x8000)
		dmy |= 0xFFFF8000;
#endif

	if(io_registers[REG_VCOUNT] == 0)
		changed = 3;

	currentX += dmx;
	currentY += dmy;

	if(changed & 1)
	{
		currentX = (BG2X_L) | ((BG2X_H & 0x07FF)<<16);
		if(BG2X_H & 0x0800)
			currentX |= 0xF8000000;
	}

	if(changed & 2)
	{
		currentY = (BG2Y_L) | ((BG2Y_H & 0x07FF)<<16);
		if(BG2Y_H & 0x0800)
			currentY |= 0xF8000000;
	}

	int realX = currentX;
	int realY = currentY;

	if(io_registers[REG_BG2CNT] & 0x40) {
		int mosaicY = ((MOSAIC & 0xF0)>>4) + 1;
		int y = io_registers[REG_VCOUNT] - (io_registers[REG_VCOUNT] % mosaicY);
		realX = startX + y*dmx;
		realY = startY + y*dmy;
	}

	int xxx = (realX >> 8);
	int yyy = (realY >> 8);

	memset(line[2], -1, 240 * sizeof(u32));
	for(u32 x = 0; x < 240u; ++x)
	{
		if(unsigned(xxx) < sizeX && unsigned(yyy) < sizeY)
			line[2][x] = (READ16LE(&screenBase[yyy * sizeX + xxx]) | prio);

		realX += dx;
		realY += dy;

		xxx = (realX >> 8);
		yyy = (realY >> 8);
	}


	int mosaicX = (MOSAIC & 0xF) + 1;
	if(io_registers[REG_BG2CNT] & 0x40 && (mosaicX > 1))
	{
		int m = 1;
		for(u32 i = 0; i < 239u; ++i)
		{
			line[2][i+1] = line[2][i];
			if(++m == mosaicX)
			{
				m = 1;
				++i;
			}
		}
	}
}

/* lineOBJpix is used to keep track of the drawn OBJs
   and to stop drawing them if the 'maximum number of OBJ per line'
   has been reached. */

INLINE void gfxDrawSprites (void)
{
	unsigned lineOBJpix, m;

	lineOBJpix = (io_registers[REG_DISPCNT] & 0x20) ? 954 : 1226;
	m = 0;

	u16 *sprites = (u16 *)oam;
	u16 *spritePalette = &((u16 *)graphics.paletteRAM)[256];
	int mosaicY = ((MOSAIC & 0xF000)>>12) + 1;
	int mosaicX = ((MOSAIC & 0xF00)>>8) + 1;
	for(u32 x = 0; x < 128; x++)
	{
		u16 a0 = READ16LE(sprites++);
		u16 a1 = READ16LE(sprites++);
		u16 a2 = READ16LE(sprites++);
		++sprites;

		lineOBJpixleft[x]=lineOBJpix;

		lineOBJpix-=2;
		if (lineOBJpix<=0)
			return;

		if ((a0 & 0x0c00) == 0x0c00)
			a0 &=0xF3FF;

		u16 a0val = a0>>14;

		if (a0val == 3)
		{
			a0 &= 0x3FFF;
			a1 &= 0x3FFF;
		}

		u32 sizeX = 8<<(a1>>14);
		u32 sizeY = sizeX;


		if (a0val & 1)
		{
#ifdef BRANCHLESS_GBA_GFX
			sizeX <<= isel(-(sizeX & (~31u)), 1, 0);
			sizeY >>= isel(-(sizeY>8), 0, 1);
#else
			if (sizeX<32)
				sizeX<<=1;
			if (sizeY>8)
				sizeY>>=1;
#endif
		}
		else if (a0val & 2)
		{
#ifdef BRANCHLESS_GBA_GFX
			sizeX >>= isel(-(sizeX>8), 0, 1);
			sizeY <<= isel(-(sizeY & (~31u)), 1, 0);
#else
			if (sizeX>8)
				sizeX>>=1;
			if (sizeY<32)
				sizeY<<=1;
#endif

		}


		int sy = (a0 & 255);
		int sx = (a1 & 0x1FF);

		// computes ticks used by OBJ-WIN if OBJWIN is enabled
		if (((a0 & 0x0c00) == 0x0800) && (graphics.layerEnable & 0x8000))
		{
			if ((a0 & 0x0300) == 0x0300)
			{
				sizeX<<=1;
				sizeY<<=1;
			}

#ifdef BRANCHLESS_GBA_GFX
			sy -= isel(256 - sy - sizeY, 0, 256);
			sx -= isel(512 - sx - sizeX, 0, 512);
#else
			if((sy+sizeY) > 256)
				sy -= 256;
			if ((sx+sizeX)> 512)
				sx -= 512;
#endif

			if (sx < 0)
			{
				sizeX+=sx;
				sx = 0;
			}
			else if ((sx+sizeX)>240)
				sizeX=240-sx;

			if ((io_registers[REG_VCOUNT]>=sy) && (io_registers[REG_VCOUNT]<sy+sizeY) && (sx<240))
			{
				lineOBJpix -= (sizeX-2);

				if (a0 & 0x0100)
					lineOBJpix -= (10+sizeX); 
			}
			continue;
		}

		// else ignores OBJ-WIN if OBJWIN is disabled, and ignored disabled OBJ
		else if(((a0 & 0x0c00) == 0x0800) || ((a0 & 0x0300) == 0x0200))
			continue;

		if(a0 & 0x0100)
		{
			u32 fieldX = sizeX;
			u32 fieldY = sizeY;
			if(a0 & 0x0200)
			{
				fieldX <<= 1;
				fieldY <<= 1;
			}
			if((sy+fieldY) > 256)
				sy -= 256;
			int t = io_registers[REG_VCOUNT] - sy;
			if(unsigned(t) < fieldY)
			{
				u32 startpix = 0;
				if ((sx+fieldX)> 512)
					startpix=512-sx;

				if (lineOBJpix && ((sx < 240) || startpix))
				{
					lineOBJpix-=8;
					int rot = (((a1 >> 9) & 0x1F) << 4);
					u16 *OAM = (u16 *)oam;
					int dx = READ16LE(&OAM[3 + rot]);
					if(dx & 0x8000)
						dx |= 0xFFFF8000;
					int dmx = READ16LE(&OAM[7 + rot]);
					if(dmx & 0x8000)
						dmx |= 0xFFFF8000;
					int dy = READ16LE(&OAM[11 + rot]);
					if(dy & 0x8000)
						dy |= 0xFFFF8000;
					int dmy = READ16LE(&OAM[15 + rot]);
					if(dmy & 0x8000)
						dmy |= 0xFFFF8000;

					if(a0 & 0x1000)
						t -= (t % mosaicY);

					int realX = ((sizeX) << 7) - (fieldX >> 1)*dx + ((t - (fieldY>>1))* dmx);
					int realY = ((sizeY) << 7) - (fieldX >> 1)*dy + ((t - (fieldY>>1))* dmy);

					u32 prio = (((a2 >> 10) & 3) << 25) | ((a0 & 0x0c00)<<6);

					int c = (a2 & 0x3FF);
					if((io_registers[REG_DISPCNT] & 7) > 2 && (c < 512))
						continue;

					if(a0 & 0x2000)
					{
						int inc = 32;
						if(io_registers[REG_DISPCNT] & 0x40)
							inc = sizeX >> 2;
						else
							c &= 0x3FE;
						for(u32 x = 0; x < fieldX; x++)
						{
							if (x >= startpix)
								lineOBJpix-=2;
							unsigned xxx = realX >> 8;
							unsigned yyy = realY >> 8;
							if(xxx < sizeX && yyy < sizeY && sx < 240)
							{

								u32 color = vram[0x10000 + ((((c + (yyy>>3) * inc)<<5)
								+ ((yyy & 7)<<3) + ((xxx >> 3)<<6) + (xxx & 7))&0x7FFF)];

								if ((color==0) && (((prio >> 25)&3) < ((line[4][sx]>>25)&3)))
								{
									line[4][sx] = (line[4][sx] & 0xF9FFFFFF) | prio;
									if((a0 & 0x1000) && m)
										line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
								}
								else if((color) && (prio < (line[4][sx]&0xFF000000)))
								{
									line[4][sx] = READ16LE(&spritePalette[color]) | prio;
									if((a0 & 0x1000) && m)
										line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
								}

								if ((a0 & 0x1000) && ((m+1) == mosaicX))
									m = 0;
							}
							sx = (sx+1)&511;
							realX += dx;
							realY += dy;
						}
					}
					else
					{
						int inc = 32;
						if(io_registers[REG_DISPCNT] & 0x40)
							inc = sizeX >> 3;
						int palette = (a2 >> 8) & 0xF0;
						for(u32 x = 0; x < fieldX; ++x)
						{
							if (x >= startpix)
								lineOBJpix-=2;
							unsigned xxx = realX >> 8;
							unsigned yyy = realY >> 8;
							if(xxx < sizeX && yyy < sizeY && sx < 240)
							{

								u32 color = vram[0x10000 + ((((c + (yyy>>3) * inc)<<5)
											+ ((yyy & 7)<<2) + ((xxx >> 3)<<5)
											+ ((xxx & 7)>>1))&0x7FFF)];
								if(xxx & 1)
									color >>= 4;
								else
									color &= 0x0F;

								if ((color==0) && (((prio >> 25)&3) <
											((line[4][sx]>>25)&3)))
								{
									line[4][sx] = (line[4][sx] & 0xF9FFFFFF) | prio;
									if((a0 & 0x1000) && m)
										line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
								}
								else if((color) && (prio < (line[4][sx]&0xFF000000)))
								{
									line[4][sx] = READ16LE(&spritePalette[palette+color]) | prio;
									if((a0 & 0x1000) && m)
										line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
								}
							}
							if((a0 & 0x1000) && m)
							{
								if (++m==mosaicX)
									m=0;
							}

							sx = (sx+1)&511;
							realX += dx;
							realY += dy;

						}
					}
				}
			}
		}
		else
		{
			if(sy+sizeY > 256)
				sy -= 256;
			int t = io_registers[REG_VCOUNT] - sy;
			if(unsigned(t) < sizeY)
			{
				u32 startpix = 0;
				if ((sx+sizeX)> 512)
					startpix=512-sx;

				if((sx < 240) || startpix)
				{
					lineOBJpix+=2;

					if(a1 & 0x2000)
						t = sizeY - t - 1;

					int c = (a2 & 0x3FF);
					if((io_registers[REG_DISPCNT] & 7) > 2 && (c < 512))
						continue;

					int inc = 32;
					int xxx = 0;
					if(a1 & 0x1000)
						xxx = sizeX-1;

					if(a0 & 0x1000)
						t -= (t % mosaicY);

					if(a0 & 0x2000)
					{
						if(io_registers[REG_DISPCNT] & 0x40)
							inc = sizeX >> 2;
						else
							c &= 0x3FE;

						int address = 0x10000 + ((((c+ (t>>3) * inc) << 5)
									+ ((t & 7) << 3) + ((xxx>>3)<<6) + (xxx & 7)) & 0x7FFF);

						if(a1 & 0x1000)
							xxx = 7;
						u32 prio = (((a2 >> 10) & 3) << 25) | ((a0 & 0x0c00)<<6);

						for(u32 xx = 0; xx < sizeX; xx++)
						{
							if (xx >= startpix)
								--lineOBJpix;
							if(sx < 240)
							{
								u8 color = vram[address];
								if ((color==0) && (((prio >> 25)&3) <
											((line[4][sx]>>25)&3)))
								{
									line[4][sx] = (line[4][sx] & 0xF9FFFFFF) | prio;
									if((a0 & 0x1000) && m)
										line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
								}
								else if((color) && (prio < (line[4][sx]&0xFF000000)))
								{
									line[4][sx] = READ16LE(&spritePalette[color]) | prio;
									if((a0 & 0x1000) && m)
										line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
								}

								if ((a0 & 0x1000) && ((m+1) == mosaicX))
									m = 0;
							}

							sx = (sx+1) & 511;
							if(a1 & 0x1000)
							{
								--address;
								if(--xxx == -1)
								{
									address -= 56;
									xxx = 7;
								}
								if(address < 0x10000)
									address += 0x8000;
							}
							else
							{
								++address;
								if(++xxx == 8)
								{
									address += 56;
									xxx = 0;
								}
								if(address > 0x17fff)
									address -= 0x8000;
							}
						}
					}
					else
					{
						if(io_registers[REG_DISPCNT] & 0x40)
							inc = sizeX >> 3;

						int address = 0x10000 + ((((c + (t>>3) * inc)<<5)
									+ ((t & 7)<<2) + ((xxx>>3)<<5) + ((xxx & 7) >> 1))&0x7FFF);

						u32 prio = (((a2 >> 10) & 3) << 25) | ((a0 & 0x0c00)<<6);
						int palette = (a2 >> 8) & 0xF0;
						if(a1 & 0x1000)
						{
							xxx = 7;
							int xx = sizeX - 1;
							do
							{
								if (xx >= (int)(startpix))
									--lineOBJpix;
								//if (lineOBJpix<0)
								//  continue;
								if(sx < 240)
								{
									u8 color = vram[address];
									if(xx & 1)
										color >>= 4;
									else
										color &= 0x0F;

									if ((color==0) && (((prio >> 25)&3) <
												((line[4][sx]>>25)&3)))
									{
										line[4][sx] = (line[4][sx] & 0xF9FFFFFF) | prio;
										if((a0 & 0x1000) && m)
											line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
									}
									else if((color) && (prio < (line[4][sx]&0xFF000000)))
									{
										line[4][sx] = READ16LE(&spritePalette[palette + color]) | prio;
										if((a0 & 0x1000) && m)
											line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
									}
								}

								if ((a0 & 0x1000) && ((m+1) == mosaicX))
									m=0;

								sx = (sx+1) & 511;
								if(!(xx & 1))
									--address;
								if(--xxx == -1)
								{
									xxx = 7;
									address -= 28;
								}
								if(address < 0x10000)
									address += 0x8000;
							}while(--xx >= 0);
						}
						else
						{
							for(u32 xx = 0; xx < sizeX; ++xx)
							{
								if (xx >= startpix)
									--lineOBJpix;
								//if (lineOBJpix<0)
								//  continue;
								if(sx < 240)
								{
									u8 color = vram[address];
									if(xx & 1)
										color >>= 4;
									else
										color &= 0x0F;

									if ((color==0) && (((prio >> 25)&3) <
												((line[4][sx]>>25)&3)))
									{
										line[4][sx] = (line[4][sx] & 0xF9FFFFFF) | prio;
										if((a0 & 0x1000) && m)
											line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;
									}
									else if((color) && (prio < (line[4][sx]&0xFF000000)))
									{
										line[4][sx] = READ16LE(&spritePalette[palette + color]) | prio;
										if((a0 & 0x1000) && m)
											line[4][sx]=(line[4][sx-1] & 0xF9FFFFFF) | prio;

									}
								}
								if ((a0 & 0x1000) && ((m+1) == mosaicX))
									m=0;

								sx = (sx+1) & 511;
								if(xx & 1)
									++address;
								if(++xxx == 8)
								{
									address += 28;
									xxx = 0;
								}
								if(address > 0x17fff)
									address -= 0x8000;
							}
						}
					}
				}
			}
		}
	}
}

INLINE void gfxDrawOBJWin (void)
{
	u16 *sprites = (u16 *)oam;
	for(int x = 0; x < 128 ; x++)
	{
		int lineOBJpix = lineOBJpixleft[x];
		u16 a0 = READ16LE(sprites++);
		u16 a1 = READ16LE(sprites++);
		u16 a2 = READ16LE(sprites++);
		sprites++;

		if (lineOBJpix<=0)
			return;

		// ignores non OBJ-WIN and disabled OBJ-WIN
		if(((a0 & 0x0c00) != 0x0800) || ((a0 & 0x0300) == 0x0200))
			continue;

		u16 a0val = a0>>14;

		if ((a0 & 0x0c00) == 0x0c00)
			a0 &=0xF3FF;

		if (a0val == 3)
		{
			a0 &= 0x3FFF;
			a1 &= 0x3FFF;
		}

		int sizeX = 8<<(a1>>14);
		int sizeY = sizeX;

		if (a0val & 1)
		{
#ifdef BRANCHLESS_GBA_GFX
			sizeX <<= isel(-(sizeX & (~31u)), 1, 0);
			sizeY >>= isel(-(sizeY>8), 0, 1);
#else
			if (sizeX<32)
				sizeX<<=1;
			if (sizeY>8)
				sizeY>>=1;
#endif
		}
		else if (a0val & 2)
		{
#ifdef BRANCHLESS_GBA_GFX
			sizeX >>= isel(-(sizeX>8), 0, 1);
			sizeY <<= isel(-(sizeY & (~31u)), 1, 0);
#else
			if (sizeX>8)
				sizeX>>=1;
			if (sizeY<32)
				sizeY<<=1;
#endif

		}

		int sy = (a0 & 255);

		if(a0 & 0x0100)
		{
			int fieldX = sizeX;
			int fieldY = sizeY;
			if(a0 & 0x0200)
			{
				fieldX <<= 1;
				fieldY <<= 1;
			}
			if((sy+fieldY) > 256)
				sy -= 256;
			int t = io_registers[REG_VCOUNT] - sy;
			if((t >= 0) && (t < fieldY))
			{
				int sx = (a1 & 0x1FF);
				int startpix = 0;
				if ((sx+fieldX)> 512)
					startpix=512-sx;

				if((sx < 240) || startpix)
				{
					lineOBJpix-=8;
					// int t2 = t - (fieldY >> 1);
					int rot = (a1 >> 9) & 0x1F;
					u16 *OAM = (u16 *)oam;
					int dx = READ16LE(&OAM[3 + (rot << 4)]);
					if(dx & 0x8000)
						dx |= 0xFFFF8000;
					int dmx = READ16LE(&OAM[7 + (rot << 4)]);
					if(dmx & 0x8000)
						dmx |= 0xFFFF8000;
					int dy = READ16LE(&OAM[11 + (rot << 4)]);
					if(dy & 0x8000)
						dy |= 0xFFFF8000;
					int dmy = READ16LE(&OAM[15 + (rot << 4)]);
					if(dmy & 0x8000)
						dmy |= 0xFFFF8000;

					int realX = ((sizeX) << 7) - (fieldX >> 1)*dx - (fieldY>>1)*dmx
						+ t * dmx;
					int realY = ((sizeY) << 7) - (fieldX >> 1)*dy - (fieldY>>1)*dmy
						+ t * dmy;

					int c = (a2 & 0x3FF);
					if((io_registers[REG_DISPCNT] & 7) > 2 && (c < 512))
						continue;

					int inc = 32;
					bool condition1 = a0 & 0x2000;

					if(io_registers[REG_DISPCNT] & 0x40)
						inc = sizeX >> 3;

					for(int x = 0; x < fieldX; x++)
					{
						bool cont = true;
						if (x >= startpix)
							lineOBJpix-=2;
						if (lineOBJpix<0)
							continue;
						int xxx = realX >> 8;
						int yyy = realY >> 8;

						if(xxx < 0 || xxx >= sizeX || yyy < 0 || yyy >= sizeY || sx >= 240)
							cont = false;

						if(cont)
						{
							u32 color;
							if(condition1)
								color = vram[0x10000 + ((((c + (yyy>>3) * inc)<<5)
											+ ((yyy & 7)<<3) + ((xxx >> 3)<<6) +
											(xxx & 7))&0x7fff)];
							else
							{
								color = vram[0x10000 + ((((c + (yyy>>3) * inc)<<5)
											+ ((yyy & 7)<<2) + ((xxx >> 3)<<5) +
											((xxx & 7)>>1))&0x7fff)];
								if(xxx & 1)
									color >>= 4;
								else
									color &= 0x0F;
							}

							if(color)
								line[5][sx] = 1;
						}
						sx = (sx+1)&511;
						realX += dx;
						realY += dy;
					}
				}
			}
		}
		else
		{
			if((sy+sizeY) > 256)
				sy -= 256;
			int t = io_registers[REG_VCOUNT] - sy;
			if((t >= 0) && (t < sizeY))
			{
				int sx = (a1 & 0x1FF);
				int startpix = 0;
				if ((sx+sizeX)> 512)
					startpix=512-sx;

				if((sx < 240) || startpix)
				{
					lineOBJpix+=2;
					if(a1 & 0x2000)
						t = sizeY - t - 1;
					int c = (a2 & 0x3FF);
					if((io_registers[REG_DISPCNT] & 7) > 2 && (c < 512))
						continue;
					if(a0 & 0x2000)
					{

						int inc = 32;
						if(io_registers[REG_DISPCNT] & 0x40)
							inc = sizeX >> 2;
						else
							c &= 0x3FE;

						int xxx = 0;
						if(a1 & 0x1000)
							xxx = sizeX-1;
						int address = 0x10000 + ((((c+ (t>>3) * inc) << 5)
									+ ((t & 7) << 3) + ((xxx>>3)<<6) + (xxx & 7))&0x7fff);
						if(a1 & 0x1000)
							xxx = 7;
						for(int xx = 0; xx < sizeX; xx++)
						{
							if (xx >= startpix)
								lineOBJpix--;
							if (lineOBJpix<0)
								continue;
							if(sx < 240)
							{
								u8 color = vram[address];
								if(color)
									line[5][sx] = 1;
							}

							sx = (sx+1) & 511;
							if(a1 & 0x1000) {
								xxx--;
								address--;
								if(xxx == -1) {
									address -= 56;
									xxx = 7;
								}
								if(address < 0x10000)
									address += 0x8000;
							} else {
								xxx++;
								address++;
								if(xxx == 8) {
									address += 56;
									xxx = 0;
								}
								if(address > 0x17fff)
									address -= 0x8000;
							}
						}
					}
					else
					{
						int inc = 32;
						if(io_registers[REG_DISPCNT] & 0x40)
							inc = sizeX >> 3;
						int xxx = 0;
						if(a1 & 0x1000)
							xxx = sizeX - 1;
						int address = 0x10000 + ((((c + (t>>3) * inc)<<5)
									+ ((t & 7)<<2) + ((xxx>>3)<<5) + ((xxx & 7) >> 1))&0x7fff);
						// u32 prio = (((a2 >> 10) & 3) << 25) | ((a0 & 0x0c00)<<6);
						// int palette = (a2 >> 8) & 0xF0;
						if(a1 & 0x1000)
						{
							xxx = 7;
							for(int xx = sizeX - 1; xx >= 0; xx--)
							{
								if (xx >= startpix)
									lineOBJpix--;
								if (lineOBJpix<0)
									continue;
								if(sx < 240)
								{
									u8 color = vram[address];
									if(xx & 1)
										color = (color >> 4);
									else
										color &= 0x0F;

									if(color)
										line[5][sx] = 1;
								}
								sx = (sx+1) & 511;
								xxx--;
								if(!(xx & 1))
									address--;
								if(xxx == -1) {
									xxx = 7;
									address -= 28;
								}
								if(address < 0x10000)
									address += 0x8000;
							}
						}
						else
						{
							for(int xx = 0; xx < sizeX; xx++)
							{
								if (xx >= startpix)
									lineOBJpix--;
								if (lineOBJpix<0)
									continue;
								if(sx < 240)
								{
									u8 color = vram[address];
									if(xx & 1)
										color = (color >> 4);
									else
										color &= 0x0F;

									if(color)
										line[5][sx] = 1;
								}
								sx = (sx+1) & 511;
								xxx++;
								if(xx & 1)
									address++;
								if(xxx == 8) {
									address += 28;
									xxx = 0;
								}
								if(address > 0x17fff)
									address -= 0x8000;
							}
						}
					}
				}
			}
		}
	}
}

INLINE u32 gfxIncreaseBrightness(u32 color, int coeff)
{
	color = (((color & 0xffff) << 16) | (color & 0xffff)) & 0x3E07C1F;

	color += ((((0x3E07C1F - color) * coeff) >> 4) & 0x3E07C1F);

	return (color >> 16) | color;
}

INLINE u32 gfxDecreaseBrightness(u32 color, int coeff)
{
	color = (((color & 0xffff) << 16) | (color & 0xffff)) & 0x3E07C1F;

	color -= (((color * coeff) >> 4) & 0x3E07C1F);

	return (color >> 16) | color;
}

#define GFX_ALPHA_BLEND(color, color2, ca, cb) \
	int r = AlphaClampLUT[(((color & 0x1F) * ca) >> 4) + (((color2 & 0x1F) * cb) >> 4)]; \
	int g = AlphaClampLUT[((((color >> 5) & 0x1F) * ca) >> 4) + ((((color2 >> 5) & 0x1F) * cb) >> 4)]; \
	int b = AlphaClampLUT[((((color >> 10) & 0x1F) * ca) >> 4) + ((((color2 >> 10) & 0x1F) * cb) >> 4)]; \
	color = (color & 0xFFFF0000) | (b << 10) | (g << 5) | r;

/*============================================================
	GBA.CPP
============================================================ */
static const bool useBios = true;
bool skipBios;
// it's a few bytes in the linkscript to make a multiboot image work in normal boot as well,
// and most of the ones i've seen have done that, so this is not terribly useful
static const bool cpuIsMultiBoot = false;
int cpuSaveType; // used only in init() to set up function pointers and for save file determination
bool mirroringEnable;

int cpuDmaCount;

uint8_t bios[0x4000];
uint8_t rom[0x2000000];
uint8_t internalRAM[0x8000];
uint8_t workRAM[0x40000];
uint8_t vram[0x20000];
u16 pix[2 * PIX_BUFFER_SCREEN_WIDTH * 160];
uint8_t oam[0x400];
uint8_t ioMem[0x400];

bool cpuEEPROMEnabled; // true to process writes to EEPROM at 0dxxxxxx
bool cpuEEPROMSensorEnabled; // eeprom motion sensor?  code is mostly disabled

#ifndef LSB_FIRST
bool cpuBiosSwapped = false;
#endif

INLINE int CPUUpdateTicks (void)
{
	int cpuLoopTicks = graphics.lcdTicks;

	if(timer0On && (timer0Ticks < cpuLoopTicks))
		cpuLoopTicks = timer0Ticks;

	if(timer1On && !(io_registers[REG_TM1CNT] & 4) && (timer1Ticks < cpuLoopTicks))
		cpuLoopTicks = timer1Ticks;

	if(timer2On && !(io_registers[REG_TM2CNT] & 4) && (timer2Ticks < cpuLoopTicks))
		cpuLoopTicks = timer2Ticks;

	if(timer3On && !(io_registers[REG_TM3CNT] & 4) && (timer3Ticks < cpuLoopTicks))
		cpuLoopTicks = timer3Ticks;

	if (IRQTicks)
	{
		if (IRQTicks < cpuLoopTicks)
			cpuLoopTicks = IRQTicks;
	}

	return cpuLoopTicks;
}

#define CPUUpdateWindow0() \
{ \
  int x00_window0 = io_registers[REG_WIN0H] >>8; \
  int x01_window0 = io_registers[REG_WIN0H] & 255; \
  int x00_lte_x01 = x00_window0 <= x01_window0; \
  for(int i = 0; i < 240; i++) \
      gfxInWin[0][i] = ((i >= x00_window0 && i < x01_window0) & x00_lte_x01) | ((i >= x00_window0 || i < x01_window0) & ~x00_lte_x01); \
}

#define CPUUpdateWindow1() \
{ \
  int x00_window1 = io_registers[REG_WIN1H]>>8; \
  int x01_window1 = io_registers[REG_WIN1H] & 255; \
  int x00_lte_x01 = x00_window1 <= x01_window1; \
  for(int i = 0; i < 240; i++) \
   gfxInWin[1][i] = ((i >= x00_window1 && i < x01_window1) & x00_lte_x01) | ((i >= x00_window1 || i < x01_window1) & ~x00_lte_x01); \
}

#define CPUCompareVCOUNT() \
  if(io_registers[REG_VCOUNT] == (io_registers[REG_DISPSTAT] >> 8)) \
  { \
    io_registers[REG_DISPSTAT] |= 4; \
    UPDATE_REG(0x04, io_registers[REG_DISPSTAT]); \
    if(io_registers[REG_DISPSTAT] & 0x20) \
    { \
      io_registers[REG_IF] |= 4; \
      UPDATE_REG(0x202, io_registers[REG_IF]); \
    } \
  } \
  else \
  { \
    io_registers[REG_DISPSTAT] &= 0xFFFB; \
    UPDATE_REG(0x4, io_registers[REG_DISPSTAT]); \
  } \
  if (graphics.layerEnableDelay > 0) \
  { \
      graphics.layerEnableDelay--; \
      if (graphics.layerEnableDelay == 1) \
          graphics.layerEnable = io_registers[REG_DISPCNT]; \
  }

int CPULoadRom(const u8 *romfile, const u32 romfilelen)
{
	if (cpuIsMultiBoot)
	{
		if (romfilelen > 0x40000)
			return 0;
	}
	else
	{
		if (romfilelen > 0x2000000)
			return 0;
	}

	uint8_t *whereToLoad = cpuIsMultiBoot ? workRAM : rom;
	
	memcpy(whereToLoad, romfile, romfilelen);
	romSize = romfilelen;

	uint16_t *temp = (uint16_t *)(rom+((romSize+1)&~1));
	int i;
	for(i = (romSize+1)&~1; i < 0x2000000; i+=2) {
		WRITE16LE(temp, (i >> 1) & 0xFFFF);
		temp++;
	}


	flashInit();
	eepromInit();

	memset(line[0], -1, 240 * sizeof(u32));
	memset(line[1], -1, 240 * sizeof(u32));
	memset(line[2], -1, 240 * sizeof(u32));
	memset(line[3], -1, 240 * sizeof(u32));

	return romSize;
}

void doMirroring (bool b)
{
	uint32_t mirroredRomSize = (((romSize)>>20) & 0x3F)<<20;
	uint32_t mirroredRomAddress = romSize;
	if ((mirroredRomSize <=0x800000) && (b))
	{
		mirroredRomAddress = mirroredRomSize;
		if (mirroredRomSize==0)
			mirroredRomSize=0x100000;
		while (mirroredRomAddress<0x01000000)
		{
			memcpy((uint16_t *)(rom+mirroredRomAddress), (uint16_t *)(rom), mirroredRomSize);
			mirroredRomAddress+=mirroredRomSize;
		}
	}
}

#define brightness_switch() \
      switch((BLDMOD >> 6) & 3) \
      { \
         case 2: \
               color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]); \
               break; \
         case 3: \
               color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]); \
               break; \
      }

#define alpha_blend_brightness_switch() \
      if(top2 & (BLDMOD>>8)) \
	if(color < 0x80000000) \
	{ \
		GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]); \
	} \
      else if(BLDMOD & top) \
      { \
         brightness_switch(); \
      }

/* we only use 16bit color depth */
#define INIT_COLOR_DEPTH_LINE_MIX() uint16_t * lineMix = (pix + PIX_BUFFER_SCREEN_WIDTH * io_registers[REG_VCOUNT])

void mode0RenderLine (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 0: Render Line\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

  if(graphics.layerEnable & 0x0100) {
    gfxDrawTextScreen(io_registers[REG_BG0CNT], io_registers[REG_BG0HOFS], io_registers[REG_BG0VOFS], line[0]);
  }

  if(graphics.layerEnable & 0x0200) {
    gfxDrawTextScreen(io_registers[REG_BG1CNT], io_registers[REG_BG1HOFS], io_registers[REG_BG1VOFS], line[1]);
  }

  if(graphics.layerEnable & 0x0400) {
    gfxDrawTextScreen(io_registers[REG_BG2CNT], io_registers[REG_BG2HOFS], io_registers[REG_BG2VOFS], line[2]);
  }

  if(graphics.layerEnable & 0x0800) {
    gfxDrawTextScreen(io_registers[REG_BG3CNT], io_registers[REG_BG3HOFS], io_registers[REG_BG3VOFS], line[3]);
  }


	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; x++)
	{
		uint32_t color = backdrop;
		uint8_t top = 0x20;

		if(line[0][x] < color) {
			color = line[0][x];
			top = 0x01;
		}

		if((uint8_t)(line[1][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[1][x];
			top = 0x02;
		}

		if((uint8_t)(line[2][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[2][x];
			top = 0x04;
		}

		if((uint8_t)(line[3][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[3][x];
			top = 0x08;
		}

		if((uint8_t)(line[4][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[4][x];
			top = 0x10;

			if(color & 0x00010000) {
				// semi-transparent OBJ
				uint32_t back = backdrop;
				uint8_t top2 = 0x20;

				if((uint8_t)(line[0][x]>>24) < (uint8_t)(back >> 24)) {
					back = line[0][x];
					top2 = 0x01;
				}

				if((uint8_t)(line[1][x]>>24) < (uint8_t)(back >> 24)) {
					back = line[1][x];
					top2 = 0x02;
				}

				if((uint8_t)(line[2][x]>>24) < (uint8_t)(back >> 24)) {
					back = line[2][x];
					top2 = 0x04;
				}

				if((uint8_t)(line[3][x]>>24) < (uint8_t)(back >> 24)) {
					back = line[3][x];
					top2 = 0x08;
				}

				alpha_blend_brightness_switch();
			}
		}


		lineMix[x] = CONVERT_COLOR(color);
	}
}

void mode0RenderLineNoWindow (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 0: Render Line No Window\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;
	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

   if(graphics.layerEnable & 0x0100) {
      gfxDrawTextScreen(io_registers[REG_BG0CNT], io_registers[REG_BG0HOFS], io_registers[REG_BG0VOFS], line[0]);
   }

   if(graphics.layerEnable & 0x0200) {
      gfxDrawTextScreen(io_registers[REG_BG1CNT], io_registers[REG_BG1HOFS], io_registers[REG_BG1VOFS], line[1]);
   }

   if(graphics.layerEnable & 0x0400) {
      gfxDrawTextScreen(io_registers[REG_BG2CNT], io_registers[REG_BG2HOFS], io_registers[REG_BG2VOFS], line[2]);
   }

   if(graphics.layerEnable & 0x0800) {
      gfxDrawTextScreen(io_registers[REG_BG3CNT], io_registers[REG_BG3HOFS], io_registers[REG_BG3VOFS], line[3]);
   }

	int effect = (BLDMOD >> 6) & 3;

	for(int x = 0; x < 240; x++) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;

		if(line[0][x] < color) {
			color = line[0][x];
			top = 0x01;
		}

		if(line[1][x] < (color & 0xFF000000)) {
			color = line[1][x];
			top = 0x02;
		}

		if(line[2][x] < (color & 0xFF000000)) {
			color = line[2][x];
			top = 0x04;
		}

		if(line[3][x] < (color & 0xFF000000)) {
			color = line[3][x];
			top = 0x08;
		}

		if(line[4][x] < (color & 0xFF000000)) {
			color = line[4][x];
			top = 0x10;
		}

		if(!(color & 0x00010000)) {
			switch(effect) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = backdrop;
						uint8_t top2 = 0x20;
						if((line[0][x] < back) && (top != 0x01))
						{
							back = line[0][x];
							top2 = 0x01;
						}

						if((line[1][x] < (back & 0xFF000000)) && (top != 0x02))
						{
							back = line[1][x];
							top2 = 0x02;
						}

						if((line[2][x] < (back & 0xFF000000)) && (top != 0x04))
						{
							back = line[2][x];
							top2 = 0x04;
						}

						if((line[3][x] < (back & 0xFF000000)) && (top != 0x08))
						{
							back = line[3][x];
							top2 = 0x08;
						}

						if((line[4][x] < (back & 0xFF000000)) && (top != 0x10))
						{
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}

					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		} else {
			// semi-transparent OBJ
			uint32_t back = backdrop;
			uint8_t top2 = 0x20;

			if(line[0][x] < back) {
				back = line[0][x];
				top2 = 0x01;
			}

			if(line[1][x] < (back & 0xFF000000)) {
				back = line[1][x];
				top2 = 0x02;
			}

			if(line[2][x] < (back & 0xFF000000)) {
				back = line[2][x];
				top2 = 0x04;
			}

			if(line[3][x] < (back & 0xFF000000)) {
				back = line[3][x];
				top2 = 0x08;
			}

			alpha_blend_brightness_switch();
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
}

void mode0RenderLineAll (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 0: Render Line All\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	bool inWindow0 = false;
	bool inWindow1 = false;

	if(graphics.layerEnable & 0x2000) {
		uint8_t v0 = io_registers[REG_WIN0V] >> 8;
		uint8_t v1 = io_registers[REG_WIN0V] & 255;
		inWindow0 = ((v0 == v1) && (v0 >= 0xe8));
		if(v1 >= v0)
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
	}
	if(graphics.layerEnable & 0x4000) {
		uint8_t v0 = io_registers[REG_WIN1V] >> 8;
		uint8_t v1 = io_registers[REG_WIN1V] & 255;
		inWindow1 = ((v0 == v1) && (v0 >= 0xe8));
		if(v1 >= v0)
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
	}

  if((graphics.layerEnable & 0x0100)) {
    gfxDrawTextScreen(io_registers[REG_BG0CNT], io_registers[REG_BG0HOFS], io_registers[REG_BG0VOFS], line[0]);
  }

  if((graphics.layerEnable & 0x0200)) {
    gfxDrawTextScreen(io_registers[REG_BG1CNT], io_registers[REG_BG1HOFS], io_registers[REG_BG1VOFS], line[1]);
  }

  if((graphics.layerEnable & 0x0400)) {
    gfxDrawTextScreen(io_registers[REG_BG2CNT], io_registers[REG_BG2HOFS], io_registers[REG_BG2VOFS], line[2]);
  }

  if((graphics.layerEnable & 0x0800)) {
    gfxDrawTextScreen(io_registers[REG_BG3CNT], io_registers[REG_BG3HOFS], io_registers[REG_BG3VOFS], line[3]);
  }

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	uint8_t inWin0Mask = io_registers[REG_WININ] & 0xFF;
	uint8_t inWin1Mask = io_registers[REG_WININ] >> 8;
	uint8_t outMask = io_registers[REG_WINOUT] & 0xFF;

	for(int x = 0; x < 240; x++) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;
		uint8_t mask = outMask;

		if(!(line[5][x] & 0x80000000)) {
			mask = io_registers[REG_WINOUT] >> 8;
		}

		int32_t window1_mask = ((inWindow1 & gfxInWin[1][x]) | -(inWindow1 & gfxInWin[1][x])) >> 31;
		int32_t window0_mask = ((inWindow0 & gfxInWin[0][x]) | -(inWindow0 & gfxInWin[0][x])) >> 31;
		mask = (inWin1Mask & window1_mask) | (mask & ~window1_mask);
		mask = (inWin0Mask & window0_mask) | (mask & ~window0_mask);

		if((mask & 1) && (line[0][x] < color)) {
			color = line[0][x];
			top = 0x01;
		}

		if((mask & 2) && ((uint8_t)(line[1][x]>>24) < (uint8_t)(color >> 24))) {
			color = line[1][x];
			top = 0x02;
		}

		if((mask & 4) && ((uint8_t)(line[2][x]>>24) < (uint8_t)(color >> 24))) {
			color = line[2][x];
			top = 0x04;
		}

		if((mask & 8) && ((uint8_t)(line[3][x]>>24) < (uint8_t)(color >> 24))) {
			color = line[3][x];
			top = 0x08;
		}

		if((mask & 16) && ((uint8_t)(line[4][x]>>24) < (uint8_t)(color >> 24))) {
			color = line[4][x];
			top = 0x10;
		}

		if(color & 0x00010000)
		{
			// semi-transparent OBJ
			uint32_t back = backdrop;
			uint8_t top2 = 0x20;

			if((mask & 1) && ((uint8_t)(line[0][x]>>24) < (uint8_t)(back >> 24))) {
				back = line[0][x];
				top2 = 0x01;
			}

			if((mask & 2) && ((uint8_t)(line[1][x]>>24) < (uint8_t)(back >> 24))) {
				back = line[1][x];
				top2 = 0x02;
			}

			if((mask & 4) && ((uint8_t)(line[2][x]>>24) < (uint8_t)(back >> 24))) {
				back = line[2][x];
				top2 = 0x04;
			}

			if((mask & 8) && ((uint8_t)(line[3][x]>>24) < (uint8_t)(back >> 24))) {
				back = line[3][x];
				top2 = 0x08;
			}

			alpha_blend_brightness_switch();
		}
		else if((mask & 32) && (top & BLDMOD))
		{
			// special FX on in the window
			switch((BLDMOD >> 6) & 3)
			{
				case 0:
					break;
				case 1:
					{
						uint32_t back = backdrop;
						uint8_t top2 = 0x20;
						if(((mask & 1) && (uint8_t)(line[0][x]>>24) < (uint8_t)(back >> 24)) && top != 0x01)
						{
							back = line[0][x];
							top2 = 0x01;
						}

						if(((mask & 2) && (uint8_t)(line[1][x]>>24) < (uint8_t)(back >> 24)) && top != 0x02)
						{
							back = line[1][x];
							top2 = 0x02;
						}

						if(((mask & 4) && (uint8_t)(line[2][x]>>24) < (uint8_t)(back >> 24)) && top != 0x04)
						{
							back = line[2][x];
							top2 = 0x04;
						}

						if(((mask & 8) && (uint8_t)(line[3][x]>>24) < (uint8_t)(back >> 24)) && top != 0x08)
						{
							back = line[3][x];
							top2 = 0x08;
						}

						if(((mask & 16) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) && top != 0x10) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
}

/*
Mode 1 is a tiled graphics mode, but with background layer 2 supporting scaling and rotation.
There is no layer 3 in this mode.
Layers 0 and 1 can be either 16 colours (with 16 different palettes) or 256 colours. 
There are 1024 tiles available.
Layer 2 is 256 colours and allows only 256 tiles.

These routines only render a single line at a time, because of the way the GBA does events.
*/

void mode1RenderLine (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 1: Render Line\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

  if(graphics.layerEnable & 0x0100) {
    gfxDrawTextScreen(io_registers[REG_BG0CNT], io_registers[REG_BG0HOFS], io_registers[REG_BG0VOFS], line[0]);
  }

  if(graphics.layerEnable & 0x0200) {
    gfxDrawTextScreen(io_registers[REG_BG1CNT], io_registers[REG_BG1HOFS], io_registers[REG_BG1VOFS], line[1]);
  }

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif
		gfxDrawRotScreen(io_registers[REG_BG2CNT], BG2X_L, BG2X_H, BG2Y_L, BG2Y_H,
				io_registers[REG_BG2PA], io_registers[REG_BG2PB], io_registers[REG_BG2PC], io_registers[REG_BG2PD],
				gfxBG2X, gfxBG2Y, changed, line[2]);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	for(uint32_t x = 0; x < 240u; ++x) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;

		uint8_t li1 = (uint8_t)(line[1][x]>>24);
		uint8_t li2 = (uint8_t)(line[2][x]>>24);
		uint8_t li4 = (uint8_t)(line[4][x]>>24);	

		uint8_t r = 	(li2 < li1) ? (li2) : (li1);

		if(li4 < r){
			r = 	(li4);
		}

		if(line[0][x] < backdrop) {
			color = line[0][x];
			top = 0x01;
		}

		if(r < (uint8_t)(color >> 24)) {
			if(r == li1){
				color = line[1][x];
				top = 0x02;
			}else if(r == li2){
				color = line[2][x];
				top = 0x04;
			}else if(r == li4){
				color = line[4][x];
				top = 0x10;
				if((color & 0x00010000))
				{
					// semi-transparent OBJ
					uint32_t back = backdrop;
					uint8_t top2 = 0x20;

					uint8_t li0 = (uint8_t)(line[0][x]>>24);
					uint8_t li1 = (uint8_t)(line[1][x]>>24);
					uint8_t li2 = (uint8_t)(line[2][x]>>24);
					uint8_t r = 	(li1 < li0) ? (li1) : (li0);

					if(li2 < r) {
						r =  (li2);
					}

					if(r < (uint8_t)(back >> 24)) {
						if(r == li0){
							back = line[0][x];
							top2 = 0x01;
						}else if(r == li1){
							back = line[1][x];
							top2 = 0x02;
						}else if(r == li2){
							back = line[2][x];
							top2 = 0x04;
						}
					}

					alpha_blend_brightness_switch();
				}
			}
		}


		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode1RenderLineNoWindow (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 1: Render Line No Window\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

  if(graphics.layerEnable & 0x0100) {
    gfxDrawTextScreen(io_registers[REG_BG0CNT], io_registers[REG_BG0HOFS], io_registers[REG_BG0VOFS], line[0]);
  }


  if(graphics.layerEnable & 0x0200) {
    gfxDrawTextScreen(io_registers[REG_BG1CNT], io_registers[REG_BG1HOFS], io_registers[REG_BG1VOFS], line[1]);
  }

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif
		gfxDrawRotScreen(io_registers[REG_BG2CNT], BG2X_L, BG2X_H, BG2Y_L, BG2Y_H,
				io_registers[REG_BG2PA], io_registers[REG_BG2PB], io_registers[REG_BG2PC], io_registers[REG_BG2PD],
				gfxBG2X, gfxBG2Y, changed, line[2]);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;

		uint8_t li1 = (uint8_t)(line[1][x]>>24);
		uint8_t li2 = (uint8_t)(line[2][x]>>24);
		uint8_t li4 = (uint8_t)(line[4][x]>>24);	

		uint8_t r = 	(li2 < li1) ? (li2) : (li1);

		if(li4 < r){
			r = 	(li4);
		}

		if(line[0][x] < backdrop) {
			color = line[0][x];
			top = 0x01;
		}

		if(r < (uint8_t)(color >> 24)) {
			if(r == li1){
				color = line[1][x];
				top = 0x02;
			}else if(r == li2){
				color = line[2][x];
				top = 0x04;
			}else if(r == li4){
				color = line[4][x];
				top = 0x10;
			}
		}

		if(!(color & 0x00010000)) {
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = backdrop;
						uint8_t top2 = 0x20;

						if((top != 0x01) && (uint8_t)(line[0][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[0][x];
							top2 = 0x01;
						}

						if((top != 0x02) && (uint8_t)(line[1][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[1][x];
							top2 = 0x02;
						}

						if((top != 0x04) && (uint8_t)(line[2][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		} else {
			// semi-transparent OBJ
			uint32_t back = backdrop;
			uint8_t top2 = 0x20;

			uint8_t li0 = (uint8_t)(line[0][x]>>24);
			uint8_t li1 = (uint8_t)(line[1][x]>>24);
			uint8_t li2 = (uint8_t)(line[2][x]>>24);	

			uint8_t r = 	(li1 < li0) ? (li1) : (li0);

			if(li2 < r) {
				r =  (li2);
			}

			if(r < (uint8_t)(back >> 24))
			{
				if(r == li0)
				{
					back = line[0][x];
					top2 = 0x01;
				}
				else if(r == li1)
				{
					back = line[1][x];
					top2 = 0x02;
				}
				else if(r == li2)
				{
					back = line[2][x];
					top2 = 0x04;
				}
			}

			alpha_blend_brightness_switch();
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode1RenderLineAll (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 1: Render Line All\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	bool inWindow0 = false;
	bool inWindow1 = false;

	if(graphics.layerEnable & 0x2000)
	{
		uint8_t v0 = io_registers[REG_WIN0V] >> 8;
		uint8_t v1 = io_registers[REG_WIN0V] & 255;
		inWindow0 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow0 = (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}
	if(graphics.layerEnable & 0x4000)
	{
		uint8_t v0 = io_registers[REG_WIN1V] >> 8;
		uint8_t v1 = io_registers[REG_WIN1V] & 255;
		inWindow1 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow1 = (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}

  if(graphics.layerEnable & 0x0100) {
    gfxDrawTextScreen(io_registers[REG_BG0CNT], io_registers[REG_BG0HOFS], io_registers[REG_BG0VOFS], line[0]);
  }

  if(graphics.layerEnable & 0x0200) {
    gfxDrawTextScreen(io_registers[REG_BG1CNT], io_registers[REG_BG1HOFS], io_registers[REG_BG1VOFS], line[1]);
  }

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif
		gfxDrawRotScreen(io_registers[REG_BG2CNT], BG2X_L, BG2X_H, BG2Y_L, BG2Y_H,
				io_registers[REG_BG2PA], io_registers[REG_BG2PB], io_registers[REG_BG2PC], io_registers[REG_BG2PD],
				gfxBG2X, gfxBG2Y, changed, line[2]);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	uint8_t inWin0Mask = io_registers[REG_WININ] & 0xFF;
	uint8_t inWin1Mask = io_registers[REG_WININ] >> 8;
	uint8_t outMask = io_registers[REG_WINOUT] & 0xFF;

	for(int x = 0; x < 240; ++x) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;
		uint8_t mask = outMask;

		if(!(line[5][x] & 0x80000000)) {
			mask = io_registers[REG_WINOUT] >> 8;
		}

		int32_t window1_mask = ((inWindow1 & gfxInWin[1][x]) | -(inWindow1 & gfxInWin[1][x])) >> 31;
		int32_t window0_mask = ((inWindow0 & gfxInWin[0][x]) | -(inWindow0 & gfxInWin[0][x])) >> 31;
		mask = (inWin1Mask & window1_mask) | (mask & ~window1_mask);
		mask = (inWin0Mask & window0_mask) | (mask & ~window0_mask);

		// At the very least, move the inexpensive 'mask' operation up front
		if((mask & 1) && line[0][x] < backdrop) {
			color = line[0][x];
			top = 0x01;
		}

		if((mask & 2) && (uint8_t)(line[1][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[1][x];
			top = 0x02;
		}

		if((mask & 4) && (uint8_t)(line[2][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[2][x];
			top = 0x04;
		}

		if((mask & 16) && (uint8_t)(line[4][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[4][x];
			top = 0x10;
		}

		if(color & 0x00010000) {
			// semi-transparent OBJ
			uint32_t back = backdrop;
			uint8_t top2 = 0x20;

			if((mask & 1) && (uint8_t)(line[0][x]>>24) < (uint8_t)(backdrop >> 24)) {
				back = line[0][x];
				top2 = 0x01;
			}

			if((mask & 2) && (uint8_t)(line[1][x]>>24) < (uint8_t)(back >> 24)) {
				back = line[1][x];
				top2 = 0x02;
			}

			if((mask & 4) && (uint8_t)(line[2][x]>>24) < (uint8_t)(back >> 24)) {
				back = line[2][x];
				top2 = 0x04;
			}

			alpha_blend_brightness_switch();
		} else if(mask & 32) {
			// special FX on the window
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = backdrop;
						uint8_t top2 = 0x20;

						if((mask & 1) && (top != 0x01) && (uint8_t)(line[0][x]>>24) < (uint8_t)(backdrop >> 24)) {
							back = line[0][x];
							top2 = 0x01;
						}

						if((mask & 2) && (top != 0x02) && (uint8_t)(line[1][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[1][x];
							top2 = 0x02;
						}

						if((mask & 4) && (top != 0x04) && (uint8_t)(line[2][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((mask & 16) && (top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

/*
Mode 2 is a 256 colour tiled graphics mode which supports scaling and rotation.
There is no background layer 0 or 1 in this mode. Only background layers 2 and 3.
There are 256 tiles available.
It does not support flipping.

These routines only render a single line at a time, because of the way the GBA does events.
*/

void mode2RenderLine (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 2: Render Line\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen(io_registers[REG_BG2CNT], BG2X_L, BG2X_H, BG2Y_L, BG2Y_H,
				io_registers[REG_BG2PA], io_registers[REG_BG2PB], io_registers[REG_BG2PC], io_registers[REG_BG2PD], gfxBG2X, gfxBG2Y,
				changed, line[2]);
	}

	if(graphics.layerEnable & 0x0800) {
		int changed = gfxBG3Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen(io_registers[REG_BG3CNT], BG3X_L, BG3X_H, BG3Y_L, BG3Y_H,
				io_registers[REG_BG3PA], io_registers[REG_BG3PB], io_registers[REG_BG3PC], io_registers[REG_BG3PD], gfxBG3X, gfxBG3Y,
				changed, line[3]);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;

		uint8_t li2 = (uint8_t)(line[2][x]>>24);
		uint8_t li3 = (uint8_t)(line[3][x]>>24);
		uint8_t li4 = (uint8_t)(line[4][x]>>24);	

		uint8_t r = 	(li3 < li2) ? (li3) : (li2);

		if(li4 < r){
			r = 	(li4);
		}

		if(r < (uint8_t)(color >> 24)) {
			if(r == li2){
				color = line[2][x];
				top = 0x04;
			}else if(r == li3){
				color = line[3][x];
				top = 0x08;
			}else if(r == li4){
				color = line[4][x];
				top = 0x10;

				if(color & 0x00010000) {
					// semi-transparent OBJ
					uint32_t back = backdrop;
					uint8_t top2 = 0x20;

					uint8_t li2 = (uint8_t)(line[2][x]>>24);
					uint8_t li3 = (uint8_t)(line[3][x]>>24);
					uint8_t r = 	(li3 < li2) ? (li3) : (li2);

					if(r < (uint8_t)(back >> 24)) {
						if(r == li2){
							back = line[2][x];
							top2 = 0x04;
						}else if(r == li3){
							back = line[3][x];
							top2 = 0x08;
						}
					}

					alpha_blend_brightness_switch();
				}
			}
		}


		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	gfxBG3Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode2RenderLineNoWindow (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 2: Render Line No Window\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen(io_registers[REG_BG2CNT], BG2X_L, BG2X_H, BG2Y_L, BG2Y_H,
				io_registers[REG_BG2PA], io_registers[REG_BG2PB], io_registers[REG_BG2PC], io_registers[REG_BG2PD], gfxBG2X, gfxBG2Y,
				changed, line[2]);
	}

	if(graphics.layerEnable & 0x0800) {
		int changed = gfxBG3Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen(io_registers[REG_BG3CNT], BG3X_L, BG3X_H, BG3Y_L, BG3Y_H,
				io_registers[REG_BG3PA], io_registers[REG_BG3PB], io_registers[REG_BG3PC], io_registers[REG_BG3PD], gfxBG3X, gfxBG3Y,
				changed, line[3]);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;

		uint8_t li2 = (uint8_t)(line[2][x]>>24);
		uint8_t li3 = (uint8_t)(line[3][x]>>24);
		uint8_t li4 = (uint8_t)(line[4][x]>>24);	

		uint8_t r = 	(li3 < li2) ? (li3) : (li2);

		if(li4 < r){
			r = 	(li4);
		}

		if(r < (uint8_t)(color >> 24)) {
			if(r == li2){
				color = line[2][x];
				top = 0x04;
			}else if(r == li3){
				color = line[3][x];
				top = 0x08;
			}else if(r == li4){
				color = line[4][x];
				top = 0x10;
			}
		}

		if(!(color & 0x00010000)) {
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = backdrop;
						uint8_t top2 = 0x20;

						if((top != 0x04) && (uint8_t)(line[2][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((top != 0x08) && (uint8_t)(line[3][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[3][x];
							top2 = 0x08;
						}

						if((top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		} else {
			// semi-transparent OBJ
			uint32_t back = backdrop;
			uint8_t top2 = 0x20;

			uint8_t li2 = (uint8_t)(line[2][x]>>24);
			uint8_t li3 = (uint8_t)(line[3][x]>>24);
			uint8_t r = 	(li3 < li2) ? (li3) : (li2);

			if(r < (uint8_t)(back >> 24)) {
				if(r == li2){
					back = line[2][x];
					top2 = 0x04;
				}else if(r == li3){
					back = line[3][x];
					top2 = 0x08;
				}
			}

			alpha_blend_brightness_switch();
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	gfxBG3Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode2RenderLineAll (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 2: Render Line All\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	bool inWindow0 = false;
	bool inWindow1 = false;

	if(graphics.layerEnable & 0x2000)
	{
		uint8_t v0 = io_registers[REG_WIN0V] >> 8;
		uint8_t v1 = io_registers[REG_WIN0V] & 255;
		inWindow0 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow0 = (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}
	if(graphics.layerEnable & 0x4000)
	{
		uint8_t v0 = io_registers[REG_WIN1V] >> 8;
		uint8_t v1 = io_registers[REG_WIN1V] & 255;
		inWindow1 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow1 = (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen(io_registers[REG_BG2CNT], BG2X_L, BG2X_H, BG2Y_L, BG2Y_H,
				io_registers[REG_BG2PA], io_registers[REG_BG2PB], io_registers[REG_BG2PC], io_registers[REG_BG2PD], gfxBG2X, gfxBG2Y,
				changed, line[2]);
	}

	if(graphics.layerEnable & 0x0800) {
		int changed = gfxBG3Changed;
#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen(io_registers[REG_BG3CNT], BG3X_L, BG3X_H, BG3Y_L, BG3Y_H,
				io_registers[REG_BG3PA], io_registers[REG_BG3PB], io_registers[REG_BG3PC], io_registers[REG_BG3PD], gfxBG3X, gfxBG3Y,
				changed, line[3]);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	uint8_t inWin0Mask = io_registers[REG_WININ] & 0xFF;
	uint8_t inWin1Mask = io_registers[REG_WININ] >> 8;
	uint8_t outMask = io_registers[REG_WINOUT] & 0xFF;

	for(int x = 0; x < 240; x++) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;
		uint8_t mask = outMask;

		if(!(line[5][x] & 0x80000000)) {
			mask = io_registers[REG_WINOUT] >> 8;
		}

		int32_t window1_mask = ((inWindow1 & gfxInWin[1][x]) | -(inWindow1 & gfxInWin[1][x])) >> 31;
		int32_t window0_mask = ((inWindow0 & gfxInWin[0][x]) | -(inWindow0 & gfxInWin[0][x])) >> 31;
		mask = (inWin1Mask & window1_mask) | (mask & ~window1_mask);
		mask = (inWin0Mask & window0_mask) | (mask & ~window0_mask);

		if((mask & 4) && line[2][x] < color) {
			color = line[2][x];
			top = 0x04;
		}

		if((mask & 8) && (uint8_t)(line[3][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[3][x];
			top = 0x08;
		}

		if((mask & 16) && (uint8_t)(line[4][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[4][x];
			top = 0x10;
		}

		if(color & 0x00010000) {
			// semi-transparent OBJ
			uint32_t back = backdrop;
			uint8_t top2 = 0x20;

			if((mask & 4) && line[2][x] < back) {
				back = line[2][x];
				top2 = 0x04;
			}

			if((mask & 8) && (uint8_t)(line[3][x]>>24) < (uint8_t)(back >> 24)) {
				back = line[3][x];
				top2 = 0x08;
			}

			alpha_blend_brightness_switch();
		} else if(mask & 32) {
			// special FX on the window
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = backdrop;
						uint8_t top2 = 0x20;

						if((mask & 4) && (top != 0x04) && line[2][x] < back) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((mask & 8) && (top != 0x08) && (uint8_t)(line[3][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[3][x];
							top2 = 0x08;
						}

						if((mask & 16) && (top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	gfxBG3Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

/*
Mode 3 is a 15-bit (32768) colour bitmap graphics mode.
It has a single layer, background layer 2, the same size as the screen.
It doesn't support paging, scrolling, flipping, rotation or tiles.

These routines only render a single line at a time, because of the way the GBA does events.
*/

void mode3RenderLine (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 3: Render Line\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();
	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen16Bit(gfxBG2X, gfxBG2Y, changed);
	}

	uint32_t background = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = background;
		uint8_t top = 0x20;

		if(line[2][x] < color) {
			color = line[2][x];
			top = 0x04;
		}

		if((uint8_t)(line[4][x]>>24) < (uint8_t)(color >>24)) {
			color = line[4][x];
			top = 0x10;

			if(color & 0x00010000) {
				// semi-transparent OBJ
				uint32_t back = background;
				uint8_t top2 = 0x20;

				if(line[2][x] < background) {
					back = line[2][x];
					top2 = 0x04;
				}

				alpha_blend_brightness_switch();
			}
		}


		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode3RenderLineNoWindow (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 3: Render Line No Window\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();
	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen16Bit(gfxBG2X, gfxBG2Y, changed);
	}

	uint32_t background = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = background;
		uint8_t top = 0x20;

		if(line[2][x] < background) {
			color = line[2][x];
			top = 0x04;
		}

		if((uint8_t)(line[4][x]>>24) < (uint8_t)(color >>24)) {
			color = line[4][x];
			top = 0x10;
		}

		if(!(color & 0x00010000)) {
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = background;
						uint8_t top2 = 0x20;

						if(top != 0x04 && (line[2][x] < background) ) {
							back = line[2][x];
							top2 = 0x04;
						}

						if(top != 0x10 && ((uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24))) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}

					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		} else {
			// semi-transparent OBJ
			uint32_t back = background;
			uint8_t top2 = 0x20;

			if(line[2][x] < background) {
				back = line[2][x];
				top2 = 0x04;
			}

			alpha_blend_brightness_switch();
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode3RenderLineAll (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 3: Render Line All\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();
	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	bool inWindow0 = false;
	bool inWindow1 = false;

	if(graphics.layerEnable & 0x2000)
	{
		uint8_t v0 = io_registers[REG_WIN0V] >> 8;
		uint8_t v1 = io_registers[REG_WIN0V] & 255;
		inWindow0 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow0 = (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}

	if(graphics.layerEnable & 0x4000)
	{
		uint8_t v0 = io_registers[REG_WIN1V] >> 8;
		uint8_t v1 = io_registers[REG_WIN1V] & 255;
		inWindow1 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow1 = (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen16Bit(gfxBG2X, gfxBG2Y, changed);
	}

	uint8_t inWin0Mask = io_registers[REG_WININ] & 0xFF;
	uint8_t inWin1Mask = io_registers[REG_WININ] >> 8;
	uint8_t outMask = io_registers[REG_WINOUT] & 0xFF;

	uint32_t background = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = background;
		uint8_t top = 0x20;
		uint8_t mask = outMask;

		if(!(line[5][x] & 0x80000000)) {
			mask = io_registers[REG_WINOUT] >> 8;
		}

		int32_t window1_mask = ((inWindow1 & gfxInWin[1][x]) | -(inWindow1 & gfxInWin[1][x])) >> 31;
		int32_t window0_mask = ((inWindow0 & gfxInWin[0][x]) | -(inWindow0 & gfxInWin[0][x])) >> 31;
		mask = (inWin1Mask & window1_mask) | (mask & ~window1_mask);
		mask = (inWin0Mask & window0_mask) | (mask & ~window0_mask);

		if((mask & 4) && line[2][x] < background) {
			color = line[2][x];
			top = 0x04;
		}

		if((mask & 16) && ((uint8_t)(line[4][x]>>24) < (uint8_t)(color >>24))) {
			color = line[4][x];
			top = 0x10;
		}

		if(color & 0x00010000) {
			// semi-transparent OBJ
			uint32_t back = background;
			uint8_t top2 = 0x20;

			if((mask & 4) && line[2][x] < background) {
				back = line[2][x];
				top2 = 0x04;
			}

			alpha_blend_brightness_switch();
		} else if(mask & 32) {
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = background;
						uint8_t top2 = 0x20;

						if((mask & 4) && (top != 0x04) && line[2][x] < back) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((mask & 16) && (top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

/*
Mode 4 is a 256 colour bitmap graphics mode with 2 swappable pages.
It has a single layer, background layer 2, the same size as the screen.
It doesn't support scrolling, flipping, rotation or tiles.

These routines only render a single line at a time, because of the way the GBA does events.
*/

void mode4RenderLine (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 4: Render Line\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();
	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x400)
	{
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen256(gfxBG2X, gfxBG2Y, changed);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x)
	{
		uint32_t color = backdrop;
		uint8_t top = 0x20;

		if(line[2][x] < backdrop) {
			color = line[2][x];
			top = 0x04;
		}

		if((uint8_t)(line[4][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[4][x];
			top = 0x10;

			if(color & 0x00010000) {
				// semi-transparent OBJ
				uint32_t back = backdrop;
				uint8_t top2 = 0x20;

				if(line[2][x] < backdrop) {
					back = line[2][x];
					top2 = 0x04;
				}

				alpha_blend_brightness_switch();
			}
		}


		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode4RenderLineNoWindow (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 4: Render Line No Window\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();
	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x400)
	{
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen256(gfxBG2X, gfxBG2Y, changed);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x)
	{
		uint32_t color = backdrop;
		uint8_t top = 0x20;

		if(line[2][x] < backdrop) {
			color = line[2][x];
			top = 0x04;
		}

		if((uint8_t)(line[4][x]>>24) < (uint8_t)(color >> 24)) {
			color = line[4][x];
			top = 0x10;
		}

		if(!(color & 0x00010000)) {
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = backdrop;
						uint8_t top2 = 0x20;

						if((top != 0x04) && line[2][x] < backdrop) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		} else {
			// semi-transparent OBJ
			uint32_t back = backdrop;
			uint8_t top2 = 0x20;

			if(line[2][x] < back) {
				back = line[2][x];
				top2 = 0x04;
			}

			alpha_blend_brightness_switch();
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode4RenderLineAll (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 4: Render Line All\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();
	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	bool inWindow0 = false;
	bool inWindow1 = false;

	if(graphics.layerEnable & 0x2000)
	{
		uint8_t v0 = io_registers[REG_WIN0V] >> 8;
		uint8_t v1 = io_registers[REG_WIN0V] & 255;
		inWindow0 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow0 = (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}

	if(graphics.layerEnable & 0x4000)
	{
		uint8_t v0 = io_registers[REG_WIN1V] >> 8;
		uint8_t v1 = io_registers[REG_WIN1V] & 255;
		inWindow1 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow1 = (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}

	if(graphics.layerEnable & 0x400)
	{
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen256(gfxBG2X, gfxBG2Y, changed);
	}

	uint32_t backdrop = (READ16LE(&palette[0]) | 0x30000000);

	uint8_t inWin0Mask = io_registers[REG_WININ] & 0xFF;
	uint8_t inWin1Mask = io_registers[REG_WININ] >> 8;
	uint8_t outMask = io_registers[REG_WINOUT] & 0xFF;

	for(int x = 0; x < 240; ++x) {
		uint32_t color = backdrop;
		uint8_t top = 0x20;
		uint8_t mask = outMask;

		if(!(line[5][x] & 0x80000000))
			mask = io_registers[REG_WINOUT] >> 8;

		int32_t window1_mask = ((inWindow1 & gfxInWin[1][x]) | -(inWindow1 & gfxInWin[1][x])) >> 31;
		int32_t window0_mask = ((inWindow0 & gfxInWin[0][x]) | -(inWindow0 & gfxInWin[0][x])) >> 31;
		mask = (inWin1Mask & window1_mask) | (mask & ~window1_mask);
		mask = (inWin0Mask & window0_mask) | (mask & ~window0_mask);

		if((mask & 4) && (line[2][x] < backdrop))
		{
			color = line[2][x];
			top = 0x04;
		}

		if((mask & 16) && ((uint8_t)(line[4][x]>>24) < (uint8_t)(color >>24)))
		{
			color = line[4][x];
			top = 0x10;
		}

		if(color & 0x00010000) {
			// semi-transparent OBJ
			uint32_t back = backdrop;
			uint8_t top2 = 0x20;

			if((mask & 4) && line[2][x] < back) {
				back = line[2][x];
				top2 = 0x04;
			}

			alpha_blend_brightness_switch();
		} else if(mask & 32) {
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = backdrop;
						uint8_t top2 = 0x20;

						if((mask & 4) && (top != 0x04) && (line[2][x] < backdrop)) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((mask & 16) && (top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

/*
Mode 5 is a low resolution (160x128) 15-bit colour bitmap graphics mode 
with 2 swappable pages!
It has a single layer, background layer 2, lower resolution than the screen.
It doesn't support scrolling, flipping, rotation or tiles.

These routines only render a single line at a time, because of the way the GBA does events.
*/

void mode5RenderLine (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 5: Render Line\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();
	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen16Bit160(gfxBG2X, gfxBG2Y, changed);
	}

	uint32_t background;
	background = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = background;
		uint8_t top = 0x20;

		if(line[2][x] < background) {
			color = line[2][x];
			top = 0x04;
		}

		if((uint8_t)(line[4][x]>>24) < (uint8_t)(color >>24)) {
			color = line[4][x];
			top = 0x10;

			if(color & 0x00010000) {
				// semi-transparent OBJ
				uint32_t back = background;
				uint8_t top2 = 0x20;

				if(line[2][x] < back) {
					back = line[2][x];
					top2 = 0x04;
				}

				alpha_blend_brightness_switch();
			}
		}


		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode5RenderLineNoWindow (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 5: Render Line No Window\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x0400) {
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen16Bit160(gfxBG2X, gfxBG2Y, changed);
	}

	uint32_t background;
	background = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = background;
		uint8_t top = 0x20;

		if(line[2][x] < background) {
			color = line[2][x];
			top = 0x04;
		}

		if((uint8_t)(line[4][x]>>24) < (uint8_t)(color >>24)) {
			color = line[4][x];
			top = 0x10;
		}

		if(!(color & 0x00010000)) {
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = background;
						uint8_t top2 = 0x20;

						if((top != 0x04) && line[2][x] < background) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}

					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		} else {
			// semi-transparent OBJ
			uint32_t back = background;
			uint8_t top2 = 0x20;

			if(line[2][x] < back) {
				back = line[2][x];
				top2 = 0x04;
			}

			alpha_blend_brightness_switch();
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void mode5RenderLineAll (void)
{
#ifdef REPORT_VIDEO_MODES
	fprintf(stderr, "MODE 5: Render Line All\n");
#endif
	INIT_COLOR_DEPTH_LINE_MIX();

	uint16_t *palette = (uint16_t *)graphics.paletteRAM;

	if(graphics.layerEnable & 0x0400)
	{
		int changed = gfxBG2Changed;

#if 0
		if(gfxLastVCOUNT > io_registers[REG_VCOUNT])
			changed = 3;
#endif

		gfxDrawRotScreen16Bit160(gfxBG2X, gfxBG2Y, changed);
	}



	bool inWindow0 = false;
	bool inWindow1 = false;

	if(graphics.layerEnable & 0x2000)
	{
		uint8_t v0 = io_registers[REG_WIN0V] >> 8;
		uint8_t v1 = io_registers[REG_WIN0V] & 255;
		inWindow0 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow0 = (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow0 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow0 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}

	if(graphics.layerEnable & 0x4000)
	{
		uint8_t v0 = io_registers[REG_WIN1V] >> 8;
		uint8_t v1 = io_registers[REG_WIN1V] & 255;
		inWindow1 = ((v0 == v1) && (v0 >= 0xe8));
#ifndef ORIGINAL_BRANCHES
		uint32_t condition = v1 >= v0;
		int32_t condition_mask = ((condition) | -(condition)) >> 31;
		inWindow1 = (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1)) & condition_mask) | (((inWindow1 | (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1)) & ~(condition_mask))));
#else
		if(v1 >= v0)
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 && io_registers[REG_VCOUNT] < v1);
		else
			inWindow1 |= (io_registers[REG_VCOUNT] >= v0 || io_registers[REG_VCOUNT] < v1);
#endif
	}

	uint8_t inWin0Mask = io_registers[REG_WININ] & 0xFF;
	uint8_t inWin1Mask = io_registers[REG_WININ] >> 8;
	uint8_t outMask = io_registers[REG_WINOUT] & 0xFF;

	uint32_t background;
	background = (READ16LE(&palette[0]) | 0x30000000);

	for(int x = 0; x < 240; ++x) {
		uint32_t color = background;
		uint8_t top = 0x20;
		uint8_t mask = outMask;

		if(!(line[5][x] & 0x80000000)) {
			mask = io_registers[REG_WINOUT] >> 8;
		}

		int32_t window1_mask = ((inWindow1 & gfxInWin[1][x]) | -(inWindow1 & gfxInWin[1][x])) >> 31;
		int32_t window0_mask = ((inWindow0 & gfxInWin[0][x]) | -(inWindow0 & gfxInWin[0][x])) >> 31;
		mask = (inWin1Mask & window1_mask) | (mask & ~window1_mask);
		mask = (inWin0Mask & window0_mask) | (mask & ~window0_mask);

		if((mask & 4) && (line[2][x] < background)) {
			color = line[2][x];
			top = 0x04;
		}

		if((mask & 16) && ((uint8_t)(line[4][x]>>24) < (uint8_t)(color >>24))) {
			color = line[4][x];
			top = 0x10;
		}

		if(color & 0x00010000) {
			// semi-transparent OBJ
			uint32_t back = background;
			uint8_t top2 = 0x20;

			if((mask & 4) && line[2][x] < back) {
				back = line[2][x];
				top2 = 0x04;
			}

			alpha_blend_brightness_switch();
		} else if(mask & 32) {
			switch((BLDMOD >> 6) & 3) {
				case 0:
					break;
				case 1:
					if(top & BLDMOD)
					{
						uint32_t back = background;
						uint8_t top2 = 0x20;

						if((mask & 4) && (top != 0x04) && (line[2][x] < background)) {
							back = line[2][x];
							top2 = 0x04;
						}

						if((mask & 16) && (top != 0x10) && (uint8_t)(line[4][x]>>24) < (uint8_t)(back >> 24)) {
							back = line[4][x];
							top2 = 0x10;
						}

						if(top2 & (BLDMOD>>8) && color < 0x80000000)
						{
							GFX_ALPHA_BLEND(color, back, coeff[COLEV & 0x1F], coeff[(COLEV >> 8) & 0x1F]);
						}
					}
					break;
				case 2:
					if(BLDMOD & top)
						color = gfxIncreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
				case 3:
					if(BLDMOD & top)
						color = gfxDecreaseBrightness(color, coeff[COLY & 0x1F]);
					break;
			}
		}

		lineMix[x] = CONVERT_COLOR(color);
	}
	gfxBG2Changed = 0;
	//gfxLastVCOUNT = io_registers[REG_VCOUNT];
}

void (Gigazoid::*renderLine)(void);
bool render_line_all_enabled;

#define CPUUpdateRender() \
  render_line_all_enabled = false; \
  switch(io_registers[REG_DISPCNT] & 7) { \
  case 0: \
    if((!fxOn && !windowOn && !(graphics.layerEnable & 0x8000))) \
      renderLine = &Gigazoid::mode0RenderLine; \
    else if(fxOn && !windowOn && !(graphics.layerEnable & 0x8000)) \
      renderLine = &Gigazoid::mode0RenderLineNoWindow; \
    else { \
      renderLine = &Gigazoid::mode0RenderLineAll; \
      render_line_all_enabled = true; \
    } \
    break; \
  case 1: \
    if((!fxOn && !windowOn && !(graphics.layerEnable & 0x8000))) \
      renderLine = &Gigazoid::mode1RenderLine; \
    else if(fxOn && !windowOn && !(graphics.layerEnable & 0x8000)) \
      renderLine = &Gigazoid::mode1RenderLineNoWindow; \
    else { \
      renderLine = &Gigazoid::mode1RenderLineAll; \
      render_line_all_enabled = true; \
    } \
    break; \
  case 2: \
    if((!fxOn && !windowOn && !(graphics.layerEnable & 0x8000))) \
      renderLine = &Gigazoid::mode2RenderLine; \
    else if(fxOn && !windowOn && !(graphics.layerEnable & 0x8000)) \
      renderLine = &Gigazoid::mode2RenderLineNoWindow; \
    else { \
      renderLine = &Gigazoid::mode2RenderLineAll; \
      render_line_all_enabled = true; \
    } \
    break; \
  case 3: \
    if((!fxOn && !windowOn && !(graphics.layerEnable & 0x8000))) \
      renderLine = &Gigazoid::mode3RenderLine; \
    else if(fxOn && !windowOn && !(graphics.layerEnable & 0x8000)) \
      renderLine = &Gigazoid::mode3RenderLineNoWindow; \
    else { \
      renderLine = &Gigazoid::mode3RenderLineAll; \
      render_line_all_enabled = true; \
    } \
    break; \
  case 4: \
    if((!fxOn && !windowOn && !(graphics.layerEnable & 0x8000))) \
      renderLine = &Gigazoid::mode4RenderLine; \
    else if(fxOn && !windowOn && !(graphics.layerEnable & 0x8000)) \
      renderLine = &Gigazoid::mode4RenderLineNoWindow; \
    else { \
      renderLine = &Gigazoid::mode4RenderLineAll; \
      render_line_all_enabled = true; \
    } \
    break; \
  case 5: \
    if((!fxOn && !windowOn && !(graphics.layerEnable & 0x8000))) \
      renderLine = &Gigazoid::mode5RenderLine; \
    else if(fxOn && !windowOn && !(graphics.layerEnable & 0x8000)) \
      renderLine = &Gigazoid::mode5RenderLineNoWindow; \
    else { \
      renderLine = &Gigazoid::mode5RenderLineAll; \
      render_line_all_enabled = true; \
    } \
  }

#define CPUSwap(a, b) \
a ^= b; \
b ^= a; \
a ^= b;

void CPUSwitchMode(int mode, bool saveState, bool breakLoop)
{
	CPU_UPDATE_CPSR();

	switch(armMode) {
		case 0x10:
		case 0x1F:
			bus.reg[R13_USR].I = bus.reg[13].I;
			bus.reg[R14_USR].I = bus.reg[14].I;
			bus.reg[17].I = bus.reg[16].I;
			break;
		case 0x11:
			CPUSwap(bus.reg[R8_FIQ].I, bus.reg[8].I);
			CPUSwap(bus.reg[R9_FIQ].I, bus.reg[9].I);
			CPUSwap(bus.reg[R10_FIQ].I, bus.reg[10].I);
			CPUSwap(bus.reg[R11_FIQ].I, bus.reg[11].I);
			CPUSwap(bus.reg[R12_FIQ].I, bus.reg[12].I);
			bus.reg[R13_FIQ].I = bus.reg[13].I;
			bus.reg[R14_FIQ].I = bus.reg[14].I;
			bus.reg[SPSR_FIQ].I = bus.reg[17].I;
			break;
		case 0x12:
			bus.reg[R13_IRQ].I  = bus.reg[13].I;
			bus.reg[R14_IRQ].I  = bus.reg[14].I;
			bus.reg[SPSR_IRQ].I =  bus.reg[17].I;
			break;
		case 0x13:
			bus.reg[R13_SVC].I  = bus.reg[13].I;
			bus.reg[R14_SVC].I  = bus.reg[14].I;
			bus.reg[SPSR_SVC].I =  bus.reg[17].I;
			break;
		case 0x17:
			bus.reg[R13_ABT].I  = bus.reg[13].I;
			bus.reg[R14_ABT].I  = bus.reg[14].I;
			bus.reg[SPSR_ABT].I =  bus.reg[17].I;
			break;
		case 0x1b:
			bus.reg[R13_UND].I  = bus.reg[13].I;
			bus.reg[R14_UND].I  = bus.reg[14].I;
			bus.reg[SPSR_UND].I =  bus.reg[17].I;
			break;
	}

	uint32_t CPSR = bus.reg[16].I;
	uint32_t SPSR = bus.reg[17].I;

	switch(mode) {
		case 0x10:
		case 0x1F:
			bus.reg[13].I = bus.reg[R13_USR].I;
			bus.reg[14].I = bus.reg[R14_USR].I;
			bus.reg[16].I = SPSR;
			break;
		case 0x11:
			CPUSwap(bus.reg[8].I, bus.reg[R8_FIQ].I);
			CPUSwap(bus.reg[9].I, bus.reg[R9_FIQ].I);
			CPUSwap(bus.reg[10].I, bus.reg[R10_FIQ].I);
			CPUSwap(bus.reg[11].I, bus.reg[R11_FIQ].I);
			CPUSwap(bus.reg[12].I, bus.reg[R12_FIQ].I);
			bus.reg[13].I = bus.reg[R13_FIQ].I;
			bus.reg[14].I = bus.reg[R14_FIQ].I;
			if(saveState)
				bus.reg[17].I = CPSR; else
				bus.reg[17].I = bus.reg[SPSR_FIQ].I;
			break;
		case 0x12:
			bus.reg[13].I = bus.reg[R13_IRQ].I;
			bus.reg[14].I = bus.reg[R14_IRQ].I;
			bus.reg[16].I = SPSR;
			if(saveState)
				bus.reg[17].I = CPSR;
			else
				bus.reg[17].I = bus.reg[SPSR_IRQ].I;
			break;
		case 0x13:
			bus.reg[13].I = bus.reg[R13_SVC].I;
			bus.reg[14].I = bus.reg[R14_SVC].I;
			bus.reg[16].I = SPSR;
			if(saveState)
				bus.reg[17].I = CPSR;
			else
				bus.reg[17].I = bus.reg[SPSR_SVC].I;
			break;
		case 0x17:
			bus.reg[13].I = bus.reg[R13_ABT].I;
			bus.reg[14].I = bus.reg[R14_ABT].I;
			bus.reg[16].I = SPSR;
			if(saveState)
				bus.reg[17].I = CPSR;
			else
				bus.reg[17].I = bus.reg[SPSR_ABT].I;
			break;
		case 0x1b:
			bus.reg[13].I = bus.reg[R13_UND].I;
			bus.reg[14].I = bus.reg[R14_UND].I;
			bus.reg[16].I = SPSR;
			if(saveState)
				bus.reg[17].I = CPSR;
			else
				bus.reg[17].I = bus.reg[SPSR_UND].I;
			break;
		default:
			break;
	}
	armMode = mode;
	CPUUpdateFlags(breakLoop);
	CPU_UPDATE_CPSR();
}



void doDMA(uint32_t &s, uint32_t &d, uint32_t si, uint32_t di, uint32_t c, int transfer32)
{
	int sm = s >> 24;
	int dm = d >> 24;
	int sw = 0;
	int dw = 0;
	int sc = c;

	cpuDmaCount = c;
	// This is done to get the correct waitstates.
	int32_t sm_gt_15_mask = ((sm>15) | -(sm>15)) >> 31;
	int32_t dm_gt_15_mask = ((dm>15) | -(dm>15)) >> 31;
	sm = ((((15) & sm_gt_15_mask) | ((((sm) & ~(sm_gt_15_mask))))));
	dm = ((((15) & dm_gt_15_mask) | ((((dm) & ~(dm_gt_15_mask))))));

	//if ((sm>=0x05) && (sm<=0x07) || (dm>=0x05) && (dm <=0x07))
	//    blank = (((io_registers[REG_DISPSTAT] | ((io_registers[REG_DISPSTAT] >> 1)&1))==1) ?  true : false);

	if(transfer32)
	{
		s &= 0xFFFFFFFC;
		if(s < 0x02000000 && (bus.reg[15].I >> 24))
		{
			do
			{
				CPUWriteMemory(d, 0);
				d += di;
				c--;
			}while(c != 0);
		}
		else
		{
			do {
				CPUWriteMemory(d, CPUReadMemory(s));
				d += di;
				s += si;
				c--;
			}while(c != 0);
		}
	}
	else
	{
		s &= 0xFFFFFFFE;
		si = (int)si >> 1;
		di = (int)di >> 1;
		if(s < 0x02000000 && (bus.reg[15].I >> 24))
		{
			do {
				CPUWriteHalfWord(d, 0);
				d += di;
				c--;
			}while(c != 0);
		}
		else
		{
			do{
				CPUWriteHalfWord(d, CPUReadHalfWord(s));
				d += di;
				s += si;
				c--;
			}while(c != 0);
		}
	}

	cpuDmaCount = 0;

	if(transfer32)
	{
		sw = 1+memoryWaitSeq32[sm & 15];
		dw = 1+memoryWaitSeq32[dm & 15];
		cpuDmaTicksToUpdate += (sw+dw)*(sc-1) + 6 + memoryWait32[sm & 15] + memoryWaitSeq32[dm & 15];
	}
	else
	{
		sw = 1+memoryWaitSeq[sm & 15];
		dw = 1+memoryWaitSeq[dm & 15];
		cpuDmaTicksToUpdate += (sw+dw)*(sc-1) + 6 + memoryWait[sm & 15] + memoryWaitSeq[dm & 15];
	}
}


void CPUCheckDMA(int reason, int dmamask)
{
	uint32_t arrayval[] = {4, (uint32_t)-4, 0, 4};
	// DMA 0
	if((DM0CNT_H & 0x8000) && (dmamask & 1))
	{
		if(((DM0CNT_H >> 12) & 3) == reason)
		{
			uint32_t sourceIncrement, destIncrement;
			uint32_t condition1 = ((DM0CNT_H >> 7) & 3);
			uint32_t condition2 = ((DM0CNT_H >> 5) & 3);
			sourceIncrement = arrayval[condition1];
			destIncrement = arrayval[condition2];
			doDMA(dma0Source, dma0Dest, sourceIncrement, destIncrement,
					DM0CNT_L ? DM0CNT_L : 0x4000,
					DM0CNT_H & 0x0400);

			if(DM0CNT_H & 0x4000)
			{
				io_registers[REG_IF] |= 0x0100;
				UPDATE_REG(0x202, io_registers[REG_IF]);
				cpuNextEvent = cpuTotalTicks;
			}

			if(((DM0CNT_H >> 5) & 3) == 3) {
				dma0Dest = DM0DAD_L | (DM0DAD_H << 16);
			}

			if(!(DM0CNT_H & 0x0200) || (reason == 0)) {
				DM0CNT_H &= 0x7FFF;
				UPDATE_REG(0xBA, DM0CNT_H);
			}
		}
	}

	// DMA 1
	if((DM1CNT_H & 0x8000) && (dmamask & 2)) {
		if(((DM1CNT_H >> 12) & 3) == reason) {
			uint32_t sourceIncrement, destIncrement;
			uint32_t condition1 = ((DM1CNT_H >> 7) & 3);
			uint32_t condition2 = ((DM1CNT_H >> 5) & 3);
			sourceIncrement = arrayval[condition1];
			destIncrement = arrayval[condition2];
			uint32_t di_value, c_value, transfer_value;
			if(reason == 3)
			{
				di_value = 0;
				c_value = 4;
				transfer_value = 0x0400;
			}
			else
			{
				di_value = destIncrement;
				c_value = DM1CNT_L ? DM1CNT_L : 0x4000;
				transfer_value = DM1CNT_H & 0x0400;
			}
			doDMA(dma1Source, dma1Dest, sourceIncrement, di_value, c_value, transfer_value);

			if(DM1CNT_H & 0x4000) {
				io_registers[REG_IF] |= 0x0200;
				UPDATE_REG(0x202, io_registers[REG_IF]);
				cpuNextEvent = cpuTotalTicks;
			}

			if(((DM1CNT_H >> 5) & 3) == 3) {
				dma1Dest = DM1DAD_L | (DM1DAD_H << 16);
			}

			if(!(DM1CNT_H & 0x0200) || (reason == 0)) {
				DM1CNT_H &= 0x7FFF;
				UPDATE_REG(0xC6, DM1CNT_H);
			}
		}
	}

	// DMA 2
	if((DM2CNT_H & 0x8000) && (dmamask & 4)) {
		if(((DM2CNT_H >> 12) & 3) == reason) {
			uint32_t sourceIncrement, destIncrement;
			uint32_t condition1 = ((DM2CNT_H >> 7) & 3);
			uint32_t condition2 = ((DM2CNT_H >> 5) & 3);
			sourceIncrement = arrayval[condition1];
			destIncrement = arrayval[condition2];
			uint32_t di_value, c_value, transfer_value;
			if(reason == 3)
			{
				di_value = 0;
				c_value = 4;
				transfer_value = 0x0400;
			}
			else
			{
				di_value = destIncrement;
				c_value = DM2CNT_L ? DM2CNT_L : 0x4000;
				transfer_value = DM2CNT_H & 0x0400;
			}
			doDMA(dma2Source, dma2Dest, sourceIncrement, di_value, c_value, transfer_value);

			if(DM2CNT_H & 0x4000) {
				io_registers[REG_IF] |= 0x0400;
				UPDATE_REG(0x202, io_registers[REG_IF]);
				cpuNextEvent = cpuTotalTicks;
			}

			if(((DM2CNT_H >> 5) & 3) == 3) {
				dma2Dest = DM2DAD_L | (DM2DAD_H << 16);
			}

			if(!(DM2CNT_H & 0x0200) || (reason == 0)) {
				DM2CNT_H &= 0x7FFF;
				UPDATE_REG(0xD2, DM2CNT_H);
			}
		}
	}

	// DMA 3
	if((DM3CNT_H & 0x8000) && (dmamask & 8))
	{
		if(((DM3CNT_H >> 12) & 3) == reason)
		{
			uint32_t sourceIncrement, destIncrement;
			uint32_t condition1 = ((DM3CNT_H >> 7) & 3);
			uint32_t condition2 = ((DM3CNT_H >> 5) & 3);
			sourceIncrement = arrayval[condition1];
			destIncrement = arrayval[condition2];
			doDMA(dma3Source, dma3Dest, sourceIncrement, destIncrement,
					DM3CNT_L ? DM3CNT_L : 0x10000,
					DM3CNT_H & 0x0400);
			if(DM3CNT_H & 0x4000) {
				io_registers[REG_IF] |= 0x0800;
				UPDATE_REG(0x202, io_registers[REG_IF]);
				cpuNextEvent = cpuTotalTicks;
			}

			if(((DM3CNT_H >> 5) & 3) == 3) {
				dma3Dest = DM3DAD_L | (DM3DAD_H << 16);
			}

			if(!(DM3CNT_H & 0x0200) || (reason == 0)) {
				DM3CNT_H &= 0x7FFF;
				UPDATE_REG(0xDE, DM3CNT_H);
			}
		}
	}
}

uint16_t *address_lut[0x300];

void CPUUpdateRegister(uint32_t address, uint16_t value)
{
	switch(address)
	{
		case 0x00:
			{
				if((value & 7) > 5) // display modes above 0-5 are prohibited
					io_registers[REG_DISPCNT] = (value & 7);

				bool change = (0 != ((io_registers[REG_DISPCNT] ^ value) & 0x80));
				bool changeBG = (0 != ((io_registers[REG_DISPCNT] ^ value) & 0x0F00));
				uint16_t changeBGon = ((~io_registers[REG_DISPCNT]) & value) & 0x0F00; // these layers are being activated

				io_registers[REG_DISPCNT] = (value & 0xFFF7); // bit 3 can only be accessed by the BIOS to enable GBC mode
				UPDATE_REG(0x00, io_registers[REG_DISPCNT]);

				graphics.layerEnable = value;

				if(changeBGon)
				{
					graphics.layerEnableDelay = 4;
					graphics.layerEnable &= ~changeBGon;
				}

				windowOn = (graphics.layerEnable & 0x6000) ? true : false;
				if(change && !((value & 0x80)))
				{
					if(!(io_registers[REG_DISPSTAT] & 1))
					{
						graphics.lcdTicks = 1008;
						io_registers[REG_DISPSTAT] &= 0xFFFC;
						UPDATE_REG(0x04, io_registers[REG_DISPSTAT]);
						CPUCompareVCOUNT();
					}
				}
				CPUUpdateRender();
				// we only care about changes in BG0-BG3
				if(changeBG)
				{
					if(!(graphics.layerEnable & 0x0100))
						memset(line[0], -1, 240 * sizeof(u32));
					if(!(graphics.layerEnable & 0x0200))
						memset(line[1], -1, 240 * sizeof(u32));
					if(!(graphics.layerEnable & 0x0400))
						memset(line[2], -1, 240 * sizeof(u32));
					if(!(graphics.layerEnable & 0x0800))
						memset(line[3], -1, 240 * sizeof(u32));
				}
				break;
			}
		case 0x04:
			io_registers[REG_DISPSTAT] = (value & 0xFF38) | (io_registers[REG_DISPSTAT] & 7);
			UPDATE_REG(0x04, io_registers[REG_DISPSTAT]);
			break;
		case 0x06:
			// not writable
			break;
		case 0x08: /* BG0CNT */
		case 0x0A: /* BG1CNT */
			*address_lut[address] = (value & 0xDFCF);
			UPDATE_REG(address, *address_lut[address]);
			break;
		case 0x0C: /* BG2CNT */
		case 0x0E: /* BG3CNT */
			*address_lut[address] = (value & 0xFFCF);
			UPDATE_REG(address, *address_lut[address]);
			break;
		case 0x10: /* BG0HOFS */
		case 0x12: /* BG0VOFS */
		case 0x14: /* BG1HOFS */
		case 0x16: /* BG1VOFS */
		case 0x18: /* BG2HOFS */
		case 0x1A: /* BG2VOFS */
		case 0x1C: /* BG3HOFS */
		case 0x1E: /* BG3VOFS */
			*address_lut[address] = value & 511;
			UPDATE_REG(address, *address_lut[address]);
			break;
		case 0x20: /* BG2PA */
		case 0x22: /* BG2PB */
		case 0x24: /* BG2PC */
		case 0x26: /* BG2PD */
			*address_lut[address] = value;
			UPDATE_REG(address, *address_lut[address]);
			break;
		case 0x28:
			BG2X_L = value;
			UPDATE_REG(0x28, BG2X_L);
			gfxBG2Changed |= 1;
			break;
		case 0x2A:
			BG2X_H = (value & 0xFFF);
			UPDATE_REG(0x2A, BG2X_H);
			gfxBG2Changed |= 1;
			break;
		case 0x2C:
			BG2Y_L = value;
			UPDATE_REG(0x2C, BG2Y_L);
			gfxBG2Changed |= 2;
			break;
		case 0x2E:
			BG2Y_H = value & 0xFFF;
			UPDATE_REG(0x2E, BG2Y_H);
			gfxBG2Changed |= 2;
			break;
		case 0x30: /* BG3PA */
		case 0x32: /* BG3PB */
		case 0x34: /* BG3PC */
		case 0x36: /* BG3PD */
			*address_lut[address] = value;
			UPDATE_REG(address, *address_lut[address]);
			break;
		case 0x38:
			BG3X_L = value;
			UPDATE_REG(0x38, BG3X_L);
			gfxBG3Changed |= 1;
			break;
		case 0x3A:
			BG3X_H = value & 0xFFF;
			UPDATE_REG(0x3A, BG3X_H);
			gfxBG3Changed |= 1;
			break;
		case 0x3C:
			BG3Y_L = value;
			UPDATE_REG(0x3C, BG3Y_L);
			gfxBG3Changed |= 2;
			break;
		case 0x3E:
			BG3Y_H = value & 0xFFF;
			UPDATE_REG(0x3E, BG3Y_H);
			gfxBG3Changed |= 2;
			break;
		case 0x40:
			io_registers[REG_WIN0H] = value;
			UPDATE_REG(0x40, io_registers[REG_WIN0H]);
			CPUUpdateWindow0();
			break;
		case 0x42:
			io_registers[REG_WIN1H] = value;
			UPDATE_REG(0x42, io_registers[REG_WIN1H]);
			CPUUpdateWindow1();
			break;
		case 0x44:
		case 0x46:
			*address_lut[address] = value;
			UPDATE_REG(address, *address_lut[address]);
			break;
		case 0x48: /* WININ */
		case 0x4A: /* WINOUT */
			*address_lut[address] = value & 0x3F3F;
			UPDATE_REG(address, *address_lut[address]);
			break;
		case 0x4C:
			MOSAIC = value;
			UPDATE_REG(0x4C, MOSAIC);
			break;
		case 0x50:
			BLDMOD = value & 0x3FFF;
			UPDATE_REG(0x50, BLDMOD);
			fxOn = ((BLDMOD>>6)&3) != 0;
			CPUUpdateRender();
			break;
		case 0x52:
			COLEV = value & 0x1F1F;
			UPDATE_REG(0x52, COLEV);
			break;
		case 0x54:
			COLY = value & 0x1F;
			UPDATE_REG(0x54, COLY);
			break;
		case 0x60:
		case 0x62:
		case 0x64:
		case 0x68:
		case 0x6c:
		case 0x70:
		case 0x72:
		case 0x74:
		case 0x78:
		case 0x7c:
		case 0x80:
		case 0x84:
			{
				int gb_addr[2] = {address & 0xFF, (address & 0xFF) + 1};
				uint32_t address_array[2] = {address & 0xFF, (address&0xFF)+1};
				uint8_t data_array[2] = {(uint8_t)(value & 0xFF), (uint8_t)(value>>8)};
				gb_addr[0] = table[gb_addr[0] - 0x60];
				gb_addr[1] = table[gb_addr[1] - 0x60];
				soundEvent_u8_parallel(gb_addr, address_array, data_array);
				break;
			}
		case 0x82:
		case 0x88:
		case 0xa0:
		case 0xa2:
		case 0xa4:
		case 0xa6:
		case 0x90:
		case 0x92:
		case 0x94:
		case 0x96:
		case 0x98:
		case 0x9a:
		case 0x9c:
		case 0x9e:
			soundEvent_u16(address&0xFF, value);
			break;
		case 0xB0:
			DM0SAD_L = value;
			UPDATE_REG(0xB0, DM0SAD_L);
			break;
		case 0xB2:
			DM0SAD_H = value & 0x07FF;
			UPDATE_REG(0xB2, DM0SAD_H);
			break;
		case 0xB4:
			DM0DAD_L = value;
			UPDATE_REG(0xB4, DM0DAD_L);
			break;
		case 0xB6:
			DM0DAD_H = value & 0x07FF;
			UPDATE_REG(0xB6, DM0DAD_H);
			break;
		case 0xB8:
			DM0CNT_L = value & 0x3FFF;
			UPDATE_REG(0xB8, 0);
			break;
		case 0xBA:
			{
				bool start = ((DM0CNT_H ^ value) & 0x8000) ? true : false;
				value &= 0xF7E0;

				DM0CNT_H = value;
				UPDATE_REG(0xBA, DM0CNT_H);

				if(start && (value & 0x8000))
				{
					dma0Source = DM0SAD_L | (DM0SAD_H << 16);
					dma0Dest = DM0DAD_L | (DM0DAD_H << 16);
					CPUCheckDMA(0, 1);
				}
			}
			break;
		case 0xBC:
			DM1SAD_L = value;
			UPDATE_REG(0xBC, DM1SAD_L);
			break;
		case 0xBE:
			DM1SAD_H = value & 0x0FFF;
			UPDATE_REG(0xBE, DM1SAD_H);
			break;
		case 0xC0:
			DM1DAD_L = value;
			UPDATE_REG(0xC0, DM1DAD_L);
			break;
		case 0xC2:
			DM1DAD_H = value & 0x07FF;
			UPDATE_REG(0xC2, DM1DAD_H);
			break;
		case 0xC4:
			DM1CNT_L = value & 0x3FFF;
			UPDATE_REG(0xC4, 0);
			break;
		case 0xC6:
			{
				bool start = ((DM1CNT_H ^ value) & 0x8000) ? true : false;
				value &= 0xF7E0;

				DM1CNT_H = value;
				UPDATE_REG(0xC6, DM1CNT_H);

				if(start && (value & 0x8000))
				{
					dma1Source = DM1SAD_L | (DM1SAD_H << 16);
					dma1Dest = DM1DAD_L | (DM1DAD_H << 16);
					CPUCheckDMA(0, 2);
				}
			}
			break;
		case 0xC8:
			DM2SAD_L = value;
			UPDATE_REG(0xC8, DM2SAD_L);
			break;
		case 0xCA:
			DM2SAD_H = value & 0x0FFF;
			UPDATE_REG(0xCA, DM2SAD_H);
			break;
		case 0xCC:
			DM2DAD_L = value;
			UPDATE_REG(0xCC, DM2DAD_L);
			break;
		case 0xCE:
			DM2DAD_H = value & 0x07FF;
			UPDATE_REG(0xCE, DM2DAD_H);
			break;
		case 0xD0:
			DM2CNT_L = value & 0x3FFF;
			UPDATE_REG(0xD0, 0);
			break;
		case 0xD2:
			{
				bool start = ((DM2CNT_H ^ value) & 0x8000) ? true : false;

				value &= 0xF7E0;

				DM2CNT_H = value;
				UPDATE_REG(0xD2, DM2CNT_H);

				if(start && (value & 0x8000)) {
					dma2Source = DM2SAD_L | (DM2SAD_H << 16);
					dma2Dest = DM2DAD_L | (DM2DAD_H << 16);

					CPUCheckDMA(0, 4);
				}
			}
			break;
		case 0xD4:
			DM3SAD_L = value;
			UPDATE_REG(0xD4, DM3SAD_L);
			break;
		case 0xD6:
			DM3SAD_H = value & 0x0FFF;
			UPDATE_REG(0xD6, DM3SAD_H);
			break;
		case 0xD8:
			DM3DAD_L = value;
			UPDATE_REG(0xD8, DM3DAD_L);
			break;
		case 0xDA:
			DM3DAD_H = value & 0x0FFF;
			UPDATE_REG(0xDA, DM3DAD_H);
			break;
		case 0xDC:
			DM3CNT_L = value;
			UPDATE_REG(0xDC, 0);
			break;
		case 0xDE:
			{
				bool start = ((DM3CNT_H ^ value) & 0x8000) ? true : false;

				value &= 0xFFE0;

				DM3CNT_H = value;
				UPDATE_REG(0xDE, DM3CNT_H);

				if(start && (value & 0x8000)) {
					dma3Source = DM3SAD_L | (DM3SAD_H << 16);
					dma3Dest = DM3DAD_L | (DM3DAD_H << 16);
					CPUCheckDMA(0,8);
				}
			}
			break;
		case 0x100:
			timer0Reload = value;
			break;
		case 0x102:
			timer0Value = value;
			timerOnOffDelay|=1;
			cpuNextEvent = cpuTotalTicks;
			break;
		case 0x104:
			timer1Reload = value;
			break;
		case 0x106:
			timer1Value = value;
			timerOnOffDelay|=2;
			cpuNextEvent = cpuTotalTicks;
			break;
		case 0x108:
			timer2Reload = value;
			break;
		case 0x10A:
			timer2Value = value;
			timerOnOffDelay|=4;
			cpuNextEvent = cpuTotalTicks;
			break;
		case 0x10C:
			timer3Reload = value;
			break;
		case 0x10E:
			timer3Value = value;
			timerOnOffDelay|=8;
			cpuNextEvent = cpuTotalTicks;
			break;
		case 0x130:
			io_registers[REG_P1] |= (value & 0x3FF);
			UPDATE_REG(0x130, io_registers[REG_P1]);
			break;
		case 0x132:
			UPDATE_REG(0x132, value & 0xC3FF);
			break;


		case 0x200:
			io_registers[REG_IE] = value & 0x3FFF;
			UPDATE_REG(0x200, io_registers[REG_IE]);
			if ((io_registers[REG_IME] & 1) && (io_registers[REG_IF] & io_registers[REG_IE]) && armIrqEnable)
				cpuNextEvent = cpuTotalTicks;
			break;
		case 0x202:
			io_registers[REG_IF] ^= (value & io_registers[REG_IF]);
			UPDATE_REG(0x202, io_registers[REG_IF]);
			break;
		case 0x204:
			{
				memoryWait[0x0e] = memoryWaitSeq[0x0e] = gamepakRamWaitState[value & 3];

				memoryWait[0x08] = memoryWait[0x09] = 3;
				memoryWaitSeq[0x08] = memoryWaitSeq[0x09] = 1;

				memoryWait[0x0a] = memoryWait[0x0b] = 3;
				memoryWaitSeq[0x0a] = memoryWaitSeq[0x0b] = 1;

				memoryWait[0x0c] = memoryWait[0x0d] = 3;
				memoryWaitSeq[0x0c] = memoryWaitSeq[0x0d] = 1;

				memoryWait32[8] = memoryWait[8] + memoryWaitSeq[8] + 1;
				memoryWaitSeq32[8] = memoryWaitSeq[8]*2 + 1;

				memoryWait32[9] = memoryWait[9] + memoryWaitSeq[9] + 1;
				memoryWaitSeq32[9] = memoryWaitSeq[9]*2 + 1;

				memoryWait32[10] = memoryWait[10] + memoryWaitSeq[10] + 1;
				memoryWaitSeq32[10] = memoryWaitSeq[10]*2 + 1;

				memoryWait32[11] = memoryWait[11] + memoryWaitSeq[11] + 1;
				memoryWaitSeq32[11] = memoryWaitSeq[11]*2 + 1;

				memoryWait32[12] = memoryWait[12] + memoryWaitSeq[12] + 1;
				memoryWaitSeq32[12] = memoryWaitSeq[12]*2 + 1;

				memoryWait32[13] = memoryWait[13] + memoryWaitSeq[13] + 1;
				memoryWaitSeq32[13] = memoryWaitSeq[13]*2 + 1;

				memoryWait32[14] = memoryWait[14] + memoryWaitSeq[14] + 1;
				memoryWaitSeq32[14] = memoryWaitSeq[14]*2 + 1;

				if((value & 0x4000) == 0x4000)
					bus.busPrefetchEnable = true;
				else
					bus.busPrefetchEnable = false;

				bus.busPrefetch = false;
				bus.busPrefetchCount = 0;

				UPDATE_REG(0x204, value & 0x7FFF);

			}
			break;
		case 0x208:
			io_registers[REG_IME] = value & 1;
			UPDATE_REG(0x208, io_registers[REG_IME]);
			if ((io_registers[REG_IME] & 1) && (io_registers[REG_IF] & io_registers[REG_IE]) && armIrqEnable)
				cpuNextEvent = cpuTotalTicks;
			break;
		case 0x300:
			if(value != 0)
				value &= 0xFFFE;
			UPDATE_REG(0x300, value);
			break;
		default:
			UPDATE_REG(address&0x3FE, value);
			break;
	}
}


void CPUInit(const u8 *biosfile, const u32 biosfilelen)
{
	eepromInUse = false;
	switch(cpuSaveType)
	{
		case 0: // automatic
		default:
			cpuEEPROMEnabled = true;
			cpuEEPROMSensorEnabled = false;
			cpuSaveGameFunc = &Gigazoid::flashSaveDecide; // EEPROM usage is automatically detected
			break;
		case 1: // EEPROM
			cpuEEPROMEnabled = true;
			cpuEEPROMSensorEnabled = false;
			cpuSaveGameFunc = &Gigazoid::dummyWrite; // EEPROM usage is automatically detected
			break;
		case 2: // SRAM
			cpuEEPROMEnabled = false;
			cpuEEPROMSensorEnabled = false;
			cpuSaveGameFunc = &Gigazoid::sramWrite;
			break;
		case 3: // FLASH
			cpuEEPROMEnabled = false;
			cpuEEPROMSensorEnabled = false;
			cpuSaveGameFunc = &Gigazoid::flashWrite;
			break;
		case 4: // EEPROM+Sensor
			cpuEEPROMEnabled = true;
			cpuEEPROMSensorEnabled = true;
			cpuSaveGameFunc = &Gigazoid::dummyWrite; // EEPROM usage is automatically detected
			break;
		case 5: // NONE
			cpuEEPROMEnabled = false;
			cpuEEPROMSensorEnabled = false;
			cpuSaveGameFunc = &Gigazoid::dummyWrite;
			break;
	}

	memcpy(bios, biosfile, 16384);

	int i = 0;

	biosProtected[0] = 0x00;
	biosProtected[1] = 0xf0;
	biosProtected[2] = 0x29;
	biosProtected[3] = 0xe1;

	for(i = 0; i < 256; i++)
	{
		int count = 0;
		int j;
		for(j = 0; j < 8; j++)
			if(i & (1 << j))
				count++;
		cpuBitsSet[i] = count;

		for(j = 0; j < 8; j++)
			if(i & (1 << j))
				break;
	}

	for(i = 0; i < 0x400; i++)
		ioReadable[i] = true;
	for(i = 0x10; i < 0x48; i++)
		ioReadable[i] = false;
	for(i = 0x4c; i < 0x50; i++)
		ioReadable[i] = false;
	for(i = 0x54; i < 0x60; i++)
		ioReadable[i] = false;
	for(i = 0x8c; i < 0x90; i++)
		ioReadable[i] = false;
	for(i = 0xa0; i < 0xb8; i++)
		ioReadable[i] = false;
	for(i = 0xbc; i < 0xc4; i++)
		ioReadable[i] = false;
	for(i = 0xc8; i < 0xd0; i++)
		ioReadable[i] = false;
	for(i = 0xd4; i < 0xdc; i++)
		ioReadable[i] = false;
	for(i = 0xe0; i < 0x100; i++)
		ioReadable[i] = false;
	for(i = 0x110; i < 0x120; i++)
		ioReadable[i] = false;
	for(i = 0x12c; i < 0x130; i++)
		ioReadable[i] = false;
	for(i = 0x138; i < 0x140; i++)
		ioReadable[i] = false;
	for(i = 0x144; i < 0x150; i++)
		ioReadable[i] = false;
	for(i = 0x15c; i < 0x200; i++)
		ioReadable[i] = false;
	for(i = 0x20c; i < 0x300; i++)
		ioReadable[i] = false;
	for(i = 0x304; i < 0x400; i++)
		ioReadable[i] = false;

	// what is this?
	if(romSize < 0x1fe2000) {
		*((uint16_t *)&rom[0x1fe209c]) = 0xdffa; // SWI 0xFA
		*((uint16_t *)&rom[0x1fe209e]) = 0x4770; // BX LR
	}

	graphics.layerEnable = 0xff00;
	graphics.layerEnableDelay = 1;
	io_registers[REG_DISPCNT] = 0x0080;
	io_registers[REG_DISPSTAT] = 0;
	graphics.lcdTicks = (useBios && !skipBios) ? 1008 : 208;

	/* address lut for use in CPUUpdateRegister */
	address_lut[0x08] = &io_registers[REG_BG0CNT];
	address_lut[0x0A] = &io_registers[REG_BG1CNT];
	address_lut[0x0C] = &io_registers[REG_BG2CNT];
	address_lut[0x0E] = &io_registers[REG_BG3CNT];
	address_lut[0x10] = &io_registers[REG_BG0HOFS];
	address_lut[0x12] = &io_registers[REG_BG0VOFS];
	address_lut[0x14] = &io_registers[REG_BG1HOFS];
	address_lut[0x16] = &io_registers[REG_BG1VOFS];
	address_lut[0x18] = &io_registers[REG_BG2HOFS];
	address_lut[0x1A] = &io_registers[REG_BG2VOFS];
	address_lut[0x1C] = &io_registers[REG_BG3HOFS];
	address_lut[0x1E] = &io_registers[REG_BG3VOFS];
	address_lut[0x20] = &io_registers[REG_BG2PA];
	address_lut[0x22] = &io_registers[REG_BG2PB];
	address_lut[0x24] = &io_registers[REG_BG2PC];
	address_lut[0x26] = &io_registers[REG_BG2PD];
	address_lut[0x48] = &io_registers[REG_WININ];
	address_lut[0x4A] = &io_registers[REG_WINOUT];
	address_lut[0x30] = &io_registers[REG_BG3PA];
	address_lut[0x32] = &io_registers[REG_BG3PB];
	address_lut[0x34] = &io_registers[REG_BG3PC];
	address_lut[0x36] = &io_registers[REG_BG3PD];
	address_lut[0x40] = &io_registers[REG_WIN0H];
	address_lut[0x42] = &io_registers[REG_WIN1H];
	address_lut[0x44] = &io_registers[REG_WIN0V];
	address_lut[0x46] = &io_registers[REG_WIN1V];
}

void CPUReset (void)
{
	rtcReset();
	memset(&bus.reg[0], 0, sizeof(bus.reg));	// clean registers
	memset(oam, 0, 0x400);				// clean OAM
	memset(graphics.paletteRAM, 0, 0x400);		// clean palette
	memset(pix, 0, 4 * 160 * 240);			// clean picture
	memset(vram, 0, 0x20000);			// clean vram
	memset(ioMem, 0, 0x400);			// clean io memory

	io_registers[REG_DISPCNT]  = 0x0080;
	io_registers[REG_DISPSTAT] = 0x0000;
	io_registers[REG_VCOUNT]   = (useBios && !skipBios) ? 0 :0x007E;
	io_registers[REG_BG0CNT]   = 0x0000;
	io_registers[REG_BG1CNT]   = 0x0000;
	io_registers[REG_BG2CNT]   = 0x0000;
	io_registers[REG_BG3CNT]   = 0x0000;
	io_registers[REG_BG0HOFS]  = 0x0000;
	io_registers[REG_BG0VOFS]  = 0x0000;
	io_registers[REG_BG1HOFS]  = 0x0000;
	io_registers[REG_BG1VOFS]  = 0x0000;
	io_registers[REG_BG2HOFS]  = 0x0000;
	io_registers[REG_BG2VOFS]  = 0x0000;
	io_registers[REG_BG3HOFS]  = 0x0000;
	io_registers[REG_BG3VOFS]  = 0x0000;
	io_registers[REG_BG2PA]    = 0x0100;
	io_registers[REG_BG2PB]    = 0x0000;
	io_registers[REG_BG2PC]    = 0x0000;
	io_registers[REG_BG2PD]    = 0x0100;
	BG2X_L   = 0x0000;
	BG2X_H   = 0x0000;
	BG2Y_L   = 0x0000;
	BG2Y_H   = 0x0000;
	io_registers[REG_BG3PA]    = 0x0100;
	io_registers[REG_BG3PB]    = 0x0000;
	io_registers[REG_BG3PC]    = 0x0000;
	io_registers[REG_BG3PD]    = 0x0100;
	BG3X_L   = 0x0000;
	BG3X_H   = 0x0000;
	BG3Y_L   = 0x0000;
	BG3Y_H   = 0x0000;
	io_registers[REG_WIN0H]    = 0x0000;
	io_registers[REG_WIN1H]    = 0x0000;
	io_registers[REG_WIN0V]    = 0x0000;
	io_registers[REG_WIN1V]    = 0x0000;
	io_registers[REG_WININ]    = 0x0000;
	io_registers[REG_WINOUT]   = 0x0000;
	MOSAIC   = 0x0000;
	BLDMOD   = 0x0000;
	COLEV    = 0x0000;
	COLY     = 0x0000;
	DM0SAD_L = 0x0000;
	DM0SAD_H = 0x0000;
	DM0DAD_L = 0x0000;
	DM0DAD_H = 0x0000;
	DM0CNT_L = 0x0000;
	DM0CNT_H = 0x0000;
	DM1SAD_L = 0x0000;
	DM1SAD_H = 0x0000;
	DM1DAD_L = 0x0000;
	DM1DAD_H = 0x0000;
	DM1CNT_L = 0x0000;
	DM1CNT_H = 0x0000;
	DM2SAD_L = 0x0000;
	DM2SAD_H = 0x0000;
	DM2DAD_L = 0x0000;
	DM2DAD_H = 0x0000;
	DM2CNT_L = 0x0000;
	DM2CNT_H = 0x0000;
	DM3SAD_L = 0x0000;
	DM3SAD_H = 0x0000;
	DM3DAD_L = 0x0000;
	DM3DAD_H = 0x0000;
	DM3CNT_L = 0x0000;
	DM3CNT_H = 0x0000;
	io_registers[REG_TM0D]     = 0x0000;
	io_registers[REG_TM0CNT]   = 0x0000;
	io_registers[REG_TM1D]     = 0x0000;
	io_registers[REG_TM1CNT]   = 0x0000;
	io_registers[REG_TM2D]     = 0x0000;
	io_registers[REG_TM2CNT]   = 0x0000;
	io_registers[REG_TM3D]     = 0x0000;
	io_registers[REG_TM3CNT]   = 0x0000;
	io_registers[REG_P1]       = 0x03FF;
	io_registers[REG_IE]       = 0x0000;
	io_registers[REG_IF]       = 0x0000;
	io_registers[REG_IME]      = 0x0000;

	armMode = 0x1F;

	if(cpuIsMultiBoot) {
		bus.reg[13].I = 0x03007F00;
		bus.reg[15].I = 0x02000000;
		bus.reg[16].I = 0x00000000;
		bus.reg[R13_IRQ].I = 0x03007FA0;
		bus.reg[R13_SVC].I = 0x03007FE0;
		armIrqEnable = true;
	} else {
#ifdef HAVE_HLE_BIOS
		if(useBios && !skipBios)
		{
			bus.reg[15].I = 0x00000000;
			armMode = 0x13;
			armIrqEnable = false;
		}
		else
		{
#endif
			bus.reg[13].I = 0x03007F00;
			bus.reg[15].I = 0x08000000;
			bus.reg[16].I = 0x00000000;
			bus.reg[R13_IRQ].I = 0x03007FA0;
			bus.reg[R13_SVC].I = 0x03007FE0;
			armIrqEnable = true;
#ifdef HAVE_HLE_BIOS
		}
#endif
	}
	armState = true;
	C_FLAG = V_FLAG = N_FLAG = Z_FLAG = false;
	UPDATE_REG(0x00, io_registers[REG_DISPCNT]);
	UPDATE_REG(0x06, io_registers[REG_VCOUNT]);
	UPDATE_REG(0x20, io_registers[REG_BG2PA]);
	UPDATE_REG(0x26, io_registers[REG_BG2PD]);
	UPDATE_REG(0x30, io_registers[REG_BG3PA]);
	UPDATE_REG(0x36, io_registers[REG_BG3PD]);
	UPDATE_REG(0x130, io_registers[REG_P1]);
	UPDATE_REG(0x88, 0x200);

	// disable FIQ
	bus.reg[16].I |= 0x40;

	CPU_UPDATE_CPSR();

	bus.armNextPC = bus.reg[15].I;
	bus.reg[15].I += 4;

	// reset internal state
	holdState = false;

	biosProtected[0] = 0x00;
	biosProtected[1] = 0xf0;
	biosProtected[2] = 0x29;
	biosProtected[3] = 0xe1;

	graphics.lcdTicks = (useBios && !skipBios) ? 1008 : 208;
	timer0On = false;
	timer0Ticks = 0;
	timer0Reload = 0;
	timer0ClockReload  = 0;
	timer1On = false;
	timer1Ticks = 0;
	timer1Reload = 0;
	timer1ClockReload  = 0;
	timer2On = false;
	timer2Ticks = 0;
	timer2Reload = 0;
	timer2ClockReload  = 0;
	timer3On = false;
	timer3Ticks = 0;
	timer3Reload = 0;
	timer3ClockReload  = 0;
	dma0Source = 0;
	dma0Dest = 0;
	dma1Source = 0;
	dma1Dest = 0;
	dma2Source = 0;
	dma2Dest = 0;
	dma3Source = 0;
	dma3Dest = 0;
	renderLine = &Gigazoid::mode0RenderLine;
	fxOn = false;
	windowOn = false;
	graphics.layerEnable = io_registers[REG_DISPCNT];

	memset(line[0], -1, 240 * sizeof(u32));
	memset(line[1], -1, 240 * sizeof(u32));
	memset(line[2], -1, 240 * sizeof(u32));
	memset(line[3], -1, 240 * sizeof(u32));

	for(int i = 0; i < 256; i++) {
		map[i].address = 0;
		map[i].mask = 0;
	}

	map[0].address = bios;
	map[0].mask = 0x3FFF;
	map[2].address = workRAM;
	map[2].mask = 0x3FFFF;
	map[3].address = internalRAM;
	map[3].mask = 0x7FFF;
	map[4].address = ioMem;
	map[4].mask = 0x3FF;
	map[5].address = graphics.paletteRAM;
	map[5].mask = 0x3FF;
	map[6].address = vram;
	map[6].mask = 0x1FFFF;
	map[7].address = oam;
	map[7].mask = 0x3FF;
	map[8].address = rom;
	map[8].mask = 0x1FFFFFF;
	map[9].address = rom;
	map[9].mask = 0x1FFFFFF;
	map[10].address = rom;
	map[10].mask = 0x1FFFFFF;
	map[12].address = rom;
	map[12].mask = 0x1FFFFFF;
	map[14].address = flashSaveMemory;
	map[14].mask = 0xFFFF;

	eepromReset();
	flashReset();

	soundReset();

	CPUUpdateWindow0();
	CPUUpdateWindow1();

	// make sure registers are correctly initialized if not using BIOS
	if(cpuIsMultiBoot)
		BIOS_RegisterRamReset(0xfe);
	else if(!useBios && !cpuIsMultiBoot)
		BIOS_RegisterRamReset(0xff);
	else if (skipBios)
		BIOS_RegisterRamReset(0xff); // ??
		
	ARM_PREFETCH;
}

void CPUInterrupt(void)
{
	uint32_t PC = bus.reg[15].I;
	bool savedState = armState;

	if(armMode != 0x12 )
		CPUSwitchMode(0x12, true, false);

	bus.reg[14].I = PC;
	if(!savedState)
		bus.reg[14].I += 2;
	bus.reg[15].I = 0x18;
	armState = true;
	armIrqEnable = false;

	bus.armNextPC = bus.reg[15].I;
	bus.reg[15].I += 4;
	ARM_PREFETCH;

	//  if(!holdState)
	biosProtected[0] = 0x02;
	biosProtected[1] = 0xc0;
	biosProtected[2] = 0x5e;
	biosProtected[3] = 0xe5;
}

void CPULoop (void)
{
	bus.busPrefetchCount = 0;
	int ticks = 300000;
	int timerOverflow = 0;
	// variable used by the CPU core
	cpuTotalTicks = 0;

	cpuNextEvent = CPUUpdateTicks();
	if(cpuNextEvent > ticks)
		cpuNextEvent = ticks;

	bool framedone = false;

	do
	{
		if(!holdState)
		{
			if(armState)
			{
				if (!armExecute())
					return;
			}
			else
			{
				if (!thumbExecute())
					return;
			}
			clockTicks = 0;
		}
		else
			clockTicks = CPUUpdateTicks();

		cpuTotalTicks += clockTicks;


		if(cpuTotalTicks >= cpuNextEvent) {
			int remainingTicks = cpuTotalTicks - cpuNextEvent;

			clockTicks = cpuNextEvent;
			cpuTotalTicks = 0;

updateLoop:

			if (IRQTicks)
			{
				IRQTicks -= clockTicks;
				if (IRQTicks<0)
					IRQTicks = 0;
			}

			graphics.lcdTicks -= clockTicks;

			soundTicksUp += clockTicks;

			AdvanceRTC(clockTicks);

			if(graphics.lcdTicks <= 0)
			{
				if(io_registers[REG_DISPSTAT] & 1)
				{ // V-BLANK
					// if in V-Blank mode, keep computing...
					if(io_registers[REG_DISPSTAT] & 2)
					{
						graphics.lcdTicks += 1008;
						io_registers[REG_VCOUNT] += 1;
						UPDATE_REG(0x06, io_registers[REG_VCOUNT]);
						io_registers[REG_DISPSTAT] &= 0xFFFD;
						UPDATE_REG(0x04, io_registers[REG_DISPSTAT]);
						CPUCompareVCOUNT();
					}
					else
					{
						graphics.lcdTicks += 224;
						io_registers[REG_DISPSTAT] |= 2;
						UPDATE_REG(0x04, io_registers[REG_DISPSTAT]);
						if(io_registers[REG_DISPSTAT] & 16)
						{
							io_registers[REG_IF] |= 2;
							UPDATE_REG(0x202, io_registers[REG_IF]);
						}
						if (scanlineCallback && scanlineCallbackLine == io_registers[REG_VCOUNT])
							scanlineCallback();
					}

					if(io_registers[REG_VCOUNT] >= 228)
					{
						//Reaching last line
						io_registers[REG_DISPSTAT] &= 0xFFFC;
						UPDATE_REG(0x04, io_registers[REG_DISPSTAT]);
						io_registers[REG_VCOUNT] = 0;
						UPDATE_REG(0x06, io_registers[REG_VCOUNT]);
						CPUCompareVCOUNT();
					}
				}
				else if(io_registers[REG_DISPSTAT] & 2)
				{
					// if in H-Blank, leave it and move to drawing mode
					io_registers[REG_VCOUNT] += 1;
					UPDATE_REG(0x06, io_registers[REG_VCOUNT]);
					graphics.lcdTicks += 1008;
					io_registers[REG_DISPSTAT] &= 0xFFFD;
					if(io_registers[REG_VCOUNT] == 160)
					{
						// moved to start of emulated frame
						//UpdateJoypad();

						io_registers[REG_DISPSTAT] |= 1;
						io_registers[REG_DISPSTAT] &= 0xFFFD;
						UPDATE_REG(0x04, io_registers[REG_DISPSTAT]);
						if(io_registers[REG_DISPSTAT] & 0x0008)
						{
							io_registers[REG_IF] |= 1;
							UPDATE_REG(0x202, io_registers[REG_IF]);
						}
						CPUCheckDMA(1, 0x0f);
						systemDrawScreen();

						process_sound_tick_fn();
						framedone = true;
					}

					UPDATE_REG(0x04, io_registers[REG_DISPSTAT]);
					CPUCompareVCOUNT();
				}
				else
				{
					bool draw_objwin = (graphics.layerEnable & 0x9000) == 0x9000;
					bool draw_sprites = graphics.layerEnable & 0x1000;
					memset(line[4], -1, 240 * sizeof(u32));	// erase all sprites

					if(draw_sprites)
						gfxDrawSprites();

					if(render_line_all_enabled)
					{
						memset(line[5], -1, 240 * sizeof(u32));	// erase all OBJ Win 
						if(draw_objwin)
							gfxDrawOBJWin();
					}

					(this->*renderLine)();

					// entering H-Blank
					io_registers[REG_DISPSTAT] |= 2;
					UPDATE_REG(0x04, io_registers[REG_DISPSTAT]);
					graphics.lcdTicks += 224;
					CPUCheckDMA(2, 0x0f);
					if(io_registers[REG_DISPSTAT] & 16)
					{
						io_registers[REG_IF] |= 2;
						UPDATE_REG(0x202, io_registers[REG_IF]);
					}
					if (scanlineCallback && scanlineCallbackLine == io_registers[REG_VCOUNT])
						scanlineCallback();
				}
			}

			// we shouldn't be doing sound in stop state, but we lose synchronization
			// if sound is disabled, so in stop state, soundTick will just produce
			// mute sound

			// moving this may have consequences; we'll see
			//soundTicksUp += clockTicks;

			if(!stopState) {
				if(timer0On) {
					timer0Ticks -= clockTicks;
					if(timer0Ticks <= 0) {
						timer0Ticks += (0x10000 - timer0Reload) << timer0ClockReload;
						timerOverflow |= 1;
						soundTimerOverflow(0);
						if(io_registers[REG_TM0CNT] & 0x40) {
							io_registers[REG_IF] |= 0x08;
							UPDATE_REG(0x202, io_registers[REG_IF]);
						}
					}
					io_registers[REG_TM0D] = 0xFFFF - (timer0Ticks >> timer0ClockReload);
					UPDATE_REG(0x100, io_registers[REG_TM0D]);
				}

				if(timer1On) {
					if(io_registers[REG_TM1CNT] & 4) {
						if(timerOverflow & 1) {
							io_registers[REG_TM1D]++;
							if(io_registers[REG_TM1D] == 0) {
								io_registers[REG_TM1D] += timer1Reload;
								timerOverflow |= 2;
								soundTimerOverflow(1);
								if(io_registers[REG_TM1CNT] & 0x40) {
									io_registers[REG_IF] |= 0x10;
									UPDATE_REG(0x202, io_registers[REG_IF]);
								}
							}
							UPDATE_REG(0x104, io_registers[REG_TM1D]);
						}
					} else {
						timer1Ticks -= clockTicks;
						if(timer1Ticks <= 0) {
							timer1Ticks += (0x10000 - timer1Reload) << timer1ClockReload;
							timerOverflow |= 2;
							soundTimerOverflow(1);
							if(io_registers[REG_TM1CNT] & 0x40) {
								io_registers[REG_IF] |= 0x10;
								UPDATE_REG(0x202, io_registers[REG_IF]);
							}
						}
						io_registers[REG_TM1D] = 0xFFFF - (timer1Ticks >> timer1ClockReload);
						UPDATE_REG(0x104, io_registers[REG_TM1D]);
					}
				}

				if(timer2On) {
					if(io_registers[REG_TM2CNT] & 4) {
						if(timerOverflow & 2) {
							io_registers[REG_TM2D]++;
							if(io_registers[REG_TM2D] == 0) {
								io_registers[REG_TM2D] += timer2Reload;
								timerOverflow |= 4;
								if(io_registers[REG_TM2CNT] & 0x40) {
									io_registers[REG_IF] |= 0x20;
									UPDATE_REG(0x202, io_registers[REG_IF]);
								}
							}
							UPDATE_REG(0x108, io_registers[REG_TM2D]);
						}
					} else {
						timer2Ticks -= clockTicks;
						if(timer2Ticks <= 0) {
							timer2Ticks += (0x10000 - timer2Reload) << timer2ClockReload;
							timerOverflow |= 4;
							if(io_registers[REG_TM2CNT] & 0x40) {
								io_registers[REG_IF] |= 0x20;
								UPDATE_REG(0x202, io_registers[REG_IF]);
							}
						}
						io_registers[REG_TM2D] = 0xFFFF - (timer2Ticks >> timer2ClockReload);
						UPDATE_REG(0x108, io_registers[REG_TM2D]);
					}
				}

				if(timer3On) {
					if(io_registers[REG_TM3CNT] & 4) {
						if(timerOverflow & 4) {
							io_registers[REG_TM3D]++;
							if(io_registers[REG_TM3D] == 0) {
								io_registers[REG_TM3D] += timer3Reload;
								if(io_registers[REG_TM3CNT] & 0x40) {
									io_registers[REG_IF] |= 0x40;
									UPDATE_REG(0x202, io_registers[REG_IF]);
								}
							}
							UPDATE_REG(0x10C, io_registers[REG_TM3D]);
						}
					} else {
						timer3Ticks -= clockTicks;
						if(timer3Ticks <= 0) {
							timer3Ticks += (0x10000 - timer3Reload) << timer3ClockReload;
							if(io_registers[REG_TM3CNT] & 0x40) {
								io_registers[REG_IF] |= 0x40;
								UPDATE_REG(0x202, io_registers[REG_IF]);
							}
						}
						io_registers[REG_TM3D] = 0xFFFF - (timer3Ticks >> timer3ClockReload);
						UPDATE_REG(0x10C, io_registers[REG_TM3D]);
					}
				}
			}

			timerOverflow = 0;
			ticks -= clockTicks;
			cpuNextEvent = CPUUpdateTicks();

			if(cpuDmaTicksToUpdate > 0)
			{
				if(cpuDmaTicksToUpdate > cpuNextEvent)
					clockTicks = cpuNextEvent;
				else
					clockTicks = cpuDmaTicksToUpdate;
				cpuDmaTicksToUpdate -= clockTicks;
				if(cpuDmaTicksToUpdate < 0)
					cpuDmaTicksToUpdate = 0;
				goto skipIRQ;
			}

			if(io_registers[REG_IF] && (io_registers[REG_IME] & 1) && armIrqEnable)
			{
				int res = io_registers[REG_IF] & io_registers[REG_IE];
				if(stopState)
					res &= 0x3080;
				if(res)
				{
					if (intState)
					{
						if (!IRQTicks)
						{
							CPUInterrupt();
							intState = false;
							holdState = false;
							stopState = false;
						}
					}
					else
					{
						if (!holdState)
						{
							intState = true;
							IRQTicks=7;
							if (cpuNextEvent> IRQTicks)
								cpuNextEvent = IRQTicks;
						}
						else
						{
							CPUInterrupt();
							holdState = false;
							stopState = false;
						}
					}
				}
			}

			skipIRQ:

			if(remainingTicks > 0) {
				if(remainingTicks > cpuNextEvent)
					clockTicks = cpuNextEvent;
				else
					clockTicks = remainingTicks;
				remainingTicks -= clockTicks;
				if(remainingTicks < 0)
					remainingTicks = 0;
				goto updateLoop;
			}

			if (timerOnOffDelay)
			{
				// Apply Timer
				if (timerOnOffDelay & 1)
				{
					timer0ClockReload = TIMER_TICKS[timer0Value & 3];
					if(!timer0On && (timer0Value & 0x80)) {
						// reload the counter
						io_registers[REG_TM0D] = timer0Reload;
						timer0Ticks = (0x10000 - io_registers[REG_TM0D]) << timer0ClockReload;
						UPDATE_REG(0x100, io_registers[REG_TM0D]);
					}
					timer0On = timer0Value & 0x80 ? true : false;
					io_registers[REG_TM0CNT] = timer0Value & 0xC7;
					UPDATE_REG(0x102, io_registers[REG_TM0CNT]);
				}
				if (timerOnOffDelay & 2)
				{
					timer1ClockReload = TIMER_TICKS[timer1Value & 3];
					if(!timer1On && (timer1Value & 0x80)) {
						// reload the counter
						io_registers[REG_TM1D] = timer1Reload;
						timer1Ticks = (0x10000 - io_registers[REG_TM1D]) << timer1ClockReload;
						UPDATE_REG(0x104, io_registers[REG_TM1D]);
					}
					timer1On = timer1Value & 0x80 ? true : false;
					io_registers[REG_TM1CNT] = timer1Value & 0xC7;
					UPDATE_REG(0x106, io_registers[REG_TM1CNT]);
				}
				if (timerOnOffDelay & 4)
				{
					timer2ClockReload = TIMER_TICKS[timer2Value & 3];
					if(!timer2On && (timer2Value & 0x80)) {
						// reload the counter
						io_registers[REG_TM2D] = timer2Reload;
						timer2Ticks = (0x10000 - io_registers[REG_TM2D]) << timer2ClockReload;
						UPDATE_REG(0x108, io_registers[REG_TM2D]);
					}
					timer2On = timer2Value & 0x80 ? true : false;
					io_registers[REG_TM2CNT] = timer2Value & 0xC7;
					UPDATE_REG(0x10A, io_registers[REG_TM2CNT]);
				}
				if (timerOnOffDelay & 8)
				{
					timer3ClockReload = TIMER_TICKS[timer3Value & 3];
					if(!timer3On && (timer3Value & 0x80)) {
						// reload the counter
						io_registers[REG_TM3D] = timer3Reload;
						timer3Ticks = (0x10000 - io_registers[REG_TM3D]) << timer3ClockReload;
						UPDATE_REG(0x10C, io_registers[REG_TM3D]);
					}
					timer3On = timer3Value & 0x80 ? true : false;
					io_registers[REG_TM3CNT] = timer3Value & 0xC7;
					UPDATE_REG(0x10E, io_registers[REG_TM3CNT]);
				}
				cpuNextEvent = CPUUpdateTicks();
				timerOnOffDelay = 0;
				// End of Apply Timer
			}

			if(cpuNextEvent > ticks)
				cpuNextEvent = ticks;

			if(ticks <= 0 || framedone)
				break;

		}
	}while(1);
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// END GBA.CPP
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void Gigazoid_Init()
{
	// one time constructor stuff

	flashState = FLASH_READ_ARRAY;
	flashReadState = FLASH_READ_ARRAY;
	flashSize = 0x10000;
	flashDeviceID = 0x1b;
	flashManufacturerID = 0x32;
	flashBank = 0;

	eepromMode = EEPROM_IDLE;

	eepromInUse = false;
	eepromSize = 512;

	// this is constant now
	// soundSampleRate    = 22050;

	soundEnableFlag   = 0x3ff; /* emulator channels enabled*/

	armState = true;
	armIrqEnable = true;
	armMode = 0x1f;

	renderLine = &Gigazoid::mode0RenderLine;

	#define ARRAYINIT(n) memcpy((n), (n##_init), sizeof(n))
	ARRAYINIT(memoryWait);
	ARRAYINIT(memoryWaitSeq);
	ARRAYINIT(memoryWait32);
	ARRAYINIT(memoryWaitSeq32);
	#undef ARRAYINIT
}

u32 *systemVideoFrameDest;
u32 *systemVideoFramePalette;
s16 *systemAudioFrameDest;
int *systemAudioFrameSamp;
bool lagged;

void (*scanlineCallback)();
int scanlineCallbackLine;

void (*fetchCallback)(u32 addr);
void (*writeCallback)(u32 addr);
void (*readCallback)(u32 addr);
void (*traceCallback)(u32 addr, u32 opcode);

void (*padCallback)();

void systemDrawScreen (void)
{
	// upconvert 555->888 (TODO: BETTER)
	for (int i = 0; i < 240 * 160; i++)
	{
		u32 input = pix[i];
		/*
		u32 output = 0xff000000 |
			input << 9 & 0xf80000 |
			input << 6 & 0xf800 |
			input << 3 & 0xf8;
		*/
		u32 output = systemVideoFramePalette[input];
		systemVideoFrameDest[i] = output;
	}
	systemVideoFrameDest = nullptr;
	systemVideoFramePalette = nullptr;
}

// called at regular intervals on sound clock
void systemOnWriteDataToSoundBuffer(int16_t * finalWave, int length)
{
	memcpy(systemAudioFrameDest, finalWave, length * 2);
	systemAudioFrameDest = nullptr;
	*systemAudioFrameSamp = length / 2;
	systemAudioFrameSamp = nullptr;
}

void UpdateJoypad()
{
	/* update joystick information */
	io_registers[REG_P1] = 0x03FF ^ (joy & 0x3FF);
#if 0
	if(cpuEEPROMSensorEnabled)
		systemUpdateMotionSensor();
#endif
	UPDATE_REG(0x130, io_registers[REG_P1]);
	io_registers[REG_P1CNT] = READ16LE(((uint16_t *)&ioMem[0x132]));

	// this seems wrong, but there are cases where the game
	// can enter the stop state without requesting an IRQ from
	// the joypad.
	if((io_registers[REG_P1CNT] & 0x4000) || stopState) {
		uint16_t p1 = (0x3FF ^ io_registers[REG_P1CNT]) & 0x3FF;
		if(io_registers[REG_P1CNT] & 0x8000) {
			if(p1 == (io_registers[REG_P1CNT] & 0x3FF)) {
				io_registers[REG_IF] |= 0x1000;
				UPDATE_REG(0x202, io_registers[REG_IF]);
			}
		} else {
			if(p1 & io_registers[REG_P1CNT]) {
				io_registers[REG_IF] |= 0x1000;
				UPDATE_REG(0x202, io_registers[REG_IF]);
			}
		}
	}
}

public:

template<bool isReader>void SyncState(NewState *ns)
{
	NSS(flashSaveMemory);
	NSS(flashState);
	NSS(flashReadState);
	NSS(flashSize);
	NSS(flashDeviceID);
	NSS(flashManufacturerID);
	NSS(flashBank);

	NSS(eepromMode);
	NSS(eepromByte);
	NSS(eepromBits);
	NSS(eepromAddress);
	NSS(eepromData);
	NSS(eepromBuffer);
	NSS(eepromInUse);
	NSS(eepromSize);

	NSS(rtcClockData);
	NSS(rtcEnabled);
	SSS(rtcInternalTime);
	NSS(RTCTicks);
	NSS(RTCUseRealTime);

	NSS(soundTicksUp);
	NSS(soundEnableFlag);
	SSS_HACKY(pcm[0], this);
	SSS_HACKY(pcm[1], this);
	SSS(pcm_synth);

	SSS(bufs_buffer[0]);
	SSS(bufs_buffer[1]);
	SSS(bufs_buffer[2]);
	NSS(mixer_samples_read);

	SSS(gb_apu);

	NSS(cpuNextEvent);
	NSS(holdState);
	NSS(cpuPrefetch);
	NSS(cpuTotalTicks);
	NSS(memoryWait);
	NSS(memoryWaitSeq);
	NSS(memoryWait32);
	NSS(memoryWaitSeq32);
	NSS(biosProtected);
	NSS(cpuBitsSet);

	NSS(N_FLAG);
	NSS(C_FLAG);
	NSS(Z_FLAG);
	NSS(V_FLAG);
	NSS(armState);
	NSS(armIrqEnable);
	NSS(armMode);

	NSS(io_registers);

	NSS(MOSAIC);

	NSS(BG2X_L);
	NSS(BG2X_H);
	NSS(BG2Y_L);
	NSS(BG2Y_H);
	NSS(BG3X_L);
	NSS(BG3X_H);
	NSS(BG3Y_L);
	NSS(BG3Y_H);
	NSS(BLDMOD);
	NSS(COLEV);
	NSS(COLY);
	NSS(DM0SAD_L);
	NSS(DM0SAD_H);
	NSS(DM0DAD_L);
	NSS(DM0DAD_H);
	NSS(DM0CNT_L);
	NSS(DM0CNT_H);
	NSS(DM1SAD_L);
	NSS(DM1SAD_H);
	NSS(DM1DAD_L);
	NSS(DM1DAD_H);
	NSS(DM1CNT_L);
	NSS(DM1CNT_H);
	NSS(DM2SAD_L);
	NSS(DM2SAD_H);
	NSS(DM2DAD_L);
	NSS(DM2DAD_H);
	NSS(DM2CNT_L);
	NSS(DM2CNT_H);
	NSS(DM3SAD_L);
	NSS(DM3SAD_H);
	NSS(DM3DAD_L);
	NSS(DM3DAD_H);
	NSS(DM3CNT_L);
	NSS(DM3CNT_H);

	NSS(timerOnOffDelay);
	NSS(timer0Value);
	NSS(dma0Source);
	NSS(dma0Dest);
	NSS(dma1Source);
	NSS(dma1Dest);
	NSS(dma2Source);
	NSS(dma2Dest);
	NSS(dma3Source);
	NSS(dma3Dest);

	EBS(cpuSaveGameFunc, 0);
	EVS(cpuSaveGameFunc, &Gigazoid::flashWrite, 1);
	EVS(cpuSaveGameFunc, &Gigazoid::sramWrite, 2);
	EVS(cpuSaveGameFunc, &Gigazoid::flashSaveDecide, 3);
	EVS(cpuSaveGameFunc, &Gigazoid::dummyWrite, 4);
	EES(cpuSaveGameFunc, nullptr);

	NSS(fxOn);
	NSS(windowOn);
	NSS(cpuDmaTicksToUpdate);
	NSS(IRQTicks);
	NSS(intState);

	NSS(bus);
	NSS(graphics);

	// map; // values never change

	NSS(clockTicks);

	NSS(romSize);
	NSS(line);
	NSS(gfxInWin);
	NSS(lineOBJpixleft);
	NSS(joy);

	NSS(gfxBG2Changed);
	NSS(gfxBG3Changed);

	NSS(gfxBG2X);
	NSS(gfxBG2Y);
	NSS(gfxBG3X);
	NSS(gfxBG3Y);

	NSS(ioReadable);

	NSS(stopState);

	NSS(timer0On);
	NSS(timer0Ticks);
	NSS(timer0Reload);
	NSS(timer0ClockReload);
	NSS(timer1Value);
	NSS(timer1On);
	NSS(timer1Ticks);
	NSS(timer1Reload);
	NSS(timer1ClockReload);
	NSS(timer2Value);
	NSS(timer2On);
	NSS(timer2Ticks);
	NSS(timer2Reload);
	NSS(timer2ClockReload);
	NSS(timer3Value);
	NSS(timer3On);
	NSS(timer3Ticks);
	NSS(timer3Reload);
	NSS(timer3ClockReload);

	NSS(skipBios);
	NSS(cpuSaveType);
	NSS(mirroringEnable);

	NSS(cpuDmaCount);

	NSS(internalRAM);
	NSS(workRAM);
	NSS(vram);
	NSS(pix);
	NSS(oam);
	NSS(ioMem);

	NSS(cpuEEPROMEnabled);
	NSS(cpuEEPROMSensorEnabled);

	EBS(renderLine, 0);
	EVS(renderLine, &Gigazoid::mode0RenderLine, 0x01);
	EVS(renderLine, &Gigazoid::mode0RenderLineNoWindow, 0x02);
	EVS(renderLine, &Gigazoid::mode0RenderLineAll, 0x03);
	EVS(renderLine, &Gigazoid::mode1RenderLine, 0x11);
	EVS(renderLine, &Gigazoid::mode1RenderLineNoWindow, 0x12);
	EVS(renderLine, &Gigazoid::mode1RenderLineAll, 0x13);
	EVS(renderLine, &Gigazoid::mode2RenderLine, 0x21);
	EVS(renderLine, &Gigazoid::mode2RenderLineNoWindow, 0x22);
	EVS(renderLine, &Gigazoid::mode2RenderLineAll, 0x23);
	EVS(renderLine, &Gigazoid::mode3RenderLine, 0x31);
	EVS(renderLine, &Gigazoid::mode3RenderLineNoWindow, 0x32);
	EVS(renderLine, &Gigazoid::mode3RenderLineAll, 0x33);
	EVS(renderLine, &Gigazoid::mode4RenderLine, 0x41);
	EVS(renderLine, &Gigazoid::mode4RenderLineNoWindow, 0x42);
	EVS(renderLine, &Gigazoid::mode4RenderLineAll, 0x43);
	EVS(renderLine, &Gigazoid::mode5RenderLine, 0x51);
	EVS(renderLine, &Gigazoid::mode5RenderLineNoWindow, 0x52);
	EVS(renderLine, &Gigazoid::mode5RenderLineAll, 0x53);
	EES(renderLine, nullptr);

	NSS(render_line_all_enabled);

	// address_lut; // values never change
	
	NSS(lagged);
}

// load a legacy battery ram file to a place where it might work, who knows
void LoadLegacyBatteryRam(const char *data, int len)
{
	std::memcpy(eepromData, data, std::min<int>(len, sizeof(eepromData)));
	std::memcpy(flashSaveMemory, data, std::min<int>(len, sizeof(flashSaveMemory)));
	if (len <= 0x10000)
	{
		// can salvage broken pokeymans saves in some cases
		std::memcpy(flashSaveMemory + 0x10000, data, std::min<int>(len, 0x10000));
	}
}

bool HasBatteryRam()
{
	return cpuSaveType != 5;
}

int BatteryRamSize()
{
	switch (cpuSaveType)
	{
	default:
	case 0: // auto
		return 0x10000;
	case 1:
	case 4: // eeprom
		return eepromSize;
	case 2: // sram
		// should only be 32K, but vba uses 64K as a stand-in for both SRAM (guess no game ever checks mirroring?),
		// and for 64K flash where the program never issues any flash commands
		return 0x10000;
	case 3: // flash
		return flashSize;
	case 5: // none
		return 0;
	}
}

void SaveLegacyBatteryRam(char *dest)
{
	switch (cpuSaveType)
	{
	default:
	case 0: // auto
		std::memcpy(dest, flashSaveMemory, 0x10000);
		return;
	case 1:
	case 4: // eeprom
		std::memcpy(dest, eepromData, eepromSize);
		return;
	case 2: // sram
		// should only be 32K, but vba uses 64K as a stand-in for both SRAM (guess no game ever checks mirroring?),
		// and for 64K flash where the program never issues any flash commands
		std::memcpy(dest, flashSaveMemory, 0x10000);
		return;
	case 3: // flash
		std::memcpy(dest, flashSaveMemory, flashSize);
		return;
	case 5: // none
		return;
	}
}

template<bool isReader>bool SyncBatteryRam(NewState *ns)
{
	// if we were given a positive ID from the gamedb, we can choose to save/load only that type
	// else, we save\load everything -- even if we used our knowledge of the current state to
	// save only what was needed, we'd have to save that metadata as well for load

	// file id detection
	char batteryramid[8];
	std::memcpy(batteryramid, "GBABATT\0", 8);
	NSS(batteryramid);
	if (std::memcmp(batteryramid, "GBABATT\0", 8) != 0)
		return false;

	int flashFileSize;
	int eepromFileSize;

	// when writing, try to figure out the sizes as smartly as we can
	switch (cpuSaveType)
	{
	default:
	case 0: // auto
		flashFileSize = 0x20000;
		eepromFileSize = 0x2000;
		break;
	case 1:
	case 4: // eeprom
		flashFileSize = 0;
		eepromFileSize = eepromSize;
		break;
	case 2: // sram
		// should only be 32K, but vba uses 64K as a stand-in for both SRAM (guess no game ever checks mirroring?),
		// and for 64K flash where the program never issues any flash commands
		flashFileSize = 0x10000;
		eepromFileSize = 0;
		break;
	case 3: // flash
		flashFileSize = flashSize;
		eepromFileSize = 0;
		break;
	case 5: // none
		flashFileSize = 0;
		eepromFileSize = 0;
		break;
	}
	NSS(flashFileSize);
	NSS(eepromFileSize);
	// when reading, cap to allowable limits.  any save file with numbers larger than this is not legal.
	flashFileSize = std::min<int>(flashFileSize, sizeof(flashSaveMemory));
	eepromFileSize = std::min<int>(eepromFileSize, sizeof(eepromData));

	PSS(flashSaveMemory, flashFileSize);
	PSS(eepromData, eepromFileSize);

	return true;
}

	Gigazoid()
	{
		Gigazoid_Init();
	}

	~Gigazoid()
	{
	}

	bool LoadRom(const u8 *romfile, const u32 romfilelen, const u8 *biosfile, const u32 biosfilelen, const FrontEndSettings &settings)
	{
		if (biosfilelen != 16384)
			return false;

		if (!CPULoadRom(romfile, romfilelen))
			return false;

		cpuSaveType = settings.cpuSaveType;
		flashSize = settings.flashSize;
		rtcEnabled = settings.enableRtc;
		mirroringEnable = settings.mirroringEnable;
		skipBios = settings.skipBios;

		RTCUseRealTime = settings.RTCuseRealTime;
		rtcInternalTime.hour = settings.RTChour;
		rtcInternalTime.mday = settings.RTCmday;
		rtcInternalTime.min = settings.RTCmin;
		rtcInternalTime.month = settings.RTCmonth;
		rtcInternalTime.sec = settings.RTCsec;
		rtcInternalTime.wday = settings.RTCwday;
		rtcInternalTime.year = settings.RTCyear;

		if(flashSize == 0x10000)
		{
			flashDeviceID = 0x1b;
			flashManufacturerID = 0x32;
		}
		else
		{
			flashDeviceID = 0x13; //0x09;
			flashManufacturerID = 0x62; //0xc2;
		}

		doMirroring(mirroringEnable);

		CPUInit(biosfile, biosfilelen);
		CPUReset();
		
		// CPUReset already calls this, but if that were to change
		// soundReset();
		
		return true;
	}

	void Reset()
	{
		CPUReset();
	}

	bool FrameAdvance(int input, u32 *videobuffer, s16 *audiobuffer, int *numsamp, u32 *videopalette)
	{
		joy = input;
		systemVideoFrameDest = videobuffer;
		systemVideoFramePalette = videopalette;
		systemAudioFrameDest = audiobuffer;
		systemAudioFrameSamp = numsamp;
		lagged = true;
		UpdateJoypad();
		do
		{
			CPULoop();
		} while (systemVideoFrameDest);
		return lagged;
	}

	void FillMemoryAreas(MemoryAreas &mem)
	{
		mem.bios = bios;
		mem.iwram = internalRAM;
		mem.ewram = workRAM;
		mem.palram = graphics.paletteRAM;
		mem.mmio = ioMem;
		mem.rom = rom;
		mem.vram = vram;
		mem.oam = oam;
		switch (cpuSaveType)
		{
		default:
		case 0: // auto
			mem.sram = flashSaveMemory;
			mem.sram_size = 0x10000;;
			return;
		case 1:
		case 4: // eeprom
			mem.sram = eepromData;
			mem.sram_size = eepromSize;
			return;
		case 2: // sram
			mem.sram = flashSaveMemory;
			mem.sram_size = 0x10000;
			return;
		case 3: // flash
			mem.sram = flashSaveMemory;
			mem.sram_size = flashSize;
			return;
		case 5: // none
			return;
		}
	}

	void BusWrite(u32 addr, u8 val)
	{
		CPUWriteByte(addr, val);
	}
	u8 BusRead(u32 addr)
	{
		return CPUReadByte(addr);
	}

	void SetScanlineCallback(void (*cb)(), int scanline)
	{
		// the sequence of calls in a frame will be:
		// 160,161,...,227,0,1,...,160
		// calls coincide with entering hblank, or something like that
		if (scanline < 0 || scanline > 227)
			cb = nullptr;
		scanlineCallback = cb;
		scanlineCallbackLine = scanline;
	}

	uint32_t *GetRegisters()
	{
		return &bus.reg[0].I;
	}

	void SetPadCallback(void (*cb)())
	{
		// before each read of the pad regs
		padCallback = cb;
	}

	void SetFetchCallback(void (*cb)(u32 addr))
	{
		// before each opcode fetch
		fetchCallback = cb;
	}

	void SetReadCallback(void (*cb)(u32 addr))
	{
		// before each read, not including opcodes, including pad regs
		readCallback = cb;
	}

	void SetWriteCallback(void (*cb)(u32 addr))
	{
		// before each write
		writeCallback = cb;
	}

	void SetTraceCallback(void (*cb)(u32 addr, u32 opcode))
	{
		// before each opcode fetch
		traceCallback = cb;
	}

}; // class Gigazoid

// zeroing mem operators: these are very important
void *operator new(std::size_t n)
{
	void *p = std::malloc(n);
	std::memset(p, 0, n);
	return p;
}
void operator delete(void *p)
{
	std::free(p);
}

#ifdef _WIN32
#define EXPORT extern "C" __declspec(dllexport)
#elif __linux__
#define EXPORT extern "C"
#endif

// public interface follows
EXPORT Gigazoid *Create()
{
	return new Gigazoid();
}

EXPORT void Destroy(Gigazoid *g)
{
	delete g;
}

EXPORT int LoadRom(Gigazoid *g, const u8 *romfile, const u32 romfilelen, const u8 *biosfile, const u32 biosfilelen, const FrontEndSettings *settings)
{
	return g->LoadRom(romfile, romfilelen, biosfile, biosfilelen, *settings);
}

EXPORT void Reset(Gigazoid *g)
{
	// TODO: this calls a soundreset that seems to remake some buffers.  that seems like it should be fixed?
	g->Reset();
}

EXPORT int FrameAdvance(Gigazoid *g, int input, u32 *videobuffer, s16 *audiobuffer, int *numsamp, u32 *videopalette)
{
	return g->FrameAdvance(input, videobuffer, audiobuffer, numsamp, videopalette);
}

EXPORT int SaveRamSize(Gigazoid *g)
{
	/*
	if (g->HasBatteryRam())
	{
		NewStateDummy dummy;
		g->SyncBatteryRam<false>(&dummy);
		return dummy.GetLength();
	}
	else
	{
		return 0;
	}*/
	return g->BatteryRamSize();
}

EXPORT int SaveRamSave(Gigazoid *g, char *data, int length)
{
	/*
	if (g->HasBatteryRam())
	{
		NewStateExternalBuffer saver(data, length);
		g->SyncBatteryRam<false>(&saver);
		return !saver.Overflow() && saver.GetLength() == length;
	}
	else
	{
		return false;
	}*/
	if (!g->HasBatteryRam() || length != g->BatteryRamSize())
		return false;
	g->SaveLegacyBatteryRam(data);
	return true;
}

EXPORT int SaveRamLoad(Gigazoid *g, const char *data, int length)
{
	if (g->HasBatteryRam())
	{
		NewStateExternalBuffer loader(const_cast<char *>(data), length);
		if (g->SyncBatteryRam<true>(&loader))
		{
			return !loader.Overflow() && loader.GetLength() == length;
		}
		else
		{
			// couldn't find the magic signature at the top, so try a salvage load
			g->LoadLegacyBatteryRam(data, length);
			return true;
		}
	}
	else
	{
		return false;
	}
}

EXPORT int BinStateSize(Gigazoid *g)
{
	NewStateDummy dummy;
	g->SyncState<false>(&dummy);
	return dummy.GetLength();
}

EXPORT int BinStateSave(Gigazoid *g, char *data, int length)
{
	NewStateExternalBuffer saver(data, length);
	g->SyncState<false>(&saver);
	return !saver.Overflow() && saver.GetLength() == length;
}
	
EXPORT int BinStateLoad(Gigazoid *g, const char *data, int length)
{
	NewStateExternalBuffer loader(const_cast<char *>(data), length);
	g->SyncState<true>(&loader);
	return !loader.Overflow() && loader.GetLength() == length;
}

EXPORT void TxtStateSave(Gigazoid *g, FPtrs *ff)
{
	NewStateExternalFunctions saver(ff);
	g->SyncState<false>(&saver);
}

EXPORT void TxtStateLoad(Gigazoid *g, FPtrs *ff)
{
	NewStateExternalFunctions loader(ff);
	g->SyncState<true>(&loader);
}

EXPORT void GetMemoryAreas(Gigazoid *g, MemoryAreas *mem)
{
	g->FillMemoryAreas(*mem);
}

EXPORT void SystemBusWrite(Gigazoid *g, u32 addr, u8 val)
{
	g->BusWrite(addr, val);
}

EXPORT u8 SystemBusRead(Gigazoid *g, u32 addr)
{
	return g->BusRead(addr);
}

EXPORT void SetScanlineCallback(Gigazoid *g, void (*cb)(), int scanline)
{
	g->SetScanlineCallback(cb, scanline);
}

EXPORT void SetTraceCallback(Gigazoid *g, void (*cb)(u32 addr, u32 opcode))
{
	g->SetTraceCallback(cb);
}

EXPORT u32 *GetRegisters(Gigazoid *g)
{
	return g->GetRegisters();
}

EXPORT void SetPadCallback(Gigazoid *g, void (*cb)()) { g->SetPadCallback(cb); }
EXPORT void SetFetchCallback(Gigazoid *g, void (*cb)(u32 addr)) { g->SetFetchCallback(cb); }
EXPORT void SetReadCallback(Gigazoid *g, void (*cb)(u32 addr)) { g->SetReadCallback(cb); }
EXPORT void SetWriteCallback(Gigazoid *g, void (*cb)(u32 addr)) { g->SetWriteCallback(cb); }


#include "optable.inc"
