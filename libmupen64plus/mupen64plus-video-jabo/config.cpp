#include "Config.h"
#include "m64p.h"

static m64p_handle video_general_section;
static m64p_handle video_jabo_section;


BOOL Config_Open()
{
    if (ConfigOpenSection("Video-General", &video_general_section) != M64ERR_SUCCESS ||
        ConfigOpenSection("Video-Jabo", &video_jabo_section) != M64ERR_SUCCESS)
    {
        //ERRLOG("Could not open configuration");
        return FALSE;
    }
    ConfigSetDefaultBool(video_general_section, "Fullscreen", false, "Use fullscreen mode if True, or windowed mode if False");
    ConfigSetDefaultInt(video_general_section, "ScreenWidth", 640, "Width of output window or fullscreen width");
    ConfigSetDefaultInt(video_general_section, "ScreenHeight", 480, "Height of output window or fullscreen height");

    return TRUE;
}

int Config_ReadScreenInt(const char *itemname)
{
    return ConfigGetParamInt(video_general_section, itemname);
}

void Config_ReadScreenResolution(int * width, int * height)
{
    *width = ConfigGetParamInt(video_general_section, "ScreenWidth");
    *height = ConfigGetParamInt(video_general_section, "ScreenHeight");
}

BOOL Config_ReadInt(const char *itemname, const char *desc, int def_value, int create, int isBoolean)
{
    //VLOG("Getting value %s", itemname);
    if (isBoolean)
    {
        ConfigSetDefaultBool(video_jabo_section, itemname, def_value, desc);
        return ConfigGetParamBool(video_jabo_section, itemname);
    }
    else
    {
        ConfigSetDefaultInt(video_jabo_section, itemname, def_value, desc);
        return ConfigGetParamInt(video_jabo_section, itemname);
    }

}