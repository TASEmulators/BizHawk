/****************************************************************************
 *  Genesis Plus
 *  SPI Serial EEPROM (25XX512 only) support
 *
 *  Copyright (C) 2012  Eke-Eke (Genesis Plus GX)
 *
 *  Redistribution and use of this code or any derivative works are permitted
 *  provided that the following conditions are met:
 *
 *   - Redistributions may not be sold, nor may they be used in a commercial
 *     product or activity.
 *
 *   - Redistributions that are modified from the original source must include the
 *     complete source code, including the source code for all components used by a
 *     binary built from the modified sources. However, as a special exception, the
 *     source code distributed need not include anything that is normally distributed
 *     (in either source or binary form) with the major components (compiler, kernel,
 *     and so on) of the operating system on which the executable runs, unless that
 *     component itself accompanies the executable.
 *
 *   - Redistributions must reproduce the above copyright notice, this list of
 *     conditions and the following disclaimer in the documentation and/or other
 *     materials provided with the distribution.
 *
 *  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 *  AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 *  IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 *  ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
 *  LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 *  CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 *  SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 *  INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 *  CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 *  ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 *  POSSIBILITY OF SUCH DAMAGE.
 *
 ****************************************************************************************/

#ifndef _EEPROM_SPI_H_
#define _EEPROM_SPI_H_

typedef enum
{
  STANDBY,
  GET_OPCODE,
  GET_ADDRESS,
  WRITE_BYTE,
  READ_BYTE
} T_STATE_SPI;

typedef struct
{
  uint8 cs;           /* !CS line state */
  uint8 clk;          /* SCLK line state */
  uint8 out;          /* SO line state */
  uint8 status;       /* status register */
  uint8 opcode;       /* 8-bit opcode */
  uint8 buffer;       /* 8-bit data buffer */
  uint16 addr;        /* 16-bit address */
  uint32 cycles;      /* current operation cycle */
  T_STATE_SPI state;  /* current operation state */
} T_EEPROM_SPI;

extern T_EEPROM_SPI spi_eeprom;

/* Function prototypes */
extern void eeprom_spi_init();
extern void eeprom_spi_write(unsigned char data);
extern unsigned int eeprom_spi_read(unsigned int address);

#endif
