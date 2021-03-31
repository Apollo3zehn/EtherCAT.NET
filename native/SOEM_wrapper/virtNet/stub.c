#include <assert.h>
#include "virt_net.h"


bool create_virtual_network_device(char *interfaceName, char* interfaceSet)
{
    assert("Not implemented for current platform");
    return false;
}

void close_virtual_network_device()
{
    assert("Not implemented for current platform");
}

long read_virtual_network_device(void* buffer, size_t bufferSize)
{
    assert("Not implemented for current platform");
    return -1;
}

long write_virtual_network_device(void* buffer, size_t bufferSize)
{
    assert("Not implemented for current platform");
    return -1;
}