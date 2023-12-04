#include <localization.h>
#include <camera_motion.h>

int findBestMatchingKeyframe(const cv::Mat& descriptors1, const std::vector<cv::KeyPoint>& kp1Filtered,
                             std::vector<cv::Point2f>& pts1, std::vector<cv::Point2f>& pts2) {
    int bestKeyframeId = -1;
    int mostGoodMatches = 0;

    std::vector<cv::DMatch> goodMatches;
    const int keyframeCount = container.getKeyframeCount();
    for (int keyframeIndex = 0; keyframeIndex < keyframeCount; keyframeIndex++) {
        std::vector<cv::DMatch> goodMatches_aux;
        std::vector<std::vector<cv::DMatch>> matches;
        matcher->knnMatch(descriptors1, container.getKeyframe(keyframeIndex)->getDescriptors(), matches, 2);
        for (size_t i = 0; i < matches.size(); i++) {
            if (matches[i].size() >= 2 && matches[i][0].distance < ratioTresh * matches[i][1].distance) {
                goodMatches_aux.push_back(matches[i][0]);
            }
        }

        if (goodMatches_aux.size() >= minFeaturesLoopClosure && goodMatches_aux.size() > mostGoodMatches) {
            mostGoodMatches = goodMatches_aux.size();
            bestKeyframeId = keyframeIndex;
            goodMatches = goodMatches_aux;
        }
    }

    if (bestKeyframeId != -1) {
        const auto& kpKeyframe = container.getKeyframe(bestKeyframeId)->getKeypoints();
        std::vector<cv::DMatch> best_matches;
        bestMatchesFilter(goodMatches, best_matches);
        if (!best_matches.empty()) {
            for (const cv::DMatch &match : best_matches) {
                pts1.push_back(kp1Filtered[match.queryIdx].pt);
                pts2.push_back(kpKeyframe[match.trainIdx].pt);
                circle(imageFeatures, pts2.back(), 1, cv::Scalar(255, 0, 0), 1);
                cv::Point2f pt1 = pts1.back();
                cv::Point2f pt2 = pts2.back();
                line(imageFeatures, pt1, pt2, cv::Scalar(255, 0, 0), 1);
            }
        }
    }
    return bestKeyframeId;
}


void computeC2MC1(const cv::Mat &R1, const cv::Mat &tvec1, const cv::Mat &R2, const cv::Mat &tvec2,
                  cv::Mat &R_1to2, cv::Mat &tvec_1to2) {
    R_1to2 = R2 * R1.t();
    tvec_1to2 = R2 * (-R1.t()*tvec1) + tvec2;
}

void translationCalc(const std::vector<cv::Point2f>& pts1, const std::vector<cv::Point2f>& pts2, cv::Mat& global_tvec,
                     cv::Mat& global_R, const rs2::frame& colorFrame, const rs2::depth_frame& depth) {
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
    
    std::vector<cv::Point2f> uImagePoints, vImagePoints;
    std::vector<cv::Point3f> vObjectPoints;
    cv::Mat tvec1 = cv::Mat::zeros(3, 1, CV_64F);
    cv::Mat tvec2 = cv::Mat::zeros(3, 1, CV_64F);
    cv::Mat rvec1 = cv::Mat::zeros(3, 1, CV_64F);
    cv::Mat rvec2 = cv::Mat::zeros(3, 1, CV_64F);
    try {
        if (pts1.size() >= 2 && pts2.size() >= 2) {
            for (size_t i = 0; i < pts1.size(); ++i) {
                float vPixel[2];
                vPixel[0] = static_cast<float>(pts1[i].x);
                vPixel[1] = static_cast<float>(pts1[i].y);
                float vPixeldepth[2];
                rs2_project_color_pixel_to_depth_pixel(vPixeldepth, depth_data_uint16, depth_scale, minDepth, maxDepth, &_depth_intrin, &_color_intrin, &depth_to_color_extrin, &color_to_depth_extrin, vPixel);
                if (vPixeldepth[0] >= 0 && vPixeldepth[0] < width &&
                    vPixeldepth[1] >= 0 && vPixeldepth[1] < height) {
                    float vDepth = depth.get_distance(vPixeldepth[0], vPixeldepth[1]);
                    float vPoint[3];
                    rs2_deproject_pixel_to_point(vPoint, &_depth_intrin, vPixeldepth, vDepth);
                    float uPixel[2];
                    uPixel[0] = static_cast<float>(pts2[i].x);
                    uPixel[1] = static_cast<float>(pts2[i].y);
                    if (vDepth > minDepth && vDepth < maxDepth) {
                        vObjectPoints.push_back(cv::Point3f(vPoint[0], vPoint[1], vPoint[2]));
                        vImagePoints.push_back(cv::Point2f(vPixel[0], vPixel[1]));
                        uImagePoints.push_back(cv::Point2f(uPixel[0], uPixel[1]));
                    }
                }
           
            }
        }
        cv::solvePnPRansac(vObjectPoints,
                            vImagePoints,
                            cameraMatrix,
                            distCoeffs,
                            rvec1,
                            tvec1,
                            false,
                            500,
                            8.0f,
                            0.9,
                            cv::noArray(),
                            cv::SOLVEPNP_ITERATIVE);
        cv::solvePnPRansac(vObjectPoints,
                            uImagePoints,
                            cameraMatrix,
                            distCoeffs,
                            rvec2,
                            tvec2,
                            false,
                            500,
                            8.0f,
                            0.9,
                            cv::noArray(),
                            cv::SOLVEPNP_ITERATIVE);

        cv::Mat R1 = cv::Mat::zeros(3, 3, CV_64F);
        cv::Mat R2 = cv::Mat::eye(3, 3, CV_64F);
        cv::Rodrigues(rvec1, R1);
        cv::Rodrigues(rvec2, R2);

        cv::Mat t_1to2 = cv::Mat::zeros(3, 1, CV_64F);
        cv::Mat R_1to2 = cv::Mat::eye(3, 1, CV_64F);
        computeC2MC1(R1, tvec1, R2, tvec2, R_1to2, t_1to2);

        global_R = R_1to2.t();
        global_tvec = -global_R * t_1to2;

    } catch (const cv::Exception& e) {
        std::cerr << "OpenCV Exception: " << e.what() << std::endl;
        std::string error_message =  "OpenCV Exception: " + std::string(e.what());
        Debug::Log(error_message, Color::Red);
    }
 
}
