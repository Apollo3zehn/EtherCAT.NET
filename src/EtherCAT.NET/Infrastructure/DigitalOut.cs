using System;
using System.Linq;

namespace EtherCAT.NET.Infrastructure
{
    /// <summary>
    /// This class provides functions to simplify the control for any
    /// EC slave that has digital outputs. 
    /// </summary>
    public unsafe class DigitalOut : DigitalIn
    {       
        public DigitalOut(SlaveInfo slave) : base(slave)
        {
        }

        /// <summary>
        /// Sets digital output value for a specific channel. 
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>  
        /// <param name="value">Value of channel output, true: High, false: Low.</param>
        /// <returns></returns>
        public bool SetChannel(int channel, bool value)
        {
            bool validChannel = ValidateChannel(channel);
            if (validChannel)
            {
                // get slave variable in order to set bit offset
                SlaveVariable slaveVariable = _slavePdos[channel - 1].Variables.First();
                int bitOffset = slaveVariable.BitOffset;

                if (value)
                    // set channel bit
                    _memoryMapping[0] |= 1 << bitOffset;
                else
                    // clear channel bit
                    _memoryMapping[0] &= ~(1 << bitOffset);  
            }

            return validChannel;
        }

        /// <summary>
        /// Toggles digital output value for a specific channel. 
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>
        /// <returns></returns>
        public bool ToggleChannel(int channel)
        {
            bool validChannel = ValidateChannel(channel);
            if (validChannel)
            {
                // get slave variable in order to set bit offset
                SlaveVariable slaveVariable = _slavePdos[channel - 1].Variables.First();
                _memoryMapping[0] ^= 1 << slaveVariable.BitOffset;
            }

            return validChannel;
        }
    }
}
