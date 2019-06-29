#include <string>

#include "SchemePlusPlus.h"
#include "SchemeAssert.h"

namespace SchemingPlusPlus {
	namespace Core {
		void scheme_wassert(
			const char *_Message,
			const char *_File,
			unsigned    _Line
		) SCHEME_THROW {
			std::string message = "runtime_assert failed: ";
			message += _File;
			message += ":";
			message += std::to_string(_Line);
			message += " - ";
			message += _Message;
			throw critical_error(CRIT_RUNTIME_ASSERT, SchemeCell(message));
		}
	}
}
