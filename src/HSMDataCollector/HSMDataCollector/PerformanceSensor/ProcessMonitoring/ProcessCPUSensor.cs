﻿using HSMDataCollector.Bar;
using HSMDataCollector.Core;
using HSMDataCollector.PerformanceSensor.StandardSensor;
using HSMSensorDataObjects;
using HSMSensorDataObjects.FullDataObject;
using System;

namespace HSMDataCollector.PerformanceSensor.ProcessMonitoring
{
    internal class ProcessCPUSensor : StandardPerformanceSensorBase<double>
    {
        private const string _sensorName = "Process CPU";
        public ProcessCPUSensor(string productKey, IValuesQueue queue, string processName,
            string nodeName = TextConstants.CurrentProcessNodeName) 
            : base($"{nodeName}/{_sensorName}", "Process", "% Processor Time", processName)
        {
            InternalBar = new BarSensor<double>($"{nodeName}/{_sensorName}", productKey, queue, SensorType.DoubleBarSensor);
        }

        protected override void OnMonitoringTimerTick(object state)
        {
            try
            {
                InternalBar.AddValue(Math.Round(InternalCounter.NextValue() / Environment.ProcessorCount, 2, MidpointRounding.AwayFromZero));
            }
            catch (Exception e)
            { }
            
        }

        public override UnitedSensorValue GetLastValueNew()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            _monitoringTimer?.Dispose();
            InternalCounter?.Dispose();
            InternalBar?.Dispose();
        }

        public override CommonSensorValue GetLastValue()
        {
            return InternalBar.GetLastValue();
        }
    }
}
