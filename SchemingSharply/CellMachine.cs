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
			LEA,    // Load effective address int A
			[HasArgument]
			ADJ,    // Adjust stack pointer to allocate local data
			PUSH,   // Push item in register A onto stack
			POP,    // Pop from stack into A
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
			LE,     // A = *Stack <= A
			SUB,    // A = *Stack - A
			MUL,    // A = *Stack * A
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
			protected IList<Cell> Stack;  // 
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

					case OpCode.DUP:
						Stack[SP] = Stack[SP - 1];
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

					case OpCode.JSR:
						Stack[--SP] = new Cell((int)PC + 1);
						PC = Code[PC];
						break;

					case OpCode.BZ:
						PC = (int)A == 0 ? Code[PC] : PC + 1;
						break;

					case OpCode.SUB:
						A = Stack[SP++] - A;
						break;

					case OpCode.MUL:
						A = Stack[SP++] * A;
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
				for (int i = SP; i < Stack.Count - 1;)
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
				int i = 0; 
				while(mach.Finished == false) // && i++ < 130)
				{
					mach.Step();
				}
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

		public class CodeBuilder
		{
			protected List<int> code = new List<int>();
			protected List<Cell> data = new List<Cell>();
			public int Entry = 0;

			public CodeResult Generate()
			{
				return new CodeResult(code, data, Entry);
			}

			public int Add (params int[] value)
			{
				int result = code.Count;
				code.AddRange(value);
				return result;
			}

			public int Add (OpCode value, params int[] values)
			{
				int result = Add((int)value);
				code.AddRange(values);
				return result;
			}

			public int Data (Cell value)
			{
				int result = data.Count;
				data.Add(value);
				return result;
			}
		}
	}
}
