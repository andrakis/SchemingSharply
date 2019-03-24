# SchemingSharply
Adventures in Scheme on C#

# About
Implements a Scheme environment, and a stack-based virtual machine which uses a Scheme Cell as its accumulator and stack data types.
The virtual machine is based upon the [C4](https://github.com/rswier/c4) virtual machine.

Also implements an assembler for the virtual machine.
Apart from tests, code is loaded and assembled from files in `Core/*.asm`.

# Why
Investigating a stack-based solution to the problem of implementing Lisp on [C4-Lisp](https://github.com/andrakis/c4-lisp), itself an adventure in implementing Lisp in the self-hosting [C4](https://github.com/rswier/c4) C (subset) interpreter.

# Current Status

**Status**: Under active development; interpreter fully functional; new frame-based interpreter mostly functional

***Coming soon***:

* Expanded builtin library

* Message send/receive

* Multiprocessor scheduling

Implemented milestones:
-----------------------

***(Most recent first)***

* Implemented first pass of frame-based interpreter. Uses frames and subframes to acheive similar effect to virtual machine interpreter, but with a much faster speed.

* New framework allows easy testing of each interpreter implementation

* Version 1.0.1.0 released

* Implemented utility functions to enable/disable debugging, and timing and step count.

* Version 1.0.0.0 released

* Command-line options implemented

* All tests now passing

* Fixed missing builtins, and broken append builtin

* Implemented tests

* Implemented string type and parser that skips comments

* Fixed tail recursion using excess stack

* Implemented REPL using [Eval.asm](https://github.com/andrakis/SchemingSharply/blob/master/SchemingSharply/Core/Eval.asm)

* Implemented test cases and updated [Eval.asm](https://github.com/andrakis/SchemingSharply/blob/master/SchemingSharply/Core/Eval.asm)

* Implement initial pass on [Eval.asm](https://github.com/andrakis/SchemingSharply/blob/master/SchemingSharply/Core/Eval.asm)

* Implemented factorial sample in [Fac.asm](https://github.com/andrakis/SchemingSharply/blob/master/SchemingSharply/Core/Fac.asm)

* Assembler implemented

* Factorial sample working

* Manually assembled factorial sample
