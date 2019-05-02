﻿using System;
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

            Assert.That(actual, Is.EquivalentTo(new[] { 15.5f }));
        }

        [Test]
        public void Iterate_TwoElement()
        {
            var coll = new LeanCollection<float>(15.5f, 25.6f);
            Assert.That(coll.Size, Is.EqualTo(2));

            var actual = coll.ToList();
            Assert.That(actual, Has.Count.EqualTo(2));

            Assert.That(actual, Is.EquivalentTo(new[] { 15.5f, 25.6f }));
        }

        [Test]
        public void Iterate_ThreeElement()
        {
            var coll = new LeanCollection<float>(15.5f, 25.6f, 35.99f);
            Assert.That(coll.Size, Is.EqualTo(3));

            var actual = coll.ToList();
            Assert.That(actual, Has.Count.EqualTo(3));

            Assert.That(actual, Is.EquivalentTo(new[] { 15.5f, 25.6f, 35.99f }));
        }

        [TestCase(null, 0)]
        [TestCase(new float[0], 0)]
        [TestCase(new[] { 15.5f }, 1)]
        [TestCase(new[] { 15.5f, 25.6f }, 2)]
        [TestCase(new[] { 15.5f, 25.6f, 35.99f, 50, 999 }, 5)]
        public void Iterate_ArrayElement(float[] elements, int expectedLength)
        {
            var coll = new LeanCollection<float>(elements);
            Assert.That(coll.Size, Is.EqualTo(expectedLength));

            var actual = coll.ToList();
            Assert.That(actual, Has.Count.EqualTo(expectedLength));

            if (elements != null)
                Assert.That(actual, Is.EquivalentTo(elements));
        }

        [Test]
        public void Conversions()
        {
            LeanCollection<float> coll0 = default;
            Assert.That(coll0.Size, Is.EqualTo(0));


            LeanCollection<float> coll1 = 100.1f;
            Assert.That(coll1.Size, Is.EqualTo(1));
            Assert.That(coll1.ToList(), Is.EqualTo(new[] { 100.1f }));


            LeanCollection<float> coll2 = (1000, 2000);
            Assert.That(coll2.Size, Is.EqualTo(2));
            Assert.That(coll2.ToList(), Is.EqualTo(new[] { 1000, 2000 }));


            LeanCollection<float> coll3 = (100,200,300);
            Assert.That(coll3.Size, Is.EqualTo(3));
            Assert.That(coll3.ToList(), Is.EqualTo(new[] { 100, 200, 300 }));
            

            var array = new[] { 15.5f, 25.6f, 35.99f, 50, 999 };
            LeanCollection<float> collMore = array;
            Assert.That(collMore.Size, Is.EqualTo(5));
            Assert.That(collMore.ToList(), Is.EqualTo(new[] { 15.5f, 25.6f, 35.99f, 50, 999 }));
        }

        [TestCase(null, "")]
        [TestCase(new float[0], "")]
        [TestCase(new[] { 15.5f }, "15.5")]
        [TestCase(new[] { 15.5f, 25.6f }, "15.5|25.6000004")]
        [TestCase(new[] { 15.5f, 25.6f, 35.99f, 50, 999 }, "15.5|25.6000004|35.9900017|50|999")]
        public void ToStringTest(float[] elements, string expectedText)
        {
            var coll = new LeanCollection<float>(elements);
            Assert.That(coll.ToString(), Is.EqualTo(expectedText));
        }

        [TestCase(null)]
        [TestCase(new float[0])]
        [TestCase(new[] { 15.5f })]
        [TestCase(new[] { 15.5f, 25.6f })]
        [TestCase(new[] { 15.5f, 25.6f, 35.99f, 50, 999 })]
        public void EqualsTest(float[] elements)
        {
            var coll1 = new LeanCollection<float>(elements);
            var coll2 = new LeanCollection<float>(elements);
            Assert.That(coll1, Is.EqualTo(coll2));

            var coll3 = new LeanCollection<float>(new[] { 15.5f, 25.6f, 35.99f, 50, 999, 1555555 });
            Assert.That(coll1, Is.Not.EqualTo(coll3));

            if (elements?.Length > 0)
            {
                var newElements = new float[elements.Length];
                Array.Copy(elements, newElements, elements.Length);
                newElements[0] = 9999;
                var coll4 = new LeanCollection<float>(newElements);
                Assert.That(coll1, Is.Not.EqualTo(coll4));
            }
        }
    }
}
