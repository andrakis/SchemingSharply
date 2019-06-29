;; More complicated eval function, written in Scheme

(begin
	% simple functions
	(define else (lambda (code) code))
	(define then (lambda (code) code))
	(define else-if (lambda (a b c) (if a b c)))

	(define scm-eval eval)
	(define eval-tests (list))
	(define eval-rule (lambda (compareCb translateCb) (push! eval-tests (list compareCb translateCb))))
	(define ?eval-rule (lambda (x) (?eval-rule-reduce eval-tests x)))
	(define ?eval-rule-reduce (lambda (rules x)
	;; (Head:(Head:ruleA Tail:transA) Tail:(Head:ruleB Tail:transB))
		(if (= #nil rules) #nil
			(if (= #true ((head (head rules)) x))
				(head (tail (head x))
				(?eval-rule-reduce (tail rules) x))
			)
		)
	))

	(set! eval (lambda (x env) (begin
		(define gotRule (?eval-rule x))
		(if (!= #nil gotRule)
			(then (gotRule x env))
			(else (scm-eval x env))
		)
	)))

	;; Eval(X:String, Env) -> Env[X]
	(eval-rule (lambda (x) (= (typeof x) (CellType.String))) (lambda (x env) (env-lookup env x)))
	;; Eval(X:Number, Env) -> X
	(eval-rule (lambda (x) (= (typeof x) (CellType.Number))) (lambda (x env) x))
)