﻿using HSMServer.Core.Model;
using HSMServer.Model.TreeViewModel;
using System;
using HSMServer.Extensions;

namespace HSMServer.Model.AccessKeysViewModels
{
    public sealed class AccessKeyViewModel
    {
        public Guid Id { get; }

        public ProductNodeViewModel ParentProduct { get; }
        
        public Guid? AuthorId { get; }

        public string ExpirationDate { get; }


        public KeyState State { get; private set; }

        public string DisplayName { get; private set; }

        public string Permissions { get; private set; }

        public string NodePath => ParentProduct?.FullPath;

        public string StatusTitle { get; private set; }
        

        public string LastIP { get; private set; }
        
        public DateTime LastUseTime { get; private set; }


        internal AccessKeyViewModel(AccessKeyModel accessKey, ProductNodeViewModel parent, Guid? authorId)
        {
            Id = accessKey.Id;
            ParentProduct = parent;
            AuthorId = authorId;
            LastIP = accessKey.LastIP;
            LastUseTime = accessKey.LastUseTime;
            ExpirationDate = BuildExpiration(accessKey.ExpirationTime);

            Update(accessKey);
        }

        internal void Update(AccessKeyModel accessKey)
        {
            DisplayName = accessKey.DisplayName;
            Permissions = BuildPermissions(accessKey.Permissions);
            State = accessKey.State;
            StatusTitle = $"Status : {State}{Environment.NewLine}Expiration date : {ExpirationDate}";

            LastIP = accessKey.LastIP;
            LastUseTime = accessKey.LastUseTime;
        }

        internal static string BuildExpiration(DateTime expirationTime) =>
            expirationTime == DateTime.MaxValue
                ? nameof(AccessKeyExpiration.Unlimited)
                : expirationTime.ToDefaultFormat();

        private static string BuildPermissions(KeyPermissions permissions) =>
            permissions == AccessKeyModel.FullPermissions ? "Full" : permissions.ToString();
    }
}