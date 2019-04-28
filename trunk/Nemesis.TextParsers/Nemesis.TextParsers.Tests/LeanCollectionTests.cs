using System.Collections.Generic;
using NUnit.Framework;

namespace Nemesis.TextParsers.Tests
{
    [TestFixture(TestOf = typeof(LeanCollection<>))]
    public class LeanCollectionTests
    {
        

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
    }
}
