using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntVM
{
	public enum OpCode
	{
		LEA,
		IMM,
		JMP,
		JSR,
		BZ,
		BNZ,
		ENT,
		ADJ,
		LEV,
		PSH,
		LI,
		SUB,
		MUL,
		LE,
		PRINT,    // Print A (accumulator)
		EXIT,     // Exit with exit code
		DEBUG,    // Print machine state
	}

	public interface ICodeGenerator
	{
		IEnumerable<int> Generate();
	}

	public class IntVM
	{

	}

	public abstract class SampleProvider
	{
		private List<int> Code = new List<int>();
		private Dictionary<string, int> Labels = new Dictionary<string, int>();

		protected void Add (int n)
		{
			Code.Add(n);
		}

		protected void Add (OpCode o)
		{
			Code.Add((int)o);
		}

		protected int Current {
			get { return Code.Count; }
		}

		protected int Label (string name)
		{
			int pos = Code.Count;
			Labels[name] = pos;
			return pos;
		}

		protected int LabelAddr (string name)
		{
			return Labels[name];
		}

		protected int[] GetCode()
		{
			return Code.ToArray();
		}

		protected void ResetCode ()
		{
			Code.Clear();
			Labels.Clear();
		}
	}

	public class FactorialSample : SampleProvider
	{
		readonly int N;

		public FactorialSample(int n)
		{
			N = n;
		}

		public IEnumerable<int> Generate ()
		{
			// write function call
			Add(OpCode.IMM); Add(N);
			Add(OpCode.PSH);
			Add(OpCode.JSR);
			int _factorial = Current + 4; Add(_factorial);
			Add(OpCode.PRINT);
			Add(OpCode.EXIT); Add(0);
		
			// int factorial (int n) {
			Add(OpCode.ENT);
			//   if(n <= 0)
			Add(OpCode.LEA); Add(2);
			Add(OpCode.LI);
			Add(OpCode.PSH);
			Add(OpCode.IMM); Add(0);
			Add(OpCode.LE);
			Add(OpCode.BZ); Add(Current + 3);
			//     return 1
			Add(OpCode.IMM); Add(1);
			Add(OpCode.LEV);

			// return n * factorial(n - 1)
			Add(OpCode.LEA); Add(2);  // n
			Add(OpCode.LI);
			Add(OpCode.PSH);
			Add(OpCode.LEA); Add(2);  // n
			Add(OpCode.LI);
			Add(OpCode.PSH);
			Add(OpCode.IMM); Add(1);
			Add(OpCode.SUB);          // n - 1
			Add(OpCode.PSH);
			Add(OpCode.JSR); Add(_factorial);
			Add(OpCode.ADJ); Add(1);  // pop result
			Add(OpCode.MUL);          // n * result
			Add(OpCode.LEV);
			Add(OpCode.LEV);

			return GetCode();
		}

		public IEnumerable<int> GenerateOld ()
		{
			List<int> code = new List<int>();

			// int factorial (int n) {
			code.Add((int)OpCode.ENT); code.Add(0);
			//   if(n <= 0) return 1;
			code.Add((int)OpCode.LEA); code.Add(2);
			code.Add((int)OpCode.LI);
			code.Add((int)OpCode.PSH);
			code.Add((int)OpCode.IMM); code.Add(0);
			code.Add((int)OpCode.LE);
			code.Add((int)OpCode.BZ); code.Add(15);
			code.Add((int)OpCode.IMM); code.Add(1);
			code.Add((int)OpCode.LEV);
			// return n * factorial(n - 1)
			code.Add((int)OpCode.LEA); code.Add(2);
			code.Add((int)OpCode.LI);
			code.Add((int)OpCode.PSH);
			code.Add((int)OpCode.LEA); code.Add(2);
			code.Add((int)OpCode.LI);
			code.Add((int)OpCode.PSH);
			code.Add((int)OpCode.IMM); code.Add(1);
			code.Add((int)OpCode.SUB);
			code.Add((int)OpCode.PSH);
			code.Add((int)OpCode.JSR); code.Add(0);
			code.Add((int)OpCode.ADJ); code.Add(1);
			code.Add((int)OpCode.MUL);
			code.Add((int)OpCode.LEV);
			// }
			code.Add((int)OpCode.LEV);

			return code;
		}
	}

	public class Program
	{
		static void Main(string[] args)
		{
		}
	}
}
