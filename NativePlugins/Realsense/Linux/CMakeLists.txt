# Minimum required CMake version
cmake_minimum_required(VERSION 3.1.0)

# add_subdirectory(${CMAKE_CURRENT_LIST_DIR}/depthai-core EXCLUDE_FROM_ALL)


# Project name
project(arml_cpp)


# Find OpenCV package
find_package(OpenCV REQUIRED)

find_package(realsense2 REQUIRED)

find_package(Threads REQUIRED) 

find_package(nlohmann_json REQUIRED)

# Path to JNI in Android folder
set(SRC_FILES ${CMAKE_SOURCE_DIR}/../Android/jni)

# Define dependencies
set(DEPENDENCIES realsense2 ${OpenCV_LIBS})

# Display OpenCV version in the message
message(STATUS "OPENCV VERSION ${OpenCV_VERSION}")
message(STATUS "REALSENSE VERSION ${realsense2_VERSION}")

# Include directories for headers (adjust as needed)
include_directories(
    ${CMAKE_SOURCE_DIR}/../Android/jni/include
    ${OpenCV_INCLUDE_DIRS}
    ${realsense2_INCLUDE_DIRS}
    ${nlohmann_json_INCLUDE_DIRS}
)
# Create shared library target

add_library(camera_motion
            SHARED
            ${SRC_FILES}/src/camera_motion.cpp
            ${SRC_FILES}/src/debugCPP.cpp
            ${SRC_FILES}/src/globals.cpp
            ${SRC_FILES}/src/localization.cpp
            ${SRC_FILES}/src/rs_camera.cpp
            ${SRC_FILES}/src/object_detection.cpp
            )

# Set C++ standard to C++11 for the shared library
set_property(TARGET camera_motion PROPERTY CXX_STANDARD 11)

# Link libraries to the shared library
target_link_libraries(camera_motion
    ${DEPENDENCIES}
    Threads::Threads
)


## Conditionally create the executable based on BUILD_EXECUTABLE
if(BUILD_EXECUTABLE)
    add_compile_definitions(BUILD_EXECUTABLE)
    # Find Boost program_options component
    find_package(Boost REQUIRED COMPONENTS program_options)
    # Create executable target
    add_executable(arml_cpp ${SRC_FILES}/src/tester.cpp)

    # Set C++ standard to C++11 for the executable
    set_property(TARGET arml_cpp PROPERTY CXX_STANDARD 11)

    # Link libraries to the executable
    target_link_libraries(arml_cpp
        camera_motion  # Link to the shared library
        ${DEPENDENCIES}
        Threads::Threads
        Boost::program_options
    )
    message(STATUS "Executable is Enabled")
else()
    message(STATUS "Executable is disabled. To enable it, set BUILD_EXECUTABLE=ON.")
endif()
