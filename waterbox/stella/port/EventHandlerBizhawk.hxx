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

#pragma once 

#include "EventHandler.hxx"

/**
  This class handles event collection from the point of view of the specific
  backend toolkit (Bizhawk).  It converts from Bizhawk-specific events into events
  that the Stella core can understand.

  @author  Stephen Anthony
*/
class EventHandlerBizhawk : public EventHandler
{
  public:
    /**
      Create a new Bizhawk event handler object
    */
    explicit EventHandlerBizhawk(OSystem& osystem);
    ~EventHandlerBizhawk() override;

  private:
    /**
      Enable/disable text events (distinct from single-key events).
    */
    void enableTextEvents(bool enable) override;

    /**
      Collects and dispatches any pending Bizhawk events.
    */
    void pollEvent() override;

  private:
    // Following constructors and assignment operators not supported
    EventHandlerBizhawk() = delete;
    EventHandlerBizhawk(const EventHandlerBizhawk&) = delete;
    EventHandlerBizhawk(EventHandlerBizhawk&&) = delete;
    EventHandlerBizhawk& operator=(const EventHandlerBizhawk&) = delete;
    EventHandlerBizhawk& operator=(EventHandlerBizhawk&&) = delete;
};

