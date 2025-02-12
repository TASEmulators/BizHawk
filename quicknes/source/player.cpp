#include "argparse/argparse.hpp"
#include "jaffarCommon/deserializers/contiguous.hpp"
#include "jaffarCommon/file.hpp"
#include "jaffarCommon/logger.hpp"
#include "jaffarCommon/serializers/contiguous.hpp"
#include "jaffarCommon/string.hpp"
#include "nesInstance.hpp"
#include "playbackInstance.hpp"
#include <cstdlib>

SDL_Window *launchOutputWindow()
{
  // Opening rendering window
  SDL_SetMainReady();

  // We can only call SDL_InitSubSystem once
  if (!SDL_WasInit(SDL_INIT_VIDEO))
    if (SDL_InitSubSystem(SDL_INIT_VIDEO) != 0) JAFFAR_THROW_LOGIC("Failed to initialize video: %s", SDL_GetError());

  auto window = SDL_CreateWindow("JaffarPlus", SDL_WINDOWPOS_UNDEFINED, SDL_WINDOWPOS_UNDEFINED, 100, 100, SDL_WINDOW_RESIZABLE);
  if (window == nullptr) JAFFAR_THROW_LOGIC("Coult not open SDL window");

  return window;
}

void closeOutputWindow(SDL_Window *window) { SDL_DestroyWindow(window); }

int main(int argc, char *argv[])
{
  // Parsing command line arguments
  argparse::ArgumentParser program("player", "1.0");

  program.add_argument("romFile")
    .help("Path to the rom file to run.")
    .required();

  program.add_argument("sequenceFile")
    .help("Path to the input sequence file (.sol) to reproduce.")
    .required();

  program.add_argument("stateFile")
    .help("(Optional) Path to the initial state file to load.")
    .default_value(std::string(""));

  program.add_argument("--reproduce")
    .help("Plays the entire sequence without interruptions and exit at the end.")
    .default_value(false)
    .implicit_value(true);

  program.add_argument("--disableRender")
    .help("Do not render game window.")
    .default_value(false)
    .implicit_value(true);

  program.add_argument("--controller1")
    .help("Specifies the controller 1 type.")
    .default_value(std::string("Joypad"));

  program.add_argument("--controller2")
    .help("Specifies the controller 2 type.")
    .default_value(std::string("None"));

  // Try to parse arguments
  try
  {
    program.parse_args(argc, argv);
  }
  catch (const std::runtime_error &err)
  {
    JAFFAR_THROW_LOGIC("%s\n%s", err.what(), program.help().str().c_str());
  }

  // Getting ROM file path
  std::string romFilePath = program.get<std::string>("romFile");

  // Getting sequence file path
  std::string sequenceFilePath = program.get<std::string>("sequenceFile");

  // If initial state file is specified, load it
  std::string stateFilePath = program.get<std::string>("stateFile");

  // Getting reproduce flag
  bool isReproduce = program.get<bool>("--reproduce");

  // Getting reproduce flag
  bool disableRender = program.get<bool>("--disableRender");

  // Getting controller 1 Type
  std::string controller1Type = program.get<std::string>("--controller1");

  // Getting controller 2 Type
  std::string controller2Type = program.get<std::string>("--controller2");

  // Loading sequence file
  std::string inputSequence;
  auto status = jaffarCommon::file::loadStringFromFile(inputSequence, sequenceFilePath.c_str());
  if (status == false) JAFFAR_THROW_LOGIC("[ERROR] Could not find or read from sequence file: %s\n", sequenceFilePath.c_str());

  // Building sequence information
  const auto sequence = jaffarCommon::string::split(inputSequence, '\n');

  // Initializing terminal
  jaffarCommon::logger::initializeTerminal();

  // Printing provided parameters
  jaffarCommon::logger::log("[] Rom File Path:      '%s'\n", romFilePath.c_str());
  jaffarCommon::logger::log("[] Sequence File Path: '%s'\n", sequenceFilePath.c_str());
  jaffarCommon::logger::log("[] Sequence Length:    %lu\n", sequence.size());
  jaffarCommon::logger::log("[] State File Path:    '%s'\n", stateFilePath.empty() ? "<Boot Start>" : stateFilePath.c_str());
  jaffarCommon::logger::log("[] Generating Sequence...\n");

  jaffarCommon::logger::refreshTerminal();

  // Creating emulator instance
  nlohmann::json emulatorConfig;
  emulatorConfig["Controller 1 Type"] = controller1Type;
  emulatorConfig["Controller 2 Type"] = controller2Type;
  NESInstance e(emulatorConfig);

  // Loading ROM File
  std::string romFileData;
  if (jaffarCommon::file::loadStringFromFile(romFileData, romFilePath) == false) JAFFAR_THROW_LOGIC("Could not rom file: %s\n", romFilePath.c_str());
  e.loadROM((uint8_t *)romFileData.data(), romFileData.size());

  // If an initial state is provided, load it now
  if (stateFilePath != "")
  {
    std::string stateFileData;
    if (jaffarCommon::file::loadStringFromFile(stateFileData, stateFilePath) == false) JAFFAR_THROW_LOGIC("Could not initial state file: %s\n", stateFilePath.c_str());
    jaffarCommon::deserializer::Contiguous deserializer(stateFileData.data());
    e.deserializeState(deserializer);
  }

  // Creating playback instance
  auto p = PlaybackInstance(&e);

  // If render is enabled then, create window now
  SDL_Window *window = nullptr;
  if (disableRender == false)
  {
    window = launchOutputWindow();
    p.enableRendering(window);
  }

  // Initializing playback instance
  p.initialize(sequence);

  // Getting state size
  auto stateSize = e.getFullStateSize();

  // Flag to continue running playback
  bool continueRunning = true;

  // Variable for current step in view
  ssize_t sequenceLength = p.getSequenceLength();
  ssize_t currentStep = 0;

  // Flag to display frame information
  bool showFrameInfo = true;

  // Interactive section
  while (continueRunning)
  {
    // Updating display
    if (disableRender == false) p.renderFrame(currentStep);

    // Getting input
    const auto &inputString = p.getInputString(currentStep);

    // Getting state hash
    const auto hash = p.getStateHash(currentStep);

    // Getting state data
    const auto stateData = p.getStateData(currentStep);

    // Printing data and commands
    if (showFrameInfo)
    {
      jaffarCommon::logger::clearTerminal();

      jaffarCommon::logger::log("[] ----------------------------------------------------------------\n");
      jaffarCommon::logger::log("[] Current Step #: %lu / %lu\n", currentStep + 1, sequenceLength);
      jaffarCommon::logger::log("[] Input:          %s\n", inputString.c_str());
      jaffarCommon::logger::log("[] State Hash:     0x%lX%lX\n", hash.first, hash.second);
      jaffarCommon::logger::log("[] Paddle X:       %u\n", e.getLowMem()[0x11A]);

      // Only print commands if not in reproduce mode
      if (isReproduce == false) jaffarCommon::logger::log("[] Commands: n: -1 m: +1 | h: -10 | j: +10 | y: -100 | u: +100 | k: -1000 | i: +1000 | s: quicksave | p: play | q: quit\n");

      jaffarCommon::logger::refreshTerminal();
    }

    // Resetting show frame info flag
    showFrameInfo = true;

    // Get command
    auto command = jaffarCommon::logger::waitForKeyPress();

    // Advance/Rewind commands
    if (command == 'n') currentStep = currentStep - 1;
    if (command == 'm') currentStep = currentStep + 1;
    if (command == 'h') currentStep = currentStep - 10;
    if (command == 'j') currentStep = currentStep + 10;
    if (command == 'y') currentStep = currentStep - 100;
    if (command == 'u') currentStep = currentStep + 100;
    if (command == 'k') currentStep = currentStep - 1000;
    if (command == 'i') currentStep = currentStep + 1000;

    // Correct current step if requested more than possible
    if (currentStep < 0) currentStep = 0;
    if (currentStep >= sequenceLength) currentStep = sequenceLength - 1;

    // Quicksave creation command
    if (command == 's')
    {
      // Storing state file
      std::string saveFileName = "quicksave.state";

      std::string saveData;
      saveData.resize(stateSize);
      memcpy(saveData.data(), stateData, stateSize);
      if (jaffarCommon::file::saveStringToFile(saveData, saveFileName.c_str()) == false) JAFFAR_THROW_LOGIC("[ERROR] Could not save state file: %s\n", saveFileName.c_str());
      jaffarCommon::logger::log("[] Saved state to %s\n", saveFileName.c_str());

      // Do no show frame info again after this action
      showFrameInfo = false;
    }

    // Start playback from current point
    if (command == 'p') isReproduce = true;

    // Start playback from current point
    if (command == 'q') continueRunning = false;
  }

  // If render is enabled then, close window now
  if (disableRender == false) closeOutputWindow(window);

  // Ending ncurses window
  jaffarCommon::logger::finalizeTerminal();
}
