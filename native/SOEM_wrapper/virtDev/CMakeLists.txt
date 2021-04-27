cmake_minimum_required(VERSION 2.8.4)
project(virtDev C)

if(WIN32)
    # Add virtual device implementation
    # for win32 and remove stub.
    set(SOURCE_FILES stub.c)
elseif(UNIX AND NOT APPLE)
    set(SOURCE_FILES virt_dev_linux.c)
elseif(APPLE)
    # Add virtual device implementation 
    # for macOS and remove stub.
    set(SOURCE_FILES stub.c)
endif()

add_library(virt_dev STATIC ${SOURCE_FILES})
