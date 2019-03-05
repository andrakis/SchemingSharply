using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SchemingSharply.CellMachine;
using SchemingSharply.Scheme;
using SchemingSharply.StackMachine;

namespace SchemingSharply {
	public class Program
	{
		class ProgramArguments {
			public List<string> Files = new List<string>();
			public bool Help = false;
			public bool Interactive = false;
			public bool Tests = false;
			public bool Debug = false;
		}

		static ProgramArguments ReadArguments(string[] args) {
			ProgramArguments parg = new ProgramArguments();
			int i = 0;
			while(i < args.Length) {
				string lowered = args[i].ToLower();
				if (args[i] == "-I")
					parg.Interactive = true;
				else if (args[i] == "-T")
					parg.Tests = true;
				else if (args[i] == "-d")
					parg.Debug = true;
				else switch (lowered) {
						case "help":
						case "-help":
						case "--help":
							parg.Help = true;
							break;
						default:
							parg.Files.Add(args[i]);
							break;
					}
				++i;
			}
			return parg;
		}
		static bool Debug = false;

		static void Main(string[] args) {
			ProgramArguments PArgs = ReadArguments(args);
			if(PArgs.Help) {
				Console.WriteLine("Usage: executable [-I] [-T] [-d] [file1 file2 ..] [-help]");
				Console.WriteLine("");
				Console.WriteLine("Where:");
				Console.WriteLine("\t-I         Enter interactive (REPL) mode. Default if no files specified.");
				Console.WriteLine("\t-T         Run tests");
				Console.WriteLine("\t-d         Enable instruction debugging");
				Console.WriteLine("\tfile1..    File to run");
				Console.WriteLine("\t-help      Show this help");
				return;
			}

			Debug = PArgs.Debug;

			SchemeEnvironment env = new SchemeEnvironment();
			StandardRuntime.AddGlobals(env);

			if (PArgs.Tests)
				RunTests();
			else if (PArgs.Files.Count == 0)
				PArgs.Interactive = true;

			if(PArgs.Files.Count > 0) {
				foreach (string file in PArgs.Files)
					RunSpecifiedFile(file, env);
				if (PArgs.Interactive == false &&
					System.Diagnostics.Debugger.IsAttached) {
					Console.WriteLine("[DEBUG] Press ENTER to end.");
					Console.ReadLine();
				}
			}
			if(PArgs.Interactive)
				REPL(env);
		}

		static string[] FileSearchPaths = {
			"./",
			"../",
			"../../",
			"./Core/",
			"../Core/",
			"../../Core/",
			"./Scheme/",
			"../Scheme/",
			"../../Scheme/"
		};
		public static string GetFilePath (string spec) {
			foreach(string prefix in FileSearchPaths) {
				string fullPath = prefix + spec;
				if (File.Exists(fullPath))
					return fullPath;
			}
			throw new FileNotFoundException(spec);
		}

		static void RunSpecifiedFile(string path, SchemeEnvironment env = null) {
			try {
				// Assemble Eval.asm
				string evalCode = File.ReadAllText(GetFilePath("Eval.asm"));
				string evalEntry = "main";
				CodeResult evalCodeResult;
				evalCodeResult = CellMachineAssembler.Assemble(evalCode, evalEntry);
				// Read in specified file
				string specCode = File.ReadAllText(GetFilePath(path));
				Cell specCodeCell = StandardRuntime.Read(specCode);
				if (env == null) {
					// Create environment
					env = new SchemeEnvironment();
					StandardRuntime.AddGlobals(env);
				}
				// Create VM
				Cell[] args = new Cell[] { specCodeCell, new Cell(env) };
				Machine machine = new Machine(evalCodeResult, args);
				machine.DebugMode = Debug;
				// Run VM
				while(machine.Finished == false) {
					machine.Step();
				}
				Console.Error.WriteLine("Finish with value: {0}", machine.A);
			} catch (Exception e) {
				Console.Error.WriteLine("!!! {0}", e.Message);
			}
		}

		const int READLINE_BUFFER_SIZE = 30 * 1024;
		static string ReadLine (string prompt) {
			Console.Write(prompt);
			Stream inputStream = Console.OpenStandardInput(READLINE_BUFFER_SIZE);
			byte[] bytes = new byte[READLINE_BUFFER_SIZE];
			int outputLength = inputStream.Read(bytes, 0, READLINE_BUFFER_SIZE);
			char[] chars = Encoding.UTF8.GetChars(bytes, 0, outputLength);
			return new string(chars);
		}
		static void REPL(SchemeEnvironment env = null) {
			string evalCode = System.IO.File.ReadAllText(GetFilePath("Eval.asm"));
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

			if (env == null) {
				env = new SchemeEnvironment();
				StandardRuntime.AddGlobals(env);
			}

			List<Cell> history = new List<Cell>();
			bool quit = false;

			// Add a function to get a history result
			env.Insert("h", new Cell(args => new Cell(history[(int)(args[0])])));
			env.Insert("eval", new Cell((args, subenv) => {
				if (args.Length > 1)
					subenv = args[1].Environment;
				return DoEval(new Cell(args[0]), evalCodeResult, subenv);
			}));
			env.Insert("env", new Cell((args, subenv) => new Cell(subenv)));
			env.Insert("str", new Cell(args => new Cell(args[0].ToString())));
			env.Insert("env-str", new Cell(args => new Cell(args[0].Environment.ToString())));
			env.Insert("exit", new Cell(args => { quit = true; return StandardRuntime.Nil; }));
			env.Insert("quit", env.Lookup(new Cell("exit")));

			Console.WriteLine("SchemingSharply v {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
			Console.WriteLine("Type `quit' to quit");
			Console.WriteLine("Type `(env-str (env))' to display environment");
			Console.WriteLine("Use `(eval expr)' or `(eval expr (env))' for testing");
			Console.WriteLine("Use `(h n)' to view history item n");
			Console.WriteLine();

			while(!quit) { 
				int index = history.Count;
				string entry = ReadLine(string.Format("{0}> ", index)).Trim();
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
			machine.DebugMode = Debug;
			while (machine.Finished == false) {
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

			RunUnitTests();
			sw.Stop();
			//Console.Error.WriteLine("[DEBUG] Code run in {0}", sw.Elapsed);
		}

		private static SchemeEnvironment UnitTestEnvironment;
		private static CodeResult UnitTestCode;
		private static int UnitTestSuccess, UnitTestFail;
		private static void RunUnitTests() {
			string evalCode = System.IO.File.ReadAllText(Program.GetFilePath("Eval.asm"));
			string evalEntry = "main";

			try {
				UnitTestCode = CellMachineAssembler.Assemble(evalCode, evalEntry);
			} catch (Exception e) {
				Console.WriteLine("Failed to assemble: {0}", e.Message);
#if DEBUG
				Console.WriteLine("Stack trace:");
				Console.WriteLine(e.StackTrace);
#endif
				return;
			}

			UnitTestEnvironment = new SchemeEnvironment();
			StandardRuntime.AddGlobals(UnitTestEnvironment);
			UnitTestSuccess = UnitTestFail = 0;

			TEST("((lambda (X) (+ X X)) 5)", "10");
			TEST("(< 10 2)", "#false");
			TEST("(<= 10 2)", "#false");
			//TEST("(quote \"f\\\"oo\")", "f\\\"oo");
			//TEST("(quote \"foo\")", "foo");
			//TEST("(quote (testing 1 (2.0) -3.14e159))", "(testing 1 (2.000000e+00) -3.140000e+159)");
			TEST("(+ 2 2)", "4");
			//TEST("(+ 2.5 2)", "4.500000e+00");
			//TEST("(* 2.25 2)", "4.500000e+00");    // Bugfix, multiplication was losing floating point value
			TEST("(+ (* 2 100) (* 1 10))", "210");
			TEST("(> 6 5)", "#true");
			TEST("(< 6 5)", "#false");
			TEST("(if (> 6 5) (+ 1 1) (+ 2 2))", "2");
			TEST("(if (< 6 5) (+ 1 1) (+ 2 2))", "4");
			TEST("(define X 3)", "3");
			TEST("X", "3");
			TEST("(+ X X)", "6");
			TEST("(begin (define X 1) (set! X (+ X 1)) (+ X 1))", "3");
			TEST("(define twice (lambda (X) (* 2 X)))", "#Lambda((X) (* 2 X))");
			TEST("(twice 5)", "10");
			TEST("(define compose (lambda (F G) (lambda (X) (F (G X)))))", "#Lambda((F G) (lambda (X) (F (G X))))");
			TEST("((compose list twice) 5)", "(10)");
			TEST("(define repeat (lambda (F) (compose F F)))", "#Lambda((F) (compose F F))");
			TEST("((repeat twice) 5)", "20");
			TEST("((repeat (repeat twice)) 5)", "80");
			// Factorial - head recursive
			TEST("(define fact (lambda (N) (if (<= N 1) 1 (* N (fact (- N 1))))))", "#Lambda((N) (if (<= N 1) 1 (* N (fact (- N 1)))))");
			TEST("(fact 3)", "6");
			// TODO: Bignum support
			TEST("(fact 12)", "479001600");
			// Factorial - tail recursive
			TEST("(begin (define fac (lambda (N) (fac2 N 1))) (define fac2 (lambda (N A) (if (<= N 0) A (fac2 (- N 1) (* N A))))))", "#Lambda((N A) (if (<= N 0) A (fac2 (- N 1) (* N A))))");
			//TEST("(fac 50.1)", "4.732679e+63");   // Bugfix, multiplication was losing floating point value
			TEST("(define abs (lambda (N) ((if (> N 0) + -) 0 N)))", "#Lambda((N) ((if (> N 0) + -) 0 N))");
			TEST("(list (abs -3) (abs 0) (abs 3))", "(3 0 3)");
			TEST("(define combine (lambda (F)" +
				"(lambda (X Y)" +
				"(if (null? X) (quote ())" +
				"(F (list (head X) (head Y))" +
				"((combine F) (tail X) (tail Y)))))))", "#Lambda((F) (lambda (X Y) (if (null? X) (quote ()) (F (list (head X) (head Y)) ((combine F) (tail X) (tail Y))))))");
			TEST("(define zip (combine cons))", "#Lambda((X Y) (if (null? X) (quote ()) (F (list (head X) (head Y)) ((combine F) (tail X) (tail Y)))))");
			TEST("(zip (list 1 2 3 4) (list 5 6 7 8))", "((1 5) (2 6) (3 7) (4 8))");
			TEST("(define riff-shuffle (lambda (Deck) (begin" +
				"(define take (lambda (N Seq) (if (<= N 0) (quote ()) (cons (head Seq) (take (- N 1) (tail Seq))))))" +
				"(define drop (lambda (N Seq) (if (<= N 0) Seq (drop (- N 1) (tail Seq)))))" +
				"(define mid (lambda (Seq) (/ (length Seq) 2)))" +
				"((combine append) (take (mid Deck) Deck) (drop (mid Deck) Deck)))))", "#Lambda((Deck) (begin (define take (lambda (N Seq) (if (<= N 0) (quote ()) (cons (head Seq) (take (- N 1) (tail Seq)))))) (define drop (lambda (N Seq) (if (<= N 0) Seq (drop (- N 1) (tail Seq))))) (define mid (lambda (Seq) (/ (length Seq) 2))) ((combine append) (take (mid Deck) Deck) (drop (mid Deck) Deck))))");
			TEST("(riff-shuffle (list 1 2 3 4 5 6 7 8))", "(1 5 2 6 3 7 4 8)");
			TEST("((repeat riff-shuffle) (list 1 2 3 4 5 6 7 8))", "(1 3 5 7 2 4 6 8)");
			TEST("(riff-shuffle (riff-shuffle (riff-shuffle (list 1 2 3 4 5 6 7 8))))", "(1 2 3 4 5 6 7 8)");
			goto end;

			end:
			if (UnitTestFail > 0)
				Console.WriteLine("TEST FAILURES OCCURRED");
			Console.WriteLine("{0} success, {1} failures", UnitTestSuccess, UnitTestFail);
		}

		static void TEST (string code, string expected) {
			Cell codeCell = StandardRuntime.Read(code);
			Cell result = DoEval(codeCell, UnitTestCode, UnitTestEnvironment);
			if(result.ToString() != expected) {
				Console.WriteLine("TEST FAILED: {0}, expected {1} got {2}",
					code, expected, result);
				UnitTestFail++;
			} else {
				Console.WriteLine("TEST SUCCESS: {0}, got expected {1}",
					code, result);
				UnitTestSuccess++;
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
