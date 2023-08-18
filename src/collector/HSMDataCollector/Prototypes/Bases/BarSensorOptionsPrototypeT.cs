﻿using HSMDataCollector.Options;

namespace HSMDataCollector.Prototypes
{
    internal abstract class BarSensorOptionsPrototype<T> : BarSensorOptions
            where T : BarSensorOptions, new()
    {
        protected abstract string SensorName { get; }

        protected abstract string Category { get; }


        protected BarSensorOptionsPrototype()
        {
            Path = DefaultPrototype.BuildPath(Category, Path);
            EnableForGrafana = true;
        }


        public virtual T Get(T customOptions)
        {
            var options = DefaultPrototype.Merge<BarSensorOptions>(this, customOptions);

            options.Alerts = customOptions.Alerts ?? Alerts;

            options.PostDataPeriod = customOptions?.PostDataPeriod ?? PostDataPeriod;
            options.BarTickPeriod = customOptions?.BarTickPeriod ?? BarTickPeriod;
            options.BarPeriod = customOptions?.BarPeriod ?? BarPeriod;

            options.Precision = customOptions?.Precision ?? Precision;

            return (T)options;
        }
    }
}