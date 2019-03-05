(begin
	(define combine (lambda (f) (begin
		(print "Combine:" f)
		(lambda (x y) (begin
			(print "Combine2:" x y)
			(if (null? x)
				(quote ())
				(f (list (head x) (head y)) ((combine f) (tail x) (tail y)))))
		)
	)))

	(define zip (combine cons))

	(define riff-shuffle (lambda (deck) (begin
		(print "riff-shuffle: " deck)
		(define take (lambda (n seq) (begin
			(print "take" n seq)
			(if (<= n 0)
				(quote ())
				(cons (head seq) (take (- n 1) (tail seq)))))))
		(define drop (lambda (n seq) (begin
			(print "drop" n seq)
			(if (<= n 0) seq (drop (- n 1) (tail seq))))))
		(define mid (lambda (seq) (begin
			(print "mid" seq)
			(/ (length seq) 2))))
		((combine append) (take (mid deck) deck) (drop (mid deck) deck))
	)))

	(print (riff-shuffle (list 1 2 3 4 5 6 7 8)))
)
