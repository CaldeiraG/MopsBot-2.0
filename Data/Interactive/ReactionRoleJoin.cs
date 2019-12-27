using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MopsBot.Data.Entities;

namespace MopsBot.Data.Interactive
{
    public class ReactionRoleJoin
    {
        //Key: Channel ID, Value: Message IDs
        public Dictionary<ulong, HashSet<ulong>> RoleInvites = new Dictionary<ulong, HashSet<ulong>>();

        public ReactionRoleJoin()
        {
            //using (StreamReader read = new StreamReader(new FileStream($"mopsdata//ReactionRoleJoin.json", FileMode.OpenOrCreate)))
            //{
            try
            {
                //RoleInvites = JsonConvert.DeserializeObject<Dictionary<ulong, HashSet<ulong>>>(read.ReadToEnd());
                //StaticBase.Database.GetCollection<MongoKVP<ulong, HashSet<ulong>>>("ReactionRoleJoin").InsertMany(Entities.MongoKVP<ulong, HashSet<ulong>>.DictToMongoKVP(RoleInvites));
                RoleInvites = new Dictionary<ulong, HashSet<ulong>>(StaticBase.Database.GetCollection<MongoKVP<ulong, HashSet<ulong>>>("ReactionRoleJoin").FindSync(x => true).ToList().Select(x => (KeyValuePair<ulong, HashSet<ulong>>)x));
            }
            catch (Exception e)
            {
                Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"", e)).Wait();
            }
            //}

            if (RoleInvites == null)
            {
                RoleInvites = new Dictionary<ulong, HashSet<ulong>>();
            }
            foreach (var channel in RoleInvites.ToList())
            {
                foreach (var message in channel.Value.ToList())
                {
                    try
                    {
                        var textmessage = (IUserMessage)((ITextChannel)Program.Client.GetChannel(channel.Key)).GetMessageAsync(message).Result;
                        var join = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("✅"), JoinRole, false);
                        var leave = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("✅"), LeaveRole, true);
                        var delete = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("🗑"), DeleteInvite, false);

                        Program.ReactionHandler.AddHandlers(textmessage, join, leave, delete).Wait();

                        foreach (var user in textmessage.GetReactionUsersAsync(new Emoji("✅"), textmessage.Reactions[new Emoji("✅")].ReactionCount).FlattenAsync().Result.Where(x => !x.IsBot).Reverse())
                        {
                            JoinRole(user.Id, textmessage);
                        }
                        /*foreach (var user in textmessage.GetReactionUsersAsync(new Emoji("❎"), textmessage.Reactions[new Emoji("❎")].ReactionCount).First().Result.Where(x => !x.IsBot).Reverse())
                        {
                            LeaveRole(user.Id, textmessage);
                        }*/
                        foreach (var user in textmessage.GetReactionUsersAsync(new Emoji("🗑"), textmessage.Reactions[new Emoji("🗑")].ReactionCount).FlattenAsync().Result.Where(x => !x.IsBot).Reverse())
                        {
                            DeleteInvite(user.Id, textmessage);
                        }
                    }
                    catch (Exception e)
                    {
                        Program.MopsLog(new LogMessage(LogSeverity.Error, "", $"error for [{channel.Key}][{message}]", e)).Wait();
                        if ((e.Message.Contains("Object reference not set to an instance of an object.") || e.Message.Contains("Value cannot be null."))
                            && Program.Client.ConnectionState.Equals(ConnectionState.Connected))
                        {
                            Program.MopsLog(new LogMessage(LogSeverity.Warning, "", $"Removing [{channel.Key}][{message}] due to missing message.")).Wait();

                            /*if (channel.Value.Count > 1){
                                channel.Value.Remove(message);
                                UpdateDBAsync(channel.Key).Wait();
                            }
                            else{
                                RoleInvites.Remove(channel.Key);
                                RemoveFromDBAsync(channel.Key).Wait();
                            }*/
                        }
                    }
                }
            }
        }

        public async Task InsertIntoDBAsync(ulong key)
        {
            await StaticBase.Database.GetCollection<MongoKVP<ulong, HashSet<ulong>>>(this.GetType().Name).InsertOneAsync(MongoKVP<ulong, HashSet<ulong>>.KVPToMongoKVP(new KeyValuePair<ulong, HashSet<ulong>>(key, RoleInvites[key])));
        }

        public async Task UpdateDBAsync(ulong key)
        {
            await StaticBase.Database.GetCollection<MongoKVP<ulong, HashSet<ulong>>>(this.GetType().Name).ReplaceOneAsync(x => x.Key == key, MongoKVP<ulong, HashSet<ulong>>.KVPToMongoKVP(new KeyValuePair<ulong, HashSet<ulong>>(key, RoleInvites[key])));
        }

        public async Task RemoveFromDBAsync(ulong key)
        {
            await StaticBase.Database.GetCollection<MongoKVP<ulong, HashSet<ulong>>>(this.GetType().Name).DeleteOneAsync(x => x.Key == key);
        }

        public async Task AddInviteGerman(ITextChannel channel, SocketRole role)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Title = role.Name + $" Einladung :{role.Id}";
            e.Description = $"Um der Rolle " + (role.IsMentionable ? role.Mention : $"**{role.Name}**") + " beizutreten, oder sie zu verlassen, füge bitte das ✅ Icon unter dieser Nachricht hinzu, oder entferne es!\n" +
                            "Falls du die Manage Role Permission besitzt, kannst du diese Einladung mit einem Druck auf den 🗑 Icon löschen.";
            e.Color = role.Color;

            var author = new EmbedAuthorBuilder();
            e.AddField("Mitgliederanzahl der Rolle", role.Members.Count(), true);

            var message = await channel.SendMessageAsync(embed: e.Build());

            var join = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("✅"), JoinRole, false);
            var leave = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("✅"), LeaveRole, true);
            var delete = new Tuple<IEmote, Func<ReactionHandlerContext, Task>, bool>(new Emoji("🗑"), DeleteInvite, false);

            await Program.ReactionHandler.AddHandlers(message, join, leave, delete);

            if (RoleInvites.ContainsKey(channel.Id))
            {
                RoleInvites[channel.Id].Add(message.Id);
                await UpdateDBAsync(channel.Id);
            }
            else
            {
                RoleInvites.Add(channel.Id, new HashSet<ulong>());
                RoleInvites[channel.Id].Add(message.Id);
                await InsertIntoDBAsync(channel.Id);
            }
        }

        public async Task AddInvite(ITextChannel channel, SocketRole role)
        {
            EmbedBuilder e = new EmbedBuilder();
            e.Title = role.Name + $" Role Invite :{role.Id}";
            e.Description = $"To join/leave the " + (role.IsMentionable ? role.Mention : $"**{role.Name}**") + " role, add/remove the ✅ Icon below this message!\n" +
                            "If you can manage Roles, you may delete this invitation by pressing the 🗑 Icon.";
            e.Color = role.Color;

            var author = new EmbedAuthorBuilder();
            e.AddField("Members in role", role.Members.Count(), true);

            var message = await channel.SendMessageAsync("", embed: e.Build());
            await Program.ReactionHandler.AddHandler(message, new Emoji("✅"), JoinRole);
            await Program.ReactionHandler.AddHandler(message, new Emoji("✅"), LeaveRole, true);
            await Program.ReactionHandler.AddHandler(message, new Emoji("🗑"), DeleteInvite);

            if (RoleInvites.ContainsKey(channel.Id))
            {
                RoleInvites[channel.Id].Add(message.Id);
                await UpdateDBAsync(channel.Id);
            }
            else
            {
                RoleInvites.Add(channel.Id, new HashSet<ulong>());
                RoleInvites[channel.Id].Add(message.Id);
                await InsertIntoDBAsync(channel.Id);
            }
        }

        private async Task JoinRole(ReactionHandlerContext context)
        {
            await JoinRole(context.Reaction.UserId, context.Message);
        }

        private async Task JoinRole(ulong userId, IUserMessage message)
        {
            var roleId = ulong.Parse(message.Embeds.First().Title.Split(new string[] { ":" }, StringSplitOptions.None).Last());
            var role = ((ITextChannel)message.Channel).Guild.GetRole(roleId);
            var user = await ((ITextChannel)message.Channel).Guild.GetUserAsync(userId);
            if (!user.RoleIds.Contains(roleId))
            {
                await user.AddRoleAsync(role);
                await updateMessage(message, (SocketRole)role);
            }
        }

        private async Task LeaveRole(ReactionHandlerContext context)
        {
            await LeaveRole(context.Reaction.UserId, context.Message);
        }

        private async Task LeaveRole(ulong userId, IUserMessage message)
        {
            var roleId = ulong.Parse(message.Embeds.First().Title.Split(new string[] { ":" }, StringSplitOptions.None).Last());
            var role = ((ITextChannel)message.Channel).Guild.GetRole(roleId);
            var user = await ((ITextChannel)message.Channel).Guild.GetUserAsync(userId);
            if (user.RoleIds.Contains(roleId))
            {
                await user.RemoveRoleAsync(role);
                await updateMessage(message, (SocketRole)role);
            }
        }

        private async Task DeleteInvite(ReactionHandlerContext context)
        {
            await DeleteInvite(context.Reaction.UserId, context.Message);
        }

        private async Task DeleteInvite(ulong userId, IUserMessage message)
        {
            var user = await ((ITextChannel)message.Channel).Guild.GetUserAsync(userId);
            if (user.GuildPermissions.ManageRoles)
            {
                await Program.ReactionHandler.ClearHandler(message);

                if (RoleInvites[message.Channel.Id].Count > 1)
                {
                    RoleInvites[message.Channel.Id].Remove(message.Id);
                    await UpdateDBAsync(message.Channel.Id);
                }
                else
                {
                    RoleInvites.Remove(message.Channel.Id);
                    await RemoveFromDBAsync(message.Channel.Id);
                }

                await message.DeleteAsync();
            }
        }

        private async Task updateMessage(ReactionHandlerContext context, SocketRole role)
        {
            if (!updating)
            {
                updating = true;
                await Task.Delay(10000);
                updating = false;
                var e = context.Message.Embeds.First().ToEmbedBuilder();

                e.Color = role.Color;
                e.Title = e.Title.Contains("Einladung") ? $"{role.Name} Einladung :{role.Id}" : $"{role.Name} Role Invite :{role.Id}";
                foreach (EmbedFieldBuilder field in e.Fields)
                {
                    if (field.Name.Equals("Members in role") || field.Name.Equals("Mitgliederanzahl der Rolle"))
                        field.Value = role.Members.Count();
                }

                await context.Message.ModifyAsync(x =>
                {
                    x.Embed = e.Build();
                });
            }
        }

        bool updating = false;
        private async Task updateMessage(IUserMessage message, SocketRole role)
        {
            if (!updating)
            {
                updating = true;
                await Task.Delay(10000);
                updating = false;
                var e = message.Embeds.First().ToEmbedBuilder();

                e.Color = role.Color;
                e.Title = e.Title.Contains("Einladung") ? $"{role.Name} Einladung :{role.Id}" : $"{role.Name} Role Invite :{role.Id}";
                foreach (EmbedFieldBuilder field in e.Fields)
                {
                    if (field.Name.Equals("Members in role") || field.Name.Equals("Mitgliederanzahl der Rolle"))
                        field.Value = role.Members.Count();
                }

                await message.ModifyAsync(x =>
                {
                    x.Embed = e.Build();
                });
            }
        }

        public async Task<List<KeyValuePair<ulong, ulong>>> TryPruneAsync(bool testing = true)
        {
            var pruneList = new List<KeyValuePair<ulong, ulong>>();

            foreach (var channel in RoleInvites.ToList())
            {
                foreach (var message in channel.Value.ToList())
                {
                    var curChannel = (ITextChannel)Program.Client.GetChannel(channel.Key);
                    if (curChannel != null)
                    {
                        var curMessage = curChannel.GetMessageAsync(message);
                        if (curMessage != null) continue;
                    }

                    pruneList.Add(KeyValuePair.Create<ulong, ulong>(channel.Key, message));
                    if (!testing)
                    {
                        if (RoleInvites[channel.Key].Count > 1)
                        {
                            RoleInvites[channel.Key].Remove(message);
                            await UpdateDBAsync(channel.Key);
                        }
                        else
                        {
                            RoleInvites.Remove(channel.Key);
                            await RemoveFromDBAsync(channel.Key);
                        }
                    }
                }
            }

            return pruneList;
        }
    }
}
