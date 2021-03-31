#ifndef VIRT_NET
#define VIRT_NET

#ifdef __cplusplus
extern "C" {
#endif

bool create_virtual_network_device(char *interface, char* interfaceSet);

void close_virtual_network_device();

long read_virtual_network_device(void* buffer, size_t bufferSize);

long write_virtual_network_device(void* buffer, size_t bufferSize);

#ifdef __cplusplus
}
#endif


#endif