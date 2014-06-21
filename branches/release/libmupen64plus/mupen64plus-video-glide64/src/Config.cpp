/*
* Glide64 - Glide video plugin for Nintendo 64 emulators.
* Copyright (c) 2010  Jon Ring
* Copyright (c) 2002  Dave2001
*
* This program is free software; you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation; either version 2 of the License, or
* any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public
* Licence along with this program; if not, write to the Free
* Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
* Boston, MA  02110-1301, USA
*/
#include "Config.h"
#include "m64p.h"

static m64p_handle video_general_section;
static m64p_handle video_glide64_section;


BOOL Config_Open()
{
    if (ConfigOpenSection("Video-General", &video_general_section) != M64ERR_SUCCESS ||
        ConfigOpenSection("Video-Glide64", &video_glide64_section) != M64ERR_SUCCESS)
    {
        WriteLog(M64MSG_ERROR, "Could not open configuration");
        return FALSE;
    }
    ConfigSetDefaultBool(video_general_section, "Fullscreen", false, "Use fullscreen mode if True, or windowed mode if False");
    ConfigSetDefaultInt(video_general_section, "ScreenWidth", 640, "Width of output window or fullscreen width");
    ConfigSetDefaultInt(video_general_section, "ScreenHeight", 480, "Height of output window or fullscreen height");

    return TRUE;
}

PackedScreenResolution Config_ReadScreenSettings()
{
    PackedScreenResolution packedResolution;

    packedResolution.width = ConfigGetParamInt(video_general_section, "ScreenWidth");
    packedResolution.height = ConfigGetParamInt(video_general_section, "ScreenHeight");
    packedResolution.fullscreen = ConfigGetParamBool(video_general_section, "Fullscreen");

    return packedResolution;
}

int Config_ReadInt(const char *itemname, const char *desc, int def_value, BOOL create, BOOL isBoolean)
{
    WriteLog(M64MSG_VERBOSE, "Getting value %s", itemname);
    if (isBoolean)
    {
        ConfigSetDefaultBool(video_glide64_section, itemname, def_value, desc);
        return ConfigGetParamBool(video_glide64_section, itemname);
    }
    else
    {
        ConfigSetDefaultInt(video_glide64_section, itemname, def_value, desc);
        return ConfigGetParamInt(video_glide64_section, itemname);
    }

}
