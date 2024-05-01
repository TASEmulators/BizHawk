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

std::unique_ptr<OSystem> _a2600;

ECL_EXPORT int stella_init(
	ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize),
	struct InitSettings *settings)
{
	fprintf(stderr, "Initializing Stella core...\n");

	load_archive_cb = NULL; // don't hold onto load_archive_cb for longer than we need it for

	Settings::Options opts;
	_a2600 = MediaFactory::createOSystem();
	if(!_a2600->initialize(opts)) { fprintf(stderr, "ERROR: Couldn't create A2600 System\n"); return 0; }

	const string romfile = "PRIMARY_ROM";
	const FSNode romnode(romfile);

	auto error = _a2600->createConsole(romnode);
	if (error != "") { fprintf(stderr, "ERROR: Couldn't create A2600 Console. Reason: '%s'\n", error.c_str()); return 0; }

 fprintf(stderr, "A2600 console created successfully");
	return 1;
}

