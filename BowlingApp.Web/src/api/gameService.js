// =============================================================================================
// STUDENT IMPLEMENTATION: API Calls
// =============================================================================================

const API_BASE_URL = "http://localhost:5000/api/game";

/**
 * Creates a new game session with a list of players.
 * POST /api/game
 */
export const createGame = async (playerNames) => {
    const response = await fetch(API_BASE_URL, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(playerNames)
    });

    if (!response.ok) {
        throw new Error(`Failed to create game: ${response.statusText}`);
    }

    return await response.json();
};

/**
 * Retrieves the current state of a specific game.
 * GET /api/game/{gameId}
 */
export const getGame = async (gameId) => {
    const response = await fetch(`${API_BASE_URL}/${gameId}`);

    if (!response.ok) {
        throw new Error(`Failed to fetch game ${gameId}: ${response.statusText}`);
    }

    return await response.json();
};

/**
 * Records a ball roll for a specific player.
 * POST /api/game/{gameId}/roll
 */
export const rollBall = async (gameId, playerId, pins) => {
    const response = await fetch(`${API_BASE_URL}/${gameId}/roll`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({ playerId, pins })
    });

    if (!response.ok) {
        // Tip: .NET backends often return specific error messages in the body
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.message || `Failed to record roll: ${response.statusText}`);
    }

    return await response.json();
};