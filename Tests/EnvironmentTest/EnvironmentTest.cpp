// EnvironmentTest.cpp : 

#include <iostream>
#include <map>
#include <memory>
#include <stdexcept>
#include <string>

// Forward declarations
class Cell;
class StringCell;
class Atom;

// Standard types
typedef unsigned AtomId;

// Macros
// Because VC++ complains about ignoring throw(...) sections
#if _MSC_VER
#define THROW(x) throw(...)
#else
#define THROW(x) throw(x)
#endif

// Standard exceptions
class OurException : public std::exception {
public:
#if _MSC_VER
	OurException(std::string message) : std::exception(message.c_str()) { }

#else
	std::string msg;
	OurException(std::string message) : msg(message) { }
	const char *what() const  _GLIBCXX_TXN_SAFE_DYN _GLIBCXX_USE_NOEXCEPT { return msg.c_str(); }
#endif
};

class Cell {
public:
	enum CellType {
		NIL,
		ATOM,
		STRING
	};
	Cell(CellType celltype = NIL) : _type(celltype) { }
	virtual ~Cell() { }
	virtual AtomId atomId() const THROW(OurException) { return 0; }
	virtual std::string str() const { return "#nil"; }
	CellType type() const { return _type; }
protected:
	CellType _type;
};

class StringCell : public Cell {
	std::string _str;
public:
	StringCell(std::string strval) : Cell(Cell::STRING), _str(strval) {}
	AtomId atomId() const THROW(OurException) { throw new OurException("String => atom invalid"); }
	std::string str() const { return _str; }
};

class Atom : public Cell {
	AtomId _atomId;

public:
	Atom() : Cell(Cell::ATOM), _atomId(0) { }
	Atom(AtomId id) : Cell(Cell::ATOM), _atomId(id) { }
	Atom(const Atom &copy) : Cell(Cell::ATOM), _atomId(copy.atomId()) { }
	AtomId atomId() const THROW(OurException) { return _atomId; }
	std::string str() const { return std::to_string(_atomId); }
};

// Atoms
namespace atoms {
	typedef std::map<AtomId, Atom> AtomMap;
	typedef std::map<std::string, AtomId> AtomMapStr;
	AtomMap _atomMap;
	AtomMapStr _atomMapStr;
	AtomId _atomCounter = 0;

	std::string AtomIdToStr(AtomId id) THROW(OurException) {
		for (auto it = _atomMapStr.cbegin(); it != _atomMapStr.cend(); ++it)
			if (it->second == id)
				return it->first;
		throw new OurException("atomid " + std::to_string(id) + " not found");
	}
	bool Exists(AtomId id) {
		return (_atomMap.find(id) != _atomMap.end());
	}
	bool Exists(const char *name) {
		return (_atomMapStr.find(name) != _atomMapStr.end());
	}
	Atom Get(AtomId id) THROW(OurException) {
		auto it = _atomMap.find(id);
		if(it == _atomMap.end())
			throw new OurException("atomid " + std::to_string(id) + " not found");
		return it->second;
	}
	Atom Get(const char *name) THROW(OurException) {
		auto it = _atomMapStr.find(name);
		if (it == _atomMapStr.end())
			throw new OurException("atomsymbol " + std::string(name) + " not found");
		return it->second;
	}
	Atom Declare(const char *name) {
		if (Exists(name))
			return Get(name);
		Atom atom(++_atomCounter);
		_atomMap.emplace(atom.atomId(), atom);
		_atomMapStr.emplace(name, atom.atomId());
		return atom;
	}
	void Initialise() {
		Declare("#nil");
	}
}

class Environment {
protected:
	template<typename T> friend class MappedEnvironment;
	typedef std::shared_ptr<Environment> OuterPtr;
	typedef std::map<std::string, Cell> StringMap;
	typedef std::map<AtomId, Cell> AtomMap;

	OuterPtr _outer;

	Environment(OuterPtr outer = nullptr) : _outer(outer) { }
public:
	virtual ~Environment() { }

	virtual Cell Lookup(const char *key) const THROW(OurException) = 0;
	virtual Cell Lookup(AtomId key) const THROW(OurException) = 0;
	virtual void Set(const char *key, const Cell &value) = 0;
	virtual void Set(AtomId key, const Cell &value) = 0;
	virtual void Define(const char *key, const Cell &value) = 0;
	virtual void Define(AtomId key, const Cell &value) = 0;
};

template<typename Key>
class MappedEnvironment : public Environment {
protected:
	friend Environment;
	typedef Key KeyType;
	typedef std::map<KeyType, Cell> MapType;
	typedef typename MapType::iterator Iterator;
	typedef typename MapType::const_iterator ConstIterator;

	MapType _map;

	// Subenvironment to fillout:
	virtual KeyType strtokey(const char *str) const = 0;
	virtual std::string keytostr(KeyType key) const = 0;
	virtual AtomId keytoatom(KeyType key) const = 0;

	bool existsLocal(KeyType key) const {
		return _map.find(key) != _map.end();
	}

	Cell lookupLocal(KeyType key) const THROW(OurException) {
		auto it = _map.find(key);
		if (it == _map.end())
			throw new OurException("Cell not found");
		return it->second;
	}

	void declareLocal(KeyType key, Cell value) {
		_map.emplace(key, value);
	}

	MappedEnvironment() : Environment(), _map() { }
	MappedEnvironment(OuterPtr outer) : Environment(outer), _map() { }
public:
};

class StringEnvironment : public MappedEnvironment<std::string> {
protected:
public:
	StringEnvironment() : MappedEnvironment() {}
	StringEnvironment(OuterPtr outer) : MappedEnvironment(outer) {}

	Cell Lookup(const char *key) const THROW(OurException) {
		if (existsLocal(key))
			return lookupLocal(key);
		if (_outer != nullptr)
			return _outer->Lookup(key);
		throw new OurException("Key not found");
	}
	Cell Lookup(AtomId key) const THROW(OurException) {
		return Lookup(atoms::AtomIdToStr(key).c_str());
	}
	void Set(const char *key, const Cell &value) {
		if (existsLocal(key)) {
			declareLocal(key, value);
		}
		if (_outer != nullptr)
			return _outer->Set(key, value);
		declareLocal(key, value);
	}
	void Set(AtomId key, const Cell &value) {
		Set(atoms::AtomIdToStr(key).c_str(), value);
	}
	void Define(const char *key, const Cell &value) {
		declareLocal(key, value);
	}
	void Define(AtomId key, const Cell &value) {
		Define(atoms::AtomIdToStr(key).c_str(), value);
	}
};

class AtomEnvironment : public MappedEnvironment<AtomId> {
private:
protected:
public:
	AtomEnvironment() : MappedEnvironment() { }
	AtomEnvironment(OuterPtr outer) : MappedEnvironment(outer) { }
};


int main()
{
	atoms::Initialise();

	return 0;
}

