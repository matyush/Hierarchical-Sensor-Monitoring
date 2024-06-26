﻿using HSMServer.Authentication;
using HSMServer.Filters.FolderRoleFilters;
using HSMServer.Folders;
using HSMServer.Model.Authentication;
using HSMServer.Model.Folders;
using HSMServer.Model.Folders.ViewModels;
using HSMServer.Model.TreeViewModel;
using HSMServer.Model.ViewModel;
using HSMServer.Notifications;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HSMServer.Controllers
{
    public class FoldersController : BaseController
    {
        private readonly ITelegramChatsManager _telegramChatsManager;
        private readonly IFolderManager _folderManager;
        private readonly TreeViewModel _tree;


        public FoldersController(IFolderManager folderManager, IUserManager userManager,
            TreeViewModel treeViewModel, ITelegramChatsManager chatsManager) : base(userManager)
        {
            _telegramChatsManager = chatsManager;
            _folderManager = folderManager;
            _tree = treeViewModel;
        }


        [HttpGet]
        [FolderRoleFilterByFolderId(nameof(folderId), ProductRoleEnum.ProductManager)]
        public IActionResult EditFolder(Guid? folderId)
        {
            return folderId == null
                ? View(new EditFolderViewModel(BuildFolderProducts()))
                : View(BuildEditFolder(folderId.Value));
        }

        [HttpPost]
        [FolderRoleFilterByEditModel(nameof(editFolder), ProductRoleEnum.ProductManager)]
        public async Task<IActionResult> EditFolder(EditFolderViewModel editFolder)
        {
            if (!ModelState.IsValid)
            {
                var invalidFolder = BuildEditFolder(editFolder.Id);

                invalidFolder.Name = editFolder.Name;
                invalidFolder.Color = editFolder.Color;
                invalidFolder.Description = editFolder.Description;
                invalidFolder.Products = BuildFolderProducts(invalidFolder.Products?.SelectedProducts);

                return View(nameof(EditFolder), invalidFolder);
            }

            var folder = _folderManager[editFolder.Id];
            var oldProducts = new Dictionary<Guid, ProductNodeViewModel>(folder.Products);

            folder.Products.Clear();
            foreach (var product in editFolder.GetFolderProducts(_tree))
                folder.Products.Add(product.Id, product);

            if (await _folderManager.TryUpdate(editFolder.ToFolderUpdate(CurrentInitiator)))
            {
                foreach (var (productId, _) in oldProducts.Except(folder.Products))
                    await _folderManager.RemoveProductFromFolder(productId, folder.Id, CurrentInitiator);

                foreach (var (productId, _) in folder.Products.Except(oldProducts))
                    await _folderManager.AddProductToFolder(productId, folder.Id, CurrentInitiator);
            }

            return View(nameof(EditFolder), BuildEditFolder(folder.Id));
        }

        [HttpPost]
        [FolderRoleFilterByEditModel(nameof(editFolder), ProductRoleEnum.ProductManager)]
        public async Task<IActionResult> AddFolder(EditFolderViewModel editFolder)
        {
            if (!ModelState.IsValid)
            {
                editFolder.Products = BuildFolderProducts(editFolder.Products?.SelectedProducts);

                return View(nameof(EditFolder), editFolder);
            }

            await _folderManager.TryAdd(editFolder.ToFolderAdd(CurrentUser, CurrentInitiator, _tree), out var newFolder);

            return RedirectToAction(nameof(EditFolder), new { folderId = newFolder.Id });
        }

        [HttpPost]
        [FolderRoleFilterByFolderId(nameof(folderId), ProductRoleEnum.ProductManager)]
        public Task RemoveFolder(Guid folderId) => _folderManager.TryRemove(new(folderId, CurrentInitiator));


        [HttpPost]
        [FolderRoleFilterByEditSettings(nameof(folderSettings), ProductRoleEnum.ProductManager)]
        public async Task<IActionResult> EditSettings(FolderSettingsViewModel folderSettings)
        {
            var update = new FolderUpdate()
            {
                Id = folderSettings.Id,
                TTL = folderSettings.ExpectedUpdateInterval,
                KeepHistory = folderSettings.SavedHistoryPeriod,
                SelfDestroy = folderSettings.SelfDestoryPeriod,
                Initiator = CurrentInitiator
            };

            await _folderManager.TryUpdate(update);

            return PartialView("_Settings", new FolderSettingsViewModel(_folderManager[update.Id]));
        }


        [HttpPost]
        public async Task<IActionResult> UpdateTelegram(FolderTelegramViewModel viewModel)
        {
            var connectedChats = viewModel.ConnectedChatIds?.Where(ch => ch != Guid.Empty).ToList() ?? new();
            if (viewModel.NewChats is not null)
                connectedChats.AddRange(viewModel.NewChats);

            var update = new FolderUpdate()
            {
                Id = viewModel.FolderId,
                DefaultChats = viewModel.DefaultChats,
                TelegramChats = new HashSet<Guid>(connectedChats),
                Initiator = CurrentInitiator
            };

            await _folderManager.TryUpdate(update);

            return PartialView("_TelegramChats", BuildFolderTelegram(_folderManager[viewModel.FolderId]));
        }


        [HttpGet]
        public IActionResult ResetUsers(Guid folderId) => GetUsersPartialView(_folderManager[folderId]);

        [HttpPost]
        [FolderRoleFilterByUserRights(nameof(model), ProductRoleEnum.ProductManager)]
        public async Task<IActionResult> AddUserRole([FromBody] UserRightViewModel model)
        {
            var user = _userManager[model.UserId];
            var folder = _folderManager[model.EntityId];

            user.FoldersRoles.Add(folder.Id, model.Role);

            foreach (var productId in folder.Products.Keys)
                if (!user.IsUserProduct(productId))
                    user.ProductsRoles.Add((productId, model.Role));

            if (await _userManager.UpdateUser(user))
                folder.UserRoles.Add(user, model.Role);

            return GetUsersPartialView(folder);
        }

        [HttpPost]
        [FolderRoleFilterByUserRights(nameof(model), ProductRoleEnum.ProductManager)]
        public async Task<IActionResult> EditUserRole([FromBody] UserRightViewModel model)
        {
            var user = _userManager[model.UserId];
            var folder = _folderManager[model.EntityId];

            if (user.FoldersRoles.TryGetValue(folder.Id, out var oldUserRole))
            {
                user.FoldersRoles[folder.Id] = model.Role;

                foreach (var productId in folder.Products.Keys)
                    if (user.ProductsRoles.Remove((productId, oldUserRole)))
                        user.ProductsRoles.Add((productId, model.Role));

                if (await _userManager.UpdateUser(user))
                    folder.UserRoles[user] = model.Role;
            }

            return GetUsersPartialView(folder);
        }

        [HttpPost]
        [FolderRoleFilterByUserRights(nameof(model), ProductRoleEnum.ProductManager)]
        public async Task<IActionResult> RemoveUserRole([FromBody] UserRightViewModel model)
        {
            var user = _userManager[model.UserId];
            var folder = _folderManager[model.EntityId];

            user.FoldersRoles.Remove(folder.Id);

            foreach (var productId in folder.Products.Keys)
                user.ProductsRoles.Remove((productId, model.Role));

            // TODO: also call user.Notifications.RemoveSensor for sensors that are not available for user 

            if (await _userManager.UpdateUser(user))
                folder.UserRoles.Remove(user);

            return GetUsersPartialView(folder);
        }

        private PartialViewResult GetUsersPartialView(FolderModel folder) => PartialView("_Users", BuildFolderUsers(folder));


        private EditFolderViewModel BuildEditFolder(Guid folderId)
        {
            var folder = _folderManager[folderId];

            return new(folder, BuildFolderProducts(), BuildFolderUsers(folder), BuildFolderTelegram(folder));
        }

        private FolderProductsViewModel BuildFolderProducts(List<string> selectedProducts = null)
        {
            var availableProducts = _tree.GetUserProducts(CurrentUser).Where(p => p.FolderId is null).ToList();

            return new(availableProducts, selectedProducts);
        }

        private FolderUsersViewModel BuildFolderUsers(FolderModel folder) =>
            new(folder, _userManager.GetUsers(u => !u.IsAdmin));

        private FolderTelegramViewModel BuildFolderTelegram(FolderModel folder)
        {
            var chats = _telegramChatsManager.GetValues();

            return new(folder, chats);
        }
    }
}
