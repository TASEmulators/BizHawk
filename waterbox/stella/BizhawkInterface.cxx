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

void printRAM()
{
  printf("[] Ram Pointer: %p\n", _a2600->console().riot().getRAM());
  printf("[] Memory Contents:\n");
		for (int i = 0; i < 8; i++)
		{
			for (int j = 0; j < 16; j++)
			{
				printf("%02X ", _a2600->console().riot().getRAM()[i*16 + j]);
			}
			printf("\n");
		}
}

ECL_EXPORT int stella_init(
	const char* romFileName,
	ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize),
	struct InitSettings *settings)
{
	fprintf(stderr, "Initializing Stella core...\n");
 
	Settings::Options opts;
	_a2600 = MediaFactory::createOSystem();
	if(!_a2600->initialize(opts)) { fprintf(stderr, "ERROR: Couldn't create A2600 System\n"); return 0; }

	uint8_t* buf = (uint8_t*) calloc(1, BUFFER_SIZE);
	int size = feload_archive_cb("PRIMARY_ROM", buf, BUFFER_SIZE);
	const FSNode romnode(romFileName, buf, size);
	printf("Romnode buffer: %p\n", romnode._buffer);

	printf("***** Creating console\n"); fflush(stdout);

	auto error = _a2600->createConsole(romnode);
	if (error != "") { fprintf(stderr, "ERROR: Couldn't create A2600 Console. Reason: '%s'\n", error.c_str()); return 0; }

 printf("A2600 console created successfully");

 printf("Before Advance");
 printRAM();
 
 _a2600->dispatchEmulation();

	printf("After Advance");
 printRAM();

	return 1;
}

