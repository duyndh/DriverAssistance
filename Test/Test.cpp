// Test.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include "pch.h"

typedef void(__stdcall *_TestImage)(const char*); _TestImage TestImage;

void ImportLibrary(LPCWSTR libraryName)
{
	HMODULE hModule = LoadLibraryW(libraryName);

	TestImage = (_TestImage)GetProcAddress(hModule, "TestImage");
	
}

int main()
{
	ImportLibrary(L"E:\\workspace\\NhapMonUngDungDiDong\\DriverAssistance\\x64\\Debug\\CoreLibrary.dll");
	TestImage("image.png");

	return 0;
}

// Run program: Ctrl + F5 or Debug > Start Without Debugging menu
// Debug program: F5 or Debug > Start Debugging menu

// Tips for Getting Started: 
//   1. Use the Solution Explorer window to add/manage files
//   2. Use the Team Explorer window to connect to source control
//   3. Use the Output window to see build output and other messages
//   4. Use the Error List window to view errors
//   5. Go to Project > Add New Item to create new code files, or Project > Add Existing Item to add existing code files to the project
//   6. In the future, to open this project again, go to File > Open > Project and select the .sln file
