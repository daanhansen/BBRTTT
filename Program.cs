using BattleBitAPI;
using BattleBitAPI.Server;
using BattleBitAPI.Common;
using System.Text.Json;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

class Program
{

    TTTGame game = new TTTGame();
    private static List<MyPlayer> spawnedPlayers = new List<MyPlayer>();
    private static bool isCountdownInProgress = false;

    public static void Main(string[] args)
    {
        var listener = new ServerListener<MyPlayer>();

        listener.OnGameServerConnected += HandleGameServerConnected;
        listener.OnGameServerDisconnected += HandleGameServerDisconnected;
        listener.OnPlayerConnected += HandlePlayerConnected;
        listener.OnPlayerDisconnected += HandlePlayerDisconnected;
        listener.OnPlayerSpawning += HandlePlayerSpawning;
        listener.OnPlayerSpawned += HandlePlayerSpawned;
        listener.OnTick += HandleGameTick;
        listener.OnAPlayerKilledAnotherPlayer += HandlePlayerKilledAnotherPlayer;

        game.InProgress = false;
        game.TraitorsLeft = 0;

        listener.Start(29294);
        Thread.Sleep(-1);
    }

    private static async Task HandleGameServerConnected(GameServer server)
    {
        
    }

    private static async Task HandleGameServerDisconnected(GameServer server)
    {
        
    }

    private async static Task HandleGameTick()
    {
        
    }

    private static async Task HandlePlayerConnected(MyPlayer player)
    {

    }

    private static async Task HandlePlayerDisconnected(MyPlayer player)
    {

    }

    private async static Task<PlayerSpawnRequest> HandlePlayerSpawning(MyPlayer player, PlayerSpawnRequest request)
    {   
        return request;
    }

    private async static Task HandlePlayerSpawned(MyPlayer player)
    {
        if (!player.isAlive) {
            player.Kill();
            player.Message("Please wait for this game to end, you can spectate in the meantime.")
        }
        if (!spawnedPlayers.Contains(player))
        {
            spawnedPlayers.Add(player);
        }
        if (spawnedPlayers.Count >= 8 && !game.InProgress && !isCountdownInProgress)
        {
            isCountdownInProgress = true;
            await StartCountdown();
            isCountdownInProgress = false;
        } else {
            SayToChat($"[GAME] {8 - spawnedPlayers.Count} Players needed to start a new game.");
        }
    }

    private async static Task HandlePlayerKilledAnotherPlayer(MyPlayer killer, Vector3 killerPos, MyPlayer victim, Vector3 victimPos, string toolUsed)
    {
        victim.isAlive = false;
        if (spawnedPlayers.Contains(victim))
        {
            spawnedPlayers.Remove(victim);
        }
        CheckGameEnd();
        
    }

    private static async Task StartCountdown()
    {
        const int countdownDuration = 60;
        var countdownTimer = new CountdownTimer(countdownDuration);

        using (var countdownCancellationTokenSource = new CancellationTokenSource())
        {
            SayToChat($"[GAME] Enough players reached. {countdownDuration} seconds to start..");
            countdownTimer.StartAsync(secondsRemaining =>
            {
                SayToChat($"[GAME] {secondsRemaining} seconds to start..");
            }, countdownCancellationTokenSource.Token);

            await Task.Delay(countdownDuration * 1000, countdownCancellationTokenSource.Token);

            SayToChat($"[GAME] Starting game!");
            countdownTimer.Cancel();
            StartGame();
        }
    }

    private static void StartGame()
    {
        game.InProgress = true;
        game.TraitorsLeft = CalculateInitialTraitors();

        Random random = new Random();
        List<MyPlayer> potentialTraitors = new List<MyPlayer>(spawnedPlayers);
        for (int i = 0; i < game.TraitorsLeft; i++)
        {
            int randomIndex = random.Next(potentialTraitors.Count);
            MyPlayer traitor = potentialTraitors[randomIndex];
            traitor.role = "Traitor";
            potentialTraitors.RemoveAt(randomIndex);
        }

        MyPlayer detective = potentialTraitors[random.Next(potentialTraitors.Count)];
        detective.role = "Detective";

        foreach (var player in spawnedPlayers)
        {
            private string description;
            private string color;
            player.isAlive = true;
            if (player.role == "Traitor")
            {
                List<string> traitorNames = new List<string>();
                foreach (var traitor in potentialTraitors)
                {
                    if (traitor != player)
                    {
                        traitorNames.Add(traitor.Name);
                    }
                }

                player.SetPrimaryWeapon(default);
                player.SetSecondaryWeapon(Weapons.USP);
                player.SetLightGadget(Gadgets.SledgeHammer);
                player.SetHeavyGadget(Gadgets.C4);
                player.SetThrowable(Throwables.FragGrenade);
                player.SetHP(200f);
                color = "red";
                description = $"Your mission is to murder all the other innocents, and the detective. Try working together with the other traitors: {string.Join(", ", traitorNames)}";
            }
            else if (player.role == "Detective")
            {
                player.SetPrimaryWeapon(Weapons.REM700);
                player.SetSecondaryWeapon(Weapons.Unica);
                player.SetLightGadget(Gadgets.SledgeHammer);
                player.SetHeavyGadget(Gadgets.AirDrone);
                player.SetThrowable(Throwables.FragGrenade);
                player.SetHP(500f);
                color = "blue";
                description = "The traitors will try to kill you, find out who they are and protect the innocents.";
            }
            else
            {
                player.role = "Innocent";
                player.SetPrimaryWeapon(default);
                player.SetSecondaryWeapon(Weapons.USP);
                player.SetLightGadget(Gadgets.SledgeHammer);
                player.SetHeavyGadget(Gadgets.AntiGrenadeTrophy);
                player.setThrowable(default)
                color = "green";
                description = "Try to avoid getting killed, and help the detective find out who the traitors are.";
            }

            player.Message($"<color=white>You are <color={color}> {player.role}. <br> <color=white> {description}");
        }

        SayToChat($"[GAME] Started with {spawnedPlayers.Count} Players of which {game.TraitorsLeft} Traitors. {detective.Name} is the Detective!");
    }

    private static void CheckGameEnd()
    {
        int traitorsAlive = spawnedPlayers.Count(player => player.role == "Traitor" && player.isAlive);
        int innocentsAlive = spawnedPlayers.Count(player => player.role == "Innocent" && player.isAlive);
        int detectivesAlive = spawnedPlayers.Count(player => player.role == "Detective" && player.isAlive);

        if (traitorsAlive == 0 || innocentsAlive + detectivesAlive == 0)
        {
            EndGame(traitorsAlive > 0 ? "Traitors" : "Innocents");
        }
    }

    private static void EndGame(string winner)
    {
        SayToChat($"[GAME] Game ended! {winner} win!");

        game.InProgress = false;
        game.TraitorsLeft = 0;

        foreach (var player in spawnedPlayers)
        {
            player.role = "None";
            player.Message("The game has ended, respawn to play another.");
            player.Kill();
        }
    }

    private static int CalculateInitialTraitors()
    {
        return (int)Math.Ceiling(spawnedPlayers.Count * 0.2);
    }

}

class MyPlayer : Player
{
    public bool isAlive = true;
    public string role = "None";
}

class TTTGame
{
    public bool InProgress { get; set; } = false;
    public int TraitorsLeft { get; set; } = 0;
}



