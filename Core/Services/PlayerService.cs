using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMapper;
using Flagscript.Gravatar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Pulse.Configuration;
using Pulse.Core.Entities;
using Pulse.Core.Models;
using Pulse.Matchmaker.Entities;
using Pulse.Rank.Services;

namespace Pulse.Core.Services
{
    public interface IPlayerService
    {
        PlayerModel Get(int playerId);
        PlayerModel Get(string username);
        List<string> Search(string query);
        string GetStatus(int playerId);
        void SetGravatar(int playerId, bool active);
        void SetCountry(int playerId, string country);
    }

    public class PlayerService : IPlayerService
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;
        private readonly IRatingService _ratingService;
        private readonly IDecayService _decayService;

        public PlayerService(DataContext context, IMapper mapper, IRatingService ratingService, IDecayService decayService)
        {
            _context = context;
            _mapper = mapper;
            _ratingService = ratingService;
            _decayService = decayService;
        }

        public PlayerModel Get(int playerId)
        {
            return GetPlayer(playerId);
        }

        public PlayerModel Get(string username)
        {
            return GetPlayer(null, username);
        }

        public List<string> Search(string query)
        {
            return _context.Player
                .Where(x => x.Username.Contains(query))
                .Select(x => x.Username)
                .ToList();
        }

        public string GetStatus(int playerId)
        {
            var lastMatch = _context.Match
                .Include(x => x.MatchPlayers)
                .Where(x => x.MatchPlayers.Any(x => x.PlayerId == playerId))
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefault();

            if (lastMatch != null && lastMatch.Status == MatchStatus.InProgress)
                return PlayerStatus.Playing.ToString("F");

            var lastSearch = _context.MatchmakerLog
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.AddedAt)
                .FirstOrDefault();
            if (lastSearch != null && lastSearch.ExpiredAt == null)
                return PlayerStatus.Searching.ToString("F");

            var playerInfo = _context.Player
                .Where(x => x.Id == playerId)
                .FirstOrDefault();
            if (playerInfo == null)
                return PlayerStatus.Blocked.ToString("F");

            // TODO: Add HasExtension condition
            // TODO: Add Player Level condition
            // TODO: Add IsBlocked condition
            // TODO: Add Too many abandons condition

            // Return Available
            return PlayerStatus.Available.ToString("F");
        }

        public void SetCountry(int playerId, string country = null)
        {
            var player = _context.Player.FirstOrDefault(x => x.Id == playerId);
            if (player == null) return;

            player.Country = country == null ? null : Regex.Replace(country, "[^a-zA-Z0-9 -]", ""); // alphanumeric, spaces, and dashes only
            _context.SaveChanges();
        }

        public void SetGravatar(int playerId, bool active)
        {
            var player = _context.Player.FirstOrDefault(x => x.Id == playerId);
            if (player == null) return;
            if (string.IsNullOrEmpty(player.Email)) return;
            player.Avatar = active ? "http://www.gravatar.com/avatar/" + new GravatarLibrary().GenerateEmailHash(player.Email) + ".jpg" : null;
            _context.SaveChanges();
        }

        private PlayerModel GetPlayer(int? playerId = null, string username = null)
        {
            var row = _context.Player
                .Include(x => x.Badges)
                .Include(x => x.Matches)
                .ThenInclude(x => x.Match)
                .Select(x => new
                {
                    LastMatch = x.Matches.OrderByDescending(x => x.Match.StartDate).FirstOrDefault(),
                        Player = x
                })
                .FirstOrDefault(x => string.IsNullOrEmpty(username) ? x.Player.Id == playerId : x.Player.Username == username);

            var model = _mapper.Map<PlayerModel>(row.Player);
            model.ConservativeRating = _ratingService.GetConservative(row.Player.RatingMean, row.Player.RatingDeviation);

            if (row.LastMatch != null)
            {
                var decayStep = _decayService.GetDecaySteps(row.LastMatch.DecayDays, row.LastMatch.Match.StartDate);
                model.TotalDecay = _decayService.GetDecayValues(decayStep);
                model.RegainDecay = _decayService.GetDecayValue(decayStep);
            }

            return model;
        }

    }
}