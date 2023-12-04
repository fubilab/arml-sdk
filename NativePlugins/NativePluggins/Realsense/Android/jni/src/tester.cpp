#include <vector>
#include <string>
#include <chrono>
#include <iostream>
#include <thread>

#include <camera_motion.h>
#include <debugCPP.h>
#include <opencv2/opencv.hpp>
#include <librealsense2/rs.hpp>

#include "cv-helpers.hpp"
#include <globals.h>
#include <localization.h>





#include <boost/program_options.hpp>



int main(int argc, char const *argv[]) {
    bool record = false;
    int fps_color = 60;
    int fps_depth = 60;
    int width = 640;
    int height = 480;
    int width_depth = 640;
    int height_depth = 480;

    std::string externalFilePath = "";

    namespace po = boost::program_options;

    // Define and configure Boost Program Options
    po::options_description desc("Allowed options");
    desc.add_options()
        ("help", "Produce help message")
        ("externalFilePath", po::value<std::string>(), "External file path of a recorded bagfile");

    po::variables_map vm;
    po::store(po::parse_command_line(argc, argv, desc), vm);
    po::notify(vm);

    if (vm.count("help")) {
        std::cout << desc << std::endl;
        return 1;
    }

    if (vm.count("externalFilePath")) {
        externalFilePath = vm["externalFilePath"].as<std::string>();
    }

    std::cout << externalFilePath << std::endl;
    // Check if the external file path is not empty, and set 'record' to true
    if (!externalFilePath.empty()) {
        const char* bagFileAddress = externalFilePath.c_str();
        camera.bagFileStreamConfig(bagFileAddress);
    } else {
        camera.colorStreamConfig(width, height, fps_color);
        camera.depthStreamConfig(width_depth, height_depth, fps_depth);
    }
    camera.initCamera();
    camera.initImu();

    systemConfig config;
    config.ratioTresh = 0.5;
    config.minDepth = 0.3;
    config.maxDepth = 6;
    config.min3DPoints = 6;
    config.maxDistanceF2F = 0.5;
    config.minFeaturesLoopClosure = 100;
    config.framesUntilLoopClosure = 200;
    config.noMovementThresh = 0.5;
    config.framesNoMovement = 50;
    config.maxGoodFeatures = 1000;
    config.minFeaturesFindObject = 30;

    setParams(config);
    
    int nfeatures = 3000;
    float scaleFactor = 2;
    int nlevels = 3;
    int edgeThreshold = 19;
    int firstLevel = 0;
    int WTA_K = 2;
    int scoreType = 0;
    int patchSize = 31;
    int fastThreshold = 20;
    createORB(nfeatures,
              scaleFactor,
              nlevels,
              edgeThreshold,
              firstLevel,
              WTA_K,
              scoreType,
              patchSize,
              fastThreshold);

    bool should_break = false;
    std::vector<cv::Mat> objectImages;
    std::vector<std::string> imageNames;
    std::string fileName = "keyframes.yml";
    int recover_info = false;
    // if (!recover_info) {
    //     processImages("/home/fubintlab/libraries/magic_lantern_unity/NativePluggins/Realsense/Linux/images", objectImages, imageNames);
    //     std::cout << "cantidad images en folder: " << objectImages.size() << std::endl;
    //     std::cout << "cantidad images en contaienr: " << objectContainer.getObjectCount() << std::endl;
    // } else {
    //     container.deserialize(fileName);
    // }
    
    

    firstIteration();
    while (!should_break) {
        
        findFeatures();
        // bool value = isLoop();
        // std::cout << "Loop Closure: " << value << std::endl;
        int key = cv::waitKey(1);
        if (key >= 0)
        {
            if (key == 113) {
                should_break = true;
            }
         else if (key == 99) {
                addKeyframe();
            }
        }
    }
    while (true) {
        int key = cv::waitKey(1);
        if (key >= 0) {
            if (key == 113) {
                break;
            }
        }
    }
    // serializing the content of keyframes
    // std::cout << "Serializing keyframes..." << std::endl;
    
    // container.serialize(fileName);
    // std::cout << "File " << fileName << " saved correctly!" << std::endl;
    camera.cleanupCamera();
    return 0;
}
