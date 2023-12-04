#ifndef OBJECT_DETECTION_H
#define OBJECT_DETECTION_H

#include <globals.h>
#include <opencv2/core.hpp>


class Object {
public:
    // Constructor
    Object(int id, const cv::Mat& frame, const cv::Mat& descriptors,
             const std::vector<cv::KeyPoint>& keypoints, const std::string& imageName)
        : id(id), frame(frame), descriptors(descriptors), keypoints(keypoints), imageName(imageName) {
    }

    // Getter functions for member variables
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

    std::string getImageName() const {
        return imageName;
    }

private:
    int id;
    cv::Mat frame;
    cv::Mat descriptors;
    std::vector<cv::KeyPoint> keypoints;
    std::string imageName;

};

class ObjectContainer {
public:
    void addObject(std::shared_ptr<Object> Object) {
        Objects.push_back(Object);
    }
    
    std::shared_ptr<Object> getObject(int index) const {
        if (index >= 0 && index < Objects.size()) {
            return Objects[index];
        } else {
            throw std::out_of_range("Index out of bounds");
        }
    }

    int getObjectCount() const {
        return Objects.size();
    }

    void removeObjectByIndex(int index) {
        if (index >= 0 && index < Objects.size()) {
            Objects.erase(Objects.begin() + index);
        } else {
            throw std::out_of_range("Index out of bounds");
        }
    }
    void clear() {
        Objects.clear();
    }

private:
    std::vector<std::shared_ptr<Object>> Objects; 
};

int findObject(cv::Mat descriptors1, std::vector<cv::KeyPoint> kp1, std::vector<cv::Point2f>& pts1,
               std::vector<cv::Point2f>& pts2);
void extractObjectsFeatures(cv::Mat& image, std::string& imageName);

#ifdef __cplusplus
extern "C" {
    #endif

    void processImages(const void* imageBytes, int imageBytesSize, const char* imageName);
    
    
    

#ifdef __cplusplus
}
#endif

#endif // OBJECT_DETECTION_H