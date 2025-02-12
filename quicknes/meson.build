project('quickerNES','c','cpp',
  version: '1.0.0',
  license: 'GPL-3.0-only',
  default_options : ['cpp_std=c++20', 'default_library=shared', 'buildtype=release'],
  subproject_dir : 'extern'
)

# Loading dependencies
subdir('source')

# Grabbing hqn dependency

hqnSubproject = subproject('hqn')
hqnDependency = hqnSubproject.get_variable('hqnDependency')

# Do not build any targets if this is a subproject
if meson.is_subproject() == false

# Common application flags
commonCompileArgs = [ '-Wfatal-errors', '-Wall']

# Grabbing jaffarCommon dependency

jaffarCommonSubproject = subproject('jaffarCommon')
jaffarCommonDependency = jaffarCommonSubproject.get_variable('jaffarCommonDependency')

# Building playback tool
  if get_option('buildPlayer') == true
    executable('player',
      'source/player.cpp',
      cpp_args            : [ commonCompileArgs, '-DNCURSES' ],
      dependencies        : [ jaffarCommonDependency, quickerNESDependency, toolDependency, dependency('sdl2'), dependency('SDL2_image'), hqnDependency ],
      link_args           : [ '-lncurses' ]
    )
  endif

  # Building tester tool for QuickerNES

  quickerNESTester = executable('quickerNESTester',
    'source/tester.cpp',
    cpp_args            : [ commonCompileArgs, '-Werror' ],
    dependencies        : [ jaffarCommonDependency, quickerNESDependency, toolDependency ]
  )

  # Building tester tool for the original QuickNES

  if get_option('buildQuickNES') == true
  quickNESTester = executable('quickNESTester',
    'source/tester.cpp',
    cpp_args            : [ commonCompileArgs, '-w', '-DDISABLE_AUTO_FILE', '-D__LIBRETRO__', '-DNDEBUG', '-DBLARGG_NONPORTABLE'  ],
    dependencies        : [ jaffarCommonDependency, quickNESDependency, toolDependency ]
  )
  endif

  # Building tests
  if get_option('buildQuickNES') == true
  subdir('tests')
  endif
  
endif # If not subproject