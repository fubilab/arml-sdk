#include "BNO055.h"
#include <iostream>
#include <cstring>
#include <cstdio>
#include <fcntl.h>
#include <unistd.h>
#include <linux/i2c-dev.h>
#include <android/log.h>
#include <utility>


static int fileDescriptor = -1;

int setupDevice(const char* device_path, int device_address) {
    if (fileDescriptor == -1) {
        fileDescriptor = open(device_path, O_RDWR);
        if (fileDescriptor == -1) {
            __android_log_write(ANDROID_LOG_ERROR, "Error", "Failed to open the port");
            // close(fileDescriptor);
            fileDescriptor = -1;
            return -1;
        }
        if (ioctl(fileDescriptor, I2C_SLAVE, device_address) == -1) {
            // close(fileDescriptor);
            fileDescriptor = -1;
            __android_log_write(ANDROID_LOG_ERROR, "Error", "Failed to read device address");
            return -1;
        }
    }

    return fileDescriptor;
}


int readRegisterData(int fileDescriptor, unsigned char registerAddress, unsigned char* data, size_t dataSize) {
    unsigned char buffer[1];
    buffer[0] = registerAddress;

    if (write(fileDescriptor, buffer, sizeof(buffer)) != sizeof(buffer)) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        __android_log_write(ANDROID_LOG_ERROR, "Error", "Failed to write register address");
        return -1; // Error handling if write fails
    }
    if (read(fileDescriptor, data, dataSize) != static_cast<ssize_t>(dataSize)) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        __android_log_write(ANDROID_LOG_ERROR, "Error", "Failed to read register data");
        return -1; // Error handling if read fails
    }

    return 0;
}

void convertSensorData(const unsigned char sensorData[6], float& sensorX, float& sensorY, float& sensorZ, float scaleFactor) {
    int sensorRawX = (sensorData[1] << 8) | sensorData[0];
    int sensorRawY = (sensorData[3] << 8) | sensorData[2];
    int sensorRawZ = (sensorData[5] << 8) | sensorData[4];

    sensorX = static_cast<float>(static_cast<short>(sensorRawX)) * scaleFactor;
    sensorY = static_cast<float>(static_cast<short>(sensorRawY)) * scaleFactor;
    sensorZ = static_cast<float>(static_cast<short>(sensorRawZ)) * scaleFactor;
}

void convertQuat(const unsigned char sensorData[8], float& sensorX, float& sensorY, float& sensorZ, float& sensorW, float scaleFactor) {
    int sensorRawW = (sensorData[1] << 8) | sensorData[0];
    int sensorRawX = (sensorData[3] << 8) | sensorData[2];
    int sensorRawY = (sensorData[5] << 8) | sensorData[4];
    int sensorRawZ = (sensorData[7] << 8) | sensorData[6];

    sensorX = scaleFactor * static_cast<float>(static_cast<short>(sensorRawX));
    sensorY = scaleFactor * static_cast<float>(static_cast<short>(sensorRawY));
    sensorZ = scaleFactor * static_cast<float>(static_cast<short>(sensorRawZ));
    sensorW = scaleFactor * static_cast<float>(static_cast<short>(sensorRawW));
    
}

extern "C" bool setMode(const char* device_path, int device_address, unsigned char register_address, unsigned char mode) {
    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        return false; // Return the array on error
    }

    unsigned char writeBuffer[WRITE_BUFFER_SIZE] = {register_address, mode};
    if (write(fileDescriptor, writeBuffer, sizeof(writeBuffer)) != sizeof(writeBuffer)) {
        __android_log_write(ANDROID_LOG_ERROR, "Error", "Failed to write register value");
        // close(fileDescriptor);
        fileDescriptor = -1;
        return false;
    }

    // close(fileDescriptor);
    return true;
}

extern "C" float* getAccelerometer(const char* device_path, int device_address) {
    static float accel[3] = {0.0f, 0.0f, 0.0f}; // Initialize a static float array with 3 elements and set them to 0.0f

    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        return accel; // Return the array on error
    }

    unsigned char sensorData[6];
    if (readRegisterData(fileDescriptor, ACCEL_REGISTER, sensorData, sizeof(sensorData)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        return accel; // Return the array on error
    }

    float accelX, accelY, accelZ;

    convertSensorData(sensorData, accelX, accelY, accelZ, ACCEL_SCALE_FACTOR);
    // std::cout << "Angular data: Roll = " << eulerX << ", Pitch = " << eulerY << ", Yaw = " << eulerZ << std::endl;

    // close(fileDescriptor);

    accel[0] = accelX;
    accel[1] = accelY;
    accel[2] = accelZ;

    return accel;
}

extern "C" float* getMagnetometer(const char* device_path, int device_address) {
    static float mag[3] = {0.0f, 0.0f, 0.0f}; // Initialize a static float array with 3 elements and set them to 0.0f

    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        return mag; // Return the array on error
    }

    unsigned char sensorData[6];
    if (readRegisterData(fileDescriptor, MAG_REGISTER, sensorData, sizeof(sensorData)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        return mag; // Return the array on error
    }

    float magX, magY, magZ;
    convertSensorData(sensorData, magX, magY, magZ, MAG_SCALE_FACTOR);
    // std::cout << "Angular data: Roll = " << eulerX << ", Pitch = " << eulerY << ", Yaw = " << eulerZ << std::endl;

    // close(fileDescriptor);

    mag[0] = magX;
    mag[1] = magY;
    mag[2] = magZ;

    return mag;
}

extern "C" float* getGyroscope(const char* device_path, int device_address) {
    static float gyro[3] = {0.0f, 0.0f, 0.0f}; // Initialize a static float array with 3 elements and set them to 0.0f
    
    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        return gyro; // Return the array on error
    }

    unsigned char sensorData[6];
    if (readRegisterData(fileDescriptor, GYRO_REGISTER, sensorData, sizeof(sensorData)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        return gyro; // Return the array on error
    }

    float gyroX, gyroY, gyroZ;
    convertSensorData(sensorData, gyroX, gyroY, gyroZ, GYRO_SCALE_FACTOR);
    // std::cout << "Angular data: Roll = " << eulerX << ", Pitch = " << eulerY << ", Yaw = " << eulerZ << std::endl;

    // close(fileDescriptor);

    gyro[0] = gyroX;
    gyro[1] = gyroY;
    gyro[2] = gyroZ;

    return gyro;
}
   

extern "C" float* getEuler(const char* device_path, int device_address) {
    static float euler[3] = {0.0f, 0.0f, 0.0f}; // Initialize a static float array with 3 elements and set them to 0.0f

    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        return euler; // Return the array on error
    }

    unsigned char sensorData[6];
    if (readRegisterData(fileDescriptor, EULER_REGISTER, sensorData, sizeof(sensorData)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        return euler; // Return the array on error
    }

    float eulerX, eulerY, eulerZ;
    convertSensorData(sensorData, eulerZ, eulerX, eulerY, EULER_SCALE_FACTOR);
    // std::cout << "Angular data: Roll = " << eulerX << ", Pitch = " << eulerY << ", Yaw = " << eulerZ << std::endl;

    // close(fileDescriptor);

    euler[0] = eulerX;
    euler[1] = eulerY;
    euler[2] = eulerZ;

    return euler;
}

extern "C" float* getQuaternion(const char* device_path, int device_address) {
    static float Quaternion[4] = {0.0f, 0.0f, 0.0f, 0.0f}; // Initialize a static float array with 3 elements and set them to 0.0f

    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        return Quaternion; // Return the array on error
    }

    unsigned char sensorData[8];
    if (readRegisterData(fileDescriptor, QUATERNION_REGISTER, sensorData, sizeof(sensorData)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        return Quaternion; // Return the array on error
    }

    float QuatX, QuatY, QuatZ, QuatW;
    convertQuat(sensorData, QuatW, QuatX, QuatY, QuatZ, QUATERNION_SCALE_FACTOR);
    // std::cout << "Angular data: Roll = " << eulerX << ", Pitch = " << eulerY << ", Yaw = " << eulerZ << std::endl;

    // close(fileDescriptor);

    Quaternion[0] = QuatX;
    Quaternion[1] = QuatY;
    Quaternion[2] = QuatZ;
    Quaternion[3] = QuatW;

    return Quaternion;
}


extern "C" float* getLinearAcceleration(const char* device_path, int device_address) {
    static float LinearAcc[3] = {0.0f, 0.0f, 0.0f}; // Initialize a static float array with 3 elements and set them to 0.0f

    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        return LinearAcc; // Return the array on error
    }

    unsigned char sensorData[6];
    if (readRegisterData(fileDescriptor, LINEAR_ACC_REGISTER, sensorData, sizeof(sensorData)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        return LinearAcc; // Return the array on error
    }

    float LinearAccX, LinearAccY, LinearAccZ;
    convertSensorData(sensorData, LinearAccX, LinearAccY, LinearAccZ, LINEAR_ACC_SCALE_FACTOR);
    // std::cout << "Angular data: Roll = " << eulerX << ", Pitch = " << eulerY << ", Yaw = " << eulerZ << std::endl;

    // close(fileDescriptor);

    LinearAcc[0] = LinearAccX;
    LinearAcc[1] = LinearAccY;
    LinearAcc[2] = LinearAccZ;

    return LinearAcc;
}

extern "C" float* getGravity(const char* device_path, int device_address) {
    static float gravity[3] = {0.0f, 0.0f, 0.0f}; // Initialize a static float array with 3 elements and set them to 0.0f

    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        return gravity; // Return the array on error
    }

    unsigned char sensorData[6];
    if (readRegisterData(fileDescriptor, GRAVITY_REGISTER, sensorData, sizeof(sensorData)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        return gravity; // Return the array on error
    }

    float gravityX, gravityY, gravityZ;
    convertSensorData(sensorData, gravityX, gravityY, gravityZ, GRAVITY_SCALE_FACTOR);
    // std::cout << "Angular data: Roll = " << eulerX << ", Pitch = " << eulerY << ", Yaw = " << eulerZ << std::endl;

    // close(fileDescriptor);

    gravity[0] = gravityX;
    gravity[1] = gravityY;
    gravity[2] = gravityZ;

    return gravity;
}

extern "C" int* getSystemStatus(const char* device_path, int device_address) {
    static int status[3] = {-1, -1, -1}; 

    int fileDescriptor = setupDevice(device_path, device_address);
    if (fileDescriptor == -1) {
        return status; // Return the array on error
    }

    unsigned char data0[1];
    if (readRegisterData(fileDescriptor, SYS_STAT_ADDR, data0, sizeof(data0)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        status[0] = -1;
        return status; // Return the array on error
    }

    unsigned char data1[1];
    if (readRegisterData(fileDescriptor, SYS_ERR_ADDR, data1, sizeof(data1)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        status[1] = -1;
        return status; // Return the array on error
    }

    unsigned char data2[1];
    if (readRegisterData(fileDescriptor, SELFTEST_RESULT_ADDR, data2, sizeof(data2)) != 0) {
        // close(fileDescriptor);
        fileDescriptor = -1;
        status[2] = -1;
        return status; // Return the array on error
    }

    status[0] = static_cast<int>(data0[0]);
    status[1] = static_cast<int>(data1[0]);
    status[2] = static_cast<int>(data2[0]);

    return status;
}

extern "C" void closeDevice() {
    if (fileDescriptor != -1) {
        close(fileDescriptor);
        fileDescriptor = -1;
    }
}