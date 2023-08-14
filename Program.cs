using BattleBitAPI;
using BattleBitAPI.Common;
using BattleBitAPI.Server;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var listener = new ServerListener<MyPlayer, MyGameServer>();
        listener.Start(29294);

        Thread.Sleep(-1);
    }
}

class MyPlayer : Player<MyPlayer>
{
    public bool isPlaying = false;
    public string role = "None";

    public override async Task OnConnected()
    {
    }
}

class MyGameServer : GameServer<MyPlayer>
{
    private static readonly Random random = new Random();
    private static readonly List<MyPlayer> spawnedPlayers = new List<MyPlayer>();
    private static bool isCountdownInProgress = false;

    public override async Task OnConnected()
    {
        ForceStartGame();
        MapRotation.SetRotation("TensaTown"); // Specify your map name here.
        GameModeRotation.SetRotation("FreeForAll");
        ServerSettings.PointLogEnabled = false;
    }

    public override async Task OnPlayerConnected(MyPlayer player)
    {
        SayToChat($"<color=blue>[+] <color=white>Welcome {player.Name}!");
        player.isPlaying = false;
        player.role="None";
        player.CanSpawn = false;
        player.CanSpectate = true;
        player.CanRespawn = false;
    }

    public override async Task OnPlayerSpawned(MyPlayer player)
    {
        if (!player.isPlaying || player.role == "None")
        {
            player.Kill();
            player.Message("Please wait for this game to end, you can spectate in the meantime.", 1f);
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
        }
        else
        {
            SayToChat($"[GAME] {8 - spawnedPlayers.Count} Players needed to start a new game.");
        }
    }

    public override async Task OnAPlayerDownedAnotherPlayer(OnPlayerKillArguments<MyPlayer> args)
    {
        SayToChat($"[GAME] <color=orange> Someone has been downed with a {args.KillerTool}!");
    }

    public override async Task OnPlayerGivenUp(MyPlayer player)
    {
        SayToChat($"[GAME] <color=red> {player.Name} gave up and died...");
    }

    public override async Task OnPlayerDied(MyPlayer player)
    {
        player.isPlaying = false;
        if (spawnedPlayers.Contains(player))
        {
            spawnedPlayers.Remove(player);
        }
        CheckGameEnd();
        SayToChat($"[GAME] <color=red> Someone has been killed!");
    }

    public override async Task OnAPlayerRevivedAnotherPlayer(MyPlayer from, MyPlayer to)
    {
       SayToChat($"[GAME] <color=green> {to} has been revived by {from}!");
    }

    public override async Task OnPlayerDisconnected(MyPlayer player)
    {
        SayToChat($"<color=orange>[-]<color=white> Goodbye {player.Name}!");
    }

    public override async Task OnPlayerTypedMessage(MyPlayer player, ChatChannel channel, string msg)
    {
        // Will make a command here for a detective to get a bearing on the nearest traitor.
        if (msg == "!locate") {
            SayToChat($" X={player.Position.X} Y={player.Position.Y}");
        }
    }


    private async Task StartCountdown()
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

    private void StartGame()
    {
        game.InProgress = true;
        game.TraitorsLeft = CalculateInitialTraitors();

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
            player.CanSpawn = false;
            player.CanRespawn = false;
            player.HP = 100f;

            player.DownTimeGiveUpTime = 30f;
            private string description = "";
            private string color = "";
            switch(player.role) 
            {
            case "Traitor":
                List<string> traitorNames = new List<string>();
                foreach (var traitor in potentialTraitors)
                {
                    if (traitor != player)
                    {
                        traitorNames.Add(traitor.Name);
                    }
                }
                player.SetPrimaryWeapon(default);
                player.SetSecondaryWeapon(Weapons.DesertEagle);
                player.SetLightGadget(Gadgets.SledgeHammer);
                player.SetHeavyGadget(Gadgets.C4);
                player.SetThrowable(Throwables.FragGrenade);
                player.HP = 200f;
                color = "red";
                description = $"Your mission is to murder all the other innocents, and the detective. Try working together with the other traitors: {string.Join(", ", traitorNames)}";
                break;
            case "Detective":
                player.SetPrimaryWeapon(Weapons.REM700);
                player.SetSecondaryWeapon(Weapons.Unica);
                player.SetLightGadget(Gadgets.SledgeHammer);
                player.SetHeavyGadget(Gadgets.AirDrone);
                player.SetThrowable(Throwables.FragGrenade);
                player.JumpHeightMultiplier = 3f;
                player.FallDamageMultiplier = 0f;
                player.HP = 500f;
                color = "blue";
                description = "The traitors will try to kill you, find out who they are and protect the innocents.";
                break;
            case "Innocent":
                player.role = "Innocent";
                player.SetPrimaryWeapon(default);
                player.SetSecondaryWeapon(Weapons.USP);
                player.SetLightGadget(Gadgets.SledgeHammer);
                player.SetHeavyGadget(Gadgets.AntiGrenadeTrophy);
                player.setThrowable(default)
                color = "green";
                description = "Try to avoid getting killed, and help the detective find out who the traitors are.";
                break;
            default:
            break;
            }
            player.Message($"<color=white>You are <color={color}> {player.role}. <br> <color=white> {description}", 1f);
        }

        SayToChat($"[GAME] Started with {spawnedPlayers.Count} Players of which {game.TraitorsLeft} Traitors. {detective.Name} is the Detective!");
    }



    private static void CheckGameEnd()
    {
        int traitorsAlive = spawnedPlayers.Count(player => player.role == "Traitor" && player.isPlaying);
        int innocentsAlive = spawnedPlayers.Count(player => player.role == "Innocent" && player.isPlaying);
        int detectivesAlive = spawnedPlayers.Count(player => player.role == "Detective" && player.isPlaying);

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

    private int CalculateInitialTraitors()
    {
        return (int)Math.Ceiling(spawnedPlayers.Count * 0.2);
    }
}
