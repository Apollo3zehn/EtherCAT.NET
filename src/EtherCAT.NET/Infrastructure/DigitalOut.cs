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
        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalOut"/> class.
        /// </summary>
        /// <param name="slave">The digital output slave.</param>
        public DigitalOut(SlaveInfo slave) : base(slave)
        {
            //
        }
        
        /// <summary>
        /// Gets the Index of Matching Slave Variable Channel.
        /// </summary>
        /// <param name="channel"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private (int bitOffset, IntPtr memptr) GetChannelInfo(int channel)
        {
            // Calculate total number of variables
            int totalVariables = _slavePdos.Sum(pdo => pdo.Variables.Count);
            if (channel < 1 || channel > totalVariables)
            {
                throw new ArgumentOutOfRangeException(nameof(channel), "Channel is out of range.");
            }

            int remainingChannels = channel - 1;
            int pdoIndex = 0;

            // Find the appropriate PDO and variable index
            while (remainingChannels >= _slavePdos[pdoIndex].Variables.Count)
            {
                remainingChannels -= _slavePdos[pdoIndex].Variables.Count;
                pdoIndex++;
            }

            int variableIndex = remainingChannels;

            var pdo = _slavePdos[pdoIndex];
            var variable = pdo.Variables[variableIndex];
            var bitOffset = variable.BitOffset;
            var memptr = new IntPtr((void*)variable.DataPtr);
            
            return (bitOffset, memptr);
        }

        /// <summary>
        /// Sets digital output value for a specific channel. 
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>  
        /// <param name="value">Value of channel output, true: High, false: Low.</param>
        /// <returns></returns>
        public bool SetChannel(int channel, bool value)
        {
            var (bitOffset, memptr) = GetChannelInfo(channel);

            int* memptrInt = (int*)memptr.ToPointer();

            
            if (value)
                // set channel bit
                memptrInt[0] |= 1 << bitOffset;
            else
                // clear channel bit
                memptrInt[0] &= ~(1 << bitOffset);

            return true;
        }

        /// <summary>
        /// Toggles digital output value for a specific channel. 
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>
        /// <returns></returns>
        public bool ToggleChannel(int channel)
        {
            var (bitOffset, memptr) = GetChannelInfo(channel);

            int* memptrInt = (int*)memptr.ToPointer();

            // Toggle channel bit
            memptrInt[0] ^= 1 << bitOffset;

            return true;
        }
    }
}