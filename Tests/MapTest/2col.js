function TwoCol (columnSize) {
	this.buffer = [];
	this.colSize = columnSize || 40;
}
TwoCol.prototype.add = function(str, newline) {
	newline = (newline === undefined ? true : newline);
	if(!newline && this.buffer.length > 0) {
		str = this._pop() + str;
	}
	this._push(str);
};
TwoCol.prototype._pop = function() {
	return this.buffer.pop();
};
TwoCol.prototype._push = function(a) {
	this.buffer.push(a);
};
TwoCol.prototype.collapse = function() {
	var midpoint = (this.buffer.length / 2);
	var out = [];
	for(var i = 0; i < midpoint; ++i) {
		var a = this.buffer[i], b = this.buffer[midpoint + i];
		if(b === undefined) b = "";
		var line = this._pad(a) + b;
		out.push(line);
	}
	return out.join("\n");
};
TwoCol.prototype._pad = function(s) {
	if(s === undefined) s = "";
	while(s.length < this.colSize)
		s += " ";
	return s;
};

var _2c = new TwoCol();
for(var i = 0; i < 10; ++i) {
	var line = "TwoCol 2 Col it " + i;
	_2c.add(line);
}

console.log(_2c.collapse());
