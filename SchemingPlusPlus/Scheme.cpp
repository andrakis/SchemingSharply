#include <ctype.h>
#include <exception>
#include <stdexcept>

#include "Scheme.h"
#include "SchemeAssert.h"
#include "SchemeCell.h"
#include "SchemeEnvironment.h"

namespace SchemingPlusPlus {
	namespace Core {
		std::string SchemeConstants::NilValue = "#nil";
		std::string SchemeConstants::FalseValue = "#false";
		std::string SchemeConstants::TrueValue = "#true";

		SchemeCell SchemeConstants::Nil(SchemeConstants::NilValue);
		SchemeCell SchemeConstants::False(SchemeConstants::NilValue);
		SchemeCell SchemeConstants::True(SchemeConstants::TrueValue);

		std::string CellTypeToString(CellType type) {
			switch (type) {
				case SYMBOL: return "SYMBOL";
				case STRING: return "STRING";
				case INTEGER: return "INTEGER";
				case FLOAT: return "FLOAT";
				case LIST: return "LIST";
				case LAMBDA: return "LAMBDA";
				case MACRO: return "MACRO";
				case PROC: return "PROC";
				case PROCENV: return "PROCENV";
				case ENVPTR: return "ENVPTR";
				default: {
					std::string message = "Unknown typeid: ";
					message += std::to_string(type);
					return message;
				}
			}
		}

		std::string enquote(std::string str, char start, char end) {
			std::string result = "";
			result += start;
			result += str;
			result += end;
			return result;
		}

		std::string critical_error::generate_message(ErrorType code, const SchemeCell &reason) const {
			std::string message = "Runtime error ";
			message += readable_error(code) + ": ";
			message += reason.ToString();
			return message;
		}

		std::string critical_error::generate_message(ErrorType code, std::string reason) const {
			std::string message = "Runtime error ";
			message += readable_error(code) + ": ";
			message += reason;
			return message;
		}

		/// Scheme Runtime methods
		TokenVector Tokenise(const std::string &str) SCHEME_THROW {
			TokenVector tokens;
			const char *s = str.c_str();
			while (*s) {
				while (isspace(*s))
					++s;
				if (*s == ';' && *(s+1) == ';')
					while (*s && *s != '\n' && *s != '\r') ++s;
				else if (*s == '(' || *s == ')')
					tokens.push_back(*s++ == '(' ? "(" : ")");
				else if (*s == '"' || *s == '\'') {
					const char *t = s;
					const char sp = *s;
					int escape = 0;
					do {
						++t;
						if (escape != 0) escape--;
						if(*t == '\\')
							escape = 2; // skip this and the next character
					} while (*t && (escape != 0 || *t != sp));
					++t;
					tokens.push_back(std::string(s, t));
					s = t;
				} else {
					const char *t = s;
					while (*t && !isspace(*t) && *t != '(' && *t != ')')
						++t;
					tokens.push_back(std::string(s, t));
					s = t;
				}
			}
			return tokens;
		}

		SchemeCell Atom(const std::string &token) SCHEME_THROW {
			if (isdigit(token[0]) || (token[0] == '-' && isdigit(token[1]))) {
				// Number
				if (token.find(".") == std::string::npos)
					return SchemeCell(token, INTEGER);
				return SchemeCell(token, FLOAT);
			} else if (token[0] == QUOTE_DOUBLE) { // "String"
				auto len = token.length();
				runtime_assert(token[len - 1] == QUOTE_DOUBLE);
				return SchemeCell(token.substr(1, len - 2), STRING);
			}	
			return SchemeCell(token, SYMBOL);
		}

		SchemeCell ReadFrom(TokenVector &tokens) SCHEME_THROW {
			runtime_assert(tokens.empty() == false);

			const std::string token(tokens.front());
			tokens.pop_front();
			if (token == "(") {
				SchemeCell c("", LIST);
				while (tokens.front() != ")") {
					c.ListValue.push_back(ReadFrom(tokens));
					runtime_assert(tokens.empty() == false);
				}
				runtime_assert(tokens.empty() == false);
				tokens.pop_front();
				return c;
			}
			return Atom(token);
		}

		SchemeCell Read(const std::string &s) SCHEME_THROW {
			TokenVector tokens(Tokenise(s));
			return ReadFrom(tokens);
		}
	}
}

