add_subdirectory(virtNet)

if(WIN32)
  set(OS "win32")

  if(CMAKE_SIZEOF_VOID_P EQUAL 8)
    link_directories(${CMAKE_SOURCE_DIR}/SOEM/oshw/win32/wpcap/Lib/x64)
  elseif(CMAKE_SIZEOF_VOID_P EQUAL 4)
    link_directories(${CMAKE_SOURCE_DIR}/SOEM/oshw/win32/wpcap/Lib)
  endif()
elseif(UNIX AND NOT APPLE)
  set(OS "linux")
  set(OS_LIBS pthread rt)
elseif(APPLE)
  set(OS "macosx")
endif()

file(GLOB SOEM_WRAPPER_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/*.c)
file(GLOB SOEM_WRAPPER_HEADERS ${CMAKE_CURRENT_SOURCE_DIR}/*.h)

add_library(soem_wrapper SHARED 
  ${CMAKE_CURRENT_SOURCE_DIR}/exports.def
  ${SOEM_WRAPPER_SOURCES}
  ${SOEM_WRAPPER_HEADERS}
)

if(WIN32)
  target_include_directories(soem_wrapper PRIVATE ${CMAKE_SOURCE_DIR}/SOEM/oshw/win32/wpcap/Include)
endif()

target_include_directories(soem_wrapper PRIVATE ${CMAKE_SOURCE_DIR}/SOEM/soem)
target_include_directories(soem_wrapper PRIVATE ${CMAKE_SOURCE_DIR}/SOEM/osal)
target_include_directories(soem_wrapper PRIVATE ${CMAKE_SOURCE_DIR}/SOEM/osal/${OS})
target_include_directories(soem_wrapper PRIVATE ${CMAKE_SOURCE_DIR}/SOEM/oshw/${OS})

target_link_libraries(soem_wrapper PRIVATE soem virt_net)
