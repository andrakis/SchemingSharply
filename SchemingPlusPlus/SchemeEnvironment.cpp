#include <assert.h>
#include <stdexcept>

#include "SchemeAssert.h"
#include "SchemeCell.h"
#include "SchemeEnvironment.h"

namespace SchemingPlusPlus {
	namespace Core {
		SchemeEnvironment::SchemeEnvironment(const VectorType &keys, const VectorType &values, EnvironmentType outer) {
			_outer = outer;
			AddRange(keys, values);
		}
		SchemeEnvironment::SchemeEnvironment(const SchemeCell &keys, const SchemeCell &values, EnvironmentType outer) {
			_outer = outer;

			VectorType lc_keys;
			VectorType lc_values;
			if (keys.Type == LIST) {
				// List of keys and values
				lc_keys.insert(lc_keys.end(), keys.ListValue.cbegin(), keys.ListValue.cend());
				lc_values.insert(lc_values.end(), values.ListValue.cbegin(), values.ListValue.cend());
			} else {
				// Single key capturing multiple values
				lc_keys.push_back(keys);
				lc_values.push_back(values);
			}
			AddRange(lc_keys, lc_values);
		}
		void SchemeEnvironment::AddRange(const VectorType &keys, const VectorType &values) {
			assert(keys.size() == values.size());
			for (auto it1 = keys.cbegin(), it2 = values.cbegin();
				 it1 != keys.cend() && it2 != values.cend();
				 ++it1, ++it2) {
				Insert((*it1).Value, *it2);
			}
		}
		void SchemeEnvironment::Insert(std::string key, const SchemeCell &value) {
			_map.emplace(key, value);
		}
		bool SchemeEnvironment::Has(const std::string &key) const {
			if (_map.find(key) != _map.cend())
				return true;
			if (_outer)
				return _outer->Has(key);
			return false;
		}
		SchemeEnvironment::MapType &SchemeEnvironment::Find(std::string key) SCHEME_THROW {
			if (_map.find(key) != _map.cend())
				return _map;
			if (_outer != nullptr)
				return _outer->Find(key);
			throw critical_error(CRIT_SYMBOL_NOT_FOUND, SchemeCell(key, SYMBOL));
		}
#pragma warning( disable : 4290 )
		SchemeEnvironment::MapType SchemeEnvironment::Find(std::string key) const SCHEME_THROW {
			if (_map.find(key) != _map.cend())
				return _map;
			if (_outer != nullptr)
				return _outer->Find(key);
			throw critical_error(CRIT_SYMBOL_NOT_FOUND, SchemeCell(key, SYMBOL));
		}
		SchemeCell &SchemeEnvironment::Lookup(std::string key) SCHEME_THROW {
			auto map = Find(key);
			return map[key];
		}
		SchemeCell SchemeEnvironment::Lookup(std::string key) const SCHEME_THROW {
			auto map = Find(key);
			return map[key];
		}
		SchemeCell &SchemeEnvironment::Lookup(const SchemeCell &key) SCHEME_THROW {
			runtime_assert(key.Type == SYMBOL || key.Type == STRING);
			auto map = Find(key.Value);
			return map[key.Value];
		}
		SchemeCell SchemeEnvironment::Lookup(const SchemeCell &key) const SCHEME_THROW {
			runtime_assert(key.Type == SYMBOL || key.Type == STRING);
			auto map = Find(key.Value);
			return map[key.Value];
		}
		SchemeCell SchemeEnvironment::Set(const SchemeCell &key, const SchemeCell &value) SCHEME_THROW {
			runtime_assert(key.Type == STRING || key.Type == SYMBOL);
			auto map = Find(key.Value);
			map[key.Value] = value;
			return value;
		}
		SchemeCell SchemeEnvironment::Define(const SchemeCell &key, const SchemeCell &value) SCHEME_THROW {
			runtime_assert(key.Type == STRING || key.Type == SYMBOL);
			_map.emplace(key.Value, value);
			return value;
		}
		SchemeCell &SchemeEnvironment::operator[] (const std::string &key) {
			return _map[key];
		}
		SchemeCell &SchemeEnvironment::operator[] (const char *key) {
			return this->operator[](std::string(key));
		}
	}
}
