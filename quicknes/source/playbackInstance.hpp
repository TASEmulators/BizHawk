#pragma once

#include "nesInstance.hpp"
#include <SDL.h>
#include <SDL_image.h>
#include <extern/hqn/hqn.h>
#include <extern/hqn/hqn_gui_controller.h>
#include <jaffarCommon/deserializers/contiguous.hpp>
#include <jaffarCommon/hash.hpp>
#include <jaffarCommon/serializers/contiguous.hpp>
#include <string>
#include <unistd.h>

#define _INVERSE_FRAME_RATE 16667

struct stepData_t
{
  std::string inputString;
  jaffar::input_t decodedInput;
  uint8_t *stateData;
  jaffarCommon::hash::hash_t hash;
};

class PlaybackInstance
{
  static const uint16_t image_width = 256;
  static const uint16_t image_height = 240;

  public:
  void addStep(const std::string &inputString, const jaffar::input_t decodedInput)
  {
    stepData_t step;
    step.inputString = inputString;
    step.decodedInput = decodedInput;
    step.stateData = (uint8_t *)malloc(_emu->getFullStateSize());

    jaffarCommon::serializer::Contiguous serializer(step.stateData);
    _emu->serializeState(serializer);
    step.hash = jaffarCommon::hash::calculateMetroHash(_emu->getLowMem(), _emu->getLowMemSize());

    // Adding the step into the sequence
    _stepSequence.push_back(step);
  }

  // Initializes the playback module instance
  PlaybackInstance(NESInstance *emu, const std::string &overlayPath = "") : _emu(emu)
  {
    // Loading overlay, if provided
    if (overlayPath != "")
    {
      // Using overlay
      _useOverlay = true;

      // Loading overlay images
      std::string imagePath;

      imagePath = _overlayPath + std::string("/base.png");
      _overlayBaseSurface = IMG_Load(imagePath.c_str());
      if (_overlayBaseSurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());

      imagePath = _overlayPath + std::string("/button_a.png");
      _overlayButtonASurface = IMG_Load(imagePath.c_str());
      if (_overlayButtonASurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());

      imagePath = _overlayPath + std::string("/button_b.png");
      _overlayButtonBSurface = IMG_Load(imagePath.c_str());
      if (_overlayButtonBSurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());

      imagePath = _overlayPath + std::string("/button_select.png");
      _overlayButtonSelectSurface = IMG_Load(imagePath.c_str());
      if (_overlayButtonSelectSurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());

      imagePath = _overlayPath + std::string("/button_start.png");
      _overlayButtonStartSurface = IMG_Load(imagePath.c_str());
      if (_overlayButtonStartSurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());

      imagePath = _overlayPath + std::string("/button_left.png");
      _overlayButtonLeftSurface = IMG_Load(imagePath.c_str());
      if (_overlayButtonLeftSurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());

      imagePath = _overlayPath + std::string("/button_right.png");
      _overlayButtonRightSurface = IMG_Load(imagePath.c_str());
      if (_overlayButtonRightSurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());

      imagePath = _overlayPath + std::string("/button_up.png");
      _overlayButtonUpSurface = IMG_Load(imagePath.c_str());
      if (_overlayButtonUpSurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());

      imagePath = _overlayPath + std::string("/button_down.png");
      _overlayButtonDownSurface = IMG_Load(imagePath.c_str());
      if (_overlayButtonDownSurface == NULL) JAFFAR_THROW_LOGIC("[Error] Could not load image: %s, Reason: %s\n", imagePath.c_str(), SDL_GetError());
    }
  }

  void initialize(const std::vector<std::string> &sequence)
  {
    // Getting input decoder
    auto inputParser = _emu->getInputParser();

    // Building sequence information
    for (const auto &input : sequence)
    {
      // Getting decoded input
      const auto decodedInput = inputParser->parseInputString(input);

      // Adding new step
      addStep(input, decodedInput);

      // Advance state based on the input received
      _emu->advanceState(decodedInput);
    }

    // Adding last step with no input
    addStep("<End Of Sequence>", jaffar::input_t());
  }

  void enableRendering(SDL_Window *window)
  {
    // Allocating video buffer
    _video_buffer = (uint8_t *)malloc(image_width * image_height);

    // Setting video buffer
    ((emulator_t *)_emu->getInternalEmulatorPointer())->set_pixels(_video_buffer, image_width + 8);

    // Loading Emulator instance HQN
    _hqnState.setEmulatorPointer(_emu->getInternalEmulatorPointer());
    static uint8_t video_buffer[image_width * image_height];
    _hqnState.m_emu->set_pixels(video_buffer, image_width + 8);

    // Enabling emulation rendering
    _emu->enableRendering();

    // Creating HQN GUI
    _hqnGUI = hqn::GUIController::create(_hqnState, window);
    _hqnGUI->setScale(1);
  }

  // Function to render frame
  void renderFrame(const size_t stepId)
  {
    // Checking the required step id does not exceed contents of the sequence
    if (stepId > _stepSequence.size()) JAFFAR_THROW_LOGIC("[Error] Attempting to render a step larger than the step sequence");

    // Getting step information
    const auto &step = _stepSequence[stepId];

    // Pointer to overlay images (NULL if unused)
    SDL_Surface *overlayButtonASurface = NULL;
    SDL_Surface *overlayButtonBSurface = NULL;
    SDL_Surface *overlayButtonSelectSurface = NULL;
    SDL_Surface *overlayButtonStartSurface = NULL;
    SDL_Surface *overlayButtonLeftSurface = NULL;
    SDL_Surface *overlayButtonRightSurface = NULL;
    SDL_Surface *overlayButtonUpSurface = NULL;
    SDL_Surface *overlayButtonDownSurface = NULL;

    // Load correct overlay images, if using overlay
    if (_useOverlay == true)
    {
      if (step.inputString.find("A") != std::string::npos) overlayButtonASurface = _overlayButtonASurface;
      if (step.inputString.find("B") != std::string::npos) overlayButtonBSurface = _overlayButtonBSurface;
      if (step.inputString.find("S") != std::string::npos) overlayButtonSelectSurface = _overlayButtonSelectSurface;
      if (step.inputString.find("T") != std::string::npos) overlayButtonStartSurface = _overlayButtonStartSurface;
      if (step.inputString.find("L") != std::string::npos) overlayButtonLeftSurface = _overlayButtonLeftSurface;
      if (step.inputString.find("R") != std::string::npos) overlayButtonRightSurface = _overlayButtonRightSurface;
      if (step.inputString.find("U") != std::string::npos) overlayButtonUpSurface = _overlayButtonUpSurface;
      if (step.inputString.find("D") != std::string::npos) overlayButtonDownSurface = _overlayButtonDownSurface;
    }

    // Since we do not store the blit information (too much memory), we need to load the previous frame and re-run the input

    // If its the first step, then simply reset
    if (stepId == 0) _emu->doHardReset();

    // Else we load the previous frame
    if (stepId > 0)
    {
      const auto stateData = getStateData(stepId - 1);
      jaffarCommon::deserializer::Contiguous deserializer(stateData);
      _emu->deserializeState(deserializer);
      _emu->advanceState(getDecodedInput(stepId - 1));
    }

    // Updating image
    int32_t curBlit[BLIT_SIZE];
    saveBlit(_emu->getInternalEmulatorPointer(), curBlit, hqn::HQNState::NES_VIDEO_PALETTE, 0, 0, 0, 0);
    _hqnGUI->update_blit(curBlit, _overlayBaseSurface, overlayButtonASurface, overlayButtonBSurface, overlayButtonSelectSurface, overlayButtonStartSurface, overlayButtonLeftSurface, overlayButtonRightSurface, overlayButtonUpSurface, overlayButtonDownSurface);
  }

  size_t getSequenceLength() const
  {
    return _stepSequence.size();
  }

  const std::string getInputString(const size_t stepId) const
  {
    // Checking the required step id does not exceed contents of the sequence
    if (stepId > _stepSequence.size()) JAFFAR_THROW_LOGIC("[Error] Attempting to render a step larger than the step sequence");

    // Getting step information
    const auto &step = _stepSequence[stepId];

    // Returning step input
    return step.inputString;
  }

  const jaffar::input_t getDecodedInput(const size_t stepId) const
  {
    // Checking the required step id does not exceed contents of the sequence
    if (stepId > _stepSequence.size()) JAFFAR_THROW_LOGIC("[Error] Attempting to render a step larger than the step sequence");

    // Getting step information
    const auto &step = _stepSequence[stepId];

    // Returning step input
    return step.decodedInput;
  }

  const uint8_t *getStateData(const size_t stepId) const
  {
    // Checking the required step id does not exceed contents of the sequence
    if (stepId > _stepSequence.size()) JAFFAR_THROW_LOGIC("[Error] Attempting to render a step larger than the step sequence");

    // Getting step information
    const auto &step = _stepSequence[stepId];

    // Returning step input
    return step.stateData;
  }

  const jaffarCommon::hash::hash_t getStateHash(const size_t stepId) const
  {
    // Checking the required step id does not exceed contents of the sequence
    if (stepId > _stepSequence.size()) JAFFAR_THROW_LOGIC("[Error] Attempting to render a step larger than the step sequence");

    // Getting step information
    const auto &step = _stepSequence[stepId];

    // Returning step input
    return step.hash;
  }


  private:
  // Internal sequence information
  std::vector<stepData_t> _stepSequence;

  // Storage for the HQN state
  hqn::HQNState _hqnState;

  // Storage for the HQN GUI controller
  hqn::GUIController *_hqnGUI;

  // Pointer to the contained emulator instance
  NESInstance *const _emu;

  // Flag to store whether to use the button overlay
  bool _useOverlay = false;

  // Video buffer
  uint8_t *_video_buffer;

  // Overlay info
  std::string _overlayPath;
  SDL_Surface *_overlayBaseSurface = NULL;
  SDL_Surface *_overlayButtonASurface;
  SDL_Surface *_overlayButtonBSurface;
  SDL_Surface *_overlayButtonSelectSurface;
  SDL_Surface *_overlayButtonStartSurface;
  SDL_Surface *_overlayButtonLeftSurface;
  SDL_Surface *_overlayButtonRightSurface;
  SDL_Surface *_overlayButtonUpSurface;
  SDL_Surface *_overlayButtonDownSurface;
};
