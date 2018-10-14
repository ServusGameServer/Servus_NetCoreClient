using System;
using System.Collections.Generic;
using ServerConnectorStandardLIB;
using Xunit;
using Xunit.Abstractions;

namespace ServerConnectorTests
{
    public class TimerTest
    {
        private readonly ITestOutputHelper _output;

        public TimerTest(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1000, 10000)]
        [InlineData(1, 20000)]
        [InlineData(10000, 25000)]
        [InlineData(100000, 250000)]
        public void TimmerSettingNormal(int intervall, int endTimer)
        {
            ServerTimer tmpTimer = new ServerTimer();
            tmpTimer.StartTimer(endTimer, intervall);
            Assert.Equal(intervall, tmpTimer.TimerIntervall);
            Assert.Equal(endTimer, tmpTimer.Endtimer);
        }
        [Theory]
        [InlineData(-1, 10000)]
        [InlineData(0, 20000)]
        [InlineData(999, 999)]
        [InlineData(350000, 250000)]
        public void TimmerSettingChangeIntervall(int intervall, int endTimer)
        {
            ServerTimer tmpTimer = new ServerTimer();
            tmpTimer.StartTimer(endTimer, intervall);
            Assert.Equal(endTimer, tmpTimer.TimerIntervall);
            Assert.Equal(endTimer, tmpTimer.Endtimer);
        }
        [Theory]
        [InlineData(1000, -1)]
        [InlineData(499, -20000)]
        [InlineData(999, 0)]
        public void TimmerSettingChangeEndTimer(int intervall, int endTimer)
        {
            ServerTimer tmpTimer = new ServerTimer();
            tmpTimer.StartTimer(endTimer, intervall);
            Assert.Equal(intervall, tmpTimer.TimerIntervall);
            Assert.Equal(1000, tmpTimer.Endtimer);
        }

        [Theory]
        [InlineData(-1, -1)]
        [InlineData(-100, -10000)]
        [InlineData(0, 0)]
        [InlineData(2000, -2000)]
        public void TimmerSettingChangeCombined(int intervall, int endTimer)
        {
            ServerTimer tmpTimer = new ServerTimer();
            tmpTimer.StartTimer(endTimer, intervall);
            Assert.Equal(1000, tmpTimer.TimerIntervall);
            Assert.Equal(1000, tmpTimer.Endtimer);
        }

        [Theory]
        [InlineData(10, 100)]
        [InlineData(1, 100)]
        [InlineData(5, 200)]
        [InlineData(1000, 1000)]
        [InlineData(100, 1001)]
        public void TimmerSettingRunNormal(int intervall, int endTimer)
        {
            ServerTimer tmpTimer = new ServerTimer();
            List<int> receivedEvents = new List<int>();
            tmpTimer.ConnectionTimerEvent += delegate (int actTimming)
            {
                receivedEvents.Add(actTimming);
            };
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(endTimer + 2500);
            _output.WriteLine(String.Join(", ", receivedEvents.ToArray()));
            int counter = (int)Math.Ceiling(((double)endTimer) / ((double)intervall)) + 1;
            Assert.Equal(counter, receivedEvents.Count);
            Assert.Equal(0, receivedEvents[receivedEvents.Count - 1]);
            Assert.Equal(endTimer, receivedEvents[0]);
            if (intervall != endTimer) Assert.Equal(endTimer - intervall, receivedEvents[1]);
        }
        [Theory]
        [InlineData(100, 1000)]
        [InlineData(1000, 1000)]
        [InlineData(50, 2000)]
        public void TimmerSettingRunNormalExactTiming(int intervall, int endTimer)
        {
            ServerTimer tmpTimer = new ServerTimer();
            List<int> receivedEvents = new List<int>();
            tmpTimer.ConnectionTimerEvent += delegate (int actTimming)
            {
                receivedEvents.Add(actTimming);
            };
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(endTimer + 100);
            _output.WriteLine(String.Join(", ", receivedEvents.ToArray()));
            int counter = (int)Math.Ceiling(((double)endTimer) / ((double)intervall)) + 1;
            Assert.Equal(counter, receivedEvents.Count);
            Assert.Equal(0, receivedEvents[receivedEvents.Count - 1]);
            Assert.Equal(endTimer, receivedEvents[0]);
            if (intervall != endTimer) Assert.Equal(endTimer - intervall, receivedEvents[1]);
        }
        [Theory]
        [InlineData(100, 1000, 2, 50)]
        [InlineData(100, 1000, 10, 50)]
        public void TimmerSettingRunStop(int intervall, int endTimer, int stopCount, int stopOffset)
        {
            ServerTimer tmpTimer = new ServerTimer();
            List<int> receivedEvents = new List<int>();
            tmpTimer.ConnectionTimerEvent += delegate (int actTimming)
            {
                receivedEvents.Add(actTimming);
            };
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(intervall * stopCount + stopOffset);
            tmpTimer.StopTimer();
            System.Threading.Thread.Sleep(intervall * 2 + stopOffset);
            _output.WriteLine(String.Join(", ", receivedEvents.ToArray()));
            int counter = (int)Math.Ceiling(((double)endTimer) / ((double)intervall)) + 1;
            Assert.Equal(stopCount + 1, receivedEvents.Count);
            Assert.Equal(endTimer - (intervall * stopCount), receivedEvents[receivedEvents.Count - 1]);
            Assert.Equal(endTimer, receivedEvents[0]);
        }

        [Theory]
        [InlineData(100, 1000, 2, 50)]
        [InlineData(100, 1000, 10, 50)]
        public void TimmerSettingRunRunStop(int intervall, int endTimer, int stopCount, int stopOffset)
        {
            ServerTimer tmpTimer = new ServerTimer();
            List<int> receivedEvents = new List<int>();
            tmpTimer.ConnectionTimerEvent += delegate (int actTimming)
            {
                receivedEvents.Add(actTimming);
            };
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(intervall * stopCount + stopOffset);
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(intervall * stopCount + stopOffset);
            tmpTimer.StopTimer();
            System.Threading.Thread.Sleep(intervall * 2 + stopOffset);
            _output.WriteLine(String.Join(", ", receivedEvents.ToArray()));
            int counter = (int)Math.Ceiling(((double)endTimer) / ((double)intervall)) + 1;
            Assert.Equal(2 * (stopCount + 1), receivedEvents.Count);
            Assert.Equal(endTimer - (intervall * stopCount), receivedEvents[receivedEvents.Count - 1]);
        }
        [Theory]
        [InlineData(100, 1000, 2, 50)]
        [InlineData(100, 1000, 10, 50)]
        public void TimmerSettingRunStopAsync(int intervall, int endTimer, int stopCount, int stopOffset)
        {
            ServerTimer tmpTimer = new ServerTimer();
            List<int> receivedEvents = new List<int>();
            tmpTimer.ConnectionTimerEvent += delegate (int actTimming)
            {
                receivedEvents.Add(actTimming);
            };
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(intervall * stopCount + stopOffset);
            tmpTimer.StopTimerAsync();
            System.Threading.Thread.Sleep(intervall * 2 + stopOffset);
            _output.WriteLine(String.Join(", ", receivedEvents.ToArray()));
            int counter = (int)Math.Ceiling(((double)endTimer) / ((double)intervall)) + 1;
            Assert.Equal(stopCount + 1, receivedEvents.Count);
            Assert.Equal(endTimer - (intervall * stopCount), receivedEvents[receivedEvents.Count - 1]);
            Assert.Equal(endTimer, receivedEvents[0]);
        }

        [Theory]
        [InlineData(100, 1000, 2, 50)]
        [InlineData(100, 1000, 10, 50)]
        public void TimmerSettingRunRunStopAsync(int intervall, int endTimer, int stopCount, int stopOffset)
        {
            ServerTimer tmpTimer = new ServerTimer();
            List<int> receivedEvents = new List<int>();
            tmpTimer.ConnectionTimerEvent += delegate (int actTimming)
            {
                receivedEvents.Add(actTimming);
            };
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(intervall * stopCount + stopOffset);
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(intervall * stopCount + stopOffset);
            tmpTimer.StopTimerAsync();
            System.Threading.Thread.Sleep(intervall * 2 + stopOffset);
            _output.WriteLine(String.Join(", ", receivedEvents.ToArray()));
            int counter = (int)Math.Ceiling(((double)endTimer) / ((double)intervall)) + 1;
            Assert.Equal(2 * (stopCount + 1), receivedEvents.Count);
            Assert.Equal(endTimer - (intervall * stopCount), receivedEvents[receivedEvents.Count - 1]);
        }

        [Theory]
        [InlineData(100, 1000, 2, 50)]
        [InlineData(100, 1000, 10, 50)]
        public void TimmerSettingRunRunStopStop(int intervall, int endTimer, int stopCount, int stopOffset)
        {
            ServerTimer tmpTimer = new ServerTimer();
            List<int> receivedEvents = new List<int>();
            tmpTimer.ConnectionTimerEvent += delegate (int actTimming)
            {
                receivedEvents.Add(actTimming);
            };
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(intervall * stopCount + stopOffset);
            tmpTimer.StopTimer();
            System.Threading.Thread.Sleep(intervall * 2 + stopOffset);
            tmpTimer.StopTimer();
            _output.WriteLine(String.Join(", ", receivedEvents.ToArray()));
            int counter = (int)Math.Ceiling(((double)endTimer) / ((double)intervall)) + 1;
            Assert.Equal(stopCount + 1, receivedEvents.Count);
            Assert.Equal(endTimer - (intervall * stopCount), receivedEvents[receivedEvents.Count - 1]);
        }
        [Theory]
        [InlineData(100, 1000, 2, 50)]
        [InlineData(100, 1000, 10, 50)]
        public void TimmerSettingRunRunStopStopAsync(int intervall, int endTimer, int stopCount, int stopOffset)
        {
            ServerTimer tmpTimer = new ServerTimer();
            List<int> receivedEvents = new List<int>();
            tmpTimer.ConnectionTimerEvent += delegate (int actTimming)
            {
                receivedEvents.Add(actTimming);
            };
            tmpTimer.StartTimer(endTimer, intervall);
            System.Threading.Thread.Sleep(intervall * stopCount + stopOffset);
            tmpTimer.StopTimerAsync();
            System.Threading.Thread.Sleep(intervall * 2 + stopOffset);
            tmpTimer.StopTimerAsync();
            _output.WriteLine(String.Join(", ", receivedEvents.ToArray()));
            int counter = (int)Math.Ceiling(((double)endTimer) / ((double)intervall)) + 1;
            Assert.Equal(stopCount + 1, receivedEvents.Count);
            Assert.Equal(endTimer - (intervall * stopCount), receivedEvents[receivedEvents.Count - 1]);
        }


    }
}
