﻿using System.Collections.Generic;

namespace HSMDatabase.AccessManager.DatabaseEntities
{
    public abstract record BaseNodeEntity
    {
        public Dictionary<string, TimeIntervalEntity> Settings { get; init; } = [];

        public List<string> Policies { get; init; } = [];


        public PolicyDestinationSettingsEntity DefaultChatsSettings { get; init; } = new();


        public required string Id { get; init; }


        public string AuthorId { get; init; }

        public long CreationDate { get; init; }


        public string DisplayName { get; init; }

        public string Description { get; init; }



        public ChangeInfoTableEntity ChangeTable { get; init; }

        public PolicyEntity TTLPolicy { get; init; }
    }


    public sealed record PolicyDestinationSettingsEntity
    {
        public Dictionary<string, string> Chats { get; init; } = [];

        public byte Mode { get; init; }
    }
}