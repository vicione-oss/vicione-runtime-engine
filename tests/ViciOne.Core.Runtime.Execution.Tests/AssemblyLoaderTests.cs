using AwesomeAssertions;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using ViciOne.Core.Runtime.Execution;
using ViciOne.Engine.DefaultPoolings;
using ViciOne.Engine.DefaultValidators;
using Xunit;

public class AssemblyLoaderTests
{
    public sealed class CreateRelevantAssemblies : IDisposable
    {
        private DirectoryInfo? WorkingDir { get; set; }
        private WeakReference? LoadContextReference { get; set; }

        [Fact]
        public void CreatesRelevantAssemblies()
        {
            var expectedFbAssembly = GetType().Assembly;

            var created = AssemblyLoader.CreateRelevantAssemblies(expectedFbAssembly);

            created.FbAssembly.Should().BeSameAs(expectedFbAssembly);
        }

        [Fact]
        public void AddsDefaultAdditionalAssemblies()
        {
            Assembly[] expectedAdditionalAssemblies =
            [
                typeof(AndPooling).Assembly,
                typeof(RangeValidator<>).Assembly,
            ];

            var created = AssemblyLoader.CreateRelevantAssemblies(GetType().Assembly);

            created.AdditionalAssemblies.Should().Contain(expectedAdditionalAssemblies);
        }

        [Fact]
        public void LoadsAssemblyFromPath()
        {
            AssemblyLoadContext? loadContext = null;
            try
            {
                WorkingDir = Directory.CreateTempSubdirectory($"{GetType().Assembly.GetName().Name}_TemporaryAssemblies_");
                loadContext = new AssemblyLoadContext(WorkingDir.Name, isCollectible: true);
                var fbAssemblyPath = CreateAndSaveTemporaryAssembly(WorkingDir.FullName);
                LoadContextReference = new WeakReference(loadContext);

                var created = AssemblyLoader.CreateRelevantAssemblies(fbAssemblyPath, loadContext);

                created.FbAssembly.Location.Should().Be(fbAssemblyPath);
            }
            finally
            {
                // Unload context to prepare deletion of the temporary assembly file
                loadContext?.Unload();
            }
        }

        private static string CreateAndSaveTemporaryAssembly(string workingDir)
        {
            var builder = AssemblyBuilder.DefineDynamicAssembly(new(Guid.NewGuid().ToString()), AssemblyBuilderAccess.RunAndCollect);
            var generator = new Lokad.ILPack.AssemblyGenerator();
            var path = Path.Combine(workingDir, Path.GetRandomFileName());
            generator.GenerateAssembly(builder, path);
            return path;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1063:Implement IDisposable correctly", Justification = "Not relevant for the tests")]
        public void Dispose()
        {
            if (LoadContextReference is not null)
            {
                var timeoutWatch = Stopwatch.StartNew();
                while (LoadContextReference.IsAlive && timeoutWatch.ElapsedMilliseconds <= 5000)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            WorkingDir?.Delete(true);

            GC.SuppressFinalize(this);
        }
    }
}
