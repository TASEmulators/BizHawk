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

#include "rdp_dump_write.hpp"
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

namespace RDP
{
RDPDumpWriter::~RDPDumpWriter()
{
	end();
	if (file)
		fclose(file);
}

bool RDPDumpWriter::init(const char *path, uint32_t dram_size, uint32_t hidden_dram_size)
{
	if (file)
		return false;

	rdp_dram_cache.clear();
	rdp_dram_cache.resize(dram_size);
	rdp_hidden_dram_cache.clear();
	rdp_hidden_dram_cache.resize(hidden_dram_size);

	file = fopen(path, "wb");
	if (!file)
		return false;

	fwrite("RDPDUMP2", 8, 1, file);
	fwrite(&dram_size, sizeof(dram_size), 1, file);
	fwrite(&hidden_dram_size, sizeof(hidden_dram_size), 1, file);
	return true;
}

void RDPDumpWriter::end_frame()
{
	if (!file)
		return;

	uint32_t cmd = RDP_DUMP_CMD_END_FRAME;
	fwrite(&cmd, sizeof(cmd), 1, file);
}

void RDPDumpWriter::end()
{
	if (!file)
		return;

	uint32_t cmd = RDP_DUMP_CMD_EOF;
	fwrite(&cmd, sizeof(cmd), 1, file);

	fclose(file);
	file = nullptr;

	rdp_dram_cache.clear();
	rdp_hidden_dram_cache.clear();
}

void RDPDumpWriter::flush(const void *dram_, uint32_t size,
                          RDPDumpCmd block_cmd, RDPDumpCmd flush_cmd,
                          uint8_t *cache)
{
	if (!file)
		return;

	const auto *dram = static_cast<const uint8_t *>(dram_);
	const uint32_t block_size = 4 * 1024;
	uint32_t i = 0;

	for (i = 0; i < size; i += block_size)
	{
		if (memcmp(dram + i, cache + i, block_size) != 0)
		{
			uint32_t cmd = block_cmd;
			fwrite(&cmd, sizeof(cmd), 1, file);
			fwrite(&i, sizeof(i), 1, file);
			fwrite(&block_size, sizeof(block_size), 1, file);
			fwrite(dram + i, 1, block_size, file);
			memcpy(cache + i, dram + i, block_size);
		}
	}

	uint32_t cmd = flush_cmd;
	fwrite(&cmd, sizeof(cmd), 1, file);

}

void RDPDumpWriter::flush_dram(const void *dram_, uint32_t size)
{
	flush(dram_, size, RDP_DUMP_CMD_UPDATE_DRAM, RDP_DUMP_CMD_UPDATE_DRAM_FLUSH, rdp_dram_cache.data());
}

void RDPDumpWriter::flush_hidden_dram(const void *dram_, uint32_t size)
{
	flush(dram_, size, RDP_DUMP_CMD_UPDATE_HIDDEN_DRAM, RDP_DUMP_CMD_UPDATE_HIDDEN_DRAM_FLUSH, rdp_hidden_dram_cache.data());
}

void RDPDumpWriter::signal_complete()
{
	if (!file)
		return;

	uint32_t cmd = RDP_DUMP_CMD_SIGNAL_COMPLETE;
	fwrite(&cmd, sizeof(cmd), 1, file);
}

void RDPDumpWriter::emit_command(uint32_t command, const uint32_t *cmd_data, uint32_t cmd_words)
{
	if (!file)
		return;

	uint32_t cmd = RDP_DUMP_CMD_RDP_COMMAND;
	fwrite(&cmd, sizeof(cmd), 1, file);
	fwrite(&command, sizeof(command), 1, file);
	fwrite(&cmd_words, sizeof(cmd_words), 1, file);
	fwrite(cmd_data, sizeof(*cmd_data), cmd_words, file);
}

void RDPDumpWriter::set_vi_register(uint32_t vi_register, uint32_t value)
{
	if (!file)
		return;

	uint32_t cmd = RDP_DUMP_CMD_SET_VI_REGISTER;
	fwrite(&cmd, sizeof(cmd), 1, file);
	fwrite(&vi_register, sizeof(vi_register), 1, file);
	fwrite(&value, sizeof(value), 1, file);
}
}
