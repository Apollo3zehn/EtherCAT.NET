// Windows (32 bit)
#if defined _WIN32
    #define CALLCONV __stdcall
// GCC
#elif defined __GNUC__
    // (64 bit)
    #if __x86_64__ || __ppc64__
        #define CALLCONV
    // (32 bit)
    #else 
        #define CALLCONV __attribute__((stdcall))
    #endif
#endif

#define byte uint8_t

typedef struct
{
    uint32 Manufacturer;
    uint32 ProductCode;
    uint32 Revision;
    uint16 OldCsa;
    uint16 Csa;
    uint16 ParentIndex;
} ec_slave_info_t;

typedef struct
{
    uint16 Index;
    byte SubIndex;
    char* Name;
    uint16 DataType;
} ec_variable_info_t;

typedef struct
{
    uint16 Index;
    char* Name;
    int VariableCount;
    ec_variable_info_t* VariableInfoSet;
} ec_pdo_info_t;