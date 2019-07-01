// SchemingPlusPlus.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <iostream>
#include <vector>

#include "SchemePlusPlus.h"

using namespace SchemingPlusPlus;

const std::string version = "0.19";

struct ReplState;
typedef void(*ReplModifier)(ReplState&);
struct ReplState {
	bool exit;
	std::string prompt;

	ReplState(std::string prompt = "> ") : exit(false), prompt(prompt) {
	}
};

struct ReplCommand {
	std::string command;
	ReplModifier modifier;
	ReplCommand() : command(""), modifier(nullptr) { }
	ReplCommand(std::string cmd, ReplModifier mod) 
		: command(cmd), modifier(mod) { }
	bool Valid() const { return modifier != nullptr; }
};

void replExit(ReplState &s) { s.exit = true; }
void replHelp(ReplState &s) {
	std::cout << "SchemingPlusPlus v " << version << std::endl;
	std::cout << "The following REPL commands are available:" << std::endl;
	std::cout << "\texit, quit, \\q, help, \\help, \\h" << std::endl;
	std::cout << "\t(tests) to run unit tests" << std::endl;
}

ReplCommand replCommands[] = {
	ReplCommand("exit", replExit),
	ReplCommand("quit", replExit),
	ReplCommand("\\q", replExit),
	ReplCommand("help", replHelp),
	ReplCommand("\\help", replHelp),
	ReplCommand("\\h", replHelp),
	ReplCommand() // Empty entry as last
};

ReplCommand *getCommand(std::string cmd) {
	ReplCommand *command = &replCommands[0];

	while (command->Valid()) {
		if (command->command == cmd)
			return command;
		++command;
	}
	return nullptr;
}
void invokeCommand(std::string cmd, ReplState &state) {
	ReplCommand *r = getCommand(cmd);
	if (r != nullptr) {
		r->modifier(state);
	}
}

Core::SchemeCell run_tests(const Core::VectorType &a) {
	Tests::RunTests();
	return Core::SchemeConstants::Nil;
}

void repl(Core::EnvironmentType env) {
	ReplState state;
	Core::SchemeSimpleEval evaluator;
	Core::SchemeParser parser;
	Core::SchemeCell env_cell(env);

	// Add unit tests function
	(*env_cell.Environment)["tests"] = run_tests;

	invokeCommand("help", state);
	while(state.exit == false) {
		std::cout << state.prompt;
		std::string line; std::getline(std::cin, line);
		// Check if line is a repl command
		ReplCommand *cmd = getCommand(line);
		if (cmd != nullptr) {
			cmd->modifier(state);
		} else {
			try {
				parser.reset(line);
				Core::SchemeCell read = parser.read();
				Core::SchemeCell result = evaluator.Eval(read, env_cell);
				std::cout << result.ToString() << std::endl;
			} catch (SchemingPlusPlus::Core::critical_error &ce) {
				std::cerr << ce.what() << std::endl;
			}
		}
	}
}

struct {
	bool run_tests = false;
	bool run_repl = false;
	bool show_help = false;
	std::vector<std::string> files;

	bool did_anything = false;

	int exit_value = 0;
} MainState;

int main()
{
	if (MainState.run_tests) {
		bool result = Tests::RunTests();
		MainState.exit_value = result ? 0 : 1;
		MainState.did_anything = true;
	}

	// Default to repl if no filename given
	if (MainState.files.empty()) MainState.run_repl = true;

	if (MainState.run_repl) {
		Core::SchemeEnvironment *env = new Core::SchemeEnvironment();
		Core::EnvironmentType env_t(env);
		Core::SchemeRuntime::AddGlobals(env_t);
		repl(env_t);
		MainState.did_anything = true;
	}

	if (MainState.did_anything == false || MainState.show_help) {
		MainState.exit_value = MainState.did_anything ? 0 : 1;

		std::cerr << "Usage:" << std::endl;
		std::cerr << "-t              Run tests" << std::endl;
		std::cerr << "-h              Show help" << std::endl;
	}

	return MainState.exit_value;
}

