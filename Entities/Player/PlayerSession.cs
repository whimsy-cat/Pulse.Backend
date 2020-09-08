﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Pulse.Entities.Player
{
    public class PlayerSession
    {
        public int Id { get; set; }
        public Player Player { get; set; }
        public int PlayerId { get; set; }
        public string RefreshToken { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}