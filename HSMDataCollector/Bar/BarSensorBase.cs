﻿using System.Collections.Generic;
using System.Threading;
using HSMDataCollector.Base;

namespace HSMDataCollector.Bar
{
    public abstract class BarSensorBase<T> : SensorBase where T: struct
    {
        private Timer _barTimer;
        protected object _syncRoot;
        protected int ValuesCount = 0;
        protected List<T> ValuesList;
        protected T Min;
        protected T Max;
        protected T Mean;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        /// <param name="productKey"></param>
        /// <param name="collectPeriod">One bar contains data for the given period. 5000 is 5 seconds</param>
        protected BarSensorBase(string path, string productKey, string serverAddress,
            int collectPeriod = 60000)
            : base(path, productKey, serverAddress)
        {
            _syncRoot = new object();
            ValuesList = new List<T>();
            _barTimer = new Timer(SendDataTimer, null, collectPeriod, collectPeriod);
        }

        //public event EventHandler<BarCollectedEventArgs<T>> BarCollected;

        protected abstract void SendDataTimer(object state);
    }
}
