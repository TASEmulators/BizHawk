#!/usr/bin/env bash
if [[ $# -ne 1 ]]; then
    echo "Usage: $0 <check|fix>"
    exit 1
fi
task="${1}"; shift

function check()
{
  if [ ! $? -eq 0 ]; then 
    echo "Error fixing style."
    exit -1 
  fi
}

function check_syntax()
{
    # If run-clang-format is not installed, clone it
    if [ ! -f  run-clang-format/run-clang-format.py ]; then

      git clone https://github.com/Sarcasm/run-clang-format.git
      if [ ! $? -eq 0 ]; then
        echo "Error installing run-clang-format."
        exit 1
      fi
    fi

     python3 run-clang-format/run-clang-format.py --recursive source --extensions "cpp,hpp"

     if [ ! $? -eq 0 ]; then
       echo "Error: C++ Code formatting in source is not normalized."
       echo "Solution: Please run this program with the 'fix' argument"
       exit -1
     fi
}

function fix_syntax()
{
      src_files=`find source -type f -name "*.cpp" -o -name "*.hpp"`
      echo $src_files | xargs -n6 -P2 clang-format -style=file -i "$@"
      check
}

##############################################
### Testing/fixing C++ Code Style
##############################################
command -v clang-format >/dev/null
if [ ! $? -eq 0 ]; then
    echo "Error: please install clang-format on your system."
    exit -1
fi
 
if [[ "${task}" == 'check' ]]; then
    check_syntax
else
    fix_syntax
fi

exit 0