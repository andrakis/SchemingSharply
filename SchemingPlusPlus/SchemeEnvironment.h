#pragma once

#include "Scheme.h"
#include "SchemeCell.h"
#include "SchemeEnvironment.h"

#include <map>

namespace SchemingPlusPlus {
	namespace Core {
		class SchemeEnvironment {
		public:
			typedef std::map<std::string, SchemeCell> MapType;
			SchemeEnvironment(EnvironmentType outer = nullptr) {
				_map = MapType();
				_outer = outer;
			}
			SchemeEnvironment(const VectorType &keys, const VectorType &values, EnvironmentType outer);
			SchemeEnvironment(const SchemeCell &keys, const SchemeCell &values, EnvironmentType outer);

			void AddRange(const VectorType &keys, const VectorType &values);
			void Insert(std::string key, const SchemeCell &value);
			MapType &Find(std::string key) SCHEME_THROW;
			bool Has(const std::string &key) const;
			MapType Find(std::string key) const SCHEME_THROW;
			SchemeCell &Lookup(std::string key) SCHEME_THROW;
			SchemeCell &Lookup(const SchemeCell &key) SCHEME_THROW;
			SchemeCell Lookup(std::string key) const SCHEME_THROW;
			SchemeCell Lookup(const SchemeCell &key) const SCHEME_THROW;
			// Set - set an existing key to value
			SchemeCell Set(const SchemeCell &key, const SchemeCell &value) SCHEME_THROW;
			// Define - create a new key set to value
			SchemeCell Define(const SchemeCell &key, const SchemeCell &value) SCHEME_THROW;
			std::string ToString() const;
			SchemeCell &operator[] (const std::string &key);
			SchemeCell &operator[] (const char *key);
		private:
			MapType _map;
			EnvironmentType _outer;
		};
	}
}
