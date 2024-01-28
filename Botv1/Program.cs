using System;
using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text;
using Newtonsoft.Json;
using Discord.Commands;
using System.Windows.Input;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;
using HtmlAgilityPack;
using TwitchLib.Api.Helix.Models.Chat.Emotes;
using System.Net;
using Google.Apis.YouTube.v3.Data;
using System.Reflection;
class Program
{
    private DiscordSocketClient _client;
    private string GptApiKey;
    private string DiscordBotToken;
    string startupPath = AppDomain.CurrentDomain.BaseDirectory;
    string userfile;
    string contentstr;
    string BotNickname;
    static void Main(string[] args)
       => new Program().RunBotAsync().GetAwaiter().GetResult();
    public async Task Mainrun()
    {
        // Load user settings from the INI file
        string userfile = @"\UserCFG.ini";
        GptApiKey = UserSettings(startupPath + userfile, "GptApiKey");
        DiscordBotToken = UserSettings(startupPath + userfile, "DiscordBotToken");
        contentstr = UserSettings(startupPath + userfile, "BotPersonality");
        BotNickname = UserSettings(startupPath + userfile, "BotNickname");
        Console.WriteLine(@"| API Keys Loaded. Opening connection to API Services | Status: Waiting For Connection...");
        // Check if the API keys are properly loaded
        if (string.IsNullOrEmpty(GptApiKey) || string.IsNullOrEmpty(DiscordBotToken))
        {
            Console.WriteLine("Error: API did not get SocketConnection. Are your API Keys correct? Exiting thread.");
            return;
        }
    }
    
    public string UserSettings(string File, string Identifier) // User Settings handler
    {
        using var S = new System.IO.StreamReader(File);
        string Result = "";
        while (S.Peek() != -1)
        {
            string Line = S.ReadLine();
            if (Line.ToLower().StartsWith(Identifier.ToLower() + "="))
            {
                Result = Line.Substring(Identifier.Length + 1);
            }
        }
        return Result;
    }
    
    public async Task RunBotAsync()
    {
        Console.Title = "VoiDBot Discord Bot";
        string fileName = "UserCFG.ini";
        string filePath = Path.Combine(startupPath, fileName);
        if (!File.Exists(filePath))
        {
            ExtractResourceToFile("Botv1.UserCFG.ini", filePath); 
        }
        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers | GatewayIntents.GuildPresences | GatewayIntents.MessageContent
        };
        await Mainrun();
        _client = new DiscordSocketClient(socketConfig);
        _client.Log += Log;
        _client.MessageReceived += HandleMessageAsync;
        _client.UserJoined += UserJoined;
        await _client.LoginAsync(TokenType.Bot, DiscordBotToken);
        await _client.StartAsync();
        await Task.Delay(-1);
    }
    
   private async Task Log(LogMessage arg)
        {
            string logText = $"{DateTime.Now} [{arg.Severity}] {arg.Source}: {arg.Exception?.ToString() ?? arg.Message}";
            // This can be used to SetOut Console to a file btw :P
            Console.WriteLine(logText);
            string filePath = Path.Combine(startupPath, "Bot_logs.txt");
            try
            {
                if (!File.Exists(filePath))
                {
                    using (StreamWriter sw = File.CreateText(filePath))
                    {
                        await sw.WriteLineAsync(logText);
                    }
                }
                else
                {
                    // Append the log text to the existing file
                    using (StreamWriter sw = File.AppendText(filePath))
                    {
                        await sw.WriteLineAsync(logText);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(SocketMessage arg)
    {
        var message = arg as SocketUserMessage;
        if (message == null || message.Author == null || message.Author.IsBot)
        {
            return;
        }
        
        Console.WriteLine($"Received message: {message.Content}");
        int argPos = 0;
        string userfile = @"\UserCFG.ini";
        string botNickname = UserSettings(startupPath + userfile, "BotNickname");
        
        if (message.MentionedUsers.Any(user => user.Id == _client.CurrentUser.Id) || message.Content.Contains(botNickname, StringComparison.OrdinalIgnoreCase) || message.Content.ToLower().StartsWith("/ask"))
        {
            string query = message.Content.Replace(botNickname, "", StringComparison.OrdinalIgnoreCase).Trim();
            // logging for debugging
            // Console.WriteLine($"Processing query: {query}");
            string response = await Task.Run(() => GetOpenAiResponse(query)).ConfigureAwait(false);
            // logging for debugging
            //Console.WriteLine($"OpenAI Response: {response}");
            await message.Channel.SendMessageAsync(response);
            Console.WriteLine("Response sent");
        }
        
        if (message.Content.ToLower().StartsWith("/roll"))
        {
            var result = new Random().Next(1, 7);
            var embed = new EmbedBuilder
            {
                Title = "🎲 You rolled",
                Description = $"{result}",
                Color = Color.DarkRed 
            };
            await message.Channel.SendMessageAsync(embed: embed.Build());
            Console.WriteLine("Dice Roll Response sent");
        }
        
        string[] EightBallResponses = { "Yes", "No", "Maybe", "Ask again later", "Tf do you think?", "Bitch, you might", "Possibly" };
        Random rand = new Random();
        if (message.Content.ToLower().StartsWith("/8ball"))
        {
            string question = message.Content.Substring("/8ball".Length).Trim();

            if (!string.IsNullOrEmpty(question))
            {
                int randomEightBallMessage = rand.Next(EightBallResponses.Length);
                string messageToPost = EightBallResponses[randomEightBallMessage];

                await message.Channel.SendMessageAsync("```" + messageToPost + "```");
                Console.WriteLine("8ball Response sent");
            }
            else
            {
                await message.Channel.SendMessageAsync("```" + "Please ask a question after `/8ball`." + "```");
            }
        }
        
        if (message.Content.ToLower().StartsWith("/duel"))
        {
            var mentionedUsers = message.MentionedUsers;

            if (mentionedUsers.Any())
            {
                IUser challengedUser = mentionedUsers.First();
                await message.DeleteAsync();
                var challenger = message.Author;
                int challengerRoll = RollDice();
                int challengedRoll = RollDice();
                // Determine the winner based on the higher roll
                IUser winner = (challengerRoll > challengedRoll) ? challenger : challengedUser;

                // Create an embed response with the duel results
                var embed = new EmbedBuilder
                {
                    Title = "⚔️ Duel Results ⚔️",
                    Description = $"{challenger.Username} challenges {challengedUser.Username} to a duel!\n" +
                                  $"{challenger.Username} rolls a {challengerRoll}\n" +
                                  $"{challengedUser.Username} rolls a {challengedRoll}\n" +
                                  $"The winner is {winner.Mention}!",
                    Color = Color.DarkRed 
                };
                await message.Channel.SendMessageAsync(embed: embed.Build());
                Console.WriteLine("Duel response sent");
            }
            else
            {
                await message.Channel.SendMessageAsync("Please mention a user to challenge to a duel.");
            }
        }
        
        if (message.Content.ToLower().StartsWith("/help"))
        {
            var embed = new EmbedBuilder
            {
                Title = "🤖 List of available Bot Commands 🤖",
                Description = "/ask: Ask the bot a question." + Environment.NewLine + "/8ball: Ask the magic 8 ball a question." + Environment.NewLine + "/roll: Roll the dice" + Environment.NewLine + "/duel: Mention a user to slappeth in thine face, and challenge to a duel!" + Environment.NewLine + "/coinflip: Flip a coin, Heads or tails." + Environment.NewLine + "/pokemon <Pokemon Name> Get a Pokemons stats " + Environment.NewLine + "/yt Search for a video or song on youtube, and automatically post the first result." + Environment.NewLine + "/lfg: (Game name) <Number of players needed>" + Environment.NewLine + "(This command only works for those in the Streamer Role)" + Environment.NewLine + "/live <YourTwitchUsername> ",
                Color = Color.DarkRed // Custom color (DarkRed)
            };
            await message.Channel.SendMessageAsync(embed: embed.Build());
            Console.WriteLine("Help Response sent");
        }
        
        if (message.Content.ToLower().StartsWith("/say") && message.Author is SocketGuildUser auth)
        {
            if (auth.GuildPermissions.Administrator)
            {
                string messageContent = message.Content.Substring("/say".Length).Trim();
                await message.DeleteAsync();
                await message.Channel.SendMessageAsync(messageContent);
                Console.WriteLine("Say Command Sent");
            }
            else
            {
                await message.Channel.SendMessageAsync("You don't have permission to use this command.");
            }
        }
        
        if (message.Content.ToLower().StartsWith("/kick") && message.Author is SocketGuildUser author)
        {
            if (author.GuildPermissions.KickMembers)
            {
                var mention = message.MentionedUsers.FirstOrDefault();

                if (mention is SocketGuildUser userToKick)
                {
                    string displayName = (message.Author as SocketGuildUser)?.DisplayName ?? message.Author.Username;
                    await message.DeleteAsync();
                    await userToKick.KickAsync();
                    await message.Channel.SendMessageAsync(displayName + $" Kicked 🦵{mention.Username}#{mention.Discriminator} from the server. D:");
                    Console.WriteLine($"Kicked {mention.Username}#{mention.Discriminator} from the server.");
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please mention the user you want to kick.");
                }
            }
            else
            {
                await message.Channel.SendMessageAsync("You don't have permission to kick members.");
            }
        }
        
        if (message.Content.ToLower().StartsWith("/coinflip"))
        {
            var result = new Random().Next(2);
            string outcome = (result == 0) ? "Heads" : "Tails";

            var embed = new EmbedBuilder
            {
                Title = "🪙 Coin Flip",
                Description = outcome,
                Color = Color.DarkRed
            };

            await message.Channel.SendMessageAsync(embed: embed.Build());
            Console.WriteLine("Coin flip Response sent");
        }
        
        if (message.Content.ToLower().StartsWith("/lfg"))
        {
            var match = Regex.Match(message.Content, @"\(([^)]*)\) (\d+)");
            if (match.Success)
            {
                string userDefinedGameName = match.Groups[1].Value.Trim();
                int userDefinedPlayersNeeded;

                if (int.TryParse(match.Groups[2].Value, out userDefinedPlayersNeeded))
                {
                    var embed = new EmbedBuilder
                    {
                        Title = "🎮 Looking For Group 🎮",
                        Description = $"Game: {userDefinedGameName}\nPlayers Needed: {userDefinedPlayersNeeded}\n{message.Author.Mention}" + " is looking for someone to party up!" + Environment.NewLine + "React to this message if you want to play!",
                        Color = Color.DarkRed
                    };
                    await message.DeleteAsync();
                    var lfgMessage = await message.Channel.SendMessageAsync(embed: embed.Build());
                    await lfgMessage.AddReactionAsync(new Emoji("👍"));
                    Console.WriteLine("Looking For Group Response sent");
                }
                else
                {
                    await message.Channel.SendMessageAsync("Invalid number of players needed. Please use a valid number.");
                }
            }
            else
            {
                await message.Channel.SendMessageAsync("Invalid command format. Please use /lfg (game_name) <players_needed>");
            }
        }

        if (message.Content.ToLower().StartsWith("/pokemon"))
        {
            string pokemonName = message.Content.Substring("/pokemon".Length).Trim();

            // URL for the PokemonDB page
            string pokemonDbUrl = $"https://pokemondb.net/pokedex/{pokemonName.ToLower()}";
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(pokemonDbUrl);

                if (response.IsSuccessStatusCode)
                {
                    string htmlContent = await response.Content.ReadAsStringAsync();
                    // Parse
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(htmlContent);
        
                    // Get Pokemon Types
                    var typesNodes = doc.DocumentNode.SelectNodes("//th[contains(text(), 'Type')]/following-sibling::td//a");
                    string pokemonTypes = string.Join(", ", typesNodes?.Select(node => node.InnerText));
                    // Get Pokemon Capture Rate
                    var captureRateNode = doc.DocumentNode.SelectSingleNode("//th[contains(text(), 'Catch rate')]/following-sibling::td");
                    string captureRate = captureRateNode?.InnerText.Trim() ?? "N/A";
                    // Get Pokemon Base Stats
                    var baseStatsNodes = doc.DocumentNode.SelectNodes("//th[contains(text(), 'Base stats')]/following-sibling::td//td[@class='cell-num']");
                    string baseStats = baseStatsNodes != null ? string.Join(", ", baseStatsNodes.Select(node => node.InnerText)) : "N/A";
                    // Get Pokemon Species
                    var speciesNode = doc.DocumentNode.SelectSingleNode("//th[contains(text(), 'Species')]/following-sibling::td");
                    string species = speciesNode?.InnerText.Trim() ?? "N/A";           
                    // Get National Pokédex number
                    var nationalDexNode = doc.DocumentNode.SelectSingleNode("//th[text()='National №']/following-sibling::td");
                    string nationalDexNumber = nationalDexNode?.InnerText.Trim() ?? "N/A";

                    // Possible image classes in descending order of preference
                    string[] imageClasses = { "img-fixed img-sprite-v21", "img-fixed img-sprite-v8", "img-fixed img-sprite-v3" };
                    string imageUrl = null;
                    foreach (string imageClass in imageClasses)
                    {
                        var imageNode = doc.DocumentNode.SelectSingleNode($"//img[@class='{imageClass}']");
                        imageUrl = imageNode?.Attributes["src"]?.Value;
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            break;
                        }
                    }
                    // Extract height from the HTML
                    var heightNode = doc.DocumentNode.SelectSingleNode("//th[contains(text(), 'Height')]/following-sibling::td");
                    string height = WebUtility.HtmlDecode(heightNode?.InnerText ?? string.Empty);
                    // Extract weight from the HTML
                    var weightNode = doc.DocumentNode.SelectSingleNode("//th[contains(text(), 'Weight')]/following-sibling::td");
                    string weight = WebUtility.HtmlDecode(weightNode?.InnerText ?? string.Empty);

                    var embed = new EmbedBuilder
                    {
                        Title = $"Pokemon: {pokemonName}",
                        Description = $"National Number: {nationalDexNumber}",
                        Url = pokemonDbUrl, // Set the URL for the title
                        Color = Color.DarkRed, // You can choose a different color if desired
                    };

                    // Add additional details to the embed
                    embed.AddField("Capture Rate", captureRate);
                    embed.AddField("Species", species);
                    embed.AddField("Types", pokemonTypes);
                    embed.AddField("Height", height);
                    embed.AddField("Weight", weight);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        embed.ImageUrl = imageUrl;
                    }

                    await message.Channel.SendMessageAsync(embed: embed.Build());
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
        }

        if (message.Content.ToLower().StartsWith("/intro") && message.Author is SocketGuildUser authorlive)
        {
            string userfile2 = @"\UserCFG.ini";
            string youtubelink = UserSettings(startupPath + userfile2, "Youtube"); // Youtube link for intro message
            string twitch = UserSettings(startupPath + userfile2, "Twitch"); // Twitch link for intro message
            string steam = UserSettings(startupPath + userfile2, "Steam"); // Steam link for intro message
            string facebook = UserSettings(startupPath + userfile2, "Facebook"); // Facebook link for intro message
            string permaInviteUrl = UserSettings(startupPath + userfile2, "InviteLink"); // Invite link for intro message
            // Delete the original command message
            await message.DeleteAsync();
            string roleString = UserSettings(startupPath + userfile2, "ModeratorRole");
            string role2String = UserSettings(startupPath + userfile2, "StreamerRole");
            if (ulong.TryParse(roleString, out ulong modrole))
            {
                if (authorlive.GuildPermissions.Administrator || (authorlive.Roles.Any(r => r.Id == modrole)))
                {
                    string welcome = "https://cdn-longterm.mee6.xyz/plugins/welcome_message/banners/1161640178218049536/welcome.gif";
                    string rules = "https://cdn-longterm.mee6.xyz/plugins/welcome_message/banners/1161640178218049536/rules.png";
                    string links = "https://cdn-longterm.mee6.xyz/plugins/welcome_message/banners/1161640178218049536/links.png";
                    string mods = "https://cdn-longterm.mee6.xyz/plugins/welcome_message/banners/1161640178218049536/mods.png";
                    string invitelink = "https://cdn-longterm.mee6.xyz/plugins/welcome_message/banners/1161640178218049536/invite_link.png";
                    string servername = authorlive.Guild.Name;
                    
                    var embedWelcome = new EmbedBuilder
                    {
                        Title = $"Welcome to {servername}",
                        Description = "Hello and a warm welcome to all of our new members! 👋 We're thrilled to have you join our community ❤️\r\n\r\n\r\n\U0001f91d Please, take some time to introduce yourself to the community – share a bit about yourself, your interests, and what brings you here.\r\n\r\n\r\n📜 Before you dive in, please take a moment to read through our server rules. We want everyone to have a great time here, and following the guidelines helps create a positive environment for everyone.\r\n\r\n\r\nWe're excited to have you here, and we can't wait to get to know you better. Enjoy your stay! 🎉\r\n",
                        Color = Color.DarkRed
                    };

                    var embedRules = new EmbedBuilder
                    {
                        Description = "1. Be Respectful\r\nWe do NOT tolerate negative behavior towards members. Respect one another as we expect you to. We STRONGLY discourage ANY sort of discrimination.\r\n\r\n\r\n2. No Spamming\r\nSpamming is annoying, and makes every sad. Just don't do it.\r\n\r\n\r\n3. Maintain Channel Topics\r\nPlease don't make clutter. Type in the appropriate channels. We want OPTIMAL server organization.\r\n\r\n\r\n4. Religion/Political Views\r\nNo pushing any kind of religious or political views on others in the Discord! Opinions are opinions. Let's leave it at that!\r\n\r\n\r\n5. Have Fun!!\r\nBeing a part of the server is a fun and welcoming experience. Stay true and supportive to one another.\r\n",
                        Color = Color.DarkRed
                    };

                    var embedLinks = new EmbedBuilder
                    {
                        Title = "Social Links",
                        Description = "Check out our social links below, Like, Follow, and Subscribe! Roles given out based on supporter status.",
                        Color = Color.DarkRed
                    };
                    
                    embedLinks.AddField("\u200B", "\u200B", inline: true); // Zero-width space to create a column, hackyyyy
                    embedLinks.AddField("YouTube", $"[YouTube Channel]({youtubelink})", inline: true);
                    embedLinks.AddField("Twitch", $"[Twitch Channel]({twitch})", inline: true);
                    embedLinks.AddField("\u200B", "\u200B", inline: true); // Zero-width space to create a column, hackyyyy
                    embedLinks.AddField("Steam", $"[Steam Profile]({steam})", inline: true);
                    embedLinks.AddField("Facebook", $"[Facebook Page]({facebook})", inline: true);
                    embedLinks.AddField("\u200B", "\u200B", inline: true); // Zero-width space to create a column, hackyyyy
                    
                    var embedMods = new EmbedBuilder
                    {
                        Description = "Our moderation team is here to make sure this cozy little corner of Discord stays warm and safe for everyone. 🍵\r\n\r\n\r\nIf you ever come across any problems related to our server, don't hesitate to tag them in the channel. ❤️\r\n\r\n\r\nQuick heads-up: The moderation team won't handle issues through DMs between members.\r\nIf you run into any trouble there, it's best to report it directly to Discord's Trust & Safety and block the user.\r\n",
                        Color = Color.DarkRed
                    };

                    var embedInviteLink = new EmbedBuilder
                    {
                        Title = @"Spread the love by sharing our server invite link!",
                        Description = $@"```{permaInviteUrl}```",
                        Color = Color.DarkRed
                    };

                    // Combine all the messages and embeds into a single SendMessagesAsync call
                    await message.Channel.SendMessageAsync(welcome);
                    await message.Channel.SendMessageAsync(text: null, isTTS: false, embed: embedWelcome.Build());
                    await message.Channel.SendMessageAsync(rules);
                    await message.Channel.SendMessageAsync(text: null, isTTS: false, embed: embedRules.Build());
                    await message.Channel.SendMessageAsync(links);
                    await message.Channel.SendMessageAsync(text: null, isTTS: false, embed: embedLinks.Build());
                    await message.Channel.SendMessageAsync(mods);
                    await message.Channel.SendMessageAsync(text: null, isTTS: false, embed: embedMods.Build());
                    await message.Channel.SendMessageAsync(invitelink);
                    await message.Channel.SendMessageAsync(text: null, isTTS: false, embed: embedInviteLink.Build());
                    Console.WriteLine("Intro Message sent");
                }
            }
            else
            {
                Console.WriteLine("Error: Could not convert role ID from string to Ulong!");
            }

        }
        
        if (message.Content.ToLower().StartsWith("/yt"))
        {
            string userfile2 = @"\UserCFG.ini";
            string youtubeAPIKey = UserSettings(startupPath + userfile2, "YoutubeAPIKey"); // Get Youtube API Key
            string youtubeappname = UserSettings(startupPath + userfile2, "YoutubeAppName"); // Get Youtube Application Name
            string query = message.Content.Substring("/yt".Length).Trim();

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = youtubeAPIKey,
                ApplicationName = youtubeappname
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = query;
            searchListRequest.MaxResults = 1;
            var searchListResponse = await searchListRequest.ExecuteAsync();
            var searchResult = searchListResponse.Items.FirstOrDefault();

            if (searchResult != null)
            {
                var videoId = searchResult.Id.VideoId;
                var videoUrl = $"https://www.youtube.com/watch?v={videoId}";
                await message.DeleteAsync();
                await message.Channel.SendMessageAsync(message.Author.Mention + " Posted a Youtube Request");
                await message.Channel.SendMessageAsync(videoUrl);
                Console.WriteLine("Youtube search Response sent");
            }
            else
            {
                await message.Channel.SendMessageAsync("No search results found.");
                Console.WriteLine("No search results found.");
            }
        }

        if (message.Content.ToLower().StartsWith("/pm"))
        {
            var isAdmin = (message.Author as IGuildUser)?.GuildPermissions.Administrator ?? false;

            if (isAdmin)
            {
                var mentionedUser = message.MentionedUsers.FirstOrDefault();
                if (mentionedUser != null)
                {
                    string messageContent = message.Content.Substring("/pm".Length).Trim();
                    try
                    {
                        await message.DeleteAsync();
                        await mentionedUser.SendMessageAsync(messageContent);
                        Console.WriteLine("PM Command successfully sent");
                    }
                    catch (Exception ex)
                    {
                        // When the mentioned user has direct messages disabled
                        // await message.Channel.SendMessageAsync($"Failed to send a private message to {mentionedUser.Mention}. Error: {ex.Message}");
                        Console.WriteLine("PM Command Error: " + ex.Message);
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("Please mention a user to send a private message.");
                }
            }
            else
            {
                await message.Channel.SendMessageAsync("You don't have permission to use this command.");
            }
        } 
        if (message.Content.ToLower().StartsWith("/live") && message.Author is SocketGuildUser authlive)
        {
               string userfile2 = @"\UserCFG.ini";
               string roleString = UserSettings(startupPath + userfile2, "ModeratorRole");
               string role2String = UserSettings(startupPath + userfile2, "StreamerRole");
    if (ulong.TryParse(roleString, out ulong modrole) && ulong.TryParse(role2String, out ulong streamerrole))
    {
        if (authlive.GuildPermissions.Administrator || (authlive.Roles.Any(r => r.Id == modrole) || (authlive.Roles.Any(r => r.Id == streamerrole))))
        {
            string[] commandParts = message.Content.Split(' ');
            
            if (commandParts.Length >= 3)
            {
                string twitchName = commandParts[1];
                string gameName = string.Join(" ", commandParts.Skip(2));
                DateTime now = DateTime.Now;
                string displayName = (message.Author as SocketGuildUser)?.DisplayName ?? message.Author.Username;
                string formattedDateTime = now.ToString("MMMM dd yyyy" + Environment.NewLine + "'Time:' h:mm tt");

                await message.DeleteAsync();
                await message.Channel.SendMessageAsync($"@everyone {displayName} is now LIVE on Twitch TV playing {gameName}!" + Environment.NewLine +
                                                      $"Stream Started At: {formattedDateTime}" + Environment.NewLine +
                                                      $"https://www.twitch.tv/{twitchName}");
                Console.WriteLine("Twitch Update sent");
            }
            else
            {
                await message.Channel.SendMessageAsync("Please provide a Twitch username and game name. Usage: `/live twitchname gamename`");
            }
        }
        else
        {
            await message.Channel.SendMessageAsync("You don't have permission to use this command.");
        }
    }
    else
    {
        Console.WriteLine("Error: Could not convert role ID from string to Ulong!");
    }
}
        
        if (message.Content.ToLower().StartsWith("/purge") && message.Author is SocketGuildUser authPurge)
        {
            string userfile2 = @"\UserCFG.ini";
            string roleString = UserSettings(startupPath + userfile2, "ModeratorRole");
            if (ulong.TryParse(roleString, out ulong xForceRole))
            {
                var isAdmin = (message.Author as SocketGuildUser)?.GuildPermissions.Administrator ?? false;
                var hasSpecificRole = (message.Author as SocketGuildUser)?.Roles.Any(r => r.Id == xForceRole) ?? false;
                if (isAdmin || hasSpecificRole)
                {
                    int messagesToPurge = 100;
                    await message.DeleteAsync();
                    
                    var channel = message.Channel as SocketTextChannel;

                    if (channel != null)
                    {
                        var messages = await channel.GetMessagesAsync(messagesToPurge).FlattenAsync();
                        var messagesToDelete = messages.Where(m => (DateTimeOffset.Now - m.CreatedAt).TotalDays < 14);

                        await channel.DeleteMessagesAsync(messagesToDelete);
                        await channel.SendMessageAsync($"Purged {messagesToDelete.Count()} messages.");
                        Console.WriteLine("Successfully " + $"Purged {messagesToDelete.Count()} messages.");
                    }
                }
                else
                {
                    await message.Channel.SendMessageAsync("You don't have permission to use this command.");
                }
            }
            else
            {
                Console.WriteLine("Error: Could not get ModeratorRole ID(ulong) from config file!");
            }
        }

        if (message.Content.ToLower().StartsWith("/updatemsg"))
        {
            string userfile2 = @"\UserCFG.ini";
            string roleString = UserSettings(startupPath + userfile2, "ModeratorRole");
            
            if (ulong.TryParse(roleString, out ulong xForceRole))
            {
                var isAdmin = (message.Author as SocketGuildUser)?.GuildPermissions.Administrator ?? false;
                var hasSpecificRole = (message.Author as SocketGuildUser)?.Roles.Any(r => r.Id == xForceRole) ?? false;
                if (isAdmin || hasSpecificRole)
                {
                    string messageContent = message.Content.Substring("/updatemsg".Length).Trim();
                    string formattedDateTime = DateTime.Now.ToString("MMMM/dd/yyyy" + Environment.NewLine + "'Time:' h:mm tt");

                    var embed = new EmbedBuilder
                    {
                        Title = "📰 Server Update Information 📰" + Environment.NewLine + Environment.NewLine + "Date: " + formattedDateTime,
                        Description = messageContent,
                        Color = Color.DarkRed
                    };
                    await message.DeleteAsync();
                    await message.Channel.SendMessageAsync(embed: embed.Build());
                    Console.WriteLine("Global Message sent");
                }
                else
                {
                    await message.Channel.SendMessageAsync("You don't have permission to use this command.");
                }
            }
            else
            {
                Console.WriteLine("Error: Could not get ModeratorRole ID(ulong) from config file!");
            }
        }
        if (message.Content.ToLower().StartsWith("/poll"))
        {
            var match = Regex.Match(message.Content, @"/poll\s+(.*)");

            if (match.Success)
            {
                string pollContent = match.Groups[1].Value.Trim();
                await message.DeleteAsync();

                var pollMessage = await message.Channel.SendMessageAsync($"@everyone **Poll:** {pollContent}\n\nReact with 👍 for Yes, and 👎 for No.");

                await pollMessage.AddReactionAsync(new Emoji("👍"));
                await pollMessage.AddReactionAsync(new Emoji("👎"));
            }
            else
            {
                await message.Channel.SendMessageAsync("Invalid command format. Please use /poll <question>");
            }
        }
    }
    private async Task RegisterSlashCommands()
        {
            var commands = new List<SlashCommandBuilder>
    {
        new SlashCommandBuilder()
            .WithName("ask")
            .WithDescription("Ask the bot a question.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("query")
                .WithDescription("Your question")
                .WithType(ApplicationCommandOptionType.String)),

        new SlashCommandBuilder()
            .WithName("roll")
            .WithDescription("Roll the dice"),

        new SlashCommandBuilder()
            .WithName("8ball")
            .WithDescription("Ask the magic 8 ball a question.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("question")
                .WithDescription("The question you want to ask")
                .WithType(ApplicationCommandOptionType.String)),

               // More commands
};
            string userfile2 = @"\UserCFG.ini";
            string GuildID = UserSettings(startupPath + userfile2, "ServerID"); //Your Server ID
            if (ulong.TryParse(GuildID, out ulong ServerId))
            {
                foreach (var command in commands)
                {
                    var builtCommand = command.Build();
                    await _client.Rest.CreateGuildCommand(builtCommand, ServerId);
                }
                Console.WriteLine("Slash commands registered.");
            }
        }
    
    static void ExtractResourceToFile(string resourceName, string filePath)
    {
        try
        {
            using (Stream resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    Console.WriteLine($"Error: Resource '{resourceName}' not found.");
                    return;
                }
                using (FileStream fileStream = File.Create(filePath))
                {
                    resourceStream.CopyTo(fileStream);
                }
                Console.WriteLine($"Config File Not found, creating file: '{resourceName}' extracted to '{filePath}'.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting resource to file: {ex.Message}");
        }
    }
    
    private async Task UserJoined(SocketGuildUser user)
    {
        var channels = user.Guild.Channels;
        var targetChannel = channels.FirstOrDefault(c => c is ITextChannel) as ITextChannel;
        var rules = channels.FirstOrDefault(c => c is ITextChannel && c.Name.Equals("welcome_channel", StringComparison.OrdinalIgnoreCase)) as ITextChannel;
        var mainChannel = channels.FirstOrDefault(c => c is ITextChannel && c.Name.Equals("main-channel", StringComparison.OrdinalIgnoreCase)) as ITextChannel;
        string userfile2 = @"\UserCFG.ini";
        string roleString = UserSettings(startupPath + userfile2, "AutoRole");

        if (ulong.TryParse(roleString, out ulong xForceRole))
        {
            await user.AddRoleAsync(xForceRole);
            Console.WriteLine("AutoRole Successful, user given new role.");
        }
        else
        {
            Console.WriteLine("Error: Could not convert AutoRole to ulong from string, AutoRole Could Not Be Assigned!");
        }
        await mainChannel.SendMessageAsync($"HEYO! Welcome to the server {user.Mention}! Be sure to read the Rules in the " + rules.Mention + " !");
        Console.WriteLine("Welcome message sent");
    }
    
    private int RollDice()
    {

        return new Random().Next(1, 7);
    }
    
    public async Task<string> GetOpenAiResponse(string query)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GptApiKey}");
            // OpenAI ChatGPT related settings and deets
            string prompt = $"User: {query}\nChatGPT:";
            // Create message objects for the system and user messages
            var messages = new List<object>
        {
            new { role = "system", content = $"\"{contentstr}\"" },
            new { role = "user", content = query }
        };
            // Create a JSON object for the request payload, change model as OpenAI updates
            var requestBody = new
            {
                model = "gpt-3.5-turbo",
                messages = messages,
                max_tokens = 200
            };
            
            var jsonContent = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();

                var jsonResponse = JObject.Parse(result);
                var choices = jsonResponse["choices"];
                var firstChoice = choices.FirstOrDefault();
                var text = firstChoice["message"]["content"].ToString();

                return text ?? "Unable to parse OpenAI response.";
            }
            else
            {
                Console.WriteLine($"Error communicating with OpenAI API. Status code: {response.StatusCode}");
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error content: {errorContent}");

                return "Error communicating with OpenAI API.";
            }
        }
    }
}
