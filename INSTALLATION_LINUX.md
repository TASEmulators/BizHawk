# BizHawkRafaelia - Linux Installation Guide

**Author**: Rafael Melo Reis  
**Last Updated**: 2025-11-23  
**Version**: 1.0  

---

## Table of Contents

1. [System Requirements](#system-requirements)
2. [Distribution-Specific Installation](#distribution-specific-installation)
3. [Manual Installation](#manual-installation)
4. [Post-Installation Setup](#post-installation-setup)
5. [Troubleshooting](#troubleshooting)
6. [Uninstallation](#uninstallation)

---

## System Requirements

### Minimum
- **Distribution**: Ubuntu 20.04, Debian 11, Fedora 36, or equivalent
- **Kernel**: Linux 5.4+
- **CPU**: Intel Core 2 Duo / AMD Athlon 64 X2 (2008+)
- **RAM**: 2 GB
- **GPU**: OpenGL 3.3 compatible
- **Storage**: 500 MB free space
- **Display Server**: X11 or Wayland

### Recommended
- **Distribution**: Ubuntu 22.04+, Debian 12+, Fedora 38+
- **Kernel**: Linux 6.0+
- **CPU**: Intel Core i5 (8th gen+) / AMD Ryzen 5 (2000+)
- **RAM**: 8 GB
- **GPU**: Mesa 22.0+ or proprietary drivers
- **Storage**: 2 GB free space (SSD)
- **Display Server**: X11 or Wayland with XWayland

---

## Distribution-Specific Installation

### Ubuntu / Debian / Linux Mint

#### Method 1: Official Package (Recommended)

```bash
# 1. Add repository (if available)
# Currently manual installation required - see Method 2

# 2. Update package list
sudo apt update

# 3. Install BizHawk (when packaged)
# sudo apt install bizhawk-rafaelia
```

#### Method 2: Manual Installation

```bash
# 1. Install dependencies
sudo apt update
sudo apt install -y \
    mono-complete \
    libgdiplus \
    libopenal1 \
    lua5.4 \
    liblua5.4-0 \
    lsb-release \
    ca-certificates \
    libgl1-mesa-glx \
    libgl1-mesa-dri

# 2. Download latest release
cd ~/Downloads
wget https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/releases/latest/download/BizHawkRafaelia-linux-x64.tar.gz

# 3. Extract to /opt or ~/Applications
sudo mkdir -p /opt/bizhawk-rafaelia
sudo tar -xzf BizHawkRafaelia-linux-x64.tar.gz -C /opt/bizhawk-rafaelia

# Or for user installation:
mkdir -p ~/.local/share/bizhawk-rafaelia
tar -xzf BizHawkRafaelia-linux-x64.tar.gz -C ~/.local/share/bizhawk-rafaelia

# 4. Make launcher executable
sudo chmod +x /opt/bizhawk-rafaelia/EmuHawkMono.sh
# Or: chmod +x ~/.local/share/bizhawk-rafaelia/EmuHawkMono.sh

# 5. Create desktop entry
cat > ~/.local/share/applications/bizhawk-rafaelia.desktop << 'EOF'
[Desktop Entry]
Type=Application
Name=BizHawk Rafaelia
Comment=Multi-system emulator for TAS
Exec=/opt/bizhawk-rafaelia/EmuHawkMono.sh
Icon=/opt/bizhawk-rafaelia/icon.png
Terminal=false
Categories=Game;Emulator;
EOF

# 6. Launch
/opt/bizhawk-rafaelia/EmuHawkMono.sh
```

### Arch Linux / Manjaro

#### Method 1: AUR Package

```bash
# Install using yay (or paru, trizen, etc.)
yay -S bizhawk-rafaelia-bin

# Or build from source
yay -S bizhawk-rafaelia

# Launch
bizhawk-rafaelia
```

#### Method 2: Manual Installation

```bash
# 1. Install dependencies
sudo pacman -S \
    mono \
    openal \
    lua54 \
    lsb-release \
    mesa \
    libglvnd

# 2. Download and extract (same as Ubuntu Method 2)
# ... (follow Ubuntu steps 2-6)
```

### Fedora / RHEL / CentOS Stream

```bash
# 1. Install dependencies
sudo dnf install -y \
    mono-complete \
    openal-soft \
    lua \
    redhat-lsb-core \
    mesa-libGL \
    mesa-dri-drivers

# 2. Install additional Mono components
sudo dnf install -y \
    mono-winforms \
    libgdiplus

# 3. Download and extract (same as Ubuntu Method 2)
# ... (follow Ubuntu steps 2-6)
```

### openSUSE

```bash
# 1. Install dependencies
sudo zypper install -y \
    mono-complete \
    openal-soft \
    lua54 \
    lsb-release \
    Mesa-libGL1 \
    Mesa-dri

# 2. Download and extract (same as Ubuntu Method 2)
# ... (follow Ubuntu steps 2-6)
```

### NixOS / Nix Package Manager

```nix
# Option 1: Imperative installation
nix-env -iA nixpkgs.bizhawk

# Option 2: Declarative (add to configuration.nix)
environment.systemPackages = with pkgs; [
  bizhawk
];

# Option 3: Nix shell (temporary)
nix-shell -p bizhawk

# Option 4: Using flakes (if enabled)
nix profile install github:rafaelmeloreisnovo/BizHawkRafaelia
```

**Using the included Nix expression**:

```bash
# Clone repository
git clone https://github.com/rafaelmeloreisnovo/BizHawkRafaelia.git
cd BizHawkRafaelia

# Build with Nix
nix-build

# Run
./result/bin/EmuHawk
```

See [nix_expr_usage_docs.md](Dist/nix_expr_usage_docs.md) for advanced usage.

### Alpine Linux

```bash
# 1. Install dependencies
sudo apk add \
    mono \
    mono-dev \
    openal-soft \
    lua5.4 \
    mesa-gl \
    libgdiplus-dev

# 2. Download and extract (same as Ubuntu Method 2)
# ... (follow Ubuntu steps 2-6)

# Note: Some cores may not work due to glibc dependency
```

---

## ARM64/AArch64 Installation

### Raspberry Pi 4/5

```bash
# 1. Ensure 64-bit OS (not 32-bit)
uname -m  # Should show "aarch64"

# If showing "armv7l", reinstall with 64-bit Raspberry Pi OS

# 2. Install dependencies (Ubuntu/Debian-based)
sudo apt update
sudo apt install -y \
    mono-complete \
    libgdiplus \
    libopenal1 \
    lua5.4 \
    lsb-release \
    mesa-utils

# 3. Download ARM64 build
cd ~/Downloads
wget https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/releases/latest/download/BizHawkRafaelia-linux-arm64.tar.gz

# 4. Extract and install
mkdir -p ~/.local/share/bizhawk-rafaelia
tar -xzf BizHawkRafaelia-linux-arm64.tar.gz -C ~/.local/share/bizhawk-rafaelia

# 5. Run
~/.local/share/bizhawk-rafaelia/EmuHawkMono.sh
```

**Performance Tips for Raspberry Pi**:
- Use lite/minimal OS (no desktop bloat)
- Overclock CPU if possible
- Use heatsink and fan
- Disable rewind feature
- Use lighter cores (NESHawk, SMSHawk, GBHawk)

### Other ARM64 SBCs

Works on:
- Orange Pi 5
- Rock Pi 4
- Odroid N2+
- Jetson Nano

Follow Raspberry Pi instructions above, adjusting for your distribution.

---

## Manual Installation (All Distributions)

### Prerequisites Check

```bash
# Check Mono version (should be 6.12+)
mono --version

# Check OpenGL support (should show OpenGL 3.3+)
glxinfo | grep "OpenGL version"

# Check Lua (should be 5.4)
lua -v

# If any missing, install via distribution package manager
```

### Download and Extract

```bash
# 1. Create installation directory
mkdir -p ~/.local/share/bizhawk-rafaelia
cd ~/.local/share/bizhawk-rafaelia

# 2. Download latest release
wget https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/releases/latest/download/BizHawkRafaelia-linux-x64.tar.gz

# 3. Extract
tar -xzf BizHawkRafaelia-linux-x64.tar.gz --strip-components=1

# 4. Make launcher executable
chmod +x EmuHawkMono.sh

# 5. Test run
./EmuHawkMono.sh
```

### Create Desktop Launcher

```bash
# Create .desktop file
cat > ~/.local/share/applications/bizhawk-rafaelia.desktop << EOF
[Desktop Entry]
Type=Application
Name=BizHawk Rafaelia
GenericName=Multi-System Emulator
Comment=TAS-focused emulator for multiple systems
Exec=$HOME/.local/share/bizhawk-rafaelia/EmuHawkMono.sh %f
Icon=$HOME/.local/share/bizhawk-rafaelia/Assets/EmuHawk.ico
Terminal=false
Categories=Game;Emulator;
MimeType=application/x-nes-rom;application/x-snes-rom;
StartupWMClass=EmuHawk
EOF

# Update desktop database
update-desktop-database ~/.local/share/applications/
```

### Create Command-Line Alias

```bash
# Add to ~/.bashrc or ~/.zshrc
echo 'alias bizhawk="$HOME/.local/share/bizhawk-rafaelia/EmuHawkMono.sh"' >> ~/.bashrc

# Reload shell
source ~/.bashrc

# Now you can run:
bizhawk /path/to/rom.nes
```

---

## Post-Installation Setup

### Initial Configuration

```bash
# Launch BizHawk
bizhawk  # or ./EmuHawkMono.sh

# First run will create:
# ~/.config/BizHawk/  (configuration)
# ~/.local/share/BizHawk/  (data files)
```

### Set Up Firmware

```bash
# Create firmware directory
mkdir -p ~/.config/BizHawk/Firmware

# Copy your legally dumped firmware files
cp /path/to/firmware/* ~/.config/BizHawk/Firmware/

# Or configure custom path in BizHawk:
# Config → Paths... → Firmware
```

### Configure Paths

Edit `~/.config/BizHawk/config.ini`:

```ini
[Paths-Global]
Firmware=/home/username/.config/BizHawk/Firmware
ROMs=/home/username/ROMs
Savestates=/home/username/.config/BizHawk/SaveStates
SaveRAM=/home/username/.config/BizHawk/SaveRAM
Screenshots=/home/username/Pictures/BizHawk
```

### Controller Setup

**Using Keyboard**:
- Config → Controllers...
- Bind keys to virtual gamepad

**Using USB Gamepad**:
```bash
# Check if detected
ls /dev/input/js*

# Should show: /dev/input/js0, etc.

# Test with jstest (install if needed)
sudo apt install joystick  # Ubuntu/Debian
jstest /dev/input/js0

# In BizHawk:
# Config → Controllers... → Use gamepad
```

**Using Bluetooth Controller**:
```bash
# Pair controller
bluetoothctl
[bluetooth]# scan on
[bluetooth]# pair XX:XX:XX:XX:XX:XX
[bluetooth]# connect XX:XX:XX:XX:XX:XX
[bluetooth]# trust XX:XX:XX:XX:XX:XX

# Verify
ls /dev/input/js*

# Use in BizHawk (same as USB)
```

---

## Troubleshooting

### Issue 1: "mono: command not found"

**Solution**:
```bash
# Ubuntu/Debian
sudo apt install mono-complete

# Arch
sudo pacman -S mono

# Fedora
sudo dnf install mono-complete
```

### Issue 2: "Could not load file or assembly"

**Solution**:
```bash
# Install Mono developer tools
sudo apt install mono-devel mono-runtime

# Or reinstall Mono completely
sudo apt remove mono-runtime mono-complete
sudo apt autoremove
sudo apt install mono-complete
```

### Issue 3: Black Screen / OpenGL Errors

**Solution**:

```bash
# Check OpenGL version
glxinfo | grep "OpenGL"

# If < 3.3, update drivers:

# Intel
sudo apt install mesa-utils intel-media-va-driver

# AMD
sudo apt install mesa-utils mesa-vulkan-drivers

# NVIDIA (proprietary)
sudo ubuntu-drivers install  # Ubuntu
# Or manually: https://www.nvidia.com/Download/index.aspx
```

### Issue 4: Audio Issues / No Sound

**Solution**:

```bash
# Check OpenAL installation
ls /usr/lib/x86_64-linux-gnu/libopenal.so*

# If missing:
sudo apt install libopenal1 libopenal-dev

# Check PulseAudio/Pipewire
pactl info  # Should show server info

# Restart audio server if needed
pulseaudio -k && pulseaudio --start
```

### Issue 5: Controller Not Detected

**Solution**:

```bash
# Check udev rules
ls /dev/input/

# Install joystick utilities
sudo apt install joystick

# Test controller
jstest /dev/input/js0

# If not working, check permissions
sudo usermod -a -G input $USER
# Log out and back in

# For Xbox controllers, install xpad
sudo apt install xboxdrv
```

### Issue 6: Poor Performance on Wayland

**Solution**:

```bash
# Option 1: Force X11 backend
GDK_BACKEND=x11 ./EmuHawkMono.sh

# Option 2: Add to launcher
Exec=env GDK_BACKEND=x11 /path/to/EmuHawkMono.sh

# Option 3: Switch to X11 session (logout → select X11)
```

### Issue 7: Crashes on Specific Cores

**Solution**:

```bash
# Check library dependencies
ldd ~/.local/share/bizhawk-rafaelia/dll/*.so

# Install missing libraries
# Example: libpng not found
sudo apt install libpng16-16

# Try different core:
# Config → Cores → Select alternative
```

### Issue 8: Permission Denied

**Solution**:

```bash
# Make launcher executable
chmod +x EmuHawkMono.sh

# Fix ownership
chown -R $USER:$USER ~/.local/share/bizhawk-rafaelia
chown -R $USER:$USER ~/.config/BizHawk

# Check SELinux (Fedora/RHEL)
sestatus  # If enforcing, may need policy adjustment
```

### Issue 9: Mono Runtime Errors

**Solution**:

```bash
# Clear Mono cache
rm -rf ~/.cache/mono

# Reinstall BizHawk
rm -rf ~/.local/share/bizhawk-rafaelia
# Re-extract from tar.gz

# Update Mono to latest
sudo apt update && sudo apt upgrade mono-complete
```

### Issue 10: High DPI / Scaling Issues

**Solution**:

```bash
# Option 1: Override DPI
GDK_DPI_SCALE=1 ./EmuHawkMono.sh

# Option 2: Use fractional scaling
gsettings set org.gnome.mutter experimental-features "['scale-monitor-framebuffer']"

# Option 3: Set environment variable in launcher
Exec=env GDK_DPI_SCALE=1 /path/to/EmuHawkMono.sh
```

---

## Performance Optimization

### For Low-End Systems

```bash
# Run with reduced settings
MONO_ENV_OPTIONS="--gc=sgen --gc-params=nursery-size=4m" ./EmuHawkMono.sh

# Or edit launcher script to add environment variables
```

### For High-End Systems

```bash
# Enable hardware acceleration
LIBGL_ALWAYS_SOFTWARE=0 ./EmuHawkMono.sh

# Use better garbage collector
MONO_ENV_OPTIONS="--gc=sgen --gc-params=nursery-size=16m,major=marksweep-conc" ./EmuHawkMono.sh
```

### Disable Compositing (for lower latency)

**KDE Plasma**:
```bash
# Temporarily disable
qdbus org.kde.KWin /Compositor suspend

# Re-enable
qdbus org.kde.KWin /Compositor resume
```

**GNOME**:
```bash
# Not easily disabled in modern GNOME
# Consider using Xfce or MATE for lowest latency
```

---

## Building from Source

### Prerequisites

```bash
# Ubuntu/Debian
sudo apt install -y \
    git \
    dotnet-sdk-8.0 \
    mono-complete \
    mono-devel \
    build-essential \
    cmake \
    ninja-build

# Arch
sudo pacman -S \
    git \
    dotnet-sdk \
    mono \
    base-devel \
    cmake \
    ninja
```

### Build Steps

```bash
# 1. Clone repository
git clone https://github.com/rafaelmeloreisnovo/BizHawkRafaelia.git
cd BizHawkRafaelia

# 2. Build
./Dist/BuildRelease.sh

# 3. Output in ./output/
cd output/
./EmuHawkMono.sh
```

---

## Uninstallation

### Package Manager Installation

```bash
# Ubuntu/Debian
sudo apt remove bizhawk-rafaelia

# Arch
yay -R bizhawk-rafaelia

# Nix
nix-env -e bizhawk
```

### Manual Installation

```bash
# Remove application
rm -rf ~/.local/share/bizhawk-rafaelia

# Remove configuration
rm -rf ~/.config/BizHawk

# Remove desktop entry
rm ~/.local/share/applications/bizhawk-rafaelia.desktop

# Update desktop database
update-desktop-database ~/.local/share/applications/

# Remove alias (if added)
# Edit ~/.bashrc and remove bizhawk alias line
```

---

## Additional Resources

- [Hardware Compatibility Matrix](HARDWARE_COMPATIBILITY_MATRIX.md)
- [Main README](README.md)
- [Nix Usage Documentation](Dist/nix_expr_usage_docs.md)

---

**Last Updated**: 2025-11-23  
**Maintainer**: Rafael Melo Reis  
**License**: MIT

For issues or questions: https://github.com/rafaelmeloreisnovo/BizHawkRafaelia/issues
