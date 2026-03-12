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

#include "OSystemStandalone.hxx"

/**
  This class defines an OSystem object for UNIX-like OS's (Linux).
  It is responsible for completely implementing getBaseDirectories(),
  to set the base directory and various other save/load locations.

  @author  Stephen Anthony
*/
class OSystemBIZHAWK : public OSystemStandalone
{
  public:
    OSystemBIZHAWK() = default;
    ~OSystemBIZHAWK() override = default;

    /**
      Determine the base directory and home directory from the derived
      class.  It can also use hints, as described below.

      @param basedir  The base directory for all configuration files
      @param homedir  The default directory to store various other files
      @param useappdir  A hint that the base dir should be set to the
                        app directory; not all ports can do this, so
                        they are free to ignore it
      @param usedir     A hint that the base dir should be set to this
                        parameter; not all ports can do this, so
                        they are free to ignore it
    */
    void getBaseDirectories(string& basedir, string& homedir,
                            bool useappdir, string_view usedir) override;

    void initPersistence(FSNode& basedir) override;

    string describePresistence() override;

  private:
    // Following constructors and assignment operators not supported
    OSystemBIZHAWK(const OSystemBIZHAWK&) = delete;
    OSystemBIZHAWK(OSystemBIZHAWK&&) = delete;
    OSystemBIZHAWK& operator=(const OSystemBIZHAWK&) = delete;
    OSystemBIZHAWK& operator=(OSystemBIZHAWK&&) = delete;
};

