using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Nemesis.TextParsers.Utils;

using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(LeanCollection<>))]
    public class LeanCollectionTests
    {
        [Test]
        public void Iterate_ZeroElement()
        {
            var coll = new LeanCollection<float>();
            Assert.That(coll.Size, Is.EqualTo(0));

            var actual = coll.ToList();
            Assert.That(actual, Has.Count.EqualTo(0));
        }

        [Test]
        public void Iterate_OneElement()
        {
            var coll = new LeanCollection<float>(15.5f);
            Assert.That(coll.Size, Is.EqualTo(1));

            var actual = coll.ToList();
            Assert.That(actual, Has.Count.EqualTo(1));

            Assert.That(actual, Is.EqualTo(new[] { 15.5f }));
        }

        [Test]
        public void Iterate_TwoElement()
        {
            var coll = new LeanCollection<float>(15.5f, 25.6f);
            Assert.That(coll.Size, Is.EqualTo(2));

            var actual = coll.ToList();
            Assert.That(actual, Has.Count.EqualTo(2));

            Assert.That(actual, Is.EqualTo(new[] { 15.5f, 25.6f }));
        }

        [Test]
        public void Iterate_ThreeElement()
        {
            var coll = new LeanCollection<float>(15.5f, 25.6f, 35.99f);
            Assert.That(coll.Size, Is.EqualTo(3));

            var actual = coll.ToList();
            Assert.That(actual, Has.Count.EqualTo(3));

            Assert.That(actual, Is.EqualTo(new[] { 15.5f, 25.6f, 35.99f }));
        }

        [TestCase(null, new float[0])]
        [TestCase(new float[0], new float[0])]
        [TestCase(new[] { 15.5f }, new[] { 15.5f })]
        [TestCase(new[] { 15.5f, 25.6f }, new[] { 15.5f, 25.6f })]
        [TestCase(new[] { 15.5f, 25.6f, 35.99f, 50, 999 }, new[] { 15.5f, 25.6f, 35.99f, 50, 999 })]
        public void Iterate_ArrayElement(float[] elements, float[] expected)
        {
            var coll = LeanCollectionFactory.FromArray(elements);
            Assert.That(coll.Size, Is.EqualTo(expected.Length));

            var actual = coll.ToList();
            Assert.That(actual, Is.EqualTo(expected));

            if (elements != null)
                Assert.That(actual, Is.EqualTo(elements));
        }

        [Test]
        public void ImplicitConversions()
        {
            LeanCollection<float> coll0 = default;
            Assert.That(coll0.Size, Is.EqualTo(0));


            LeanCollection<float> coll1 = 100.1f;
            Assert.That(coll1.Size, Is.EqualTo(1));
            Assert.That(coll1.ToList(), Is.EqualTo(new[] { 100.1f }));


            LeanCollection<float> coll2 = (1000, 2000);
            Assert.That(coll2.Size, Is.EqualTo(2));
            Assert.That(coll2.ToList(), Is.EqualTo(new[] { 1000, 2000 }));


            LeanCollection<float> coll3 = (100, 200, 300);
            Assert.That(coll3.Size, Is.EqualTo(3));
            Assert.That(coll3.ToList(), Is.EqualTo(new[] { 100, 200, 300 }));


            var array = new[] { 15.5f, 25.6f, 35.99f, 50, 999 };
            var collMore = LeanCollectionFactory.FromArray(array);
            Assert.That(collMore.Size, Is.EqualTo(5));
            Assert.That(collMore.ToList(), Is.EqualTo(new[] { 15.5f, 25.6f, 35.99f, 50, 999 }));
        }

        [Test]
        public void ExplicitConversions_T()
        {
            var zero = (double)default(LeanCollection<double>);
            Assert.That(zero, Is.EqualTo(0.0));

            var one = (double)new LeanCollection<double>(3.14);
            Assert.That(one, Is.EqualTo(3.14));

            var two = (double)new LeanCollection<double>(123, 456);
            Assert.That(two, Is.EqualTo(123));

            var three = (double)new LeanCollection<double>(11, 22, 33);
            Assert.That(three, Is.EqualTo(11));

            var many = (double)new LeanCollection<double>(new[] { 1.1, 2.2, 3.3, 4.4, 5.5 });
            Assert.That(many, Is.EqualTo(1.1));
        }

        [Test]
        public void ExplicitConversions_T_T()
        {
            var zero = ((double, double))default(LeanCollection<double>);
            Assert.That(zero, Is.EqualTo(
                (0.0, 0.0)
            ));

            var one = ((double, double))new LeanCollection<double>(3.14);
            Assert.That(one, Is.EqualTo(
                (3.14, 0.0)
            ));

            var two = ((double, double))new LeanCollection<double>(123, 456);
            Assert.That(two, Is.EqualTo(
                (123, 456)
            ));

            var three = ((double, double))new LeanCollection<double>(11, 22, 33);
            Assert.That(three, Is.EqualTo(
                (11, 22)
            ));

            var many = ((double, double))new LeanCollection<double>(new[] { 1.1, 2.2, 3.3, 4.4, 5.5 });
            Assert.That(many, Is.EqualTo(
                (1.1, 2.2)
            ));
        }

        [Test]
        public void ExplicitConversions_T_T_T()
        {
            var zero = ((double, double, double))default(LeanCollection<double>);
            Assert.That(zero, Is.EqualTo(
                (0.0, 0.0, 0.0)
            ));

            var one = ((double, double, double))new LeanCollection<double>(3.14);
            Assert.That(one, Is.EqualTo(
                (3.14, 0.0, 0.0)
            ));

            var two = ((double, double, double))new LeanCollection<double>(123, 456);
            Assert.That(two, Is.EqualTo(
                (123, 456, 0.0)
            ));

            var three = ((double, double, double))new LeanCollection<double>(11, 22, 33);
            Assert.That(three, Is.EqualTo(
                (11, 22, 33)
            ));

            var many = ((double, double, double))new LeanCollection<double>(new[] { 1.1, 2.2, 3.3, 4.4, 5.5 });
            Assert.That(many, Is.EqualTo(
                (1.1, 2.2, 3.3)
            ));
        }

        [Test]
        public void ExplicitConversions_ArrayT()
        {
            var zero = (double[])default(LeanCollection<double>);
            Assert.That(zero, Is.EqualTo(
                Array.Empty<double>()
                ));

            var one = (double[])new LeanCollection<double>(3.14);
            Assert.That(one, Is.EqualTo(
                new[] { 3.14 }
                ));

            var two = (double[])new LeanCollection<double>(123.5, 456);
            Assert.That(two, Is.EqualTo(
                new[] { 123.5, 456 }
                ));

            var three = (double[])new LeanCollection<double>(11.0, 22, 33);
            Assert.That(three, Is.EqualTo(
                new[] { 11.0, 22, 33 }
                ));

            var many = (double[])new LeanCollection<double>(new[] { 1.1, 2.2, 3.3, 4.4, 5.5 });
            Assert.That(many, Is.EqualTo(
                new[] { 1.1, 2.2, 3.3, 4.4, 5.5 }
                ));
        }

        [TestCase(null)]
        [TestCase(new float[0])]
        [TestCase(new[] { 15.5f })]
        [TestCase(new[] { 15.5f, 25.6f })]
        [TestCase(new[] { 15.5f, 25.6f, 35.99f, 50, 999 })]
        public void EqualsTest(float[] elements)
        {
            var coll1 = LeanCollectionFactory.FromArray(elements);
            var coll2 = LeanCollectionFactory.FromArray(elements);
            Assert.That(coll1.GetHashCode(), Is.EqualTo(coll2.GetHashCode()), "#1 != #2");
            Assert.That(coll1, Is.EqualTo(coll2), "1 != 2");


            var coll2A = LeanCollectionFactory.FromArray(elements?.ToArray());
            Assert.That(coll1.GetHashCode(), Is.EqualTo(coll2A.GetHashCode()), "#1 != #2a");
            Assert.That(coll1, Is.EqualTo(coll2A), "1 != 2a");


            var coll3 = LeanCollectionFactory.FromArray(new[] { 15.5f, 25.6f, 35.99f, 50, 999, 1555555 });
            Assert.That(coll1, Is.Not.EqualTo(coll3));

            if (elements?.Length > 0)
            {
                var newElements = new float[elements.Length];
                Array.Copy(elements, newElements, elements.Length);
                newElements[0] = 9999;
                var coll4 = LeanCollectionFactory.FromArray(newElements);
                Assert.That(coll1, Is.Not.EqualTo(coll4));
            }
        }

        [TestCase(new[] { 1, 2f }, new[] { 1, 2f })]
        [TestCase(new[] { 1, 1f }, new[] { 1, 1f })]
        [TestCase(new[] { 2, 1f }, new[] { 1, 2f })]

        [TestCase(new[] { 1f, 2, 3 }, new[] { 1f, 2, 3 })]
        [TestCase(new[] { 3f, 2, 1 }, new[] { 1f, 2, 3 })]
        [TestCase(new[] { 3f, 1, 2 }, new[] { 1f, 2, 3 })]
        [TestCase(new[] { 2f, 3, 1 }, new[] { 1f, 2, 3 })]
        [TestCase(new[] { 2f, 2, 2 }, new[] { 2f, 2, 2 })]

        [TestCase(new[] { 1f, 8, 9, 5, 3, 4 }, new[] { 1.0f, 3.0f, 4.0f, 5.0f, 8.0f, 9.0f })]
        [TestCase(new[] { 1000f, 80, 90, 50, 3, 4 }, new[] { 3f, 4, 50, 80, 90, 1000 })]
        public void SortTest_CopiedArray(float[] elements, float[] expectedElements)
        {
            var copy = elements.ToArray();

            var test = LeanCollectionFactory.FromArray(elements, true);

            var actual = ((IListOperations<float>)test).Sort().ToList();

            Assert.That(actual, Is.EqualTo(expectedElements));

            Assert.That(actual, Is.Ordered);

            Assert.That(elements, Is.EqualTo(copy), "Post condition - do NOT mutate array");
        }

        [TestCase(new[] { 1f, 8, 9, 5, 3, 4 }, new[] { 1.0f, 3.0f, 4.0f, 5.0f, 8.0f, 9.0f })]
        [TestCase(new[] { 1000f, 80, 90, 50, 3, 4 }, new[] { 3f, 4, 50, 80, 90, 1000 })]
        public void SortTest_OriginalBufferIsModified(float[] elements, float[] expectedElements)
        {
            Assert.That(elements, Is.Not.Ordered);

            var copy = elements.ToArray();

            var test = LeanCollectionFactory.FromArray(elements);

            var actual = ((IListOperations<float>)test).Sort().ToList();

            Assert.That(actual, Is.EqualTo(expectedElements));

            Assert.That(actual, Is.Ordered);
            Assert.That(elements, Is.Ordered); //original buffer get's mutated

            Assert.That(elements, Is.Not.EqualTo(copy), "Post condition - DO mutate array");
        }

        [TestCase(null, new float[0])]
        [TestCase(new float[0], new float[0])]
        [TestCase(new[] { 15.5f }, new[] { 15.5f })]
        [TestCase(new[] { 15.5f, 25.6f }, new[] { 15.5f, 25.6f })]
        [TestCase(new[] { 15.5f, 25.6f, 35.99f }, new[] { 15.5f, 25.6f, 35.99f })]
        [TestCase(new[] { 15.5f, 25.6f, 35.99f, 50, 999 }, new[] { 15.5f, 25.6f, 35.99f, 50, 999 })]
        public void Enumerators_CheckEnumeration(float[] elements, float[] expected)
        {
            var coll = LeanCollectionFactory.FromArray(elements);

            LeanCollection<float>.LeanCollectionEnumerator structEnumerator = coll.GetEnumerator();

            var iEnumerator = ((IEnumerable)coll).GetEnumerator();
            using var iEnumeratorT = ((IEnumerable<float>)coll).GetEnumerator();


            var actual = new List<float>();
            while (structEnumerator.MoveNext())
                actual.Add(structEnumerator.Current);
            Assert.That(actual, Is.EqualTo(expected), "structEnumerator");


            actual = new();
            while (iEnumerator.MoveNext())
                actual.Add((iEnumerator.Current as float?) ?? float.NaN);
            Assert.That(actual, Is.EqualTo(expected), "iEnumerator");


            actual = new();
            while (iEnumeratorT.MoveNext())
                actual.Add(iEnumeratorT.Current);
            Assert.That(actual, Is.EqualTo(expected), "iEnumeratorT");
        }
    }
}
