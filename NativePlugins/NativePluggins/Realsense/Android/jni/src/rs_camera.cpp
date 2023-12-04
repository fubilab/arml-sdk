#include "rs_camera.h"
#include <globals.h>

RealSenseCamera::RealSenseCamera() {
}

RealSenseCamera::~RealSenseCamera() {
    cleanupCamera();
}

void RealSenseCamera::initCamera() {
    try {
        profile = pipeline.start(cfg);
    } catch (const rs2::error& e) {
        std::string error_message = "Frame pipeline: " + std::string(e.what());
        Debug::Log(error_message, Color::Red);
    }
}

void RealSenseCamera::initImu() {
    try {
        imu_cfg.enable_stream(RS2_STREAM_ACCEL, RS2_FORMAT_MOTION_XYZ32F);
        imu_cfg.enable_stream(RS2_STREAM_GYRO, RS2_FORMAT_MOTION_XYZ32F);
        auto imu_profile = imu_pipeline.start(imu_cfg, [&](rs2::frame imu_frame) {
            auto motion = imu_frame.as<rs2::motion_frame>();
            if (motion && motion.get_profile().stream_type() == RS2_STREAM_GYRO &&
                motion.get_profile().format() == RS2_FORMAT_MOTION_XYZ32F) {
                double ts = motion.get_timestamp();
                rs2_vector gyro_data = motion.get_motion_data();
                algo.process_gyro(gyro_data, ts);
            }
            if (motion && motion.get_profile().stream_type() == RS2_STREAM_ACCEL &&
                motion.get_profile().format() == RS2_FORMAT_MOTION_XYZ32F) {
                rs2_vector accel_data = motion.get_motion_data();
                algo.process_accel(accel_data);
            }
        });
    } catch (const std::exception& e) {
        std::string error_message = "Imu pipeline: " + std::string(e.what());
        Debug::Log(error_message, Color::Red);
    }
}

void RealSenseCamera::cleanupCamera() {
    try {
        pipeline.stop();
        imu_pipeline.stop();
        std::string message = "Camera pipelines stopped!";
        Debug::Log(message, Color::Red);
    } catch(const std::exception& e) {
        std::string error_message = e.what();
        Debug::Log(error_message, Color::Red);
    }
}

void RealSenseCamera::bagFileStreamConfig(const char* bagFileAddress) {
    try {
        cfg.enable_device_from_file(bagFileAddress, true);
    } catch (const rs2::error& e) {
        std::string error_message = e.what();
        Debug::Log(error_message, Color::Red);
    }
}

void RealSenseCamera::colorStreamConfig(int width, int height, int fps) {
    cfg.enable_stream(RS2_STREAM_COLOR, -1, width, height, RS2_FORMAT_RGB8, fps);
}

void RealSenseCamera::depthStreamConfig(int width, int height, int fps) {
    cfg.enable_stream(RS2_STREAM_DEPTH, -1, width, height, RS2_FORMAT_Z16, fps);
}

rs2::frameset RealSenseCamera::waitForFrames() {
    return pipeline.wait_for_frames();
}

rs2::pipeline_profile RealSenseCamera::getProfile() {
    return profile;
}

void initCamera() {
    camera.initCamera();
}

void initImu() {
    camera.initImu();
}

void cleanupCamera() {
    camera.cleanupCamera();
}

void bagFileStreamConfig(const char* bagFileAddress) {
    camera.bagFileStreamConfig(bagFileAddress);
}

void colorStreamConfig(int width, int height, int fps) {
    camera.colorStreamConfig(width, height, fps);
}

void depthStreamConfig(int width, int height, int fps) {
    camera.depthStreamConfig(width, height, fps);
}




