using BowlingApp.API.Data;
using BowlingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BowlingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly BowlingContext _context;

        public GameController(BowlingContext context)
        {
            _context = context;
        }

        // POST: api/Game
        // Create a new game with players
        [HttpPost]
        public async Task<ActionResult<Game>> CreateGame([FromBody] List<string> playerNames)
        {
            var game = new Game { DatePlayed = DateTime.UtcNow, Players = new List<Player>() };

            foreach (var name in playerNames)
            {
                var player = new Player
                {
                    Name = name,
                    Frames = Enumerable.Range(1, 10).Select(i => new Frame { FrameNumber = i }).ToList()
                };
                game.Players.Add(player);
            }

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGame), new { id = game.Id }, game);
        }

        // GET: api/Game/5
        // Get game details and current scores
        [HttpGet("{id}")]
        public async Task<ActionResult<Game>> GetGame(int id)
        {
            var game = await _context.Games
                .Include(g => g.Players)
                    .ThenInclude(p => p.Frames)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (game == null) return NotFound();

            return game;
        }

        // POST: api/Game/5/roll
        // Record a roll for a specific player
        [HttpPost("{gameId}/roll")]
        public async Task<IActionResult> Roll(int gameId, [FromBody] RollRequest request)
        {
            // 1. Find the Player and Game (with Frames included)
            var player = await _context.Players
                .Include(p => p.Frames)
                .FirstOrDefaultAsync(p => p.Id == request.PlayerId && p.GameId == gameId);

            if (player == null) return NotFound("Player or Game not found.");

            // 2. Determine the Current Frame (the first frame that isn't "complete")
            // We order by FrameNumber to ensure we are processing in sequence.
            var currentFrame = player.Frames
                .OrderBy(f => f.FrameNumber)
                .FirstOrDefault(f => !IsFrameComplete(f));

            if (currentFrame == null) return BadRequest("Game is already finished for this player.");

            // 3. Update the Frame with the rolled pins
            if (currentFrame.Roll1 == null)
            {
                currentFrame.Roll1 = request.Pins;
            }
            else if (currentFrame.Roll2 == null)
            {
                currentFrame.Roll2 = request.Pins;
            }
            else if (currentFrame.FrameNumber == 10 && currentFrame.Roll3 == null)
            {
                // Only the 10th frame can have a 3rd roll
                currentFrame.Roll3 = request.Pins;
            }

            // 4. Update Scores (using your existing logic)
            // Note: We pass the ordered list to ensure the loop works correctly
            UpdateScores(player.Frames.OrderBy(f => f.FrameNumber).ToList());

            // 5. Save changes
            await _context.SaveChangesAsync();

            return Ok(player);
        }

        // Helper method to determine if a frame is finished
        private bool IsFrameComplete(Frame f)
        {
            // 10th Frame Logic
            if (f.FrameNumber == 10)
            {
                if (f.Roll1 + f.Roll2 >= 10) // Strike or Spare in the 10th
                    return f.Roll3 != null;
                return f.Roll2 != null;
            }

            // Standard Frame Logic
            if (f.Roll1 == 10) return true; // Strike
            return f.Roll2 != null;         // Two rolls recorded
        }

        private void UpdateScores(List<Frame> frames)
        {
            int runningTotal = 0;
            for (int i = 0; i < frames.Count; i++)
            {
                var f = frames[i];
                if (f.Roll1 == null) break; // Frame not started

                // Basic score
                int frameScore = (f.Roll1 ?? 0) + (f.Roll2 ?? 0) + (f.Roll3 ?? 0);

                // Strike Bonus
                if (f.Roll1 == 10 && i < 9)
                {
                    var next = frames[i + 1];
                    frameScore += (next.Roll1 ?? 0);
                    if (next.Roll1 == 10 && i < 8) frameScore += (frames[i + 2].Roll1 ?? 0);
                    else frameScore += (next.Roll2 ?? 0);
                }
                // Spare Bonus
                else if ((f.Roll1 + f.Roll2) == 10 && i < 9)
                {
                    frameScore += (frames[i + 1].Roll1 ?? 0);
                }

                runningTotal += frameScore;
                f.Score = runningTotal;
            }
        }
    }

    public class RollRequest
    {
        public int PlayerId { get; set; }
        public int Pins { get; set; }
    }
}
