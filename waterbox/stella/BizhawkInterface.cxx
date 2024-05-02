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


struct InitSettings
{
	uint32_t dummy;
};

std::unique_ptr<OSystem> _a2600;

ECL_EXPORT int stella_init(
	const char* romFileName,
	ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize),
	struct InitSettings *settings)
{
	fprintf(stderr, "Initializing Stella core...\n");
 
	Settings::Options opts;
	_a2600 = MediaFactory::createOSystem();
	if(!_a2600->initialize(opts)) { fprintf(stderr, "ERROR: Couldn't create A2600 System\n"); return 0; }

	const string romfile = "PRIMARY_ROM";
	const FSNode romnode(romFileName);
	 feload_archive_cb("PRIMARY_ROM", romnode.getBuffer(), BUFFER_SIZE);

	auto error = _a2600->createConsole(romnode);
	if (error != "") { fprintf(stderr, "ERROR: Couldn't create A2600 Console. Reason: '%s'\n", error.c_str()); return 0; }

 fprintf(stderr, "A2600 console created successfully");
	return 1;
}

