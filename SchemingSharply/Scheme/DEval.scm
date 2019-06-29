(begin
	; debug eval implementation

	(define deval (lambda (x env) (begin
		(print x " => " )
		(define result (deval-inner x env))
		(print x " => " result)
		result
	)))

)