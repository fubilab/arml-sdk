#ifndef LOCALIZATION_H
#define LOCALIZATION_H

#include <vector>
#include <string>
#include <chrono>
#include <iostream>

#include <camera_motion.h>
#include <opencv2/opencv.hpp>
#include <librealsense2/rs.hpp>
#include <globals.h>



int findBestMatchingKeyframe(const cv::Mat& descriptors1, const std::vector<cv::KeyPoint>& kp1Filtered,
                             std::vector<cv::Point2f>& pts1, std::vector<cv::Point2f>& pts2);
void computeC2MC1(const cv::Mat &R1, const cv::Mat &tvec1, const cv::Mat &R2, const cv::Mat &tvec2,
                  cv::Mat &R_1to2, cv::Mat &tvec_1to2);
void translationCalc(const std::vector<cv::Point2f>& pts1, const std::vector<cv::Point2f>& pts2, cv::Mat& t_1to2,
                     cv::Mat& R_1to2, const rs2::frame& colorFrame, const rs2::depth_frame& depth);

#endif // LOCALIZATION_H
