#include "nesInstance.hpp"
#include <argparse/argparse.hpp>
#include <chrono>
#include <jaffarCommon/deserializers/contiguous.hpp>
#include <jaffarCommon/deserializers/differential.hpp>
#include <jaffarCommon/file.hpp>
#include <jaffarCommon/hash.hpp>
#include <jaffarCommon/json.hpp>
#include <jaffarCommon/serializers/contiguous.hpp>
#include <jaffarCommon/serializers/differential.hpp>
#include <jaffarCommon/string.hpp>
#include <sstream>
#include <string>
#include <vector>

int main(int argc, char *argv[])
{
  // Parsing command line arguments
  argparse::ArgumentParser program("tester", "1.0");

  program.add_argument("scriptFile")
    .help("Path to the test script file to run.")
    .required();

  program.add_argument("--cycleType")
    .help("Specifies the emulation actions to be performed per each input. Possible values: 'Simple': performs only advance state, 'Rerecord': performs load/advance/save, and 'Full': performs load/advance/save/advance.")
    .default_value(std::string("Simple"));

  program.add_argument("--hashOutputFile")
    .help("Path to write the hash output to.")
    .default_value(std::string(""));

  // Try to parse arguments
  try
  {
    program.parse_args(argc, argv);
  }
  catch (const std::runtime_error &err)
  {
    JAFFAR_THROW_LOGIC("%s\n%s", err.what(), program.help().str().c_str());
  }

  // Getting test script file path
  std::string scriptFilePath = program.get<std::string>("scriptFile");

  // Getting path where to save the hash output (if any)
  std::string hashOutputFile = program.get<std::string>("--hashOutputFile");

  // Getting reproduce flag
  std::string cycleType = program.get<std::string>("--cycleType");

  // Loading script file
  std::string scriptJsonRaw;
  if (jaffarCommon::file::loadStringFromFile(scriptJsonRaw, scriptFilePath) == false) JAFFAR_THROW_LOGIC("Could not find/read script file: %s\n", scriptFilePath.c_str());

  // Parsing script
  const auto scriptJson = nlohmann::json::parse(scriptJsonRaw);

  // Getting rom file path
  if (scriptJson.contains("Rom File") == false) JAFFAR_THROW_LOGIC("Script file missing 'Rom File' entry\n");
  if (scriptJson["Rom File"].is_string() == false) JAFFAR_THROW_LOGIC("Script file 'Rom File' entry is not a string\n");
  std::string romFilePath = scriptJson["Rom File"].get<std::string>();

  // Getting initial state file path
  if (scriptJson.contains("Initial State File") == false) JAFFAR_THROW_LOGIC("Script file missing 'Initial State File' entry\n");
  if (scriptJson["Initial State File"].is_string() == false) JAFFAR_THROW_LOGIC("Script file 'Initial State File' entry is not a string\n");
  std::string initialStateFilePath = scriptJson["Initial State File"].get<std::string>();

  // Getting sequence file path
  if (scriptJson.contains("Sequence File") == false) JAFFAR_THROW_LOGIC("Script file missing 'Sequence File' entry\n");
  if (scriptJson["Sequence File"].is_string() == false) JAFFAR_THROW_LOGIC("Script file 'Sequence File' entry is not a string\n");
  std::string sequenceFilePath = scriptJson["Sequence File"].get<std::string>();

  // Getting expected ROM SHA1 hash
  if (scriptJson.contains("Expected ROM SHA1") == false) JAFFAR_THROW_LOGIC("Script file missing 'Expected ROM SHA1' entry\n");
  if (scriptJson["Expected ROM SHA1"].is_string() == false) JAFFAR_THROW_LOGIC("Script file 'Expected ROM SHA1' entry is not a string\n");
  std::string expectedROMSHA1 = scriptJson["Expected ROM SHA1"].get<std::string>();

  // Parsing disabled blocks in lite state serialization
  std::vector<std::string> stateDisabledBlocks;
  std::string stateDisabledBlocksOutput;
  if (scriptJson.contains("Disable State Blocks") == false) JAFFAR_THROW_LOGIC("Script file missing 'Disable State Blocks' entry\n");
  if (scriptJson["Disable State Blocks"].is_array() == false) JAFFAR_THROW_LOGIC("Script file 'Disable State Blocks' is not an array\n");
  for (const auto &entry : scriptJson["Disable State Blocks"])
  {
    if (entry.is_string() == false) JAFFAR_THROW_LOGIC("Script file 'Disable State Blocks' entry is not a string\n");
    stateDisabledBlocks.push_back(entry.get<std::string>());
    stateDisabledBlocksOutput += entry.get<std::string>() + std::string(" ");
  }

  // Getting Controller 1 type
  if (scriptJson.contains("Controller 1 Type") == false) JAFFAR_THROW_LOGIC("Script file missing 'Controller 1 Type' entry\n");
  if (scriptJson["Controller 1 Type"].is_string() == false) JAFFAR_THROW_LOGIC("Script file 'Controller 1 Type' entry is not a string\n");
  std::string controller1Type = scriptJson["Controller 1 Type"].get<std::string>();

  // Getting Controller 2 type
  if (scriptJson.contains("Controller 2 Type") == false) JAFFAR_THROW_LOGIC("Script file missing 'Controller 2 Type' entry\n");
  if (scriptJson["Controller 2 Type"].is_string() == false) JAFFAR_THROW_LOGIC("Script file 'Controller 2 Type' entry is not a string\n");
  std::string controller2Type = scriptJson["Controller 2 Type"].get<std::string>();

  // Getting differential compression configuration
  if (scriptJson.contains("Differential Compression") == false) JAFFAR_THROW_LOGIC("Script file missing 'Differential Compression' entry\n");
  if (scriptJson["Differential Compression"].is_object() == false) JAFFAR_THROW_LOGIC("Script file 'Differential Compression' entry is not a key/value object\n");
  const auto &differentialCompressionJs = scriptJson["Differential Compression"];

  if (differentialCompressionJs.contains("Enabled") == false) JAFFAR_THROW_LOGIC("Script file missing 'Differential Compression / Enabled' entry\n");
  if (differentialCompressionJs["Enabled"].is_boolean() == false) JAFFAR_THROW_LOGIC("Script file 'Differential Compression / Enabled' entry is not a boolean\n");
  const auto differentialCompressionEnabled = differentialCompressionJs["Enabled"].get<bool>();

  if (differentialCompressionJs.contains("Max Differences") == false) JAFFAR_THROW_LOGIC("Script file missing 'Differential Compression / Max Differences' entry\n");
  if (differentialCompressionJs["Max Differences"].is_number() == false) JAFFAR_THROW_LOGIC("Script file 'Differential Compression / Max Differences' entry is not a number\n");
  const auto differentialCompressionMaxDifferences = differentialCompressionJs["Max Differences"].get<size_t>();

  if (differentialCompressionJs.contains("Use Zlib") == false) JAFFAR_THROW_LOGIC("Script file missing 'Differential Compression / Use Zlib' entry\n");
  if (differentialCompressionJs["Use Zlib"].is_boolean() == false) JAFFAR_THROW_LOGIC("Script file 'Differential Compression / Use Zlib' entry is not a boolean\n");
  const auto differentialCompressionUseZlib = differentialCompressionJs["Use Zlib"].get<bool>();

  // Creating emulator instance
  NESInstance e(scriptJson);

  // Loading ROM File
  std::string romFileData;
  if (jaffarCommon::file::loadStringFromFile(romFileData, romFilePath) == false) JAFFAR_THROW_LOGIC("Could not rom file: %s\n", romFilePath.c_str());
  e.loadROM((uint8_t *)romFileData.data(), romFileData.size());

  // Calculating ROM SHA1
  auto romSHA1 = jaffarCommon::hash::getSHA1String(romFileData);

  // If an initial state is provided, load it now
  if (initialStateFilePath != "")
  {
    std::string stateFileData;
    if (jaffarCommon::file::loadStringFromFile(stateFileData, initialStateFilePath) == false) JAFFAR_THROW_LOGIC("Could not initial state file: %s\n", initialStateFilePath.c_str());
    jaffarCommon::deserializer::Contiguous d(stateFileData.data());
    e.deserializeState(d);
  }

  // Disabling requested blocks from state serialization
  for (const auto &block : stateDisabledBlocks) e.disableStateBlock(block);

  // Disable rendering
  e.disableRendering();

  // Getting full state size
  const auto stateSize = e.getFullStateSize();

  // Getting differential state size
  const auto fixedDiferentialStateSize = e.getDifferentialStateSize();
  const auto fullDifferentialStateSize = fixedDiferentialStateSize + differentialCompressionMaxDifferences;

  // Checking with the expected SHA1 hash
  if (romSHA1 != expectedROMSHA1) JAFFAR_THROW_LOGIC("Wrong ROM SHA1. Found: '%s', Expected: '%s'\n", romSHA1.c_str(), expectedROMSHA1.c_str());

  // Loading sequence file
  std::string sequenceRaw;
  if (jaffarCommon::file::loadStringFromFile(sequenceRaw, sequenceFilePath) == false) JAFFAR_THROW_LOGIC("[ERROR] Could not find or read from input sequence file: %s\n", sequenceFilePath.c_str());

  // Building sequence information
  const auto sequence = jaffarCommon::string::split(sequenceRaw, '\n');

  // Getting sequence lenght
  const auto sequenceLength = sequence.size();

  // Getting input parser from the emulator
  const auto inputParser = e.getInputParser();

  // Getting decoded emulator input for each entry in the sequence
  std::vector<jaffar::input_t> decodedSequence;
  for (const auto &inputString : sequence) decodedSequence.push_back(inputParser->parseInputString(inputString));

  // Getting emulation core name
  std::string emulationCoreName = e.getCoreName();

  // Printing test information
  printf("[] -----------------------------------------\n");
  printf("[] Running Script:                         '%s'\n", scriptFilePath.c_str());
  printf("[] Cycle Type:                             '%s'\n", cycleType.c_str());
  printf("[] Emulation Core:                         '%s'\n", emulationCoreName.c_str());
  printf("[] ROM File:                               '%s'\n", romFilePath.c_str());
  printf("[] ROM Hash:                               'SHA1: %s'\n", romSHA1.c_str());
  printf("[] Sequence File:                          '%s'\n", sequenceFilePath.c_str());
  printf("[] Sequence Length:                        %lu\n", sequenceLength);
  printf("[] State Size:                             %lu bytes - Disabled Blocks:  [ %s ]\n", stateSize, stateDisabledBlocksOutput.c_str());
  printf("[] Use Differential Compression:           %s\n", differentialCompressionEnabled ? "true" : "false");
  if (differentialCompressionEnabled == true)
  {
    printf("[]   + Max Differences:                    %lu\n", differentialCompressionMaxDifferences);
    printf("[]   + Use Zlib:                           %s\n", differentialCompressionUseZlib ? "true" : "false");
    printf("[]   + Fixed Diff State Size:              %lu\n", fixedDiferentialStateSize);
    printf("[]   + Full Diff State Size:               %lu\n", fullDifferentialStateSize);
  }
  printf("[] ********** Running Test **********\n");

  fflush(stdout);

  // Serializing initial state
  auto currentState = (uint8_t *)malloc(stateSize);
  {
    jaffarCommon::serializer::Contiguous cs(currentState);
    e.serializeState(cs);
  }

  // Serializing differential state data (in case it's used)
  uint8_t *differentialStateData = nullptr;
  size_t differentialStateMaxSizeDetected = 0;

  // Allocating memory for differential data and performing the first serialization
  if (differentialCompressionEnabled == true)
  {
    differentialStateData = (uint8_t *)malloc(fullDifferentialStateSize);
    auto s = jaffarCommon::serializer::Differential(differentialStateData, fullDifferentialStateSize, currentState, stateSize, differentialCompressionUseZlib);
    e.serializeState(s);
    differentialStateMaxSizeDetected = s.getOutputSize();
  }

  // Check whether to perform each action
  bool doPreAdvance = cycleType == "Full";
  bool doDeserialize = cycleType == "Rerecord" || cycleType == "Full";
  bool doSerialize = cycleType == "Rerecord" || cycleType == "Full";

  // Actually running the sequence
  auto t0 = std::chrono::high_resolution_clock::now();
  for (const auto &input : decodedSequence)
  {
    if (doPreAdvance == true) e.advanceState(input);

    if (doDeserialize == true)
    {
      if (differentialCompressionEnabled == true)
      {
        jaffarCommon::deserializer::Differential d(differentialStateData, fullDifferentialStateSize, currentState, stateSize, differentialCompressionUseZlib);
        e.deserializeState(d);
      }

      if (differentialCompressionEnabled == false)
      {
        jaffarCommon::deserializer::Contiguous d(currentState, stateSize);
        e.deserializeState(d);
      }
    }

    e.advanceState(input);

    if (doSerialize == true)
    {
      if (differentialCompressionEnabled == true)
      {
        auto s = jaffarCommon::serializer::Differential(differentialStateData, fullDifferentialStateSize, currentState, stateSize, differentialCompressionUseZlib);
        e.serializeState(s);
        differentialStateMaxSizeDetected = std::max(differentialStateMaxSizeDetected, s.getOutputSize());
      }

      if (differentialCompressionEnabled == false)
      {
        auto s = jaffarCommon::serializer::Contiguous(currentState, stateSize);
        e.serializeState(s);
      }
    }
  }
  auto tf = std::chrono::high_resolution_clock::now();

  // Calculating running time
  auto dt = std::chrono::duration_cast<std::chrono::nanoseconds>(tf - t0).count();
  double elapsedTimeSeconds = (double)dt * 1.0e-9;

  // Calculating final state hash
  auto result = jaffarCommon::hash::calculateMetroHash(e.getLowMem(), e.getLowMemSize());

  // Creating hash string
  char hashStringBuffer[256];
  sprintf(hashStringBuffer, "0x%lX%lX", result.first, result.second);

  // Printing time information
  printf("[] Elapsed time:                           %3.3fs\n", (double)dt * 1.0e-9);
  printf("[] Performance:                            %.3f inputs / s\n", (double)sequenceLength / elapsedTimeSeconds);
  printf("[] Final State Hash:                       %s\n", hashStringBuffer);
  if (differentialCompressionEnabled == true)
  {
    printf("[] Differential State Max Size Detected:   %lu\n", differentialStateMaxSizeDetected);
  }
  // If saving hash, do it now
  if (hashOutputFile != "") jaffarCommon::file::saveStringToFile(std::string(hashStringBuffer), hashOutputFile.c_str());

  // If reached this point, everything ran ok
  return 0;
}
