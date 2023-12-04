#ifndef BNO055_H
#define BNO055_H

#include <cstdio>
#include <fcntl.h>
#include <unistd.h>
#include <linux/i2c-dev.h>
#include <android/log.h>
#include <utility>

const int ACCEL_REGISTER = 0x08;
const float ACCEL_SCALE_FACTOR = 1.0 / 100;
const int GYRO_REGISTER = 0x14;
const float GYRO_SCALE_FACTOR = 1.0 / 16.0;
const int MAG_REGISTER = 0X0E;
const float MAG_SCALE_FACTOR = 1.0 / 16.0;
const int EULER_REGISTER = 0x1a;
const float EULER_SCALE_FACTOR = 1.0 / 16.0;
const int QUATERNION_REGISTER = 0X20;
const float QUATERNION_SCALE_FACTOR = (1.0 / (1 << 14));
const int LINEAR_ACC_REGISTER = 0x28;
const float LINEAR_ACC_SCALE_FACTOR = 1.0 / 100;
const int GRAVITY_REGISTER = 0x2e;
const float GRAVITY_SCALE_FACTOR = 1.0 / 100;
const int WRITE_BUFFER_SIZE = 2;

const int SYS_STAT_ADDR = 0X39;
const int SYS_ERR_ADDR = 0X3A;
const int SELFTEST_RESULT_ADDR = 0X36;


int setupDevice(const char* device_path, int device_address);
int readRegisterData(int fileDescriptor, unsigned char registerAddress, unsigned char* data, size_t dataSize);
void convertSensorData(const unsigned char sensorData[6], float& sensorX, float& sensorY, float& sensorZ, float scaleFactor);
void convertQuat(const unsigned char sensorData[8], float& sensorX, float& sensorY, float& sensorZ, float& sensorW, float scaleFactor);

extern "C" bool setMode(const char* device_path, int device_address, unsigned char register_address, unsigned char mode);
extern "C" float* getAccelerometer(const char* device_path, int device_address);
extern "C" float* getMagnetometer(const char* device_path, int device_address);
extern "C" float* getGyroscope(const char* device_path, int device_address);
extern "C" float* getEuler(const char* device_path, int device_address);
extern "C" float* getQuaternion(const char* device_path, int device_address);
extern "C" float* getLinearAcceleration(const char* device_path, int device_address);
extern "C" float* getGravity(const char* device_path, int device_address);
extern "C" int* getSystemStatus(const char* device_path, int device_address);
extern "C" void closeDevice();


#endif // BNO055_H
