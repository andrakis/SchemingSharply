using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemingSharply.Scheme {
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
