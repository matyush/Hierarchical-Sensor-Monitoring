﻿using HSMDatabase.AccessManager.DatabaseEntities;
using HSMServer.Core.Cache.UpdateEntities;
using HSMServer.Core.Extensions;
using HSMServer.Core.Journal;
using HSMServer.Core.Model.NodeSettings;
using HSMServer.Core.Model.Policies;
using HSMServer.Core.TableOfChanges;
using System;
using System.Runtime.CompilerServices;

namespace HSMServer.Core.Model
{
    public abstract class BaseNodeModel : IChangesEntity
    {
        private readonly PolicyEntity _ttlEntity;


        internal ChangeInfoTable ChangeTable { get; }

        public abstract BaseSettingsCollection Settings { get; }

        public abstract PolicyCollectionBase Policies { get; }


        public Guid Id { get; }

        public Guid? AuthorId { get; }

        public DateTime CreationDate { get; }


        public ProductModel Parent { get; private set; }


        public string DisplayName { get; protected set; }

        public string Description { get; private set; }


        public bool IsRoot => Parent is null;

        public string FullPath => IsRoot ? $"{DisplayName}" : $"{Parent.FullPath}/{DisplayName}";

        public string Path => IsRoot ? string.Empty : $"{Parent.Path}/{DisplayName}";

        public string RootProductName => Parent?.RootProductName ?? DisplayName;

        public ProductModel Root => Parent?.Root ?? (ProductModel)this;


        public event Action<JournalRecordModel> ChangesHandler;


        protected BaseNodeModel()
        {
            ChangeTable = new ChangeInfoTable(() => FullPath);

            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;

            Policies.Attach(this);
        }

        protected BaseNodeModel(string name, Guid? authorId) : this()
        {
            DisplayName = name;
            AuthorId = authorId ?? Guid.Empty;
        }

        protected BaseNodeModel(BaseNodeEntity entity) : this()
        {
            _ttlEntity = entity.TTLPolicy;

            Id = Guid.Parse(entity.Id);
            AuthorId = Guid.TryParse(entity.AuthorId, out var authorId) ? authorId : Guid.Empty;
            CreationDate = new DateTime(entity.CreationDate);

            DisplayName = entity.DisplayName;
            Description = entity.Description;

            ChangeTable.FromEntity(entity.ChangeTable);
        }


        internal abstract bool CheckTimeout();

        protected abstract void UpdateTTL(PolicyUpdate update);


        internal BaseNodeModel AddParent(ProductModel parent)
        {
            Parent = parent;

            Settings.SetParentSettings(parent.Settings);
            Policies.BuildDefault(this, _ttlEntity); //need for correct calculating $product and $path properties

            return this;
        }

        protected void Update(BaseNodeUpdate update)
        {
            Description = UpdateProperty(Description, update.Description ?? Description, update.Initiator);

            Settings.Update(update, ChangeTable);

            if (update.TTLPolicy is not null && ChangeTable.TtlPolicy.CanChange(update.Initiator))
            {
                UpdateTTL(update.TTLPolicy);
                ChangeTable.TtlPolicy.SetUpdate(update.Initiator);
            }

            CheckTimeout();
        }


        protected T UpdateProperty<T>(T oldValue, T newValue, InitiatorInfo initiator, [CallerArgumentExpression(nameof(oldValue))] string propName = "", bool? forced = null, SensorUpdate update = null, BaseSensorModel oldModel = null)
        {
            var infoNode = ChangeTable.Properties[propName];
            var forceUpdate = forced ?? initiator.IsForceUpdate;

            if (!forceUpdate && !infoNode.CanChange(initiator))
                return oldValue;

            if (newValue is not null && !newValue.Equals(oldValue ?? newValue))
            {
                ChangesHandler?.Invoke(new JournalRecordModel(Id, initiator)
                {
                    Enviroment = "General info update",
                    OldValue = GetSpecificValue(oldValue),
                    NewValue = GetSpecificValue(newValue),

                    PropertyName = propName,
                    Path = FullPath,
                });

                infoNode.SetUpdate(initiator);
            }

            return newValue ?? oldValue;

            string GetSpecificValue(T value)
            {
                if (update is null || oldModel is null)
                    return value.ToString();

                return value switch
                {
                    SensorState state => state is SensorState.Muted ? $"{state} until {(update.EndOfMutingPeriod ?? oldModel.EndOfMuting)?.ToDefaultFormat()}" : state.ToString(),
                    _ => value.ToString()
                };
            }
        }
    }
}