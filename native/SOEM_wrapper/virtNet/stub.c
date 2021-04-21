#include <assert.h>
#include "virt_net.h"


int create_virtual_network_device(char *interfaceName, char* interfaceSet)
{
    assert("Not implemented for current platform");
    return -1;
}

void close_virtual_network_device(int deviceId)
{
    assert("Not implemented for current platform");
}

long read_virtual_network_device(void* buffer, size_t bufferSize, int deviceId)
{
    assert("Not implemented for current platform");
    return -1;
}

long write_virtual_network_device(void* buffer, size_t bufferSize, int deviceId)
{
    assert("Not implemented for current platform");
    return -1;
}