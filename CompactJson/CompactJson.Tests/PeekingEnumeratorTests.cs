using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompactJson.Tests
{
    public class PeekingEnumeratorTests
    {
        [Test]
        public void TestPeeker() => TestPeekerCore<int>();

        [Test]
        public void TestPeekerNulla() => TestPeekerCore<int?>();

        [Test]
        public void TestPeekerObject() => TestPeekerCore<object>();

        public void TestPeekerCore<T>()
        {
            var enu = Enumerable.Range(0, 2).Reverse().Cast<T>();
            using (var enumerator = enu.GetEnumerator())
            using (var peeker = new PeekingEnumerator<T>(enumerator))
            {
                Assert.That(peeker.Empty, Is.False);
                Assert.That(peeker.First, Is.EqualTo(1));
                Assert.That(peeker.Current, Is.EqualTo(default(T))); // this is "undefined"; actually it returns default

                Assert.That(peeker.MoveNext(), Is.True);
                Assert.That(peeker.Current, Is.EqualTo(1));
                Assert.That(peeker.MoveNext(), Is.True);
                Assert.That(peeker.Current, Is.EqualTo(0));
                Assert.That(peeker.MoveNext(), Is.False);
            }
        }

        [Test]
        public void TestPeekerJust1()
        {
            var enu = new List<int>() { 999 };
            using (var enumerator = enu.GetEnumerator())
            using (var peeker = new PeekingEnumerator<int>(enumerator))
            {
                Assert.That(peeker.Empty, Is.False);
                Assert.That(peeker.First, Is.EqualTo(999));
                Assert.That(peeker.Current, Is.EqualTo(0)); // this is "undefined"; actually it returns default

                Assert.That(peeker.MoveNext(), Is.True);
                Assert.That(peeker.Current, Is.EqualTo(999));
                Assert.That(peeker.MoveNext(), Is.False);
            }
        }

        [Test]
        public void TestPeekerJust0()
        {
            var enu = new List<int>(); // forever empty.
            using (var enumerator = enu.GetEnumerator())
            using (var peeker = new PeekingEnumerator<int>(enumerator))
            {
                Assert.That(peeker.Empty, Is.True);
                Assert.That(peeker.First, Is.EqualTo(0)); // this is "undefined"; actually it returns default
                Assert.That(peeker.Current, Is.EqualTo(0)); // this is "undefined"; actually it returns default

                Assert.That(peeker.MoveNext(), Is.False);
            }
        }

        [Test]
        public void TestPeekingEnumerable() => TestPeekingEnumerableCore<int>();

        [Test]
        public void TestPeekingEnumerableNulla() => TestPeekingEnumerableCore<int?>();

        [Test]
        public void TestPeekingEnumerableObject() => TestPeekingEnumerableCore<object>();

        public void TestPeekingEnumerableCore<T>()
        {
            var enu = Enumerable.Range(0, 2).Reverse().Cast<T>();
            var expected = enu.ToList();
            using (var penu = new PeekingEnumerable<T>(enu))
            {
                Assert.That(penu.Empty, Is.False);
                Assert.That(penu.First, Is.EqualTo(1));
                Assert.That(penu.EnumeratorCount, Is.EqualTo(1));
                for (var idx = 0; idx < 10; idx++)
                { // repeat a bunch of times (really only need 2 tries total)
                    Assert.That(penu, Is.EqualTo(expected), "sequence equal on try {0}", idx);
                    Assert.That(penu.EnumeratorCount, Is.EqualTo(1 + idx));
                }
            }
        }
    }
}