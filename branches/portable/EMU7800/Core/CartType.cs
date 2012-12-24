/*
 * CartType.cs
 *
 * Defines the set of all known Game Cartridges.
 *
 * 2010 Mike Murphy
 *
 */
namespace EMU7800.Core
{
    public enum CartType
    {
        None,
        A2K,          // Atari 2kb cart
        TV8K,         // Tigervision 8kb bankswitched cart
        A4K,          // Atari 4kb cart
        PB8K,         // Parker Brothers 8kb bankswitched cart
        MN16K,        // M-Network 16kb bankswitched cart
        A16K,         // Atari 16kb bankswitched cart
        A16KR,        // Atari 16kb bankswitched cart w/128 bytes RAM
        A8K,          // Atari 8KB bankswitched cart
        A8KR,         // Atari 8KB bankswitched cart w/128 bytes RAM
        A32K,         // Atari 32KB bankswitched cart
        A32KR,        // Atari 32KB bankswitched cart w/128 bytes RAM
        CBS12K,       // CBS' RAM Plus bankswitched cart w/256 bytes RAM
        DC8K,         // Special Activision cart (Robot Tank and Decathlon)
        DPC,          // Pitfall II DPC cart
        M32N12K,      // 32N1 Multicart: 32x2KB
        A7808,        // Atari7800 non-bankswitched 8KB cart
        A7816,        // Atari7800 non-bankswitched 16KB cart
        A7832,        // Atari7800 non-bankswitched 32KB cart
        A7832P,       // Atari7800 non-bankswitched 32KB cart w/Pokey
        A7848,        // Atari7800 non-bankswitched 48KB cart
        A78SG,        // Atari7800 SuperGame cart
        A78SGP,       // Atari7800 SuperGame cart w/Pokey
        A78SGR,       // Atari7800 SuperGame cart w/RAM
        A78S9,        // Atari7800 SuperGame cart, nine banks
        A78S4,        // Atari7800 SuperGame cart, four banks
        A78S4R,       // Atari7800 SuperGame cart, four banks, w/RAM
        A78AB,        // F18 Hornet cart (Absolute)
        A78AC,        // Double dragon cart (Activision)
    };
}
