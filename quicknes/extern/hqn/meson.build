project('hqn','c','cpp',
  version: '1.0.0',
  license: 'GPL-3.0-only',
  default_options : ['cpp_std=c++20', 'default_library=shared', 'buildtype=release']
)

hqnSrc = [
  'hqn.cpp',
  'hqn_gui_controller.cpp',
  'hqn_surface.cpp',
]

hqnDependency = declare_dependency(
  sources             : hqnSrc,
  include_directories : '.'
 )