using AwesomeAssertions;
using System;
using ViciOne.Engine;
using Xunit;

public class FunctionBlockCycleInfoTests
{
    public class CycleNo
    {
        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Keine unsichere Zufälligkeitsstufe verwenden", Justification = "Wird nicht auf sicherheitsrelevante Weise verwendet.")]
        public void GetsCorrectCycleNo()
        {
            ulong cycleNo = 0;
            ulong GetCycleNo() => cycleNo;
            var cycleInfo = new FunctionBlockCycleInfo(GetCycleNo);
            var random = new Random();

            for (var i = 0; i < 10; i++)
            {
                cycleNo = (ulong)random.Next();

                cycleInfo.CycleNo.Should().Be(cycleNo);
            }
        }
    }
}
