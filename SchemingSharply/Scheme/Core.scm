;; Core.scm
;;
;; Contains the core runtime functions.

(begin

	(debug #true)
	(define car (lambda (l) (head l)))
	(define cdr (lambda (l) (tail l)))

	(define else (macro (action) (list (quote list) #true action)))
	(define cond (macro (tests)
		(list
			(list (quote if)
				(list (quote =) #true (head (head tests)))
				(list (head (tail (head tests))))
				(list (quote cond) (tail tests))))))

	;; (print (env-str (env)))
	;; (help)
	;; (repl)



)
