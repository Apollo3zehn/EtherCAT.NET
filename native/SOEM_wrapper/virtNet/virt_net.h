#ifndef VIRT_NET
#define VIRT_NET

#include <stdlib.h>
#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

int create_virtual_network_device(char* interfaceName, char* interfaceSet);

void close_virtual_network_device(int deviceId);

long read_virtual_network_device(void* buffer, size_t bufferSize, int deviceId);

long write_virtual_network_device(void* buffer, size_t bufferSize, int deviceId);

#ifdef __cplusplus
}
#endif


#endif