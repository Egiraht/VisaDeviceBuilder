using System;
using System.Threading.Tasks;
using VisaDeviceBuilder.Abstracts;
using VisaDeviceBuilder.Tests.Components;
using Xunit;

namespace VisaDeviceBuilder.Tests
{
  /// <summary>
  ///   The unit tests class covering the <see cref="MessageDeviceBuilder" /> class.
  /// </summary>
  public class MessageDeviceBuilderTests
  {
    /// <summary>
    ///   Defines the test asynchronous operations delay in milliseconds.
    /// </summary>
    private const int OperationDelay = 1;

    /// <summary>
    ///   Defines the test string value.
    /// </summary>
    private const string TestString = "Test string";

    /// <summary>
    ///   The test message processor method.
    /// </summary>
    private string TestMessageProcessor(IVisaDevice device, string request)
    {
      Task.Delay(OperationDelay).Wait();
      return device.AliasName + request;
    }

    /// <summary>
    ///   Testing a custom message processor.
    /// </summary>
    [Fact]
    public async Task MessageProcessorTest()
    {
      await using var messageDevice = new MessageDeviceBuilder()
        .UseMessageProcessor(TestMessageProcessor)
        .UseCustomVisaResourceManagerType<TestResourceManager>()
        .UseDefaultResourceName(TestResourceManager.SerialTestDeviceResourceName)
        .BuildDevice();

      await messageDevice.OpenSessionAsync();
      Assert.Equal(messageDevice.AliasName + TestString, await messageDevice.SendMessageAsync(TestString));
    }

    /// <summary>
    ///   Testing copying device builder configuration from another buildable device instance using the device builder's
    ///   constructor.
    /// </summary>
    [Fact]
    public async Task DeviceBuilderConfigurationCopyingTest()
    {
      // Creating a base message-based device instance.
      await using var baseMessageDevice = new MessageDeviceBuilder()
        .UseMessageProcessor(TestMessageProcessor)
        .UseCustomVisaResourceManagerType<TestResourceManager>()
        .UseDefaultResourceName(TestResourceManager.SerialTestDeviceResourceName)
        .BuildDevice();

      // Copying the configuration from the base message-based buildable device instance to another message-based device
      // builder.
      var derivedMessageDevice = new MessageDeviceBuilder(baseMessageDevice).BuildDevice();
      await derivedMessageDevice.OpenSessionAsync();
      Assert.Equal(derivedMessageDevice.AliasName + TestString,
        await derivedMessageDevice.SendMessageAsync(TestString));

      // Passing an invalid (non-buildable) message-based device instance to the constructor must throw an exception.
      Assert.Throws<InvalidOperationException>(() => new MessageDeviceBuilder(new MessageDevice()));
    }
  }
}
