#ifndef GLOBALS_H
#define GLOBALS_H

#include <opencv2/core.hpp>
#include <opencv2/opencv.hpp>  
#include <librealsense2/rs.hpp>
#include <rs_camera.h>
#include <rs_motion.h>
#include <camera_motion.h>
#include <localization.h>
#include <object_detection.h>
#include <vector>

extern RealSenseCamera camera;
extern rotation_estimator algo;

extern cv::Ptr<cv::Feature2D> featureExtractor;
extern cv::Ptr<cv::Feature2D> featureDescriptor;
extern cv::Ptr<cv::FlannBasedMatcher> matcher;

extern cv::Mat imgColorPrev;
extern cv::Mat colorMat;
extern std::vector<cv::KeyPoint> prevFeatures;
extern cv::Mat prevDescriptors;
extern cv::Mat t_f;
extern cv::Mat R_f;
extern cv::Mat imageFeatures;

extern KeyframeContainer container;
extern ObjectContainer objectContainer;

extern float ratioTresh;
extern float minDepth;
extern float maxDepth;
extern int min3DPoints;
extern float maxDistanceF2F;
extern int minFeaturesLoopClosure;
extern int framesUntilLoopClosure;
extern float noMovementThresh;
extern int framesNoMovement;
extern int maxGoodFeatures;
extern int minFeaturesFindObject;

#endif // REALSENSECAMERA_H