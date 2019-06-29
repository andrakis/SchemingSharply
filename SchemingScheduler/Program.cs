using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemingScheduler {
	namespace Scheduler {
		public interface ITaskId {
			byte GlobalId { get; }
			byte NodeId { get; }
			uint ProcessId { get; }
		}
		public struct TaskId : ITaskId {
			public byte GlobalId { get; set; }
			public byte NodeId { get; set; }
			public uint ProcessId { get; set; }
			public TaskId (byte global, byte node, uint process) {
				GlobalId = global;
				NodeId = node;
				ProcessId = process;
			}
		}
		public interface ITaskMessage {
			ITaskId FromTaskId { get; }
			ITaskId ToTaskId { get; }
		}
		public interface ITaskMessage<MessageType> : ITaskMessage {
			MessageType Message { get; }
		}
		public struct TaskMessage<MessageType> : ITaskMessage<MessageType> {
			public ITaskId FromTaskId { get; set; }
			public ITaskId ToTaskId { get; set; }
			public MessageType Message { get; set; }

			public TaskMessage(ITaskId from, ITaskId to, MessageType message) {
				FromTaskId = from;
				ToTaskId = to;
				Message = message;
			}
		}

		public class SimpleScheduler : ITaskId {
			public ConcurrentQueue<ITaskMessage> MessageQueue = new ConcurrentQueue<ITaskMessage>();
			public byte GlobalId { get; protected set; }
			public byte NodeId { get; protected set; }
			public uint ProcessId { get; protected set; }

			public SimpleScheduler (byte globalId = 0, byte localNodeId = 0) {
				GlobalId = globalId;
				NodeId = localNodeId;
				if (NodeId == 0)
					NodeId = (byte)Process.GetCurrentProcess().ProcessorAffinity;
				Process.GetCurrentProcess().ProcessorAffinity = (IntPtr)NodeId;
				ProcessId = 0;
			}


		}

	}
	class Program {
		static void Main(string[] args) {
			int cpuCount = Environment.ProcessorCount;
			Console.WriteLine("Starting {0} schedulers...", cpuCount);
			for(int i = 0; i < cpuCount; ++i) {
			}

			Console.WriteLine("Press [ENTER] to exit");
			Console.ReadLine();
		}
	}
}
