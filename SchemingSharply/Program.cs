using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchemingSharply.CellMachine;
using SchemingSharply.Scheme;
using SchemingSharply.StackMachine;

namespace SchemingSharply {
	public class Program
	{
		static void Main(string[] args) {
			//RunTests();
			RunEvalLoop();
		}

		static void RunEvalLoop() {
			string evalCode = System.IO.File.ReadAllText("../../Core/Eval.asm");
			string evalEntry = "main";
			CodeResult evalCodeResult;

			try {
				evalCodeResult = CellMachineAssembler.Assemble(evalCode, evalEntry);
			} catch (Exception e) {
				Console.WriteLine("Failed to assemble: {0}", e.Message);
#if DEBUG
				Console.WriteLine("Stack trace:");
				Console.WriteLine(e.StackTrace);
#endif
				return;
			}

			SchemeEnvironment env = new SchemeEnvironment();
			StandardRuntime.AddGlobals(env);

			List<Cell> history = new List<Cell>();
			// Add a function to get a history result
			env.Insert("h", new Cell(args => new Cell(history[(int)(args[0])])));

			Console.WriteLine("SchemingSharply v {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine("Type `quit' to quit");

			for (; ;) {
				int index = history.Count;
				Console.Write("{0}> ", index);
				string entry = Console.ReadLine().Trim();
				if (entry.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
					entry.Equals("exit", StringComparison.OrdinalIgnoreCase))
					break;
				if (entry.Equals(""))
					continue;
				try {
					Cell entered = StandardRuntime.Read(entry);
					Cell result = DoEval(entered, evalCodeResult, env);
					Console.WriteLine("===> {0}", result);
					history.Add(result);
				} catch (Exception e) {
					Console.WriteLine("!!!> {0}", e.Message);
				}
			}
		}

		protected static Cell DoEval(Cell code, CodeResult eval, SchemeEnvironment env) {
			Cell[] args = { code, new Cell(env) };
			Machine machine = new Machine(eval, args);
			while(machine.Finished == false) {
				machine.Step();
			}
			return machine.A;
		}

		static void RunTests() {
			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			//TestScheme();
			//TestVM();
			//SchemingSharply.CellMachine.Machine.Test1();
			//SchemingSharply.CellMachine.Machine.TestCompileFac();
			SchemingSharply.CellMachine.Machine.TestCompileEval();

			if (System.Diagnostics.Debugger.IsAttached)
			{
				sw.Stop();
				Console.Error.WriteLine("[DEBUG] Code run in {0}", sw.Elapsed);
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
