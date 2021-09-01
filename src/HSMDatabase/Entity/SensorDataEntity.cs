﻿using System;

namespace HSMDatabase.Entity
{
    public class SensorDataEntity
    {
        public DateTime Time { get; set; }
        public long Timestamp { get; set; }
        public string Path { get; set; }
        public byte DataType { get; set; }
        public string TypedData { get; set; }
        public DateTime TimeCollected { get; set; }
        public byte Status { get; set; }
    }
}