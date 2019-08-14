// Cell.cpp
//
// Testing a better Cell implementation

#include <exception>
#include <iostream>
#include <memory>
#include <sstream>
#include <string>
#include <string.h>
#include <vector>

// Macros
// Because VC++ complains about ignoring throw(...) sections
#if _MSC_VER
#define THROW(x) throw(...)
#else
#define THROW(x) throw(x)
#endif

// Standard exceptions
class Exception : public std::exception {
public:
#if _MSC_VER
	Exception(std::string message) : std::exception(message.c_str()) { }
#else
	std::string msg;
	Exception(std::string message) : msg(message) { }
	const char *what() const  _GLIBCXX_TXN_SAFE_DYN _GLIBCXX_USE_NOEXCEPT { return msg.c_str(); }
#endif
};

#define EXCEPT	THROW(Exception)

struct Environment {
};
typedef std::shared_ptr<Environment> EnvPtr;

enum CellType {
	INTEGER,
	FLOAT,
	STRING,
	LIST,
	LAMBDA,
	MACRO,
	ENVPTR
};

class Cell;
typedef std::vector<Cell> ListType;
typedef long IntType;
typedef double FloatType;
typedef std::string StringType;

template<typename T> T parseString(std::string s) {
	T i;
	std::istringstream(s) >> i;
	return i;
}


class Cell {
	CellType _type;
	union {
		StringType _stringval;
		IntType _intval;
		FloatType _floatval;
		ListType _listval;
		EnvPtr _envptr;
	};
	void assign(const Cell &other) {
		reset(_type);
		_type = other._type;
		switch (_type) {
			case INTEGER: _intval = other._intval; break;
			case FLOAT: _floatval = other._floatval; break;
			case STRING: _stringval = other._stringval; break;
			case LAMBDA:
			case MACRO:
			case LIST:
				_listval = other._listval; break;
			case ENVPTR: _envptr = other._envptr; break;
		}
	}
	void reset(CellType oldtype = INTEGER) {
		destruct(oldtype);
		switch (_type) {
			case INTEGER: _intval = 0; break;
			case FLOAT: _floatval = 0; break;
			case STRING: _stringval = ""; break;
			case LAMBDA:
			case MACRO:
			case LIST:
				_listval = ListType();
				break;
			case ENVPTR:
				_envptr = EnvPtr();
				break;
		}
	}
	bool is_listish() const {
		switch (_type) {
			case LIST:
			case LAMBDA:
			case MACRO:
				return true;
			default:
				return false;
		}
	}
	template<typename T = ListType> T single_list(const Cell &c) const {
		T list;
		list.push_back(c);
		return list;
	}
public:
	Cell(IntType value = 0) : _type(INTEGER), _intval(value) {}
	Cell(FloatType value) : _type(FLOAT), _floatval(value) {}
	Cell(StringType value) : _type(STRING), _stringval(value) {}
	Cell(CellType type) : _type(type) {
		reset();
	}
	Cell(ListType value) : _type(LIST), _listval(value) {}
	Cell(ListType::const_iterator begin, ListType::const_iterator end) : _type(LIST), _listval(begin, end) {}
	Cell(const Cell &other) : _type(other.type()) {
		assign(other);
	}
	~Cell() {
		destruct(_type);
	}
	void destruct (CellType type) {
		switch (type) {
			case LAMBDA:
			case MACRO:
			case LIST: _listval.~vector(); break;
			case ENVPTR: _envptr.~shared_ptr(); break;
		}
	}
	Cell& operator= (Cell other) {
		assign(other);
		return *this;
	}

	IntType intval() const EXCEPT {
		switch (_type) {
			case INTEGER: return _intval;
			case FLOAT: return (IntType)_floatval;
			case STRING: return parseString<IntType>(_stringval);
			default: throw Exception("Cannot coerce to integer");
		}
	}
	FloatType floatval() const EXCEPT {
		switch (_type) {
			case INTEGER: return (FloatType)_intval;
			case FLOAT: return _floatval;
			case STRING: return parseString<FloatType>(_stringval);
			default: throw Exception("Cannot coerce to float");
		}
	}
	ListType listval() const EXCEPT {
		switch (_type) {
			case INTEGER:
			case FLOAT:
			case STRING:
				return single_list(*this);
			case LIST: 
			case LAMBDA:
			case MACRO:
				return _listval;
			default: throw Exception("Cannot coerce to list");
		}
	}
	EnvPtr envptr() const EXCEPT {
		if (_type != ENVPTR) throw Exception("Not an environment pointer");
		return _envptr;
	}

	Cell operator+ (const Cell &other) const EXCEPT {
		Cell val(*this);
		switch (_type) {
			case INTEGER: val._intval += other.intval(); break;
			case FLOAT: val._floatval += other.floatval(); break;
			case STRING: val._stringval += other.str(); break;
			case LIST:
			{
				const ListType &ol = other.listval();
				val._listval.insert(val._listval.end(), ol.begin(), ol.end());
				break;
			}
			default: throw Exception("Cannot + the given items");
		}
		return val;
	}

	CellType type() const { return _type; }
	StringType str() const {
		std::stringstream ss;
		switch (_type) {
			case INTEGER: return std::to_string(_intval);
			case FLOAT: return std::to_string(_floatval);
			case STRING: return _stringval;
			case ENVPTR: return "<EnvPtr>";
			case LAMBDA:
			case MACRO:
			case LIST: {
				if (_type == LAMBDA) ss << "(#Lambda ";
				else if (_type == MACRO) ss << "(#Macro ";
				else ss << "(";
				bool first = true;
				for (auto it = _listval.cbegin(); it != _listval.cend(); ++it) {
					if (first)
						first = false;
					else
						ss << " ";
					ss << it->str();
				}
				ss << ")";
				return ss.str();
			}
			default: return "<!!!!>";
		}
	}

	Cell head() const EXCEPT {
		if (!is_listish()) throw Exception("Not a list type");
		if (_listval.empty()) return Cell("#nil");
		return _listval.front();
	}
	Cell tail() const EXCEPT {
		if (!is_listish()) throw Exception("Not a list type");
		if (_listval.empty()) return *this;
		return Cell(_listval.cbegin() + 1, _listval.cend());
	}

	size_t size() const EXCEPT {
		switch (_type) {
			case INTEGER: return sizeof(IntType);
			case FLOAT: return sizeof(FloatType);
			case STRING: return _stringval.size();
			case LAMBDA:
			case MACRO:
			case LIST:
				return _listval.size();
			case ENVPTR:
				return sizeof(EnvPtr);
			default:
				return 0;
		}
	}
	bool empty() const EXCEPT {
		if (is_listish()) return _listval.empty();
		throw Exception("Not a valid type for empty()");
	}
};

std::ostream &operator<<(std::ostream &os, const Cell &cell) {
	os << cell.str();
	return os;
}

int main(int argc, char **argv) {
	bool wait_at_end = false;

	if (!strcmp(argv[1], "-w"))
		wait_at_end = true;

	std::cout << "Size of Cell: " << sizeof(Cell) << std::endl;
	Cell one((IntType)1), two("2");
	Cell three = one + two;
	std::cout << "One: " << one << ", two: " << two << ", three: " << three << std::endl;

	ListType l;
	l.push_back(one); l.push_back(two); l.push_back(three);
	Cell list(l);
	std::cout << "List 1: " << list << std::endl;
	Cell list2(LIST);
	Cell list2_f = list2 + one;
	std::cout << "List 2: " << list2_f << std::endl;

	if (wait_at_end) {
		std::cerr << "Press enter to exit";
		while (std::cin.get() != '\n') /* Nothing */;
	}
}