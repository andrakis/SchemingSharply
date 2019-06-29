/**
 * TextUtils
 * Borrowed from
 *  https://stackoverflow.com/questions/5288396/c-ostream-out-manipulation/5289170#5289170
 */
#include "TextUtils.h"

#include <iostream>
#include <iomanip>

static inline auto ut_display(std::string const& v) { return std::quoted(v); }
static inline auto ut_display(size_t v) { return v; }

#define UT_EQUAL(a, b)                                                                         \
    do {                                                                                       \
        auto &&_a = (a);                                                                       \
        auto &&_b = (b);                                                                       \
        bool result = (_a == _b);                                                              \
        if (!result) {                                                                         \
            std::cerr << __FILE__ << ":" << __LINE__ << " " << __FUNCTION__ << "\n";           \
            std::cerr << "\texpressions: a:" << #a << " b:" << #b << "\n";                     \
            std::cerr << "\tactuals: a:" << ut_display(_a) << " b:" << ut_display(_b) << "\n"; \
        }                                                                                      \
    } while (0)

void testSplit() {
	std::vector<std::string> res;
	const std::string test = "a test ,string, to,,,be, split,\"up,up\",";
	TextUtils::Split(',', res, test);

	UT_EQUAL(10u, res.size());
	UT_EQUAL("a test ", res[0]);
	UT_EQUAL("string", res[1]);
	UT_EQUAL(" to", res[2]);
	UT_EQUAL("", res[3]);
	UT_EQUAL("", res[4]);
	UT_EQUAL("be", res[5]);
	UT_EQUAL(" split", res[6]);
	UT_EQUAL("\"up", res[7]); // Thus making 'split' unusable for parsing
	UT_EQUAL("up\"", res[8]); //  csv files...
	UT_EQUAL("", res[9]);

	TextUtils::Split('.', res, "dossier_id");
	UT_EQUAL(11u, res.size());

	res.clear();
	UT_EQUAL(0u, res.size());

	TextUtils::Split('.', res, "dossier_id");
	UT_EQUAL(1u, res.size());
	std::string UseName = res[res.size() - 1];
	UT_EQUAL("dossier_id", UseName);
}

void testJoin() {
	std::string elements[] ={ "aap", "noot", "mies" };

	typedef std::vector<std::string> strings;

	UT_EQUAL("", TextUtils::Join(strings(), ""));
	UT_EQUAL("", TextUtils::Join(strings(), "bla"));
	UT_EQUAL("aap", TextUtils::Join(strings(elements, elements + 1), ""));
	UT_EQUAL("aap", TextUtils::Join(strings(elements, elements + 1), "#"));
	UT_EQUAL("aap", TextUtils::Join(strings(elements, elements + 1), "##"));
	UT_EQUAL("aapnoot", TextUtils::Join(strings(elements, elements + 2), ""));
	UT_EQUAL("aap#noot", TextUtils::Join(strings(elements, elements + 2), "#"));
	UT_EQUAL("aap##noot", TextUtils::Join(strings(elements, elements + 2), "##"));
	UT_EQUAL("aapnootmies", TextUtils::Join(strings(elements, elements + 3), ""));
	UT_EQUAL("aap#noot#mies", TextUtils::Join(strings(elements, elements + 3), "#"));
	UT_EQUAL("aap##noot##mies", TextUtils::Join(strings(elements, elements + 3), "##"));
	UT_EQUAL("aap  noot  mies", TextUtils::Join(strings(elements, elements + 3), "  "));

	UT_EQUAL("aapnootmies", TextUtils::Join(strings(elements, elements + 3), "\0"));
	UT_EQUAL("aapnootmies", TextUtils::Join(strings(elements, elements + 3), std::string("\0", 1).c_str()));
	UT_EQUAL("aapnootmies", TextUtils::Join(strings(elements, elements + 3), std::string("\0+", 2).c_str()));
	UT_EQUAL("aap+noot+mies", TextUtils::Join(strings(elements, elements + 3), std::string("+\0", 2).c_str()));
}

