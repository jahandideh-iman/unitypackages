using Arman.InventorySystem;
using NUnit.Framework;

namespace Arman.InventorySystem.Tests
{
    public class MinMaxInventoryItemConstraintTest
    {
        MinMaxInventoryItemConstraint _constraint;

        [Test]
        public void ApplyingShouldReturnTheGivenValueWhenTheValueIsBetweenMaxAndMin()
        {
            _constraint = new MinMaxInventoryItemConstraint(3, 5);

            Assert.That(_constraint.ApplyTo(3), Is.EqualTo(3));
            Assert.That(_constraint.ApplyTo(4), Is.EqualTo(4));
            Assert.That(_constraint.ApplyTo(5), Is.EqualTo(5));
        }

        [Test]
        public void ApplyingShouldReturnTheMaxValueWhenTheGivenValueIsMoreThanMax()
        {
            _constraint = new MinMaxInventoryItemConstraint(3, 5);

            Assert.That(_constraint.ApplyTo(6), Is.EqualTo(5));
            Assert.That(_constraint.ApplyTo(12), Is.EqualTo(5));
        }

        [Test]
        public void ApplyingShouldReturnTheMinValueWhenTheGivenValueIsLessThanMin()
        {
            _constraint = new MinMaxInventoryItemConstraint(3, 5);

            Assert.That(_constraint.ApplyTo(2), Is.EqualTo(3));
            Assert.That(_constraint.ApplyTo(-4), Is.EqualTo(3));
        }
    }
}
