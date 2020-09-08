﻿using System;
using System.Collections.Generic;
using Pulse.Matchmaker.Entities;
using Pulse.Rank.Entities;

namespace Pulse.Core.Entities
{
    public class Player
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Avatar { get; set; }
        public string Country { get; set; }

        public Division Division { get; set; }
        public int Level { get; set; }
        public double RatingMean { get; set; }
        public double RatingDeviation { get; set; }

        public List<MatchPlayer> Matches { get; set; } = new List<MatchPlayer>();
        public List<LeaderboardLog> LeaderboardLogs { get; set; } = new List<LeaderboardLog>();
        public List<PlayerSession> Sessions { get; set; } = new List<PlayerSession>();
        public List<PlayerSetting> Settings { get; set; }
        public List<PlayerBadge> Badges { get; set; } = new List<PlayerBadge>();

        public DateTime? IsBlockedUntil { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}