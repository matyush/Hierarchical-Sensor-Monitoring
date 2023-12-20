﻿using HSMDatabase.AccessManager.DatabaseEntities;
using HSMServer.Core.Extensions;
using HSMServer.Core.Model.Policies;
using System.Collections.Generic;
using System.Linq;

namespace HSMServer.Core.Model
{
    public abstract class BaseSensorModel<T> : BaseSensorModel where T : BaseValue, new()
    {
        public override SensorPolicyCollection<T> Policies { get; }

        internal override ValuesStorage<T> Storage { get; }


        protected BaseSensorModel(SensorEntity entity) : base(entity) { }


        internal override bool TryAddValue(BaseValue value)
        {
            if (value?.IsTimeout ?? false)
            {
                Storage.AddValueBase((T)value);
                ReceivedNewValue?.Invoke(value);

                return true;
            }

            if (Statistics.HasEma() && value is T valueT)
                value = Storage.CalculateStatistics(valueT);

            var isLastValue = Storage.LastValue is null || value.Time >= Storage.LastValue.Time;
            var canStore = Policies.TryValidate(value, out valueT, isLastValue);

            if (canStore)
            {
                var isNewValue = true;

                if (AggregateValues)
                    isNewValue &= !Storage.TryAggregateValue(valueT);
                else if (IsSingleton)
                    isNewValue &= Storage.TryAddAsSingleton(valueT);
                else
                    Storage.AddValue(valueT);

                Policies.SensorTimeout(valueT);

                if (isNewValue)
                    ReceivedNewValue?.Invoke(valueT);
            }

            return canStore;
        }

        internal override bool TryUpdateLastValue(BaseValue value, bool changeLast = false)
        {
            if (Storage.TryChangeLastValue(value, changeLast))
            {
                var isLastValue = Storage.LastValue is null || value.Time >= Storage.LastValue.Time;
                var canStore = Policies.TryValidate(value, out _, isLastValue);

                ReceivedNewValue?.Invoke(value);

                return canStore;
            }

            return false;
        }


        internal override bool CheckTimeout() => Policies.SensorTimeout(LastValue);

        internal override void AddDbValue(byte[] bytes)
        {
            var dbValue = Convert(bytes);

            if (dbValue.IsTimeout)
                Storage.AddValue((T)dbValue);
            else if (Policies.TryValidate(dbValue, out var valueT))
            {
                Storage.AddValue(valueT);
                Policies.SensorTimeout(valueT);
            }
        }


        internal override IEnumerable<BaseValue> Convert(List<byte[]> pages) => pages.Select(Convert);

        internal override BaseValue Convert(byte[] bytes) => bytes.ToValue<T>();
    }
}