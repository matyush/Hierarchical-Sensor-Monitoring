﻿using System;
using System.Collections.Generic;
using HSMServer.DataLayer.Model;

namespace HSMServer.DataLayer
{
    public interface IDatabaseClass : IDisposable
    {
        #region Products

        void AddProductToList(string productName);
        List<string> GetProductsList();
        Product GetProductInfo(string productName);
        void PutProductInfo(Product product);
        void RemoveProductInfo(string name);
        void RemoveProductFromList(string name);

        #endregion

        #region Sensors

        void RemoveSensor(SensorInfo info);
        void AddSensor(SensorInfo info);
        void WriteSensorData(SensorDataObject dataObject, string productName);
        /// <summary>
        /// Use for sensors, for which only last value must be stored
        /// </summary>
        /// <param name="dataObject"></param>
        /// <param name="productName"></param>
        void WriteOneValueSensorData(SensorDataObject dataObject, string productName);
        SensorDataObject GetLastSensorValue(string productName, string path);
        List<SensorDataObject> GetSensorDataHistory(string productName, string path, long n);
        List<string> GetSensorsList(string productName);
        void AddNewSensorToList(string productName, string path);
        void RemoveSensorFromList(string productName, string sensorName);

        #endregion

    }
}