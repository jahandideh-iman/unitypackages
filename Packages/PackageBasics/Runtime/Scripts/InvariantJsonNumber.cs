using System;
using System.Globalization;
using NiceJson;

namespace Arman.PackageBasics
{
    /// <summary>
    /// A NiceJson number that serializes with the invariant culture, so its JSON uses a <c>.</c>
    /// decimal separator on every device locale. NiceJson's own <see cref="JsonBasic"/> formats
    /// every number except <c>decimal</c> with the device culture, which writes <c>1,5</c> on a
    /// locale such as de-DE and produces JSON that does not parse back.
    /// </summary>
    /// <remarks>
    /// <c>float</c> and <c>double</c> use the round-trip format, so a parsed value converts back
    /// to the exact number that was written. Parsing needs nothing extra: NiceJson already reads
    /// numbers with the invariant culture.
    /// </remarks>
    [Serializable]
    public class InvariantJsonNumber : JsonBasic
    {
        public InvariantJsonNumber(int value)
            : base(value) { }

        public InvariantJsonNumber(long value)
            : base(value) { }

        public InvariantJsonNumber(float value)
            : base(value) { }

        public InvariantJsonNumber(double value)
            : base(value) { }

        public InvariantJsonNumber(decimal value)
            : base(value) { }

        public override string ToJsonString()
        {
            switch (ValueObject)
            {
                case float value:
                    return value.ToString("R", CultureInfo.InvariantCulture);
                case double value:
                    return value.ToString("R", CultureInfo.InvariantCulture);
                default:
                    return ((IFormattable)ValueObject).ToString(null, CultureInfo.InvariantCulture);
            }
        }
    }
}
