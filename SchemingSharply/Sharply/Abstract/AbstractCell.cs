using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SchemingSharply.Sharply {
	public enum CellType : int {
		Number,
		String,
		List,
		Dict,
		Proc,
		ProcExt,
		Lambda,
		Macro,
	}
}

namespace SchemingSharply.Sharply.VeryAbstract {
	public interface IAbstractCell {
		string ToString();
	}

	public abstract class VeryAbstractCell<TNumber,TString,TList,TDict,TProc,TProcExt> {
		protected TNumber numberValue;
		protected TString stringValue;
		protected TList listValue;
		protected TDict dictValue;
		protected TProc procValue;
		protected TProcExt procExtValue;
	}
}

namespace SchemingSharply.Sharply.Abstract {
	//public abstract class AbstractCell : VeryAbstract.VeryAbstractCell<decimal,string,List<AbstractCell>,Dictionary<string,AbstractCell>,>
	//{
	//}
}
