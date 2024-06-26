﻿using HSMServer.Attributes;
using HSMServer.Core.Cache.UpdateEntities;
using HSMServer.Core.TableOfChanges;
using HSMServer.Folders;
using HSMServer.Model.Controls;
using HSMServer.Model.TreeViewModel;
using HSMServer.Notifications;
using System;
using System.ComponentModel.DataAnnotations;

namespace HSMServer.Model.ViewModel
{
    public class ProductGeneralInfoViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "{0} is required.")]
        [StringLength(100, ErrorMessage = "{0} length should be less than {1}.")]
        [UniqueValidation(ErrorMessage = "Product with the same name already exists.")]
        [RegularExpression(@"^[0-9a-zA-Z .,_\-=#:;%&*()]*$", ErrorMessage = "{0} contains forbidden characters.")]
        public string Name { get; set; }

        public string OldName { get; set; }

        public string Description { get; set; }

        public DefaultChatViewModel DefaultChats { get; set; }


        public bool IsNameChanged => Name != OldName;


        public ProductGeneralInfoViewModel() { }

        public ProductGeneralInfoViewModel(ProductNodeViewModel product)
        {
            Id = product.Id;
            Name = product.Name;
            OldName = product.Name;
            Description = product.Description;
            DefaultChats = new(product);
        }


        internal ProductUpdate ToUpdate(ProductNodeViewModel product, ITelegramChatsManager chatsManager, IFolderManager folderManager, InitiatorInfo initiator) =>
            new()
            {
                Id = Id,
                Name = IsNameChanged ? Name : null,
                Description = Description is null ? string.Empty : Description,
                DefaultChats = DefaultChats.ToUpdate(product, chatsManager, folderManager),
                Initiator = initiator,
            };
    }
}
