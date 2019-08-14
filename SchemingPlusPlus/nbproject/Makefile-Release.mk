#
# Generated Makefile - do not edit!
#
# Edit the Makefile in the project folder instead (../Makefile). Each target
# has a -pre and a -post target defined where you can add customized code.
#
# This makefile implements configuration specific macros and targets.


# Environment
MKDIR=mkdir
CP=cp
GREP=grep
NM=nm
CCADMIN=CCadmin
RANLIB=ranlib
CC=gcc
CCC=g++
CXX=g++
FC=gfortran
AS=as

# Macros
CND_PLATFORM=Cygwin_x86_64-Windows
CND_DLIB_EXT=dll
CND_CONF=Release
CND_DISTDIR=dist
CND_BUILDDIR=build

# Include project Makefile
include Makefile

# Object Directory
OBJECTDIR=${CND_BUILDDIR}/${CND_CONF}/${CND_PLATFORM}

# Object Files
OBJECTFILES= \
	${OBJECTDIR}/Scheme.o \
	${OBJECTDIR}/SchemeAssert.o \
	${OBJECTDIR}/SchemeCell.o \
	${OBJECTDIR}/SchemeEnvironment.o \
	${OBJECTDIR}/SchemeEvalSimple.o \
	${OBJECTDIR}/SchemeParser.o \
	${OBJECTDIR}/SchemeRuntime.o \
	${OBJECTDIR}/SchemingPlusPlus.o \
	${OBJECTDIR}/SchemingTests.o \
	${OBJECTDIR}/TextUtils.o


# C Compiler Flags
CFLAGS=

# CC Compiler Flags
CCFLAGS=-flto
CXXFLAGS=-flto

# Fortran Compiler Flags
FFLAGS=

# Assembler Flags
ASFLAGS=

# Link Libraries and Options
LDLIBSOPTIONS=

# Build Targets
.build-conf: ${BUILD_SUBPROJECTS}
	"${MAKE}"  -f nbproject/Makefile-${CND_CONF}.mk ${CND_DISTDIR}/${CND_CONF}/${CND_PLATFORM}/schemingplusplus.exe

${CND_DISTDIR}/${CND_CONF}/${CND_PLATFORM}/schemingplusplus.exe: ${OBJECTFILES}
	${MKDIR} -p ${CND_DISTDIR}/${CND_CONF}/${CND_PLATFORM}
	${LINK.cc} -o ${CND_DISTDIR}/${CND_CONF}/${CND_PLATFORM}/schemingplusplus ${OBJECTFILES} ${LDLIBSOPTIONS}

${OBJECTDIR}/Scheme.o: Scheme.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/Scheme.o Scheme.cpp

${OBJECTDIR}/SchemeAssert.o: SchemeAssert.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/SchemeAssert.o SchemeAssert.cpp

${OBJECTDIR}/SchemeCell.o: SchemeCell.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/SchemeCell.o SchemeCell.cpp

${OBJECTDIR}/SchemeEnvironment.o: SchemeEnvironment.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/SchemeEnvironment.o SchemeEnvironment.cpp

${OBJECTDIR}/SchemeEvalSimple.o: SchemeEvalSimple.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/SchemeEvalSimple.o SchemeEvalSimple.cpp

${OBJECTDIR}/SchemeParser.o: SchemeParser.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/SchemeParser.o SchemeParser.cpp

${OBJECTDIR}/SchemeRuntime.o: SchemeRuntime.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/SchemeRuntime.o SchemeRuntime.cpp

${OBJECTDIR}/SchemingPlusPlus.o: SchemingPlusPlus.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/SchemingPlusPlus.o SchemingPlusPlus.cpp

${OBJECTDIR}/SchemingTests.o: SchemingTests.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/SchemingTests.o SchemingTests.cpp

${OBJECTDIR}/TextUtils.o: TextUtils.cpp
	${MKDIR} -p ${OBJECTDIR}
	${RM} "$@.d"
	$(COMPILE.cc) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/TextUtils.o TextUtils.cpp

# Subprojects
.build-subprojects:

# Clean Targets
.clean-conf: ${CLEAN_SUBPROJECTS}
	${RM} -r ${CND_BUILDDIR}/${CND_CONF}

# Subprojects
.clean-subprojects:

# Enable dependency checking
.dep.inc: .depcheck-impl

include .dep.inc
