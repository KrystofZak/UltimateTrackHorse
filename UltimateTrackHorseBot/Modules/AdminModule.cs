using Discord;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltimateTrackHorseBot.Modules
{
    public class AdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        // CZ Příkaz
        [SlashCommand("nastaveni", "Vytvoří kategorie a kanály pro hru Ultimate Track Horse")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task NastaveniAsync() => await ProcessSetupAsync();

        // EN Příkaz
        [SlashCommand("setup", "Creates categories and channels for Ultimate Track Horse")]
        [RequireUserPermission(GuildPermission.ManageChannels)]
        [RequireBotPermission(GuildPermission.ManageChannels)]
        public async Task SetupAsync() => await ProcessSetupAsync();

        // Společná logika
        private async Task ProcessSetupAsync()
        {
            await DeferAsync(ephemeral: true);
            var guild = Context.Guild;

            var category = guild.CategoryChannels.FirstOrDefault(c => c.Name.ToLower() == "ultimatetrackhorse");
            if (category == null)
            {
                var restCategory = await guild.CreateCategoryChannelAsync("UltimateTrackHorse");
                category = guild.GetCategoryChannel(restCategory.Id);
            }

            var tableChannel = guild.TextChannels.FirstOrDefault(c => c.Name.ToLower() == "table" && c.CategoryId == category.Id);
            if (tableChannel == null)
            {
                await guild.CreateTextChannelAsync("table", properties =>
                {
                    properties.CategoryId = category.Id;
                    properties.Topic = "Zde se budou objevovat nejlepší výsledky! / Best results will appear here!";
                });
            }

            var discusionChannel = guild.TextChannels.FirstOrDefault(c => c.Name.ToLower() == "discusion" && c.CategoryId == category.Id);
            if (discusionChannel == null)
            {
                await guild.CreateTextChannelAsync("discusion", properties =>
                {
                    properties.CategoryId = category.Id;
                    properties.Topic = "Volná diskuze o hře / Free discussion about Ultimate Track Horse.";
                });
            }

            string response = "✅ **CZ:** Setup ověřen a dokončen! Všechny kanály jsou připraveny.\n" +
                              "✅ **EN:** Setup verified and complete! All channels are ready.";

            await FollowupAsync(response, ephemeral: true);
        }
    }
}
