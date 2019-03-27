using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			/// <summary>
			/// Whether to display help.
			/// </summary>
			public bool Help = false;
			/// <summary>
			/// Whether to go into REPL.
			/// </summary>
			public bool Interactive = false;
			/// <summary>
			/// Whether to run tests.
			/// </summary>
			public bool Tests = false;
			/// <summary>
			/// Whether to enable debug mode.
			/// </summary>
			public bool Debug = false;
			/// <summary>
			/// Whether to enable timing mode.
			/// </summary>
			public bool Timing = false;
			/// <summary>
			/// The evaluator class to use.
			/// </summary>
			public Type EvaluatorType =
				// typeof(CellMachineEval);
				// typeof(StandardEval);
				 typeof(FrameEval);
		}

		static ProgramArguments ReadArguments(string[] args) {
			ProgramArguments parg = new ProgramArguments();
			int i = 0;
			while(i < args.Length) {
				string lowered = args[i].ToLower();
				if (args[i] == "-I")
					parg.Interactive = true;
				else if (args[i] == "-t")
					parg.Timing = true;
				else if (args[i] == "-T")
					parg.Tests = true;
				else if (args[i] == "-d")
					parg.Debug = true;
				else if (args[i] == "-E") {
					++i;
					switch(args[i].ToLower()) {
						case "cell":
							parg.EvaluatorType = typeof(CellMachineEval);
							break;
						case "classic":
							parg.EvaluatorType = typeof(StandardEval);
							break;
						case "frame":
							parg.EvaluatorType = typeof(FrameEval);
							break;
						default:
							Console.Error.WriteLine("Invalid evaluator mode, must be one of: cell, classic, frame");
							break;
					}
				}
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

		protected static ISchemeEval Evaluator;
		static void Main(string[] args) {
			ProgramArguments PArgs = ReadArguments(args);
			if(PArgs.Help) {
				Console.WriteLine("Usage: executable [-I] [-E evaluator] [-T] [-t] [-d] [file1 file2 ..] [-help]");
				Console.WriteLine("");
				Console.WriteLine("Where:");
				Console.WriteLine("\t-I            Enter interactive (REPL) mode. Default if no files specified.");
				Console.WriteLine("\t-E evaluator  Use given evaluator, valid options: cell, classic, frame");
				Console.WriteLine("\t-T            Run tests");
				Console.WriteLine("\t-t            Enable timing");
				Console.WriteLine("\t-d            Enable instruction debugging");
				Console.WriteLine("\tfile1..       File to run");
				Console.WriteLine("\t-help         Show this help");
				Console.WriteLine("");
				Console.WriteLine("Evaluators:");
				Console.WriteLine("\tframe         Frame evaluator. Combines cell and classic benefits. Default.");
				Console.WriteLine("\tcell          Cell virtual machine.");
				Console.WriteLine("\tclassic       Classic eval loop, no multitasking.");
				return;
			}

			Debug = PArgs.Debug;
			ShowTimings = PArgs.Timing;
			Evaluator = Activator.CreateInstance(PArgs.EvaluatorType) as ISchemeEval;

			SchemeEnvironment env = new SchemeEnvironment();
			StandardRuntime.AddGlobals(env);

			if (PArgs.Tests)
				RunTests();
			else if (PArgs.Files.Count == 0)
				PArgs.Interactive = true;

			if(PArgs.Files.Count > 0) {
				foreach (string file in PArgs.Files)
					RunSpecifiedFile(file, env);
			}
			if(PArgs.Interactive)
				REPL(env);
			else if (System.Diagnostics.Debugger.IsAttached) {
				Console.WriteLine("[DEBUG] Press ENTER to end.");
				Console.ReadLine();
			}
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

		static bool ShowTimings = false;
		static void RunSpecifiedFile(string path, SchemeEnvironment env = null) {
			try {
				// Read in specified file
				string specCode = File.ReadAllText(GetFilePath(path));
				Cell specCodeCell = StandardRuntime.Read(specCode);
				if (env == null) {
					// Create environment
					env = new SchemeEnvironment();
					StandardRuntime.AddGlobals(env);
				}
				Stopwatch sw = new Stopwatch();
				DoEval(specCodeCell, env);
				sw.Stop();
				if(ShowTimings)
					Console.WriteLine("=== Executed {0} steps in {1}ms", Evaluator.Steps, sw.ElapsedMilliseconds);
				Console.Error.WriteLine("Finish with value: {0}", Evaluator.Result);
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
			if (env == null) {
				env = new SchemeEnvironment();
				StandardRuntime.AddGlobals(env);
			}

			ISchemeEval evaluator;
			evaluator = new CellMachineEval();

			List<Cell> history = new List<Cell>();
			bool quit = false;
			bool timing = false;

			// Add a function to get a history result
			env.Insert("h", new Cell(args => new Cell(history[(int)(args[0])])));
			env.Insert("eval", new Cell((args, subenv) => {
				if (args.Length > 1)
					subenv = args[1].Environment;
				return DoEval(new Cell(args[0]), subenv);
			}));
			env.Insert("env", new Cell((args, subenv) => new Cell(subenv)));
			env.Insert("str", new Cell(args => new Cell(args[0].ToString())));
			env.Insert("env-str", new Cell(args => new Cell(args[0].Environment.ToString())));
			env.Insert("exit", new Cell(args => { quit = true; return StandardRuntime.Nil; }));
			env.Insert("quit", env.Lookup(new Cell("exit")));
			env.Insert("timing", new Cell(args => {
				if (args.Length > 0) {
					timing = ((string)args[0] == "on" || args[0] == StandardRuntime.True);
					Console.Error.WriteLine("Timing is now " + (timing ? "on" : "off"));
				}
				return timing ? StandardRuntime.True : StandardRuntime.False;
			}));

			// Add evaluator constants
			env.Insert("evcell", new Cell("evcell"));
			env.Insert("evclassic", new Cell("evclassic"));
			env.Insert("evframe", new Cell("evframe"));
			// Add function to change evaluator
			env.Insert("sweval", new Cell(args => {
				if (args.Length == 0) {
					if (Evaluator.GetType() == typeof(CellMachineEval))
						return new Cell("evcell");
					else if (Evaluator.GetType() == typeof(StandardEval))
						return new Cell("evclassic");
					else if (Evaluator.GetType() == typeof(FrameEval))
						return new Cell("evframe");
					else
						throw new InvalidDataException();
				}

				ISchemeEval newEvaluator;
				Cell result;
				switch(args[0].Value) {
					case "evcell":
						newEvaluator = new CellMachineEval();
						result = new Cell("evcell");
						break;
					case "evclassic":
						newEvaluator = new StandardEval();
						result = new Cell("evclassic");
						break;
					case "evframe":
						newEvaluator = new FrameEval();
						result = new Cell("evframe");
						break;

					default:
						return new Cell("#invalid_argument");
				}

				// Copy details to new evaluator
				newEvaluator.Debug = Evaluator.Debug;
				Evaluator = newEvaluator;

				return result;
			}));
			// Add function to run unit tests on evaluator
			env.Insert("swtest", new Cell(args => {
				var results = SchemeEval.RunTests(Evaluator);
				Cell result = new Cell(CellType.LIST);
				result.ListValue.Add(new Cell(results.Success));
				result.ListValue.Add(new Cell(results.Failures));
				return result;
			}));

			env.Insert("help", new Cell(args => {
				Console.WriteLine("SchemingSharply v {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
				Console.WriteLine("Type `quit' or `\\q' to quit");
				//Console.WriteLine("Type `(env-str (env))' to display environment");
				//Console.WriteLine("Use `(eval expr)' or `(eval expr (env))' for testing");
				Console.WriteLine("Use `(h n)' to view history item n");
				Console.WriteLine("Use `(timing #true)` to enable timing, `(debug #true)` to enable debugging");
				Console.WriteLine("Use `(sweval ...)` to get or set evaluator; valid options: evcell, evclassic, evframe");
				Console.WriteLine("Use `(swtest)` to run unit tests");
				Console.WriteLine("Use `(help)' to display this message again");
				Console.WriteLine();
				return StandardRuntime.Nil;
			}));

			env["help"].ProcValue(new Cell[] { }); // invoke

			while(!quit) { 
				int index = history.Count;
				string entry = ReadLine(string.Format("{0}> ", index)).Trim();
				if (entry.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
					entry.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
					entry == "\\q")
					break;
				if (entry.Equals(""))
					continue;
				try {
					Stopwatch sw = new Stopwatch();
					sw.Start();
					Cell entered = StandardRuntime.Read(entry);
					LastExecutedSteps = Evaluator.Steps;
					Cell result = DoEval(entered, env);
					string steps = (Evaluator.Steps - LastExecutedSteps).ToString();
					if (Evaluator.Steps < LastExecutedSteps) steps = "??";
					sw.Stop();
					Console.WriteLine("===> {0}", result);
					if (timing)
						Console.WriteLine("=== Executed {0} steps in {1}ms", steps, sw.ElapsedMilliseconds);
					history.Add(result);
				} catch (Exception e) {
					Console.WriteLine("!!!> {0}", e.Message);
				}
			}
		}

		static uint LastExecutedSteps = 0;
		protected static Cell DoEval(Cell code, SchemeEnvironment env) {
			// Update/set (debug) function
			env.Insert("debug", new Cell(btargs => {
				if(btargs.Length > 0) {
					Evaluator.Debug = (btargs[0] == StandardRuntime.True);
				}
				Debug = Evaluator.Debug;
				return Debug ? StandardRuntime.True : StandardRuntime.False;
			}));
			Evaluator.Debug = Debug;
			Evaluator.Eval(code, env);
			return Evaluator.Result;
		}

		[Flags]
		enum UnitTestSelection : uint {
			CellTest1   = 0b0000001,
			CellTestFac = 0b0000010,
			CellTestEval= 0b0000100,
			CellUnitTest= 0b0001000,
			CellAll     = 0b0001111,
			EvalStandard= 0b0010000,
			EvalFrame   = 0b0100000,
			EvalCell    = 0b1000000,
			EvalAll     = 0b1110000,
			Release     = CellUnitTest,
			Debug       = EvalStandard | EvalCell,
		}
		static void RunTests() {
			UnitTestSelection selection;
#if DEBUG
			selection = UnitTestSelection.Debug;
#else
			selection = UnitTestSelection.Release;
#endif

			Stopwatch sw = new Stopwatch();
			sw.Start();

			if(selection.HasFlag(UnitTestSelection.CellTest1)) Machine.Test1();
			if(selection.HasFlag(UnitTestSelection.CellTestFac)) Machine.TestCompileFac();
			if(selection.HasFlag(UnitTestSelection.CellTestEval)) Machine.TestCompileEval();
			if(selection.HasFlag(UnitTestSelection.CellUnitTest)) RunUnitTests();

			StandardEval evl = new StandardEval();
			FrameEval frevl = new FrameEval();
			CellMachineEval clevl = new CellMachineEval();
			if(selection.HasFlag(UnitTestSelection.EvalStandard)) SchemeEval.RunTests(evl);
			if(selection.HasFlag(UnitTestSelection.EvalCell)) SchemeEval.RunTests(clevl);
			if(selection.HasFlag(UnitTestSelection.EvalFrame)) SchemeEval.RunTests(frevl);

			sw.Stop();
			if(ShowTimings)
				Console.WriteLine("=== Executed {0} steps in {1}ms", CellMachine.Machine.StepsExecuted, sw.ElapsedMilliseconds);
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
			Cell result = DoEval(codeCell, UnitTestEnvironment);
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
