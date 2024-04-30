#include <cstdio>
#include <cstdint>
#include "BizhawkInterface.hxx"
#include "OSystem.hxx"
#include "Settings.hxx"
#include "MediaFactory.hxx"
#include "Serializer.hxx"
#include "StateManager.hxx"
#include "Console.hxx"
#include "Control.hxx"
#include "Switches.hxx"
#include "M6532.hxx"
#include "TIA.hxx"


ECL_ENTRY int (*load_archive_cb)(const char *filename, unsigned char *buffer, int maxsize);

int load_archive(const char *filename, unsigned char *buffer, int maxsize, char *extension)
{
	return load_archive_cb(filename, buffer, maxsize);
}

struct InitSettings
{
	uint32_t dummy;
};


ECL_EXPORT int stella_init(
	ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize),
	struct InitSettings *settings)
{
	fprintf(stderr, "Initializing Stella core...\n");

	load_archive_cb = NULL; // don't hold onto load_archive_cb for longer than we need it for

	return 1;
}

