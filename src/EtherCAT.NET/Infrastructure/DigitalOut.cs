using System.Linq;

namespace EtherCAT.NET.Infrastructure
{
    /// <summary>
    /// This class provides functions to simplify the control for any
    /// EC slave that has digital outputs. 
    /// </summary>
    public unsafe class DigitalOut : DigitalIn
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalOut"/> class.
        /// </summary>
        /// <param name="slave">The digital output slave.</param>
        public DigitalOut(SlaveInfo slave) : base(slave)
        {
            //
        }

        /// <summary>
        /// Sets digital output value for a specific channel. 
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>  
        /// <param name="value">Value of channel output, true: High, false: Low.</param>
        /// <returns></returns>
        public bool SetChannel(int channel, bool value)
        {
            var validChannel = ValidateChannel(channel);

            if (validChannel)
            {
                // get slave variable in order to set bit offset
                var slaveVariable = _slavePdos[channel - 1].Variables.First();
                var bitOffset = slaveVariable.BitOffset;
                var memptr = (int*)slaveVariable.DataPtr;

                if (value)
                    // set channel bit
                    memptr[0] |= 1 << bitOffset;

                else
                    // clear channel bit
                    memptr[0] &= ~(1 << bitOffset);  
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
            var validChannel = ValidateChannel(channel);

            if (validChannel)
            {
                // get slave variable in order to set bit offset
                var slaveVariable = _slavePdos[channel - 1].Variables.First();
                var memptr = (int*)slaveVariable.DataPtr;

                memptr[0] ^= 1 << slaveVariable.BitOffset;
            }

            return validChannel;
        }
    }
}
