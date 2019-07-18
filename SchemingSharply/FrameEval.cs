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
			public FrameStep Step;
			public Cell Result;
			public uint Steps { get; private set; }
			/// <summary>
			/// The current value being examined.
			/// </summary>
			public Cell X;
			/// <summary>
			/// Result of a subframe finishing.
			/// </summary>
			public Cell SubframeResult;
			/// <summary>
			/// The current environment.
			/// </summary>
			public Cell Env;
			/// <summary>
			/// Cells used in (begin ...) statement.
			/// </summary>
			public Cell BeginCells;
			/// <summary>
			/// Proc to be called.
			/// </summary>
			public Cell Proc;
			/// <summary>
			/// Expressions to pass to callable.
			/// </summary>
			public Cell Exps;
			/// <summary>
			/// Expressions iterator.
			/// </summary>
			public Cell ExpsIt;
			// Used in if statement tests
			public Cell Test, Conseq, Consalt;

			public readonly ulong Id;
			protected static ulong frameId = 0;
			public FrameState (Cell x, Cell env, FrameStep step = FrameStep.ENTER) {
				X = x;
				Env = env;
				Step = step;
				Id = ++frameId;
				//if(DebugMode) Console.WriteLine("Frame.{0}({1})", Id, X);
			}
			~FrameState () {
				//if (DebugMode) Console.WriteLine("~FrameState.{0} => {1}", Id, Result);
			}
		}

		public struct FrameEvalState {
			public Stack<FrameState> State;
			public Cell Result;
			public bool Complete;

			public FrameEvalState(FrameState initialState) {
				State = new Stack<FrameState>();
				State.Push(initialState);
				Result = StandardRuntime.Nil;
				Complete = false;
			}
		}

		protected uint stepCounter = 0;
		public override uint Steps => stepCounter;

		public override Cell Eval(Cell Arg, SchemeEnvironment Env) {
			return Result = InternalEval(Arg, Env);
		}

		public TaskState CreateTask(Cell Arg, SchemeEnvironment Env) {
			FrameState istate = new FrameState(Arg, new Cell(Env));
			FrameEvalState state = new FrameEvalState(istate);
			return new TaskState<FrameEvalState>(state);
		}
		protected TaskState<FrameEvalState> taskState(TaskState t) {
			return t.ToState<FrameEvalState>();
		}
		public bool TaskComplete(TaskState t) {
			return taskState(t).Finished;
		}
		public Cell TaskResult(TaskState t) {
			return taskState(t).Result;
		}

		protected bool IsSimple(Cell x) {
			switch(x.Type) {
				case CellType.STRING:
				case CellType.NUMBER:
					return true;
				default:
					return false;
			}
		}

		public void ExecuteTask(TaskState task, uint cycles = 10) {
			ExecuteTask(taskState(task));
		}
		public void ExecuteTask(TaskState<FrameEvalState> task, uint cycles = 1000) {
			bool done = false;
			if (task.State.Complete) return;
			do {
				FrameState fstate = task.State.State.Peek();
				switch (fstate.Step) {
					case FrameStep.ENTER:
						if (IsSimple(fstate.X)) {
							fstate.Result = fstate.X;
							fstate.Step = FrameStep.DONE;
						} else if (fstate.X.Type == CellType.SYMBOL) {
							fstate.Result = fstate.Env.Environment.Lookup(fstate.X);
							fstate.Step = FrameStep.DONE;
						} else if (fstate.X.Empty()) {
							fstate.Result = StandardRuntime.Nil;
							fstate.Step = FrameStep.DONE;
						} else {
							fstate.Step = FrameStep.BUILTIN;
							continue;
						}
						break;

					case FrameStep.BUILTIN:
						if (fstate.X.Type != CellType.LIST) {
							fstate.Step = FrameStep.PROC;
							continue;
						} else if (fstate.X.ListValue[0].Type != CellType.SYMBOL) {
							fstate.Step = FrameStep.PROC;
							continue;
						}
						switch(fstate.X.ListValue[0].Value) {
							case "if":
								fstate.Step = FrameStep.IF_TEST | FrameStep.SUBFRAME;
								task.State.State.Push(new FrameState(fstate.X.ListValue[1], fstate.Env));
								continue;

							case "define":
							case "set!":
								// Framestep DEFINE checks value of X to determine whether to invoke a
								// define or set!
								fstate.Step = FrameStep.DEFINE | FrameStep.SUBFRAME;
								task.State.State.Push(new FrameState(fstate.X.ListValue[2], fstate.Env));
								break;

							case "quote":
								fstate.Step = FrameStep.DONE;
								fstate.Result = fstate.X.ListValue[1];
								break;

							case "lambda":
								fstate.Step = FrameStep.DONE;
								fstate.X.Environment = fstate.Env.Environment;
								fstate.X.Type = CellType.LAMBDA;
								fstate.Result = fstate.X;
								break;

							case "macro":
								fstate.Step = FrameStep.DONE;
								fstate.X.Environment = fstate.Env.Environment;
								fstate.X.Type = CellType.MACRO;
								fstate.Result = fstate.X;
								break;

							case "begin":
								fstate.Step = FrameStep.BEGIN;
								fstate.BeginCells = fstate.X.Tail();
								break;

							default:
								fstate.Step = FrameStep.PROC;
								continue;
						}
						break;

					case FrameStep.IF_TEST | FrameStep.SUBFRAME_FIN:
						Cell cons;
						if (fstate.SubframeResult == StandardRuntime.False)
							cons = fstate.X.Tail().Tail().Tail().HeadOr(StandardRuntime.Nil);
						else
							cons = fstate.X.Tail().Tail().Head();
						// Tail recurse
						fstate.X = cons;
						fstate.Step = FrameStep.ENTER;
						break;

					case FrameStep.DEFINE | FrameStep.SUBFRAME_FIN:
						fstate.Result = fstate.SubframeResult;
						switch (fstate.X.ListValue[0].Value) {
							case "define":
								fstate.Env.Environment.Define(fstate.X.ListValue[1], fstate.Result);
								break;
							case "set!":
								fstate.Env.Environment.Set(fstate.X.ListValue[1], fstate.Result);
								break;
							default:
								throw new NotImplementedException();
						}
						fstate.Step = FrameStep.DONE;
						break;

					case FrameStep.BEGIN:
						Cell h = fstate.BeginCells.Head();
						if(fstate.BeginCells.ListValue.Count == 1) {
							// Tail recurse
							fstate.X = h;
							fstate.Step = FrameStep.ENTER;
							break;
						}
						fstate.BeginCells = fstate.BeginCells.Tail();
						fstate.Step = FrameStep.BEGIN | FrameStep.SUBFRAME;
						task.State.State.Push(new FrameState(h, fstate.Env));
						break;

					case FrameStep.BEGIN | FrameStep.SUBFRAME_FIN:
						fstate.Result = fstate.SubframeResult;
						fstate.Step = fstate.BeginCells.Empty() ? FrameStep.DONE : FrameStep.BEGIN;
						break;

					case FrameStep.PROC:
						fstate.Step |= FrameStep.SUBFRAME;
						task.State.State.Push(new FrameState(fstate.X.Head(), fstate.Env));
						break;

					case FrameStep.PROC | FrameStep.SUBFRAME_FIN:
						fstate.Proc = fstate.SubframeResult;
						if (fstate.Proc.Type == CellType.MACRO) {
							fstate.Exps = fstate.X.Tail();
							fstate.Step = FrameStep.EXEC_PROC;
						} else {
							fstate.Exps = new Cell(CellType.LIST);
							fstate.ExpsIt = fstate.X.Tail();
							if (fstate.ExpsIt.ListValue.Count == 0) {
								fstate.Step = FrameStep.EXEC_PROC;
							} else {
								// Have arguments to evaluate
								fstate.Step = FrameStep.EXPS | FrameStep.SUBFRAME;
								task.State.State.Push(new FrameState(fstate.ExpsIt.Head(), fstate.Env));
								fstate.ExpsIt = fstate.ExpsIt.Tail();
							}
						}
						break;

					case FrameStep.EXPS | FrameStep.SUBFRAME_FIN:
						fstate.Exps.ListValue.Add(fstate.SubframeResult);
						if (fstate.ExpsIt.Empty())
							fstate.Step = FrameStep.EXEC_PROC;
						else {
							fstate.Step = FrameStep.EXPS | FrameStep.SUBFRAME;
							task.State.State.Push(new FrameState(fstate.ExpsIt.Head(), fstate.Env));
							fstate.ExpsIt = fstate.ExpsIt.Tail();
						}
						break;

					case FrameStep.EXEC_PROC:
						SchemeEnvironment parent, envNew;
						switch(fstate.Proc.Type) {
							case CellType.PROC:
								fstate.Result = fstate.Proc.ProcValue(fstate.Exps.ListValue.ToArray());
								fstate.Step = FrameStep.DONE;
								break;

							case CellType.PROCENV:
								fstate.Result = fstate.Proc.ProcEnvValue(fstate.Exps.ListValue.ToArray(), fstate.Env.Environment);
								fstate.Step = FrameStep.DONE;
								break;

							case CellType.LAMBDA:
								parent = fstate.Proc.Environment;
								envNew = new SchemeEnvironment(fstate.Proc.ListValue[1], fstate.Exps, parent);
								// Tail recurse
								fstate.Step = FrameStep.ENTER;
								fstate.Env = new Cell(envNew);
								fstate.X = fstate.Proc.ListValue[2];
								break;

							case CellType.MACRO:
								parent = fstate.Proc.Environment;
								envNew = new SchemeEnvironment(fstate.Proc.ListValue[1], fstate.Exps, parent);
								// Execute macro in envNew
								fstate.Step = FrameStep.EXEC_MACRO | FrameStep.SUBFRAME;
								task.State.State.Push(new FrameState(fstate.Proc.ListValue[2], new Cell(envNew)));
								break;

							default:
								throw new SchemeException(string.Format("Cannot execute: {0} with arguments {1}", fstate.Proc, fstate.Exps));
						}
						break;

					case FrameStep.EXEC_MACRO | FrameStep.SUBFRAME_FIN:
						// Tail recurse with this result
						fstate.Step = FrameStep.ENTER;
						fstate.X = fstate.SubframeResult;
						break;

				}
				if(fstate.Step == FrameStep.DONE) {
					if (task.State.State.Count() > 1) {
						// Subframe finished
						FrameState subframe = task.State.State.Pop();
						fstate = task.State.State.Peek();
						fstate.SubframeResult = subframe.Result;
						// Mark subframe finished
						fstate.Step &= ~FrameStep.SUBFRAME;
						fstate.Step |= FrameStep.SUBFRAME_FIN;
					} else {
						// Call chain finished
						task.Result = fstate.Result;
						task.Finished = true;
					}
				}
				done = cycles-- == 0 || task.Finished;
			} while (!done);
		}

		protected Cell InternalEval(Cell Arg, SchemeEnvironment Env) {
			TaskState t = CreateTask(Arg, Env);
			do {
				ExecuteTask(t);
			} while (!TaskComplete(t));
			return TaskResult(t);
		}
	}
}
