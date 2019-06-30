using SchemingSharply.CellMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemingSharply.Scheme
{
	public enum CellType
	{
		/// <summary>
		///   Cell is a symbolic string. Use .Value
		/// </summary>
		SYMBOL,
		/// <summary>
		///   Cell is a string. Use .Value
		/// </summary>
		STRING,
		/// <summary>
		///   Cell is a number. Use (int)Cell
		/// </summary>
		NUMBER,
		/// <summary>
		///   Cell is a list. Use .ListValue
		/// </summary>
		LIST,
		/// <summary>
		///   Cell is a lambda. Use .ListValue and .Environment
		/// </summary>
		LAMBDA,
		/// <summary>
		///   Cell is a macro. Use .ListValue
		/// </summary>
		MACRO,
		/// <summary>
		///   Cell is a pointer to a C# function, signature:
		///   delegate Cell CellProc(Cell[] args)
		/// </summary>
		PROC,
		/// <summary>
		///   Cell is a pointer to a C#  function, signature:
		///   delegate Cell CellProcEnv(Cell[] args, SchemeEnvironment env)
		/// </summary>
		PROCENV,
		/// <summary>
		///   Cell is a pointer to an environment
		/// </summary>
		ENVPTR
	}

	public interface IVeryAbstractCell {
		int ToInteger();
		string ToString();
	}

	public interface IAbstractCell<TCell>
		where TCell : ICell
	{
		int ToInteger();
		TCell Head();
		TCell Tail();
	}

	public interface ICell : IAbstractCell<Cell>
	{
		CellType Type { get; }
		string Value { get; }
		List<Cell> ListValue { get; }
		SchemeEnvironment Environment { get; set; }

	}

	public struct Cell : ICell
	{
		public delegate Cell CellProc(Cell[] args);
		public delegate Cell CellProcEnv(Cell[] args, SchemeEnvironment env);

		public static string NilValue = "#nil";
		public static string TrueValue = "#true";
		public static string FalseValue = "#false";

		public CellType Type { get; set; }
		public string Value { get; }
		public List<Cell> ListValue { get; }
		public CellProc ProcValue { get; }
		public CellProcEnv ProcEnvValue { get; }
		public SchemeEnvironment Environment { get; set; }

		public Cell(string value, CellType type = CellType.SYMBOL)
		{
			Type = type;
			Value = value;
			ListValue = new List<Cell>();
			if (Value == null) Value = NilValue;
			ProcValue = null;
			ProcEnvValue = null;
			Environment = null;
		}

		public Cell(bool value)
			: this(value ? TrueValue : FalseValue)
		{
		}

		public Cell(int value)
			: this(value.ToString(), CellType.NUMBER)
		{
		}

		public Cell(IEnumerable<Cell> list)
		{
			Type = CellType.LIST;
			Value = "";
			ListValue = list.ToList();
			ProcValue = null;
			ProcEnvValue = null;
			Environment = null;
		}

		public Cell(CellProc proc)
		{
			Type = CellType.PROC;
			Value = "";
			ListValue = new List<Cell>();
			ProcValue = proc;
			ProcEnvValue = null;
			Environment = null;
		}

		public Cell(CellProcEnv proc)
		{
			Type = CellType.PROCENV;
			Value = "";
			ListValue = new List<Cell>();
			ProcValue = null;
			ProcEnvValue = proc;
			Environment = null;
		}

		public Cell(CellType type)
		{
			Type = type;
			Value = "";
			ListValue = new List<Cell>();
			ProcValue = null;
			ProcEnvValue = null;
			Environment = null;
		}

		public Cell(SchemeEnvironment envptr) {
			Type = CellType.ENVPTR;
			Value = "";
			ListValue = new List<Cell>();
			ProcValue = null;
			ProcEnvValue = null;
			Environment = envptr;
		}

		public Cell (Cell other) {
			Type = other.Type;
			Value = other.Value;
			ListValue = other.ListValue;
			ProcValue = other.ProcValue;
			ProcEnvValue = other.ProcEnvValue;
			Environment = other.Environment;
		}

		public override bool Equals(object obj) => base.Equals(obj);
		public override int GetHashCode() => base.GetHashCode();
		public override string ToString() => (string)this;

		public static explicit operator int (Cell c)
		{
			if (c.Type != CellType.NUMBER)
			{
				if (c == StandardRuntime.True)
					return 1;
				return 0;
			}

			return int.Parse(c.Value);
		}

		public int ToInteger() { return (int)this; }

		public Cell Head()
		{
			if (ListValue.Count == 0) return StandardRuntime.Nil;
			return ListValue[0];
		}

		public Cell HeadOr(Cell or) => ListValue.Count == 0 ? or : ListValue[0];

		public Cell Tail()
		{
			if (ListValue.Count == 0)
				return new Cell(CellType.LIST);
			return new Cell(ListValue.Skip(1));
		}

		public bool Empty () { return ListValue.Count == 0; }
		private static string listToString(List<Cell> cells)
		{
			var cells2 = cells.ConvertAll((c) => (string)c);
			return "(" + string.Join(" ", cells2) + ")";
		}

		public IEnumerator<Cell> GetEnumerator() => ListValue.GetEnumerator();

		public static explicit operator string(Cell c)
		{
			switch (c.Type) {
				case CellType.SYMBOL:
				case CellType.STRING:
				case CellType.NUMBER:
					return c.Value;
				case CellType.PROC:
					return "#Proc";
				case CellType.PROCENV:
					return "#ProcEnv";
				case CellType.LIST:
					return listToString(c.ListValue);
				case CellType.LAMBDA:
				case CellType.MACRO:
					string r = c.Type == CellType.LAMBDA ? "#Lambda(" : "#Macro(";
					r += c.ListValue[1].ToString();
					r += " " + c.ListValue[2].ToString();
					r += ")";
					return r;
				case CellType.ENVPTR:
					return "#Env";
				default:
					throw new InvalidOperationException();
			}
		}

		public static Cell operator + (Cell a, Cell b)
		{
			if (a.Type == CellType.NUMBER)
				return new Cell((int)a + (int)b);
			else if (a.Type == CellType.SYMBOL || a.Type == CellType.STRING)
				return new Cell((string)a + (string)b);
			else if (a.Type == CellType.LIST)
			{
				List<Cell> cells = a.ListValue;
				cells.AddRange(b.ListValue);
				return new Cell(cells);
			} else
			{
				throw new SchemeException("Invalid operands to +");
			}
		}

		public static Cell operator - (Cell a, Cell b)
		{
			return new Cell((int)a - (int)b);
		}

		public static Cell operator * (Cell a, Cell b)
		{
			return new Cell((int)a * (int)b);
		}

		public static Cell operator / (Cell a, Cell b)
		{
			return new Cell((int)a / (int)b);
		}

		public static bool operator !=(Cell a, Cell b)
		{
			return !(a == b);
		}

		public static bool operator ==(Cell a, Cell b)
		{
			switch(a.Type)
			{
				case CellType.SYMBOL:
				case CellType.STRING:
				case CellType.NUMBER:
					return a.Value == b.Value;
				case CellType.LIST:
					return a.ListValue == b.ListValue;
				case CellType.LAMBDA:
					return a.ListValue == b.ListValue && a.Environment == b.Environment;
				case CellType.PROC:
					return a.ProcValue == b.ProcValue;
				case CellType.ENVPTR:
					return a.Environment == b.Environment;
				default:
					return false;
			}
		}
	}

	public class CellNameNotFound : Exception {
		public CellNameNotFound(string name)
			: base("Cell name not found: " + name) { }
	}

	public class MissingCloseParenException : Exception {
		public MissingCloseParenException()
			: base("Missing close parenthese: )") { }
	}

	public class SchemeEnvironment
	{
		protected Dictionary<string, Cell> map = new Dictionary<string, Cell>();
		protected SchemeEnvironment outer;

		public SchemeEnvironment(SchemeEnvironment Outer = null)
		{
			outer = Outer;
		}

		public SchemeEnvironment(List<Cell> keys, List<Cell> values, SchemeEnvironment Outer) {
			outer = Outer;
			AddRange(keys, values);
		}

		public SchemeEnvironment(Cell keys, Cell values, SchemeEnvironment Outer) {
			outer = Outer;

			List<Cell> lc_keys = new List<Cell>();
			List<Cell> lc_values = new List<Cell>();
			if (keys.Type == CellType.LIST) {
				// List of keys
				lc_keys.AddRange(keys.ListValue);
				lc_values.AddRange(values.ListValue);
			}  else {
				// Single key capturing multiple arguments
				lc_keys.Add(keys);
				// Make into single list
				lc_values.Add(values);
			}
			AddRange(lc_keys, lc_values);
		}

		public void AddRange(List<Cell> keys, List<Cell> values) { 
			for (int i = 0; i < keys.Count(); ++i)
				Insert(keys[i].Value, values[i]);
		}

		public void Insert(string key, Cell value)
		{
			map[key] = value;
		}

		public Dictionary<string, Cell> Find (string key)
		{
			if (map.ContainsKey(key))
				return map;
			if (outer != null)
				return outer.Find(key);
			throw new CellNameNotFound(key);
		}

		public Cell this[string key] {
			get {
				return Lookup(key);
			}
		}

		public Cell Lookup (string key) {
			return Find(key)[key];
		}
		public Cell Lookup(Cell key)
		{
			return Lookup(key.ToString());
		}

		public Cell Set(Cell key, Cell value)
		{
			return Find(key.Value)[key.Value] = value;
		}

		public Cell Define(Cell key, Cell value)
		{
			map[key.Value] = value;
			return value;
		}

		public override string ToString() {
			var parts = map.Keys.Select(k => k + ": " + map[k].ToString()).ToList();
			if (outer != null)
				parts.Add(outer.ToString());
			return "#Env{ " + string.Join(", ", parts) + "}";
		}
	}

	public static class StandardRuntime
	{
		public static Cell Nil = new Cell((string)null);
		public static Cell True = new Cell(true);
		public static Cell False = new Cell(false);

		public static Cell Plus(Cell[] args)
		{
			Cell acc = args[0];
			for (int i = 1; i < args.Length; ++i)
				acc += args[i];
			return acc;
		}

		public static Cell Minus(Cell[] args)
		{
			Cell acc = args[0];
			for (int i = 1; i < args.Length; ++i)
				acc -= args[i];
			return acc;
		}

		public static Cell Multiply(Cell[] args)
		{
			Cell acc = args[0];
			for (int i = 1; i < args.Length; ++i)
				acc *= args[i];
			return acc;
		}

		public static Cell Divide(Cell[] args)
		{
			Cell acc = args[0];
			for (int i = 1; i < args.Length; ++i)
				acc /= args[i];
			return acc;
		}

		public static Cell LessThan (Cell[] args)
		{
			return (int)args[0] < (int)args[1] ? True : False;
		}

		public static Cell Equal (Cell[] args)
		{
			return args[0] == args[1] ? True : False;
		}

		public static Cell LessThanEqual (Cell[] args)
		{
			return ((int)args[0] <= (int)args[1]) ?  True : False;
		}

		public static Cell GreaterThan (Cell[] args) {
			return (int)args[0] > (int)args[1] ? True : False;
		}

		public static Cell GreaterThanEqual(Cell[] args) {
			return (int)args[0] >= (int)args[1] ? True : False;
		}

		public static Cell Print(Cell[] args)
		{
			var cellStrings = args.ToList().ConvertAll<string>((c) => (string)c);
			System.Console.WriteLine(string.Join(" ", cellStrings));
			return Nil;
		}

		public static Cell Head(Cell[] args) => args[0].Head();
		public static Cell Tail(Cell[] args) => args[0].Tail();
		public static Cell Nullp(Cell[] args) => (args[0].ListValue.Count == 0) ? True : False;
		public static Cell List(Cell[] args) => new Cell(args);
		public static Cell Append(Cell[] args) {
			Cell result = args[0];
			for (int i = 0; i < args[1].ListValue.Count; ++i)
				result.ListValue.Add(args[1].ListValue[i]);
			return result;
		}
		public static Cell Cons(Cell[] args) {
			Cell result = new Cell(CellType.LIST);
			result.ListValue.Add(args[0]);
			for (int i = 0; i < args[1].ListValue.Count; ++i)
				result.ListValue.Add(args[1].ListValue[i]);
			return result;
		}
		public static Cell Length(Cell[] args) => new Cell(args[0].ListValue.Count);

		public static void AddGlobals(SchemeEnvironment e)
		{
			e.Insert(False.Value, False); e.Insert(True.Value, True); e.Insert(Nil.Value, Nil);
			e.Insert("+", new Cell(Plus)); e.Insert("-", new Cell(Minus));
			e.Insert("*", new Cell(Multiply)); e.Insert("/", new Cell(Divide));
			e.Insert("<", new Cell(LessThan)); e.Insert("<=", new Cell(LessThanEqual));
			e.Insert(">", new Cell(GreaterThan)); e.Insert(">=", new Cell(GreaterThanEqual));
			e.Insert("=", new Cell(Equal)); e.Insert("==", new Cell(Equal));
			e.Insert("print", new Cell(Print));
			e.Insert("head", new Cell(Head)); e.Insert("tail", new Cell(Tail));
			e.Insert("null?", new Cell(Nullp)); e.Insert("list", new Cell(List));
			e.Insert("cons", new Cell(Cons)); e.Insert("append", new Cell(Append));
			e.Insert("length", new Cell(Length));
			
			// Add CellType.[Name] definitions
			CellType cellType = CellType.SYMBOL;
			foreach (var kv in cellType.GetKeyValues<int>())
				e.Insert("CellType." + kv.Key, new Cell(kv.Value));
		}

		public static bool IsWhiteSpace(char c) {
			return char.IsWhiteSpace(c) || c == '\n' || c == '\t' || c == '\r';
		}

		public static List<string> Tokenise (string str)
		{
			List<string> tokens = new List<string>();
			int s = 0;
			while(s < str.Length)
			{
				while (s < str.Length && IsWhiteSpace(str[s]))
					++s;
				if (s == str.Length) break;

				if (str[s] == ';' && str[s + 1] == ';')
					while (s < str.Length && str[s] != '\n' && str[s] != '\r')
						++s;
				else if (str[s] == '(' || str[s] == ')')
					tokens.Add(str[s++] == '(' ? "(" : ")");
				else if (str[s] == '"' || str[s] == '\'') {
					int t = s;
					char sp = str[s];
					int escape = 0;
					do {
						++t;
						if (escape != 0) escape--;
						if (str[t] == '\\')
							escape = 2; // skip this and the next character
					} while (t < str.Length && (escape != 0 || str[t] != sp));
					++t;
					tokens.Add(str.Substring(s, t - s));
					s = t;
				} else {
					int t = s;
					while (t < str.Length && !IsWhiteSpace(str[t])
						&& str[t] != '(' && str[t] != ')')
						++t;
					tokens.Add(str.Substring(s, t - s));
					s = t;
				}
			}
			return tokens;
		}

		public static Cell Atom(string token)
		{
			bool isNumber = false;
			isNumber = char.IsDigit(token[0]);
			if (token.Length > 1)
				isNumber = isNumber || (token[0] == '-' && char.IsDigit(token[1]));
			if (isNumber)
				return new Cell(token, CellType.NUMBER);
			if (token[0] == '"' && token.EndsWith("\""))
				return new Cell(token.Substring(1, token.Length - 2), CellType.STRING);
			return new Cell(token);
		}

		public static Cell ReadFrom (List<string> tokens)
		{
			string token = tokens.First();
			tokens.RemoveAt(0);
			if (token == "(")
			{
				List<Cell> cells = new List<Cell>();
				while (tokens.First() != ")")
				{
					cells.Add(ReadFrom(tokens));
					if (tokens.Count() == 0)
						throw new MissingCloseParenException();
				}
				tokens.RemoveAt(0);
				return new Cell(cells);
			} else
				return Atom(token);
		}

		public static Cell Read(string s)
		{
			List<string> tokens = Tokenise(s);
			return ReadFrom(tokens);
		}
	}

	public interface ISchemeEval
	{
		Cell Eval(Cell Arg, SchemeEnvironment Env);
		bool Debug { set; get; }
		uint Steps { get; }
		Cell Result { get; }
	}

	public class SchemeException : Exception
	{
		public SchemeException(string message) : base(message) { }
	}

	public abstract class SchemeEval : ISchemeEval {
		public abstract Cell Eval(Cell Arg, SchemeEnvironment Env);
		public virtual bool IsDebug() => false;
		public virtual void SetDebug(bool val) { }
		public virtual uint Steps { get { return 0; } }
		public Cell Result { get; protected set; }

		public bool Debug {
			get { return IsDebug(); }
			set { SetDebug(value); }
		}

		public struct TestResults {
			public readonly int Success, Failures, Total;
			public TestResults (int success, int failures, int total = 0) {
				if (total == 0) total = success + failures;
				Success = success;
				Failures = failures;
				Total = total;
			}
		}
		public static TestResults RunTests(ISchemeEval evalClass, bool display = true) {
			int success = 0, failures = 0;

			SchemeEnvironment env = new SchemeEnvironment();
			StandardRuntime.AddGlobals(env);
			Func<Cell, Cell> EvalCell = (Cell x) => evalClass.Eval(x, env);
			Func<string, Cell> EvalString = (string x) => {
				Cell code = StandardRuntime.Read(x);
				return evalClass.Eval(code, env);
			};
			Func<Cell, Cell, bool> AssertEqual = (Cell a, Cell b) => {
				if (a.ToString() != b.ToString()) {
					string message = string.Format("{0} != {1}", a, b);
					Console.WriteLine("Assertion failed: {0}", message);
					//Debug.Assert(false, message);
					failures++;
					return false;
				} else {
					success++;
					return true;
				}
			};

			// Core tests
			//if (true) goto unit;
			Console.WriteLine("Core tests=======");
			if (1 == 1) AssertEqual(EvalString(StandardRuntime.True.Value), StandardRuntime.True);
			if (1 == 1) AssertEqual(EvalString("1"), new Cell(1));
			if (1 == 1) AssertEqual(EvalCell(new Cell(new Cell[] { })), StandardRuntime.Nil);
			if (1 == 1) AssertEqual(EvalString("()"), StandardRuntime.Nil);
			if (1 == 1) AssertEqual(EvalString("(quote 1 2 3)"), new Cell(1));
			if (1 == 1) AssertEqual(EvalString("(define x 123)"), new Cell(123));
			if (1 == 1) AssertEqual(EvalString("x"), new Cell(123));
			if (1 == 1) AssertEqual(EvalString("(set! x 456)"), new Cell(456));
			if (1 == 1) AssertEqual(EvalString("x"), new Cell(456));
			if (1 == 1) AssertEqual(EvalString("(lambda (x y) (+ x y))"), new Cell("#Lambda((x y) (+ x y))"));
			if (1 == 1) AssertEqual(EvalString("(begin (define y 789) y)"), new Cell(789));
			if (1 == 1) AssertEqual(EvalString("(+ 1 2)"), new Cell(3));
			if (1 == 1)
				AssertEqual(EvalString("(begin (define add (lambda (x y) (+ x y))) (add 1 2))"), new Cell(3));
			if (1 == 1) AssertEqual(EvalString("(if (= 1 1) 1 0)"), new Cell("1"));
			if (1 == 1) AssertEqual(EvalString("(if (= 0 1) 0 1)"), new Cell("1"));
			if (1 == 1) AssertEqual(EvalString("(if (= 1 1) 1)"), new Cell("1"));
			if (1 == 1) AssertEqual(EvalString("(if (= 1 0) 1)"), StandardRuntime.Nil);
			if (1 == 1)
				AssertEqual(EvalString("(if (= 1 1) (+ 1 1) (- 1 1))"), new Cell(2));
			if (1 == 1)
				AssertEqual(EvalString("(if (= 1 1) (begin 1) (- 1 1))"), new Cell(1));

			unit:
			// Unit tests
			Console.WriteLine("Unit tests=======");
			Action<string, string> TEST = (string code, string expected) => {
				if (AssertEqual(EvalString(code), new Cell(expected))) {
					Console.WriteLine("PASS: {0} == {1}", code, expected);
				} else {
					Console.WriteLine("FAIL: {0}", code);
				}
			};
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
			TEST("(define combine (lambda (F) " +
				"(lambda (X Y) " +
				"(if (null? X) (quote ()) " +
				"(F (list (head X) (head Y)) " +
				"((combine F) (tail X) (tail Y)))))))", "#Lambda((F) (lambda (X Y) (if (null? X) (quote ()) (F (list (head X) (head Y)) ((combine F) (tail X) (tail Y))))))");
			TEST("(define zip (combine cons))", "#Lambda((X Y) (if (null? X) (quote ()) (F (list (head X) (head Y)) ((combine F) (tail X) (tail Y)))))");
			TEST("(zip (list 1 2 3 4) (list 5 6 7 8))", "((1 5) (2 6) (3 7) (4 8))");
			TEST("(define riff-shuffle (lambda (Deck) (begin " +
				"(define take (lambda (N Seq) (if (<= N 0) (quote ()) (cons (head Seq) (take (- N 1) (tail Seq)))))) " +
				"(define drop (lambda (N Seq) (if (<= N 0) Seq (drop (- N 1) (tail Seq))))) " +
				"(define mid (lambda (Seq) (/ (length Seq) 2))) " +
				"((combine append) (take (mid Deck) Deck) (drop (mid Deck) Deck)))))", "#Lambda((Deck) (begin (define take (lambda (N Seq) (if (<= N 0) (quote ()) (cons (head Seq) (take (- N 1) (tail Seq)))))) (define drop (lambda (N Seq) (if (<= N 0) Seq (drop (- N 1) (tail Seq))))) (define mid (lambda (Seq) (/ (length Seq) 2))) ((combine append) (take (mid Deck) Deck) (drop (mid Deck) Deck))))");
			TEST("(riff-shuffle (list 1 2 3 4 5 6 7 8))", "(1 5 2 6 3 7 4 8)");
			TEST("((repeat riff-shuffle) (list 1 2 3 4 5 6 7 8))", "(1 3 5 7 2 4 6 8)");
			TEST("(riff-shuffle (riff-shuffle (riff-shuffle (list 1 2 3 4 5 6 7 8))))", "(1 2 3 4 5 6 7 8)");

			Console.WriteLine("Macro tests=======");
			//TEST("(define do (macro (expr) (expr)))", "#Macro");
			TEST("(define abc 1)", "1");
			TEST("(define get! (macro (var) var)))", "#Macro((var) var)");
			TEST("(get! abc)", "1");
			TEST("(define incr (macro (var n) (list (quote set!) var (list (quote +) n (list (quote get!) var)))))", "#Macro((var n) (list (quote set!) var (list (quote +) n (list (quote get!) var))))");
			TEST("(incr abc 2)", "3");
			TEST("(get! abc)", "3");

			// Repositional end marker for specific unit testing
			goto end;

			end:
			if(display) {
				if (failures > 0)
					Console.WriteLine("TEST FAILURES OCCURRED");
				Console.WriteLine("{0} success, {1} failures", success, failures);
			}

			return new TestResults(success, failures);
		}
	}

	public class StandardEval : SchemeEval {
		protected uint stepCounter = 0;
		public override uint Steps => stepCounter;
		protected bool debug = false;
		public override bool IsDebug() => debug;
		public override void SetDebug(bool val) => debug = val;
		protected int depth = -1;

		public override Cell Eval(Cell Arg, SchemeEnvironment Env) {
			return Result = InternalEval(Arg, Env);
		}
		protected Cell InternalEval(Cell x, SchemeEnvironment env)
		{
			Cell original = x;
			++depth;
		recurse:
			if (debug) {
				if(original != x)
					Console.WriteLine("{0}) {1} => {2}", new string('-', depth), original, x);
				else
					Console.WriteLine("{0}) {1}", new string('-', depth), x);
			}
			++stepCounter;
			if (x.Type == CellType.SYMBOL) {
				x = env.Find(x.Value)[x.Value];
				goto done;
			}
			if (x.Type == CellType.NUMBER)
				goto done;
			if (x.ListValue.Count == 0) {
				x = StandardRuntime.Nil;
				goto done;
			}
			if (x.ListValue[0].Type == CellType.SYMBOL)
			{
				Cell sym = x.ListValue[0];
				switch((string)sym)
				{
					case "quote": // (quote exp)
						x = x.ListValue[1];
						goto done;
					case "if":    // (if test conseq [alt])
						Cell test = x.ListValue[1];
						Cell conseq = x.ListValue[2];
						Cell alt = StandardRuntime.Nil;
						if (x.ListValue.Count >= 4)
							alt = x.ListValue[3];
						Cell testval = Eval(test, env);
						Cell final = testval == StandardRuntime.False ? alt : conseq;
						x = final;
						goto recurse;
					case "set!":  // (set! var exp) - must exist
						x = env.Find(x.ListValue[1].Value)[x.ListValue[1].Value] = Eval(x.ListValue[2], env);
						goto done;
					case "define":// (define var exp) - creates new
						Cell b = Eval(x.ListValue[2], env);
						env.Insert(x.ListValue[1].Value, b);
						x = b;
						goto done;
					case "lambda":// (lambda (var*) exp)
						x.Type = CellType.LAMBDA;
						x.Environment = env;
						goto done;
					case "macro": // (macro (var*) exp)
						x.Type = CellType.MACRO;
						x.Environment = env;
						goto done;
					case "begin": // (begin exp*)
						for (int i = 1; i < x.ListValue.Count - 1; ++i)
							Eval(x.ListValue[i], env);
						// tail recurse
						x = x.ListValue.Last();
						goto recurse;
				}
			}
			// (proc exp*)
			Cell proc = Eval(x.ListValue[0], env);
			List<Cell> exps = new List<Cell>();
			if (proc.Type == CellType.MACRO) {
				exps = x.Tail().ListValue;
			} else { 
				for (int i = 1; i < x.ListValue.Count; ++i)
					exps.Add(Eval(x.ListValue[i], env));
			}
			if (proc.Type == CellType.LAMBDA) {
				env = new SchemeEnvironment(proc.ListValue[1].ListValue, exps, proc.Environment);
				x = proc.ListValue[2];
				goto recurse;
			} else if (proc.Type == CellType.MACRO) { 
				SchemeEnvironment env2 = new SchemeEnvironment(proc.ListValue[1].ListValue, exps, proc.Environment);
				x = Eval(proc.ListValue[2], env2);
				goto recurse;
			} else if (proc.Type == CellType.PROC) {
				x = proc.ProcValue(exps.ToArray());
				goto done;
			} else if (proc.Type == CellType.PROCENV) {
				x = proc.ProcEnvValue(exps.ToArray(), env);
				goto done;
			}

			throw new SchemeException("Invalid item in Eval");

		done:
			if(debug) {
				Console.WriteLine("{0}) {1} => {2} ", new string('-', depth), original, x);
			}
			--depth;
			return x;
		}
	}

	public class FrameEval : SchemeEval {
		protected static bool debugMode = true;
		public static bool DebugMode { get => debugMode; }
		public override bool IsDebug() => debugMode;
		public override void SetDebug(bool val) => debugMode = val;

		[Flags]
		public enum FrameStep : uint {
			NONE,
			ENTER,
			BUILTIN,
			BEGIN,
			IF_TEST,
			IF_DONE,
			DEFINE,
			PROC,
			EXPS,
			EXEC_PROC,
			EXEC_MACRO,
			SUBFRAME = 0x100,
			SUBFRAME_FIN = 0x200,
			DONE = 0x500
		}
		public class FrameState {
			public FrameStep Step { get; private set; }
			public Cell Result { get; private set; } = StandardRuntime.Nil;
			public FrameState Subframe { get; private set; } = null;
			public uint Steps { get; private set; } = 0;

			protected Cell X;
			protected Cell Env;
			protected Cell BeginCells;
			protected Cell Proc;
			protected Cell Exps;
			protected Cell ExpsIt;
			protected Cell Test, Conseq, Consalt;

			public readonly ulong Id;

			protected static ulong frameId = 0;
			public FrameState (Cell x, Cell env, FrameStep step = FrameStep.ENTER) {
				X = x;
				Env = env;
				Step = step;
				Id = ++frameId;
				if(DebugMode) Console.WriteLine("Frame.{0}({1})", Id, X);
			}
			~FrameState () {
				if (DebugMode) Console.WriteLine("~FrameState.{0} => {1}", Id, Result);
			}

			public bool IsSimple () { return X.Type == CellType.NUMBER || X.Type == CellType.STRING; }
			public bool IsDone() { return Step == FrameStep.DONE; }
			public void SingleStep () {
				if(Step.HasFlag(FrameStep.SUBFRAME)) {
					Subframe.SingleStep();
					if (!Subframe.IsDone())
						return;
					// Mark subframe finished
					Step &= ~FrameStep.SUBFRAME;
					Step |= FrameStep.SUBFRAME_FIN;
					// Housekeeping
					Steps += Subframe.Steps;
				}
				Steps++;
			Again:
				if(DebugMode) Console.WriteLine("Frame.{0}.Step = {1}", Id, Step);
				switch(Step) {
					case FrameStep.ENTER:
						if (IsSimple()) {
							Result = X;
							Step = FrameStep.DONE;
						} else if (X.Type == CellType.SYMBOL) {
							Result = Env.Environment.Lookup(X);
							Step = FrameStep.DONE;
						} else if (X.Empty()) {
							Result = StandardRuntime.Nil;
							Step = FrameStep.DONE;
						} else {
							Step = FrameStep.BUILTIN;
							goto Again;
						}
						break;

					case FrameStep.BUILTIN:
						if (X.Type != CellType.LIST) {
							Step = FrameStep.PROC;
							goto Again;
						} else {
							if (X.ListValue[0].Type != CellType.SYMBOL) {
								Step = FrameStep.PROC;
								goto Again;
							} else
								switch (X.ListValue[0].Value) {
									case "if":
										Step = FrameStep.IF_TEST | FrameStep.SUBFRAME;
										Subframe = new FrameState(X.ListValue[1], Env);
										break;

									case "define":
									case "set!":
										Step = FrameStep.DEFINE | FrameStep.SUBFRAME;
										Subframe = new FrameState(X.ListValue[2], Env);
										break;

									case "quote":
										Step = FrameStep.DONE;
										Result = X.ListValue[1];
										break;

									case "lambda":
										Step = FrameStep.DONE;
										X.Environment = Env.Environment;
										X.Type = CellType.LAMBDA;
										Result = X;
										break;

									case "macro":
										Step = FrameStep.DONE;
										X.Environment = Env.Environment;
										X.Type = CellType.MACRO;
										Result = X;
										break;

									case "begin":
										Step = FrameStep.BEGIN;
										BeginCells = X.Tail();
										break;

									default:
										Step = FrameStep.PROC;
										goto Again;
								}
						}
						break;

					case FrameStep.IF_TEST | FrameStep.SUBFRAME_FIN:
						Cell cons;
						if (Subframe.Result == StandardRuntime.True) {
							cons = X.Tail().Tail().Head();
						} else {
							cons = X.Tail().Tail().Tail().HeadOr(StandardRuntime.Nil);
						}
						// Tail recurse
						X = cons;
						Step = FrameStep.ENTER;
						break;

					case FrameStep.DEFINE | FrameStep.SUBFRAME_FIN:
						Result = Subframe.Result;
						if (X.ListValue[0].Value == "define")
							Env.Environment.Define(X.ListValue[1], Result);
						else if (X.ListValue[0].Value == "set!")
							Env.Environment.Set(X.ListValue[1], Result);
						else throw new NotImplementedException();
						Step = FrameStep.DONE;
						break;

					case FrameStep.BEGIN:
						Cell h = BeginCells.Head();
						if(BeginCells.ListValue.Count == 1) {
							// Tail recurse
							X = h;
							Step = FrameStep.ENTER;
							break;
						}
						BeginCells = BeginCells.Tail();
						Step = FrameStep.BEGIN | FrameStep.SUBFRAME;
						Subframe = new FrameState(h, Env);
						break;

					case FrameStep.BEGIN | FrameStep.SUBFRAME_FIN:
						Result = Subframe.Result;
						Step = BeginCells.Empty() ? FrameStep.DONE : FrameStep.BEGIN;
						break;

					case FrameStep.PROC:
						Step |= FrameStep.SUBFRAME;
						Subframe = new FrameState(X.Head(), Env);
						break;

					case FrameStep.PROC | FrameStep.SUBFRAME_FIN:
						Proc = Subframe.Result;
						if (Proc.Type == CellType.MACRO) {
							Exps = X.Tail();
							Step = FrameStep.EXEC_PROC;
						} else {
							Exps = new Cell(CellType.LIST);
							ExpsIt = X.Tail();
							if(ExpsIt.ListValue.Count == 0) {
								Step = FrameStep.EXEC_PROC;
							} else {
								// Have arguments to evaluate
								Step = FrameStep.EXPS | FrameStep.SUBFRAME;
								Subframe = new FrameState(ExpsIt.Head(), Env);
								ExpsIt = ExpsIt.Tail();
							}
						}
						break;

					case FrameStep.EXPS | FrameStep.SUBFRAME_FIN:
						Exps.ListValue.Add(Subframe.Result);
						if (ExpsIt.Empty())
							Step = FrameStep.EXEC_PROC;
						else {
							Step = FrameStep.EXPS | FrameStep.SUBFRAME;
							Subframe = new FrameState(ExpsIt.Head(), Env);
							ExpsIt = ExpsIt.Tail();
						}
						break;

					case FrameStep.EXEC_PROC:
						SchemeEnvironment parent, envNew;

						switch (Proc.Type) {
							case CellType.PROC:
								Result = Proc.ProcValue(Exps.ListValue.ToArray());
								Step = FrameStep.DONE;
								break;

							case CellType.PROCENV:
								Result = Proc.ProcEnvValue(Exps.ListValue.ToArray(), Env.Environment);
								Step = FrameStep.DONE;
								break;

							case CellType.LAMBDA:
								parent = Proc.Environment;
								envNew = new SchemeEnvironment(Proc.ListValue[1], Exps, parent);
								// Tail recurse
								Step = FrameStep.ENTER;
								Env = new Cell(envNew);
								X = Proc.ListValue[2];
								break;

							case CellType.MACRO:
								parent = Proc.Environment;
								envNew = new SchemeEnvironment(Proc.ListValue[1], Exps, parent);
								// Execute macro in envNew
								Step = FrameStep.EXEC_MACRO | FrameStep.SUBFRAME;
								Subframe = new FrameState(Proc.ListValue[2], new Cell(envNew));
								break;

							default:
								throw new SchemeException(string.Format("Cannot execute: {0} with arguments {1}", Proc, Exps));
						}
						break;

					case FrameStep.EXEC_MACRO | FrameStep.SUBFRAME_FIN:
						// Tail recurse with this result
						Step = FrameStep.ENTER;
						X = Subframe.Result;
						break;

					default:
						throw new SchemeException("Unimplemented step: " + Step.ToString());
				}
			}
		}

		protected uint stepCounter = 0;
		public override uint Steps => stepCounter;

		public override Cell Eval(Cell Arg, SchemeEnvironment Env) {
			return Result = InternalEval(Arg, Env);
		}

		protected Cell InternalEval(Cell Arg, SchemeEnvironment Env) { 
			FrameState state = new FrameState(Arg, new Cell(Env));
			while (!state.IsDone())
				state.SingleStep();
			stepCounter += state.Steps;
			return state.Result;
		}
	}

	public class StacklessFrameEval : FrameEval
	{
		public struct StacklessState
		{
			public Stack<FrameState> State { get; set; }
			public Cell Item { get; private set; }
			public Cell Environment { get; private set; }

			private bool finished;
			public bool Finished { get { return finished; } }
			private int steps;
			public int Steps { get { return steps; } }

			public StacklessState(Cell item, Cell env) {
				State = new Stack<FrameState>();
				Item = item;
				Environment = env;
				State.Push(new FrameState(item, env));
				finished = false;
				steps = 0;
			}

			public void SingleStep () {
				FrameStep step = FrameStep.NONE;
			}
		}

		public override Cell Eval(Cell Arg, SchemeEnvironment Env) {
			return internalEval(Arg, Env);
		}

		protected Cell internalEval(Cell Arg, SchemeEnvironment Env) {
			StacklessState state = new StacklessState(Arg, new Cell(Env));
			throw new NotImplementedException();
		}
	}
}
