// SchemingPlusPlus.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>

#include "SchemePlusPlus.h"

using namespace SchemingPlusPlus;

void repl(const std::string &prompt, Core::EnvironmentType env) {
	for (;;) {
		std::cout << prompt;
		std::string line; std::getline(std::cin, line);
	}
}

int main()
{
	bool result = Tests::RunTests();
	return result == true ? 0 : 1;
}

