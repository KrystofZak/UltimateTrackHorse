using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace UltimateTrackHorseBot.Modules
{
    public class LeaderboardModule : InteractionModuleBase<SocketInteractionContext>
    {
        // CZ Příkaz
        [SlashCommand("tabulka", "Zobrazí žebříček pro konkrétní seed")]
        public async Task TabulkaAsync([Summary("seed", "Zadej seed tratě")] string seed)
            => await ShowOrUpdateLeaderboardAsync(seed, isGlobal: true);

        // EN Příkaz
        [SlashCommand("leaderboard", "Shows leaderboard for a specific seed")]
        public async Task LeaderboardAsync([Summary("seed", "Enter track seed")] string seed)
            => await ShowOrUpdateLeaderboardAsync(seed, isGlobal: true);

        // --- HANDLERY PRO TLAČÍTKA ---

        [ComponentInteraction("lb_global_*")]
        public async Task HandleGlobalButton(string seed)
        {
            await UpdateLeaderboardFromButtonAsync(seed, isGlobal: true);
        }

        [ComponentInteraction("lb_server_*")]
        public async Task HandleServerButton(string seed)
        {
            await UpdateLeaderboardFromButtonAsync(seed, isGlobal: false);
        }

        // Tlačítko Refresh
        [ComponentInteraction("lb_refresh_*_*")]
        public async Task HandleRefreshButton(string isGlobalStr, string seed)
        {
            bool isGlobal = bool.Parse(isGlobalStr);
            await UpdateLeaderboardFromButtonAsync(seed, isGlobal: isGlobal);
        }

        // --- LOGIKA ODESÍLÁNÍ (SLASH PŘÍKAZY) ---

        private async Task ShowOrUpdateLeaderboardAsync(string seed, bool isGlobal)
        {
            await DeferAsync(ephemeral: true);

            var (embed, components) = await BuildLeaderboardAsync(seed, isGlobal);

            var messages = await Context.Channel.GetMessagesAsync(50).FlattenAsync();
            var oldMessage = messages.FirstOrDefault(m => m.Author.Id == Context.Client.CurrentUser.Id &&
                                                          m.Embeds.Any(e => e.Title != null && e.Title.Contains("Tabulka / Leaderboard"))) as IUserMessage;

            if (oldMessage != null)
            {
                // 1. Zpráva nalezena -> Upravíme ji
                await oldMessage.ModifyAsync(msg =>
                {
                    msg.Embed = embed;
                    msg.Components = components;
                });

                // ULOŽENÍ DO FIREBASE: Bot si pamatuje, že tato zpráva je aktivní
                await SaveBoardStateToFirebaseAsync(oldMessage.Id, Context.Channel.Id, seed, isGlobal);

                await FollowupAsync("✅ **CZ:** Tabulka nahoře byla aktualizována a připojena na Auto-Sync.\n✅ **EN:** The leaderboard above has been updated.", ephemeral: true);
            }
            else
            {
                // 2. Zpráva nenalezena -> Pošleme novou
                var sentMsg = await Context.Channel.SendMessageAsync(embed: embed, components: components);

                // ULOŽENÍ DO FIREBASE: Bot si pamatuje, že tato zpráva je aktivní
                await SaveBoardStateToFirebaseAsync(sentMsg.Id, Context.Channel.Id, seed, isGlobal);

                await FollowupAsync("✅ **CZ:** Tabulka byla vytvořena a připojena na Auto-Sync.\n✅ **EN:** The leaderboard has been created.", ephemeral: true);
            }
        }

        // --- LOGIKA ODESÍLÁNÍ (TLAČÍTKA) ---

        private async Task UpdateLeaderboardFromButtonAsync(string seed, bool isGlobal)
        {
            await DeferAsync();

            var (embed, components) = await BuildLeaderboardAsync(seed, isGlobal);

            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Embed = embed;
                msg.Components = components;
            });

            // Musíme získat ID zprávy, na kterou uživatel kliknul, a updatovat ve Firebase její stav (jestli je Global nebo Server)
            var interactionMessage = await Context.Interaction.GetOriginalResponseAsync();
            await SaveBoardStateToFirebaseAsync(interactionMessage.Id, Context.Channel.Id, seed, isGlobal);
        }

        // --- POMOCNÁ METODA PRO ULOŽENÍ DO FIREBASE ---
        private async Task SaveBoardStateToFirebaseAsync(ulong messageId, ulong channelId, string seed, bool isGlobal)
        {
            var boardState = new ActiveBoardState
            {
                Seed = seed,
                ChannelId = channelId,
                IsGlobal = isGlobal
            };

            // Vytvoříme JSON a pošleme přes PUT (přepíše nebo vytvoří záznam s klíčem ID zprávy)
            string saveUrl = $"{BotConfig.FirebaseUrl}/active_boards/{messageId}.json";
            string jsonState = JsonSerializer.Serialize(boardState);
            var content = new StringContent(jsonState, Encoding.UTF8, "application/json");

            await BotConfig.HttpClient.PutAsync(saveUrl, content);
        }

        // --- HLAVNÍ LOGIKA PRO STAŽENÍ A TŘÍDĚNÍ DAT ---
        private async Task<(Embed, MessageComponent)> BuildLeaderboardAsync(string seed, bool isGlobal)
        {
            string url = $"{BotConfig.FirebaseUrl}/leaderboards/{seed}.json";
            var response = await BotConfig.HttpClient.GetAsync(url);
            string json = await response.Content.ReadAsStringAsync();

            var embed = new EmbedBuilder()
                .WithTitle($"🏆 Tabulka / Leaderboard | Seed: {seed}")
                .WithColor(Color.Gold);

            if (json == "null")
            {
                embed.WithDescription("**CZ:** Pro tento seed zatím nejsou žádné záznamy.\n**EN:** No records found for this seed yet.");
            }
            else
            {
                var scoresDict = JsonSerializer.Deserialize<Dictionary<string, FirebaseScore>>(json);

                var sortedScores = scoresDict.Values
                    .OrderByDescending(s => s.Laps)
                    .ThenBy(s => s.BestLap)
                    .ToList();

                if (!isGlobal)
                {
                    await Context.Guild.DownloadUsersAsync();
                    sortedScores = sortedScores.Where(s => Context.Guild.GetUser(ulong.Parse(s.DiscordId)) != null).ToList();
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

            embed.WithFooter(isGlobal ? "🌍 Globální pohled / Global View" : "🏠 Serverový pohled / Server View");

            var components = new ComponentBuilder()
                .WithButton("🌍 Global", $"lb_global_{seed}", ButtonStyle.Primary, disabled: isGlobal)
                .WithButton("🏠 Server", $"lb_server_{seed}", ButtonStyle.Success, disabled: !isGlobal)
                .WithButton("🔄 Refresh", $"lb_refresh_{isGlobal}_{seed}", ButtonStyle.Secondary)
                .Build();

            return (embed.Build(), components);
        }

        // Třídy pro JSON deserializaci
        private class FirebaseScore
        {
            [JsonPropertyName("discordId")]
            public string DiscordId { get; set; }

            [JsonPropertyName("laps")]
            public int Laps { get; set; }

            [JsonPropertyName("bestLap")]
            public float BestLap { get; set; }

            [JsonPropertyName("token")]
            public string Token { get; set; }
        }

        private class ActiveBoardState
        {
            [JsonPropertyName("seed")]
            public string Seed { get; set; }

            [JsonPropertyName("channelId")]
            public ulong ChannelId { get; set; }

            [JsonPropertyName("isGlobal")]
            public bool IsGlobal { get; set; }
        }
    }
}