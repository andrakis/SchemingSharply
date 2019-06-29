#pragma once

#include "Scheme.h"

namespace SchemingPlusPlus {
	namespace Core {
		class SchemeEvaluator {
		protected:
			SchemeEvaluator() { }
			virtual ~SchemeEvaluator() { }
		public:
			virtual SchemeCell Eval(const SchemeCell &x, const SchemeCell &env) THROW(critical_error) = 0;
		};
	}
}
