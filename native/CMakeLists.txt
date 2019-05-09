if(WIN32)
    cmake_minimum_required(VERSION 3.14)
else()
    cmake_minimum_required(VERSION 2.8.4)
endif()

project (SoemWrapper)

set(CMAKE_POSITION_INDEPENDENT_CODE ON)

add_subdirectory(SOEM)
add_subdirectory(SOEM_wrapper)