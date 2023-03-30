﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HSMServer.ConcurrentStorage
{
    public abstract class ConcurrentStorage<ModelType, EntityType, UpdateType> : ConcurrentDictionary<Guid, ModelType>
        where ModelType : class, IServerModel<EntityType, UpdateType>
        where UpdateType : IUpdateModel
    {
        private readonly ConcurrentDictionary<string, Guid> _modelNames = new();


        protected abstract Action<EntityType> AddToDb { get; }

        protected abstract Action<EntityType> UpdateInDb { get; }

        protected abstract Action<ModelType> RemoveFromDb { get; }

        protected abstract Func<List<EntityType>> GetFromDb { get; }


        public new ModelType this[Guid id] => this.GetValueOrDefault(id);

        public ModelType this[string name] => !string.IsNullOrEmpty(name) &&
            TryGetIdByName(name, out var id) && TryGetValue(id, out var model) ? model : null;


        public event Action<ModelType> Added;
        public event Action<ModelType> Updated;
        public event Action<ModelType> Removed;


        protected abstract ModelType FromEntity(EntityType entity);

        public bool TryAdd(EntityType entity)
        {
            var model = FromEntity(entity);

            return TryAdd(model.Id, model) && _modelNames.TryAdd(model.Name, model.Id);
        }

        public Task<bool> TryAdd(ModelType model)
        {
            var result = TryAdd(model.Id, model) && _modelNames.TryAdd(model.Name, model.Id);

            if (result)
            {
                AddToDb(model.ToEntity());
                Added?.Invoke(model);
            }

            return Task.FromResult(result);
        }

        public async Task<bool> TryUpdate(UpdateType update)
        {
            var result = TryGetValue(update.Id, out var model);

            if (result)
            {
                model.Update(update);
                result &= await TryUpdate(model);
            }

            return result;
        }

        public Task<bool> TryUpdate(ModelType value)
        {
            var result = TryGetValue(value.Id, out var model);

            if (result)
            {
                UpdateInDb(model.ToEntity());
                Updated?.Invoke(model);
            }

            return Task.FromResult(result);
        }

        public Task<bool> TryRemove(Guid id)
        {
            var result = TryRemove(id, out var model) && _modelNames.TryRemove(model.Name, out _);

            if (result)
            {
                RemoveFromDb(model);
                Removed?.Invoke(model);
            }

            return Task.FromResult(result);
        }

        public bool TryGetIdByName(string name, out Guid id) => _modelNames.TryGetValue(name, out id);

        public virtual Task Initialize()
        {
            foreach (var entity in GetFromDb())
                TryAdd(entity);

            return Task.CompletedTask;
        }
    }
}