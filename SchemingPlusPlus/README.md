# SchemingPlusPlus
Adventures in Scheme on C++

This is a C++ port of SchemingSharply, intended to be runtime-compatible.

# About
Implements a Scheme environment, and several different Scheme evaluators:

  * **SimpleEval**: A simple C++ function which calls itself recursively. Offers no flexibility for message passing.

    This is the current default evaluator.

(Coming soon: the CellMachine and Frame evaluators)

# Why
Investigating multiple solutions to the problem of implementing Lisp on [C4-Lisp](https://github.com/andrakis/c4-lisp), itself an adventure in implementing Lisp in the self-hosting [C4](https://github.com/rswier/c4) C (subset) interpreter.

The goal is to design a Scheme interpreter that can run multiple processes (microthreads) and allow clean messaging passing, message waiting, and (micro)process management. Ideas that come to fruition in this project may end up in the [Elispidae Lisp Project](https://github.com/andrakis/Elispidae).

# Current Status

**Status**: Under active development; new evaluators coming soon

***Coming soon***:

* CellMachine and Frame evaluators

* Expanded builtin library

* Message send/receive

* Multiprocessor scheduling

Implemented milestones:
-----------------------

***(Most recent first)***

* Version 0.19 released.

* Makefile added.

* SimpleEval is now tail recursive

* Version 0.15 released.

* C++ port passes all tests.


