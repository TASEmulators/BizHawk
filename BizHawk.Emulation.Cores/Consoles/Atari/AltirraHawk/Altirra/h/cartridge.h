//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2009 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.

#ifndef f_AT_CARTRIDGE_H
#define f_AT_CARTRIDGE_H

#include <vd2/system/vdalloc.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>
#include <at/atcore/devicecart.h>
#include <at/atio/cartridgeimage.h>
#include <at/atemulation/flash.h>
#include <at/atemulation/eeprom.h>

class ATSaveStateReader;
class ATSaveStateWriter;
class IVDRandomAccessStream;
class IATUIRenderer;
class ATMemoryManager;
class ATMemoryLayer;
class ATScheduler;

class ATCartridgeEmulator final
	: public IATDeviceCartridge
{
	ATCartridgeEmulator(const ATCartridgeEmulator&) = delete;
	ATCartridgeEmulator& operator=(const ATCartridgeEmulator&) = delete;

public:
	ATCartridgeEmulator();
	~ATCartridgeEmulator();

	void Init(ATMemoryManager *memman, ATScheduler *sch, int basePriority, ATCartridgePriority cartPri, bool fastBus);
	void Shutdown();

	void SetUIRenderer(IATUIRenderer *r);

	void SetFastBus(bool fastBus);

	int GetCartBank() const { return mCartBank; }
	bool IsBASICDisableAllowed() const;		// Cleared if we have a cart type that doesn't want OPTION pressed (AtariMax).
	bool IsDirty() const { return mbDirty; }
	
	const wchar_t *GetPath() const;

	ATCartridgeMode GetMode() const { return mCartMode; }

	uint64 GetChecksum();
	std::optional<uint32> GetImageFileCRC() const;

	void Load5200Default();
	void LoadNewCartridge(ATCartridgeMode mode);
	bool Load(const wchar_t *fn, ATCartLoadContext *loadCtx);
	bool Load(const wchar_t *origPath, IVDRandomAccessStream& stream, ATCartLoadContext *loadCtx);
	void Load(IATCartridgeImage *image);
	void Unload();

	void Save(const wchar_t *fn, bool includeHeader);

	void ColdReset();

	void BeginLoadState(ATSaveStateReader& reader);
	void LoadStatePrivate(ATSaveStateReader& reader);
	void EndLoadState(ATSaveStateReader& reader);
	void BeginSaveState(ATSaveStateWriter& writer);
	void SaveStatePrivate(ATSaveStateWriter& writer);

	uint8 DebugReadLinear(uint32 offset) const;
	uint8 DebugReadBanked(uint32 globalAddress) const;

public:		// IATDeviceCartridge
	void InitCartridge(IATDeviceCartridgePort *cartPort) override;
	bool IsLeftCartActive() const override;
	void SetCartEnables(bool leftEnable, bool rightEnable, bool cctlEnable) override;
	void UpdateCartSense(bool leftActive) override;

protected:
	struct LayerEnables {
		bool mbRead;
		bool mbWrite;
		bool mbRD5Masked;

		void Init(uint8 pageBase, uint8 pageSize);
	};

	template<class T> void ExchangeState(T& io);

	static sint32 ReadByte_Unmapped(void *thisptr0, uint32 address);
	static bool WriteByte_Unmapped(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_BB5200_1(void *thisptr0, uint32 address);
	static sint32 ReadByte_BB5200_2(void *thisptr0, uint32 address);
	static bool WriteByte_BB5200_1(void *thisptr0, uint32 address, uint8 value);
	static bool WriteByte_BB5200_2(void *thisptr0, uint32 address, uint8 value);
	static sint32 ReadByte_BB800_1(void *thisptr0, uint32 address);
	static sint32 ReadByte_BB800_2(void *thisptr0, uint32 address);
	static bool WriteByte_BB800_1(void *thisptr0, uint32 address, uint8 value);
	static bool WriteByte_BB800_2(void *thisptr0, uint32 address, uint8 value);
	static sint32 DebugReadByte_SIC(void *thisptr0, uint32 address);
	static sint32 ReadByte_SIC(void *thisptr0, uint32 address);
	static bool WriteByte_SIC(void *thisptr0, uint32 address, uint8 value);
	static sint32 DebugReadByte_TheCart(void *thisptr0, uint32 address);
	static sint32 ReadByte_TheCart(void *thisptr0, uint32 address);
	static bool WriteByte_TheCart(void *thisptr0, uint32 address, uint8 value);
	static sint32 DebugReadByte_MegaCart3(void *thisptr0, uint32 address);
	static sint32 ReadByte_MegaCart3(void *thisptr0, uint32 address);
	static bool WriteByte_MegaCart3(void *thisptr0, uint32 address, uint8 value);
	static sint32 DebugReadByte_MaxFlash(void *thisptr0, uint32 address);
	static sint32 ReadByte_MaxFlash(void *thisptr0, uint32 address);
	static bool WriteByte_MaxFlash(void *thisptr0, uint32 address, uint8 value);
	static bool WriteByte_Corina1M(void *thisptr0, uint32 address, uint8 value);
	static bool WriteByte_Corina512K(void *thisptr0, uint32 address, uint8 value);
	static bool WriteByte_TelelinkII(void *thisptr0, uint32 address, uint8 value);

	static bool WriteByte_CCTL_Phoenix(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_Blizzard_32K(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_Blizzard_32K(void *thisptr0, uint32 address, uint8 value);

	template<uint8 T_Mask>
	static bool WriteByte_CCTL_AddressToBank(void *thisptr0, uint32 address, uint8 value);

	template<uint8 T_Mask>
	static sint32 ReadByte_CCTL_AddressToBank_Switchable(void *thisptr0, uint32 address);

	template<uint8 T_Mask>
	static bool WriteByte_CCTL_AddressToBank_Switchable(void *thisptr0, uint32 address, uint8 value);

	template<uint8 T_Mask>
	static bool WriteByte_CCTL_DataToBank(void *thisptr0, uint32 address, uint8 value);

	static bool WriteByte_CCTL_XEGS_64K_Alt(void *thisptr0, uint32 address, uint8 value);

	template<uint8 T_Mask>
	static bool WriteByte_CCTL_DataToBank_Switchable(void *thisptr0, uint32 address, uint8 value);

	template<uint8 T_Mask>
	static sint32 ReadByte_CCTL_Williams(void *thisptr0, uint32 address);

	template<uint8 T_Mask>
	static bool WriteByte_CCTL_Williams(void *thisptr0, uint32 address, uint8 value);

	template<uint8 T_Address>
	static sint32 ReadByte_CCTL_SDX64(void *thisptr0, uint32 address);

	template<uint8 T_Address>
	static bool WriteByte_CCTL_SDX64(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_SDX128(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_SDX128(void *thisptr0, uint32 address, uint8 value);
	static sint32 ReadByte_CCTL_MaxFlash_128K(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_MaxFlash_128K(void *thisptr0, uint32 address, uint8 value);
	static sint32 ReadByte_CCTL_MaxFlash_128K_MyIDE(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_MaxFlash_128K_MyIDE(void *thisptr0, uint32 address, uint8 value);
	static sint32 ReadByte_CCTL_MaxFlash_1024K(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_MaxFlash_1024K(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_SIC(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_SIC(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_TheCart(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_TheCart(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_SC3D(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_SC3D(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_TelelinkII(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_TelelinkII(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_OSS_034M(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_OSS_034M(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_OSS_043M(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_OSS_043M(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_OSS_M091(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_OSS_M091(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_OSS_8K(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_OSS_8K(void *thisptr0, uint32 address, uint8 value);
	
	static bool WriteByte_CCTL_Corina(void *thisptr0, uint32 address, uint8 value);
	
	static bool WriteByte_CCTL_AST_32K(void *thisptr0, uint32 address, uint8 value);
	
	static sint32 ReadByte_CCTL_Turbosoft_64K(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_Turbosoft_64K(void *thisptr0, uint32 address, uint8 value);
	static sint32 ReadByte_CCTL_Turbosoft_128K(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_Turbosoft_128K(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_5200_64K_32KBanks(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_5200_64K_32KBanks(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_5200_512K_32KBanks(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_5200_512K_32KBanks(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_MicroCalc(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_MicroCalc(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_MegaCart3(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_MegaCart3(void *thisptr0, uint32 address, uint8 value);

	static sint32 ReadByte_CCTL_aDawliah_32K(void *thisptr0, uint32 address);
	static sint32 ReadByte_CCTL_aDawliah_64K(void *thisptr0, uint32 address);
	static bool WriteByte_CCTL_aDawliah_32K(void *thisptr0, uint32 address, uint8 value);
	static bool WriteByte_CCTL_aDawliah_64K(void *thisptr0, uint32 address, uint8 value);

	void InitFromImage();
	void InitMemoryLayers();
	void ShutdownMemoryLayers();
	void SetCartBank(int bank);
	void SetCartBank2(int bank);
	void UpdateCartBank();
	void UpdateCartBank2();
	void UpdateLayerBuses();
	void UpdateLayerMasks();
	void UpdateTheCartBanking();
	void UpdateTheCart();

	void InitDebugBankMap();
	void ResetDebugBankMap();

	ATCartridgeMode mCartMode;
	int	mCartBank;
	int	mCartBank2;
	uint32 mCartSizeMask;
	int	mInitialCartBank;
	int	mInitialCartBank2;
	int mBasePriority;
	bool mbDirty;
	bool mbRD4Gate;
	bool mbRD5Gate;
	bool mbCCTLGate;
	bool mbFastBus;
	IATUIRenderer *mpUIRenderer;
	ATMemoryManager *mpMemMan;
	ATScheduler *mpScheduler;
	IATDeviceCartridgePort *mpCartridgePort = nullptr;
	uint32 mCartId = 0;

	LayerEnables mLayerEnablesFixedBank1;
	LayerEnables mLayerEnablesFixedBank2;
	LayerEnables mLayerEnablesVarBank1;
	LayerEnables mLayerEnablesVarBank2;
	LayerEnables mLayerEnablesSpec1;
	LayerEnables mLayerEnablesSpec2;

	ATMemoryLayer *mpMemLayerFixedBank1;
	ATMemoryLayer *mpMemLayerFixedBank2;
	ATMemoryLayer *mpMemLayerVarBank1;
	ATMemoryLayer *mpMemLayerVarBank2;
	ATMemoryLayer *mpMemLayerSpec1;
	ATMemoryLayer *mpMemLayerSpec2;
	ATMemoryLayer *mpMemLayerControl;

	ATFlashEmulator mFlashEmu;
	ATFlashEmulator mFlashEmu2;
	ATEEPROMEmulator mEEPROM;

	union {
		uint8	mSC3D[4];
		uint8	mTheCartRegs[9];
	};

	uint8 *mpROM;
	uint32	mCartSize;

	vdfastvector<uint8> mCARTRAM;

	vdrefptr<IATCartridgeImage> mpImage;

	vdblock<sint16> mTheCartBankInfo;
	bool mbTheCartBankByAddress;
	uint8 mTheCartOSSBank;
	uint8 mTheCartSICEnables;
	uint16 mTheCartBankMask;
	bool mbTheCartConfigLock;

	enum TheCartBankMode {
		kTheCartBankMode_Disabled,
		kTheCartBankMode_8K,
		kTheCartBankMode_16K,
		kTheCartBankMode_Flexi,
		kTheCartBankMode_8KFixed_8K,
		kTheCartBankMode_OSS,		// 4K fixed + 4K variable
		kTheCartBankMode_SIC		// 16K, independently switchable halves
	};

	TheCartBankMode mTheCartBankMode;

	// Map from bank and A10-A13 to raw cartridge bank offset.
	sint32 mDebugBankMap[256][4];
};

bool ATIsCartridgeModeHWCompatible(ATCartridgeMode cartmode, int hwmode);

#endif	// f_AT_CARTRIDGE_H
