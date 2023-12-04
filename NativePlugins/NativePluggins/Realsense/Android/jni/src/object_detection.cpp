#include <globals.h>
#include <object_detection.h>
#include <camera_motion.h>




void extractObjectsFeatures(cv::Mat& image, std::string& imageName) {
    int id = objectContainer.getObjectCount();
    std::vector<cv::KeyPoint> kp1;
    cv::Mat descriptors1;
    featureDetection(image, kp1, descriptors1);
    std::shared_ptr<Object> object1 = std::make_shared<Object>(id,
                                                               image.clone(),
                                                               descriptors1.clone(),
                                                               kp1,
                                                               imageName);

    objectContainer.addObject(object1);
}

int findObject(cv::Mat descriptors1, std::vector<cv::KeyPoint> kp1, std::vector<cv::Point2f>& pts1, std::vector<cv::Point2f>& pts2) {
    int bestObjectId = -1;
    int mostGoodMatches = 0;
    std::vector<std::vector<cv::DMatch>> matches;
    std::vector<cv::DMatch> goodMatches;
    std::vector<cv::DMatch> goodMatches_aux;
    const int objectCount = objectContainer.getObjectCount();
    for (int objectIndex = 0; objectIndex < objectCount; objectIndex++) {
        matcher->knnMatch(descriptors1, objectContainer.getObject(objectIndex)->getDescriptors(), matches, 2);
        if (!goodMatches_aux.empty()) {
            goodMatches_aux.clear();
        }
        for (size_t i = 0; i < matches.size(); i++) {
            if (matches[i].size() >= 2 && matches[i][0].distance < ratioTresh * matches[i][1].distance) {
                goodMatches_aux.push_back(matches[i][0]);
            }
        }
        matches.clear();
        if (goodMatches_aux.size() >= minFeaturesFindObject && goodMatches_aux.size() > mostGoodMatches) {
            goodMatches.clear();
            bestObjectId = objectIndex;
            goodMatches = goodMatches_aux;
        }
    }
    if (bestObjectId != -1) {
        const auto& kpObject = objectContainer.getObject(bestObjectId)->getKeypoints();
        std::vector<cv::DMatch> best_matches;
        bestMatchesFilter(goodMatches, best_matches);
        if (!best_matches.empty()) {
            for (const cv::DMatch& match : best_matches) {
                pts1.push_back(kp1[match.queryIdx].pt);
                pts2.push_back(kpObject[match.trainIdx].pt);
                circle(imageFeatures, pts1.back(), 1, cv::Scalar(0, 0, 255), 5);
                cv::Point2f pt1 = pts1.back();
                cv::Point2f pt2 = pts2.back();
                line(imageFeatures, pt1, pt2, cv::Scalar(0, 0, 255), 1);
            }
            
        }
    }
    return bestObjectId;
}

void processImages(const void* imageBytes, int imageBytesSize, const char* imageName) {
    if (imageBytes == nullptr || imageBytesSize <= 0) {
        return;
    }
    std::vector<uchar> imageBuffer(imageBytesSize);
    std::copy(static_cast<const uchar*>(imageBytes), static_cast<const uchar*>(imageBytes) + imageBytesSize, imageBuffer.begin());
    cv::Mat image = cv::imdecode(imageBuffer, cv::IMREAD_COLOR);
    std::string imageNameStr(imageName);
    if (!image.empty()) {
        std::string name(imageName);
        extractObjectsFeatures(image, name);
    }
}



