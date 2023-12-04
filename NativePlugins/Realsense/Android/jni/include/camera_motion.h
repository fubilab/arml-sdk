#ifndef CAMERA_MOTION_H
#define CAMERA_MOTION_H

#include <vector>
#include <memory>

#include <opencv2/core.hpp>
#include <Eigen/Core>
#include <Eigen/Geometry>




class Keyframe {
public:
    
    Keyframe() {

    }


    Keyframe(int id, const cv::Mat& frame, const cv::Mat& descriptors,
             const std::vector<cv::KeyPoint>& keypoints, const cv::Mat& worldTranslation, const cv::Mat& worldRot,
             const std::string& imageName, const cv::Mat& relativeTrans, const cv::Mat& relativeRot)
        : id(id), frame(frame), descriptors(descriptors), keypoints(keypoints),
          worldTranslation(worldTranslation), worldRot(worldRot), imageName(imageName),
          relativeTrans(relativeTrans), relativeRot(relativeRot) {
    }

    int getId() const {
        return id;
    }

    const cv::Mat& getFrame() const {
        return frame;
    }

    const cv::Mat& getDescriptors() const {
        return descriptors;
    }

    const std::vector<cv::KeyPoint>& getKeypoints() const {
        return keypoints;
    }

    const cv::Mat& getWorldTrans() const {
        return worldTranslation;
    }

    const cv::Mat& getWorldRot() const {
        return worldRot;
    }

    std::string getImageName() const {
        return imageName;
    }

    const cv::Mat& getRelativeTrans() const {
        return relativeTrans;
    }

    const cv::Mat& getRelativeRot() const {
        return relativeRot;
    }

    void serialize(cv::FileStorage& fs) const {
        fs << "{" << "id" << id;
        fs << "frame" << frame;
        fs << "descriptors" << descriptors;
        fs << "keypoints" << "[";
        for (const auto& keypoint : keypoints) {
            fs << "{:" << "x" << keypoint.pt.x << "y" << keypoint.pt.y << "}";
        }
        fs << "]";
        fs << "worldTranslation" << worldTranslation;
        fs << "worldRot" << worldRot;
        fs << "imageName" << imageName;
        fs << "relativeTrans" << relativeTrans;
        fs << "relativeRot" << relativeRot;
        fs << "}";
    }

    void deserialize(cv::FileNode node) {
        node["id"] >> id;
        node["frame"] >> frame;
        node["descriptors"] >> descriptors;
        cv::FileNode keypointsNode = node["keypoints"];
        for (cv::FileNodeIterator it = keypointsNode.begin(); it != keypointsNode.end(); ++it) {
            cv::KeyPoint keypoint;
            (*it)["x"] >> keypoint.pt.x;
            (*it)["y"] >> keypoint.pt.y;
            keypoints.push_back(keypoint);
        }
        node["worldTranslation"] >> worldTranslation;
        node["worldRot"] >> worldRot;
        node["imageName"] >> imageName;
        node["relativeTrans"] >> relativeTrans;
        node["relativeRot"] >> relativeRot;
    }

public:
    int id;
    cv::Mat frame;
    cv::Mat descriptors;
    std::vector<cv::KeyPoint> keypoints;
    cv::Mat worldTranslation;
    cv::Mat worldRot;
    std::string imageName;
    cv::Mat relativeTrans;
    cv::Mat relativeRot;
};

class KeyframeContainer {
public:
    std::vector<std::shared_ptr<Keyframe>> keyframes;

    void addKeyframe(std::shared_ptr<Keyframe> keyframe) {
        keyframes.push_back(keyframe);
    }

    std::shared_ptr<Keyframe> getKeyframe(int index) const {
        if (index >= 0 && index < keyframes.size()) {
            return keyframes[index];
        } else {
            throw std::out_of_range("Index out of bounds");
        }
    }

    int getKeyframeCount() const {
        return keyframes.size();
    }

    void clear() {
        keyframes.clear();
    }

    void serialize(const std::string& filename) const {
        cv::FileStorage fs(filename, cv::FileStorage::WRITE);
        fs << "Keyframes" << "[";
        for (const std::shared_ptr<Keyframe>& keyframe : keyframes) {
            keyframe->serialize(fs);
        }
        fs << "]";
        fs.release();
    }

    void deserialize(const std::string& filename) {
        cv::FileStorage fs(filename, cv::FileStorage::READ);
        cv::FileNode keyframesNode = fs["Keyframes"];
        keyframes.clear();
        for (cv::FileNodeIterator it = keyframesNode.begin(); it != keyframesNode.end(); ++it) {
            std::shared_ptr<Keyframe> keyframe = std::make_shared<Keyframe>();
            keyframe->deserialize(*it);
            keyframes.push_back(keyframe);
        }
        fs.release();
    }
};

struct systemConfig {
    float ratioTresh;
    float minDepth;
    float maxDepth;
    int min3DPoints;
    float maxDistanceF2F;
    int minFeaturesLoopClosure;
    int framesUntilLoopClosure;
    float noMovementThresh;
    int framesNoMovement;
    int maxGoodFeatures;
    int minFeaturesFindObject;
};

void bestMatchesFilter(std::vector<cv::DMatch>& goodMatches, std::vector<cv::DMatch>& bestMatches);
void matchingAndFilteringByDistance(const cv::Mat& descriptors1, const std::vector<cv::KeyPoint>& kp1Filtered,
                                    std::vector<cv::Point2f>& pts1, std::vector<cv::Point2f>& pts2);
void filterKeypointsByROI(const std::vector<cv::KeyPoint>& keypoints, const cv::Mat& descriptors,
                          std::vector<cv::KeyPoint>& filteredKeypoints, cv::Mat& filteredDescriptors, cv::Rect &zone);
void featureDetection(const cv::Mat& img, std::vector<cv::KeyPoint>& keypoints1, cv::Mat& descriptors1);
void preprocessImage(cv::Mat& inputImage, cv::Mat& colorMat);



#ifdef __cplusplus
extern "C" {
    #endif

    void firstIteration();
    void findFeatures();
    float getDepthAtCenter();
    void getTranslationVector(float* t_f_data);
    void getCameraRotation(float* R_f_data);
    void getCameraOrientation(float* cameraAngle);
    void createORB(int nfeatures,
                   float scaleFactor,
                   int nlevels,
                   int edgeThreshold,
                   int firstLevel,
                   int WTA_K,
                   int scoreType,
                   int patchSize,
                   int fastThreshold); 	

    void setParams(systemConfig config); 		
    const uchar* getJpegBuffer(int* bufferSize);	
    void resetOdom();
    void addKeyframe();
    bool isLoop();
    void setProjectorZone(int sectionX, int sectionY, int sectionWidth, int sectionHeight);
    void serializeKeyframeData(const char* fileName);
    void deserializeKeyframeData(const char* fileName);
    
#ifdef __cplusplus
}
#endif

#endif // CAMERA_MOTION_H
