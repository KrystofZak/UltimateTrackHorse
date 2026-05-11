using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace UltimateTrackHorseBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private InteractionService _interactions;
        private IServiceProvider _services;

        static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info,
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers

            });

            _interactions = new InteractionService(_client.Rest);

            _services = new ServiceCollection()
                .AddSingleton(_client)
                .AddSingleton(_interactions)
                .BuildServiceProvider();

            // Navázání událostí
            _client.Log += LogAsync;
            _interactions.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.InteractionCreated += HandleInteraction;

            // NOVÉ: Globální ošetření výsledku všech příkazů
            _interactions.InteractionExecuted += HandleInteractionExecuted;

            await _client.LoginAsync(TokenType.Bot, BotConfig.BotToken);
            await _client.StartAsync();

            // NOVÉ: Smyčka pro čtení konzolových příkazů (nahrazuje Task.Delay(-1))
            await ConsoleInputLoopAsync();
        }

        private async Task ReadyAsync()
        {
            await _interactions.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactions.RegisterCommandsGloballyAsync();
            Console.WriteLine("Bot je online a připraven!");
            Console.WriteLine("Dostupné konzolové příkazy: 'send <ChannelID> <Zpráva>', 'shutdown'");
            _ = Task.Run(() => StartLeaderboardUpdaterAsync());

        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                var context = new SocketInteractionContext(_client, interaction);
                await _interactions.ExecuteCommandAsync(context, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Kritická chyba interakce: {ex}");
            }
        }

        // NOVÉ: Zpracování chyb pro uživatele
        // NOVÉ: Zpracování chyb pro uživatele (Česky + Anglicky)
        private async Task HandleInteractionExecuted(ICommandInfo command, IInteractionContext context, IResult result)
        {
            // Pokud se příkaz provedl správně, nic neděláme
            if (result.IsSuccess) return;

            // Sestavení bilingvní chybové zprávy
            string errorMessage = $"❌ **Jejda, něco se pokazilo! / Oops, something went wrong!**\n\n" +
                                  $"**Důvod / Reason:** {result.ErrorReason}\n\n" +
                                  $"**Řešení / How to fix:**\n" +
                                  $"1. **CZ:** Zkontroluj, zda jsi zadal všechny parametry správně.\n" +
                                  $"   **EN:** Make sure you entered all parameters correctly.\n" +
                                  $"2. **CZ:** Ujisti se, že ty i bot máte na tento příkaz příslušná oprávnění.\n" +
                                  $"   **EN:** Ensure that both you and the bot have the required permissions.\n" +
                                  $"3. **CZ:** Zkus to znovu. Pokud problém přetrvává, kontaktuj administrátora serveru.\n" +
                                  $"   **EN:** Try again. If the issue persists, contact the server administrator.";

            // Zjistíme, jestli už bot "přemýšlel" (DeferAsync), nebo jestli je to úplně nová zpráva
            if (context.Interaction.HasResponded)
            {
                await context.Interaction.FollowupAsync(errorMessage, ephemeral: true);
            }
            else
            {
                await context.Interaction.RespondAsync(errorMessage, ephemeral: true);
            }
        }
        // NOVÉ: Logika pro zadávání příkazů přímo do černého okna konzole
        private async Task ConsoleInputLoopAsync()
        {
            while (true)
            {
                string input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;

                // Rozdělíme vstup na příkaz a zbytek textu
                string[] args = input.Split(' ', 2);
                string command = args[0].ToLower();

                if (command == "shutdown")
                {
                    Console.WriteLine("Vypínám bota a ukončuji aplikaci...");
                    await _client.StopAsync();
                    await _client.LogoutAsync();
                    break; // Ukončí smyčku a tím celou aplikaci
                }
                else if (command == "send")
                {
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Chyba: Použití je 'send <ChannelID> <Zpráva>'");
                        continue;
                    }

                    // Zbytek rozdělíme na ID kanálu a samotnou zprávu
                    string[] sendArgs = args[1].Split(' ', 2);
                    if (sendArgs.Length < 2 || !ulong.TryParse(sendArgs[0], out ulong channelId))
                    {
                        Console.WriteLine("Chyba: Neplatné Channel ID. Použití je 'send <ChannelID> <Zpráva>'");
                        continue;
                    }

                    string message = sendArgs[1];

                    // Pokus o nalezení kanálu a odeslání
                    if (_client.GetChannel(channelId) is IMessageChannel channel)
                    {
                        await channel.SendMessageAsync(message);
                        Console.WriteLine($"Zpráva úspěšně odeslána do kanálu {channel.Name}.");
                    }
                    else
                    {
                        Console.WriteLine("Chyba: Kanál nenalezen. Ujisti se, že bot je na daném serveru a má právo číst tento kanál.");
                    }
                }
                else
                {
                    Console.WriteLine($"Neznámý příkaz '{command}'. Dostupné příkazy: send, shutdown");
                }
            }
        }

        public class ActiveBoardState
        {
            [System.Text.Json.Serialization.JsonPropertyName("seed")]
            public string Seed { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("channelId")]
            public ulong ChannelId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("isGlobal")]
            public bool IsGlobal { get; set; }
        }

        public class FirebaseScore
        {
            [System.Text.Json.Serialization.JsonPropertyName("discordId")]
            public string DiscordId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("laps")]
            public int Laps { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("bestLap")]
            public float BestLap { get; set; }
        }

        // NOVÉ: Smyčka pro automatickou aktualizaci tabulek
        private async Task StartLeaderboardUpdaterAsync()
        {
            Console.WriteLine($"[INFO] Spouštím automatickou aktualizaci tabulek každých {BotConfig.RefreshIntervalSeconds} vteřin.");

            while (true)
            {
                try
                {
                    // 1. Stáhneme si z Firebase paměť všech našich odeslaných tabulek
                    string boardsUrl = $"{BotConfig.FirebaseUrl}/active_boards.json";
                    var boardsResponse = await BotConfig.HttpClient.GetAsync(boardsUrl);

                    if (boardsResponse.IsSuccessStatusCode)
                    {
                        string boardsJson = await boardsResponse.Content.ReadAsStringAsync();
                        if (boardsJson != "null")
                        {
                            var boards = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, ActiveBoardState>>(boardsJson);

                            // 2. Projdeme každou aktivní tabulku
                            foreach (var kvp in boards)
                            {
                                if (!ulong.TryParse(kvp.Key, out ulong messageId)) continue;
                                var state = kvp.Value;

                                // Zkusíme najít kanál a zprávu na Discordu
                                if (_client.GetChannel(state.ChannelId) is not SocketTextChannel channel) continue;
                                var msg = await channel.GetMessageAsync(messageId) as IUserMessage;
                                if (msg == null) continue; // Zpráva už možná byla smazána

                                // 3. Stáhneme aktuální data pro konkrétní trať (Seed)
                                string seedUrl = $"{BotConfig.FirebaseUrl}/leaderboards/{state.Seed}.json";
                                string seedJson = await BotConfig.HttpClient.GetStringAsync(seedUrl);

                                // 4. Sestavení embedu (Stejná logika jako máš v LeaderboardModule)
                                var embed = new EmbedBuilder()
                                    .WithTitle($"🏆 Tabulka / Leaderboard | Seed: {state.Seed}")
                                    .WithColor(Color.Gold);

                                if (seedJson == "null")
                                {
                                    embed.WithDescription("**CZ:** Pro tento seed zatím nejsou žádné záznamy.\n**EN:** No records found for this seed yet.");
                                }
                                else
                                {
                                    var scoresDict = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, FirebaseScore>>(seedJson);

                                    var sortedScores = scoresDict.Values
                                        .OrderByDescending(s => s.Laps)
                                        .ThenBy(s => s.BestLap)
                                        .ToList();

                                    // Filtrace pro "Server View"
                                    if (!state.IsGlobal)
                                    {
                                        sortedScores = sortedScores.Where(s => channel.Guild.GetUser(ulong.Parse(s.DiscordId)) != null).ToList();
                                    }

                                    if (sortedScores.Count == 0)
                                    {
                                        embed.WithDescription("**CZ:** Nikdo z tohoto serveru tento seed ještě nejel.\n**EN:** Nobody from this server has played this seed yet.");
                                    }
                                    else
                                    {
                                        string boardText = "";
                                        int rank = 1;
                                        foreach (var score in sortedScores.Take(10))
                                        {
                                            string medal = rank == 1 ? "🥇" : rank == 2 ? "🥈" : rank == 3 ? "🥉" : $"**{rank}.**";
                                            boardText += $"{medal} <@{score.DiscordId}> | 🏁 Kola/Laps: **{score.Laps}** | ⏱️ Čas/Time: **{score.BestLap}s**\n";
                                            rank++;
                                        }
                                        embed.WithDescription(boardText);
                                    }
                                }

                                embed.WithFooter(state.IsGlobal ? "🌍 Globální pohled / Global View" : "🏠 Serverový pohled / Server View");

                                // 5. Tlačítka
                                var components = new ComponentBuilder()
                                    .WithButton("🌍 Global", $"lb_global_{state.Seed}", ButtonStyle.Primary, disabled: state.IsGlobal)
                                    .WithButton("🏠 Server", $"lb_server_{state.Seed}", ButtonStyle.Success, disabled: !state.IsGlobal)
                                    .WithButton("🔄 Refresh", $"lb_refresh_{state.IsGlobal}_{state.Seed}", ButtonStyle.Secondary)
                                    .Build();

                                // 6. Přepsání zprávy (Pokud by se data nezměnila, API Discordu si s tím poradí)
                                await msg.ModifyAsync(x =>
                                {
                                    x.Embed = embed.Build();
                                    x.Components = components;
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Aby nám pád při stahování neshodil celou smyčku
                    Console.WriteLine($"[CHYBA] Nepodařilo se aktualizovat tabulky: {ex.Message}");
                }

                // Počkáme x vteřin do dalšího zkontrolování
                await Task.Delay(BotConfig.RefreshIntervalSeconds * 1000);
            }
        }


        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}