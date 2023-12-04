#!/bin/bash
echo "Compiling the library..."
# Set the NDK_PROJECT_PATH environment variable to the current working directory
export NDK_PROJECT_PATH=$(pwd)

# Path to the ndk-build command. Adjust this path based on your actual NDK installation.
NDK_BUILD_PATH=~/libraries/android-ndk-r25c/ndk-build

# Run ndk-build to build the native libraries
$NDK_BUILD_PATH 

# Define the source directory of the generated library
LIB_SRC_DIR=./libs/armeabi-v7a

