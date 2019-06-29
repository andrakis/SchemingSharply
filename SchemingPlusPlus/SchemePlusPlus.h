/*
 * Master include file
 *
 * Include this file for access to all classes.
 */

#pragma once

#include "Scheme.h"
#include "SchemeCell.h"
#include "SchemeEnvironment.h"
#include "SchemeParser.h"
#include "SchemeAssert.h"
#include "SchemeRuntime.h"
#include "SchemeEvalSimple.h"


namespace SchemingPlusPlus {
	namespace Tests {
		bool RunTests(); // SchemingTests.cpp
	}
}
