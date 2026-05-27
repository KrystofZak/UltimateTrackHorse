using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateTrackHorseBot.Modules
{
    public class AuthModule : InteractionModuleBase<SocketInteractionContext>
    {
        // CZ Příkaz
        [SlashCommand("propojit", "Propojí tvůj herní účet pomocí 4místného PINu")]
        public async Task PropojitAsync([Summary("pin", "Zadej 4místný PIN ze hry")] string pin)
            => await ProcessLinkingAsync(pin);

        // EN Příkaz
        [SlashCommand("link", "Links your game account using a 4-digit PIN")]
        public async Task LinkAsync([Summary("pin", "Enter the 4-digit PIN from the game")] string pin)
            => await ProcessLinkingAsync(pin);

        // Společná logika
        private async Task ProcessLinkingAsync(string pin)
        {
            pin = pin.ToUpper();
            await DeferAsync(ephemeral: true);

            string discordId = Context.User.Id.ToString();
            string getUrl = $"{BotConfig.FirebaseUrl}/pending_links/{pin}.json";

            // 1. Získáme PIN z Firebase (a zjistíme, zda nás databáze vůbec pustí číst)
            var response = await BotConfig.HttpClient.GetAsync(getUrl);
            if (!response.IsSuccessStatusCode)
            {
                string getError = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[FIREBASE GET CHYBA]: {response.StatusCode} - {getError}");
                await FollowupAsync($"**CZ:** Nelze se spojit s databází ({response.StatusCode}). Zkontroluj konzoli bota.\n**EN:** Database connection failed. Check bot console.", ephemeral: true);
                return;
            }

            string json = await response.Content.ReadAsStringAsync();
            if (json == "null")
            {
                await FollowupAsync($"**CZ:** PIN **{pin}** neexistuje, nebo již vypršel.\n**EN:** PIN **{pin}** does not exist or has expired.", ephemeral: true);
                return;
            }

            // 2. PRÁCE S PROFILEM HRÁČE VE FIREBASE (Složka users)
            string userUrl = $"{BotConfig.FirebaseUrl}/users/{discordId}.json";
            var getUserResponse = await BotConfig.HttpClient.GetAsync(userUrl);
            string userJson = await getUserResponse.Content.ReadAsStringAsync();

            string token;

            // Pokud uživatel už existuje (hraje na jiném zařízení), použijeme jeho starý token
            if (userJson != "null")
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(userJson);
                token = node["token"].ToString();
                Console.WriteLine($"[INFO] Hráč {Context.User.Username} už má účet. Recykluji token pro nové zařízení.");
            }
            // Pokud je to úplně nový uživatel, vygenerujeme token a uložíme ho
            else
            {
                token = Guid.NewGuid().ToString();
                string userPayload = $"{{\"token\":\"{token}\"}}";
                var userResponse = await BotConfig.HttpClient.PutAsync(userUrl, new StringContent(userPayload, Encoding.UTF8, "application/json"));

                if (!userResponse.IsSuccessStatusCode)
                {
                    string putError = await userResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"[FIREBASE PUT CHYBA - USERS]: {userResponse.StatusCode} - {putError}");

                    await FollowupAsync($"**CZ:** Databáze zamítla vytvoření profilu (Chyba: {userResponse.StatusCode}). Podívej se do konzole bota!\n**EN:** Database rejected profile creation. Check bot console!", ephemeral: true);
                    return;
                }
            }

            // 3. PŘEDÁNÍ ÚDAJŮ DO UNITY (Složka pending_links)
            string patchUrl = $"{BotConfig.FirebaseUrl}/pending_links/{pin}.json";
            string patchPayload = $"{{\"discordId\":\"{discordId}\", \"token\":\"{token}\"}}";
            var patchResponse = await BotConfig.HttpClient.PatchAsync(patchUrl, new StringContent(patchPayload, Encoding.UTF8, "application/json"));

            if (patchResponse.IsSuccessStatusCode)
            {
                await FollowupAsync($"✅ **CZ:** Úspěšně propojeno! Můžeš odesílat výsledky ze hry.\n✅ **EN:** Successfully linked! You can now send results from the game.", ephemeral: true);
                Console.WriteLine($"Hráč {Context.User.Username} úspěšně (re)propojen s PINem {pin}.");
            }
            else
            {
                await FollowupAsync("**CZ:** Nastala chyba při komunikaci s databází (odesílání zpět do hry).\n**EN:** An error occurred while communicating with the database.", ephemeral: true);
            }
        }
    }
}
