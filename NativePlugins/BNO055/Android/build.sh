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

# # Define the destination directory where the library should be moved
# LIB_DEST_DIR=./Assets/Plugins/Android

# # Check if the source directory exists
# if [ -d "$LIB_SRC_DIR" ]; then
#     echo "Found the source directory: $LIB_SRC_DIR"
# else
#     echo "Error: Source directory not found: $LIB_SRC_DIR"
#     exit 1
# fi

# # Check if the destination directory exists, if not create it
# if [ -d "$LIB_DEST_DIR" ]; then
#     echo "Found the destination directory: $LIB_DEST_DIR"
# else
#     echo "Error: Destination directory not found: $LIB_DEST_DIR"
#     exit 1
# fi

# mv -f "$LIB_SRC_DIR"/*.so "$LIB_DEST_DIR"

# # Check if the move operation was successful
# if [ $? -eq 0 ]; then
#     echo "Library moved successfully!"
# else
#     echo "Failed to move the library. An error occurred."
# fi
