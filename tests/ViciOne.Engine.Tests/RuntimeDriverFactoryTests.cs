using AwesomeAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Engine;
using Xunit;

public class RuntimeDriverFactoryTests
{
    public class Constructor
    {
        [Fact]
        public void CanCreateInstance()
        {
            Action action = () => _ = new RuntimeDriverFactory(GetAssemblies());

            action.Should().NotThrow();
        }
    }

    public class CreateInstances
    {
        [Fact]
        public void CanCreateInstances()
        {
            var driverInfosExpectedToSucceed = CreateGoodDriverInfos().ToList();
            var driverInfosExpectedToFail = CreateBadDriverInfos().ToList();
            var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
            using var loggerFactory = new TestLoggerFactory();
            var logger = new TestLogger();
            var factory = new RuntimeDriverFactory(GetAssemblies());

            var (instances, failedDriverInfos) = factory.CreateInstances(driverInfosExpectedToSucceed.Concat(driverInfosExpectedToFail), uniqueNameProvider, loggerFactory, logger);

            driverInfosExpectedToSucceed.Should().BeEquivalentTo(instances.Select(x => x.DriverTypeName));
            foreach (var driverTypeName in driverInfosExpectedToSucceed)
            {
                var runtimeDriver = instances.First(x => x.DriverTypeName == driverTypeName).RuntimeDriver;
                runtimeDriver.GetType().FullName.Should().Be(driverTypeName);
            }

            driverInfosExpectedToFail.Should().BeEquivalentTo(failedDriverInfos.Select(x => x.DriverTypeName));
            var errorMessages = logger.GetLoggedMessages(LogLevel.Error);
            foreach (var driverTypeName in driverInfosExpectedToFail)
            {
                var exception = failedDriverInfos.First(x => x.DriverTypeName == driverTypeName).Exception;
                errorMessages
                    .FirstOrDefault(x => x.Contains(driverTypeName, StringComparison.Ordinal) && x.Contains(exception.Message, StringComparison.Ordinal))
                    .Should().NotBeNull();
            }
        }

        [Fact]
        public void CreatesOwnLoggerForEveryDriver()
        {
            var factory = new RuntimeDriverFactory(GetAssemblies());
            var driverInfosExpectedToSucceed = CreateGoodDriverInfos().ToList();
            var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
            using var loggerFactory = new TestLoggerFactory();

            var (instances, _) = factory.CreateInstances(driverInfosExpectedToSucceed, uniqueNameProvider, loggerFactory, Substitute.For<ILogger>());

            instances.Select(x => x.RuntimeDriver).Cast<IHasLogger>().Select(x => x.Logger).Should()
                .NotContainNulls().And
                .OnlyHaveUniqueItems();
        }

        [Fact]
        public void CreatesCorrectLoggerCategory()
        {
            var factory = new RuntimeDriverFactory(GetAssemblies());
            var driverInfosExpectedToSucceed = CreateGoodDriverInfos().ToList();
            var uniqueNameProvider = Substitute.For<IUniqueNameProvider>();
            using var loggerFactory = new TestLoggerFactory();

            var (instances, _) = factory.CreateInstances(driverInfosExpectedToSucceed, uniqueNameProvider, loggerFactory, Substitute.For<ILogger>());

            instances.Select(x => x.RuntimeDriver).Cast<IHasLogger>().Should()
                .AllSatisfy(x =>
                {
                    ((TestLogger)x.Logger).Category.Should().Be(x.GetType().FullName);
                });
        }

        private static IEnumerable<string> CreateGoodDriverInfos()
        {
            yield return typeof(GoodDummyDriver1).FullName!;
            yield return typeof(GoodDummyDriver2).FullName!;
        }

        private static IEnumerable<string> CreateBadDriverInfos()
        {
            yield return "NotExisting.Type";
            yield return typeof(BadDummyDriver).FullName!;
        }
    }

    private static Assembly[] GetAssemblies()
        => [.. System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!.Assemblies];
}

public abstract class DummyDriver<TDriver> : RuntimeDriver<TDriver>, IHasLogger where TDriver : RuntimeDriver<TDriver>, new()
{
    public new ILogger Logger => base.Logger;
}

public class GoodDummyDriver1 : DummyDriver<GoodDummyDriver1> { }

public class GoodDummyDriver2 : DummyDriver<GoodDummyDriver2> { }

public class BadDummyDriver : DummyDriver<BadDummyDriver>, IRuntimeDriver
{
    void IRuntimeDriver.Setup(ILogger logger)
        => throw new InvalidOperationException();
}
