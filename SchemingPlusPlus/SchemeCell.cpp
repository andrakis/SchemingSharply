#include <numeric>
#include <ostream>
#include <string>

#include "SchemeAssert.h"
#include "SchemeCell.h"
#include "TextUtils.h"

namespace SchemingPlusPlus {
	namespace Core {
		std::string SchemeCell::ToString(bool expr) const SCHEME_THROW {
			switch (Type) {
				case SYMBOL: return Value;
				case STRING: return expr ? enquote(Value) : Value;
				case INTEGER: // Fall through
				case FLOAT: return Value;
				case LIST: {
					std::string result = "(";
					auto conv = [expr] (const SchemeCell &cell) { return cell.ToString(expr); };
					std::string joined = TextUtils::Join(ListValue, " ", conv);
					result += joined;
					result += ")";
					return result;
				}
				case LAMBDA:  // Fall through
				case MACRO: {
					if (!expr) {
						runtime_assert(Type == LAMBDA || Type == MACRO);
						if (Type == LAMBDA) return "<Lambda>";
						if (Type == MACRO) return "<Macro>";
					}
					std::string result = "(";
					if (Type == LAMBDA) result += "lambda ";
					else if (Type == MACRO) result += "macro ";
					result += Head().ToString(expr);
					result += Tail().Head().ToString(expr);
					result += ")";
					return result;
				}
				case PROC:
					return !expr ? "<Proc>" : ("(proc-addr! " + std::to_string((unsigned long)ProcValue) + ")");
				case PROCENV:
					return !expr ? "<ProcEnv>" : ("(procenv-addr! " + std::to_string((unsigned long)ProcEnvValue) + ")");
				case ENVPTR:
					return !expr ? "<EnvPtr>" : ("(envptr-addr! " + std::to_string((unsigned long)&Environment) + ")");
				default:
					throw critical_error(CRIT_TYPE_NOT_IMPL, CellTypeToString(Type));
			}
		}

		std::ostream& operator<< (std::ostream& stream, const SchemeCell& cell) {
			stream << cell.ToString();
			return stream;
		}
	}
}
