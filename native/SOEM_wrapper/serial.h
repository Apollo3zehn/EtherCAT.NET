#ifndef SERIAL
#define SERIAL

#include <stdint.h>
#include <stdbool.h>

typedef void (*rx_callback_t)(uint16_t slave, const uint8_t* buffer, int datasize, bool rx_fifo_full);

bool init_serial(uint16_t slave, bool multibyte_ctrl_status);
bool close_serial(uint16_t slave);
void register_rx_callback(uint16_t slave, rx_callback_t callback);
bool set_tx_buffer(uint16_t slave, uint8_t* tx_buffer, int datasize);
bool get_rx_buffer(uint16_t slave, uint8_t* rx_buffer, int* datasize);
void update_serial(uint16_t slave, uint8_t* tx_data, uint8_t* rx_data);

#endif