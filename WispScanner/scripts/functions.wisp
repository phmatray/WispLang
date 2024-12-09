fun count(n) {
  if (n > 1) count(n - 1);
  print n;
}

print count;
count(3);

fun add(a, b, c) {
  print a + b + c;
}

print add;
add(1, 2, 3);

fun sayHi(first, last) {
  print "Hi, " + first + " " + last + "!";
}

print sayHi;
sayHi("Dear", "Reader");

var a = "global";
{
  fun showA() {
    print a;
  }

  showA();
  var a = "block";
  showA();
}

print clock;
print clock();

fun fib(n) {
  if (n <= 1) return n;
  return fib(n - 2) + fib(n - 1);
}

print fib;
for (var i = 0; i < 20; i = i + 1) {
  print fib(i);
}

