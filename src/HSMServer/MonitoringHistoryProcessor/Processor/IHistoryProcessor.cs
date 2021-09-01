﻿using System.Collections.Generic;
using HSMCommon.Model.SensorsData;

namespace HSMServer.MonitoringHistoryProcessor.Processor
{
    public interface IHistoryProcessor
    {
        List<SensorHistoryData> ProcessHistory(List<SensorHistoryData> uncompressedData);
    }
}