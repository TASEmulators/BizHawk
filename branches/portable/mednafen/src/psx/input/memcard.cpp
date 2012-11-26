/* Mednafen - Multi-system Emulator
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

// I could find no other commands than 'R', 'W', and 'S' (not sure what 'S' is for, however)

#include "../psx.h"
#include "../frontio.h"
#include "memcard.h"

namespace MDFN_IEN_PSX
{

class InputDevice_Memcard : public InputDevice
{
 public:

 InputDevice_Memcard();
 virtual ~InputDevice_Memcard();

 virtual void Power(void);

 //
 //
 //
 virtual void SetDTR(bool new_dtr);
 virtual bool GetDSR(void);
 virtual bool Clock(bool TxD, int32 &dsr_pulse_delay);

 //
 //
 virtual uint32 GetNVSize(void);
 virtual void ReadNV(uint8 *buffer, uint32 offset, uint32 size);
 virtual void WriteNV(const uint8 *buffer, uint32 offset, uint32 size);

 virtual uint64 GetNVDirtyCount(void);
 virtual void ResetNVDirtyCount(void);

 private:

 bool presence_new;

 uint8 card_data[1 << 17];
 uint8 rw_buffer[128];
 uint8 write_xor;

 uint64 dirty_count;

 bool dtr;
 int32 command_phase;
 uint32 bitpos;
 uint8 receive_buffer;

 uint8 command;
 uint16 addr;
 uint8 calced_xor;

 uint8 transmit_buffer;
 uint32 transmit_count;
};

InputDevice_Memcard::InputDevice_Memcard()
{
 Power();

 dirty_count = 0;

 // Init memcard as formatted.
 assert(sizeof(card_data) == (1 << 17));
 memset(card_data, 0x00, sizeof(card_data));

 card_data[0x00] = 0x4D;
 card_data[0x01] = 0x43;
 card_data[0x7F] = 0x0E;

 for(unsigned int A = 0x80; A < 0x800; A += 0x80)
 {
  card_data[A + 0x00] = 0xA0;
  card_data[A + 0x08] = 0xFF;
  card_data[A + 0x09] = 0xFF;
  card_data[A + 0x7F] = 0xA0;
 }

 for(unsigned int A = 0x0800; A < 0x1200; A += 0x80)
 {
  card_data[A + 0x00] = 0xFF;
  card_data[A + 0x01] = 0xFF;
  card_data[A + 0x02] = 0xFF;
  card_data[A + 0x03] = 0xFF;
  card_data[A + 0x08] = 0xFF;
  card_data[A + 0x09] = 0xFF;
 }

}

InputDevice_Memcard::~InputDevice_Memcard()
{

}

void InputDevice_Memcard::Power(void)
{
 dtr = 0;

 //buttons[0] = buttons[1] = 0;

 command_phase = 0;

 bitpos = 0;

 receive_buffer = 0;

 command = 0;

 transmit_buffer = 0;

 transmit_count = 0;

 addr = 0;

 presence_new = true;
}

void InputDevice_Memcard::SetDTR(bool new_dtr)
{
 if(!dtr && new_dtr)
 {
  command_phase = 0;
  bitpos = 0;
  transmit_count = 0;
 }
 else if(dtr && !new_dtr)
 {
  if(command_phase > 0)
   PSX_WARNING("[MCR] Communication aborted???");
 }
 dtr = new_dtr;
}

bool InputDevice_Memcard::GetDSR(void)
{
 if(!dtr)
  return(0);

 if(!bitpos && transmit_count)
  return(1);

 return(0);
}

bool InputDevice_Memcard::Clock(bool TxD, int32 &dsr_pulse_delay)
{
 bool ret = 1;

 dsr_pulse_delay = 0;

 if(!dtr)
  return(1);

 if(transmit_count)
  ret = (transmit_buffer >> bitpos) & 1;

 receive_buffer &= ~(1 << bitpos);
 receive_buffer |= TxD << bitpos;
 bitpos = (bitpos + 1) & 0x7;

 if(!bitpos)
 {
  //if(command_phase > 0 || transmit_count)
  // printf("[MCRDATA] Received_data=0x%02x, Sent_data=0x%02x\n", receive_buffer, transmit_buffer);

  if(transmit_count)
  {
   transmit_count--;
  }


  switch(command_phase)
  {
   case 0:
          if(receive_buffer != 0x81)
            command_phase = -1;
          else
          {
	   //printf("[MCR] Device selected\n");
           transmit_buffer = presence_new ? 0x08 : 0x00;
           transmit_count = 1;
           command_phase++;
          }
          break;

   case 1:
        command = receive_buffer;
	//printf("[MCR] Command received: %c\n", command);
	if(command == 'R' || command == 'W')
	{
	 command_phase++;
         transmit_buffer = 0x5A;
         transmit_count = 1;
	}
	else
	{
	 if(command == 'S')
	 {
	  PSX_WARNING("[MCR] Memcard S command unsupported.");
	 }

	 command_phase = -1;
	 transmit_buffer = 0;
	 transmit_count = 0;
	}
        break;

   case 2:
	transmit_buffer = 0x5D;
	transmit_count = 1;
	command_phase++;
	break;

   case 3:
	transmit_buffer = 0x00;
	transmit_count = 1;
	if(command == 'R')
	 command_phase = 1000;
	else if(command == 'W')
	 command_phase = 2000;
	break;

  //
  // Read
  //
  case 1000:
	addr = receive_buffer << 8;
	transmit_buffer = receive_buffer;
	transmit_count = 1;
	command_phase++;
	break;

  case 1001:
	addr |= receive_buffer & 0xFF;
	transmit_buffer = '\\';
	transmit_count = 1;
	command_phase++;
	break;

  case 1002:
	//printf("[MCR]   READ ADDR=0x%04x\n", addr);
	if(addr >= (sizeof(card_data) >> 7))
	 addr = 0xFFFF;

	calced_xor = 0;
	transmit_buffer = ']';
	transmit_count = 1;
	command_phase++;

	// TODO: enable this code(or something like it) when CPU instruction timing is a bit better.
	//
	//dsr_pulse_delay = 32000;
	//goto SkipDPD;
	//

	break;

  case 1003:
	transmit_buffer = addr >> 8;
	calced_xor ^= transmit_buffer;
	transmit_count = 1;
	command_phase++;
	break;

  case 1004:
	transmit_buffer = addr & 0xFF;
	calced_xor ^= transmit_buffer;

	if(addr == 0xFFFF)
	{
	 transmit_count = 1;
	 command_phase = -1;
	}
	else
	{
	 transmit_count = 1;
	 command_phase = 1024;
	}
	break;

  // Transmit actual 128 bytes data
  //zero 07-feb-2012 - remove case range
  //case (1024 + 0) ... (1024 + 128 - 1):
  case 1024: case 1025: case 1026: case 1027: case 1028: case 1029: case 1030: case 1031: case 1032: case 1033: case 1034: case 1035: case 1036: case 1037: case 1038: case 1039: case 1040: case 1041: case 1042: case 1043: case 1044: case 1045: case 1046: case 1047: case 1048: case 1049: case 1050: case 1051: case 1052: case 1053: case 1054: case 1055: case 1056: case 1057: case 1058: case 1059: case 1060: case 1061: case 1062: case 1063: case 1064: case 1065: case 1066: case 1067: case 1068: case 1069: case 1070: case 1071: case 1072: case 1073: case 1074: case 1075: case 1076: case 1077: case 1078: case 1079: case 1080: case 1081: case 1082: case 1083: case 1084: case 1085: case 1086: case 1087: case 1088: case 1089: case 1090: case 1091: case 1092: case 1093: case 1094: case 1095: case 1096: case 1097: case 1098: case 1099: case 1100: case 1101: case 1102: case 1103: case 1104: case 1105: case 1106: case 1107: case 1108: case 1109: case 1110: case 1111: case 1112: case 1113: case 1114: case 1115: case 1116: case 1117: case 1118: case 1119: case 1120: case 1121: case 1122: case 1123: case 1124: case 1125: case 1126: case 1127: case 1128: case 1129: case 1130: case 1131: case 1132: case 1133: case 1134: case 1135: case 1136: case 1137: case 1138: case 1139: case 1140: case 1141: case 1142: case 1143: case 1144: case 1145: case 1146: case 1147: case 1148: case 1149: case 1150: case 1151: 
	transmit_buffer = card_data[(addr << 7) + (command_phase - 1024)];
	calced_xor ^= transmit_buffer;
	transmit_count = 1;
	command_phase++;
	break;

  // XOR
  case (1024 + 128):
	transmit_buffer = calced_xor;
	transmit_count = 1;
	command_phase++;
	break;

  // End flag
  case (1024 + 129):
	transmit_buffer = 'G';
	transmit_count = 1;
	command_phase = -1;
	break;

  //
  // Write
  //
  case 2000:
	calced_xor = receive_buffer;
        addr = receive_buffer << 8;
        transmit_buffer = receive_buffer;
        transmit_count = 1;
        command_phase++;
	break;

  case 2001:
	calced_xor ^= receive_buffer;
        addr |= receive_buffer & 0xFF;
	//printf("[MCR]   WRITE ADDR=0x%04x\n", addr);
        transmit_buffer = receive_buffer;
        transmit_count = 1;
        command_phase = 2048;
        break;


   //zero 07-feb-2012 - remove case range
  //case (2048 + 0) ... (2048 + 128 - 1):
  case 2048: case 2049: case 2050: case 2051: case 2052: case 2053: case 2054: case 2055: case 2056: case 2057: case 2058: case 2059: case 2060: case 2061: case 2062: case 2063: case 2064: case 2065: case 2066: case 2067: case 2068: case 2069: case 2070: case 2071: case 2072: case 2073: case 2074: case 2075: case 2076: case 2077: case 2078: case 2079: case 2080: case 2081: case 2082: case 2083: case 2084: case 2085: case 2086: case 2087: case 2088: case 2089: case 2090: case 2091: case 2092: case 2093: case 2094: case 2095: case 2096: case 2097: case 2098: case 2099: case 2100: case 2101: case 2102: case 2103: case 2104: case 2105: case 2106: case 2107: case 2108: case 2109: case 2110: case 2111: case 2112: case 2113: case 2114: case 2115: case 2116: case 2117: case 2118: case 2119: case 2120: case 2121: case 2122: case 2123: case 2124: case 2125: case 2126: case 2127: case 2128: case 2129: case 2130: case 2131: case 2132: case 2133: case 2134: case 2135: case 2136: case 2137: case 2138: case 2139: case 2140: case 2141: case 2142: case 2143: case 2144: case 2145: case 2146: case 2147: case 2148: case 2149: case 2150: case 2151: case 2152: case 2153: case 2154: case 2155: case 2156: case 2157: case 2158: case 2159: case 2160: case 2161: case 2162: case 2163: case 2164: case 2165: case 2166: case 2167: case 2168: case 2169: case 2170: case 2171: case 2172: case 2173: case 2174: case 2175: 
	calced_xor ^= receive_buffer;
	rw_buffer[command_phase - 2048] = receive_buffer;

        transmit_buffer = receive_buffer;
        transmit_count = 1;
        command_phase++;
        break;

  case (2048 + 128):	// XOR
	write_xor = receive_buffer;
	transmit_buffer = '\\';
	transmit_count = 1;
	command_phase++;
	break;

  case (2048 + 129):
	transmit_buffer = ']';
	transmit_count = 1;
	command_phase++;
	break;

  case (2048 + 130):	// End flag
	//MDFN_DispMessage("%02x %02x", calced_xor, write_xor);
	//printf("[MCR] Write End.  Actual_XOR=0x%02x, CW_XOR=0x%02x\n", calced_xor, write_xor);

	if(calced_xor != write_xor)
 	 transmit_buffer = 'N';
	else if(addr >= (sizeof(card_data) >> 7))
	 transmit_buffer = 0xFF;
	else
	{
	 transmit_buffer = 'G';
	 presence_new = false;

	 // If the current data is different from the data to be written, increment the dirty count.
	 // memcpy()'ing over to card_data is also conditionalized here for a slight optimization.
         if(memcmp(&card_data[addr << 7], rw_buffer, 128))
	 {
	  memcpy(&card_data[addr << 7], rw_buffer, 128);
	  dirty_count++;
	 }
	}

	transmit_count = 1;
	command_phase = -1;
	break;

  }

  //if(command_phase != -1 || transmit_count)
  // printf("[MCR] Receive: 0x%02x, Send: 0x%02x -- %d\n", receive_buffer, transmit_buffer, command_phase);
 }

 if(!bitpos && transmit_count)
  dsr_pulse_delay = 0x100;

 //SkipDPD: ;

 return(ret);
}

uint32 InputDevice_Memcard::GetNVSize(void)
{
 return(sizeof(card_data));
}

void InputDevice_Memcard::ReadNV(uint8 *buffer, uint32 offset, uint32 size)
{
 while(size--)
 {
  *buffer = card_data[offset & (sizeof(card_data) - 1)];
  buffer++;
  offset++;
 }
}

void InputDevice_Memcard::WriteNV(const uint8 *buffer, uint32 offset, uint32 size)
{
 if(size)
  dirty_count++;

 while(size--)
 {
  card_data[offset & (sizeof(card_data) - 1)] = *buffer;
  buffer++;
  offset++;
 }
}

uint64 InputDevice_Memcard::GetNVDirtyCount(void)
{
 return(dirty_count);
}

void InputDevice_Memcard::ResetNVDirtyCount(void)
{
 dirty_count = 0;
}


InputDevice *Device_Memcard_Create(void)
{
 return new InputDevice_Memcard();
}

}
