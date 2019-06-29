#pragma once

#include "Scheme.h"

namespace SchemingPlusPlus {
	namespace Core {
		void scheme_wassert(
			const char *_Message,
			const char *_File,
			unsigned    _Line
		) SCHEME_THROW;

#define runtime_assert(expression) (void)(                                                       \
		(!!(expression)) ||                                                              \
			(scheme_wassert(#expression, __FILE__, (unsigned)(__LINE__)), 0) \
		)
	}
}
