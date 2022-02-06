/* Copyright (c) 2021 Themaister
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#pragma once

#include <stdint.h>
#include <stdio.h>
#include <vector>

namespace RDP
{
class RDPDumpWriter
{
public:
	~RDPDumpWriter();
	bool init(const char *path, uint32_t dram_size, uint32_t hidden_dram_size);
	void flush_dram(const void *dram, uint32_t size);
	void flush_hidden_dram(const void *dram, uint32_t size);
	void signal_complete();
	void emit_command(uint32_t command, const uint32_t *cmd_data, uint32_t cmd_words);
	void set_vi_register(uint32_t vi_register, uint32_t value);
	void end_frame();

private:
	enum RDPDumpCmd : uint32_t
	{
		RDP_DUMP_CMD_INVALID = 0,
		RDP_DUMP_CMD_UPDATE_DRAM = 1,
		RDP_DUMP_CMD_RDP_COMMAND = 2,
		RDP_DUMP_CMD_SET_VI_REGISTER = 3,
		RDP_DUMP_CMD_END_FRAME = 4,
		RDP_DUMP_CMD_SIGNAL_COMPLETE = 5,
		RDP_DUMP_CMD_EOF = 6,
		RDP_DUMP_CMD_UPDATE_DRAM_FLUSH = 7,
		RDP_DUMP_CMD_UPDATE_HIDDEN_DRAM = 8,
		RDP_DUMP_CMD_UPDATE_HIDDEN_DRAM_FLUSH = 9,
		RDP_DUMP_CMD_INT_MAX = 0x7fffffff
	};

	FILE *file = nullptr;
	std::vector<uint8_t> rdp_dram_cache;
	std::vector<uint8_t> rdp_hidden_dram_cache;
	void flush(const void *dram_, uint32_t size, RDPDumpCmd block_cmd, RDPDumpCmd flush_cmd, uint8_t *cache);
	void end();
};
}
