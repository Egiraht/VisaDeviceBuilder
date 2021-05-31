using System;
using Moq;
using VisaDeviceBuilder.Abstracts;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="VisaDeviceException" /> class.
  /// </summary>
  public class VisaDeviceExceptionTests
  {
    /// <summary>
    ///   Defines the exception's test message.
    /// </summary>
    private const string TestMessage = "Test message";

    /// <summary>
    ///   Defines the exception's test message.
    /// </summary>
    private const string TestDeviceAlias = "Test device";

    /// <summary>
    ///   Testing the exception.
    /// </summary>
    [Fact]
    public void ExceptionTest()
    {
      var deviceMock = new Mock<IVisaDevice>();
      deviceMock.SetupGet(visaDevice => visaDevice.AliasName).Returns(TestDeviceAlias);
      var device = deviceMock.Object;

      var exception = new VisaDeviceException(device);
      Assert.Equal(device, exception.Device);
      Assert.Contains(exception.GetType().Name, exception.Message);

      exception = new VisaDeviceException(device, string.Empty);
      Assert.Equal(device, exception.Device);
      Assert.Contains(TestDeviceAlias, exception.Message);

      exception = new VisaDeviceException(device, TestMessage);
      Assert.Equal(device, exception.Device);
      Assert.Equal(TestMessage, exception.Message);

      var innerException = new Exception(TestMessage);
      exception = new VisaDeviceException(device, innerException);
      Assert.Equal(device, exception.Device);
      Assert.Equal(innerException, exception.InnerException);
      Assert.Contains(innerException.GetType().Name, exception.Message);
      Assert.Contains(TestDeviceAlias, exception.Message);
      Assert.Contains(TestMessage, exception.Message);

      exception = new VisaDeviceException(device, innerException, TestMessage);
      Assert.Equal(device, exception.Device);
      Assert.Equal(innerException, exception.InnerException);
      Assert.Equal(TestMessage, exception.Message);
    }
  }
}
