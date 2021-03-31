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

#include <string.h>
#include <sys/ioctl.h>
#include <fcntl.h>
#include <unistd.h>
#include <linux/if.h>
#include <linux/if_tun.h>
#include <stdbool.h>
#include <stdlib.h>
#include <stdio.h>
#include "virt_net.h"

int fd = -1;

/*
 *  Create virtual TAP network device.
 *
 *  interfaceName: Virtual network interface name. If interface is '\0', the kernel 
 *  will try to create the first available interface (eg, tap0, tap1 .... tapn).
 *  interfaceSet: Virtual network interface name set by kernel.
 *
 *  returns: True if operation was successful, false otherwise.
 */
bool create_virtual_network_device(char *interfaceName, char* interfaceSet)
{
    char* clonedev = "/dev/net/tun";
    if( (fd = open(clonedev, O_RDWR | O_NONBLOCK )) < 0 ) 
        return false;
    
    struct ifreq ifr;
    memset(&ifr, 0, sizeof(ifr));
    ifr.ifr_flags = IFF_TAP | IFF_NO_PI;

    if(*interfaceName)
        strncpy(ifr.ifr_name, interfaceName, IFNAMSIZ);

    int err = 0;
    if( (err = ioctl(fd, TUNSETIFF, (void*)&ifr)) < 0 )
    {
        close(fd);
        return false;
    }

    if(interfaceSet != NULL)
        strcpy(interfaceSet, ifr.ifr_name);

    return true;
}
/*
 *  Close virtual TAP network device.
 *
 */
void close_virtual_network_device()
{
    if(fd != -1)
        close(fd);
}

/*
 *  Read data from virtual TAP network device.
 *
 *  buffer: Buffer of read data.
 *  bufferSize: Buffer size of buffer.
 *
 *  returns: Number of bytes read, -1 if error occurred.
 */
long read_virtual_network_device(void* buffer, size_t bufferSize)
{
    if(fd != -1)
        return read(fd, buffer, bufferSize);
    else
        return -1;
}

/*
 *  Write data to virtual TAP network device.
 *
 *  buffer: Buffer of data to write.
 *  bufferSize: Buffer size of buffer.
 *
 *  returns: Number of bytes written, -1 if error occurred.
 */

long write_virtual_network_device(void* buffer, size_t bufferSize)
{
    if(fd != -1)
        return write(fd, buffer, bufferSize);
    else
        return -1;
}