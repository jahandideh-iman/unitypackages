
using Arman.InventorySystem;
using NUnit.Framework;


namespace Arman.InventorySystem.Tests
{
    public class MinMaxInventoryItemConstraintTest
    {
        MinMaxInventoryItemConstraint constraint;

        [Test]
        public void ApplyingShouldReturnTheGivenValueWhenTheValueIsBetweenMaxAndMin()
        {
            constraint = new MinMaxInventoryItemConstraint(3, 5);

            Assert.That(constraint.ApplyTo(3), Is.EqualTo(3));
            Assert.That(constraint.ApplyTo(4), Is.EqualTo(4));
            Assert.That(constraint.ApplyTo(5), Is.EqualTo(5));
        }

        [Test]
        public void ApplyingShouldReturnTheMaxValueWhenTheGivenValueIsMoreThanMax()
        {
            constraint = new MinMaxInventoryItemConstraint(3, 5);

            Assert.That(constraint.ApplyTo(6), Is.EqualTo(5));
            Assert.That(constraint.ApplyTo(12), Is.EqualTo(5));
        }

        [Test]
        public void ApplyingShouldReturnTheMinValueWhenTheGivenValueIsLessThanMin()
        {
            constraint = new MinMaxInventoryItemConstraint(3, 5);

            Assert.That(constraint.ApplyTo(2), Is.EqualTo(3));
            Assert.That(constraint.ApplyTo(-4), Is.EqualTo(3));
        }
    }
}