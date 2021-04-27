case `echo $1 | cut -d '-' -f 1` in
        ubuntu)
                sudo apt-get -qq update
                sudo apt-get install -yq bison libpng-dev pkg-config libsdl2-dev
                (
                cd `mktemp -d`
                curl -L https://github.com/rednex/rgbds/archive/v0.4.0.zip > rgbds.zip
                unzip rgbds.zip
                cd rgbds-*
                make -sj
                sudo make install
                cd ..
                rm -rf *
                )
                ;;
        macos)
                brew install rgbds sdl2
                ;;
        *)
                echo "Unsupported OS"
                exit 1
                ;;
esac