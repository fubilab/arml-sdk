# Minimum required CMake version
cmake_minimum_required(VERSION 3.1.0)

# add_subdirectory(${CMAKE_CURRENT_LIST_DIR}/depthai-core)

# Project name
project(arml_cpp)

# # Find Boost program_options component
# find_package(Boost REQUIRED COMPONENTS program_options)

# # Find OpenCV package
# find_package(OpenCV REQUIRED)

# # find_package(depthai CONFIG REQUIRED)

# find_package(realsense2 REQUIRED)

find_package(depthai CONFIG REQUIRED)

# # Define dependencies
# set(DEPENDENCIES realsense2 ${OpenCV_LIBS})

# # Display OpenCV version in the message
# message(STATUS "OPENCV VERSION ${OpenCV_VERSION}")

# # Include directories for headers (adjust as needed)
# include_directories(
#     ${CMAKE_SOURCE_DIR}/include
#     ${Boost_INCLUDE_DIRS}
#     ${OpenCV_INCLUDE_DIRS}
#     ${realsense2_INCLUDE_DIRS}
# )
# # Create shared library target
# add_library(camera_motion SHARED src/camera_motion.cpp src/debugCPP.cpp)

# # Set C++ standard to C++11 for the shared library
# set_property(TARGET camera_motion PROPERTY CXX_STANDARD 11)

# # Link libraries to the shared library
# target_link_libraries(camera_motion
#     ${DEPENDENCIES}
#     ${Boost_LIBRARIES}
# )

# Create executable target
add_executable(arml_cpp src/oakd_motion.cpp)

# Set C++ standard to C++11 for the executable
# set_property(TARGET arml_cpp PROPERTY CXX_STANDARD 11)

# # Link libraries to the executable
# target_link_libraries(arml_cpp 
#     camera_motion  # Link to the shared library
#     ${DEPENDENCIES}
#     ${Boost_LIBRARIES}
# )

target_link_libraries(arml_cpp PRIVATE depthai::opencv ${OpenCV_LIBS} Threads::Threads)