﻿using HSMServer.Authentication;
using HSMServer.Core.Cache;
using HSMServer.Core.Model;
using HSMServer.Extensions;
using HSMServer.Model.TreeViewModel;
using HSMServer.Notification.Settings;
using HSMServer.ServerConfiguration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using User = HSMServer.Model.Authentication.User;

namespace HSMServer.Notifications
{
    public sealed class TelegramBot : IAsyncDisposable
    {
        private const string ConfigurationsError = "Invalid Bot configurations.";

        private readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AddressBook _addressBook = new();
        private readonly ReceiverOptions _options = new()
        {
            AllowedUpdates = { }, // receive all update types
        };

        private readonly TelegramUpdateHandler _updateHandler;
        private readonly IUserManager _userManager;
        private readonly TelegramConfig _config;
        private readonly TreeViewModel _tree;

        private CancellationTokenSource _tokenSource = new();
        private TelegramBotClient _bot;

        private bool IsBotRunning => _bot is not null;

        private string BotToken => _config.BotToken;

        private bool AreBotMessagesEnabled => _config.IsRunning;


        internal TelegramBot(IUserManager userManager, ITreeValuesCache cache, TreeViewModel tree, TelegramConfig config)
        {
            _userManager = userManager;
            _userManager.Removed += _addressBook.RemoveAllChats;

            _config = config;
            _tree = tree;

            cache.ChangeProductEvent += RemoveProductEventHandler;
            cache.NotifyAboutChangesEvent += SendMessage;

            _updateHandler = new(_addressBook, _userManager, _tree, config);

            FillAddressBook();
        }


        public async ValueTask DisposeAsync()
        {
            _userManager.Removed -= _addressBook.RemoveAllChats;

            await StopBot();
        }

        internal string BotName => _config.BotName;

        internal string GetInvitationLink(User user) =>
            $"https://t.me/{BotName}?start={_addressBook.BuildInvitationToken(user)}";

        internal string GetStartCommandForGroup(ProductNodeViewModel product) =>
            $"{TelegramBotCommands.Start}@{BotName} {_addressBook.BuildInvitationToken(product)}";

        internal async Task<string> GetChatLink(long chatId)
        {
            var link = await _bot.CreateChatInviteLinkAsync(new ChatId(chatId), cancellationToken: _tokenSource.Token);

            return link.InviteLink;
        }

        internal void RemoveOldInvitationTokens() => _addressBook.RemoveOldTokens();

        internal void RemoveChat(INotificatable entity, long chatId)
        {
            _addressBook.RemoveChat(entity, new ChatId(chatId));
            entity.UpdateEntity(_userManager, _tree);
        }

        internal void SendTestMessage(ChatId chatId, string message)
        {
            if (IsBotRunning)
                _bot?.SendTextMessageAsync(chatId, message, cancellationToken: _tokenSource.Token);
        }

        internal async Task<string> StartBot()
        {
            if (IsBotRunning)
            {
                var message = await StopBot();
                if (!string.IsNullOrEmpty(message))
                    return message;
            }

            if (!IsValidBotConfigurations())
                return ConfigurationsError;

            _tokenSource = new CancellationTokenSource();
            _bot = new TelegramBotClient(BotToken)
            {
                Timeout = new TimeSpan(0, 5, 0) // 5 minutes timeout
            };

            try
            {
                await _bot.GetMeAsync(_tokenSource.Token);
            }
            catch (ApiRequestException exc)
            {
                _bot = null;
                return $"An error ({exc.Message}) has been occurred while starting the Bot. Please check Bot configurations. The current state of the Bot is stopped.";
            }

            await _bot.SetMyCommandsAsync(TelegramBotCommands.Commands, cancellationToken: _tokenSource.Token);

            _bot.StartReceiving(_updateHandler, _options, _tokenSource.Token);
            ThreadPool.QueueUserWorkItem(async _ => await MessageReceiver());

            return string.Empty;
        }

        internal async Task<string> StopBot()
        {
            _tokenSource.Cancel();

            var bot = _bot;
            _bot = null;

            if (bot is not null)
            {
                try
                {
                    await bot?.DeleteWebhookAsync();
                    await bot?.CloseAsync();
                }
                catch (Exception ex)
                {
                    return $"An error ({ex.Message}) has been occurred while stopping the Bot. The current state of the Bot is stopped. Try to restart Bot again.";
                }
            }

            return string.Empty;
        }

        private void FillAddressBook()
        {
            foreach (var user in _userManager.GetUsers())
                foreach (var (_, chat) in user.Notifications.Telegram.Chats)
                    _addressBook.RegisterChat(user, chat);

            foreach (var product in _tree.GetRootProducts())
                foreach (var (_, chat) in product.Notifications.Telegram.Chats)
                    _addressBook.RegisterChat(product, chat);
        }

        private static bool ShouldSendMessage(INotificatable entity, BaseSensorModel sensor, PolicyResult oldStatus, ChatId chatId)
        {
            var newStatus = sensor.Status;
            var minWebStatus = entity.Notifications.UsedTelegram.MessagesMinStatus.ToClient();

            return entity.CanSendData(sensor, chatId) && newStatus != oldStatus && sensor.State != SensorState.Muted &&
                   (newStatus.Status.ToClient() >= minWebStatus || oldStatus.Status.ToClient() >= minWebStatus);
        }

        private void SendMessage(BaseSensorModel sensor, PolicyResult oldStatus)
        {
            try
            {
                if (IsBotRunning && AreBotMessagesEnabled)
                    foreach (var (entity, chats) in _addressBook.ServerBook)
                        foreach (var (_, chat) in chats)
                            if (ShouldSendMessage(entity, sensor, oldStatus, chat.ChatId))
                            {
                                if (entity.Notifications.UsedTelegram.MessagesDelaySec > 0)
                                    chat.MessageBuilder.AddMessage(sensor, oldStatus.Status);
                                else
                                    SendMarkdownMessageAsync(chat.ChatId, MessageBuilder.GetSingleMessage(sensor));
                            }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private async Task MessageReceiver()
        {
            while (IsBotRunning)
            {
                try
                {
                    foreach (var (entity, chats) in _addressBook.ServerBook)
                    {
                        foreach (var (_, chat) in chats)
                            if (chat.MessageBuilder.ExpectedSendingTime <= DateTime.UtcNow)
                            {
                                var message = chat.MessageBuilder.GetAggregateMessage(entity.Notifications.UsedTelegram.MessagesDelaySec);
                                if (!string.IsNullOrEmpty(message))
                                    SendMarkdownMessageAsync(chat.ChatId, message);
                            }
                    }

                    if (_tokenSource.IsCancellationRequested)
                        break;

                    await Task.Delay(500, _tokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
        }

        private void SendMarkdownMessageAsync(ChatId chat, string message) =>
            _bot?.SendTextMessageAsync(chat, message, ParseMode.MarkdownV2, cancellationToken: _tokenSource.Token);

        private void RemoveProductEventHandler(ProductModel model, ActionType transaction)
        {
            if (transaction == ActionType.Delete)
            {
                var product = _addressBook.ServerBook.Keys.FirstOrDefault(e => e.Id == model.Id);

                if (product != null)
                    _addressBook.RemoveAllChats(product);
            }
        }

        private bool IsValidBotConfigurations() =>
            !string.IsNullOrEmpty(BotName) && !string.IsNullOrEmpty(BotToken);
    }
}
