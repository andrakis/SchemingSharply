#include <ctype.h>

#include "Scheme.h"
#include "SchemeParser.h"

namespace SchemingPlusPlus {
	namespace Core {
		SchemeParser::SchemeParser() {
			this->str = "";
		}
		SchemeParser::SchemeParser(const std::string &str) {
			this->str = str;
		}
		SchemeParser::~SchemeParser() {}

		SchemeCell SchemeParser::TokeniseString(const std::string &str) {
			SchemeParser parser(str);
			return parser.read();
		}

		TokenVectorType SchemeParser::tokenise(const std::string &str) const {
			return Tokenise(str);
			TokenVectorType tokens;
			const char *s = str.c_str();
			while (*s) {
				while (isspace(*s))
					++s;
				if (*s == '(' || *s == ')')
					tokens.push_back(*s++ == '(' ? "(" : ")");
				else {
					const char *t = s;
					while (*t && !isspace(*t) && *t != '(' && *t != ')')
						++t;
					tokens.push_back(std::string(s, t));
					s = t;
				}
			}
			return tokens;
		}

		bool isdig(char c) { return isdigit(static_cast<unsigned char>(c)) != 0; }
		SchemeCell SchemeParser::atom(const std::string &token) const {
			return Atom(token);
			//if (isdig(token[0]) || (token[0] == '-' && isdig(token[1])))
			//	return SchemeCell(token, INTEGER);
			if (token[0] == '"' && *(token.cend() - 1) == '"')
				return SchemeCell(token.substr(1, token.length() - 1), STRING);
			auto isInteger = detail::TryStringToNumber<IntegerType>(token);
			if (std::get<0>(isInteger))
				return SchemeCell(token, INTEGER);
			auto isFloat = detail::TryStringToNumber<FloatType>(token);
			if (std::get<0>(isFloat))
				return SchemeCell(token, FLOAT);
			return SchemeCell(token, SYMBOL);
		}

		SchemeCell SchemeParser::read() {
			return Read(str);
			TokenVectorType tokens = tokenise(str);
			return readFrom(tokens);
		}

		SchemeCell SchemeParser::readFrom(TokenVectorType &tokens) const {
			return ReadFrom(tokens);
			const std::string token(tokens.front());
			tokens.pop_front();
			if (token == "(") {
				VectorType cells;
				while (tokens.front() != ")")
					cells.push_back(readFrom(tokens));
				tokens.pop_front();
				return SchemeCell(cells);
			} else
				return atom(token);
		}

	}
}
