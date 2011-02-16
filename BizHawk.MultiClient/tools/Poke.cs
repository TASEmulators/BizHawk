using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    /// <summary>
    /// This class prompts the user for an address & value and writes to memory
    /// If supplied a Watch object it will use it to generate default values
    /// </summary>
    class Poke
    {
        Watch w = new Watch();

        public bool PokeAddress(Watch watch)
        {
            w = watch;
            return PokeWatch();
        }

        public bool PokeAddress()
        {
            return PokeWatch();
        }

        private bool PokeWatch()
        {
            InputPrompt i = new InputPrompt();
            i.Text = "Poke address";
            i.ShowDialog();
            //Prompt user
            //Attempt to poke
            //If use cancels or some failure, return false, else turn true
            return true;
        }
    }
}
