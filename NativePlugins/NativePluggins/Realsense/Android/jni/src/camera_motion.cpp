#include <camera_motion.h>
#include <debugCPP.h>
#include <opencv2/opencv.hpp>
#include <librealsense2/rs.hpp>
#include <Eigen/Dense>
#include <Eigen/Core>
#include <Eigen/Geometry>

#include "cv-helpers.hpp"
#include <globals.h>
#include <localization.h>

#include <vector>
#include <string>
#include <chrono>
#include <iostream>
#include <thread>

float3 algoPrev;
cv::Mat traj;
bool add_keyframe = false;
bool add_keyframe_by_hand = false;
bool is_loop = false;
int no_move_counter = 0;
int sectionX;
int sectionY;
int sectionWidth;
int sectionHeight;
int keyFrameId = 0;

void bestMatchesFilter(std::vector<cv::DMatch>& goodMatches, std::vector<cv::DMatch>& bestMatches) {
    std::sort(goodMatches.begin(), goodMatches.end(), 
                    [](const cv::DMatch& a, const cv::DMatch& b) {
                        return a.distance < b.distance;
                    }
                );
    if (goodMatches.size() >= maxGoodFeatures) {
        bestMatches.assign(goodMatches.begin(), goodMatches.begin() + maxGoodFeatures);
    } else {
        bestMatches = goodMatches;
    }
}

void matchingAndFilteringByDistance(const cv::Mat& descriptors1, const std::vector<cv::KeyPoint>& kp1Filtered,
                                    std::vector<cv::Point2f>& pts1, std::vector<cv::Point2f>& pts2) {
    std::vector<std::vector<cv::DMatch>> matches;
    std::vector<cv::DMatch> good_matches;
    matcher->knnMatch(descriptors1, prevDescriptors, matches, 2);
        for (size_t i = 0; i < matches.size(); i++) {
            if (matches[i].size() >= 2) {
                if (matches[i][0].distance < ratioTresh * matches[i][1].distance) {
                good_matches.push_back(matches[i][0]);
                }
            }
        }
    std::vector<cv::DMatch> best_matches;
    bestMatchesFilter(good_matches, best_matches);
    if (!best_matches.empty()) {
        for (const cv::DMatch &match : best_matches) {
            pts1.push_back(kp1Filtered[match.queryIdx].pt);
            pts2.push_back(prevFeatures[match.trainIdx].pt);
            circle(imageFeatures, pts2.back(), 1, cv::Scalar(255, 0, 0), 1);
            cv::Point2f pt1 = pts1.back();
            cv::Point2f pt2 = pts2.back();
            line(imageFeatures, pt1, pt2, cv::Scalar(255, 0, 0), 1);
        }
    }
    
}

void filterKeypointsByROI(const std::vector<cv::KeyPoint>& keypoints, const cv::Mat& descriptors,
                          std::vector<cv::KeyPoint>& filteredKeypoints, cv::Mat& filteredDescriptors, cv::Rect &zone) {
    for (size_t i = 0; i < keypoints.size(); i++) {
        if (zone.contains(keypoints[i].pt)) {
            continue;
        }
        filteredKeypoints.push_back(keypoints[i]);
        filteredDescriptors.push_back(descriptors.row(i));  
    }
}

void featureDetection(const cv::Mat& img, std::vector<cv::KeyPoint>& keypoints1, cv::Mat& descriptors1) {
    featureExtractor->detect(img, keypoints1);
    featureDescriptor->compute(img, keypoints1, descriptors1);
}

void preprocessImage(cv::Mat& inputImage, cv::Mat& colorMat) {
    // cv::Mat improvedColorMat;
    // cv::Mat lookUpTable(1, 256, CV_8U);
    // uchar* p = lookUpTable.ptr();
    // for( int i = 0; i < 256; ++i)
    // p[i] = cv::saturate_cast<uchar>(pow(i / 255.0, gamma_) * 255.0);
    // LUT(colorMat, lookUpTable, improvedColorMat);
    cv::cvtColor(colorMat, inputImage, cv::COLOR_BGR2GRAY);
    // cv::Ptr<cv::CLAHE> clahe = cv::createCLAHE();
    // clahe->setClipLimit(clipLimit);
    // clahe->setTilesGridSize(cv::Size(tilesGridSize, tilesGridSize));
    // clahe->apply(inputImage, inputImage);
    // cv::fastNlMeansDenoising(inputImage,
                                // inputImage,
                                // filterStrengH,
                                // filterTemplateWindowSize,
                                // filterSearchWindowSize);
}

void firstIteration() {
    rs2::frameset frames = camera.waitForFrames();
    for (int i = 0; i < 30; i++) {
        frames = camera.waitForFrames();
    }
    if (!frames) {
        Debug::Log("One or both frames are null", Color::Red);
        return;
    }
    rs2::frame colorFrame = frames.get_color_frame();
    if (!colorFrame) {
        Debug::Log("Color frame is null", Color::Red);
        return;
    }
    auto colour_profile = colorFrame.get_profile().as<rs2::video_stream_profile>();
    int width = colour_profile.width();
    int height = colour_profile.height();
    colorMat = frame_to_mat(colorFrame);
    cv::Mat grayImage(height, width, CV_8UC1);
    preprocessImage(grayImage, colorMat);
    std::vector<cv::KeyPoint> kp1;
    cv::Mat descriptors1;
    featureDetection(grayImage, kp1, descriptors1);
    std::vector<cv::KeyPoint> kp1Filtered;
    cv::Mat descriptorsFiltered;
    cv::Rect seccionToFilter(sectionX, sectionY, sectionWidth, sectionHeight);
    filterKeypointsByROI(kp1, descriptors1, kp1Filtered, descriptorsFiltered, seccionToFilter);
    std::string imageName = "First Frame";
    cv::Mat relativeRot = cv::Mat::eye(3, 3, CV_64F);
    Keyframe keyframe1(keyFrameId,
                       grayImage.clone(),
                       descriptorsFiltered.clone(),
                       kp1Filtered,
                       t_f.clone(),
                       relativeRot.clone(),
                       imageName,
                       t_f.clone(),
                       relativeRot.clone()
                       );
    container.addKeyframe(std::make_shared<Keyframe>(keyframe1));
    keyFrameId += 1;
}

void findFeatures() {
    rs2::frameset frames = camera.waitForFrames();
    if (!frames) {
        Debug::Log("One or both frames are null", Color::Red);
        return;
    }
    rs2::frame colorFrame = frames.get_color_frame();
    if (!colorFrame) {
        Debug::Log("Color frame is null", Color::Red);
        return;
    }
    rs2::depth_frame depth = frames.get_depth_frame();

    if (!depth) {
        Debug::Log("Depth frame is null", Color::Red);
        return;
    }
    auto colour_profile = colorFrame.get_profile().as<rs2::video_stream_profile>();
    int width = colour_profile.width();
    int height = colour_profile.height();
    if (!(algoPrev.x == 0 && algoPrev.y == 0 && algoPrev.z == 0)) {
        float current_angleX = algo.get_theta().x - algoPrev.x;
        float current_angleY = algo.get_theta().y - algoPrev.y;
        float current_angleZ = algo.get_theta().z - algoPrev.z;
        if ((abs(current_angleX) > noMovementThresh || abs(current_angleY) > noMovementThresh || abs(current_angleZ) > noMovementThresh)) {
            no_move_counter = 0;
        } else {
            no_move_counter += 1;
        }
    }
    colorMat = frame_to_mat(colorFrame);
    imageFeatures = colorMat.clone();
    cv::Mat grayImage(height, width, CV_8UC1);
    preprocessImage(grayImage, colorMat);
    std::vector<cv::KeyPoint> kp1;
    cv::Mat descriptors1;
    featureDetection(grayImage, kp1, descriptors1);
    matcher = cv::makePtr<cv::FlannBasedMatcher>(new cv::flann::LshIndexParams(5, 20, 2));
    std::vector<cv::Point2f> pts1Object, pts2Object;
    int objectId = findObject(descriptors1, kp1, pts1Object, pts2Object);
    std::string objectName;
    if (objectId != -1) {
        objectName = objectContainer.getObject(objectId)->getImageName();
        add_keyframe = true;
    } else {
        objectName = "No Object detected";
    }
    std::vector<cv::KeyPoint> kp1Filtered;
    cv::Mat descriptorsFiltered;
    cv::Rect seccionToFilter(sectionX, sectionY, sectionWidth, sectionHeight);
    filterKeypointsByROI(kp1, descriptors1, kp1Filtered, descriptorsFiltered, seccionToFilter);
    cv::Mat t_prev = cv::Mat::zeros(3, 1, CV_64F);
    cv::Mat t_1to2 = cv::Mat::zeros(3, 1, CV_64F);
    bool addTF = false;
    #ifdef BUILD_EXECUTABLE
        for (int i = 0; i < kp1Filtered.size(); i++) {
            circle(imageFeatures, kp1Filtered[i].pt, 1, cv::Scalar(0, 255, 0), 1);
        }

        traj = cv::Mat::zeros(height, width, CV_8UC3);
        int rows = 10;
        int cols = 10;

        // Calculate the width and height of each cell
        int cellWidth = traj.cols / cols;
        int cellHeight = traj.rows / rows;

        // Draw vertical lines
        for (int i = 1; i < cols; ++i) {
            int x = i * cellWidth;
            cv::line(traj, cv::Point(x, 0), cv::Point(x, traj.rows), cv::Scalar(255, 255, 255), 1);
        }

        // Draw horizontal lines
        for (int i = 1; i < rows; ++i) {
            int y = i * cellHeight;
            cv::line(traj, cv::Point(0, y), cv::Point(traj.cols, y), cv::Scalar(255, 255, 255), 1);
        }
    #endif
    int bestKeyframeId = -1;
    if (!imgColorPrev.empty()) {
        auto matcher_time1 = std::chrono::high_resolution_clock::now();
        std::vector<cv::Point2f> pts1, pts2;
        std::vector<cv::Point2f> pts1Keyframe, pts2Keyframe;
        std::vector<cv::Point2f> pts1ToEstimate, pts2ToEstimate;
        is_loop = false;
        if (kp1Filtered.size() >= 2 && prevFeatures.size() >= 2) {
            std::thread keyFrameThread([&]() {
                bestKeyframeId = findBestMatchingKeyframe(descriptorsFiltered, kp1Filtered, pts1Keyframe, pts2Keyframe);
                 });
            matchingAndFilteringByDistance(descriptorsFiltered, kp1Filtered, pts1, pts2);
            keyFrameThread.join();
            if (bestKeyframeId != -1) {
                is_loop = true;
                pts1ToEstimate = pts1Keyframe;
                pts2ToEstimate = pts2Keyframe;
            } else {
                is_loop = false;
                pts1ToEstimate = pts1;
                pts2ToEstimate = pts2;
            }
            cv::Mat R_1to2 = cv::Mat::eye(3, 3, CV_64F);
            translationCalc(pts1ToEstimate, pts2ToEstimate, t_1to2, R_1to2, colorFrame, depth);
            if (!t_1to2.empty() && !R_1to2.empty()){
                cv::Mat R_1to2Object = cv::Mat::eye(3, 3, CV_64F);
                cv::Mat t_1to2Object = cv::Mat::zeros(3, 1, CV_64F);
                if (pts1Object.size() >= min3DPoints) {
                    translationCalc(pts1Object, pts1ToEstimate, t_1to2Object, R_1to2Object, colorFrame, depth);
                }    
                float distanceX = cv::norm(t_1to2.at<double>(0)-t_prev.at<double>(0));
                float distanceY = cv::norm(t_1to2.at<double>(1)-t_prev.at<double>(1));
                float distanceZ = cv::norm(t_1to2.at<double>(2)-t_prev.at<double>(2));
                if ((distanceX < maxDistanceF2F) && (distanceY < maxDistanceF2F) && (distanceZ < maxDistanceF2F)) {
                    if (is_loop && no_move_counter <= framesNoMovement) {
                            R_f = R_f * container.getKeyframe(bestKeyframeId)->getWorldRot();
                            t_f = t_1to2 + container.getKeyframe(bestKeyframeId)->getWorldTrans();
                    } else {
                        if (no_move_counter <= framesNoMovement) {
                            R_f = R_f * R_1to2;
                            t_f += t_1to2; 
                        }
                    }
                    if (add_keyframe) {
                        Keyframe keyframe2(keyFrameId,
                                           grayImage.clone(),
                                           descriptorsFiltered.clone(),
                                           kp1Filtered,
                                           t_f.clone(),
                                           R_f.clone(),
                                           objectContainer.getObject(objectId)->getImageName(),
                                           t_1to2Object.clone(),
                                           R_1to2Object.clone());
                        container.addKeyframe(std::make_shared<Keyframe>(keyframe2));
                        keyFrameId += 1;
                        add_keyframe = false;
                        std::cout << "New KeyFrame Added" << std::endl;
                        std::string message = "New Keyframe Added related to: " + objectContainer.getObject(objectId)->getImageName();
                        Debug::Log(message, Color::Orange);
                        objectContainer.removeObjectByIndex(objectId);
                    } else if (add_keyframe_by_hand) {
                        std::string name = "By Hand";
                        Keyframe keyframe2(keyFrameId,
                                            grayImage.clone(),
                                            descriptorsFiltered.clone(),
                                            kp1Filtered,
                                            t_f.clone(),
                                            R_f.clone(),
                                            name,
                                            t_1to2.clone(),
                                            R_1to2.clone());
                        container.addKeyframe(std::make_shared<Keyframe>(keyframe2));
                        keyFrameId += 1;
                        add_keyframe_by_hand = false;
                        std::cout << "New KeyFrame Added" << std::endl;
                        std::string message = "New Keyframe Added related to: " + name;
                        Debug::Log(message, Color::Orange);
                    }
                    addTF = true;
                }
            }
        }
    }
    imgColorPrev = grayImage.clone();
    prevFeatures = kp1Filtered;
    prevDescriptors = descriptorsFiltered.clone();
    algoPrev = algo.get_theta();
    if (addTF) {
        t_prev = t_1to2;
        addTF = false;
    }
    #ifdef BUILD_EXECUTABLE
    int x = static_cast<int>((t_f.at<double>(0) / 5) * width);
    int y = static_cast<int>((t_f.at<double>(2) / 5) * height);
    cv::circle(traj, cv::Point(-x+ width / 2, -y+height/2) ,1, CV_RGB(0,255,0), 2);
    // Put the text on the image
    cv::putText(colorMat, objectName, cv::Point(400, 30), cv::FONT_HERSHEY_SIMPLEX, 1, cv::Scalar(0, 0, 255), 2);
    
    int imageWidth = imageFeatures.cols;
    int imageHeight = imageFeatures.rows;

    cv::Mat canvas = cv::Mat::zeros(2 * imageHeight, 2 * imageWidth, imageFeatures.type());

    cv::Mat colorGrayscaleImage;
    cv::cvtColor(grayImage, colorGrayscaleImage, cv::COLOR_GRAY2BGR);
    // cv::Mat prueba;
    // cv::cvtColor(traj, prueba, cv::COLOR_GRAY2BGR);
    // Copy each image onto the canvas at the desired positions
    if ( !colorMat.data || !colorGrayscaleImage.data || !imageFeatures.data || !traj.data ) {
    std::cout<< " --(!) Error reading images " << std::endl;
    } else {
        colorMat.copyTo(canvas(cv::Rect(0, 0, imageWidth, imageHeight)));
        colorGrayscaleImage.copyTo(canvas(cv::Rect(imageWidth, 0, imageWidth, imageHeight)));
        imageFeatures.copyTo(canvas(cv::Rect(0, imageHeight, imageWidth, imageHeight)));
        traj.copyTo(canvas(cv::Rect(imageWidth, imageHeight, imageWidth, imageHeight)));
        cv::imshow("Concatenated Images", canvas);
    }

    #endif
}

float getDepthAtCenter() {
    rs2::frameset frames = camera.waitForFrames();
    if (!frames) {
        Debug::Log("One or both frames are null", Color::Red);
        return 0.0f;
    }
    rs2::frame colorFrame = frames.get_color_frame();
    if (!colorFrame) {
        Debug::Log("Color frame is null", Color::Red);
        return 0.0f;
    }
    rs2::depth_frame depth = frames.get_depth_frame();

    if (!depth) {
        Debug::Log("Depth frame is null", Color::Red);
        return 0.0f;
    }
    auto depth_profile = depth.get_profile().as<rs2::video_stream_profile>();
    auto colour_profile = colorFrame.get_profile().as<rs2::video_stream_profile>();
    auto _depth_intrin = depth_profile.get_intrinsics();
    auto _color_intrin = colour_profile.get_intrinsics();
    auto depth_to_color_extrin = depth_profile.get_extrinsics_to(camera.getProfile().get_stream(RS2_STREAM_COLOR));
    auto color_to_depth_extrin = colour_profile.get_extrinsics_to(camera.getProfile().get_stream(RS2_STREAM_DEPTH));
    auto sensorAuto = camera.getProfile().get_device().first<rs2::depth_sensor>();
    auto depth_scale = sensorAuto.get_depth_scale();
    int width = colour_profile.width();
    int height = colour_profile.height();
    const void* depth_data = depth.get_data();
    const uint16_t* depth_data_uint16 = reinterpret_cast<const uint16_t*>(depth_data);
    cv::Mat cameraMatrix = (cv::Mat_<double>(3, 3) <<
                                    static_cast<double>(_color_intrin.fx), 0.0, static_cast<double>(_color_intrin.ppx),
                                    0.0, static_cast<double>(_color_intrin.fy), static_cast<double>(_color_intrin.ppy),
                                    0.0, 0.0, 1.0);
    cv::Mat distCoeffs = (cv::Mat_<double>(1, 5) << _color_intrin.coeffs[0],
                                                    _color_intrin.coeffs[1],
                                                    _color_intrin.coeffs[2],
                                                    _color_intrin.coeffs[3],
                                                    _color_intrin.coeffs[4]);
    int x = width / 2;
    int y = height / 2;
    float vPixel[2];
    vPixel[0] = x;
    vPixel[1] = y;
    float vPixeldepth[2];
    rs2_project_color_pixel_to_depth_pixel(vPixeldepth, depth_data_uint16, depth_scale, minDepth, maxDepth, &_depth_intrin, &_color_intrin, &depth_to_color_extrin, &color_to_depth_extrin, vPixel);
    float vDepth = depth.get_distance(vPixeldepth[0], vPixeldepth[1]);
    return vDepth;         
}

void createORB(int nfeatures,
               float scaleFactor,
               int nlevels,
               int edgeThreshold,
               int firstLevel,
               int WTA_K,
               int scoreType,
               int patchSize,
               int fastThreshold) {
    cv::ORB::ScoreType score;
    if (scoreType == 1) {
        cv::ORB::ScoreType score = cv::ORB::FAST_SCORE;
    } else {
        cv::ORB::ScoreType score = cv::ORB::HARRIS_SCORE;
    }
    featureExtractor = cv::ORB::create(nfeatures,
                                       scaleFactor,
                                       nlevels,
                                       edgeThreshold,
                                       firstLevel,
                                       WTA_K,
                                       score,
                                       patchSize,
                                       fastThreshold);
    featureDescriptor = cv::ORB::create(nfeatures,
                                        scaleFactor,
                                        nlevels,
                                        edgeThreshold,
                                        firstLevel,
                                        WTA_K,
                                        score,
                                        patchSize,
                                        fastThreshold);
}

void getTranslationVector(float* t_f_data) {
    if (t_f.empty()) {
        return;
    }
    for (int i = 0; i < 3; i++) {
        t_f_data[i] = static_cast<float>(t_f.at<double>(i));
    }
}

void getCameraRotation(float* R_f_data) {
    if (R_f.empty()) {
        return;
    }
    Eigen::Matrix3d eigenRotationMatrix;
        for (int i = 0; i < 3; i++) {
            for (int j = 0; j < 3; j++) {
                eigenRotationMatrix(i, j) = R_f.at<double>(i, j);
            }
        }
    Eigen::Quaterniond quaternion(eigenRotationMatrix);
    R_f_data[0] = static_cast<float>(quaternion.x());
    R_f_data[1] = static_cast<float>(quaternion.y());
    R_f_data[2] = static_cast<float>(quaternion.z());
    R_f_data[3] = static_cast<float>(quaternion.w());
}

void getCameraOrientation(float* cameraAngle) {
    if (algo.get_theta().x == 0 && algo.get_theta().y == 0 && algo.get_theta().z == 0) {
        return;
    }    
    cameraAngle[0] = static_cast<float>(-(algo.get_theta().x * 180 / PI_FL));
    cameraAngle[1] = static_cast<float>(algo.get_theta().y * 180 / PI_FL);
    cameraAngle[2] = static_cast<float>(-(algo.get_theta().z * 180 / PI_FL) - 90);
}

void setParams(systemConfig config) {
    ratioTresh = config.ratioTresh;
    minDepth = config.minDepth;
    maxDepth = config.maxDepth;
    min3DPoints = config.min3DPoints;
    maxDistanceF2F = config.maxDistanceF2F;
    minFeaturesLoopClosure = config.minFeaturesLoopClosure;
    framesUntilLoopClosure = config.framesUntilLoopClosure;
    noMovementThresh = config.noMovementThresh / 10000;
    framesNoMovement = config.framesNoMovement;
    maxGoodFeatures = config.maxGoodFeatures;
    minFeaturesFindObject = config.minFeaturesFindObject;
}

void resetOdom() {
    t_f = cv::Mat::zeros(3, 1, CV_64F);
}

void addKeyframe() {
    add_keyframe_by_hand = true;
}

bool isLoop() {
    return is_loop;
}

void setProjectorZone(int _sectionX, int _sectionY, int _sectionWidth, int _sectionHeight) {
    sectionX = _sectionX;
    sectionY = _sectionY;
    sectionWidth = _sectionWidth;
    sectionHeight = _sectionHeight;
}

void serializeKeyframeData(const char *fileName) {
    std::string fileNameStr(fileName);
    container.serialize(fileName);
    
}

void deserializeKeyframeData(const char *fileName) {
    std::string fileNameStr(fileName);
    container.deserialize(fileName);
}

const uchar* getJpegBuffer(int* bufferSize) {


    std::vector<uchar> jpegBuffer;

    cv::imencode(".jpeg", colorMat, jpegBuffer, std::vector<int>{cv::IMWRITE_JPEG_QUALITY, 100});
    uchar* unityBuffer = new uchar[jpegBuffer.size()];
    memcpy(unityBuffer, jpegBuffer.data(), jpegBuffer.size());
    *bufferSize = jpegBuffer.size();

    return unityBuffer;
}


