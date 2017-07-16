/*
(The MIT License)

Copyright (c) 2008-2016 by
David Etherton, Eric Anderton, Alec Bourque (Uze), Filipe Rinaldi,
Sandor Zsuga (Jubatian), Matt Pandina (Artcfox)
        
Permission is hereby granted, free of charge, to any person
obtaining a copy of this software and associated documentation
files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use,
copy, modify, merge, publish, distribute, sublicense, and/or
sell copies of the Software, and to permit persons to whom the
Software is furnished to do so, subject to the following
conditions:

The above copyright notice and this permission notice shall be
included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
OTHER DEALINGS IN THE SOFTWARE.
*/
#include "uzerom.h"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef unsigned char u8;
typedef signed char s8;
typedef unsigned short u16;
typedef signed short s16;
typedef unsigned long u32;

#define MAGIC_SIZE 6

const unsigned char magic[7] = "UZEBOX";

bool isUzeromFile(const char *in_filename)
{
	unsigned char test[MAGIC_SIZE];
	FILE *f = fopen(in_filename, "rb");
	if (f)
	{
		if (fread(test, 1, MAGIC_SIZE, f) != MAGIC_SIZE)
		{
			printf("Error: failed to read the file %s.\n", in_filename);
			return false;
		}

		for (int i = 0; i < MAGIC_SIZE; i++)
		{
			if (test[i] != magic[i])
				return false;
		}
		fclose(f);
		return true;
	}
	return false;
}

bool loadUzeImage(const char *in_filename, RomHeader *header, u8 *buffer)
{
	FILE *f = fopen(in_filename, "rb");
	size_t ret;
	if (f)
	{
		ret = fread(header, 1, 512, f);
		if (ret != 512)
		{
			printf("Error: failed to read the file %s.\n", in_filename);
			return false;
		}

		if (header->version != HEADER_VERSION)
		{
			printf("Error: cannot parse version %d UzeROM files.\n", header->version);
		}
		printf("%s\n", header->name);
		printf("%s\n", header->author);
		printf("%d\n", header->year);

		if (header->target == 0)
		{
			printf("Uzebox 1.0 - ATmega644\n");
		}
		else if (header->target == 1)
		{
			printf("Uzebox 2.0 - XTmega128\n");
			printf("Error: Uzebox 2.0 ROM images are not supported.\n");
			return false;
		}
		printf("\n");

		if (fread(buffer, 1, header->progSize, f) != header->progSize)
		{
			printf("Erro: failed to read the file %s.\n", in_filename);
			return false;
		}
		fclose(f);
		return true;
	}
	return false;
}

static inline int parse_hex_nibble(char s)
{
	if (s >= '0' && s <= '9')
		return s - '0';
	else if (s >= 'A' && s <= 'F')
		return s - 'A' + 10;
	else if (s >= 'a' && s <= 'a')
		return s - 'a' + 10;
	else
		return -1;
}

static int parse_hex_byte(const char *s)
{
	return (parse_hex_nibble(s[0]) << 4) | parse_hex_nibble(s[1]);
}

static int parse_hex_word(const char *s)
{
	return (parse_hex_nibble(s[0]) << 12) | (parse_hex_nibble(s[1]) << 8) |
		   (parse_hex_nibble(s[2]) << 4) | parse_hex_nibble(s[3]);
}

bool loadHex(const char *in_filename, unsigned char *buffer, unsigned int *bytesRead)
{
	(void)bytesRead;
	//http://en.wikipedia.org/wiki/.hex

	//(I've added the spaces for clarity, they don't exist in the real files)
	//:10 65B0 00 661F771F881F991F1A9469F760957095 59
	//:10 65C0 00 809590959B01AC01BD01CF010895F894 91
	//:02 65D0 00 FFCF FB
	//:02 65D2 00 0100 C6
	//:00 0000 01 FF [EOF marker]

	//First field is the byte count. Second field is the 16-bit address. Third field is the record type;
	//00 is data, 01 is EOF.	For record type zero, next "wide" field is the actual data, followed by a
	//checksum.
	u16 progmemLast = 0;
	char line[128];
	int lineNumber = 1;

	FILE *in_file = fopen(in_filename, "rb");
	if (!in_file)
		return false;

	while (fgets(line, sizeof(line), in_file) && line[0]==':')
	{
		int bytes = parse_hex_byte(line + 1);
		int addr = parse_hex_word(line + 3);
		int recordType = parse_hex_byte(line + 7);
		if (recordType == 0)
		{
			char *lp = line + 9;
			while (bytes > 0)
			{
				unsigned char value = parse_hex_byte(lp);
				buffer[addr] = value;
				addr++;
				if (addr > progmemLast)
				{
					progmemLast = addr;
				}
				lp += 2;
				bytes -= 1;
			}
		}
		else if (recordType == 1)
		{
			break;
		}
		else
			fprintf(stderr, "ignoring unknown record type %d in line %d of %s\n", recordType, lineNumber, in_filename);

		++lineNumber;
	}
	/*
    if(bytesRead != 0){
        *bytesRead=progmemLast;
    }
    */
	fclose(in_file);

	return true;
}
