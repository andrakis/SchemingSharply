(begin
	; debug eval implementation

	(define deval (lambda (x env) (begin
		(print x " => " )
		(define result (deval-inner x env))
		(print x " => " result)
		result
	)))

	(define deval-inner (lambda (x env) (begin
		(define x`t (typeof x))
		(if (= x`t #symbol)
			(env.lookup env x)
			(if (or (= x`t #integer) (= x`t #float))
				x
				(if (empty? x) #nil (deval-complex x env))
			)
		)
	)))

	(define deval-complex (lambda (x env) (begin
		(define x`h (head x))
		(define x`h`t (typeof x`h))
		(if (= x`h`t #symbol)
			(if (= x`h "quote") (deval-quote x)
			(if (= x`h "if") (deval-if x env)
			(if (= x`h "set!") (deval-set! x env)
			(if (= x`h "define") (deval-define x env)
			(if (= x`h "lambda") (deval-lambda x env)
			(if (= x`h "macro") (deval-macro x env)
			(if (= x`h "begin") (deval-begin x env)
			; else
				(deval-proc x env)
			)))))))
			; else
			(deval-proc x env)
		)
	)))
)