﻿using System;
using System.Threading;

namespace HSMDataCollector.SyncQueue
{
    internal abstract class SyncQueue : IDisposable
    {
        private readonly TimeSpan _packageCollectPeriod;
        private Timer _sendTimer;

        internal bool IsStopped => _sendTimer == null;


        protected abstract string QueueName { get; }


        public event Action<string, int> SendValuesCnt;

        public event Action<string, int> OverflowCnt;


        protected SyncQueue(TimeSpan collectPeriod)
        {
            _packageCollectPeriod = collectPeriod;
        }


        public abstract void Flush();


        public void Init()
        {
            if (_sendTimer == null)
                _sendTimer = new Timer((_) => Flush(), null, _packageCollectPeriod, _packageCollectPeriod);
        }

        public void Stop()
        {
            Flush();

            _sendTimer?.Dispose();
            _sendTimer = null;
        }

        public void Dispose() => Stop();


        protected void ThrowSendValuesCount(int count)
        {
            if (count > 0)
                SendValuesCnt?.Invoke(QueueName, count);
        }

        protected void ThrowQueueOverflowCount(int count)
        {
            if (count > 0)
                OverflowCnt?.Invoke(QueueName, count);
        }
    }
}