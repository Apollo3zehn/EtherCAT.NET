/** \file
 * \brief
 * TAP virtual linux network device.
 *
 * TAP interfaces are a software-only interfaces, meaning that they exists only in the kernel and, unlike 
 * regular network interfaces, they are not connected to a physical hardware component. If the kernel network
 * stack is sending data the TAP file descriptor can be used by userspace program to read the data for further
 * processing and then sending it "to the wire". In the same way, if userspace program writes to the TAP device,
 * the data will appear as input to the TAP interface and is forwarded to the kernel network stack. It would 
 * look like the TAP interface is receiving data "from the wire".
 * 
 * For EoE the TAP device is used to read data from the network stack and send it to a EoE slave. The received
 * data from a EoE slave is written to the TAP interface which injects the data to the network stack.
 * 
 * Commands for creating a TAP interface. 
 * Parameters for this example:  Name - tap0, ip - 169.254.90.115, network mask - 255.255.0.0
 * 
 * sudo ip tuntap add name tap0 mode tap
 * sudo ip addr add 169.254.90.115 dev tap0
 * sudo ifconfig tap0 netmask 255.255.0.0
 * sudo ip link set dev tap0 up
 * 
 */

#define _GNU_SOURCE
#include <string.h>
#include <sys/ioctl.h>
#include <fcntl.h>
#include <unistd.h>
#include <linux/if.h>
#include <linux/if_tun.h>
#include <stdbool.h>
#include <stdlib.h>
#include <stdio.h>
#include "virt_dev.h"


/*
 *  Create virtual TAP network device.
 *
 *  interfaceName: Virtual network interface name. If interface is '\0', the kernel 
 *  will try to create the first available interface (eg, tap0, tap1 .... tapn).
 *  interfaceSet [out]: Virtual network interface name set by kernel.
 *
 *  returns: File descriptor of virtual network interface, -1 if it fails.
 */
int create_virtual_network_device(char *interfaceName, char* interfaceSet)
{
    int fd = -1;
    char* clonedev = "/dev/net/tun";
    if( (fd = open(clonedev, O_RDWR | O_NONBLOCK )) < 0 ) 
        return -1;
    
    struct ifreq ifr;
    memset(&ifr, 0, sizeof(ifr));
    ifr.ifr_flags = IFF_TAP | IFF_NO_PI;

    if(*interfaceName)
        strncpy(ifr.ifr_name, interfaceName, IFNAMSIZ);

    int err = 0;
    if( (err = ioctl(fd, TUNSETIFF, (void*)&ifr)) < 0 )
    {
        close(fd);
        return -1;
    }

    if(interfaceSet != NULL)
        strcpy(interfaceSet, ifr.ifr_name);

    return fd;
}
/*
 *  Close virtual TAP network device.
 *  deviceId: Virtual network device Id.
 *
 */
void close_virtual_network_device(int deviceId)
{
    close(deviceId);
}

/*
 *  Read data from virtual TAP network device.
 *
 *  buffer: Buffer of read data.
 *  bufferSize: Buffer size of buffer.
 *  deviceId: Virtual network device Id.
 *
 *  returns: Number of bytes read, -1 if error occurred.
 */
long read_virtual_network_device(void* buffer, size_t bufferSize, int deviceId)
{
    return read(deviceId, buffer, bufferSize);
}

/*
 *  Write data to virtual TAP network device.
 *
 *  buffer: Buffer of data to write.
 *  bufferSize: Buffer size of buffer.
 *  deviceId: Virtual network device Id.
 *
 *  returns: Number of bytes written, -1 if error occurred.
 */

long write_virtual_network_device(void* buffer, size_t bufferSize, int deviceId)
{
    return write(deviceId, buffer, bufferSize);
}


/** \file
 * \brief
 * Virtual linux pseudo terminal pair. A virtual master / slave terminal pair is used for 
 * serial communication. Any data written to the pseudo terminal master is received by the
 * application connected to the slave terminal. Any data written to the slave terminal is 
 * received by the pseudo terminal master.
 *
 */


/*
 *  Create virtual master / slave terminal pair.
 *
 *  slaveTerminal [out]: Path of pseudo terminal slave device, which is created in 
 *  the /dev/pts directory. Any application connected to this slave device is able
 *  to communicate with a serial EterCAT slave device. 
 *
 *  returns: File descriptor of pseudo terminal master (device Id), -1 if it fails.
 */
int create_virtual_serial_port(char* slaveTerminal)
{
    int fd = -1;
    const char* device = "/dev/ptmx";
    // get a file descriptor for a pseudoterminal master (PTM), and a 
    // pseudoterminal slave (PTS) device is created in the /dev/pts directory.
    if( (fd = open(device, O_RDWR | O_NOCTTY | O_NONBLOCK)) < 0 ) 
        return -1;
    
    // grant access to the slave pseudoterminal 
    int err = 0;
    if( (err = grantpt(fd)) < 0 )
    {
        close(fd);
        return -1;
    }

    // unlock a pseudoterminal master / slave pair 
    if( (err = unlockpt(fd)) < 0 )
    {
        close(fd);
        return -1;
    }
    
    // get the name of the slave pseudoterminal 
    char* slaveTermName = ptsname(fd);

    if(slaveTerminal != NULL)
        strcpy(slaveTerminal, slaveTermName);
    
    return fd;
}

/*
 *  Close pseudoterminal master device.
 *  deviceId: File descriptor of pseudo terminal master (device Id).
 *
 */
void close_virtual_serial_port(int deviceId)
{
    close(deviceId);
}

/*
 *  Read data from pseudoterminal master device.
 *
 *  buffer: Buffer of read data.
 *  bufferSize: Buffer size of buffer.
 *  deviceId: File descriptor of pseudo terminal master (device Id).
 *
 *  returns: Number of bytes read, -1 if error occurred.
 */
long read_virtual_serial_port(void* buffer, size_t bufferSize, int deviceId)
{
    return read(deviceId, buffer, bufferSize);
}

/*
 *  Write data to pseudoterminal master device.
 *
 *  buffer: Buffer of read data.
 *  bufferSize: Buffer size of buffer.
 *  deviceId: File descriptor of pseudo terminal master (device Id).
 *
 *  returns: Number of bytes read, -1 if error occurred.
 */
long write_virtual_serial_port(void* buffer, size_t bufferSize, int deviceId)
{
    return write(deviceId, buffer, bufferSize);
}