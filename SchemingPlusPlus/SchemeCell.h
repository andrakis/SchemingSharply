#pragma once

#include <ostream>

#include "Scheme.h"
#include "SchemeRuntime.h"

namespace SchemingPlusPlus {
	namespace Core {
		struct SchemeCell {
		public:
			CellType Type;
			std::string Value;
			VectorType ListValue;
			ProcType ProcValue;
			ProcEnvType ProcEnvValue;
			EnvironmentType Environment;

			SchemeCell() : SchemeCell("") { }

			SchemeCell(const std::string &value, CellType type = SYMBOL) {
				Type = type;
				Value = value;
				ListValue = VectorType();
				ProcValue = nullptr;
				ProcEnvValue = nullptr;
				Environment = nullptr;
			}

			SchemeCell(const bool value)
				: SchemeCell(value ? SchemeConstants::TrueValue : SchemeConstants::FalseValue) {}

			SchemeCell(const IntegerType value)
				: SchemeCell(std::to_string(value), INTEGER) {}

			SchemeCell(const VectorType &value, CellType type = LIST) {
				Type = type;
				Value = "";
				ListValue = value;
				ProcValue = nullptr;
				ProcEnvValue = nullptr;
				Environment = nullptr;
			}

			SchemeCell(VectorType::const_iterator start, VectorType::const_iterator end, CellType type = LIST) {
				Type = type;
				Value = "";
				ListValue = VectorType(start, end);
				ProcValue = nullptr;
				ProcEnvValue = nullptr;
				Environment = nullptr;
			}

			SchemeCell(ProcType proc) {
				Type = PROC;
				Value = "";
				ListValue = VectorType();
				ProcValue = proc;
				ProcEnvValue = nullptr;
				Environment = nullptr;
			}

			SchemeCell(ProcEnvType proc) {
				Type = PROCENV;
				Value = "";
				ListValue = VectorType();
				ProcValue = nullptr;
				ProcEnvValue = proc;
				Environment = nullptr;
			}

			SchemeCell(CellType type)
				: SchemeCell("", type) {}

			SchemeCell(EnvironmentType env) {
				Type = ENVPTR;
				Value = "";
				ListValue = VectorType();
				ProcValue = nullptr;
				ProcEnvValue = nullptr;
				Environment = env;
			}

			SchemeCell(const SchemeCell &other) {
				Type = other.Type;
				Value = other.Value;
				ListValue = other.ListValue;
				ProcValue = other.ProcValue;
				ProcEnvValue = other.ProcEnvValue;
				Environment = other.Environment;
			}

			SchemeCell coerce(CellType to) const SCHEME_THROW {
				// Basic types are already stored as strings. No conversion needed.
				if (SchemeRuntime::IsBasicType(Type) && SchemeRuntime::IsBasicType(to))
					return *this;
				// TODO - coerce other types
				std::string message = "Conversion not implemented from ";
				message += std::to_string(Type);
				message += " to ";
				message += std::to_string(to);
				throw critical_error(CRIT_INVALID_COERCE, message);
			}

			// Operators
			bool operator == (const SchemeCell &other) const SCHEME_THROW {
				if (Type != other.Type) {
					if (SchemeRuntime::CanCoerce(Type, other.Type)) {
						const SchemeCell &coerced = other.coerce(Type);
						return *this == coerced;
					}
				}
				switch (Type) {
					case INTEGER: return ToInteger() == other.ToInteger();
					case FLOAT: return ToFloat() == other.ToFloat();
					case SYMBOL: /* Fall through */
					case STRING: return Value == other.Value;
					case LAMBDA: /* Fall through */
					case MACRO: return Environment == other.Environment && ListValue == other.ListValue;
					case LIST: return ListValue == other.ListValue;
					case PROC: return ProcValue == other.ProcValue;
					case PROCENV: return ProcEnvValue == other.ProcEnvValue;
					case ENVPTR: return Environment == other.Environment;
					default:
						throw critical_error(CRIT_TYPE_NOT_IMPL, *this);
				}
			}

			bool operator != (const SchemeCell &other) const SCHEME_THROW {
				return !(*this == other);
			}

			SchemeCell &operator += (const SchemeCell &other) SCHEME_THROW {
				if (Type == INTEGER) {
					IntegerType intval = other.ToInteger();
					Value = std::to_string(ToInteger() + intval);
				} else if (Type == FLOAT) {
					FloatType fltval = other.ToFloat();
					Value = std::to_string(ToFloat() + fltval);
				} else if (Type == STRING) {
					Value += other.Value;
				} else if (Type == LIST) {
					ListValue.insert(ListValue.end(), other.ListValue);
				} else {
					throw critical_error(CRIT_TYPE_NOT_IMPL, *this);
				}
				return *this;
			}

			SchemeCell &operator -= (const SchemeCell &other) SCHEME_THROW {
				if (Type == INTEGER) {
					IntegerType intval = other.ToInteger();
					Value = std::to_string(ToInteger() - intval);
				} else if (Type == FLOAT) {
					FloatType fltval = other.ToFloat();
					Value = std::to_string(ToFloat() - fltval);
				} else {
					throw critical_error(CRIT_TYPE_NOT_IMPL, *this);
				}
				return *this;
			}

			SchemeCell &operator *= (const SchemeCell &other) SCHEME_THROW {
				if (Type == INTEGER) {
					IntegerType intval = other.ToInteger();
					Value = std::to_string(ToInteger() * intval);
				} else if (Type == FLOAT) {
					FloatType fltval = other.ToFloat();
					Value = std::to_string(ToFloat() * fltval);
				} else {
					throw critical_error(CRIT_TYPE_NOT_IMPL, *this);
				}
				return *this;
			}

			SchemeCell &operator /= (const SchemeCell &other) SCHEME_THROW {
				if (Type == INTEGER) {
					IntegerType intval = other.ToInteger();
					Value = std::to_string(ToInteger() / intval);
				} else if (Type == FLOAT) {
					FloatType fltval = other.ToFloat();
					Value = std::to_string(ToFloat() / fltval);
				} else {
					throw critical_error(CRIT_TYPE_NOT_IMPL, *this);
				}
				return *this;
			}

			SchemeCell &operator [](VectorType::size_type index) SCHEME_THROW {
				if (!IsList()) return SchemeConstants::Nil;
				try {
					return ListValue[index];
				} catch (...) {
					throw critical_error(CRIT_INVALID_INDEX, *this);
				}
			}
			SchemeCell operator [](VectorType::size_type index) const SCHEME_THROW {
				if (!IsList()) return SchemeConstants::Nil;
				try {
					return ListValue[index];
				} catch (...) {
					throw critical_error(CRIT_INVALID_INDEX, *this);
				}
			}

			operator IntegerType() const { return ToInteger(); }
			IntegerType ToInteger() const {
				if (Type == FLOAT)
					return (IntegerType)ToFloat();
				try {
					return std::stoll(Value);
				} catch (...) {
					return 0;
				}
			}

			operator FloatType() const { return ToFloat(); }
			FloatType ToFloat() const {
				if (Type != FLOAT)
					return (FloatType)ToInteger();
				try {
					return std::stod(Value);
				} catch (...) {
					return 0;
				}
			}
			// Convert to string. Pass true to return as expression.
			std::string ToString(bool expr = false) const SCHEME_THROW;

			// List functions
			bool IsList() const {
				return (Type == LIST || Type == LAMBDA || Type == MACRO);
			}
			bool Empty() const { return Type != LIST || ListValue.empty(); }
			size_t Size() const {
				if (!IsList()) return 0;
				return ListValue.size();
			}
			bool SizeAtLeast(size_t size) const {
				// if (!IsList()) return false;
				size_t index = 0;
				for (auto it = ListValue.cbegin(); it != ListValue.cend() && index < size; ++it)
					++index;
				return index == size;
			}
			SchemeCell Head() const {
				if (Empty()) return SchemeConstants::Nil;
				// Type should always be LIST here
				return ListValue.front();
			}
			SchemeCell HeadOr(SchemeCell &r) const {
				if (Empty()) return r;
				return ListValue.front();
			}
			SchemeCell Tail() const {
				if (Empty()) return SchemeCell("", LIST);
				return SchemeCell(ListValue.cbegin() + 1, ListValue.cend());
			}
		};

		std::ostream& operator<< (std::ostream& stream, const SchemeCell& cell);
	}
}
