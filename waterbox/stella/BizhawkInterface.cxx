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

uint16_t soundbuffer[4096];
int nsamples;

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

enum regionType
{
   ntsc = 0,
   pal = 1,
			secam = 2
};

ECL_EXPORT int stella_get_region()
{
	const auto regionString = _a2600->console().getFormatString();

		if (regionString == "NTSC" || regionString == "NTSC50") return regionType::ntsc;
		if (regionString == "PAL" || regionString == "PAL60") return regionType::pal;
		if (regionString == "SECAM" || regionString == "SECAM60") return regionType::secam;

 	return -1;
}

ECL_EXPORT void stella_get_frame_rate(int& fps)
{
	 fps = _a2600->console().gameRefreshRate();
}

void printFrameBuffer()
{
	 auto frameBuffer = _a2600->console().tia().frameBuffer();
		auto height =   _a2600->console().tia().height();
		auto width =   _a2600->console().tia().width();

  printf("[] Frame Buffer Pointer: %p\n", frameBuffer);
  // printf("[] Frame Buffer Contents:\n");

		uint64_t checkSum = 0;
		for (int i = 0; i < height; i++)
		{
			for (int j = 0; j < width; j++)
			{
				// printf("%02X ", frameBuffer[i*height + j]);
				checkSum += frameBuffer[i*height + j];
			}
			// printf("\n");
		}

		printf("[] Frame Buffer Checksum: 0x%lX\n", checkSum);
}


ECL_EXPORT void stella_get_audio(int *n, void **buffer)
{
	if (n)
		*n = nsamples;
	if (buffer)
		*buffer = soundbuffer;
}

ECL_EXPORT void stella_get_video(int& w, int& h, int& pitch, uint8_t*& buffer)
{
	 w = _a2600->console().tia().width();
		h = _a2600->console().tia().height();
		buffer = _a2600->console().tia().frameBuffer();
	 pitch =	_a2600->console().tia().width();
}

ECL_EXPORT void stella_frame_advance(uint8_t port1, uint8_t port2, bool reset, bool power, bool leftDiffToggled, bool rightDiffToggled)
{
				_a2600->console().switches().setLeftDifficultyA(leftDiffToggled);
    _a2600->console().switches().setRightDifficultyA(rightDiffToggled);
				_a2600->console().switches().setReset(!reset);
    if (power) _a2600->console().system().reset(true);

				_a2600->console().leftController().write(::Controller::DigitalPin::One,   port1 & 0b00010000);  // Up
				_a2600->console().leftController().write(::Controller::DigitalPin::Two,   port1 & 0b00100000);  // Down
				_a2600->console().leftController().write(::Controller::DigitalPin::Three, port1 & 0b01000000);  // Left
				_a2600->console().leftController().write(::Controller::DigitalPin::Four,  port1 & 0b10000000);  // Right
				_a2600->console().leftController().write(::Controller::DigitalPin::Six,   port1 & 0b00001000);  // Button

				_a2600->console().rightController().write(::Controller::DigitalPin::One,   port2 & 0b00010000);  // Up
				_a2600->console().rightController().write(::Controller::DigitalPin::Two,   port2 & 0b00100000);  // Down
				_a2600->console().rightController().write(::Controller::DigitalPin::Three, port2 & 0b01000000);  // Left
				_a2600->console().rightController().write(::Controller::DigitalPin::Four,  port2 & 0b10000000);  // Right
				_a2600->console().rightController().write(::Controller::DigitalPin::Six,   port2 & 0b00001000);  // Button

				nsamples = 0;
    _a2600->dispatchEmulation();
				//  printRAM();
				// printFrameBuffer();
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

	return 1;
}

