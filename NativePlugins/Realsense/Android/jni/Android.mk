LOCAL_PATH := $(call my-dir)


include $(CLEAR_VARS)
LOCAL_MODULE := realsense2
LOCAL_SRC_FILES := ../shared_libraries/librealsense2.so
LOCAL_EXPORT_C_INCLUDES := $(LOCAL_PATH)/include
include $(PREBUILT_SHARED_LIBRARY)


include $(CLEAR_VARS)
OPENCV_INSTALL_MODULES:=on
OPENCV_LIB_TYPE:=STATIC
include /home/fubintlab/libraries/OpenCV-android-sdk/sdk/native/jni/OpenCV.mk

LOCAL_MODULE += camera_motion
LOCAL_SRC_FILES := src/camera_motion.cpp
LOCAL_SRC_FILES += src/debugCPP.cpp
LOCAL_SRC_FILES += src/globals.cpp
LOCAL_SRC_FILES += src/localization.cpp
LOCAL_SRC_FILES += src/object_detection.cpp
LOCAL_SRC_FILES += src/rs_camera.cpp
LOCAL_C_INCLUDES += $(LOCAL_PATH)/include
LOCAL_C_INCLUDES += $(LOCAL_PATH)/android_include
LOCAL_SHARED_LIBRARIES := realsense2
LOCAL_CFLAGS += -frtti -fexceptions -fopenmp -w
LOCAL_CPPFLAGS += -std=c++11
LOCAL_LDLIBS += -llog
LOCAL_LDFLAGS += -fopenmp



include $(BUILD_SHARED_LIBRARY)
