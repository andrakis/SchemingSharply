; Factorial sample
;
; Calculates the factorial of the given number using recursion.

; fac(n) ->
fac:
!define fac_n 2
	ENTER 0
	;   if (<= n 1) return 1
	;      load n, push
	LEA fac_n
	PUSH
	;      load 1
	DATA $1
	;      n <= 1
	LE
	;      if A=true return 1
	BZ n_not_le_1
	;        load 1, leave
	DATA $1
	LEAVE

n_not_le_1:
	; return n * fac(n - 1)
	;    n *
	LEA fac_n
	PUSH
	;     fac( n - 1)
	LEA fac_n
	PUSH
	DATA $1
	SUB
	;    fac(n - 1)
	PUSH
	JSR fac
	ADJ 1
	;    on return result will be in A
	;    n * fac(n - 1)
	MUL
	;    return
	LEAVE

; fac_tail(N) -> N!
; Calculates the factorial of N using tail recursion
;
; fac_tail(N) -> fac_tail_2(N, 1)
fac_tail:
!define fc_n 2
	ENTER 0
	LEA fc_n
	PUSH
	DATA $1
	PUSH
	JSR fac_tail_2
	LEAVE

; fac_tail_2(N, A) ->
fac_tail_2:
!define fc2_N 3
!define fc2_A 2
	ENTER 0
	LEA fc2_N
fac_tail_2_a:   ; tail-recursive entrance
	; optimization: tail recursion leaves N in the A register
	; fac_tail_2(1, A) -> A;
	PUSH
	DATA $1
	EQ
	BZ fac_tail_2_b
	LEA fc2_A
	LEAVE

fac_tail_2_b:
	; fac_tail_2(N, A) -> fac_tail_2(N - 1, N * A)
	; modifies arguments and starts again.
	; optimization: calculate N - 1 last, leave in A, skip a LEA.
	LEA fc2_N
	PUSH
	LEA fc2_A
	MUL
	SEA fc2_A
	LEA fc2_N
	PUSH
	DATA $1
	SUB
	SEA fc2_N
	JMP fac_tail_2_a ; tail recurse

; int main (int n) {
main:
!define main_n 1
	ENTER 0
;    print "Head-recursive factorial of "
	DATA "Head-recursive factorial of "
	PRINT
	;    print n
	LEA main_n
	PRINT
	;    print " is "
	DATA " is "
	PRINT
	;    print fac(n)
	LEA main_n
	PUSH
	JSR fac
	PRINT
	ADJ 2
	DATA Environment.NewLine
	PRINT

;    print "Tail-recursive factorial of "
	DATA "Tail-recursive factorial of "
	PRINT
	;    print n
	LEA main_n
	PRINT
	;    print " is "
	DATA " is "
	PRINT
	;    print fac_tail(n)
	LEA main_n
	PUSH
	JSR fac_tail
	PRINT
	ADJ 2
	DATA Environment.NewLine
	PRINT
	; return 0
	DATA $0
	EXIT
	; }

