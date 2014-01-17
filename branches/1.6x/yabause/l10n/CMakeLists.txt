project(yabause-l10n)

set(LANGS de es fr it lt pt pt_BR sv)

if (UNIX AND NOT APPLE)
    foreach(LANG ${LANGS})
	    install(FILES "yabause_${LANG}.yts" DESTINATION "share/yabause/yts" RENAME "${LANG}.yts")
    endforeach()
elseif (WIN32)
    foreach(LANG ${LANGS})
	    install(FILES "yabause_${LANG}.yts" DESTINATION "trans" RENAME "${LANG}.yts")
    endforeach()
endif ()
