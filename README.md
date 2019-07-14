# SchemingSharply
Adventures in Scheme on C#

(And a [minor in C++](https://github.com/andrakis/SchemingSharply/tree/master/SchemingPlusPlus))

# About
Implements a Scheme environment, and several different Scheme evaluators:

  * **CellMachine**: A stack-based virtual machine which uses a Scheme Cell as its accumulator and stack data types.
  
    The virtual machine is based upon the [C4](https://github.com/rswier/c4) virtual machine.

    Also implements an assembler for the virtual machine.
    
    Apart from tests, code is loaded and assembled from files in `Core/*.asm`.
    
  * **StandardEval**: A simple C# function which calls itself recursively. Offers no flexibility for message passing.
  
  * **FrameEval**: A frame-based state machine that is faster than the virtual machine, yet still offers flexibility for message passing and multiprocessing.
  
    This is the current default evaluator.

# Why
Investigating multiple solutions to the problem of implementing Lisp on [C4-Lisp](https://github.com/andrakis/c4-lisp), itself an adventure in implementing Lisp in the self-hosting [C4](https://github.com/rswier/c4) C (subset) interpreter.

The goal is to design a Scheme interpreter that can run multiple processes (microthreads) and allow clean messaging passing, message waiting, and (micro)process management. Ideas that come to fruition in this project may end up in the [Elispidae Lisp Project](https://github.com/andrakis/Elispidae).

# Current Status

**Status**: Under active development; new frame-based interpreter fully functional

***Coming soon***:

* Expanded builtin library

* Message send/receive

* Multiprocessor scheduling

Implemented milestones:
-----------------------

***(Most recent first)***

* C# - FrameEval is now completely stackless.

* C++ port passes all tests.

* Version 1.0.4.0 released

* All three evaluators brought up-to-date, and passing all tests.

* Implemented macro system.

* Version 1.0.2.0 released

* The frame evaluator is now the default evaluator.

* Implemented tail recursion to frame evaluator; fixed bug with function calls with 0 arguments.

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
