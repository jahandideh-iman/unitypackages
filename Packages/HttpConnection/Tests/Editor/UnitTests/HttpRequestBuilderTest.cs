using System.Collections.Generic;
using NUnit.Framework;

namespace Arman.HttpConnection.Tests
{
    public class HttpRequestBuilderTest
    {
        HttpRequestBuilder _builder = null!;

        [SetUp]
        public void Setup()
        {
            _builder = new HttpRequestBuilder();
        }

        [Test]
        public void BuildingShouldCarryTheGivenValues()
        {
            var request = _builder
                .SetType(HttpRequestType.POST)
                .SetURL("https://example.com/api")
                .SetBody("{\"key\":1}")
                .SetTimeout(3f)
                .Build();

            Assert.That(request.RequestType, Is.EqualTo(HttpRequestType.POST));
            Assert.That(request.Url, Is.EqualTo("https://example.com/api"));
            Assert.That(request.Body, Is.EqualTo("{\"key\":1}"));
            Assert.That(request.TimeOut, Is.EqualTo(3f));
        }

        [Test]
        public void BuildingWithoutSettingValuesShouldGiveAGetRequestWithNoTimeout()
        {
            var request = _builder.Build();

            Assert.That(request.RequestType, Is.EqualTo(HttpRequestType.GET));
            Assert.That(request.TimeOut, Is.Null);
            Assert.That(request.Headers, Is.Empty);
            Assert.That(request.Parameters, Is.Empty);
        }

        [Test]
        public void BuildingShouldCarryTheAddedHeaders()
        {
            var request = _builder
                .AddHeader("Content-Type", "application/json")
                .AddHeader("Authorization", "token")
                .Build();

            Assert.That(
                request.Headers,
                Is.EquivalentTo(
                    new Dictionary<string, string>
                    {
                        ["Content-Type"] = "application/json",
                        ["Authorization"] = "token",
                    }
                )
            );
        }

        [Test]
        public void BuildingShouldCarryTheAddedParameters()
        {
            var request = _builder.AddParameter("page", "2").AddParameter("size", "10").Build();

            Assert.That(
                request.Parameters,
                Is.EquivalentTo(new Dictionary<string, string> { ["page"] = "2", ["size"] = "10" })
            );
        }

        [Test]
        public void AddingAHeaderWithAnExistingKeyShouldReplaceItsValue()
        {
            var request = _builder.AddHeader("Key", "first").AddHeader("Key", "second").Build();

            Assert.That(request.Headers["Key"], Is.EqualTo("second"));
            Assert.That(request.Headers, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddingAParameterWithAnExistingKeyShouldReplaceItsValue()
        {
            var request = _builder
                .AddParameter("Key", "first")
                .AddParameter("Key", "second")
                .Build();

            Assert.That(request.Parameters["Key"], Is.EqualTo("second"));
            Assert.That(request.Parameters, Has.Count.EqualTo(1));
        }

        [Test]
        public void SettingAValueAgainShouldReplaceThePreviousOne()
        {
            var request = _builder
                .SetType(HttpRequestType.POST)
                .SetType(HttpRequestType.GET)
                .SetURL("https://first.example.com")
                .SetURL("https://second.example.com")
                .SetTimeout(1f)
                .SetTimeout(2f)
                .Build();

            Assert.That(request.RequestType, Is.EqualTo(HttpRequestType.GET));
            Assert.That(request.Url, Is.EqualTo("https://second.example.com"));
            Assert.That(request.TimeOut, Is.EqualTo(2f));
        }
    }
}
