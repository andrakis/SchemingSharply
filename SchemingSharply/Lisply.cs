using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemingSharply {
	public enum AbstractCellType {
		NUMBER,
		SYMBOL,
		STRING,
		LIST,
		LAMBDA,
		MACRO,
		PROC,
		PROCENV
	}

	public interface IAbstractEnvironment {

	}
	public interface IAbstractCell {
		AbstractCellType Type { get; }
		string StringValue { get; }
		List<IAbstractCell> ListValue { get; }
		int IntValue { get; }
		long LongValue { get; }
		float FloatValue { get; }
		double DoubleValue { get; }
		decimal DecimalValue { get; }
		T Cast<T>();

		Func<IAbstractCell[], IAbstractCell> ProcValue { get; }
		Func<IAbstractCell[], IAbstractEnvironment, IAbstractCell> ProcEnvValue { get; }
	}
	namespace Lisply {
		public abstract class AbstractCell : IAbstractCell {
			public abstract T Cast<T>();
		}
		public abstract class AbstractedCell<TCell> : AbstractCell {
			protected TCell CellValue = default(TCell);

			public virtual T Cast<T> () {
				if (typeof(T) == typeof(TCell))
					return (T)CellValue;

			}
		}
}
