using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchemingSharply.Scheme;
using SchemingSharply.StackMachine;

namespace SchemingSharply {
	public class Program
	{
		static void Main(string[] args)
		{
			//TestScheme();
			//TestVM();
			SchemingSharply.CellMachine.Machine.Test1();
			SchemingSharply.CellMachine.Machine.TestCompileFac();
			//SchemingSharply.CellMachine.Machine.TestCompileEval();

			if (System.Diagnostics.Debugger.IsAttached)
			{
				Console.WriteLine("[DEBUG] Press ENTER to end.");
				Console.ReadLine();
			}
		}

		static void TestVM ()
		{
			List<int> code = new List<int>();
			code.Add((int)StackMachine.OpCode.DEBUG_STATE);
			code.Add((int)StackMachine.OpCode.DEBUG3);
			code.Add(1); code.Add(2); code.Add(3);
			code.Add((int)StackMachine.OpCode.DEBUG_STATE);
			StackMachine.StackMachine machine = new StackMachine.StackMachine(code);
			while(machine.Active)
			{
				machine.Cycle();
			}
		}

		static void TestScheme () {
			string code = "(begin " +
				"(define fac (lambda (n) " +
					"(if (<= n 1) 1 " +
						"(* n (fac (- n 1))))))" +
				"(print (fac 10)))";
			Cell codeCell = StandardRuntime.Read(code);
			Console.WriteLine("Code: {0}{1}", codeCell, System.Environment.NewLine);
			SchemeEnvironment env = new SchemeEnvironment();
			StandardRuntime.AddGlobals(env);
			StandardEval ev = new StandardEval();
			ev.Eval(codeCell, env);
		}
	}
}
