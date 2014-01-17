#!/bin/bash
#build ffmpeg for armv7,armv7s and uses lipo to create fat libraries and deletes the originals


./configure \
--prefix=ios/armv7 \
--disable-ffmpeg \
--disable-ffplay \
--disable-ffprobe \
--disable-ffserver \
--enable-avresample \
--enable-cross-compile \
--sysroot="/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS6.1.sdk" \
--target-os=darwin \
--cc="/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/gcc" \
--extra-cflags="-arch armv7 -mfpu=neon -miphoneos-version-min=6.0" \
--extra-ldflags="-arch armv7 -isysroot /Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS6.1.sdk -miphoneos-version-min=6.0" \
--arch=arm \
--cpu=cortex-a8 \
--enable-pic

make clean
make
make install

./configure \
--prefix=ios/armv7s \
--disable-ffmpeg \
--disable-ffplay \
--disable-ffprobe \
--disable-ffserver \
--enable-avresample \
--enable-cross-compile \
--sysroot="/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS6.1.sdk" \
--target-os=darwin \
--cc="/Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/usr/bin/gcc" \
--extra-cflags="-arch armv7s -mfpu=neon -miphoneos-version-min=6.0" \
--extra-ldflags="-arch armv7s -isysroot /Applications/Xcode.app/Contents/Developer/Platforms/iPhoneOS.platform/Developer/SDKs/iPhoneOS6.1.sdk -miphoneos-version-min=6.0" \
--arch=arm \
--cpu=cortex-a9 \
--enable-pic

make clean
make
make install

cd ios

mkdir -p universal/lib


xcrun -sdk iphoneos lipo -create -arch armv7 armv7/lib/libavformat.a -arch armv7s armv7s/lib/libavformat.a -output universal/lib/libavformat.a

xcrun -sdk iphoneos lipo -create -arch armv7 armv7/lib/libavutil.a -arch armv7s armv7s/lib/libavutil.a -output universal/lib/libavutil.a

xcrun -sdk iphoneos lipo -create -arch armv7 armv7/lib/libswresample.a -arch armv7s armv7s/lib/libswresample.a -output universal/lib/libswresample.a

xcrun -sdk iphoneos lipo -create -arch armv7 armv7/lib/libavcodec.a -arch armv7s armv7s/lib/libavcodec.a -output universal/lib/libavcodec.a

xcrun -sdk iphoneos lipo -create -arch armv7 armv7/lib/libswscale.a -arch armv7s armv7s/lib/libswscale.a -output universal/lib/libswscale.a

xcrun -sdk iphoneos lipo -create -arch armv7 armv7/lib/libavresample.a -arch armv7s armv7s/lib/libavresample.a -output universal/lib/libavresample.a

xcrun -sdk iphoneos lipo -create -arch armv7 armv7/lib/libavdevice.a -arch armv7s armv7s/lib/libavdevice.a -output universal/lib/libavdevice.a

xcrun -sdk iphoneos lipo -create -arch armv7 armv7/lib/libavfilter.a -arch armv7s armv7s/lib/libavfilter.a -output universal/lib/libavfilter.a

mv armv7/include universal/include

rm -rf armv7 armv7s