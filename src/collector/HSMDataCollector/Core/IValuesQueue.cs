﻿using HSMSensorDataObjects.FullDataObject;

namespace HSMDataCollector.Core
{
    public interface IValuesQueue
    {
        void EnqueueData(UnitedSensorValue value);
    }
}