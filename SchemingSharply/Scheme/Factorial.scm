;; (Factorial Sample)
;;
;;

(begin

	;; Factorial - head recursive
	(define fac (lambda (n)
		(if (<= n 1) 1 (* n (fac (- n 1))))))

	;; Factorial - tail recursive
	(define fac_t (lambda (n) (fac_t/2 n 1)))
	(define fac_t/2 (lambda (n a)
		(if (<= n 1) a
			(fac_t/2 (- n 1) (* n a)))))

	(print "Use (fac 10) for a head-recursive example")
	(print "Use (fac_t 10) for a tail-recursive example")
	(if (= #true (debug)) (print "Note: debug mode on, use (debug #false) to disable"))
	(repl)

)
