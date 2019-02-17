; Factorial sample
;
; 
fac:
	ENTER 0
	;   if (<= n 1) return 1
	;      load n, push
	LEA 2
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
	NOP

n_not_le_1:

	; return n * fac(n - 1)
	;    n *
	LEA 2
	PUSH
	;     fac( n - 1)
	LEA 2
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

;
	NOP
; int main (int n) {
main:
	ENTER 0
;    print "Factorial of "
; TODO: Assembler bug - spaces break data
	DATA "Factorial"
	PRINT
	;    print n
	LEA 1
	PRINT
	;    print "is"
	DATA "is"
	PRINT
	;    print fac(n)
	LEA 1
	PUSH
	JSR fac
	PRINT
	ADJ 2
	DATA Environment.NewLine
	PRINT
	DATA $0
	EXIT
