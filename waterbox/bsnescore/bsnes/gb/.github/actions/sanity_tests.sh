set -e

./build/bin/tester/sameboy_tester --jobs 5 \
      --length 40 .github/actions/cgb_sound.gb \
      --length 10  .github/actions/cgb-acid2.gbc \
      --length 10  .github/actions/dmg-acid2.gb \
--dmg --length 40 .github/actions/dmg_sound-2.gb \
--dmg --length 20 .github/actions/oam_bug-2.gb

mv .github/actions/dmg{,-mode}-acid2.bmp

./build/bin/tester/sameboy_tester \
--dmg --length 10  .github/actions/dmg-acid2.gb 

set +e

FAILED_TESTS=`
shasum .github/actions/*.bmp | grep -q -E -v \(\
44ce0c7d49254df0637849c9155080ac7dc3ef3d\ \ .github/actions/cgb-acid2.bmp\|\
dbcc438dcea13b5d1b80c5cd06bda2592cc5d9e0\ \ .github/actions/cgb_sound.bmp\|\
0caadf9634e40247ae9c15ff71992e8f77bbf89e\ \ .github/actions/dmg-acid2.bmp\|\
c50daed36c57a8170ff362042694786676350997\ \ .github/actions/dmg-mode-acid2.bmp\|\
c9e944b7e01078bdeba1819bc2fa9372b111f52d\ \ .github/actions/dmg_sound-2.bmp\|\
f0172cc91867d3343fbd113a2bb98100074be0de\ \ .github/actions/oam_bug-2.bmp\
\)`

if [ -n "$FAILED_TESTS" ] ; then
    echo "Failed the following tests:"
    echo $FAILED_TESTS | tr " " "\n" | grep -q -o -E "[^/]+\.bmp" | sed s/.bmp// | sort
    exit 1
fi

echo Passed all tests