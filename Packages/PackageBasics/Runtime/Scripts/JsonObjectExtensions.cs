using System;
using System.Globalization;
using NiceJson;

namespace Arman.PackageBasics
{
    /// <summary>
    /// Locale-safe additions to NiceJson's <see cref="JsonObject"/>.
    /// </summary>
    public static class JsonObjectExtensions
    {
        /// <summary>
        /// Adds <paramref name="value"/> under <paramref name="key"/> so that its JSON uses a
        /// <c>.</c> decimal separator on every device locale.
        /// </summary>
        /// <remarks>
        /// NiceJson's own <c>Add(key, value)</c> formats a float with the device culture, which
        /// writes <c>1,5</c> on a locale such as de-DE and produces JSON that does not parse back.
        /// This writes the round-trip format with the invariant culture, so the parsed number
        /// converts back to the exact float that was written. Parsing needs nothing extra: NiceJson
        /// already reads numbers with the invariant culture.
        ///
        /// It is not named <c>Add</c> because <see cref="JsonObject.Add"/> also accepts a float,
        /// through NiceJson's implicit conversion to <see cref="JsonNode"/>, and C# chooses an
        /// applicable instance method over an extension method.
        /// </remarks>
        public static void AddInvariant(this JsonObject jsonObject, string key, float value)
        {
            jsonObject.Add(key, new InvariantFloat(value));
        }

        [Serializable]
        private class InvariantFloat : JsonBasic
        {
            public InvariantFloat(float value)
                : base(value) { }

            public override string ToJsonString()
            {
                return ((float)ValueObject).ToString("R", CultureInfo.InvariantCulture);
            }
        }
    }
}
