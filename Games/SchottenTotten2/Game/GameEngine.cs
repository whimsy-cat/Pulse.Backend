using System;
using System.Collections.Generic;
using System.Net.Mime;
using Pulse.Backend;
using Pulse.Core.AppErrors;
using Pulse.Games.SchottenTotten2.Cards;
using Pulse.Games.SchottenTotten2.Wall;

namespace Pulse.Games.SchottenTotten2.Game {
  public class GameEngine {
    private readonly DataContext _context;
    private readonly CardService _cardService;
    private readonly GameConfig _config;
    public GameEngine(DataContext context) {
      _context = context;
      _cardService = new CardService();
      _config = new GameConfig();;
    }

    public GameState CreateGame() {

      // Create Deck
      var siegeCards = _cardService.CreateDeck(_config.SuitCount, _config.RankCount);
      // Draw Cards/Hands
      var attackerCards = new List<Card>();
      var defenderCards = new List<Card>();
      for (var i = 0; i < _config.HandSize; i++) {
        attackerCards.Add(_cardService.DrawCard(siegeCards));
        defenderCards.Add(_cardService.DrawCard(siegeCards));
      }
      // Setup State
      var state = new GameState() {
        IsAttackersTurn = true,
        OilCount = _config.OilCount,
        Sections = CreateSections(),
        SiegeCards = siegeCards,
        AttackerCards = attackerCards,
        DefenderCards = defenderCards,
        DiscardCards = new List<Card>(),
      };
      return state;
    }

    public GameState Retreat(GameState state, int sectionIndex) {
      var cards = state.Sections[sectionIndex].Attack;
      state.DiscardCards.AddRange(cards);
      state.Sections[sectionIndex].Attack = new List<Card>();
      return state;
    }

    private List<Section> CreateSections() {
      var leftPit = _config.GetSection("LeftPit");
      var leftWall = _config.GetSection("Wall");
      var leftTower = _config.GetSection("Tower");
      var door = _config.GetSection("Door");
      var rightTower = _config.GetSection("Tower");
      var rightWall = _config.GetSection("Wall");
      var rightPit = _config.GetSection("RightPit");
      return new List<Section>() { leftPit, leftTower, leftWall, door, rightWall, rightTower, rightPit };
    }

    public GameState UseOil(GameState state, int sectionIndex) {
      var oilIndex = _config.OilIndex;
      var cards = state.Sections[sectionIndex].Attack;
      state.DiscardCards.Add(cards[oilIndex]);
      cards.RemoveAt(oilIndex);
      state.OilCount--;

      return state;
    }

    public GameState PlayCard(GameState state, int sectionIndex, int handIndex) {
      var section = state.Sections[sectionIndex];
      var formation = state.IsAttackersTurn ? section.Attack : section.Defense;
      if (formation.Count >= section.Spaces) throw new ForbiddenException("Formation capacity reached.");
      var hand = state.IsAttackersTurn ? state.AttackerCards : state.DefenderCards;
      if (handIndex < 0 || handIndex >= hand.Count) throw new ForbiddenException("Invalid Hand Card.");

      var card = hand[handIndex];
      formation.Add(card);
      hand.RemoveAt(handIndex);
      if (CheckControl(state)) {
        state.AttackerCards = new List<Card>();
        return state;
      }
      if (state.IsAttackersTurn && state.SiegeCards.Count == 0) {
        state.DefenderCards = new List<Card>();
        return state;
      }
      if (state.SiegeCards.Count != 0) {
        hand.Add(_cardService.DrawCard(state.SiegeCards));
      }
      state.IsAttackersTurn = !state.IsAttackersTurn;

      return state;
    }

    public bool CheckControl(GameState state) {
      var extraCards = new List<Card>(state.DefenderCards);
      extraCards.AddRange(state.SiegeCards);
      extraCards = state.Sections[0].SortFormation(extraCards);
      for (var i = 0; i < state.Sections.Count; i++) {
        var section = state.Sections[i];
        if (!section.CanDefend(extraCards)) {
          if (section.IsDamaged || GetDamagedCount(state.Sections) == 3) return true;
          state.DiscardCards.AddRange(section.Attack);
          state.DiscardCards.AddRange(section.Defense);
          state.Sections[i] = _config.GetSection(section.Name, true);
        }
      }
      return false;
    }

    public int GetDamagedCount(List<Section> sections) {
      var count = 0;
      foreach (var section in sections) {
        if (section.IsDamaged) count++;
      }
      return count;
    }
  }
}