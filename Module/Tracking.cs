using Discord.Commands;
using Discord.WebSocket;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using MopsBot.Module.Preconditions;
using System.Text.RegularExpressions;
using static MopsBot.StaticBase;
using MopsBot.Data.Tracker;

namespace MopsBot.Module
{
    public class Tracking : ModuleBase
    {
        [Group("Twitter")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitter : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.\n"+
                      "You can specify the tweet notification like so: Normal tweet notification|Retweet or answer notification")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackTwitter(string twitterUser, [Remainder]string tweetNotification = "~Tweet Tweet~|~Tweet Tweet~")
            {
                await Trackers["twitter"].AddTrackerAsync(twitterUser, Context.Channel.Id, tweetNotification.Contains('|') ? tweetNotification : tweetNotification + "|" + tweetNotification);

                await ReplyAsync("Keeping track of " + twitterUser + "'s tweets, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified TwitterUser, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackTwitter(string twitterUser)
            {
                if(await Trackers["twitter"].TryRemoveTrackerAsync(twitterUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + twitterUser + "'s tweets!");
                else
                    await ReplyAsync($"Could not find tracker for `{twitterUser}`\n"+
                                     $"Currently tracked Twitter Users are:", embed:StaticBase.Trackers["twitter"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the twitters that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following twitters are currently being tracked:", embed:StaticBase.Trackers["twitter"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new Tweet is found.\n"+
                     "To differentiate between main tweets and other tweets, use `<Main Tweet Notification>|<Other Tweet Notification>`\n"+
                     "To disable a kind of tweet, set notification to **NONE**")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string twitterUser, [Remainder]string notification)
            {
                notification = notification.Contains("|") ? notification : notification + "|" + notification;

                if(await StaticBase.Trackers["twitter"].TrySetNotificationAsync(twitterUser, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{twitterUser}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{twitterUser}`\n"+
                                     $"Currently tracked Twitter Users are:", embed:StaticBase.Trackers["twitter"].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Osu")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Osu : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Osu player, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackOsu([Remainder]string OsuUser)
            {
                await Trackers["osu"].AddTrackerAsync(OsuUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + OsuUser + "'s plays above `0.1pp` gain, from now on!\nYou can change the lower pp boundary by using the `Osu SetPPBounds` subcommand!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Osu player, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOsu([Remainder]string OsuUser)
            {
                if(await Trackers["osu"].TryRemoveTrackerAsync(OsuUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + OsuUser + "'s plays!");
                else
                    await ReplyAsync($"Could not find tracker for `{OsuUser}`\n"+
                                     $"Currently tracked osu! players are:", embed:StaticBase.Trackers["osu"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the Osu players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Osu players are currently being tracked:", embed:StaticBase.Trackers["osu"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetPPBounds")]
            [Summary("Sets the lower bounds of pp gain that must be reached, to show a notification.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetPPBounds(string osuUser, double threshold)
            {
                var tracker = (OsuTracker)StaticBase.Trackers["osu"].GetTracker(Context.Channel.Id, osuUser);
                if(tracker != null){
                    if(threshold > 0.1){
                        tracker.PPThreshold = threshold;
                        await StaticBase.Trackers["osu"].UpdateDBAsync(tracker);
                        await ReplyAsync($"Changed threshold for `{osuUser}` to `{threshold}`");
                    }
                    else
                        await ReplyAsync("Threshold must be above 0.1!");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{osuUser}`\n"+
                                     $"Currently tracked Osu Players are:", embed:StaticBase.Trackers["osu"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a player gained pp.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string osuUser, [Remainder]string notification)
            {
                if(await StaticBase.Trackers["osu"].TrySetNotificationAsync(osuUser, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{osuUser}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{osuUser}`\n"+
                                     $"Currently tracked Osu Players are:", embed:StaticBase.Trackers["osu"].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Youtube")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Youtube : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackYoutube(string channelID, [Remainder]string notificationMessage = "New Video")
            {
                await Trackers["youtube"].AddTrackerAsync(channelID, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + channelID + "'s videos, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Youtuber, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackYoutube(string channelID)
            {
                if(await Trackers["youtube"].TryRemoveTrackerAsync(channelID, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + channelID + "'s videos!");
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n"+
                                     $"Currently tracked Youtubers are:", embed:StaticBase.Trackers["youtube"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the Youtubers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following Youtubers are currently being tracked:", embed:StaticBase.Trackers["youtube"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new video appears.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string channelID, [Remainder]string notification)
            {
                if(await StaticBase.Trackers["youtube"].TrySetNotificationAsync(channelID, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{channelID}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{channelID}`\n"+
                                     $"Currently tracked channels are:", embed:StaticBase.Trackers["youtube"].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Twitch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Twitch : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            [RequireBotPermission(ChannelPermission.ReadMessageHistory)]
            [RequireBotPermission(ChannelPermission.AddReactions)]
            [RequireBotPermission(ChannelPermission.ManageMessages)]
            public async Task trackStreamer(string streamerName, [Remainder]string notificationMessage = "Stream went live!")
            {
                await Trackers["twitch"].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + streamerName + "'s streams, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(string streamerName)
            {
                if(await Trackers["twitch"].TryRemoveTrackerAsync(streamerName, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName + "'s streams!");
                else
                    await ReplyAsync($"Could not find tracker for `{streamerName}`\n"+
                                     $"Currently tracked Streamers are:", embed:StaticBase.Trackers["twitch"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:", embed:StaticBase.Trackers["twitch"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a streamer goes live.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string streamer, [Remainder]string notification)
            {
                if(await StaticBase.Trackers["twitch"].TrySetNotificationAsync(streamer, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{streamer}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n"+
                                     $"Currently tracked streamers are:", embed:StaticBase.Trackers["twitch"].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("TwitchClips")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class TwitchClips : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified streamer's top clips every 30 minutes, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackClips(string streamerName, [Remainder]string notificationMessage = "New trending clip found!")
            {
                await Trackers["twitchclips"].AddTrackerAsync(streamerName, Context.Channel.Id, notificationMessage);

                await ReplyAsync("Keeping track of " + streamerName + "'s top clips above **2** views every 30 minutes, from now on!\nUse the `SetViewThreshold` subcommand to change the threshold.");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified streamer's clips.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackStreamer(string streamerName)
            {
                if(await Trackers["twitchclips"].TryRemoveTrackerAsync(streamerName, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + streamerName + "'s streams!");
                else
                    await ReplyAsync($"Could not find tracker for `{streamerName}`\n"+
                                     $"Currently tracked Streamers are:", embed:StaticBase.Trackers["twitchclips"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the streamers that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following streamers are currently being tracked:", embed:StaticBase.Trackers["twitchclips"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new clip is found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string streamer, [Remainder]string notification)
            {
                if(await StaticBase.Trackers["twitchclips"].TrySetNotificationAsync(streamer, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{streamer}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n"+
                                     $"Currently tracked streamers are:", embed:StaticBase.Trackers["twitchclips"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetViewThreshold")]
            [Summary("Sets the minimum views a top clip must have to be shown.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetViewThreshold(string streamer, uint threshold)
            {
                var tracker = (TwitchClipTracker)StaticBase.Trackers["twitchclips"].GetTracker(Context.Channel.Id, streamer);
                if(tracker != null){
                    tracker.ViewThreshold = threshold;
                    await StaticBase.Trackers["twitchclips"].UpdateDBAsync(tracker);
                    await ReplyAsync($"Will only notify about clips equal or above **{threshold}** views for `{streamer}` now.");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{streamer}`\n"+
                                     $"Currently tracked streamers are:", embed:StaticBase.Trackers["twitchclips"].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Reddit")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Reddit : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Subreddit, in the Channel you are calling this command right now.\nRequires Manage channel permissions."
            + "\n queries MUST look something like this: `title:mei+title:hanzo`")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackSubreddit(string subreddit, string query = null)
            {
                await Trackers["reddit"].AddTrackerAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id);

                await ReplyAsync("Keeping track of " + subreddit + $"'s posts, from now on, using {query}!");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking the specified Subreddit.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackSubreddit(string subreddit, string query = null)
            {
                if(await Trackers["reddit"].TryRemoveTrackerAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + subreddit + "'s posts!");
                else
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n"+
                                     $"Currently tracked Subreddits are:", embed:StaticBase.Trackers["reddit"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the subreddits that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following subreddits are currently being tracked:", embed:StaticBase.Trackers["reddit"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a new post was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string subreddit, string notification, string query = null)
            {
                if(await StaticBase.Trackers["reddit"].TrySetNotificationAsync(String.Join(" ", new string[] { subreddit, query }.Where(x => x != null)), Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{subreddit}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{subreddit}`\n"+
                                     $"Currently tracked subreddits are:", embed:StaticBase.Trackers["reddit"].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("Overwatch")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class Overwatch : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackOW(string owUser)
            {
                owUser = owUser.Replace("#", "-");
                await Trackers["overwatch"].AddTrackerAsync(owUser, Context.Channel.Id);

                await ReplyAsync("Keeping track of " + owUser + "'s stats, from now on!");
            }

            [Command("UnTrack")]
            [Summary("Stops keeping track of the specified Overwatch player, in the Channel you are calling this command right now.\nParameter: Username-Battletag")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackOW(string owUser)
            {
                owUser = owUser.Replace("#", "-");
                if(await Trackers["overwatch"].TryRemoveTrackerAsync(owUser, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + owUser + "'s stats!");
                else
                    await ReplyAsync($"Could not find tracker for `{owUser}`\n"+
                                     $"Currently tracked players are:", embed:StaticBase.Trackers["overwatch"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetStats")]
            [Summary("Returns an embed representing the stats of the specified Overwatch player")]
            public async Task GetStats(string owUser)
            {
                await ReplyAsync("Stats fetched:", false, await Data.Tracker.OverwatchTracker.overwatchInformation(owUser));
            }

            [Command("GetTrackers")]
            [Summary("Returns the players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:", embed:StaticBase.Trackers["overwatch"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a players' stats changed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string owUser, [Remainder]string notification)
            {
                owUser = owUser.Replace("#", "-");
                if(await StaticBase.Trackers["overwatch"].TrySetNotificationAsync(owUser, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{owUser}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{owUser}`\n"+
                                     $"Currently tracked players are:", embed:StaticBase.Trackers["overwatch"].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("News")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class News : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of articles from the specified source.\n"+
                     "Here is a list of possible sources: https://newsapi.org/sources")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task trackNews(string source, [Remainder]string query = "")
            {
                await Trackers["news"].AddTrackerAsync(String.Join("|", new string[] { source, query }), Context.Channel.Id);
                await ReplyAsync($"Keeping track of `{source}`'s articles {(query.Equals("") ? "" : $"including `{query}` from now on!")}");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking articles with the specified query.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task unTrackNews([Remainder]string articleQuery)
            {
                if(await Trackers["news"].TryRemoveTrackerAsync(articleQuery, Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of articles including " + articleQuery + "!");
                else
                    await ReplyAsync($"Could not find tracker for `{articleQuery}`\n"+
                                     $"Currently tracked article queries are:", embed:StaticBase.Trackers["news"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the article queries that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following article queries are currently being tracked:", embed:StaticBase.Trackers["news"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a article was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string articleQuery, [Remainder]string notification)
            {
                if(await StaticBase.Trackers["news"].TrySetNotificationAsync(articleQuery, Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{articleQuery}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{articleQuery}`\n"+
                                     $"Currently tracked article queries are:", embed:StaticBase.Trackers["news"].GetTrackersEmbed(Context.Channel.Id));
            }
        }

        [Group("WoW")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class WoW : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of changes in stats of the specified WoW player.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Track(string Region, string Realm, string Name)
            {
                await Trackers["wow"].AddTrackerAsync(String.Join("|", new string[] { Region, Realm, Name }), Context.Channel.Id);
                await ReplyAsync($"Keeping track of `{Name}`'s stats in `{Realm}` from now on.");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking stats of the specified player.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrack(string Region, string Realm, string Name)
            {
                if(await Trackers["wow"].TryRemoveTrackerAsync(string.Join("|", Region, Realm, Name), Context.Channel.Id))
                    await ReplyAsync($"Stopped keeping track of `{Region} {Realm} {Name}`'s stats!");
                else
                    await ReplyAsync($"Could not find tracker for `{Region} {Realm} {Name}`\n"+
                                     $"Currently tracked WoW players are:", embed:StaticBase.Trackers["wow"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the WoW players that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following players are currently being tracked:", embed:StaticBase.Trackers["wow"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetStats")]
            [Summary("Returns the WoW players' stats.")]
            public async Task getStats(string Region, string Realm, string Name)
            {
                await ReplyAsync("Stats for player:", embed: WoWTracker.createStatEmbed(Region, Realm, Name));
            }

            /*[Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a change in stats was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string Region, string Realm, string Name, [Remainder]string notification)
            {
                if(StaticBase.Trackers["wow"].TrySetNotification(string.Join("|", Region, Realm, Name), Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{Region} {Realm} {Name}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{Region} {Realm} {Name}`\n"+
                                     $"Currently tracked players are: ``{String.Join(", ", StaticBase.Trackers["wow"].GetTrackers(Context.Channel.Id).Select(x => x.Name.Replace("|", " ")))}``");
            }*/

            [Command("ChangeEQTrack")]
            [Summary("Notifies on change of equipment.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableEQTrack(string Region, string Realm, string Name)
            {
                WoWTracker tracker = (WoWTracker)StaticBase.Trackers["wow"].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackEquipment = !tracker.trackEquipment;
                
                await StaticBase.Trackers["wow"].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed EQTrack for `{Region} {Realm} {Name}` to `{tracker.trackEquipment}`");
            }

            [Command("ChangeStatTrack")]
            [Summary("Notifies on change of stats.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableStatTrack(string Region, string Realm, string Name)
            {
                WoWTracker tracker = (WoWTracker)StaticBase.Trackers["wow"].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackStats = !tracker.trackStats;
                
                await StaticBase.Trackers["wow"].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed StatTrack for `{Region} {Realm} {Name}` to `{tracker.trackStats}`");
            }

            [Command("ChangeFeedTrack")]
            [Summary("Notifies on change of feed.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableFeedTrack(string Region, string Realm, string Name)
            {
                WoWTracker tracker = (WoWTracker)StaticBase.Trackers["wow"].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackFeed = !tracker.trackFeed;
                
                await StaticBase.Trackers["wow"].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed FeedTrack for `{Region} {Realm} {Name}` to `{tracker.trackFeed}`");
            }
        }
        
        [Group("WoWGuild")]
        [RequireBotPermission(ChannelPermission.SendMessages)]
        public class WoWGuild : ModuleBase
        {
            [Command("Track")]
            [Summary("Keeps track of changes in news of the specified WoW guild.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task Track(string Region, string Realm, string Name)
            {
                await Trackers["wowguild"].AddTrackerAsync(String.Join("|", new string[] { Region, Realm, Name }), Context.Channel.Id);
                await ReplyAsync($"Keeping track of `{Name}`'s news in `{Realm}` from now on.");
            }

            [Command("UnTrack")]
            [Summary("Stops tracking stats of the specified Guild.\nRequires Manage channel permissions.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task UnTrack(string Region, string Realm, string Name)
            {
                if(await Trackers["wowguild"].TryRemoveTrackerAsync(string.Join("|", Region, Realm, Name), Context.Channel.Id))
                    await ReplyAsync("Stopped keeping track of " + string.Join("|", Region, Realm, Name) + "'s news!");
                else
                    await ReplyAsync($"Could not find tracker for `{Region} {Realm} {Name}`\n"+
                                     $"Currently tracked WoW Guilds are:", embed:StaticBase.Trackers["wowguild"].GetTrackersEmbed(Context.Channel.Id));
            }

            [Command("GetTrackers")]
            [Summary("Returns the WoW Guilds that are tracked in the current channel.")]
            public async Task getTrackers()
            {
                await ReplyAsync("Following guilds are currently being tracked:", embed:StaticBase.Trackers["wowguild"].GetTrackersEmbed(Context.Channel.Id));
            }

            /*[Command("SetNotification")]
            [Summary("Sets the notification text that is used each time a change in news was found.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task SetNotification(string Region, string Realm, string Name, [Remainder]string notification)
            {
                if(StaticBase.Trackers["wowguild"].TrySetNotification(string.Join("|", Region, Realm, Name), Context.Channel.Id, notification)){
                    await ReplyAsync($"Changed notification for `{Region} {Realm} {Name}` to `{notification}`");
                }
                else
                    await ReplyAsync($"Could not find tracker for `{Region} {Realm} {Name}`\n"+
                                     $"Currently tracked guilds are: ``{String.Join(", ", StaticBase.Trackers["wowguild"].GetTrackers(Context.Channel.Id).Select(x => x.Name))}``");
            }*/

            [Command("ChangeLootTrack")]
            [Summary("Notifies when member gains loot.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableEQTrack(string Region, string Realm, string Name)
            {
                WoWGuildTracker tracker = (WoWGuildTracker)StaticBase.Trackers["wowguild"].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackLoot = !tracker.trackLoot;
                
                await StaticBase.Trackers["wowguild"].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed EQTrack for `{Region} {Realm} {Name}` to `{tracker.trackLoot}`");
            }

            [Command("ChangeAchievementTrack")]
            [Summary("Notifies on gained achievements.")]
            [RequireUserPermission(ChannelPermission.ManageChannels)]
            public async Task EnableStatTrack(string Region, string Realm, string Name)
            {
                WoWGuildTracker tracker = (WoWGuildTracker)StaticBase.Trackers["wowguild"].GetTracker(Context.Channel.Id, string.Join("|", Region, Realm, Name));
                tracker.trackAchievements = !tracker.trackAchievements;
                
                await StaticBase.Trackers["wowguild"].UpdateDBAsync(tracker);
                await ReplyAsync($"Changed StatTrack for `{Region} {Realm} {Name}` to `{tracker.trackAchievements}`");
            }
        }
        /*[Command("trackClips")]
        [Summary("Keeps track of clips from streams of the specified Streamer, in the Channel you are calling this command right now.\nRequires Manage channel permissions.")]
        [RequireUserPermission(ChannelPermission.ManageChannels)]
        public async Task trackClips(string streamerName)
        {
            ClipTracker.AddTracker(streamerName, Context.Channel.Id);

            await ReplyAsync("Keeping track of clips of " + streamerName + "'s streams, from now on!");
        }*/
    }
}
