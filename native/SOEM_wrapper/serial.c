/** \file
 * \brief
 * Implementation of serial communication for Beckhoff EL6021.
 */

#include "serial.h"
#include <stdlib.h>
#include <string.h>


#ifndef PACKED

#if defined(__GNUC__) || defined(__ARMEL__) || defined(__APPLE__) 
#define PACKED_BEGIN
#define PACKED   __attribute__((__packed__))
#define PACKED_END
#elif _WIN32
#define PACKED_BEGIN __pragma(pack(push, 1))
#define PACKED
#define PACKED_END __pragma(pack(pop))
#endif

#endif


// Tx control bitmap
PACKED_BEGIN
typedef struct PACKED
{
    uint8_t transmit_request : 1;
    uint8_t receive_accepted : 1;
    uint8_t init_request : 1;
    uint8_t send_continuous : 1;
    uint8_t unused_bit_4 : 1;
    uint8_t unused_bit_5 : 1;
    uint8_t unused_bit_6 : 1;
    uint8_t unused_bit_7 : 1;
    uint8_t output_length;

} tx_control_t;
PACKED_END

// Rx status bitmap
PACKED_BEGIN
typedef struct PACKED
{
    uint8_t transmit_accepted : 1;
    uint8_t receive_request : 1;
    uint8_t init_accepted : 1;
    uint8_t buffer_full : 1;
    uint8_t parity_error : 1;
    uint8_t framing_error : 1;
    uint8_t overrun_error : 1;
    uint8_t unused_bit_7 : 1;
    uint8_t input_length;

} rx_status_t;
PACKED_END


static const int RX_CACHE_SIZE = 1024;
static const int TX_CACHE_SIZE = 1024;
static const uint8_t MAX_TX_SIZE = 22;

#define MAX_SLAVES   255

struct sm_state_data_t;
typedef void (*sm_state_func_t)(struct sm_state_data_t* next_state);


// State data
struct sm_state_data_t
{
    uint16_t slave;
    bool slave_set;

    tx_control_t* tx_control;
    uint8_t* tx_buffer;
    uint8_t* tx_cache;

    int tx_cache_head;
    int tx_cache_tail;
    bool tx_cache_full;
    bool tx_cache_empty;

    rx_status_t* rx_status;
    uint8_t* rx_buffer;
    uint8_t* rx_cache;
    int rx_offset;
    bool rx_updated;

    sm_state_func_t next_state;
    sm_state_func_t current_state;

    bool initialized;

    uint8_t receive_request_bit;
    uint8_t transmit_accepted_bit;

    bool receive_request;
    bool transmit_request;

    void (*rx_callback)(uint16_t slave, uint8_t* buffer, int datasize);
};

// State functions
static void state_init_enter(struct sm_state_data_t* data);
static void state_init_run(struct sm_state_data_t* data);
static void state_idle_run(struct sm_state_data_t* data);
static void state_receive_run(struct sm_state_data_t* data);
static void state_transmit_run(struct sm_state_data_t* data);
static void state_wait_transmit_accepted(struct sm_state_data_t* data);
static void check_requests(struct sm_state_data_t* data);

// Static data containers
static int _mapping_index[MAX_SLAVES] = { 0 };
static struct sm_state_data_t _state_data[MAX_SLAVES] = { 0 };

// Tx ringbuffer functions
static void write_tx_cache(struct sm_state_data_t* data, uint8_t* buffer, int length);
static void read_tx_cache(struct sm_state_data_t* data, uint8_t* buffer, int length);
static void update_tx_cache_write(struct sm_state_data_t* data, int length);
static void update_tx_cache_read(struct sm_state_data_t* data, int length);
static int get_nof_tx_cache_elements(struct sm_state_data_t* data);
static bool is_tx_cache_empty(struct sm_state_data_t* data);

/*
 *  Assign slave number to state struct data entry.
 *
 *  slave: Slave number.
 *  returns: Pointer to sm_state_data_t struct.
 */
static struct sm_state_data_t* set_state(int slave)
{ 
    struct sm_state_data_t* state = NULL;
    for(int i = 0; i < MAX_SLAVES; i ++)
    {
        if(!_state_data[i].slave_set)
        {  
            _state_data[i].slave = slave;
            _state_data[i].slave_set = true; 
            state = &_state_data[i];
            _mapping_index[i] = slave;
            break;
        }
    }

    return state;
}

/*
 *  Get state struct data entry for specific slave number.
 *
 *  slave: Slave number.
 *  returns: Pointer to sm_state_data_t struct.
 */
static struct sm_state_data_t* get_state(int slave)
{
    struct sm_state_data_t* state = NULL;
    for(int i = 0; i < MAX_SLAVES; i ++)
    {
        if(_mapping_index[i] == slave)
        {
            state = &_state_data[i];
            break;
        }
    }

    return state;
}

/*
 *  Initialize state struct data entry.
 *
 *  state: Pointer to sm_state_data_t struct.
 */
static void init_state_data(struct sm_state_data_t* state)
{
    state->tx_control = NULL;
    state->tx_buffer = NULL;
    state->tx_cache_head = 0;
    state->tx_cache_tail = 0;
    state->tx_cache_full = false;
    state->rx_status = NULL;
    state->rx_buffer = NULL;
    state->rx_offset = 0;
    state->rx_updated = false;
    state->next_state = NULL;
    state->current_state = &state_init_enter;
    state->initialized = false;
    state->receive_request_bit = 0;
    state->transmit_accepted_bit = 0;
    state->receive_request = false;
    state->transmit_request = false;
    state->rx_callback = NULL;
}

/*
 *  Initialize state struct data entry and alloc buffer for tx/rx cache.
 *
 *  slave: Slave number.
 *  returns: True if successfully initialized, false otherwise.
 */
bool init_serial(uint16_t slave)
{
    struct sm_state_data_t* state = set_state(slave);
    if(state == NULL)
        return false;
    
    init_state_data(state);

    if(state->tx_cache == NULL)
        state->tx_cache = (uint8_t*)malloc(TX_CACHE_SIZE);

    if(state->rx_cache == NULL)    
        state->rx_cache = (uint8_t*)malloc(RX_CACHE_SIZE);

    return true;
}

/*
 *  Close state struct data entry and free buffer for tx/rx cache.
 *
 *  slave: Slave number.
 *  returns: True if successfully closed, false otherwise.
 */
bool close_serial(uint16_t slave)
{
    bool success = false;
    struct sm_state_data_t* state = get_state(slave);
    if(state != NULL)
    {
        state->slave_set = false;
        state->slave = 0;
        init_state_data(state);

        if(state->tx_cache != NULL)
        {
            free(state->tx_cache);
            state->tx_cache = NULL;
        }

        if(state->rx_cache != NULL)
        {    
            free(state->rx_cache);
            state->rx_cache = NULL;
        }

        for(int i = 0; i < MAX_SLAVES; i ++)
        {
            if(_mapping_index[i] == slave)
            {
                _mapping_index[i] = 0;
                break;
            }
        }

        success = true;
    }

    return success;
}

/*
 *  Register callback for rx buffer.
 *
 *  slave: Slave number.
 *  callack: Pointer to callback function.
 */
void register_rx_callback(uint16_t slave, void callback(uint16_t slave, uint8_t* buffer, int datasize))
{
    struct sm_state_data_t* state = get_state(slave);
    if(state != NULL)
    {
        state->rx_callback = callback;
    }
}

/*
 *  Set Tx buffer trasnmitted to slave.
 *
 *  slave: Slave number.
 *  tx_buffer: Tx buffer.
 *  datasize: Size of tx buffer.
 *
 *  returns: True if operation was successful, false otherwise.
 */
bool set_tx_buffer(uint16_t slave, uint8_t* tx_buffer, int datasize)
{
    bool success = false;
    struct sm_state_data_t* data = get_state(slave);
    
    if((data != NULL) && ((TX_CACHE_SIZE - get_nof_tx_cache_elements(data)) >= datasize))
    {
        write_tx_cache(data, tx_buffer, datasize);
        data->transmit_request = true;
        success = true;
    }
    
    return success;
}

/*
 *  Get Rx buffer received from slave.
 *
 *  slave: Slave number.
 *  rx_buffer: Rx buffer.
 *  datasize: Size of Rx buffer.
 *  returns: True if operation was successful, false otherwise.
 */
bool get_rx_buffer(uint16_t slave, uint8_t* rx_buffer, int* datasize)
{
    bool success = false;

    struct sm_state_data_t* state = get_state(slave);
    if(state != NULL && state->rx_updated)
    {
        if(*datasize >= state->rx_status->input_length)
        {
            *datasize = state->rx_status->input_length;
            memcpy(rx_buffer, state->rx_buffer, *datasize);
            state->rx_updated = false;
            success = true;
        }
    }

    return success;
}

/*
 *  Update serial handshake processing.
 *
 *  slave: Slave number.
 *  tx_data: Pointer to slaves output mapping.
 *  rx_data: Pointer to slaves input mapping.
 */
void update_serial(uint16_t slave, uint8_t* tx_data, uint8_t* rx_data)
{
    struct sm_state_data_t* state = get_state(slave);
    if(state != NULL)
    {
        state->tx_control = (tx_control_t *)tx_data;
        state->tx_buffer = tx_data + sizeof(uint16_t);

        state->rx_status = (rx_status_t *)rx_data;
        state->rx_buffer = rx_data + sizeof(uint16_t);

        state->current_state(state);
        state->current_state = state->next_state;
    }
}

/*
 *  Check if rx receive request occurred.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static void check_requests(struct sm_state_data_t* data)
{
    uint8_t current_receive_request = data->rx_status->receive_request;

    if (current_receive_request != data->receive_request_bit)
    {
        data->receive_request_bit = current_receive_request;
        data->receive_request = true;
    }
}

/*
 *  Init enter state function.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static void state_init_enter(struct sm_state_data_t* data)
{
    if (!data->initialized)
    {
        data->tx_control->init_request = 1;
        data->next_state = &state_init_run;
    }
    else
    {
        data->next_state = &state_idle_run;
    }
}

/*
 *  Init run state function.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static void state_init_run(struct sm_state_data_t* data)
{
    if ((data->tx_control->init_request == 1) && (data->rx_status->init_accepted == 1))
    {
        data->tx_control->init_request = 0;
    }
    else if ((data->tx_control->init_request == 0) && (data->rx_status->init_accepted == 0))
    {
        data->initialized = true;
        data->next_state = &state_idle_run;

        if (data->tx_control->receive_accepted != data->rx_status->receive_request)
        {
            data->tx_control->receive_accepted = data->rx_status->receive_request;
            data->receive_request_bit = data->rx_status->receive_request;
        }
    }
}

/*
 *  Idle run state function.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static void state_idle_run(struct sm_state_data_t* data)
{
    check_requests(data);

    // check if receive request occurred
    if (data->receive_request)
    {
        // call receive function immediately
        state_receive_run(data);
    }

    // check if tx request occurred
    if (data->transmit_request)
    {
        // call transmit function immediately
        state_transmit_run(data);
    }
}

/*
 *  Receive run state function.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static void state_receive_run(struct sm_state_data_t* data)
{
    uint8_t length = data->rx_status->input_length;
    if((data->rx_offset + length) > RX_CACHE_SIZE)
    {
        data->rx_offset = 0;
    }
    
    // receive data
    memcpy(data->rx_cache + data->rx_offset, data->rx_buffer, length);
    data->rx_offset += length;
    
    if(data->rx_callback != NULL)
    {
        data->rx_callback(data->slave, data->rx_buffer, length);
    }

    // toggle accepted bit
    data->tx_control->receive_accepted = !data->tx_control->receive_accepted;

    data->rx_updated = true;
    data->receive_request = false;
    data->next_state = &state_idle_run;
}

/*
 *  Transmit run state function.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static void state_transmit_run(struct sm_state_data_t* data)
{
    int tx_cache_elements = get_nof_tx_cache_elements(data);

    int data_length = 0;
    if (tx_cache_elements <= MAX_TX_SIZE)
        data_length = tx_cache_elements;
    else
        data_length = MAX_TX_SIZE;

    // fill tx buffer
    read_tx_cache(data, data->tx_buffer, data_length);
    data->tx_control->output_length = data_length;

    // store transmit accepted bit
    data->transmit_accepted_bit = data->rx_status->transmit_accepted;
    // toggle transmit request
    data->tx_control->transmit_request = !data->tx_control->transmit_request;
    // set wait for transmit state and wait for tx accepted
    data->next_state = &state_wait_transmit_accepted;
}

/*
 *  Transmit wait accepted state function.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static void state_wait_transmit_accepted(struct sm_state_data_t* data)
{
    if (data->transmit_accepted_bit != data->rx_status->transmit_accepted)
    {
        if (get_nof_tx_cache_elements(data) == 0)
        {
            data->transmit_request = false;
            data->next_state = &state_idle_run;     
        }
        else
        {
            // call transmit function immediately
            state_transmit_run(data);
        }
    }
    else
    {
        // stay in wait_transmit_accepted state
        data->next_state = &state_wait_transmit_accepted;
    }
}

/*
 *  Write buffer to tx cache.
 *
 *  data: Pointer to sm_state_data_t struct.
 *  buffer: Pointer to buffer.
 *  length: Length of buffer.
 */
static void write_tx_cache(struct sm_state_data_t* data, uint8_t* buffer, int length)
{
    if(get_nof_tx_cache_elements(data) <= TX_CACHE_SIZE)
    {
        if(data->tx_cache_head + length <= TX_CACHE_SIZE)
            memcpy(&data->tx_cache[data->tx_cache_head], buffer, length);
        else
        {
            int elements = TX_CACHE_SIZE - data->tx_cache_head;
            memcpy(&data->tx_cache[data->tx_cache_head], buffer, elements);
            int start = (data->tx_cache_head + elements) % TX_CACHE_SIZE;
            int remaining = length - elements;
            memcpy(&data->tx_cache[start], buffer + elements, remaining);
        }

        update_tx_cache_write(data, length);
    }
}

/*
 *  Read buffer from tx cache.
 *
 *  data: Pointer to sm_state_data_t struct.
 *  buffer: Pointer to buffer.
 *  length: Length of buffer.
 */
static void read_tx_cache(struct sm_state_data_t* data, uint8_t* buffer, int length)
{
    if(!is_tx_cache_empty(data) && (get_nof_tx_cache_elements(data) >= length))
    {
        if(data->tx_cache_tail + length <= TX_CACHE_SIZE)
            memcpy(buffer, &data->tx_cache[data->tx_cache_tail], length);
        else
        {
            int elements = TX_CACHE_SIZE - data->tx_cache_tail;
            memcpy(buffer, &data->tx_cache[data->tx_cache_tail], elements);
            int start = (data->tx_cache_tail + elements) % TX_CACHE_SIZE;
            int remaining = length - elements;
            memcpy(buffer + elements, &data->tx_cache[start], remaining);
        }

        update_tx_cache_read(data, length);
    }
}

/*
 *  Update tx ringbuffer indizes after write operation.
 *
 *  data: Pointer to sm_state_data_t struct.
 *  length: Length of buffer.
 */
static void update_tx_cache_write(struct sm_state_data_t* data, int length)
{
    bool overflow = length >= (TX_CACHE_SIZE - get_nof_tx_cache_elements(data));
	
    if(data->tx_cache_full)
        data->tx_cache_tail = (data->tx_cache_tail + length) % TX_CACHE_SIZE;
    
    data->tx_cache_head = (data->tx_cache_head + length) % TX_CACHE_SIZE;

    if(overflow)
        data->tx_cache_tail = data->tx_cache_head;
    
    data->tx_cache_full = (data->tx_cache_head == data->tx_cache_tail);
}

/*
 *  Update tx ringbuffer indizes after read operation.
 *
 *  data: Pointer to sm_state_data_t struct.
 *  length: Length of buffer.
 */
static void update_tx_cache_read(struct sm_state_data_t* data, int length)
{
    data->tx_cache_full = false;
    data->tx_cache_tail = (data->tx_cache_tail + length) % TX_CACHE_SIZE;
}

/*
 *  Return number of current tx ringbuffer elements.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static int get_nof_tx_cache_elements(struct sm_state_data_t* data)
{
    int size = TX_CACHE_SIZE;
    if(!data->tx_cache_full)
	{
		if(data->tx_cache_head >= data->tx_cache_tail)
			size = (data->tx_cache_head - data->tx_cache_tail);
		else
			size = (TX_CACHE_SIZE + data->tx_cache_head - data->tx_cache_tail);
	}
	return size;
}

/*
 *  Check if tx ringbuffer is empty.
 *
 *  data: Pointer to sm_state_data_t struct.
 */
static bool is_tx_cache_empty(struct sm_state_data_t* data)
{
    return (data->tx_cache_tail == data->tx_cache_head) && !data->tx_cache_full; 
}




