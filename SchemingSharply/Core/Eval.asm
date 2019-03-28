; Eval.asm
; Implements the Scheme eval(x, env) loop.
;
; Written in a simple assembly format:
;   1) Comment style: ; comment.
;   2) A number of pre-defined words are added to the name dictionary.
;      These include all OpCodes, as well as CellType enum and some
;      standard runtime values.
;   3) Recognition of line labels and storing line number to dictionary
;   4) Allows users to define define symbols to dictionary.
;   5) DATA opcodes must provide either a number or a string, and are
;      then converted into a DATA code and the equivalent offset in the
;      Data segment for the data provided.
;      NOTE: when providing numbers to DATA, precede with $.
;        EG: DATA $1
;   6) After all lines read, labels added, and symbols added, a final
;      pass runs that replaces any known words with number equivalents.
; xxx) Optionally, OpCode can be checked to ensure a number is provided
;      on the same line as the command for OpCodes that require it one.
;   8) OpCodes are implemented as numbers.
;   9) Data segment generated: Cell[] type.
;  10) int[] generated that contains each number placed consecutively.
;
; A few notes on stack usage:
;   1) Stack starts at the end of the memory segment, and grows downwards.
;   2) (JSR) Calling a subroutine pushes the return address onto the stack.
;   3) (ENTER) Entering a subroutine saves the current bp to the stack and
;      sets up a new bp and sp using sp+(ARGUMENT TO ENTER) which allows for define
;      variables on the stack.
;   4) (LEA) Accessing pushed arguments requires using LEA with a minimum
;      argument value of 2 (to skip the above two instruction stack values.)
;      If a single value is pushed, LEA 2 accesses that value.
;      If a second value was pushed, you would access with LEA 2 and LEA 3.
;   5) By convention, arguments are pushed right-to-left.
;   6) LEAVE restores the previous bp, and returns to the previous program
;      counter (PC) value, popping those two values off the stack as it does
;      so. Any define variables allocated by ENTER are inaccessible.(1)
;      Return value should be left in A.
;  Footnotes: (1) Not deallocated when BP adjusted, but no longer deemed
;                 in use. Will be overridden by PUSH or other stack use.

; Notes on virtual machine:
;   1) Many comparison OpCodes implicitly pop a value off the stack.
;      Variations on those OpCodes that end with a K do not do this.
;
; Notes on specific OpCodes:
;  ...
;
; func eval(x, env)
;   defines: xl0, exps, proc
eval:
; bp+3 = x
; bp+2 = env
; bp+1 = return addr
; bp+0 = previous bp
; bp-1 = xl0
; bp-2 =
!define x     3
!define env   2
!define xl0  -1
!define exps -2
!define proc -3
!define test -4
!define conseq -5
!define alt -6
	ENTER 6
; tail recursive entry
eval_tail_recurse:
	LEA x ; get x
	CELLTYPE ; get cell type
	PUSH ; leave on stack
	; if (typeof x == Symbol)
	DATA $CellType.SYMBOL
	EQK
	BZ if_x_ne_symbol
	;   return env[x];
	LEA x ; x
	PUSH ; onto stack
	LEA env ; env
	ENVLOOKUP ; A = cell.env[A]
	LEAVE
if_x_ne_symbol:
	; if (typeof x == Number || typeof x == String)
	DATA $CellType.NUMBER
	EQK
	BNZ if_x_number_string
	DATA $CellType.STRING
	EQK
	BZ if_x_ne_number_string
if_x_number_string:
	;   return x;
	LEA x
	LEAVE

if_x_ne_number_string:
	; remove celltype from stack
	ADJ 1
	; if (x.listcount == 0)
	LEA x
	CELLCOUNT ; A = A.ListValue.Count
	PUSH
	DATA $0
	EQ
	BZ if_xcount_ne_0
	;   return sym_nil;
	DATA StandardRuntime.Nil
	LEAVE

if_xcount_ne_0:
	; xl0 = x.list[0]
	LEA x
	PUSH
	DATA $0
	CELLINDEX
	SEA xl0
	ADJ 1 ; remove x from stack
	PUSH ; will be grabbed later
	CELLTYPE ; stack = x.list[0].type
	PUSH
	; if (typeof xl0 == Symbol) {
	DATA $CellType.SYMBOL
	EQ
	BZ if_xl0_ne_symbol
	;   if (xl0 == "quote")
	DATA "quote"
	EQK
	BZ if_xl0_ne_quote
	;     return x.list[1]
	LEA x
	PUSH
	DATA $1
	CELLINDEX ; A = Stack[SP][A]
	LEAVE ; stack will be cleared

if_xl0_ne_quote:
	;   if (xl0 == "if")
	DATA "if"
	EQK ; keep xl0 on stack
	BZ if_xl0_ne_if
	ADJ 1 ; remove compared item
	;     return eval_if(x, env)
	;  test = eval(x.list[1], env)
	LEA x
	PUSH   ; stack++ = parts
	DATA $1
	CELLINDEX
	ADJ 1 ; remove parts from stack
	PUSH   ; stack++ = parts.list[1]
	LEA env
	PUSH   ; stack++ = targetenv
	JSR eval
	SEA test ; test = A
	ADJ 2
	;  conseq = parts.list[2]
	LEA x
	PUSH   ; stack++ = parts
	DATA $2
	CELLINDEX  ; leaving *stack == parts
	SEA conseq
	;  if parts.listcount <= 3
	PEEK      ; copy *stack into A
	CELLCOUNT ; get cell count of A
	PUSH      ; push count on to stack
	DATA $3
	LE        ; stack now has parts after comparison
	BZ eval_if_has_alt
	;    alt = nil
	DATA StandardRuntime.Nil
	JMP eval_if_store_alt
	;  else
eval_if_has_alt:
	;    alt = x.list[3]
	DATA $3
	CELLINDEX
eval_if_store_alt:
	; (save into alt)
	SEA alt
	ADJ 1
	;  return test == false ? alt : conseq
	LEA test
	PUSH
	DATA StandardRuntime.False
	EQ
	BZ eval_if_conseq
	LEA alt
	JMP eval_if_dosub
eval_if_conseq:
	LEA conseq
	; fall through
eval_if_dosub:
	;PUSH
	;LEA env
	;PUSH
	;JSR eval
	; ADJ 2
	; LEAVE

	; tail recurse
	SEA x
	JMP eval_tail_recurse

if_xl0_ne_if:
	;   if (xl0 == "set!")
	DATA "set!"
	EQK
	BZ if_xl0_ne_set!
	ADJ 1 ; remove compared item
	;     env.Set(x.list[1], eval(x.list[2], env))
	;     env
	LEA env
	PUSH
	;       eval(x.list[2], env)
	LEA x
	PUSH
	DATA $2
	CELLINDEX
	ADJ 1 ; remove x from stack
	PUSH
	LEA env
	PUSH
	JSR eval
	ADJ 2 ; remove env and x
	PUSH  ; push result onto stack
	;     x.list[1]
	LEA x
	PUSH
	DATA $1
	CELLINDEX
	ADJ 1
	PUSH ; push x[1]
	SWITCH POP ; grab 123 from stack
	; ADJ 1 ; remove x from stack
	;   env.Set() - puts value in A
	ENVSET
	LEAVE

if_xl0_ne_set!:
	;   if (xl0 == "define")
	DATA "define"
	EQK
	BZ if_xl0_ne_define
	ADJ 1 ; remove compared item
	;     env.Define(x.list[1], eval(x.list[2], env))
	;     env
	LEA env
	PUSH
	;       eval(x.list[2], env)
	LEA x
	PUSH
	DATA $2
	CELLINDEX
	ADJ 1 ; remove x from stack
	PUSH
	LEA env
	PUSH
	JSR eval
	ADJ 2 ; remove env and x
	PUSH  ; push result onto stack
	;     x.list[1]
	LEA x
	PUSH
	DATA $1
	CELLINDEX
	ADJ 1
	PUSH ; push x[1]
	SWITCH POP ; grab 123 from stack
	;   env.Define() - puts value in A
	ENVDEFINE
	LEAVE

if_xl0_ne_define:
	; if (xl0 == "lambda") {
	DATA "lambda"
	EQK
	BZ if_xl0_ne_lambda
	; x.type = Lambda
	LEA x
	PUSH ; copy x to stack for modification
	DATA $CellType.LAMBDA
	CELLSETTYPE
	; x.env = env
	LEA env ; x is still on stack
	CELLSETENV
	POP ; move modified cell into A
	;     return x
	LEAVE
	;   }

if_xl0_ne_lambda:
	; if (xl0 == "macro") {
	DATA "macro"
	EQK
	BZ if_xl0_ne_macro
	; x.type = MAcro
	LEA x
	PUSH
	DATA $CellType.MACRO
	CELLSETTYPE
	; x.env = env
	LEA env
	CELLSETENV
	POP
	LEAVE

if_xl0_ne_macro:
	DATA "begin"
	EQK
	BZ if_xl0_ne_begin
	ADJ 1 ; remove compared item
	;   *stack = x[1...]
	LEA x
	PUSH   ; will be reused in while loop
	CELLTAIL
	PUSH
	;   while(*stack.length > 1)
begin_while:
	PEEK
	CELLCOUNT ; a = *a.listvalue.length
	PUSH ; *++stack = stack.length
	DATA $1 ; a = 1
	GT ; a = *stack > 1
	BZ if_stacklen_not_gt1
	;     eval(*stack, env)
	CELLHEAD
	PUSH
	LEA env
	PUSH
	JSR eval ; recurse!
	;     *stack = *stack.tail()
	ADJ 2
	CELLTAIL  ; a = *stack[1...]
	ADJ 1 ; remove from stack
	PUSH ; put it back onto the stack for comparison loop
	JMP begin_while
	;   }
if_stacklen_not_gt1:
	;   return eval(*stack, env)  - item to eval still on stack
	CELLHEAD
	;PUSH
	;LEA env
	;PUSH
	;JSR eval
	;LEAVE
	SEA x
	ADJ 2
	JMP eval_tail_recurse

if_xl0_ne_begin:
	DATA "typeof"
	EQK
	BZ if_xl0_ne_typeof
	LEA x
	PUSH
	DATA $1
	CELLINDEX
	CELLTYPE
	LEAVE

if_xl0_ne_typeof:
	; falls through:
	; }
	; label if_xl0_ne_symbol:
if_xl0_ne_symbol:
	; proc = eval(x.list[0], env)
	LEA xl0
	PUSH
	LEA env
	PUSH
	JSR eval
	ADJ 2 ; remove from stack
	SEA proc
	; exps = new Cell(List)
	DATA $CellType.LIST
	CELLNEW
	SEA exps
	; skip first element
	LEA x
	PUSH
	CELLTAIL
	ADJ 1
	PUSH   ; keep exps on stack
	; if(proc.type == Macro)
	LEA proc
	CELLTYPE
	PUSH
	DATA $CellType.MACRO
	EQ
	BZ eval_exps_while_head
	;    exps = x.tail()
	POP
	SEA exps
; BREAK
	JMP eval_exps_done
	; else
eval_exps_while_head:
	PEEK
	;   while(x.listvalue.count > 0) {
eval_exps_while:
	CELLCOUNT
	PUSH
	DATA $0
	GT
	BZ eval_exps_done
	;   exps.push(eval(head(x), env))
	CELLHEAD
	PUSH
	LEA env
	PUSH
	JSR eval
	ADJ 2 ; remove head(x) and env
	PUSH
	LEA exps
	PUSH
	SWITCH POP ; swap result and exps, move result to A
	CELLPUSH  ; *stack.list.push(A)
	POP
	SEA exps
	;   x = tail(x)
	CELLTAIL  ; A = *Stack.listvalue.Tail()
	ADJ 1 ; remove it from stack to replace
	PUSH
	JMP eval_exps_while
	; }

eval_exps_done:
	; if (proc.type == Lambda) {
	LEA proc
	CELLTYPE
	PUSH
	DATA $CellType.LAMBDA
	EQK
	BZ eval_proc_ne_lambda
	ADJ 1
	; a) proc.list[1] contains parameter names
	; b) proc.list[2] contains lambda body
	; 1) Push proc.list[2]
	; 2) Create new environment parented to current env, using proc.list[1]
	;    as keys and exps as values.
	; 3) return eval(proc.list[2], newenv)
	LEA proc
	PUSH  ; *stack++ = proc
	DATA $2
	CELLINDEX ; a = *stack[a] (proc)
	PUSH  ; *stack++ = proc.list[2]
	SWITCH ; switch *stack and *stack-1 (get back to proc)
	DATA $1
	CELLINDEX ; a = *stack[a] (proc)
	ADJ 1 ; remove proc from stack
	PUSH  ; *stack++ = proc.list[1]
	LEA exps
	PUSH  ; *stack++ = env
	LEA proc
	CELLGETENV
	ENVNEW; A = new env(*stack+1, *stack, A.Environment)
	ADJ 1 ; remove proc.list[1]
	; PUSH  ; *stack++ = newenv
	; JSR eval
	; LEAVE
	SEA env ; put new env into env
	POP SEA x   ; put body into x
	ADJ 2
	JMP eval_tail_recurse

eval_proc_ne_lambda:
	; if (proc.type == Macro) {
	LEA proc
	CELLTYPE
	PUSH
	DATA $CellType.MACRO
	EQK
	BZ eval_proc_ne_macro
	ADJ 1
	; a) proc.list[1] contains parameter names
	; b) proc.list[2] contains lambda body
	; 1) Push proc.list[2]
	; 2) Create new environment parented to current env, using proc.list[1]
	;    as keys and exps as values.
	; 3) return eval(proc.list[2], newenv)
	LEA proc
	PUSH  ; *stack++ = proc
	DATA $2
	CELLINDEX ; a = *stack[a] (proc)
	PUSH  ; *stack++ = proc.list[2]
	SWITCH ; switch *stack and *stack-1 (get back to proc)
	DATA $1
	CELLINDEX ; a = *stack[a] (proc)
	ADJ 1 ; remove proc from stack
	PUSH  ; *stack++ = proc.list[1]
	LEA exps
	PUSH  ; *stack++ = env
	LEA proc
	CELLGETENV
	ENVNEW; A = new env(*stack+1, *stack, A.Environment)
	ADJ 1 ; remove proc.list[1]
	PUSH  ; *stack++ = newenv
	JSR eval
	ADJ 2
	SEA x
	JMP eval_tail_recurse

eval_proc_ne_macro:
	; if (proc.type == PROC)
	DATA $CellType.PROC
	EQK
	BZ eval_proc_ne_proc
	; return proc.ProcValue(exps)
	LEA exps PUSH
	LEA proc
	CELLINVOKE
	LEAVE

eval_proc_ne_proc:
	; if (proc.type == PROCENV)
	DATA $CellType.PROCENV
	EQK
	BZ eval_proc_ne_procenv
	; return proc.ProcValueEnv(exps, env)
	LEA exps PUSH
	LEA env PUSH
	LEA proc
	CELLINVENV
	LEAVE

eval_proc_ne_procenv:
	DATA "Invalid CellType in eval "
	PRINT
	POP
	PRINT
	DATA Environment.NewLine
	PRINT
	STATE
	HALTMSG "Unknown type in eval at proc stage"
	
; ======== end proc eval

; main (Code, Env)
main:
!define Code 2
!define Env  1
	ENTER 0
	;   Eval(Code, Env)
	LEA Code
	PUSH
	LEA Env
	PUSH
	JSR eval
	EXIT
