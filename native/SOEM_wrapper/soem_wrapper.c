/* 
 *	Timeout occurs often. Effects are:
 *
 *	- wkc = 0 BUT ecx_SDOread succeeds (!) (see ecx_readPDOassign) -> Slaves do no start and throw ADS error 0x1D or 0x1E because sync manager length is not correctly calculated.
 *		- Let managed application do the calculation with offline configuration
 *		- Use realtime OS
 *		- Increase timeout
 *		- Increase thread priority
 *		- ...
 *
 *	- SdoWrite takes too long (e.g. EL6601). 
 *		-> Solution: increase timeout SdoWrite() 
 *
 */

#include <ethercat.h>
#include <stdbool.h>
#include <string.h>
#include <stdlib.h>
#include <stdio.h>
#include "soem_wrapper.h"
#include "virtDev/virt_dev.h"
#include "serial.h"


#define EC_VER2

// size of firmware buffer
#define FWBUFSIZE (30 * 1024 * 1024)
char filebuffer[FWBUFSIZE]; // 30MB buffer

// private
int _referenceSlave;

// FMMU / SM register content, needed to revert bootsrap config
ec_fmmut FMMU[2];
char fmmu_zero[sizeof(ec_fmmut)];

ec_smt SM[2];
char sm_zero[sizeof(ec_smt)];

// virtual network device buffer
uint8_t tx_buffer_net[1500];

// current RX fragment number */
uint8_t rxfragmentno = 0;
// complete rx frame size of current frame */
uint16_t rxframesize = 0;
// current rx data offset in frame */
uint16_t rxframeoffset = 0;
// current rx frame number */
uint16_t rxframeno = 0;
uint8 rx_buffer_net[1500];


// virtual terminal buffer
uint8_t tx_buffer_term[1500];
uint8 rx_buffer_term[1500];

char tmp_char[255];




uint16 CalculateCrc(byte* data)
{
    uint16 result = 0xFF;

    for (int i = 0; i < 14; i++)
    {
        result = result ^ data[i];

        for (int j = 0; j < 8; j++)
        {
            if ((result & 0x80) > 0)
                result = (result << 1) ^ 0x7U;
            else
                result = (result << 1);
        }
    }

    result = result & 0xFF;

    return result;
}

bool FindChildren(ecx_contextt* context, int* currentIndex, ec_slave_info_t* slaveInfoSet)
{
    int availablePorts;
    int portOrder[3];
    int currentPort;
    bool isPotentialParent;
    bool isParent;

    ec_slavet* currentSlave;

    //
    currentSlave = &context->slavelist[*currentIndex];
    availablePorts = 4;

    // port order is D -> B -> C (no subsequent slaves can be connected to port A)
    portOrder[0] = 3;
    portOrder[1] = 1;
    portOrder[2] = 2;

    for (int i = 0; i < 4; i++)
    {
        if (((currentSlave->ptype >> (i * 2)) & 0x03) <= 0x01) // 0x00 = port not existent, 0x01 == port not configured (make ENUM)
        {
            availablePorts--;
        }
    }

    isPotentialParent = availablePorts >= 3;

    // for each port
    for (int i = 0; i < 3; i++)
    {
        currentPort = portOrder[i];

        // if current port is active
        if ((currentSlave->activeports & (1 << currentPort)) > 0)
        {
            // current port type is EBUS
            isParent = isPotentialParent && ((currentSlave->ptype >> (currentPort * 2)) & 0x03) == 0x02;

            (*currentIndex)++;

            for (; *currentIndex < *context->slavecount + 1; (*currentIndex)++)
            {
                // parent of child slave is either current slave or its parent
                slaveInfoSet[*currentIndex].ParentIndex = isParent ? (*currentIndex) - 1 : slaveInfoSet[*currentIndex - 1].ParentIndex;

                if (!FindChildren(context, currentIndex, slaveInfoSet))
                {
                    continue;
                }
            }
        }
    }

    // is not an endpoint
    return context->slavelist[*currentIndex].topology > 1;
}

// low level
ecx_contextt* CALLCONV CreateContext()
{
    ecx_contextt* context;

    context = (ecx_contextt*)calloc(1, sizeof(ecx_contextt));

    context->maxslave = EC_MAXSLAVE;
    context->maxgroup = EC_MAXGROUP;
    context->esislave = 0;
    context->DCtO = 0;
    context->DCl = 0;
    context->FOEhook = NULL;
    context->EOEhook = NULL;

    context->port = (ecx_portt*)calloc(1, sizeof(ecx_portt));
    context->slavelist = (ec_slavet*)calloc(EC_MAXSLAVE, sizeof(ec_slavet));
    context->slavecount = (int*)calloc(1, sizeof(int));
    context->grouplist = (ec_groupt*)calloc(EC_MAXGROUP, sizeof(ec_groupt));
    context->esibuf = (uint8*)calloc(EC_MAXEEPBUF, sizeof(uint8));
    context->esimap = (uint32*)calloc(EC_MAXEEPBITMAP, sizeof(uint32));
    context->elist = (ec_eringt*)calloc(1, sizeof(ec_eringt));
    context->idxstack = (ec_idxstackT*)calloc(1, sizeof(ec_idxstackT));
    context->ecaterror = (boolean*)calloc(1, sizeof(boolean));
    context->DCtime = (int64*)calloc(1, sizeof(int64));
    context->SMcommtype = (ec_SMcommtypet*)calloc(EC_MAX_MAPT, sizeof(ec_SMcommtypet));
    context->PDOassign = (ec_PDOassignt*)calloc(EC_MAX_MAPT, sizeof(ec_PDOassignt));
    context->PDOdesc = (ec_PDOdesct*)calloc(EC_MAX_MAPT, sizeof(ec_PDOdesct));
    context->eepSM = (ec_eepromSMt*)calloc(1, sizeof(ec_eepromSMt));
    context->eepFMMU = (ec_eepromFMMUt*)calloc(1, sizeof(ec_eepromFMMUt));

    return context;
}

void CALLCONV Free(void* obj)
{
    if (obj)
    {
        free(obj);
    }
}

void CALLCONV FreeContext(ecx_contextt* context)
{
    if (context)
    {
        free(context->port);
        free(context->slavelist);
        free(context->slavecount);
        free(context->grouplist);
        free(context->esibuf);
        free(context->esimap);
        free(context->elist);
        free(context->idxstack);
        free(context->ecaterror);
        free(context->DCtime);
        free(context->SMcommtype);
        free(context->PDOassign);
        free(context->PDOdesc);
        free(context->eepSM);
        free(context->eepFMMU);

        free(context);
    }
}

int CALLCONV NoCaSdoRead(ecx_contextt* context, uint16 slaveIndex, uint16 sdoIndex, uint8 sdoSubIndex, uint16* data)
{
    int size = sizeof(data);

    return ecx_SDOread(context, slaveIndex, sdoIndex, sdoSubIndex, FALSE, &size, data, EC_TIMEOUTSAFE);
}

bool CALLCONV HasEcError(ecx_contextt* context)
{
    return *context->ecaterror;
}

char* CALLCONV GetNextError(ecx_contextt* context)
{
    return ecx_elist2string(context);
}

// called before OP
int CALLCONV UpdateCsa(ecx_contextt* context, int slaveIndex, uint16 csaValue)
{
    uint16 eepromData[7] = { 0 };

    for (int eepromAddress = 0; eepromAddress < 7; eepromAddress++)
    {
        eepromData[eepromAddress] = (uint16)ecx_readeepromFP(context, context->slavelist[slaveIndex].configadr, eepromAddress, EC_TIMEOUTEEP);
    }

    eepromData[4] = csaValue;

    if (ecx_writeeepromFP(context, context->slavelist[slaveIndex].configadr, 0x04, csaValue, EC_TIMEOUTEEP))
    {
        if (ecx_writeeepromFP(context, context->slavelist[slaveIndex].configadr, 0x07, CalculateCrc((byte*)eepromData), EC_TIMEOUTEEP))
            context->slavelist[slaveIndex].aliasadr = csaValue;
        else
            return -0xF001;
    }
    else
    {
        return -0xF001;
    }

    return 1;
}

int CALLCONV UploadPdoConfig(ecx_contextt* context, uint16 slaveIndex, uint16 smIndex, ec_pdo_info_t** pdoInfoSet, uint16* pdoCount)
{
    ec_ODlistt odList;
    ec_OElistt oeList;

    ec_variable_info_t* variableInfoSet;

    uint8 variableCount;
    uint8 variableSubIndex;
    uint16 pdoIndex;
    uint16 variableIndex;

    int32 pdoContent;

    int wkc;
    int bufferSize;

    //
    memset(&odList, 0, sizeof(odList));
    bufferSize = sizeof(*pdoCount);

    /* read PDO assign subindex 0 (= number of PDO's) */
    wkc = ecx_SDOread(context, slaveIndex, smIndex, 0x00, FALSE, &bufferSize, pdoCount, EC_TIMEOUTRXM);
    *pdoCount = etohs(*pdoCount);
    *pdoInfoSet = (ec_pdo_info_t*)calloc(*pdoCount, sizeof(ec_pdo_info_t));

    if (wkc <= 0)
    {
        return -0x0B01;
    }

    /* read all PDO's */
    for (int pdo = 1; pdo <= *pdoCount; pdo++)
    {
        /* read PDO assign */
        bufferSize = sizeof(pdoIndex);
        wkc = ecx_SDOread(context, slaveIndex, smIndex, (uint8)pdo, FALSE, &bufferSize, &pdoIndex, EC_TIMEOUTRXM);

        if (wkc <= 0)
        {
            return -0x0B02;
        }

        /* result is index of PDO */
        pdoIndex = etohl(pdoIndex);

        odList.Slave = slaveIndex;
        odList.Index[pdo] = smIndex;

        ecx_readODdescription(context, pdo, &odList);

        (*pdoInfoSet)[pdo - 1].Index = pdoIndex;
        (*pdoInfoSet)[pdo - 1].Name = odList.Name[pdo];

        if (pdoIndex > 0)
        {
            /* read number of subindexes of PDO */
            bufferSize = sizeof(variableCount);
            wkc = ecx_SDOread(context, slaveIndex, pdoIndex, 0x00, FALSE, &bufferSize, &variableCount, EC_TIMEOUTRXM);

            if (wkc <= 0)
            {
                return -0x0B03;
            }

            variableInfoSet = (ec_variable_info_t*)calloc(variableCount, sizeof(ec_variable_info_t));

            /* for each subindex */
            for (int pdoSubIndex = 1; pdoSubIndex <= variableCount; pdoSubIndex++)
            {
                /* read SDO that is mapped in PDO */
                bufferSize = sizeof(pdoContent);
                wkc = ecx_SDOread(context, slaveIndex, pdoIndex, (uint8)pdoSubIndex, FALSE, &bufferSize, &pdoContent, EC_TIMEOUTRXM);

                if (wkc <= 0)
                {
                    return -0x0B04;
                }

                pdoContent = etohl(pdoContent);

                /* extract bitlength of SDO */
                variableIndex = (uint16)(pdoContent >> 16);
                variableSubIndex = (uint8)((pdoContent >> 8) & 0x000000ff);

                odList.Slave = slaveIndex;
                odList.Index[0] = variableIndex;

                variableInfoSet[pdoSubIndex - 1].Index = variableIndex;
                variableInfoSet[pdoSubIndex - 1].SubIndex = variableSubIndex;

                /* read object entry from dictionary if not a filler (0x0000:0x00) */
                if (variableIndex || variableSubIndex)
                {
                    wkc = ecx_readOEsingle(context, 0, variableSubIndex, &odList, &oeList);
                    variableInfoSet[pdoSubIndex - 1].Name = oeList.Name[variableSubIndex];
                    variableInfoSet[pdoSubIndex - 1].DataType = oeList.DataType[variableSubIndex];
                }
            }

            (*pdoInfoSet)[pdo - 1].VariableCount = variableCount;
            (*pdoInfoSet)[pdo - 1].VariableInfoSet = variableInfoSet;
        }
    }

    return 0;
}

int CALLCONV GetSyncManagerType(ecx_contextt* context, uint16 slaveIndex, uint16 index, uint8* syncManagerType)
{
    *syncManagerType = context->slavelist[slaveIndex].SMtype[index - ECT_SDO_PDOASSIGN];

    return 0;
}

/*
 *  Clear FMMU and SM registers of slave in order to 
 *  program bootstrap configuration
 *
 *  context: Current context pointer
 *  slave: Slave number
 */
static void clear_FMMU_and_SM_registers(ecx_contextt* context, int slave)
{
    memset(sm_zero, 0, sizeof(sm_zero));
    memset(fmmu_zero, 0, sizeof(fmmu_zero));

    /* clean FMMU registers */
    ecx_FPWR(context->port, context->slavelist[slave].configadr, ECT_REG_FMMU0, 
        sizeof(fmmu_zero), fmmu_zero, EC_TIMEOUTRET3);
    ecx_FPWR(context->port, context->slavelist[slave].configadr, ECT_REG_FMMU1, 
        sizeof(fmmu_zero), fmmu_zero, EC_TIMEOUTRET3);

    /* clean SM0 and SM1 registers to set new bootstrap values later */
    ecx_FPWR(context->port, context->slavelist[slave].configadr, ECT_REG_SM0, 
        sizeof(ec_smt), sm_zero, EC_TIMEOUTRET3);
    ecx_FPWR(context->port, context->slavelist[slave].configadr, ECT_REG_SM1, 
        sizeof(ec_smt), sm_zero, EC_TIMEOUTRET3);
}

/*
 *  Set boot mailbox configuration master --> slave
 * 
 *  slave: Pointer to ec_slavet
 *  startAddress: SM start address
 *  length: SM length
 */
static void set_rx_boot_mailbox(ec_slavet* slave, uint16 startAddress, uint16 length)
{
    slave->SM[0].StartAddr = startAddress;
    slave->SM[0].SMlength = length;
    /* store boot write mailbox address */
    slave->mbx_wo = startAddress;
    /* store boot write mailbox size */
    slave->mbx_l = length;
}

/*
 *  Set boot mailbox configuration slave --> master
 * 
 *  slave: Pointer to ec_slavet
 *  startAddress: SM start address
 *  length: SM length
 */
static void set_tx_boot_mailbox(ec_slavet* slave, uint16 startAddress, uint16 length)
{
    slave->SM[1].StartAddr = startAddress;
    slave->SM[1].SMlength = length;
    /* store boot read mailbox address */
    slave->mbx_ro = startAddress;
    /* store boot read mailbox size */
    slave->mbx_rl = length;
}

/*
 *  Set SM mailbox bootstrap configuration
 *
 *  context: Current context pointer
 *  slave: Slave number
 */
static void set_bootstrap(ecx_contextt* context, int slave)
{
    memset(FMMU, 0, sizeof(ec_fmmut) * 2);
    /* read content of current FMMU registers */
    ecx_FPRD(context->port, context->slavelist[slave].configadr, ECT_REG_FMMU0, 
        sizeof(ec_fmmut), &FMMU[0], EC_TIMEOUTRET3);
    ecx_FPRD(context->port, context->slavelist[slave].configadr, ECT_REG_FMMU1, 
        sizeof(ec_fmmut), &FMMU[1], EC_TIMEOUTRET3);

    memset(SM, 0, sizeof(ec_smt) * 2);
    /* read content of current SM registers */
    ecx_FPRD(context->port, context->slavelist[slave].configadr, ECT_REG_SM0, 
        sizeof(ec_smt), &SM[0], EC_TIMEOUTRET3);
    ecx_FPRD(context->port, context->slavelist[slave].configadr, ECT_REG_SM1, 
	    sizeof(ec_smt), &SM[1], EC_TIMEOUTRET3);

    clear_FMMU_and_SM_registers(context, slave);	

    /* read BOOT mailbox data, master -> slave */
    uint32 data = ecx_readeeprom(context, slave, ECT_SII_BOOTRXMBX, EC_TIMEOUTEEP);
    set_rx_boot_mailbox(&context->slavelist[slave], (uint16)LO_WORD(data), (uint16)HI_WORD(data));

    /* read BOOT mailbox data, slave -> master */
    data = ecx_readeeprom(context, slave, ECT_SII_BOOTTXMBX, EC_TIMEOUTEEP);
    set_tx_boot_mailbox(&context->slavelist[slave], (uint16)LO_WORD(data), (uint16)HI_WORD(data));

    /* program SM0 mailbox in for slave */
    ecx_FPWR (context->port, context->slavelist[slave].configadr, ECT_REG_SM0, 
        sizeof(ec_smt), &context->slavelist[slave].SM[0], EC_TIMEOUTRET);
    /* program SM1 mailbox out for slave */
    ecx_FPWR (context->port, context->slavelist[slave].configadr, ECT_REG_SM1, 
        sizeof(ec_smt), &context->slavelist[slave].SM[1], EC_TIMEOUTRET);
}

/*
 *  Revert SM mailbox bootstrap configuration
 *
 *  context: Current context pointer
 *  slave: Slave number
 */
static void revert_bootstrap(ecx_contextt* context, int slave)
{
    clear_FMMU_and_SM_registers(context, slave);

    /* restore mailbox data, master -> slave */
    set_rx_boot_mailbox(&context->slavelist[slave], SM[0].StartAddr, SM[0].SMlength);
    /* restore mailbox data, slave -> master */
    set_tx_boot_mailbox(&context->slavelist[slave], SM[1].StartAddr, SM[1].SMlength);

    /* restore SM0 mailbox in for slave */
    ecx_FPWR (context->port, context->slavelist[slave].configadr, ECT_REG_SM0, 
        sizeof(ec_smt), &context->slavelist[slave].SM[0], EC_TIMEOUTRET);
    /* restore SM1 mailbox out for slave */
    ecx_FPWR (context->port, context->slavelist[slave].configadr, ECT_REG_SM1, 
        sizeof(ec_smt), &context->slavelist[slave].SM[1], EC_TIMEOUTRET);

    /* copy stored FMMU registers */
    memcpy(&context->slavelist[slave].FMMU[0], &FMMU[0], sizeof(ec_fmmut));
    memcpy(&context->slavelist[slave].FMMU[1], &FMMU[1], sizeof(ec_fmmut));

    /* restore FMMU0 */
    ecx_FPWR (context->port, context->slavelist[slave].configadr, ECT_REG_FMMU0, 
        sizeof(ec_fmmut), &context->slavelist[slave].FMMU[0], EC_TIMEOUTRET);
    /* restore FMMU0 */
    ecx_FPWR (context->port, context->slavelist[slave].configadr, ECT_REG_FMMU1, 
        sizeof(ec_fmmut), &context->slavelist[slave].FMMU[1], EC_TIMEOUTRET);
}

/*
 *  Read firmware file to filebuffer
 *
 *  fileName: File name
 *  length [out]: File length
 *
 *  returns: 1 if operation was successful, -1 otherwise
 */
static int read_file(const char *fileName, int *length)
{
    memset(&filebuffer, 0, FWBUFSIZE);

    FILE * file = fopen(fileName, "rb");
    if(file == NULL)
        return -1;

    int counter = 0, c;
    while (((c = fgetc(file)) != EOF) && (counter < FWBUFSIZE))
        filebuffer[counter ++] = (uint8)c;

    *length = counter;
    fclose(file);
    return 1;
}

/*
 *  Request new slave state.
 *
 *  context: Current context pointer
    slave: Slave number
 *  state: Requested slave state
 *
 *  returns: Slave state which was set
 */
uint16 CALLCONV RequestState(ecx_contextt* context, int slave, uint16 state)
{
    if(state == EC_STATE_BOOT)
    {
        // set mailbox bootstrap configuration
        set_bootstrap(context, slave);
    }

    /* if current state is EC_STATE_BOOT and requested state is EC_STATE_INIT 
        we have to restore FMMU and SM registers */
    if((context->slavelist[slave].state == EC_STATE_BOOT) && 
        (state == EC_STATE_INIT))
    {
        revert_bootstrap(context, slave);
    }

    context->slavelist[slave].state = state;
    ecx_writestate(context, slave);
	
    uint16 slaveState = EC_STATE_NONE;
    int counter = 10;

    do
    {
        /* wait for slave to reach requested state,
        returns current slave state */
        slaveState = ecx_statecheck(context, slave, state, EC_TIMEOUTSTATE);
    } while ((counter--) && (slaveState != state));

    return slaveState;
}

/*
 *  Return current slave state.
 *
 *  context: Current context pointer
 *  slave: Slave number
 *
 *  returns: Current slave state
 */
uint16 CALLCONV GetState(ecx_contextt* context, int slave)
{
    return context->slavelist[slave].state;
}

/*
 *  Download firmware file to slave.
 *
 *  context: Current context pointer
 *  slave: Slave number
 *  fileName: File name 
 *  length: File length
 *
 *  returns: Workcounter from last slave response
 */
int CALLCONV DownloadFirmware(ecx_contextt* context, int slave, char *fileName, int length)
{
    if(context->slavelist[slave].state != EC_STATE_BOOT)
        return -1;

    int wk = 0;
    int readLength = 0;
    if(read_file(fileName, &readLength))
    {
        if(readLength == length)
        {
            wk = ecx_FOEwrite(context, slave, fileName, 0, readLength , &filebuffer, EC_TIMEOUTSTATE);
        }
    }

    return  (wk > 0) ? 1 : -1;
}

/*
 *  Register callback for FoE.
 *
 *  context: Current context pointer
 *  callback: Callback function pointer
 *
 */
void CALLCONV RegisterFOECallback(ecx_contextt* context, int CALLCONV callback(uint16 slave, int packetnumber, int datasize))
{
    context->FOEhook = (int (*)(uint16 slave, int packetnumber, int datasize))callback;	
}

/*
 *  Create virtual network device.
 *
 *  interfaceName: Virtual network interface name.
 *  deviceId [out]: Virtual network device Id != -1 if successful.
 *
 *  returns: Actually virtual network device name set by kernel. 
 * 
 */
char* CALLCONV CreateVirtualNetworkDevice(char *interfaceName, int* deviceId)
{
    memset(tmp_char, 0, sizeof(tmp_char));
     *deviceId = create_virtual_network_device(interfaceName, tmp_char);

     return (char*) tmp_char;
}

/*
 *  Close virtual network device.
 *  deviceId: Virtual network device Id.
 *
 */
void CALLCONV CloseVirtualNetworkDevice(int deviceId)
{
    close_virtual_network_device(deviceId);
}

/*
 *  Read data from virtual network interface and  
 *  forward ethernet frames with EoE to slave.
 *
 *  context: Current context pointer.
 *  slave: Slave number.
 *  deviceId: Virtual network device Id.
 *
 *  returns: True if any data was forwarded, false otherwise.
 */
bool CALLCONV SendEthernetFramesToSlave(ecx_contextt* context, int slave, int deviceId)
{
    long size = read_virtual_network_device(tx_buffer_net, sizeof(tx_buffer_net), deviceId);
    int wk = 0;

    if(size > 0)
    {
        ec_etherheadert *bp = (ec_etherheadert *)tx_buffer_net;
        uint16 type = (bp->etype << 8 | bp->etype >> 8);
       
        if (type != ETH_P_ECAT)
        {
            wk = ecx_EOEsend(context, slave, 0, size, (void*)tx_buffer_net, 0);
        }
    }

    return (size > 0) && (wk > 0) ? true : false;
}

/*
 *  Read EoE frames from slave device. Ethernet data is extracted
 *  and forwarded to virtual network interface.
 *
 *  context: Current context pointer.
 *  slave: Slave number.
 *  deviceId: Virtual network device Id.
 *
 *  returns: True if any data was received, false otherwise.
 */
bool CALLCONV ReadEthernetFramesFromSlave(ecx_contextt* context, int slave, int deviceId)
{
    int size_of_rx = sizeof(rx_buffer_net);
    int wk = ecx_EOErecv(context, slave, 0, &size_of_rx, (void*)&rx_buffer_net, 0);
    long size = 0;

    if (wk > 0)
    {
        ec_etherheadert *bp = (ec_etherheadert *)rx_buffer_net;        
        uint16 type = (bp->etype << 8 | bp->etype >> 8);

        if(type != ETH_P_ECAT)
        {
            size = write_virtual_network_device(bp, size_of_rx, deviceId);
        }
    }

    return (size > 0) && (wk > 0) ? true : false;
}

/*
 *  Create virtual serial port.
 *
 *  deviceId [out]: Device Id of virtual serial port.
 *
 *  returns: Name of virtual serial port.
 */
char* CreateVirtualSerialPort(int* deviceId)
{
    memset(tmp_char, 0, sizeof(tmp_char));
    *deviceId = create_virtual_serial_port(tmp_char);
    
    return (char*) tmp_char;
}

/*
 *  Close virtual serial port.
 *
 *  deviceId: Device Id of virtual serial port.
 *
 */
void CALLCONV CloseVirtualSerialPort(int deviceId)
{
    close_virtual_serial_port(deviceId);
}

/*
 *  Read data from virtual serial port and forward it to slave.
 *
 *  slave: Slave number.
 *  deviceId: Device Id of virtual serial port.
 *
 *  returns: True if any data was forwarded, false otherwise.
 */
bool CALLCONV SendSerialDataToSlave(int slave, int deviceId)
{
    long size = read_virtual_serial_port(tx_buffer_term, sizeof(tx_buffer_term), deviceId);
    bool success = false;

    if(size > 0)
    {
        success = set_tx_buffer(slave, tx_buffer_term, size);
    }

    return (size > 0) && success ? true : false;
}

/*
 *  Read data from slave and forward it to virtual serial port.
 *
 *  slave: Slave number.
 *  deviceId: Device Id of virtual terminal.
 *
 *  returns: True if any data was forwarded, false otherwise.
 */
bool CALLCONV ReadSerialDataFromSlave(int slave, int deviceId)
{
    int size_of_rx = sizeof(rx_buffer_term);
    bool data_received = get_rx_buffer(slave, rx_buffer_term, &size_of_rx);
    long size = 0;

    if(data_received)
    {
        size = write_virtual_serial_port(rx_buffer_term, size_of_rx, deviceId);   
    }

    return (size > 0) && data_received ? true : false;
}

/*
 *  Initialize serial handshake processing for slave device.
 *
 *  slave: Slave number.
 * 
 * returns: True if initialization was successful, false otherwise.
 */
bool CALLCONV InitSerial(int slave)
{
    return init_serial(slave);
}

/*
 *  Close serial handshake processing for slave device.
 *
 *  slave: Slave number.
 * 
 * returns: True if close was successful, false otherwise.
 */
bool CALLCONV CloseSerial(int slave)
{
    return close_serial(slave);
}

/*
 *  Register serial rx callback for slave device.
 *
 *  slave: Slave number.
 *  callback: Callback.
 * 
 */
void CALLCONV RegisterSerialRxCallback(uint16 slave, void CALLCONV callback(uint16 slave, uint8_t* buffer, int datasize))
{
    register_rx_callback(slave, (void (*)(uint16_t slave, uint8_t* buffer, int datasize))callback);
}

/*
 *  Set tx buffer transmit to slave device.
 *
 *  slave: Slave number.
 *  tx_buffer: Tx buffer
 *  datasize: Size of tx buffer.
 * 
 * returns: True if buffer was set successfully, false otherwise.
 */
bool CALLCONV SetTxBuffer(uint16 slave, uint8* tx_buffer, int datasize)
{
    return set_tx_buffer(slave, tx_buffer, datasize);
}

/*
 *  Update serial handshake processing for slave device.
 *
 *  context: Current context pointer.
 *  slave: Slave number.
 * 
 * returns: True if close was successful, false otherwise.
 */
void CALLCONV UpdateSerialIo(ecx_contextt* context, int slave)
{
    update_serial(slave, context->slavelist[slave].outputs, context->slavelist[slave].inputs);
}

/*
 *  Request specific state for all slaves.
 *
 *  context: Current context pointer
 *  state: Requested state
 *
 *  returns: 1 if operation was successful, -0x0601 otherwise
 */
int CALLCONV RequestCommonState(ecx_contextt* context, uint16 state)
{
    int counter = 200;
    context->slavelist[0].state = state;
    bool operationalState = (state == EC_STATE_OPERATIONAL);

    if(operationalState)
    {
        ecx_send_processdata(context);
        ecx_receive_processdata(context, EC_TIMEOUTRET);
    }

    ecx_writestate(context, 0);

    // wait for all slaves to reach state
    do
    {
        if(operationalState)
        {
            ecx_send_processdata(context);
            ecx_receive_processdata(context, EC_TIMEOUTRET);
        }

        ecx_statecheck(context, 0, state, 5 * EC_TIMEOUTSTATE);
    } while (counter-- && (context->slavelist[0].state != state));
    
    return context->slavelist[0].state == state ? 1 : -0x0601;
}

int CALLCONV CheckSafeOpState(ecx_contextt* context)
{
    ecx_statecheck(context, 0, EC_STATE_SAFE_OP, EC_TIMEOUTSTATE);

    return context->slavelist[0].state == EC_STATE_SAFE_OP ? 1 : -0x0501;
}

int CALLCONV ConfigureSync01(ecx_contextt* context, uint16 slaveIndex, byte* assignActivate[], int32 assignActivateByteLength, uint32 cycleTime0, uint32 cycleTime1, uint32 cycleShift)
{
    // improve - Is it the most efficient approach to have a variable length AssignActivate variable? Maybe it's better to reconstruct the parsed AssignActivate to always have 2 bytes.

    int returnValue;
    int activationRegister = 1;

    switch (assignActivateByteLength)
    {
        case 1:
            activationRegister = (*assignActivate)[0];
            break;
        case 2:
            activationRegister = (*assignActivate)[1];
            break;
        default:
            return -0x0401;
    }

    if ((activationRegister & 0x7) == 0x7)
    {
        ecx_dcsync01(context, slaveIndex, TRUE, cycleTime0, cycleTime1, cycleShift);
    }
    else if ((activationRegister & 0x3) == 0x3)
    {
        ecx_dcsync0(context, slaveIndex, TRUE, cycleTime0, cycleShift);
    }
    else if ((activationRegister & 0x1) == 0x1)
    {
        return -0x0402;
    }

    // since ecx_dcsync01 and ecx_dcsync0 only write to ECT_REG_DCCUC:
    switch (assignActivateByteLength)
    {
        case 1:
            returnValue = ecx_FPWR(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_DCSYNCACT, 1, *assignActivate, EC_TIMEOUTRET);
            break;
        case 2:
            returnValue = ecx_FPWR(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_DCCUC, 2, *assignActivate, EC_TIMEOUTRET);
            break;
        default:
            return -0x0401;
    }

    return returnValue == 1 ? 1 : 0;
}

int CALLCONV ConfigureDc(ecx_contextt* context, uint32 frameCount, uint32 targetTimeDifference, uint32* systemTimeDifference)
{
    uint64 referenceClockTime;

    if (ecx_configdc(context))
    {
        // find first slave with dc capabilities
        for (int slave = 1; slave < *context->slavecount + 1; slave++)
        {
            if (context->slavelist[slave].hasdc)
            {
                _referenceSlave = slave;
                break;
            }
        }

        // compensate static drift. Works up to 100 us initial offset. frameCount cycles are necessary to settle control loop.
        for (uint32 counter = 1; counter <= frameCount; counter++)
        {
            ecx_FRMW(context->port, context->slavelist[_referenceSlave].configadr, ECT_REG_DCSYSTIME, sizeof(referenceClockTime), &referenceClockTime, EC_TIMEOUTRET);
        }

        ecx_BRD(context->port, 0x00, ECT_REG_DCSYSDIFF, sizeof(*systemTimeDifference), systemTimeDifference, EC_TIMEOUTRET);

        return (*systemTimeDifference & 0x7FFF) <= targetTimeDifference ? 1 : -0x0302;
    }
    else
    {
        return -0x0301;
    }
}

int CALLCONV ConfigureIoMap(ecx_contextt* context, char* ioMap, int* slaveRxPdoOffsetSet, int* slaveTxPdoOffsetSet, int* expectedWorkingCounter)
{
    int ioMapSize;

    ioMapSize = ecx_config_map_group(context, ioMap, 0);

    // translate input and output pointers to IoMap offset

    slaveRxPdoOffsetSet[0] = -1;
    slaveTxPdoOffsetSet[0] = -1;

    for (int slave = 1; slave < *context->slavecount + 1; slave++)
    {
        if (context->slavelist[slave].outputs != NULL)
            slaveRxPdoOffsetSet[slave] = (int)(context->slavelist[slave].outputs - (uint8*)ioMap);
        else
            slaveRxPdoOffsetSet[slave] = -1;

        if (context->slavelist[slave].inputs != NULL)
            slaveTxPdoOffsetSet[slave] = (int)(context->slavelist[slave].inputs - (uint8*)ioMap);
        else
            slaveTxPdoOffsetSet[slave] = -1;
    }

    *expectedWorkingCounter = (context->grouplist[0].outputsWKC * 2) + context->grouplist[0].inputsWKC;

    return ioMapSize;
}

int CALLCONV SdoWrite(ecx_contextt* context, uint16 slaveIndex, uint16 sdoIndex, uint8 sdoSubIndex, uint8* dataset, uint32 datasetCount, int32* byteCountSet)
{
    uint8 null = 0;
    int returnValue = 0;
    int totalByteCount = 0;
    int timeout = EC_TIMEOUTSAFE * 10;

    if (context->slavelist[slaveIndex].mbx_l == 0)
    {
        return -0x0A01;
    }

    if (sdoSubIndex <= 1 && context->slavelist[slaveIndex].CoEdetails & ECT_COEDET_SDOCA) // complete access
    {
        for (uint32 i = 0; i < datasetCount; i++)
        {
            totalByteCount += byteCountSet[i];
        }

        // Clause 12.2 of the ETG.1020 specification, sub-point 1 "SDO Complete Access" -> Subindex 0 is padded to 16 bits
        if (sdoSubIndex == 0)
        {
            uint8* datasetPadded = calloc(totalByteCount + 1, 1);
            
            datasetPadded[0] = dataset[0];

            for (int i = 1; i < totalByteCount; i++)
            {
                datasetPadded[i + 1] = dataset[i];
            }

            returnValue += ecx_SDOwrite(context, slaveIndex, sdoIndex, sdoSubIndex, TRUE, totalByteCount + 1, datasetPadded, timeout);
            free(datasetPadded);
        }
        else
        {
            returnValue += ecx_SDOwrite(context, slaveIndex, sdoIndex, sdoSubIndex, TRUE, totalByteCount, dataset, timeout);
        }

        return returnValue == 1 ? 1 : 0;
    }
    else // legacy access
    {
        int offset = 0;

        if (sdoSubIndex == 0) // sdoSubIndex == 0
        {
            returnValue += ecx_SDOwrite(context, slaveIndex, sdoIndex, 0, FALSE, byteCountSet[0], &null, timeout);
            offset += byteCountSet[0];

            for (uint32 i = 1; i < datasetCount; i++)
            {
                returnValue += ecx_SDOwrite(context, slaveIndex, sdoIndex, i, FALSE, byteCountSet[i], &dataset[offset], timeout);
                offset += byteCountSet[i];
            }

            if (datasetCount > 1)
            {
                returnValue += ecx_SDOwrite(context, slaveIndex, sdoIndex, 0, FALSE, byteCountSet[0], &dataset[0], timeout);

                return returnValue == datasetCount + 1 ? 1 : 0;
            }
        }
        else // random access
        {
            for (uint32 i = 0; i < datasetCount; i++)
            {
                returnValue += ecx_SDOwrite(context, slaveIndex, sdoIndex, i + sdoSubIndex, FALSE, byteCountSet[i], &dataset[i], timeout);
            }
        }

        return returnValue == datasetCount ? 1 : 0;
    }
}

int CALLCONV ScanDevices(ecx_contextt* context, char* interfaceName, ec_slave_info_t** slaveInfoSet, int* slaveCount)
{
    int wkc;
    int watchdogTime;
    int slaveIndex;

    watchdogTime = 0;

    if (ecx_init(context, interfaceName))
    {
        if (ecx_config_init(context, false) < 0)
        {
            return -0x0103;
        }

        *slaveInfoSet = (ec_slave_info_t*)calloc(*context->slavecount + 1, sizeof(ec_slave_info_t));
        *slaveCount = *context->slavecount;

        // request PREOP state for all slaves
        int counter = 5;

        do
        {
            ecx_statecheck(context, 0, EC_STATE_PRE_OP, EC_TIMEOUTSTATE);
            
            for (int slaveIndex = 1; slaveIndex < *context->slavecount + 1; slaveIndex++)
            {
                if(context->slavelist[slaveIndex].state != (EC_STATE_PRE_OP | EC_STATE_ACK))
                {
                    context->slavelist[slaveIndex].state = EC_STATE_PRE_OP | EC_STATE_ACK;
                    ecx_writestate(context, slaveIndex);
                }
            }
            
        } while (counter-- && (context->slavelist[0].state != (EC_STATE_PRE_OP | EC_STATE_ACK)));

        if (context->slavelist[0].state != (EC_STATE_PRE_OP | EC_STATE_ACK))
            return -0x0104;

        // read real CSA value from EEPROM
        for (int slaveIndex = 1; slaveIndex < *context->slavecount + 1; slaveIndex++)
        {
            ecx_eeprom2master(context, slaveIndex);
            context->slavelist[slaveIndex].aliasadr = (uint16)ecx_readeepromFP(context, context->slavelist[slaveIndex].configadr, 0x04, EC_TIMEOUTEEP);
        }

        // find parents
        slaveIndex = 1;

        if (*context->slavecount > 0)
        {
            FindChildren(context, &slaveIndex, *slaveInfoSet);
        }

        for (int slaveIndex = 1; slaveIndex < *context->slavecount + 1; slaveIndex++)
        {
            // clear watchdog trigger enable in SM control register
            for (int i = 0; i < EC_MAXSM; i++)
            {
                if (context->slavelist[slaveIndex].SMtype[i] == 3)
                    context->slavelist[slaveIndex].SM[i].SMflags &= ~0x40;
            }

            // clear watchdog time process data register
            if (!(wkc = ecx_FPWR(context->port, context->slavelist[slaveIndex].configadr, 0x420, sizeof(watchdogTime), &watchdogTime, EC_TIMEOUTRET)))
            {
                return -0x0102;
            }

            // copy relevant data
            (*slaveInfoSet)[slaveIndex].Manufacturer = context->slavelist[slaveIndex].eep_man;
            (*slaveInfoSet)[slaveIndex].ProductCode = context->slavelist[slaveIndex].eep_id;
            (*slaveInfoSet)[slaveIndex].Revision = context->slavelist[slaveIndex].eep_rev;
            (*slaveInfoSet)[slaveIndex].OldCsa = context->slavelist[slaveIndex].aliasadr;
            (*slaveInfoSet)[slaveIndex].Csa = context->slavelist[slaveIndex].aliasadr;
        }
    }
    else
    {
        return -0x0101;
    }

    return 1;
}

int CALLCONV RegisterCallback(ecx_contextt* context, uint16 slaveIndex, int CALLCONV callback(uint16))
{
    context->slavelist[slaveIndex].PO2SOconfig = (int (*)(uint16 slaveIndex))callback;

    return 0;
}

// called during OP
int CALLCONV ReadState(ecx_contextt* context)
{
    return ecx_readstate(context);
}

int CALLCONV ReadSlaveState(ecx_contextt* context, uint16 slaveIndex, uint16* requestedState, uint16* actualState, uint16* alStatusCode, int32* systemTimeDifference, uint16* speedCounterDifference, uint16* outputPdoCount, uint16* inputPdoCount)
{
    int returnValue = 0;
    int size = 1;

    returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_ALCTL, sizeof(*requestedState), requestedState, EC_TIMEOUTSAFE);
    returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_ALSTAT, sizeof(*actualState), actualState, EC_TIMEOUTSAFE);
    returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_ALSTATCODE, sizeof(*alStatusCode), alStatusCode, EC_TIMEOUTSAFE);

    if (context->slavelist[slaveIndex].mbx_l == 0)
    {
        if (context->slavelist[slaveIndex].SMtype[0] == 3)
        {
            returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_SM0 + 0x02, sizeof(*outputPdoCount), outputPdoCount, EC_TIMEOUTSAFE);
            *inputPdoCount = 0;
            returnValue += 1;
        }
        else if (context->slavelist[slaveIndex].SMtype[0] == 4)
        {
            returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_SM0 + 0x02, sizeof(*inputPdoCount), inputPdoCount, EC_TIMEOUTSAFE);
            *outputPdoCount = 0;
            returnValue += 1;
        }
        else
        {
            returnValue += 2;
        }
    }
    else
    {
        returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_SM2 + 0x02, sizeof(*outputPdoCount), outputPdoCount, EC_TIMEOUTSAFE);
        returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_SM3 + 0x02, sizeof(*inputPdoCount), inputPdoCount, EC_TIMEOUTSAFE);
    }

    if (returnValue != 5)
    {
        return 0;
    }

    // not all slave types will respond
    returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_DCSYSDIFF, sizeof(*systemTimeDifference), systemTimeDifference, EC_TIMEOUTSAFE);
    returnValue += ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, ECT_REG_DCSPEEDCNT + 0x02, sizeof(*speedCounterDifference), speedCounterDifference, EC_TIMEOUTSAFE);

    return 1;
}

int CALLCONV CompensateDcDrift(ecx_contextt* context, int32 ticks)
{
    int64 systemTimeOffset;

    if (ecx_FPRD(context->port, context->slavelist[_referenceSlave].configadr, ECT_REG_DCSYSOFFSET, sizeof(systemTimeOffset), &systemTimeOffset, EC_TIMEOUTRET) > 0)
    {
        systemTimeOffset += ticks;
        ecx_FPWR(context->port, context->slavelist[_referenceSlave].configadr, ECT_REG_DCSYSOFFSET, sizeof(systemTimeOffset), &systemTimeOffset, EC_TIMEOUTRET);
    }

    return 1;
}

int CALLCONV UpdateIo(ecx_contextt* context, int64* dcTime)
{
    int wkc;

    wkc = 0;

    if (ecx_send_processdata(context))
    {
        wkc = ecx_receive_processdata(context, EC_TIMEOUTRET);
    }
    
    *dcTime = *context->DCtime;

    return wkc;
}

// debug
void CALLCONV ReadAllRegisters(ecx_contextt* context, uint16 slaveIndex)
{
    byte data[1024];

    ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, 0x0000, 1024, &data, 4000);
    ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, 0x0300, 1024, &data, 4000);
    ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, 0x0600, 1024, &data, 4000);
    ecx_FPRD(context->port, context->slavelist[slaveIndex].configadr, 0x0900, 1024, &data, 4000);

    return;
}
