using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchemingSharply.Scheme;

namespace SchemingSharply
{
	namespace StackMachine
	{
		public class OpWithArgumentAttribute : Attribute
		{
			public readonly int Arguments;
			public OpWithArgumentAttribute(int arguments = 1)
			{
				Arguments = arguments;
			}
		}

		public enum OpCode : int
		{
			[OpWithArgument]
			LEA_DATA,      // Load from data
			[OpWithArgument]
			LEA_CODE,      // Load from code
			PUSH,           // Push Accumulator onto stack
			POP,            // Pop from the stack into Accumulator
			DUP,            // Duplicate and push item on top of stack
			[OpWithArgument]
			ENTER,          // Enter subroutine with X stack allocated
			LEAVE,          // Leave subroutine
			[OpWithArgument]
			ADJUST,         // Adjust stack
			[OpWithArgument]
			JSR,            // Jump into subroutine
			PRINT,          // Print item in accumulator
			[OpWithArgument]
			DEBUG1,         // Print and skip the next code item
			[OpWithArgument(2)]
			DEBUG2,         // Print and skip the next code two items
			[OpWithArgument(3)]
			DEBUG3,         // Print and skip the next code three items
			DEBUG_STATE,    // Print state machine state
			JMP,            // Unconditional jump
			JZ,             // Jump if A == 0
			JNZ,            // Jump if A != 0
		}

		public static class EnumExtensions
		{
			public static TAttribute GetAttribute<TAttribute>(this Enum value)
				where TAttribute : Attribute
			{
				var type = value.GetType();
				var name = Enum.GetName(type, value);
				return type.GetField(name) // I prefer to get attributes this way
					.GetCustomAttributes(false)
					.OfType<TAttribute>()
					.SingleOrDefault();
			}

			public static bool HasAttribute<TAttribute>(this Enum value)
				where TAttribute : Attribute
			{
				var type = value.GetType();
				var name = Enum.GetName(type, value);
				return type.GetField(name)
					.GetCustomAttributes(false)
					.OfType<TAttribute>()
					.Count() > 0;
			}
		}

		public class StackMachine
		{
			protected int PC;  // Instruction pointer
			protected int SP;  // Stack pointer
			protected int BP;  // Base pointer
			protected int A;  // Accumulator

			protected int[] Code;
			protected int[] Stack;
			protected int[] Data;

			protected readonly int StackSize = 1000;

			public StackMachine(IEnumerable<int> code)
			{
				PC = SP = BP = 0;
				A = 0;
				Stack = new int[StackSize];
				SP = BP = Stack.Length;
				Code = code.ToArray();
			}

			public void Cycle()
			{
				OpCode ins = (OpCode)Code[PC++];
				List<int> args = new List<int>();
				if (false && ins.HasAttribute<OpWithArgumentAttribute>())
				{
					OpWithArgumentAttribute a = ins.GetAttribute<OpWithArgumentAttribute>();
					for (int i = 0; i < a.Arguments; ++i)
						args.Add(Code[PC++]);
				}

				switch (ins)
				{
					case OpCode.LEA_CODE:
						A = Code[BP + Code[PC++]];
						break;

					case OpCode.LEA_DATA:
						A = Data[BP + Code[PC++]];
						break;

					case OpCode.JMP:
						PC = Code[PC];
						break;

					case OpCode.JZ:
						PC = (A == 0) ? Code[PC] : PC + 1;
						break;

					case OpCode.JNZ:
						PC = (A != 0) ? Code[PC] : PC + 1;
						break;

					case OpCode.PUSH:
						Stack[--SP] = A;
						break;

					case OpCode.POP:
						A = Stack[++SP];
						break;

					case OpCode.ENTER:
						Stack[--SP] = BP;
						BP = SP;
						SP -= Code[PC++];
						break;

					case OpCode.ADJUST:
						SP -= Code[PC++];
						break;

					case OpCode.LEAVE:
						SP = BP;
						BP = Stack[SP++];
						PC = Stack[SP++];
						break;

					case OpCode.DEBUG3:
						Console.Write("D3 ");
						for (int i = 0; i < 3; ++i)
							Console.Write("{0}", args[i]);
						Console.WriteLine();
						break;

					case OpCode.DEBUG_STATE:
						Console.WriteLine("Machine State:");
						Console.WriteLine(" A: {0}  IP: 0x{1:X}  SP: 0x{2:X}  BP: 0x{3:X}",
							A, PC, SP, BP);
						break;
				}
			}

			public bool Active {
				get { return PC < Code.Length; }
			}
		}
	}
}

