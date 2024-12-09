fun makeCounter() {
  var i = 10;
  fun count() {
    i = i + 1;
    print i;
  }

  return count;
}

var counter = makeCounter();
counter(); // "11".
counter(); // "12".