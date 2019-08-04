//	Timing tests for different implementations of a Scheme environment.
//  Testing:
//    o Insertion speed
//    o Lookup speed
//
// Approach 1: use a map<string,Cell>
//  Negatives:
//    - Insertion speed
//    - Lookup speed
//    - Memory consumption
//  Positives:
//    + Simple
//    + Better lookup time on strings than unordered_map
//
// Approach 2: use a map<Atom,Cell>
//  where Atom is a subclass of Cell
//  and a static map<AtomId,Atom> exists
//  and AtomId is unsigned
//  Negatives:
//     - Complexity (must also manage Atom dictionary)
//     - Insertion speed is lacking due to containing a sorted list
//  Positives:
//     + Inherently faster key comparison
//     + Less memory usage in the environment map
//     + Faster lookup speed than using string
//
// Bonus approach: Use an unordered_map instead of a map.
// Uses a hash table instead of balanced trees.
//   Negatives:
//     - Strings do not hash so well
//     - Non-sorted, making lookup time potentially slower: O(n)
//   Positives:
//     + Integers work well as hash keys
//     + Non-sorted, making insertion time faster
//
#include <algorithm> // std::find
#include <chrono>
#include <iterator>
#include <iostream>
#include <limits>
#include <list>
#include <map>
#include <sstream>
#include <string>
#include <unordered_map>
#include <vector>

// Special configuration
#define ENSURE_UNIQUE_KEYS false // true
#if  _DEBUG
int it_mult = 1;
#else
int it_mult = 4000;
#endif

#ifdef _MSC_VER
  #include <Windows.h>
  #define IsDebuggerAttached() IsDebuggerPresent()
  // Windows.h defines these, but we don't want their definitions
  #undef min
  #undef max
#else
  #define IsDebuggerAttached() false
#endif

// Sourced from: http://coliru.stacked-crooked.com/a/0d56f604931a7441
namespace TextUtils {
	/*! note: delimiter cannot contain NUL characters
	 */
	template <typename Range, typename Value = typename Range::value_type>
	std::string Join(Range const& elements, const char *const delimiter) {
		std::ostringstream os;
		auto b = begin(elements), e = end(elements);

		if (b != e) {
			std::copy(b, prev(e), std::ostream_iterator<Value>(os, delimiter));
			b = prev(e);
		}
		if (b != e) {
			os << *b;
		}

		return os.str();
	}

	/*! note: imput is assumed to not contain NUL characters
	 */
	template <typename Input, typename Output, typename Value = typename Output::value_type>
	void Split(char delimiter, Output &output, Input const& input) {
		using namespace std;
		for (auto cur = begin(input), beg = cur; ; ++cur) {
			if (cur == end(input) || *cur == delimiter || !*cur) {
				output.insert(output.end(), Value(beg, cur));
				if (cur == end(input) || !*cur)
					break;
				else
					beg = next(cur);
			}
		}
	}
} // namespace TextUtils

// Time related utils
typedef typename std::chrono::high_resolution_clock ClockType;
typedef typename std::chrono::milliseconds ResolutionType;
//typedef typename std::chrono::microseconds ResolutionType;
typedef long TimeType;
template<typename T> std::string period_type() { return "unknown"; }
template<> std::string period_type<std::chrono::microseconds>() { return "us"; }
template<> std::string period_type<std::chrono::milliseconds>() { return "ms"; }

// A generic cell
struct Cell;
typedef unsigned AtomKey;
typedef std::string StringKey;
typedef std::map<AtomKey, Cell> AtomMapType;
typedef std::unordered_map<AtomKey, Cell> AtomUnorderedMapType;
typedef std::map<StringKey, Cell> StringMapType;
typedef std::unordered_map<StringKey, Cell> StringUnorderedMapType;

const AtomKey nil = 0;

// Two-column output
class TwoColOutput {
	std::vector<std::string> _list;
	std::string pad(std::string str, size_t columnWidth) {
		std::string res = str;
		while (res.length() < columnWidth)
			res += " ";
		return res;
	}
public:
	TwoColOutput() : _list() { }
	void add(std::string content, bool newline = true) {
		if (!newline) {
			std::string curr = _list.back();
			content = curr + content;
			_list.pop_back();
		}
		_list.push_back(content);
	}
	std::string collapse(size_t columnWidth = 60) {
		size_t i, midpoint = this->_list.size() / 2;
		std::vector<std::string> out;
		for (i = 0; i < midpoint; ++i) {
			std::string a, b;
			size_t bi = midpoint + i;
			a = this->_list[i];
			b = "";
			if (bi < this->_list.size())
				b = this->_list[bi];
			out.push_back(pad(a, columnWidth) + b);
		}
		return TextUtils::Join(out, "\n");
	}
};
TwoColOutput Output;

struct Cell {
	enum Type {
		Atom,
		String
	};
	Cell(AtomKey atomValue = nil) : _type(Atom), _atomValue(atomValue), _stringValue("") { }
	Cell(const std::string &stringValue) : _type(Atom), _atomValue(nil), _stringValue(stringValue) {}
	AtomKey atomValue() const { return _atomValue; }
	StringKey stringValue() const { return _stringValue; }
private:
	Type _type;
	AtomKey _atomValue;
	StringKey _stringValue;
};

template<
	typename Callback,
	typename Resolution = ResolutionType,
	typename Time = TimeType>
Time timeCallback(Callback callback) {
	auto start = ClockType::now();
	callback();
	auto finish = ClockType::now();
	auto duration = std::chrono::duration_cast<Resolution>(finish - start);
	return (Time)duration.count();
}

template<typename Callback, typename Resolution = ResolutionType, typename Time = TimeType>
Time reportTime(const std::string &message, Callback callback) {
	std::stringstream ss;
	ss << message;
	Time t = timeCallback(callback);
	ss << t << period_type<Resolution>();
	Output.add(ss.str());
	return t;
}

template<typename KeyType>
struct KeyGeneratedResult {
	KeyGeneratedResult (KeyType key, Cell value)
		: _key(key), _value(value) {}
	KeyType key() const { return _key; }
	Cell value() const { return _value; }
	bool operator== (const KeyType &kv) const {
		return key() == kv;
	}
	bool operator== (const KeyGeneratedResult<KeyType> &other) const {
		return key() == other.key();
	}
private:
	KeyType _key;
	Cell _value;
};

struct TimingInfo {
	int generation = 0;
	int insertion = 0;
	int lookup = 0;
	int keys_generated = 0;
	Cell last_cell;
};

template<
	typename KeyType, 
	typename MapType,
	typename ResultType = KeyGeneratedResult<KeyType>,
	typename VectorType = std::vector<ResultType>>
void test(VectorType &keyMap, MapType &valueMap, bool keepResults, TimingInfo &info) {
	if (!keepResults)
		valueMap.clear();

	// Insertion test
	info.insertion = reportTime("    Insertion test: ", [&keyMap, &valueMap] () {
		for (auto it = keyMap.cbegin(); it != keyMap.cend(); ++it) {
			ResultType result = *it;
			valueMap[result.key()] = result.value();
		}
	});

	info.lookup = reportTime("    Lookup test: ", [&keyMap, &valueMap, &info] () {
		Cell c;
		for (auto it = keyMap.cbegin(); it != keyMap.cend(); ++it) {
			ResultType result = *it;
			auto find_it = valueMap.find(result.key());
			if (find_it != valueMap.end())
				c = find_it->second;
		}
		info.last_cell = c;
	});
}

template<typename T> T typeRand() { return rand() % std::numeric_limits<T>::max(); }

template<
	typename KeyType = AtomKey,
	typename ResultType = KeyGeneratedResult<KeyType>,
	typename VectorType = std::vector<ResultType>>
void generateAtomKeys(int keyCount, bool clear, VectorType &keys) {
	if (clear)
		keys.clear();
	for (KeyType i = keyCount * it_mult; i > 0; --i) {
		KeyType key;
		do {
			key = typeRand<KeyType>();
		} while (ENSURE_UNIQUE_KEYS && std::find(keys.cbegin(), keys.cend(), key) != keys.cend());
		keys.push_back(ResultType(key, Cell(key)));
	}
}

template<typename KeyType = StringKey,
	typename KeySubType = char,
	typename ResultType = KeyGeneratedResult<KeyType>,
	typename VectorType = std::vector<ResultType>>
void generateStringKeys(int keyCount, size_t keyLength, bool clear, VectorType &keys) {
	if (clear)
		keys.clear();
	for (int i = keyCount * it_mult; i > 0; --i) {
		// Generate a random string up to keyLength length.
		std::string key;
		do {
			do {
				key += typeRand<KeySubType>();
			} while (key.length() < keyLength);
		} while (ENSURE_UNIQUE_KEYS && std::find(keys.cbegin(), keys.cend(), key) != keys.cend());
		keys.push_back(ResultType(key, Cell(key)));
	}
}

template<
	typename MapType,
	typename KeyType = AtomKey,
	typename ResultType = KeyGeneratedResult<KeyType>,
	typename VectorType = std::vector<ResultType>>
void testAtoms(VectorType &keys, MapType &values) {
	for (auto clearingKeys = 0; clearingKeys < 1; ++clearingKeys) {
		for (auto iteration = 1; iteration < 3; ++iteration) {
			auto keyCount = iteration * 50;
			bool clearKeys = clearingKeys == 0;

			std::stringstream ss;
			ss << "Iteration " << iteration << ","
				<< "  Key count: " << keyCount
				<< "  Clear keys: " << (clearKeys ? "yes" : "no");
			Output.add(ss.str());
			ss = std::stringstream(); // clear

			TimingInfo info;
			info.generation = reportTime("    Generate keys: ", [iteration, keyCount, clearKeys, &keys] () {
				generateAtomKeys(keyCount, clearKeys, keys);
			});
			ss << "    Keys generated: " << keys.size();
			Output.add(ss.str());
			ss = std::stringstream(); // clear
			test<KeyType,MapType>(keys, values, clearKeys, info);
			ss << "    Insert+Lookup: " << (info.insertion + info.lookup) << period_type<ResolutionType>();
			Output.add(ss.str());
		}
	}
}

template<
	typename MapType,
	typename KeyType = StringKey,
	typename ResultType = KeyGeneratedResult<KeyType>,
	typename VectorType = std::vector<ResultType>>
void testStrings(VectorType &keys, MapType &values) {
	for (auto clearingKeys = 0; clearingKeys < 1; ++clearingKeys) {
		for (auto iteration = 1; iteration < 3; ++iteration) {
			auto keyCount = iteration * 50;
			auto keyLength = iteration * 5;
			bool clearKeys = clearingKeys == 0;

			std::stringstream ss;
			ss << "Iteration " << iteration << ","
				<< "Key length: " << keyLength
				<< " nr: " << keyCount
				<< " Clear keys: " << (clearKeys ? "yes" : "no");
			Output.add(ss.str());
			ss = std::stringstream(); // clear

			TimingInfo info;
			info.generation = reportTime("    Generate keys: ", [iteration, keyCount, keyLength, clearKeys, &keys] () {
				generateStringKeys(keyCount, keyLength, clearKeys, keys);
			});
			ss << "    Keys generated: " << keys.size();
			Output.add(ss.str());
			ss = std::stringstream(); // clear
			test<KeyType,MapType>(keys, values, clearKeys, info);
			ss << "    Insert+Lookup: " << (info.insertion + info.lookup) << period_type<ResolutionType>();
			Output.add(ss.str());
		}
	}
}

int main(int argc, char **argv) {
	std::vector<KeyGeneratedResult<AtomKey>> atomKeys;
	std::vector<KeyGeneratedResult<StringKey>> stringKeys;
	AtomMapType atomMap;
	AtomUnorderedMapType atomMapU;
	StringMapType stringMap;
	StringUnorderedMapType stringMapU;

	std::cerr << "\rPlease wait for the tests to complete [0/4]";

	Output.add("=== Ordered map ATOM test ===");
	testAtoms(atomKeys, atomMap);
	std::cerr << "\rPlease wait for the tests to complete [1/4]";
	Output.add("");
	Output.add("=== UNOrdered map ATOM test ===");
	testAtoms(atomKeys, atomMapU);
	std::cerr << "\rPlease wait for the tests to complete [2/4]";
	Output.add("");

	Output.add("=== Ordered map STRING test ===");
	testStrings(stringKeys, stringMap);
	std::cerr << "\rPlease wait for the tests to complete [3/4]";
	Output.add("");
	Output.add("=== UNOrdered map STRING test ===");
	testStrings(stringKeys, stringMapU);
	std::cerr << "\rPlease wait for the tests to complete [4/4]";
	Output.add("");

	//std::cerr << "\rTests complete:" << std::endl;
	std::cerr << Output.collapse() << std::endl;
	std::cerr << "Iteration multiplier: " << it_mult << std::endl;

	if (IsDebuggerAttached()) {
		std::cout << "Press [enter] to finish" << std::endl;
		while (std::cin.get() != '\n') /* Nothing */;
	}

	return 0;
}
