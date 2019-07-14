using SchemingSharply.Scheme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemingSharply
{
	public enum TaskWaitState
	{
		/// <summary>
		/// Not waiting.
		/// </summary>
		NONE,
	}

	public interface ITaskMachineTask
	{
		void Init(Cell item, SchemeEnvironment environment);
		void Loop(uint iterations = TaskMachine.Iterations);
		bool Finished { get; }
		uint TaskId { get; }
		int Priority { get; }
		int PriorityLevel { get; set; }
		Cell Result { get; }
		string Title { get; }
	}

	public abstract class TaskMachineTask : ITaskMachineTask
	{
		public const int PRI_DEFAULT = 20;
		public const int PRI_IDLE = int.MaxValue;
		public const int PRI_RUN = -20;
		public const int PRI_HIGHEST = int.MinValue;

		public Cell Item { get; private set; }
		public SchemeEnvironment Environment { get; private set; }
		public Cell Result { get; protected set; }
		public uint TaskId { get; private set; }
		public TaskWaitState WaitState = TaskWaitState.NONE;
		public int Priority { get; private set; }
		public int PriorityLevel { get; set; }
		public string Title { get; set; }

		public TaskMachineTask(string title, uint taskId, int priority = PRI_DEFAULT) {
			Title = title;
			TaskId = taskId;
			Priority = PriorityLevel = priority;
		}

		public virtual void Init(Cell item, SchemeEnvironment env) {
			Item = item;
			Environment = env;
		}
		public abstract void Loop(uint iterations = TaskMachine.Iterations);
		public abstract bool Finished { get; }
	}

	public class ClassicTask : TaskMachineTask
	{
		public ClassicTask(uint taskId)
			: base(taskId.ToString() + ".ClassicTask", taskId) {
		}

		public ClassicTask(string title, uint taskId)
			: base(title, taskId) {
		}

		public override void Loop(uint iterations = TaskMachine.Iterations) {
			StandardEval eval = new StandardEval();
			Result = eval.Eval(Item, Environment);
		}

		public override bool Finished => true;
	}

	public interface ITaskState {
		bool Finished { get; }
		Cell Result { get; }
	}

	public class TaskState : ITaskState {
		public bool Finished { get; set; }
		public Cell Result { get; set; }
		public TaskState<StateType> ToState<StateType> () {
			return this as TaskState<StateType>;
		}
	}

	public class TaskState<StateType> : TaskState {
		public StateType State { get; set; }
		public TaskState(StateType state) {
			State = state;
		}
	}

	public class FrameTask : TaskMachineTask
	{
		FrameEval.FrameState state;

		public FrameTask(uint taskId)
			: base(taskId.ToString() + ".FrameTask", taskId) {
		}

		public FrameTask(string title, uint taskId)
			: base(title, taskId) {
		}

		public override void Init(Cell item, SchemeEnvironment env) {
			base.Init(item, env);
			state = new FrameEval.FrameState(Item, new Cell(Environment));
		}

		public override void Loop(uint iterations = TaskMachine.Iterations) {
			throw new NotImplementedException();
		}

		public override bool Finished => throw new NotImplementedException();
	}

	public class CellMachineTask : TaskMachineTask
	{
		CellMachine.CodeResult cr = CellMachine.CellMachineEval.GetCodeResult("Eval.asm");
		CellMachine.Machine machine;

		public CellMachineTask(uint taskId)
			: base(taskId.ToString() + ".CellTask", taskId) {
		}

		public CellMachineTask(string title, uint taskId)
			: base(title, taskId) {
		}

		public override void Init(Cell item, SchemeEnvironment env) {
			base.Init(item, env);
			machine = new CellMachine.Machine(cr, new Cell[] { Item, new Cell(Environment) });
		}

		public override void Loop(uint iterations = TaskMachine.Iterations) {
			while (!Finished && iterations-- > 0)
				machine.Step();
			Result = machine.A;
		}

		public override bool Finished => machine.Finished;
	}

	public class IdleTask : TaskMachineTask
	{
		public IdleTask (uint taskId)
			: base(taskId.ToString() + ".Idle", taskId, PRI_IDLE) { }

		public IdleTask (string title, uint TaskId)
			: base(title, TaskId, PRI_IDLE) { }

		public override bool Finished => false;
		public override void Loop(uint iterations = TaskMachine.Iterations) {

		}
	}

	public interface ITaskMachine
	{
		void Loop();
	}

	public class TaskMachine : ITaskMachine
	{
		public const uint Iterations = 100;
		protected List<TaskMachineTask> Tasks = new List<TaskMachineTask>();
		protected int TaskId = 0;
		protected uint TaskIdCounter = 0;

		public delegate void AwakeDelegate();
		public delegate void TaskCompleteDelegate(TaskMachineTask task);
		public delegate void TaskRemoveDelegate(TaskMachineTask task);
		/// <summary>
		/// Delegate to handle the act of the task machine sleeping.
		/// </summary>
		/// <param name="sleeptime">The desired time to sleep.</param>
		/// <returns>The number of milliseconds to sleep for.</returns>
		public delegate int SleepDelegate(int sleeptime);
		public AwakeDelegate OnAwaken;
		public TaskCompleteDelegate OnComplete;
		public TaskRemoveDelegate OnRemove;
		/// <summary>
		/// Fires when the task machine wants to sleep.
		/// </summary>
		public SleepDelegate OnSleep;

		protected TaskMachineTask FindIdle() {
			foreach (var task in Tasks)
				if (task.GetType() == typeof(IdleTask))
					return task;
			throw new Exception("Idle task not found");
		}

		protected bool TaskRunnable(TaskMachineTask t) {
			if (t.WaitState == TaskWaitState.NONE)
				return true;

			return false;
		}
		protected TaskMachineTask FindNextTask(int from) {
			int t = from + 1;
			do {
				TaskMachineTask task = Tasks[t % Tasks.Count];
				if (TaskRunnable(task))
					return task;
				t = ++t % Tasks.Count;
			} while (t != from);

			return null;
		}

		public void Loop() {
		}

		protected bool RunTasks() { 
			if (Tasks.Count == 0) {
				return false;
			}

			// Find runnable task
			TaskMachineTask t;
			int prevTaskId = TaskId;
			bool runnable = false;
			do {
				t = Tasks[TaskId];
				TaskId = (TaskId + 1) % Tasks.Count;
				if (TaskId == prevTaskId)
					break;

				if (--t.PriorityLevel <= TaskMachineTask.PRI_RUN)
					t.PriorityLevel = t.Priority;
				else
					continue; // Skip below check

				runnable = TaskRunnable(t);
			} while (!runnable);

			if (!runnable)
				return false;

			t.Loop();

			if (t.Finished) {
				OnComplete(t);
				lock (Tasks) {
					Tasks.RemoveAt(TaskId);
					OnRemove(t);
					TaskId = TaskId % Tasks.Count; // clamp
				}
			}

			return true;
		}

		public void Sleep(int milliseconds = 1) {
			System.Threading.Thread.Sleep(milliseconds);
			OnAwaken();
		}
	}
}
