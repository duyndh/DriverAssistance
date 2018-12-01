// stdafx.h : include file for standard system include files,
// or project specific include files that are used frequently, but
// are changed infrequently
//

#pragma once

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN             // Exclude rarely-used stuff from Windows headers
// Windows Header Files
#include <windows.h>

#include <opencv2/core/core.hpp>
#include <opencv2/highgui/highgui.hpp>

#pragma comment(lib, "opencv_world343d.lib")
//#pragma comment(lib, "opencv_world343.lib")

#include <iostream>

using namespace std;
using namespace cv;

// reference additional headers your program requires here
void TestImage(const char *fileName);