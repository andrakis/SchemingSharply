#include "SchemeEvalSimple.h"

namespace SchemingPlusPlus {
	namespace Core {
		SchemeCell SchemeSimpleEval::Eval(const SchemeCell &item, const SchemeCell &env_item) THROW(critical_error) {
			runtime_assert(env_item.Environment != nullptr);
			SchemeCell x = item;
			SchemeCell env = env_item;
		recurse:
			switch(x.Type) {
					case SYMBOL:
						runtime_assert(env.Environment != nullptr);
						return env.Environment->Find(x.Value)[x.Value];
					case STRING: // Fall through
					case INTEGER: // Fall through
					case FLOAT:
						return x;
			}
			if (x.Empty()) return SchemeConstants::Nil;

			const SchemeCell &sym = x.Head();
			if (sym.Type == SYMBOL) {
				if (sym.Value == "quote") { // (quote exp)
					return x.Tail().Head();
				}
				if (sym.Value == "if") { // (if test conseq [alt])
					const SchemeCell &test = x[1];
					const SchemeCell &conseq = x[2];
					SchemeCell alt = SchemeConstants::Nil;
					if (x.SizeAtLeast(4)) alt = x[3];
					SchemeCell testval = Eval(test, env);
					x = (testval == SchemeConstants::False) ? alt : conseq;
					goto recurse;
				}
				if (sym.Value == "set!") { // (set! var exp) - must exist
					return env.Environment->Find(x[1].Value)[x[1].Value] = Eval(x[2], env);
				}
				if (sym.Value == "define") { // (define var exp) - creates new or updates existing
					return (*env.Environment)[x.ListValue[1].Value] = Eval(x.ListValue[2], env);
				}
				if (sym.Value == "lambda") { // (lambda (var*) exp)
					SchemeCell copy(x);
					copy.Type = LAMBDA;
					copy.Environment = env.Environment;
					return copy;
				}
				if (sym.Value == "macro") { // (macro (var*) exp)
					SchemeCell copy(x);
					copy.Type = MACRO;
					copy.Environment = env.Environment;
					return copy;
				}
				if (sym.Value == "begin") { // (begin exp*)
					runtime_assert(x.SizeAtLeast(1));
					auto it = x.ListValue.cbegin() + 1;
					for (; it != x.ListValue.cend() - 1; ++it)
						Eval(*it, env);
					x = *it;
					goto recurse;
				}
			}
			// (proc exp*)
			const SchemeCell proc = Eval(x[0], env);
			VectorType exps = VectorType();
			if (proc.Type == MACRO)
				exps = x.Tail().ListValue;
			else {
				// (map (tail x) (lambda (y) (eval y env)))
				for (auto it = x.ListValue.cbegin() + 1; it != x.ListValue.cend(); ++it)
					exps.push_back(Eval(*it, env));
			}
			switch (proc.Type) {
				case LAMBDA: {
					SchemeEnvironment *env_ptr = new SchemeEnvironment(proc[1].ListValue, exps, proc.Environment);
					EnvironmentType env_shared(env_ptr); // shared_ptr
					env.Environment = env_shared; // swap environments
					x = proc[2]; // set x to body
					goto recurse;
				}
				case MACRO: {
					SchemeEnvironment *env_ptr = new SchemeEnvironment(proc[1].ListValue, exps, proc.Environment);
					EnvironmentType env_shared(env_ptr); // shared_ptr
					SchemeCell env2(env_shared); // short life
					x = Eval(proc[2], env2);
					// env2 should deallocate here
					goto recurse;
				}
				case PROC: {
					runtime_assert(proc.ProcValue != nullptr);
					return proc.ProcValue(exps);
				}
				case PROCENV: {
					runtime_assert(proc.ProcEnvValue != nullptr);
					return proc.ProcEnvValue(exps, env.Environment);
				}
				default:
					throw critical_error(CRIT_INVALID_PROC, proc);
			}
		}
	}
}
