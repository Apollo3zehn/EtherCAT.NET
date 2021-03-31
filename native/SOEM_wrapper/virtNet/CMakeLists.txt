cmake_minimum_required(VERSION 2.8.4)
project(virtNet C)

if(WIN32)
    # Add virtual network device implementation
    # for win32 and remove stub.
    set(SOURCE_FILES stub.c)
elseif(UNIX AND NOT APPLE)
    set(SOURCE_FILES virt_net_linux.c)
elseif(APPLE)
    # Add virtual network device implementation 
    # for macOS and remove stub.
    set(SOURCE_FILES stub.c)
endif()

add_library(virt_net STATIC ${SOURCE_FILES})
