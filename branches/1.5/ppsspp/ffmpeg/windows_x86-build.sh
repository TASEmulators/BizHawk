#!/bin/bash
#ffmpeg for windows x86
#windows_x86-build.sh requements.
# MinGW
# MSYS
# 1. open command-prompt of Visual Studio
# 2. run msys.bat on the command-line.
# 3. $ cd /PathTo/ppsspp/ffmpeg
# 4. $ windows_x86-build.sh
#build requements.
# use toolchain=msvc
# http://ffmpeg.org/platform.html#Windows

#///////////////////////////////////////////////////////////////////////////////

ARCH=x86

PREFIX="./Windows/${ARCH}"

mkdir -p ${PREFIX}

IS_STATIC_LIB=TRUE

IS_SHARED_LIB_INTO_BIN_DIR=FALSE

GENERAL="
    --toolchain=msvc
    --prefix=$PREFIX
    --arch=${ARCH}
#    --cpu=opteron-sse3
#    --extra-ldflags="-lz"
#    --optflags=""
    --disable-programs
    --disable-avfilter
    --disable-postproc
    --disable-doc
    --disable-pthreads
    --enable-w32threads
    --disable-network
    --disable-everything
    --disable-encoders
    --disable-muxers
    --disable-hwaccels
    --disable-parsers
    --disable-protocols
    --enable-dxva2
"

AUDIO_DECODERS="
    --enable-decoder=aac
    --enable-decoder=aac_latm
    --enable-decoder=atrac3
    --enable-decoder=mp3
    --enable-decoder=pcm_s16le
    --enable-decoder=pcm_s8
"

VIDEO_DECODERS="
    --enable-decoder=h264
    --enable-decoder=mpeg4
    --enable-decoder=mpeg2video
    --enable-decoder=mjpeg
    --enable-decoder=mjpegb
"

#unused
AUDIO_ENCODERS="
    --enable-encoder=aac
    --enable-encoder=pcm_s16le
    --enable-encoder=pcm_s8
"

#unused
VIDEO_ENCODERS="
#    --enable-encoder=libx264
#    --enable-encoder=libx264rgb
    --enable-encoder=mpeg4
#    --enable-encoder=msmpeg4v2
#    --enable-encoder=msmpeg4v3
#    --enable-encoder=libxvid
    --enable-encoder=mpeg2video
#    --enable-encoder=mjpeg
"

HARDWARE_ACCELS="
    --enable-hwaccel=h264_dxva2
#    --enable-hwaccel=h264_vaapi
#    --enable-hwaccel=h264_vda
#    --enable-hwaccel=h264_vdpau
#    --enable-hwaccel=mpeg4_vaapi
#    --enable-hwaccel=mpeg4_vdpau
"

#unused
MUXERS="
    --enable-muxer=h264
    --enable-muxer=mp4
    --enable-muxer=m4v
    --enable-muxer=avi
    --enable-muxer=mp3
    --enable-muxer=psp
    --enable-muxer=oma
    --enable-muxer=wav
    --enable-muxer=pcm_s16le
    --enable-muxer=pcm_s8

"

DEMUXERS="
    --enable-demuxer=h264
    --enable-demuxer=m4v
    --enable-demuxer=mp3
    --enable-demuxer=mpegvideo
    --enable-demuxer=mpegps
    --enable-demuxer=mjpeg
    --enable-demuxer=avi
    --enable-demuxer=aac
    --enable-demuxer=pmp
    --enable-demuxer=oma
    --enable-demuxer=pcm_s16le
    --enable-demuxer=pcm_s8
    --enable-demuxer=wav
"

PARSERS="
    --enable-parser=h264
    --enable-parser=mpeg4video
    --enable-parser=mpegaudio
    --enable-parser=mpegvideo
    --enable-parser=mjpeg
    --enable-parser=aac
    --enable-parser=aac_latm
"

PROTOCOLS=""

BSFS="
#    --enable-bsf=aac_adtstoasc
#    --enable-bsf=chomp
#    --enable-bsf=dump_extradata
#    --enable-bsf=h264_mp4toannexb
#    --enable-bsf=mjpeg2jpeg
#    --enable-bsf=mjpega_dump_header
#    --enable-bsf=mp3_header_compress
#    --enable-bsf=mp3_header_decompress
#    --enable-bsf=remove_extradata
"

INPUT_DEVICES="
    --enable-indev=dshow
"

OUTPUT_DEVICES="
#    --enable-outdev=sdl
"

FILTERS=""

#///////////////////////////////////////////////////////////////////////////////

append() {
    var=$1
    shift
    eval "$var=\"\$$var $*\""
}

isstaticlib() {
    case "$IS_STATIC_LIB" in
        "TRUE" | "true" | "1" ) return 0 ;;
        *) return 1 ;;
    esac
}

isintobin() {
    case "$IS_SHARED_LIB_INTO_BIN_DIR" in
        "TRUE" | "true" | "1" ) return 0 ;;
        *) return 1 ;;
    esac
}

genelatelibparams() {
    ret=""
    if (isstaticlib) then
        ret=" --enable-static --disable-shared"
    else
        ret=" --enable-shared --disable-static"
    fi
    echo "$ret"
}

genelateparams() {
    eval "value=\"\$$1\""
    ret=""
    value=$(echo "$value" | sed "s/ //g")
    for var in $value ; do
        if [ ! `echo "$var" | fgrep -o "#"` ]; then
            ret="$ret $var"
        fi
    done
    echo "$ret"
}

params_dump() {
    eval "value=\"\$$1\""
    echo "---- dump configure params ----"
    IFS=" "
    for var in $value ; do
        echo "$var"
    done
    echo "---- end dump ----"
}


function build_ffmpeg
{
echo "Converting From CRLF To LF."
find ./ -regex "\(.*\.mak\|.*Makefile\)" | xargs dos2unix

echo "Generate configure params."

PARAMS="$(genelateparams GENERAL)\
$(genelatelibparams)\
$(echo -e "$(genelateparams AUDIO_DECODERS)")\
$(echo -e "$(genelateparams VIDEO_DECODERS)")\
$(echo -e "$(genelateparams BSFS)")\
$(echo -e "$(genelateparams PARSERS)")\
$(echo -e "$(genelateparams DEMUXERS)")\
$(echo -e "$(genelateparams HARDWARE_ACCELS)")\
$(echo -e "$(genelateparams INPUT_DEVICES)")\
"
params_dump PARAMS

# these are not necessary
# $(echo -e "$(genelateparams MUXERS)")
# $(echo -e "$(genelateparams AUDIO_ENCODERS)")\
# $(echo -e "$(genelateparams VIDEO_ENCODERS)")\


echo "---- configure ----"
./configure --extra-cflags="-MD -IWindowsInclude" $PARAMS

echo "---- make clean ----"
make clean
echo "---- make install ----"
make install 2>&1 | tee build.log
echo "---- rename and copy for ppsspp ----"
if (isstaticlib) then
    pushd $PREFIX/lib
    echo "Renaming "foo.a" to "foo.lib" in the build-directory."
    for fname in *.a; do
        mv -fv $fname $(echo "$fname" | sed -e "s/lib\(.*\)\.a/\1/").lib
    done
    popd
else
    binpath=$PREFIX/bin
    libpath=$PREFIX/lib
    absbin=$(cd $(dirname "$binpath/*") && pwd)
    absbin=$(echo "${absbin//\//\\}" | sed -e "s/\\\\\(.\)/\1:/")
    pushd $libpath
    for fname in *.def; do
        outname=${fname%%-*}
        if (isintobin) then
            abspath="$absbin\\$outname.lib"
            lib \/machine:i386 \/def:$fname \/out:$abspath
            popd
        	rm -fv $binpath/$outname.exp
        	pushd $libpath
        else
            lib \/machine:i386 \/def:$fname \/out:$outname.lib
            rm -fv $outname.exp
        fi
    done
    popd
fi
echo "---- windows_x86-build.sh finished ----"
}
build_ffmpeg
