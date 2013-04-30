project(yabause-wii)

if(NOT WII)
    return()
endif()

set(yabause_wii_SOURCES keys.h menu.h perwii.c perwii.h sndwii.c sndwii.h yui.c)

add_executable(yabause-wii ${yabause_wii_SOURCES})
set_target_properties(yabause-wii PROPERTIES LINK_FLAGS -L/opt/devkitpro/libogc/lib/wii/)
set_target_properties(yabause-wii PROPERTIES OUTPUT_NAME yabause.elf)
target_link_libraries(yabause-wii yabause -lfat -lwiiuse -lbte -logc -lm)
