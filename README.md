# SchemingSharply
Adventures in Scheme on C#

# About
Implements a Scheme environment, and a stack-based virtual machine which uses a Scheme Cell as its accumulator and stack data types.
The virtual machine is based upon the [C4](https://github.com/rswier/c4) virtual machine.

Also implements an assembler for the virtual machine.
Apart from tests, code is loaded and assembled from files in `Core/*.asm`.

# Why
Investigating a stack-based solution to the problem of implementing Lisp on [C4-Lisp](https://github.com/andrakis/c4-lisp), itself an adventure in implementing Lisp in the self-hosting [C4](https://github.com/rswier/c4) C (subset) interpreter.

