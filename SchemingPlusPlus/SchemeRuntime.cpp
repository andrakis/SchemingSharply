#include <iostream>
#include <string>

#include "SchemeAssert.h"
#include "SchemeRuntime.h"
#include "SchemeCell.h"
#include "SchemeEnvironment.h"
#include "TextUtils.h"

namespace SchemingPlusPlus {
	namespace Core {
		// Big complex macros are bad.
		#define MATH_COMPARE(op, check) do { \
			runtime_assert(args.empty() == false); \
			runtime_assert(args.size() > 1); \
			SchemeCell first = args[0]; \
			for (auto it = args.cbegin() + 1; it != args.cend(); ++it) { \
				switch (it->Type) { \
					case INTEGER: \
						if (first.ToInteger() check it->ToInteger()) \
							return SchemeConstants::False; \
						break; \
					case FLOAT: \
						if (first.ToFloat() check it->ToFloat()) \
							return SchemeConstants::False; \
						break; \
					default: \
						throw critical_error(CRIT_OP_INVALID, first.ToString() + " " + #op + " " + it->ToString(true)); \
				} \
			} \
			return SchemeConstants::True; \
        } while(0)

		bool SchemeRuntime::IsBasicType(CellType type) {
			switch (type) {
				case INTEGER:
				case FLOAT:
				case SYMBOL:
				case STRING:
					return true;
				default: return false;
			}
		}
		bool SchemeRuntime::CanCoerce(CellType from, CellType to) {
			// Same: no coerce required
			if (from == to) return true;
			// Basic types: can coerce between
			if (IsBasicType(from) && IsBasicType(to)) return true;
			// Complex types: cannot coerce between
			return false;
		}

		//SchemeCell SchemeRuntime::proc_greater(const VectorType &args) SCHEME_THROW { MATH_COMPARE(>, <=); }
		SchemeCell SchemeRuntime::proc_greater(const VectorType &args) SCHEME_THROW { 
			runtime_assert(args.empty() == false); 
			runtime_assert(args.size() > 1); 
			SchemeCell first = args[0]; 
			for (auto it = args.cbegin() + 1; it != args.cend(); ++it) { 
				switch (it->Type) { 
					case INTEGER: 
						if (first.ToInteger() <= it->ToInteger()) 
							return SchemeConstants::False; 
						break;
					case FLOAT: 
						if (first.ToFloat() <= it->ToFloat()) 
							return SchemeConstants::False; 
						break;
					default: 
						throw critical_error(CRIT_OP_INVALID, first.ToString() + " > " + it->ToString(true)); 
				} 
			} 
			return SchemeConstants::True; 
		}
		SchemeCell SchemeRuntime::proc_less(const VectorType &args) SCHEME_THROW { MATH_COMPARE(<, >=); }
		SchemeCell SchemeRuntime::proc_less_equal(const VectorType &args) SCHEME_THROW { MATH_COMPARE(<=, >); }
		SchemeCell SchemeRuntime::proc_equal(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.empty() == false); 
			runtime_assert(args.size() > 1); 
			SchemeCell first = args[0]; 
			for (auto it = args.cbegin() + 1; it != args.cend(); ++it) { 
				if (first != *it) return SchemeConstants::False;
			} 
			return SchemeConstants::True; 
		}
		inline SchemeCell _not(const SchemeCell &arg) {
			if (arg == SchemeConstants::False)
				return SchemeConstants::True;
			return SchemeConstants::False;
		}
		inline SchemeCell _truthy(bool truthy) {
			return truthy ? SchemeConstants::True : SchemeConstants::False;
		}
		SchemeCell SchemeRuntime::proc_not(const VectorType &args) SCHEME_THROW {
			return _not(args[0]);
		}
		SchemeCell SchemeRuntime::_not(const SchemeCell &arg) SCHEME_THROW {
			if (arg == SchemeConstants::False)
				return SchemeConstants::True;
			return SchemeConstants::False;
		}
		SchemeCell SchemeRuntime::proc_not_equal(const VectorType &args) SCHEME_THROW {
			return _not(SchemeRuntime::proc_equal(args));
		}

		SchemeCell SchemeRuntime::proc_add(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 0);
			SchemeCell value = args[0];
			for (auto it = args.cbegin() + 1; it != args.cend(); ++it) {
				const SchemeCell &cell = *it;
				value += cell;
			}
			return value;
		}

		SchemeCell SchemeRuntime::proc_sub(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 0);
			SchemeCell value = args[0];
			for (auto it = args.cbegin() + 1; it != args.cend(); ++it) {
				const SchemeCell &cell = *it;
				value -= cell;
			}
			return value;
		}

		SchemeCell SchemeRuntime::proc_mul(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 0);
			SchemeCell value = args[0];
			for (auto it = args.cbegin() + 1; it != args.cend(); ++it) {
				const SchemeCell &cell = *it;
				value *= cell;
			}
			return value;
		}

		SchemeCell SchemeRuntime::proc_div(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 0);
			SchemeCell value = args[0];
			for (auto it = args.cbegin() + 1; it != args.cend(); ++it) {
				const SchemeCell &cell = *it;
				value /= cell;
			}
			return value;
		}

		// List functions
		SchemeCell SchemeRuntime::proc_length(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 0);
			return SchemeCell((IntegerType)args[0].ListValue.size());
		}
		SchemeCell SchemeRuntime::proc_nullp(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 0);
			return _truthy(args[0].ListValue.empty());
		}
		SchemeCell SchemeRuntime::proc_head(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 0);
			return args[0].Head();
		}
		SchemeCell SchemeRuntime::proc_tail(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 0);
			return args[0].Tail();
		}
		SchemeCell SchemeRuntime::proc_append(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 1);
			SchemeCell result(args[0].ListValue);
			const SchemeCell &arg1 = args[1];
			for (auto it = arg1.ListValue.cbegin(); it != arg1.ListValue.cend(); ++it) {
				result.ListValue.push_back(*it);
			}
			return result;
		}
		SchemeCell SchemeRuntime::proc_cons(const VectorType &args) SCHEME_THROW {
			runtime_assert(args.size() > 1);
			SchemeCell result("", LIST);
			result.ListValue.push_back(args[0]);
			const SchemeCell &arg1 = args[1];
			for (auto it = arg1.ListValue.cbegin(); it != arg1.ListValue.cend(); ++it) {
				result.ListValue.push_back(*it);
			}
			return result;
		}
		SchemeCell SchemeRuntime::proc_list(const VectorType &args) {
			return SchemeCell(args);
		}

		// SchemeCell mapper function
		auto map_cell_to_string(bool expr) {
			return [expr] (const SchemeCell &cell) { return cell.ToString(expr); };
		}
		// IO functions
		SchemeCell SchemeRuntime::proc_print(const VectorType &args) {
			std::cout << TextUtils::Join(args, " ", map_cell_to_string(false)) << std::endl;
			return SchemeConstants::Nil;
		}
		SchemeCell SchemeRuntime::proc_expr(const VectorType &args) {
			return SchemeCell(TextUtils::Join(args, " ", map_cell_to_string(true)), STRING);
		}

		void SchemeRuntime::AddGlobals(EnvironmentType _env) {
			SchemeEnvironment &env = *_env;
			env["nil"] = SchemeConstants::Nil;
			env["#f"] = SchemeConstants::False;
			env["#t"] = SchemeConstants::True;
			env["<"] = proc_less; env["<="] = proc_less_equal;
			env[">"] = proc_greater;
			env["="] = proc_equal; env["=="] = proc_equal;
			env["!"] = proc_not; env["!="] = proc_not_equal;
			env["+"] = proc_add; env["-"] = proc_sub;
			env["*"] = proc_mul; env["/"] = proc_div;
			// List functions
			env["length"] = proc_length; env["null?"] = proc_nullp;
			env["head"] = proc_head; env["tail"] = proc_tail;
			env["append"] = proc_append; env["cons"] = proc_cons;
			env["list"] = proc_list;
			// IO functions
			env["print"] = proc_print; env["expr"] = proc_expr;
		}
	}
}
