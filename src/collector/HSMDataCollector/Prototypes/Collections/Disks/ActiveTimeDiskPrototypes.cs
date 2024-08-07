﻿using System.Collections.Generic;
using HSMDataCollector.Alerts;
using HSMDataCollector.DefaultSensors.Windows;
using HSMSensorDataObjects.SensorRequests;


namespace HSMDataCollector.Prototypes.Collections.Disks
{
    internal sealed class WindowsActiveTimeDiskPrototype : BarDisksMonitoringPrototype
    {
        protected override string SensorNameTemplate => "Active time on {0} disk";

        protected override string DescriptionPath => WindowsActiveTimeDisk.Counter;


        public WindowsActiveTimeDiskPrototype() : base()
        {
            SensorUnit = Unit.Percents;

            Alerts = new List<BarAlertTemplate>()
            {
                AlertsFactory.IfEmaMean(AlertOperation.GreaterThanOrEqual, 80)
                             .ThenSendInstantHourlyScheduledNotification($"[$product]$path $property $operation $target$unit")
                             .AndSetIcon(AlertIcon.Warning).Build()
            };
        }
    }
}