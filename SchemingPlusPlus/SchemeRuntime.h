#pragma once

#include "Scheme.h"

namespace SchemingPlusPlus {
	namespace Core {
		struct SchemeRuntime {
			static void AddGlobals(EnvironmentType env);
			// Comparison operators
			static SchemeCell proc_greater(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_less(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_less_equal(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_equal(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_not_equal(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_not(const VectorType &args) SCHEME_THROW;
			// Convenience functions
			static SchemeCell _not(const SchemeCell &arg) SCHEME_THROW;
			// Math / manipulation functions
			static SchemeCell proc_add(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_sub(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_mul(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_div(const VectorType &args) SCHEME_THROW;
			// List functions
			static SchemeCell proc_length(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_nullp(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_head(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_tail(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_append(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_cons(const VectorType &args) SCHEME_THROW;
			static SchemeCell proc_list(const VectorType &args);

			static bool IsBasicType(CellType type);
			static bool CanCoerce(CellType from, CellType to);

		};
	}
}
