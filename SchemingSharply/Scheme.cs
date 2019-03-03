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
		///   Cell is a pointer to a C# function, signature:
		///   delegate Cell CellProc(Cell[] args)
		/// </summary>
		PROC,
		/// <summary>
		///   Cell is a pointer to an environment
		/// </summary>
		ENVPTR
	}

	public interface IAbstractCell : IEnumerable<Cell>, IEnumerable<char>
	{
		int ToInteger();
	}

	public interface ICell : IAbstractCell
	{
		CellType Type { get; }
		string Value { get; }
		List<Cell> ListValue { get; }
		SchemeEnvironment Environment { get; set; }

		Cell Head();
		Cell Tail();
	}

	public struct Cell : ICell
	{
		public delegate Cell CellProc(Cell[] args);

		public static string NilValue = "#nil";
		public static string TrueValue = "#true";
		public static string FalseValue = "#false";

		public CellType Type { get; set; }
		public string Value { get; }
		public List<Cell> ListValue { get; }
		public CellProc ProcValue { get; }
		public SchemeEnvironment Environment { get; set; }

		public Cell(string value, CellType type = CellType.STRING)
		{
			Type = type;
			Value = value;
			ListValue = new List<Cell>();
			if (Value == null) Value = NilValue;
			ProcValue = null;
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
			Environment = null;
		}

		public Cell(CellProc proc)
		{
			Type = CellType.PROC;
			Value = "";
			ListValue = new List<Cell>();
			ProcValue = proc;
			Environment = null;
		}

		public Cell(CellType type)
		{
			Type = type;
			Value = "";
			ListValue = new List<Cell>();
			ProcValue = null;
			Environment = null;
		}

		public Cell(SchemeEnvironment envptr) {
			Type = CellType.ENVPTR;
			Value = "";
			ListValue = new List<Cell>();
			ProcValue = null;
			Environment = envptr;
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

		public Cell Tail()
		{
			if (ListValue.Count == 0)
				return new Cell(CellType.LIST);
			return new Cell(ListValue.Skip(1));
		}

		private static string listToString(List<Cell> cells)
		{
			var cells2 = cells.ConvertAll((c) => (string)c);
			return "(" + string.Join(" ", cells2) + ")";
		}

		public IEnumerator<Cell> GetEnumerator() => ListValue.GetEnumerator();
		IEnumerator<char> IEnumerable<char>.GetEnumerator() => Value.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => ListValue.GetEnumerator();

		public static explicit operator string(Cell c)
		{
			switch (c.Type) {
				case CellType.STRING:
				case CellType.NUMBER:
					return c.Value;
				case CellType.PROC:
					return "#Proc";
				case CellType.LIST:
					return listToString(c.ListValue);
				case CellType.LAMBDA:
					string r = "#Lambda(";
					r += listToString(c.ListValue[1].ListValue);
					r += " " + listToString(c.ListValue[2].ListValue);
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
			else if (a.Type == CellType.STRING)
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

		public static bool operator !=(Cell a, Cell b)
		{
			return !(a == b);
		}

		public static bool operator ==(Cell a, Cell b)
		{
			switch(a.Type)
			{
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

	public class CellNameNotFound : Exception
	{
		public CellNameNotFound(string name)
			: base(name) { }
	}

	public class SchemeEnvironment
	{
		protected Dictionary<string, Cell> map = new Dictionary<string, Cell>();
		protected SchemeEnvironment outer;

		public SchemeEnvironment(SchemeEnvironment Outer = null)
		{
			outer = Outer;
		}

		public SchemeEnvironment(List<Cell> keys, List<Cell> values, SchemeEnvironment Outer)
		{
			outer = Outer;
			for (int i = 0; i < keys.Count; ++i)
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

		public Cell Lookup(Cell key)
		{
			return Find(key.Value)[key.Value];
		}

		public Cell Set(Cell key, Cell value)
		{
			return Find(key.Value)[key.Value] = value;
		}

		public Cell Define(Cell key, Cell value)
		{
			map.Add(key.Value, value);
			return value;
		}

		public override string ToString() {
			var parts = map.Keys.Select(k => k + ": " + map[k].ToString()).ToList();
			if (outer != null)
				parts.Add(outer.ToString());
			return "#Env{ " + string.Join(", ", parts) + "}";
		}
	}

	public struct ProcessState
	{
		public List<Cell> Stack;
		public int[] Code;
		public string[] Data;

		// State
		public int SP, BP;
		public Cell Accumulator;

		public ProcessState (int[] code, string[] data)
		{
			Stack = new List<Cell>();
			Code = code;
			Data = data;
			SP = BP = 0;
			Accumulator = new Cell();
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
			return (args[0] == args[1] || (int)args[0] < (int)args[1]) ?
				True : False;
		}

		public static Cell Print(Cell[] args)
		{
			var cellStrings = args.ToList().ConvertAll<string>((c) => (string)c);
			System.Console.WriteLine(String.Join(" ", cellStrings));
			return Nil;
		}

		public static void AddGlobals(SchemeEnvironment e)
		{
			e.Insert(StandardRuntime.False.Value, StandardRuntime.False);
			e.Insert(StandardRuntime.True.Value, StandardRuntime.True);
			e.Insert(StandardRuntime.Nil.Value, StandardRuntime.Nil);
			e.Insert("+", new Cell(Plus)); e.Insert("-", new Cell(Minus));
			e.Insert("*", new Cell(Multiply));
			e.Insert("<", new Cell(LessThan)); e.Insert("<=", new Cell(LessThanEqual));
			e.Insert("=", new Cell(Equal)); e.Insert("==", new Cell(Equal));
			e.Insert("print", new Cell(Print));
		}

		public static List<string> Tokenise (string str)
		{
			List<string> tokens = new List<string>();
			int s = 0;
			while(s < str.Length)
			{
				while (Char.IsWhiteSpace(str[s]))
					++s;
				if (str[s] == '(' || str[s] == ')')
					tokens.Add(str[s++] == '(' ? "(" : ")");
				else
				{
					int t = s;
					while (t < str.Length && !Char.IsWhiteSpace(str[t])
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
			isNumber = Char.IsDigit(token[0]);
			if (token.Length > 1)
				isNumber = isNumber || (token[0] == '-' && Char.IsDigit(token[1]));
			if (isNumber)
				return new Cell(token, CellType.NUMBER);
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
				}
				tokens.RemoveAt(0);
				return new Cell(cells);
			}
			else
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
	}

	public class SchemeException : Exception
	{
		public SchemeException(string message) : base(message) { }
	}
	public class StandardEval : ISchemeEval
	{
		public Cell Eval(Cell x, SchemeEnvironment env)
		{
			if (x.Type == CellType.STRING)
				return env.Find(x.Value)[x.Value];
			if (x.Type == CellType.NUMBER)
				return x;
			if (x.ListValue.Count == 0)
				return StandardRuntime.Nil;
			if (x.ListValue[0].Type == CellType.STRING)
			{
				Cell sym = x.ListValue[0];
				switch((string)sym)
				{
					case "quote": // (quote exp)
						return x.ListValue[1];
					case "if":    // (if test conseq [alt])
						Cell test = x.ListValue[1];
						Cell conseq = x.ListValue[2];
						Cell alt = StandardRuntime.Nil;
						if (x.ListValue.Count >= 4)
							alt = x.ListValue[3];
						Cell testval = Eval(test, env);
						Cell final = testval == StandardRuntime.False ? alt : conseq;
						return Eval(final, env);
					case "set!":  // (set! var exp) - must exist
						return env.Find(x.ListValue[1].Value)[x.ListValue[1].Value] = Eval(x.ListValue[2], env);
					case "define":// (define var exp) - creates new
						Cell b = Eval(x.ListValue[2], env);
						env.Insert(x.ListValue[1].Value, b);
						return b;
					case "lambda":// (lambda (var*) exp)
						x.Type = CellType.LAMBDA;
						x.Environment = env;
						return x;
					case "begin": // (begin exp*)
						for (int i = 1; i < x.ListValue.Count - 1; ++i)
							Eval(x.ListValue[i], env);
						return Eval(x.ListValue.Last(), env);
				}
			}
			// (proc exp*)
			Cell proc = Eval(x.ListValue[0], env);
			List<Cell> exps = new List<Cell>();
			for (int i = 1; i < x.ListValue.Count; ++i)
				exps.Add(Eval(x.ListValue[i], env));
			if (proc.Type == CellType.LAMBDA)
			{
				SchemeEnvironment env2 = new SchemeEnvironment(proc.ListValue[1].ListValue, exps, proc.Environment);
				return Eval(proc.ListValue[2], env2);
			} else if(proc.Type == CellType.PROC)
			{
				return proc.ProcValue(exps.ToArray());
			}

			throw new SchemeException("Invalid item in Eval");
		}
	}
}
