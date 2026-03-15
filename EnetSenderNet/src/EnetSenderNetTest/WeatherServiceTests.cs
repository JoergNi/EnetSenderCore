using EnetSenderNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EnetSenderNetTest
{
    [TestClass]
    public class WeatherServiceTests
    {
        private static HttpClient MakeClient(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            return new HttpClient(new FakeHandler(json, statusCode));
        }

        private static string CurrentJson(double temperature) =>
            $"{{\"current\":{{\"temperature_2m\":{temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}}}";

        private static string DailyHighJson(double temperature) =>
            $"{{\"daily\":{{\"temperature_2m_max\":[{temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)}]}}}}";

        [TestMethod]
        public void GetCurrentTemperature_ReturnsTemperatureFromResponse()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient(CurrentJson(18.5)));
            Assert.AreEqual(18.5, service.GetCurrentTemperature());
        }

        [TestMethod]
        public void GetCurrentTemperature_ReturnsNullOnHttpError()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient("error", HttpStatusCode.InternalServerError));
            Assert.IsNull(service.GetCurrentTemperature());
        }

        [TestMethod]
        public void GetCurrentTemperature_ReturnsNullOnInvalidJson()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient("not json"));
            Assert.IsNull(service.GetCurrentTemperature());
        }

        [TestMethod]
        public void GetDailyHighTemperature_ReturnsTemperatureFromResponse()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient(DailyHighJson(28.3)));
            Assert.AreEqual(28.3, service.GetDailyHighTemperature());
        }

        [TestMethod]
        public void GetDailyHighTemperature_ReturnsNullOnHttpError()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient("error", HttpStatusCode.InternalServerError));
            Assert.IsNull(service.GetDailyHighTemperature());
        }

        [TestMethod]
        public void IsHot_ReturnsTrueWhenDailyHighAboveThreshold()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient(DailyHighJson(25.0)));
            Assert.IsTrue(service.IsHot(24));
        }

        [TestMethod]
        public void IsHot_ReturnsFalseWhenDailyHighBelowThreshold()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient(DailyHighJson(20.0)));
            Assert.IsFalse(service.IsHot(24));
        }

        [TestMethod]
        public void IsHot_ReturnsFalseWhenDailyHighExactlyAtThreshold()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient(DailyHighJson(24.0)));
            Assert.IsFalse(service.IsHot(24));
        }

        [TestMethod]
        public void IsHot_ReturnsFalseOnFetchFailure()
        {
            var service = new WeatherService(50.9, 7.1, MakeClient("error", HttpStatusCode.ServiceUnavailable));
            Assert.IsFalse(service.IsHot());
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetCurrentTemperature_RealApi_ReturnsReasonableValue()
        {
            var service = new WeatherService(50.921210, 7.086539);
            var temp = service.GetCurrentTemperature();
            Assert.IsNotNull(temp, "Expected a temperature value from the real API");
            Assert.IsTrue(temp >= -30 && temp <= 50, $"Temperature {temp}°C is outside a reasonable range");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetDailyHighTemperature_RealApi_ReturnsReasonableValue()
        {
            var service = new WeatherService(50.921210, 7.086539);
            var temp = service.GetDailyHighTemperature();
            Assert.IsNotNull(temp, "Expected a daily high temperature from the real API");
            Assert.IsTrue(temp >= -30 && temp <= 50, $"Daily high {temp}°C is outside a reasonable range");
        }

        private class FakeHandler : HttpMessageHandler
        {
            private readonly string _content;
            private readonly HttpStatusCode _statusCode;

            public FakeHandler(string content, HttpStatusCode statusCode)
            {
                _content = content;
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_content, Encoding.UTF8, "application/json")
                };
                return Task.FromResult(response);
            }
        }
    }
}
