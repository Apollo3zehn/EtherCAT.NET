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
        protected int* _memoryMapping = null;

        public DigitalIn(SlaveInfo slave)
        {
            _slavePdos = slave.DynamicData.Pdos;
            _nofSlavePdos = _slavePdos.Count;

            if (_nofSlavePdos == 0)
                throw new Exception($"No slave PDOs present.");

            var channel1Pdo = _slavePdos[0];
            var variableCh1 = channel1Pdo.Variables.First();
            // get memory address of outputs
            _memoryMapping = (int*)variableCh1.DataPtr.ToPointer();
        }

        /// <summary>
        /// Returns digital value for a specific channel. 
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>
        /// <returns>Channel state, true: High, false: Low./returns>
        public bool GetChannel(int channel)
        {
            bool channelSet = false;

            if (ValidateChannel(channel))
            {
                // get slave variable in order to set bit offset
                SlaveVariable slaveVariable = _slavePdos[channel - 1].Variables.First();
                int bitOffset = slaveVariable.BitOffset;

                int channelInput = _memoryMapping[0] & (1 << bitOffset);
                channelSet = BitConverter.ToBoolean(channelInput.ToByteArray(), 0);
            }

            return channelSet;
        }

        /// <summary>
        /// Validates the channel index.
        /// </summary>
        /// <param name="channel">Channel index 1 - n.</param>
        /// <returns>True if channel index is valid, false otherwise</returns>
        protected bool ValidateChannel(int channel)
        {
            if (channel <= 0 || channel > _nofSlavePdos)
            {
                return false;
            }

            return true;
        }
    }
}
