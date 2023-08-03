using System;
using System.Collections.Generic;
using System.Linq;

namespace EtherCAT.NET.Infrastructure
{
    /// <summary>
    /// This class provides functions to simplify the control for any
    /// EC slave that has digital inputs. 
    /// </summary>
    public unsafe class DigitalIn
    {
        protected IList<SlavePdo> _slavePdos;
        protected int _nofSlavePdos;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalIn"/> class.
        /// </summary>
        /// <param name="slave">The digital input slave.</param>
        public DigitalIn(SlaveInfo slave)
        {
            _slavePdos = slave.DynamicData.Pdos;
            _nofSlavePdos = _slavePdos.Count;

            if (_nofSlavePdos == 0)
                throw new Exception($"No slave PDOs present.");
        }

        /// <summary>
        /// Returns digital value for a specific channel. 
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>
        /// <returns>Channel state, true: High, false: Low./returns>
        public bool GetChannel(int channel)
        {
            var isChannelSet = false;

            if (ValidateChannel(channel))
            {
                // get slave variable in order to set bit offset
                var slaveVariable = _slavePdos[channel - 1].Variables.First();
                var bitOffset = slaveVariable.BitOffset;
                var memptr = (int*)slaveVariable.DataPtr;
                var channelInput = memptr[0] & (1 << bitOffset);

                isChannelSet = BitConverter.ToBoolean(channelInput.ToByteArray(), bitOffset / 8);
            }

            return isChannelSet;
        }

        /// <summary>
        /// Validates the channel index.
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>
        /// <returns>True if channel index is valid, false otherwise</returns>
        protected bool ValidateChannel(int channel)
        {
            if (channel <= 0 || channel > _nofSlavePdos)
                return false;

            return true;
        }
    }
}
