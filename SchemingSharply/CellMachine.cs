using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchemingSharply.Scheme;

namespace SchemingSharply
{
	namespace CellMachine
	{
		public class HasArgumentAttribute : Attribute { }
		public class MissingOpCodeException : Exception
		{
			public MissingOpCodeException(OpCode code)
				: base("Missing OpCode handling for " + code.ToString() + " (" + ((int)code).ToString() + ")")
			{ }
		}

		public enum OpCode : int
		{
			NOP,
			[HasArgument]
			LEA,    // Load effective address into A
			[HasArgument]
			SEA,    // Store A into effective address
			[HasArgument]
			ADJ,    // Adjust stack pointer to allocate local data
			PUSH,   // Push item in register A onto stack
			POP,    // Pop from stack into A
			PEEK,   // A = *Stack
			DUP,    // Duplicate item at the top of stack
			SWITCH, // Switch the top two items on the stack
			[HasArgument]
			DATA,   // Load data using offset in next code position, into A
			[HasArgument]
			JSR,    // Jump to subroutine
			[HasArgument]
			ENTER,  // Save BP to stack, set new BP and enter subroutine
			LEAVE,  // Pop BP from stack
			[HasArgument]
			BZ,     // Branch to next code position's value if A == 0
			JMP,    // Unconditonal branch to next code position's value
			LT,     // A = *Stack++ <  A
			LTK,    // A = *Stack   <  A
			LE,     // A = *Stack++ <= A
			LEK,    // A = *Stack   <= A
			GT,     // A = *Stack++ >  A
			GTK,    // A = *Stack   >  A
			EQ,		// A = *Stack++ == A
			EQK,    // A = *Stack   == A
			NEQK,   // A = *Stack   != A
			NEQ,    // A = *Stack++ != A
			SUB,    // A = *Stack++  - A
			MUL,    // A = *Stack++  * A
			CELLNEW,    // TODO
			CELLTYPE,   // A = A.Type
			CELLCOUNT,  // A = A.ListValue.Count
			CELLINDEX,  // A = *Stack[A]
			CELLSETENV, // *Stack.Environment = A.Environment
			CELLGETENV, // A = *Stack++.Environment
			CELLPUSH,   // *Stack.Push(A)
			CELLSETTYPE,// *Stack.Type = A
			CELLINVOKE, // A = *Stack-1.ProcValue(*Stack++)
			CELLHEAD,   // TODO
			CELLTAIL,   // TODO
			ENVLOOKUP,  // A = A.Environment[*Stack++]
			ENVSET,     // *Stack-1.Environment[*Stack++] = A
			ENVDEFINE,  // *Stack-1.Environment[*Stack++] = A
			ENVNEW,     // A = new Environment(*Stack-1, *Stack++, A.Environment)
			EXIT,   // Exit with value in A
			PRINT,  // Print Cell in register A
			STATE,  // Print Machine state
		}

		public static class EnumExtensions
		{
			public static T GetAttribute<T>(this Enum value)
				where T : Attribute
			{
				var type = value.GetType();
				var memberInfo = type.GetMember(value.ToString());
				var attributes = memberInfo[0].GetCustomAttributes(typeof(T), false);
				return attributes.Length > 0 ? (T)attributes[0] : null;
			}

			public static bool HasArgument(this Enum value)
			{
				var attribute = value.GetAttribute<HasArgumentAttribute>();
				return attribute != null;
			}
		}

		public class Machine
		{
			//protected IList<Cell> Stack;  // 
			protected Cell[] Stack;
			protected int SP;             // Stack pointer
			protected int BP;             // Base pointer
			protected int PC;             // Program counter
			protected Cell A;             // Accumulator
			protected IList<int> Code;    // Code
			protected IList<Cell> Data;   // Data
			protected readonly int Entry; // Initial PC value

			protected bool finished = false;
			public bool Finished { get { return finished; } }

			public const int StackSize = 3000;

			public Machine(IList<int> code, IList<Cell> data,
				int entry,
				IEnumerable<Cell> arguments, int stacksize = StackSize)
			{
				Stack = new Cell[stacksize];
				for (SP = BP = 0; SP < stacksize; ++SP, ++BP)
					Stack[SP] = new Cell(0);
				--SP; --BP;

				PC = Entry = entry;
				Code = code;
				Data = data;
				A = new Cell(0);

				// Write arguments to stack, can be loaded with LEA
				foreach (Cell arg in arguments)
					Stack[--SP] = arg;
			}

			public Machine(CodeResult code, IEnumerable<Cell> arguments, int stacksize = StackSize)
				: this(code.Code, code.Data, code.Entry, arguments, stacksize)
			{
			}

			public void Step ()
			{
				if (finished) return;

				int ins = Code[PC++];

#if DEBUG
				OpCode insOp = (OpCode)ins;
				Console.Write("!ins {0}", typeof(OpCode).GetEnumName(insOp));
				if(insOp.HasArgument())
				{
					Console.Write(" {0}", Code[PC]);
				}
				Console.WriteLine();
#endif

				switch((OpCode)ins)
				{
					case OpCode.NOP:
						break;

					case OpCode.LEA:
						A = Stack[BP + Code[PC++]];
						break;

					case OpCode.SEA:
						Stack[BP + Code[PC++]] = A;
						break;

					case OpCode.ENTER:
						Stack[--SP] = new Cell(BP);
						BP = SP;
						SP = SP - Code[PC++];
						break;

					case OpCode.ADJ:
						SP += Code[PC++];
						break;

					case OpCode.LEAVE:
						SP = BP;
						BP = (int)Stack[SP++];
						PC = (int)Stack[SP++];
						break;

					case OpCode.PUSH:
						Stack[--SP] = A;
						break;

					case OpCode.POP:
						A = Stack[SP++];
						break;

					case OpCode.PEEK:
						A = Stack[SP];
						break;

					case OpCode.DUP:
						Stack[SP - 1] = Stack[SP];
						--SP;
						break;

					case OpCode.SWITCH:
						Cell tmp = Stack[SP + 1];
						Stack[SP + 1] = Stack[SP];
						Stack[SP] = tmp;
						break;

					case OpCode.DATA:
						A = Data[Code[PC++]];
						break;

					/*case OpCode.PROCEX:
						Cell args = Stack[SP];
						A = A.ProcValue(args.ListValue.ToArray());
						break;*/

					case OpCode.LE:
						A = new Cell(((int)Stack[SP++] <= (int)A) ? 1 : 0);
						break;

					case OpCode.EQ:
						A = new Cell(((int)Stack[SP++] == (int)A) ? 1 : 0);
						break;

					case OpCode.EQK:
						A = new Cell(((int)Stack[SP] != (int)A) ? 1 : 0);
						break;

					case OpCode.NEQ:
						A = new Cell(((int)Stack[SP++] != (int)A) ? 1 : 0);
						break;

					case OpCode.NEQK:
						A = new Cell(((int)Stack[SP] == (int)A) ? 1 : 0);
						break;

					case OpCode.JSR:
						Stack[--SP] = new Cell((int)PC + 1);
						PC = Code[PC];
						break;

					case OpCode.BZ:
						PC = (int)A == 0 ? Code[PC] : PC + 1;
						break;

					case OpCode.JMP:
						PC = Code[PC];
						break;

					case OpCode.SUB:
						A = Stack[SP++] - A;
						break;

					case OpCode.MUL:
						A = Stack[SP++] * A;
						break;

					case OpCode.CELLTYPE:
						A = new Cell((int)A.Type);
						break;

					case OpCode.CELLCOUNT:
						A = new Cell(A.ListValue.Count);
						break;

					case OpCode.CELLINDEX:
						A = Stack[SP].ListValue[(int)A];
						break;

					case OpCode.CELLSETTYPE:
						Stack[SP].Type = (CellType)(int)A;
						break;

					case OpCode.CELLSETENV:
						Stack[SP].Environment = A.Environment;
						break;

					case OpCode.ENVLOOKUP:
						A = A.Environment.Lookup(Stack[SP++]);
						break;

					case OpCode.ENVSET:
						// *Stack++.Environment[*Stack++] = A
						Stack[SP - 1].Environment.Set(Stack[SP], A);
						SP--; // remove key from stack
						break;

					case OpCode.ENVDEFINE:
						Stack[SP - 1].Environment.Define(Stack[SP], A);
						SP--; // remove key from stack
						break;

					case OpCode.PRINT:
						Console.Write("{0}", A);
						break;

					case OpCode.STATE:
						PrintState();
						break;

					case OpCode.EXIT:
						finished = true;
						Console.WriteLine("!Exit with code {0}", A);
						break;

					default:
						throw new MissingOpCodeException((OpCode)ins);
				}
			}

			public void PrintState()
			{
				Console.WriteLine("!DEBUG STATE:");
				Console.WriteLine("  SP: {0} BP: {1}", SP, BP);
				Console.WriteLine("  PC: {0}  A: {1}", PC, A);
				for (int i = SP; i < Stack.Length - 1;)
					Console.WriteLine("  Stack[{0}] = {1}", i, Stack[i++]);
			}

			public static void Test1 ()
			{
				CodeBuilder code = new CodeBuilder();
				List<Cell> data = new List<Cell>();
				Dictionary<string, int> labels = new Dictionary<string, int>();
				int cp = 0; // code position

				// func fac(n)
				//   locals: none
				labels.Add("fac", code.Add(OpCode.ENTER, 0));
				//   if (<= n 1) return 1
				//      load n, push
				code.Add(OpCode.LEA, 2);
				code.Add(OpCode.PUSH);
				//      load 1
				code.Add(OpCode.DATA, code.Data(new Cell(1)));
				//      n <= 1
				code.Add(OpCode.LE);
				//      if A=true return 1
				cp = code.Add(OpCode.BZ); code.Add(cp + 5); // if !(n <= 1) skip next instructions
				//        load 1, leave
				code.Add(OpCode.DATA, code.Data(new Cell(1)));
				code.Add(OpCode.LEAVE);

				// return n * fac(n - 1)
				//    n *
				code.Add(OpCode.LEA, 2);
				code.Add(OpCode.PUSH);
				//     fac( n - 1)
				code.Add(OpCode.LEA, 2);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(new Cell(1)));
				code.Add(OpCode.SUB);
				//    fac(n - 1)
				code.Add(OpCode.PUSH);
				code.Add(OpCode.JSR, labels["fac"]);
				code.Add(OpCode.ADJ, 1);
				//    on return result will be in A
				//    n * fac(n - 1)
				code.Add(OpCode.MUL);
#if DEBUG
				code.Add(OpCode.STATE);
#endif
				//    return
				code.Add(OpCode.LEAVE);

				// int main (int n) {
				cp = code.Add(OpCode.ENTER, 0);
				labels["main"] = cp;
				//    print "Factorial of "
				code.Add(OpCode.DATA, code.Data(new Cell("Factorial of ")));
				code.Add(OpCode.PRINT);
				//    print n
				code.Add(OpCode.LEA, 1);
				code.Add(OpCode.PRINT);
				//    print " is "
				code.Add(OpCode.DATA, code.Data(new Cell(" is ")));
				code.Add(OpCode.PRINT);
				//    print fac(n)
				code.Add(OpCode.LEA, 1);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.JSR, labels["fac"]);
				code.Add(OpCode.PRINT);
				code.Add(OpCode.ADJ, 2);
				code.Add(OpCode.DATA, code.Data(new Cell(Environment.NewLine)));
				code.Add(OpCode.PRINT);
				code.Add(OpCode.DATA, code.Data(new Cell(0)));
				code.Add(OpCode.EXIT);

				code.Entry = labels["main"];
				List<Cell> args = new List<Cell>();
				args.Add(new Cell(10));

				Machine mach = new Machine(code.Generate(), args);
				//int i = 0; 
				while(mach.Finished == false) // && i++ < 130)
				{
					mach.Step();
				}
			}

			public static void TestEval ()
			{
				CodeBuilder code = new CodeBuilder();
				List<Cell> data = new List<Cell>();
				Dictionary<string, int> labels = new Dictionary<string, int>();
				// effective addresses of values (avoid magic numbers)
				int env = 2, x = 3, xl0 = -1, i = -2, exps = -3, proc = -4;

				// func eval(x, env)
				//   locals: xl0, exps, proc
				code.SetLabel("eval", code.Add(OpCode.ENTER, 5));
				code.Add(OpCode.LEA, x); // get x
				code.Add(OpCode.CELLTYPE); // get cell type
				code.Add(OpCode.PUSH); // leave on stack
				// if (typeof x == Symbol)
				code.Add(OpCode.DATA, code.Data((int)CellType.STRING));
				code.Add(OpCode.EQK);
				code.Add(OpCode.BZ); code.Delayed("if_x_ne_symbol");
				//   return env[x];
				code.Add(OpCode.LEA, x); // x
				code.Add(OpCode.PUSH); // onto stack
				code.Add(OpCode.LEA, env); // env
				code.Add(OpCode.ENVLOOKUP); // A = cell.env[A]
				code.Add(OpCode.LEAVE);
				// if (typeof x == Number)
				code.SetLabel("if_x_ne_symbol", code.Add(OpCode.DATA));
				code.Add(code.Data((int)CellType.NUMBER));
				code.Add(OpCode.EQK);
				code.Add(OpCode.BZ); code.Delayed("if_x_ne_number");
				//   return x;
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.LEAVE);

				// remove celltype from stack
				code.SetLabel("if_x_ne_number", code.Add(OpCode.ADJ, 1));
				// if (x.listcount == 0)
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.CELLCOUNT); // A = A.ListValue.Count
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(0));
				code.Add(OpCode.EQ);
				code.Add(OpCode.BZ); code.Delayed("if_xcount_ne_0");
				//   return sym_nil;
				code.Add(OpCode.DATA, code.Data(StandardRuntime.Nil));
				code.Add(OpCode.LEAVE);

				code.SetLabel("if_xcount_ne_0", code.Add(OpCode.NOP));
				// xl0 = x.list[0]
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(0));
				code.Add(OpCode.CELLINDEX);
				code.Add(OpCode.SEA, xl0);
				code.Add(OpCode.ADJ, 1); // remove x from stack
				code.Add(OpCode.PUSH); // will be grabbed later
				code.Add(OpCode.CELLTYPE); // stack = x.list[0].type
				code.Add(OpCode.PUSH);
				// if (typeof xl0 == Symbol) {
				code.Add(OpCode.DATA, code.Data((int)CellType.STRING));
				code.Add(OpCode.EQ);
				code.Add(OpCode.BZ); code.Delayed("if_xl0_ne_symbol");
				//   if (xl0 == "quote")
				code.Add(OpCode.DATA, code.Data("quote"));
				code.Add(OpCode.EQK); // keeps xl0 on stack
				code.Add(OpCode.BZ); code.Delayed("if_xl0_ne_quote");
				//     return x.list[1]
				code.Add(OpCode.DATA, code.Data(1));
				code.Add(OpCode.CELLINDEX); // A = Stack[SP][A]
				code.Add(OpCode.LEAVE); // stack will be cleared
				//   if (xl0 == "if")
				code.SetLabel("if_xl0_ne_quote", code.Add(OpCode.DATA));
				code.Add(code.Data("if"));
				code.Add(OpCode.EQK); // keep xl0 on stack
				code.Add(OpCode.BZ); code.Delayed("if_xl0_ne_if");
				//     return eval_if(x, env)
				code.Add(OpCode.LEA, env); code.Add(OpCode.PUSH);
				code.Add(OpCode.LEA, x); code.Add(OpCode.PUSH);
				code.Add(OpCode.JSR); code.Delayed("eval_if");
				code.Add(OpCode.LEAVE);

				code.SetLabel("if_xl0_ne_if", code.Add(OpCode.DATA));
				//   if (xl0 == "set!")
				code.Add(code.Data("set!"));
				code.Add(OpCode.EQK);
				code.Add(OpCode.BZ); code.Delayed("if_xl0_ne_set!");
				//     env.Set(x.list[1], eval(x.list[2], env))
				//     env
				code.Add(OpCode.LEA, env);
				//       eval(x.list[2], env)
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(2));
				code.Add(OpCode.CELLINDEX);
				code.Add(OpCode.ADJ, 1); // remove x from stack
				code.Add(OpCode.PUSH);
				code.Add(OpCode.LEA, env);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.JSR); code.Delayed("eval");
				code.Add(OpCode.PUSH);
				//     x.list[1]
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(1));
				code.Add(OpCode.CELLINDEX);
				code.Add(OpCode.ADJ, 1); // remove x from stack
				//   env.Set() - puts value in A
				code.Add(OpCode.ENVSET);
				code.Add(OpCode.LEAVE);
				code.SetLabel("if_xl0_ne_set!", code.Add(OpCode.DATA));
				//   if (xl0 == "define!")
				code.Add(code.Data("define"));
				code.Add(OpCode.EQK);
				code.Add(OpCode.BZ); code.Delayed("if_xl0_ne_define");
				//     env.Define(x.list[1], eval(x.list[2], env))
				//     env
				code.Add(OpCode.LEA, env);
				//       eval(x.list[2], env)
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(2));
				code.Add(OpCode.CELLINDEX);
				code.Add(OpCode.ADJ, 1); // remove x from stack
				code.Add(OpCode.PUSH);
				code.Add(OpCode.LEA, env);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.JSR); code.Delayed("eval");
				code.Add(OpCode.PUSH);
				//     x.list[1]
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(1));
				code.Add(OpCode.CELLINDEX);
				code.Add(OpCode.ADJ, 1); // remove x from stack
				//   env.Set() - puts value in A
				code.Add(OpCode.ENVDEFINE);
				code.Add(OpCode.LEAVE);

				code.SetLabel("if_xl0_ne_define", code.Add(OpCode.DATA));
				// if (xl0 == "lambda") {
				code.Add(code.Data("lambda"));
				code.Add(OpCode.EQK);
				code.Add(OpCode.BZ); code.Delayed("if_xl0_ne_lambda");
				// x.type = Lambda
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.PUSH); // copy x to stack for modification
				code.Add(OpCode.DATA, code.Data((int)CellType.LAMBDA));
				code.Add(OpCode.CELLSETTYPE);
				// x.env = env
				code.Add(OpCode.LEA, env); // x is still on stack
				code.Add(OpCode.CELLSETENV);
				code.Add(OpCode.POP); // move modified cell into A
				//     return x
				code.Add(OpCode.LEAVE);
				//   }
				code.SetLabel("if_xl0_ne_lambda", code.Add(OpCode.DATA));
				code.Add(code.Data("begin"));
				code.Add(OpCode.EQK);
				code.Add(OpCode.BZ); code.Delayed("if_xl0_ne_begin");
				//   *stack = x[1...]
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.PUSH);   // will be reused in while loop
				//   while(*stack.length > 1)
				code.SetLabel("begin_while", code.Add(OpCode.CELLCOUNT)); // a = *stack.listvalue.length
				code.Add(OpCode.PUSH); // *++stack = stack.length
				code.Add(OpCode.DATA, code.Data(1)); // a = 1
				code.Add(OpCode.GT); // a = *stack > 1
				code.Add(OpCode.BZ); code.Delayed("if_stacklen_not_gt1");
				//     eval(*stack, env)
				code.Add(OpCode.LEA, env);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.JSR); code.Delayed("eval"); // recurse!
				//     *stack = *stack.tail()
				code.Add(OpCode.CELLTAIL);  // a = *stack++[1...] - removes from stack
				code.Add(OpCode.PUSH); // put it back onto the stack for comparison loop
				code.Add(OpCode.JMP); code.Delayed("begin_while");
				//   }
				code.SetLabel("if_stacklen_not_gt1", code.Add(OpCode.NOP));
				//   return eval(*stack, env)  - item to eval still on stack
				code.Add(OpCode.LEA, env);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.JSR); code.Delayed("eval");
				code.Add(OpCode.LEAVE);
				code.SetLabel("if_xl0_ne_begin", code.Add(OpCode.NOP));
				// falls through:
				// }
				// label if_xl0_ne_symbol:
				code.SetLabel("if_xl0_ne_symbol", code.Add(OpCode.NOP));
				// proc = eval(x.list[0], env)
				code.Add(OpCode.LEA, xl0);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.LEA, env);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.JSR); code.Delayed("eval");
				//code.Add(OpCode.ADJ, 2); // remove from stack
				// exps = new Cell(List)
				code.Add(OpCode.DATA, (int)CellType.LIST);
				code.Add(OpCode.CELLNEW);
				code.Add(OpCode.SEA, exps);
				// while(x.listvalue.count > 0) {
				code.Add(OpCode.LEA, x);
				code.SetLabel("eval_exps_while", code.Add(OpCode.CELLCOUNT));
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(0));
				code.Add(OpCode.GT);
				code.Add(OpCode.BZ); code.Delayed("eval_exps_done");
				//   exps.push(eval(head(x), env))
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.CELLHEAD);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.LEA, env);
				code.Add(OpCode.JSR); code.Delayed("eval");
				code.Add(OpCode.ADJ, 2); // remove head(x) and env??
				code.Add(OpCode.PUSH);
				code.Add(OpCode.LEA, exps);
				//code.Add(OpCode.SWITCH);  // swap exps and result
				code.Add(OpCode.CELLPUSH);  // *stack.list.push(A)
				//   x = tail(x)
				code.Add(OpCode.LEA, x);
				code.Add(OpCode.CELLTAIL);  // A = A.listvalue.Tail()
				code.Add(OpCode.SEA, x);
				code.Add(OpCode.JMP); code.Delayed("eval_exps_while");
				// }
				// if (proc.type == Lambda) {
				code.SetLabel("eval_exps_done", code.Add(OpCode.LEA, proc));

				// func eval_if(parts, targetenv)
				//    locals: test, conseq, alt
				int parts = 3, targetenv = 2, test = -1, conseq = -2, alt = -3;
				code.SetLabel("eval_if", code.Add(OpCode.ENTER, 3));
				//  test = eval(parts.list[1], targetenv)
				code.Add(OpCode.LEA, parts);
				code.Add(OpCode.PUSH);   // stack++ = parts
				code.Add(OpCode.DATA, code.Data(1));
				code.Add(OpCode.CELLINDEX);
				code.Add(OpCode.ADJ, 1); // remove parts from stack
				code.Add(OpCode.PUSH);   // stack++ = parts.list[1]
				code.Add(OpCode.LEA, targetenv);
				code.Add(OpCode.PUSH);   // stack++ = targetenv
				code.Add(OpCode.JSR); code.Delayed("eval");
				code.Add(OpCode.SEA, test); // test = A
				//  conseq = parts.list[2]
				code.Add(OpCode.LEA, parts);
				code.Add(OpCode.PUSH);   // stack++ = parts
				code.Add(OpCode.DATA, code.Data(2));
				code.Add(OpCode.CELLINDEX);  // leaving *stack == parts
				code.Add(OpCode.SEA, conseq);
				//  if parts.listcount < 4
				code.Add(OpCode.PEEK);      // copy *stack into A
				code.Add(OpCode.CELLCOUNT); // get cell count of A
				code.Add(OpCode.PUSH);      // push count on to stack
				code.Add(OpCode.DATA, code.Data(4));
				code.Add(OpCode.LE);        // stack now has parts after comparison
				code.Add(OpCode.BZ); code.Delayed("eval_if_has_alt");
				//    alt = nil
				code.Add(OpCode.DATA, code.Data(StandardRuntime.Nil));
				code.Add(OpCode.JMP); code.Delayed("eval_if_store_alt");
				//  else
				code.SetLabel("eval_if_has_alt", code.Add(OpCode.NOP));
				//    alt = parts.list[3]
				code.Add(OpCode.DATA, code.Data(3));
				code.Add(OpCode.CELLINDEX);
				// (save into alt)
				code.SetLabel("eval_if_store_alt", code.Add(OpCode.SEA, alt));
				//  return test == false ? alt : conseq
				code.Add(OpCode.LEA, test);
				code.Add(OpCode.PUSH);
				code.Add(OpCode.DATA, code.Data(StandardRuntime.False));
				code.Add(OpCode.EQ);
				code.Add(OpCode.BZ); code.Delayed("eval_conseq");
				code.Add(OpCode.LEA, alt);
				code.Add(OpCode.LEAVE);
				code.SetLabel("eval_conseq", code.Add(OpCode.LEA, conseq));
				code.Add(OpCode.LEAVE);
			}

			public static void TestCompile() {
				string code = System.IO.File.ReadAllText("../../Core/Eval.asm");
				CellMachineAssembler assembler = new CellMachineAssembler(code);
				CodeResult result = assembler.Generate();

			}

		}

		public struct CodeResult
		{
			public IList<int> Code { get; }
			public IList<Cell> Data { get; }
			public int Entry { get; }

			public CodeResult(IList<int> code, IList<Cell> data, int entry)
			{
				Code = code;
				Data = data;
				Entry = entry;
			}
		}

		public interface ICodeBuilder
		{
			CodeResult Generate();
		}

		public class CodeBuilder : ICodeBuilder
		{
			public interface ICodeEntry
			{
				int GetValue(CodeBuilder builder);
			}

			public struct FixedCodeEntry : ICodeEntry
			{
				public readonly int Value;

				public FixedCodeEntry(int value)
				{
					Value = value;
				}

				public int GetValue(CodeBuilder builder)
				{
					return Value;
				}
			}

			public struct DelayedCodeEntry : ICodeEntry {
				public string Label { get; }
				public DelayedCodeEntry(string label) {
					Label = label;
				}
				public int GetValue(CodeBuilder builder) {
					return builder.GetLabel(Label);
				}
			}

			public struct CellCodeEntry : ICodeEntry {
				public Cell Value { get; }
				public CellCodeEntry(Cell value) {
					Value = value;
				}
				public int GetValue(CodeBuilder builder) {
					return builder.Data(Value);
				}
			}

			protected List<ICodeEntry> code = new List<ICodeEntry>();
			protected List<Cell> data = new List<Cell>();
			protected Dictionary<string, ICodeEntry> labels = new Dictionary<string, ICodeEntry>();
			public int Entry = 0;

			public CodeResult Generate()
			{
				List<int> generated = code.ConvertAll((e) => e.GetValue(this));
				return new CodeResult(generated, data, Entry);
			}

			public int Add (params int[] value)
			{
				int result = code.Count;
				code.AddRange(value.Select(i => (ICodeEntry)new FixedCodeEntry(i)));
				return result;
			}

			public int Add (OpCode value, params int[] values)
			{
				int result = Add((int)value);
				Add(values);
				return result;
			}

			public int Delayed (params string[] values)
			{
				int result = code.Count;
				code.AddRange(values.Select(label => (ICodeEntry)new DelayedCodeEntry(label)));
				return result;
			}

			public int Data (Cell value)
			{
				int result = data.Count;
				data.Add(value);
				return result;
			}

			public int Data (int value)
			{
				return Data(new Cell(value));
			}

			public int Data (string value)
			{
				return Data(new Cell(value));
			}

			public void SetLabel (string label, int value)
			{
				labels[label] = new FixedCodeEntry(value);
			}

			public void SetLabel (string label, Cell value) {
				labels[label] = new CellCodeEntry(value);
			}

			public int GetLabel (string label)
			{
				return labels[label].GetValue(this);
			}

			internal bool HasLabel(string value) => labels.ContainsKey(value);
		}

		public class CellMachineAssembler : ICodeBuilder
		{
			public string Code { get; }

			protected CodeBuilder builder;

			public CellMachineAssembler(string code)
			{
				Code = code;
			}

			protected enum AssembleStatus {
				NONE,
				DEFINE_NAME,
				DEFINE_VALUE,
			}

			protected struct AssembleState {
				public AssembleStatus Status;
				public string DefineName;
				public string DefineValue;

				public AssembleState(AssembleStatus status = AssembleStatus.NONE,
					string defineName = "", string defineValue = "") {
					Status = status;
					DefineName = defineName;
					DefineValue = defineValue;
				}

				public static AssembleState None {
					get { return new AssembleState(); }
				}
			}

			public void Assemble ()
			{
				string[] lines = readLines();
				int position = 0;
				builder = new CodeBuilder();
				string[] words = readWords(lines);
				AssembleState state = AssembleState.None;

				addStandardLabels();

				foreach (string word in words) {
					if (word.EndsWith(":")) { // label
						builder.SetLabel(word.Substring(0, word.Length - 1), position);
						continue;
					} else if (word == "!define") {
						state.Status = AssembleStatus.DEFINE_NAME;
						continue;
					}

					if (state.Status == AssembleStatus.DEFINE_NAME) {
						state.DefineName = word;
						state.Status = AssembleStatus.DEFINE_VALUE;
					} else if (state.Status == AssembleStatus.DEFINE_VALUE) {
						state.DefineValue = word;
						builder.SetLabel(state.DefineName, assembleValue(state.DefineValue));
						state.Status = AssembleStatus.NONE;
					} else {
						int value;
						if (word.StartsWith("\"") && word.EndsWith("\"")) {
							position = builder.Add(builder.Data(word.Substring(1, word.Length - 2)));
						} else if (int.TryParse(word, out value)) {
							position = builder.Add(builder.Data(value));
						} else {
							position = builder.Delayed(word);
						}
					}
				}

				System.Diagnostics.Debugger.Break();
			}

			protected void addStandardLabels() {
				builder.SetLabel("StandardRuntime.Nil", StandardRuntime.Nil);
				builder.SetLabel("StandardRuntime.False", StandardRuntime.False);
				builder.SetLabel("StandardRuntime.True", StandardRuntime.True);
#if FALSE
				builder.SetLabel("CellType.STRING", (int)CellType.STRING);
				builder.SetLabel("CellType.NUMBER", (int)CellType.NUMBER);
				builder.SetLabel("CellType.LIST", (int)CellType.LIST);
				builder.SetLabel("CellType.LAMBDA", (int)CellType.LAMBDA);
				builder.SetLabel("CellType.PROC", (int)CellType.PROC);
				builder.SetLabel("CellType.ENVPTR", (int)CellType.ENVPTR);
#endif
				Type celltype = typeof(CellType);
				foreach (var i in celltype.GetEnumValues())
					builder.SetLabel("CellType." + celltype.GetEnumName(i), (int)i);
				Type opcode = typeof(OpCode);
				foreach(var i in opcode.GetEnumValues())
					builder.SetLabel(opcode.GetEnumName(i), (int)i);
			}

			public class LabelNotFoundException : Exception {
				public LabelNotFoundException(string label)
					: base(string.Format("Label not found: {0}", label)) { }
			}
			protected int assembleValue(string value) {
				int result;
				if(int.TryParse(value, out result)) {
					return result;
				}
				if (builder.HasLabel(value))
					return builder.GetLabel(value);
				throw new LabelNotFoundException(value);
			}

			protected string[] readWords (string[] lines) {
				List<string> words = new List<string>();
				foreach (string line in lines)
					words.AddRange(line.Split(' ').Where(word => word.Length > 0));
				return words.ToArray();
			}

			enum ReadLineState
			{
				NONE,
				COMMENT,
				ESCAPE,
			}

			/// <summary>
			/// Read code and split into lines, removing comments.
			/// </summary>
			/// <returns>string[] the lines, sans comments</returns>
			protected string[] readLines()
			{
				int roughLineCount = Code.Count(c => c == '\n');
				List<string> result = new List<string>(roughLineCount);
				int line = 0;
				ReadLineState flags = ReadLineState.NONE;

				result.Add("");
				
				foreach(char ch in Code) {
					if (ch == '\n') {
						flags = ReadLineState.NONE;
						if (result[line].Trim().Length > 0) {
							line++;
							result.Add("");
						}
						continue;
					}

					if (flags == ReadLineState.ESCAPE) {
						result[line] += ch;
						flags = ReadLineState.NONE;
						continue;
					} else if (flags == ReadLineState.COMMENT)
						continue;

					switch (ch) {
						case '\\':
							flags = ReadLineState.ESCAPE;
							break;
						case ';':
							flags = ReadLineState.COMMENT;
							break;
						case '\r':
							break;
						case '\t':
							result[line] += ' ';
							break;
						default:
							result[line] += ch;
							break;
					}
				}

				return result.ToArray();
			}

			public CodeResult Generate()
			{
				builder = new CodeBuilder();
				Assemble();
				return builder.Generate();
			}
		}
	}
}
