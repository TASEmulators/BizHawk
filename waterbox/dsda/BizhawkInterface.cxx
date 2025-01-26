#include <cstdio>
#include <cstdint>
#include "BizhawkInterface.hxx"

struct InitSettings
{
	uint32_t dummy;
};

ECL_EXPORT void dsda_get_audio(int *n, void **buffer)
{
	*n = 0;
	*buffer = nullptr;
}

ECL_EXPORT void dsda_get_video(int& w, int& h, int& pitch, uint8_t*& buffer)
{
}


ECL_EXPORT void dsda_frame_advance()
{
}

ECL_ENTRY void (*input_callback_cb)(void);

void real_input_callback(void)
{
	if (input_callback_cb)
		input_callback_cb();
}

ECL_EXPORT void dsda_set_input_callback(ECL_ENTRY void (*fecb)(void))
{
	input_callback_cb = fecb;
}


ECL_EXPORT int dsda_init(
	const char* wadFileName,
	ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize),
	struct InitSettings *settings)
{

	return 1;
}

