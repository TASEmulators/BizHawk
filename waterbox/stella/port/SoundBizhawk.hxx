//============================================================================
//
//   SSSS    tt          lll  lll
//  SS  SS   tt           ll   ll
//  SS     tttttt  eeee   ll   ll   aaaa
//   SSSS    tt   ee  ee  ll   ll      aa
//      SS   tt   eeeeee  ll   ll   aaaaa  --  "An Atari 2600 VCS Emulator"
//  SS  SS   tt   ee      ll   ll  aa  aa
//   SSSS     ttt  eeeee llll llll  aaaaa
//
// Copyright (c) 1995-2024 by Bradford W. Mott, Stephen Anthony
// and the Stella Team
//
// See the file "License.txt" for information on usage and redistribution of
// this file, and for a DISCLAIMER OF ALL WARRANTIES.
//============================================================================

#ifdef SOUND_SUPPORT

#ifndef SOUND_SDL2_HXX
#define SOUND_SDL2_HXX

class OSystem;
class AudioQueue;
class EmulationTiming;
class AudioSettings;
class Resampler;

#include "bspf.hxx"
#include "Sound.hxx"

/**
  This class implements the sound API for SDL.

  @author Stephen Anthony and Christian Speckner (DirtyHairy)
*/
class SoundBizhawk : public Sound
{
  public:
    /**
      Create a new sound object.  The init method must be invoked before
      using the object.
    */
    SoundBizhawk(OSystem& osystem, AudioSettings& audioSettings);

    /**
      Destructor
    */
    ~SoundBizhawk() override;

  public:
    /**
      Enables/disables the sound subsystem.

      @param enable  Either true or false, to enable or disable the sound system
    */
    void setEnabled(bool enable) override;

    /**
      Initializes the sound device.  This must be called before any
      calls are made to derived methods.
    */
    void open(shared_ptr<AudioQueue> audioQueue,
              EmulationTiming* emulationTiming) override;

    /**
      Sets the sound mute state; sound processing continues.  When enabled,
      sound volume is 0; when disabled, sound volume returns to previously
      set level.

      @param enable  Mutes sound if true, unmute if false
    */
    void mute(bool enable) override;

    /**
      Toggles the sound mute state; sound processing continues.
      Switches between mute(true) and mute(false).
    */
    void toggleMute() override;

    /**
      Set the pause state of the sound object.  While paused, sound is
      neither played nor processed (ie, the sound subsystem is temporarily
      disabled).

      @param enable  Pause sound if true, unpause if false

      @return  The previous (old) pause state
    */
    bool pause(bool enable) override;

    /**
      Sets the volume of the sound device to the specified level.  The
      volume is given as a range from 0 to 100 (0 indicates mute).  Values
      outside this range indicate that the volume shouldn't be changed at all.

      @param volume  The new volume level for the sound device
    */
    void setVolume(uInt32 volume) override;

    /**
      Adjusts the volume of the sound device based on the given direction.

      @param direction  +1 indicates increase, -1 indicates decrease.
      */
    void adjustVolume(int direction = 1) override;

    /**
      This method is called to provide information about the sound device.
    */
    string about() const override;

    /**
      Play a WAV file.

      @param fileName  The name of the WAV file
      @param position  The position to start playing
      @param length    The played length

      @return  True if the WAV file can be played, else false
    */
    bool playWav(const string& fileName, const uInt32 position = 0,
                 const uInt32 length = 0) override;

    /**
      Stop any currently playing WAV file.
    */
    void stopWav() override;

    /**
      Get the size of the WAV file which remains to be played.

      @return  The remaining number of bytes
    */
    uInt32 wavSize() const override;

  private:
    /**
      This method is called to query the audio devices.

      @param devices  List of device names
    */
    void queryHardware(VariantList& devices) override;

    /**
      The actual sound device is opened only when absolutely necessary.
      Typically this will only happen once per program run, but it can also
      happen dynamically when changing sample rate and/or fragment size.
    */
    bool openDevice();

    void initResampler();

  private:
    AudioSettings& myAudioSettings;

    // Audio specification structure
    static float myVolumeFactor;  // Current volume level (0 - 100)

  private:
    // Callback functions invoked by the SDL Audio library when it needs data
    static void callback(void* object, uInt8* stream, int len);

    // Following constructors and assignment operators not supported
    SoundBizhawk() = delete;
    SoundBizhawk(const SoundBizhawk&) = delete;
    SoundBizhawk(SoundBizhawk&&) = delete;
    SoundBizhawk& operator=(const SoundBizhawk&) = delete;
    SoundBizhawk& operator=(SoundBizhawk&&) = delete;
};

#endif

#endif  // SOUND_SUPPORT
