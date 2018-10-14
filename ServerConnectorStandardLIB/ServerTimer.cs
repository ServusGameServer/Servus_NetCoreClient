using System;
using System.Threading;
using IServerConnectorStandard;

namespace ServerConnectorStandardLIB
{
    public class ServerTimer
    {

        public int Endtimer { get; private set; }
        public int TimerIntervall { get; private set; }
        public event ConnectionTimerDelegate ConnectionTimerEvent;
        private Thread _timerThread;
        private SemaphoreSlim _timerThreadSemaphore;
        private CancellationTokenSource _timerThreadCts;

        public ServerTimer()
        {
            this.Endtimer = 1000;
            this.TimerIntervall = 1000;
        }


        private  void SetTimer(int timer, int intervall)
        {
            this.Endtimer = timer;
            this.TimerIntervall = intervall;
            if (timer <= 0) this.Endtimer = 1000;
            if (intervall <= 0 || this.TimerIntervall > this.Endtimer) this.TimerIntervall = this.Endtimer;
        }
        public void StartTimer(int timer, int intervall)
        {
            this.StopTimer();
            _timerThreadCts = new CancellationTokenSource();
            _timerThreadSemaphore = new SemaphoreSlim(1);
            this.SetTimer(timer, intervall);
            this._timerThread = new Thread(this.InternalRun);
            this._timerThread.Start();
        }

        public void StopTimer()
        {
            if (this._timerThread == null || !this._timerThread.IsAlive || this._timerThreadCts == null) return;
            try
            {
                _timerThreadCts.Cancel();
                _timerThreadSemaphore.Wait(this.TimerIntervall + 500);
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                if (_timerThreadCts != null) _timerThreadCts.Dispose();
                _timerThread = null;
                _timerThreadSemaphore = null;
                _timerThreadCts = null;
            }
           
        }

        public async void StopTimerAsync()
        {
            if (this._timerThread == null || !this._timerThread.IsAlive || this._timerThreadCts == null) return;
            try
            {
                _timerThreadCts.Cancel();
                await _timerThreadSemaphore.WaitAsync(this.TimerIntervall + 500);
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                if (_timerThreadCts != null) _timerThreadCts.Dispose();
                _timerThread = null;
                _timerThreadSemaphore = null;
                _timerThreadCts = null;
            }
        }

        private void InternalRun()
        {
            try
            {
                if (_timerThreadSemaphore == null) return;
                _timerThreadSemaphore.Wait();
                int counter = (int) Math.Ceiling(((double) this.Endtimer) / ((double) this.TimerIntervall));
                for (int i = 0; i <= counter; i++)
                {
                    if (_timerThreadCts.IsCancellationRequested)
                    {
                        _timerThreadSemaphore.Release();
                        break;
                    }
                    if (ConnectionTimerEvent != null)
                    {
                        int timeLeft = this.Endtimer - (i * this.TimerIntervall);
                        if (timeLeft <= 0)
                        {
                            timeLeft = 0;
                            ConnectionTimerEvent(timeLeft);
                            break;
                        }
                        ConnectionTimerEvent(timeLeft);
                    }
                    Thread.Sleep(TimerIntervall);
                }
            }
            catch (Exception ex)
            {
                // ignored
            }
            finally
            {
                if (_timerThreadCts != null) _timerThreadCts.Dispose();
                if (_timerThreadSemaphore != null) _timerThreadSemaphore.Release();
            }
        }

    }
}
