#pragma once

#include "SchemeCell.h"
#include "SchemeEnvironment.h"
#include "SchemeEval.h"
#include "SchemeAssert.h"

namespace SchemingPlusPlus {
	namespace Core {
		class SchemeSimpleEval : public SchemeEvaluator {
		public:
			SchemeSimpleEval() : SchemeEvaluator() { }
			SchemeCell Eval(const SchemeCell &x, const SchemeCell &env) THROW(critical_error) override;
		};
	}
}
