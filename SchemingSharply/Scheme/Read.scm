(begin
	(define expand (lambda (form)
		(cond
			((macro? (head form)
				(expand ((macro-function (head form)) form))))
			((= (head form) (quote quote)))
			(#true (map expand form))
			)))

	(define expand._macros_ (dict))
	(define macro-function (lambda (name) (dict-get expand._macros_ name)))
	(define install-macro (lambda (name func) (dict-put expand._macros_ name func)))
	(define macro? (lambda (name) (dict-key? expand._macros_ name)))

	;; Parse a string character-by-character to replace single 
	;; or should we attempt to build up a word and parse that
	(define char-parse (lambda (str) (begin
		#nil
	)))

	(define read (lambda (str) (begin
		(expand (char-parse str))
	)))
)
