﻿using HSMServer.Core.Model;
using HSMServer.Dashboards;
using System;
using System.Numerics;

namespace HSMServer.Datasources
{
    public abstract class BaseLineDatasource<TValue, TProp, TChart> : SensorDatasourceBase
        where TValue : BaseValue
        where TChart : INumber<TChart>
    {
        private BaseChartValue<TChart> _lastVisibleValue;
        protected Func<TValue, TProp> _getPropertyFactory;


        protected override ChartType AggregatedType => ChartType.Line;

        protected override ChartType NormalType => ChartType.Line;


        internal override SensorDatasourceBase AttachSensor(BaseSensorModel sensor, PlottedProperty plotProperty)
        {
            base.AttachSensor(sensor, plotProperty);

            _getPropertyFactory = GetPropertyFactory();

            return this;
        }


        protected override void AddVisibleValue(BaseValue rawValue)
        {
            if (rawValue is TValue value)
            {
                _lastVisibleValue = new LineChartValue<TChart>(rawValue, ToChartValue(value));

                AddVisibleToLast(_lastVisibleValue);
            }
        }

        protected override void ApplyToLast(BaseValue rawValue)
        {
            if (rawValue is TValue value)
                _lastVisibleValue.Apply(ToChartValue(value));
        }


        protected abstract Func<TValue, TProp> GetPropertyFactory();

        protected abstract TChart ConvertToChartType(TProp value);


        protected Exception BuildException() => new($"Unsupport cast property for {typeof(TValue).Name} {_plotProperty} from {typeof(TProp).Name} to {typeof(TChart).Name}");


        private TChart ToChartValue(TValue value) => ConvertToChartType(_getPropertyFactory(value));
    }
}