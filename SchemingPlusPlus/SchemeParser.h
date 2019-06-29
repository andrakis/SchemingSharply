#pragma once

#include "SchemePlusPlus.h"
#include <sstream>
#include <string>
#include <tuple>
#include <list>

namespace SchemingPlusPlus {
	namespace Core {
		typedef std::list<std::string> TokenVectorType;

		namespace detail {
			template<typename T>
			std::tuple<bool,T> TryStringToNumber(const std::string &str) {
				T value;
				std::stringstream stream(str);
				stream >> value;
				return std::make_tuple(stream.fail(), value);
			}
		}

		class SchemeParser {
		public:
			SchemeParser(const std::string &);
			~SchemeParser();

			static SchemeCell TokeniseString(const std::string &);

			SchemeCell read();

			TokenVectorType tokenise(const std::string &) const;
			SchemeCell atom(const std::string &) const;
			SchemeCell readFrom(TokenVectorType &) const;

		protected:
			std::string str;
		};
	}
}
