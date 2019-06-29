#pragma once

#include <list>
#include <map>
#include <memory>
#include <string>
#include <stdexcept>
#include <vector>

#if _MSC_VER
#define COMPILER_MSC
#define THROW(x) throw(...)
#else
#define THROW(x) throw(x)
#endif

#define SCHEME_THROW THROW(critical_error)

namespace SchemingPlusPlus {
	namespace Core {
		enum CellType {
			SYMBOL,
			STRING,
			INTEGER,
			FLOAT,
			LIST,
			LAMBDA,
			MACRO,
			PROC,
			PROCENV,
			ENVPTR
		};

		// Convert CellType type to string.
		std::string CellTypeToString(CellType type);

		// Enquote a string
		enum { QUOTE_SINGLE = '\'', QUOTE_DOUBLE = '"' };
		std::string enquote(std::string str, char start = QUOTE_DOUBLE, char end = QUOTE_DOUBLE);

		struct SchemeCell;
		class SchemeEnvironment;

		typedef long long IntegerType;
		typedef double FloatType;
		typedef std::vector<SchemeCell> VectorType;
		typedef std::shared_ptr<SchemeEnvironment> EnvironmentType;
		typedef SchemeCell(*ProcType)(const VectorType &);
		typedef SchemeCell(*ProcEnvType)(const VectorType &, EnvironmentType);

		typedef size_t ErrorType;
		enum Errors {
			ERROR_NONE,
			CRIT_RUNTIME_ASSERT,
			CRIT_SYMBOL_NOT_FOUND,
			CRIT_TYPE_NOT_IMPL,
			CRIT_INVALID_COERCE,
			CRIT_INVALID_INDEX,
			CRIT_INVALID_PROC,
			CRIT_OP_INVALID
		};

		class critical_error : public std::runtime_error {
			std::string generate_message(ErrorType code, const SchemeCell &reason) const;
			std::string generate_message(ErrorType code, std::string reason) const;
			std::string readable_error(ErrorType code) const {
				switch (code) {
					case ERROR_NONE: return "(No error)";
					case CRIT_RUNTIME_ASSERT: return "RuntimeAssert";
					case CRIT_SYMBOL_NOT_FOUND: return "Symbol not found";
					case CRIT_TYPE_NOT_IMPL: return "Type not implemented";
					case CRIT_INVALID_COERCE: return "Invalid coersion";
					case CRIT_INVALID_INDEX: return "Index out of range";
					case CRIT_INVALID_PROC: return "Proc not valid";
					case CRIT_OP_INVALID: return "Invalid operation";
				}
				std::string message = "(Unknown: ";
				message += std::to_string(code);
				message += ")";
				return message;
			}
		public:
			critical_error(ErrorType code, const SchemeCell &reason)
				: runtime_error(generate_message(code, reason)) {}
			critical_error(ErrorType code, std::string reason)
				: runtime_error(generate_message(code, reason)) {}
		};

		typedef std::list<std::string> TokenVector;
		TokenVector Tokenise(const std::string &str) SCHEME_THROW;
		SchemeCell Atom(const std::string &token) SCHEME_THROW;
		SchemeCell ReadFrom(TokenVector &tokens) SCHEME_THROW;
		SchemeCell Read(const std::string &s) SCHEME_THROW;

		struct SchemeConstants {
			static std::string NilValue;
			static std::string TrueValue;
			static std::string FalseValue;
			static SchemeCell Nil;
			static SchemeCell False;
			static SchemeCell True;
		};
	}
}
