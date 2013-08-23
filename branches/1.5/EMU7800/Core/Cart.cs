/*
 * Cart.cs
 *
 * An abstraction of a game cart.  Attributable to Kevin Horton's Bankswitching
 * document, the Stella source code, and Eckhard Stolberg's 7800 Bankswitching Guide. 
 *
 * Copyright © 2003, 2004, 2010, 2011 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    public abstract class Cart : IDevice
    {
        static int _multicartBankSelector;

        protected MachineBase M { get; set; }
        protected internal byte[] ROM { get; set; }

        #region IDevice Members

        public virtual void Reset() { }

        public abstract byte this[ushort addr] { get; set; }

        #endregion

        public virtual void Attach(MachineBase m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            if (M != null && M != m)
                throw new InvalidOperationException("Cart already attached to a different machine.");
            M = m;
        }

        public virtual void StartFrame()
        {
        }

        public virtual void EndFrame()
        {
        }

        protected internal virtual bool RequestSnooping
        {
            get { return false; }
        }

        /// <summary>
        /// Creates an instance of the specified cart.
        /// </summary>
        /// <param name="romBytes"></param>
        /// <param name="cartType"></param>
        /// <exception cref="Emu7800Exception">Specified CartType is unexpected.</exception>
        public static Cart Create(byte[] romBytes, CartType cartType)
        {
            if (cartType == CartType.None)
            {
                switch (romBytes.Length)
                {
                    case 2048:
                        cartType = CartType.A2K;
                        break;
                    case 4096:
                        cartType = CartType.A4K;
                        break;
                    case 8192:
                        cartType = CartType.A8K;
                        break;
                    case 16384:
                        cartType = CartType.A16K;
                        break;
                    case 32768:
                        cartType = CartType.A32K;
                        break;
                }
            }

            switch (cartType)
            {
                case CartType.A2K:     return new CartA2K(romBytes);
                case CartType.A4K:     return new CartA4K(romBytes);
                case CartType.A8K:     return new CartA8K(romBytes);
                case CartType.A8KR:    return new CartA8KR(romBytes);
                case CartType.A16K:    return new CartA16K(romBytes);
                case CartType.A16KR:   return new CartA16KR(romBytes);
                case CartType.DC8K:    return new CartDC8K(romBytes);
                case CartType.PB8K:    return new CartPB8K(romBytes);
                case CartType.TV8K:    return new CartTV8K(romBytes);
                case CartType.CBS12K:  return new CartCBS12K(romBytes);
                case CartType.A32K:    return new CartA32K(romBytes);
                case CartType.A32KR:   return new CartA32KR(romBytes);
                case CartType.MN16K:   return new CartMN16K(romBytes);
                case CartType.DPC:     return new CartDPC(romBytes);
                case CartType.M32N12K: return new CartA2K(romBytes, _multicartBankSelector++);
                case CartType.A7808:   return new Cart7808(romBytes);
                case CartType.A7816:   return new Cart7816(romBytes);
                case CartType.A7832P:  return new Cart7832P(romBytes);
                case CartType.A7832:   return new Cart7832(romBytes);
                case CartType.A7848:   return new Cart7848(romBytes);
                case CartType.A78SGP:  return new Cart78SGP(romBytes);
                case CartType.A78SG:   return new Cart78SG(romBytes, false);
                case CartType.A78SGR:  return new Cart78SG(romBytes, true);
                case CartType.A78S9:   return new Cart78S9(romBytes);
                case CartType.A78S4:   return new Cart78S4(romBytes, false);
                case CartType.A78S4R:  return new Cart78S4(romBytes, true);
                case CartType.A78AB:   return new Cart78AB(romBytes);
                case CartType.A78AC:   return new Cart78AC(romBytes);
                default:
                    throw new Emu7800Exception("Unexpected CartType: " + cartType);
            }
        }

        protected void LoadRom(byte[] romBytes, int multicartBankSize, int multicartBankNo)
        {
            if (romBytes == null)
                throw new ArgumentNullException("romBytes");

            ROM = new byte[multicartBankSize];
            Buffer.BlockCopy(romBytes, multicartBankSize*multicartBankNo, ROM, 0, multicartBankSize);
        }

        protected void LoadRom(byte[] romBytes, int minSize)
        {
            if (romBytes == null)
                throw new ArgumentNullException("romBytes");

            if (romBytes.Length >= minSize)
            {
                ROM = romBytes;
            }
            else
            {
                ROM = new byte[minSize];
                Buffer.BlockCopy(romBytes, 0, ROM, 0, romBytes.Length);
            }
        }

        protected void LoadRom(byte[] romBytes)
        {
            LoadRom(romBytes, romBytes.Length);
        }

        protected Cart()
        {
        }

        #region Serialization Members

        protected Cart(DeserializationContext input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            input.CheckVersion(1);
        }

        public virtual void GetObjectData(SerializationContext output)
        {
            if (output == null)
                throw new ArgumentNullException("output");

            output.WriteVersion(1);
        }

        #endregion
    }
}