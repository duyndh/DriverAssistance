// CoreLibrary.cpp : Defines the exported functions for the DLL application.
//

#include "stdafx.h"

void TestImage(const char *fileName)
{
	Mat img = imread(fileName);
	imshow("Test", img);
}