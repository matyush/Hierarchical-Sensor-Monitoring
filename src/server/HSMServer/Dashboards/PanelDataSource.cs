﻿using HSMDatabase.AccessManager.DatabaseEntities.VisualEntity;
using HSMServer.Core;
using HSMServer.Core.Model;
using HSMServer.Datasources;
using HSMServer.Extensions;
using System;
using System.Drawing;

namespace HSMServer.Dashboards
{
    public enum PlottedProperty : byte
    {
        Value = 0,

        Bar = 50,
        Min = 51,
        Mean = 52,
        Max = 53,
        FirstValue = 54,
        LastValue = 55,
        Count = 56,

        EmaValue = 200,
        EmaMin = 201,
        EmaMean = 202,
        EmaMax = 203,
        EmaCount = 204,
    }


    public sealed class PanelDatasource : IDisposable
    {
        public BaseSensorModel Sensor { get; }

        public Guid SensorId { get; }

        public Guid Id { get; }


        public SensorDatasourceBase Source { get; private set; }


        public PlottedProperty Property { get; private set; }

        public string Label { get; private set; }

        public Color Color { get; private set; }


        internal event Action UpdateEvent;


        public PanelDatasource(BaseSensorModel sensor)
        {
            Id = Guid.NewGuid();
            SensorId = sensor.Id;

            Sensor = sensor;

            Color = Color.FromName(ColorExtensions.GenerateRandomColor());
            Property = sensor.Type.IsBar() ? PlottedProperty.Max : PlottedProperty.Value;
            Label = $"{sensor.DisplayName} ({Property})";
        }

        public PanelDatasource(BaseSensorModel sensor, PanelSourceEntity entity) : this(sensor)
        {
            Id = new Guid(entity.Id);
            SensorId = new Guid(entity.SensorId);

            Property = (PlottedProperty)entity.Property;
            Color = Color.FromName(entity.Color);
            Label = entity.Label;
        }


        public PanelDatasource BuildSource()
        {
            Source?.Dispose(); // unsubscribe prev version
            Source = DatasourceFactory.Build(Sensor, Property);

            return this;
        }


        public PanelDatasource Update(PanelSourceUpdate update)
        {
            Color = update.Color is not null ? Color.FromName(update.Color) : Color;
            Label = !string.IsNullOrEmpty(update.Name) ? update.Name : Label;

            if (update.Property is not null && Property != update.Property)
            {
                Property = update.Property.Value;
                BuildSource();
            }

            UpdateEvent?.Invoke();

            return this;
        }


        public PanelSourceEntity ToEntity() =>
            new()
            {
                Id = Id.ToByteArray(),
                SensorId = SensorId.ToByteArray(),

                Color = Color.Name,
                Label = Label,
            };

        public void Dispose()
        {
            Source?.Dispose();
            UpdateEvent = null;
        }
    }
}