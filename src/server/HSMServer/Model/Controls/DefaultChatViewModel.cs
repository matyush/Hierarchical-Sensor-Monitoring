﻿using HSMServer.Extensions;
using HSMServer.Model.TreeViewModel;
using HSMServer.Notifications;
using System;
using System.Collections.Generic;

namespace HSMServer.Model.Controls
{
    public class DefaultChatViewModel
    {
        public HashSet<Guid> AvailableChats { get; } = new();

        public Guid SelectedChat { get; set; }


        public DefaultChatViewModel() { }

        public DefaultChatViewModel(BaseNodeViewModel node)
        {
            if (node.TryGetChats(out var availableChats))
                AvailableChats = availableChats;
        }


        public bool ChatIsSelected(TelegramChat chat) => SelectedChat == chat.Id;
    }
}
