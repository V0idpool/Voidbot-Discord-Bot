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

    private Task Log(LogMessage arg)
    {
        Console.WriteLine(arg);
        return Task.CompletedTask;
    }

    private async Task HandleMessageAsync(SocketMessage arg)
    {

        var message = arg as SocketUserMessage;
        if (message == null || message.Author == null || message.Author.IsBot)
        {
            // Either the message is null or the author is null or the author is a bot, so we don't process it
            return;
        }


        Console.WriteLine($"Received message: {message.Content}");
        int argPos = 0;
        string userfile = @"\UserCFG.ini";
        string botNickname = UserSettings(startupPath + userfile, "BotNickname"); // Gets the bots nickname defined in UserCFG.ini

        if (message.MentionedUsers.Any(user => user.Id == _client.CurrentUser.Id) || message.Content.Contains(botNickname, StringComparison.OrdinalIgnoreCase) || message.Content.ToLower().StartsWith("/ask"))
        {
            string query = message.Content.Replace(botNickname, "", StringComparison.OrdinalIgnoreCase).Trim();
            // Add logging for debugging
            // Console.WriteLine($"Processing query: {query}");
            string response = await Task.Run(() => GetOpenAiResponse(query)).ConfigureAwait(false);
            // Add logging for debugging
            //Console.WriteLine($"OpenAI Response: {response}");
            await message.Channel.SendMessageAsync(response);

            Console.WriteLine("Response sent");
        }
        if (message.Content.ToLower().StartsWith("/roll"))
        {
            // Generate a random number between 1 and 6
            var result = new Random().Next(1, 7);

            // Create an embed response with a custom color
            var embed = new EmbedBuilder
            {
                Title = "🎲 You rolled",
                Description = $"{result}",
                Color = Color.DarkRed 
            };

            // Send the embed response to the channel
            await message.Channel.SendMessageAsync(embed: embed.Build());

            // Log to console
            Console.WriteLine("Dice Roll Response sent");
        }
        // EightBall is an array of responses
        string[] EightBallResponses = { "Yes", "No", "Maybe", "Ask again later", "Tf do you think?", "Bitch, you might", "Possibly" };
        // Create an instance of the Random class
        Random rand = new Random();
        if (message.Content.ToLower().StartsWith("/8ball"))
        {
            // Check if the message contains a question
            string question = message.Content.Substring("/8ball".Length).Trim();

            if (!string.IsNullOrEmpty(question))
            {
                // Get a random response from the EightBallResponses array
                int randomEightBallMessage = rand.Next(EightBallResponses.Length);
                string messageToPost = EightBallResponses[randomEightBallMessage];

                // Respond with the 8-ball answer
                await message.Channel.SendMessageAsync("```" + messageToPost + "```");
                Console.WriteLine("8ball Response sent");
            }
            else
            {
                // If no question is provided, prompt the user to ask a question
                await message.Channel.SendMessageAsync("```" + "Please ask a question after `/8ball`." + "```");
            }
        }
        if (message.Content.ToLower().StartsWith("/duel"))
        {
            // Extract the mentioned user from the message
            var mentionedUsers = message.MentionedUsers;

            // Ensure there is at least one mentioned user
            if (mentionedUsers.Any())
            {
                IUser challengedUser = mentionedUsers.First();

                // Delete the original command message
                await message.DeleteAsync();

                // Get the user who invoked the command
                var challenger = message.Author;

                // Roll a 6-sided die for each participant
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
                    Color = Color.DarkRed // Custom color (DarkRed)
                };

                // Send the embed response to the same channel
                await message.Channel.SendMessageAsync(embed: embed.Build());

                // Log to console
                Console.WriteLine("Duel response sent");
            }
            else
            {
                // If no mentioned user, inform the user to mention someone
                await message.Channel.SendMessageAsync("Please mention a user to challenge to a duel.");
            }
        }



        if (message.Content.ToLower().StartsWith("/help"))
        {
          

            // Create an embed response with a custom color
            var embed = new EmbedBuilder
            {
                Title = "🤖 List of available Bot Commands 🤖",
                Description = "/ask: Ask the bot a question." + Environment.NewLine + "/8ball: Ask the magic 8 ball a question." + Environment.NewLine + "/roll: Roll the dice" + Environment.NewLine + "/duel: Mention a user to slappeth in thine face, and challenge to a duel!" + Environment.NewLine + "/coinflip: Flip a coin, Heads or tails." + Environment.NewLine + "/pokemon <Pokemon Name> Get a Pokemons stats " + Environment.NewLine + "/yt Search for a video or song on youtube, and automatically post the first result." + Environment.NewLine + "/lfg: (Game name) <Number of players needed>" + Environment.NewLine + "(This command only works for those in the Streamer Role)" + Environment.NewLine + "/live <YourTwitchUsername> ",
                Color = Color.DarkRed // Custom color (DarkRed)
            };

            // Send the embed response to the channel
            await message.Channel.SendMessageAsync(embed: embed.Build());

            // Log to console
            Console.WriteLine("Help Response sent");
        }
        if (message.Content.ToLower().StartsWith("/say") && message.Author is SocketGuildUser auth)
        {
            // Check if the author has the "Administrator" permission
            if (auth.GuildPermissions.Administrator)
            {
                // Extract the message content after "/say"
                string messageContent = message.Content.Substring("/say".Length).Trim();

                // Delete the original command message
                await message.DeleteAsync();

                // Send the echoed message to the channel
                await message.Channel.SendMessageAsync(messageContent);

                // Log to console
                Console.WriteLine("Say Command Sent");
            }
            else
            {
                // Inform the user that they don't have the necessary permission
                await message.Channel.SendMessageAsync("You don't have permission to use this command.");
            }
        }
        if (message.Content.ToLower().StartsWith("/kick") && message.Author is SocketGuildUser author)
        {
            // Check if the author has the "Kick Members" permission
            if (author.GuildPermissions.KickMembers)
            {
                // Extract the mention from the message
                var mention = message.MentionedUsers.FirstOrDefault();

                // Check if a user is mentioned
                if (mention is SocketGuildUser userToKick)
                {
                    string displayName = (message.Author as SocketGuildUser)?.DisplayName ?? message.Author.Username;
                    // Check if the mentioned user is a bot
                    await message.DeleteAsync();
                    // Kick the mentioned user
                    await userToKick.KickAsync();
                    // Log to console
                    await message.Channel.SendMessageAsync(displayName + $" Kicked 🦵{mention.Username}#{mention.Discriminator} from the server. D:");
                    Console.WriteLine($"Kicked {mention.Username}#{mention.Discriminator} from the server.");
                }
                else
                {
                    // Inform the user that no user was mentioned
                    await message.Channel.SendMessageAsync("Please mention the user you want to kick.");
                }
            }
            else
            {
                // Inform the user that they don't have the necessary permission
                await message.Channel.SendMessageAsync("You don't have permission to kick members.");
            }
        }
        if (message.Content.ToLower().StartsWith("/coinflip"))
        {
            // Generate a random result (0 or 1) for heads or tails
            var result = new Random().Next(2);

            // Determine the outcome based on the random result
            string outcome = (result == 0) ? "Heads" : "Tails";

            // Create an embed response with a custom color
            var embed = new EmbedBuilder
            {
                Title = "🪙 Coin Flip",
                Description = outcome,
                Color = Color.DarkRed // You can choose a different color if desired
            };

            // Send the embed response to the channel
            await message.Channel.SendMessageAsync(embed: embed.Build());

            // Log to console
            Console.WriteLine("Coin flip Response sent");
        }
        if (message.Content.ToLower().StartsWith("/lfg"))
        {
            // Use a regular expression to extract the game name inside parentheses
            var match = Regex.Match(message.Content, @"\(([^)]*)\) (\d+)");

            // Check if the regular expression matches
            if (match.Success)
            {
                // Extract game name and players needed from the match
                string userDefinedGameName = match.Groups[1].Value.Trim();
                int userDefinedPlayersNeeded;

                // Try to parse the number of players needed
                if (int.TryParse(match.Groups[2].Value, out userDefinedPlayersNeeded))
                {
                    // Create an embed response with a custom color
                    var embed = new EmbedBuilder
                    {
                        Title = "🎮 Looking For Group 🎮",
                        Description = $"Game: {userDefinedGameName}\nPlayers Needed: {userDefinedPlayersNeeded}\n{message.Author.Mention}" + " is looking for someone to party up!" + Environment.NewLine + "React to this message if you want to play!",
                        Color = Color.DarkRed // You can choose a different color if desired
                    };

                    // Delete the original command message
                    await message.DeleteAsync();

                    // Send the embed response to the channel
                    var lfgMessage = await message.Channel.SendMessageAsync(embed: embed.Build());

                    // Add a thumbs-up reaction to the LFG message by the person who ran the /lfg command
                    await lfgMessage.AddReactionAsync(new Emoji("👍"));

                    // Log to console
                    Console.WriteLine("Looking For Group Response sent");
                }
                else
                {
                    // Handle the case where the provided players needed is not a valid number
                    await message.Channel.SendMessageAsync("Invalid number of players needed. Please use a valid number.");
                }
            }
            else
            {
                // Handle the case where the command format is incorrect
                await message.Channel.SendMessageAsync("Invalid command format. Please use /lfg (game_name) <players_needed>");
            }
        }


        if (message.Content.ToLower().StartsWith("/pokemon"))
        {
            // Extract the Pokémon name from the message content
            string pokemonName = message.Content.Substring("/pokemon".Length).Trim();

            // Construct the URL for the PokemonDB page
            string pokemonDbUrl = $"https://pokemondb.net/pokedex/{pokemonName.ToLower()}";

            // Make a request to the PokemonDB website
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(pokemonDbUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Read the HTML content
                    string htmlContent = await response.Content.ReadAsStringAsync();

                    // Parse the HTML
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(htmlContent);

                    // Extract information using XPath or other methods
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



                    // Define an array of possible image classes in descending order of preference
                    string[] imageClasses = { "img-fixed img-sprite-v21", "img-fixed img-sprite-v8", "img-fixed img-sprite-v3" };

                    // Initialize imageUrl to null
                    string imageUrl = null;

                    // Loop through each image class and try to find the image URL
                    foreach (string imageClass in imageClasses)
                    {
                        var imageNode = doc.DocumentNode.SelectSingleNode($"//img[@class='{imageClass}']");
                        imageUrl = imageNode?.Attributes["src"]?.Value;

                        // If imageUrl is found, break out of the loop
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            break;
                        }
                    }

                    // Extract height from the HTML and decode special characters
                    var heightNode = doc.DocumentNode.SelectSingleNode("//th[contains(text(), 'Height')]/following-sibling::td");
                    string height = WebUtility.HtmlDecode(heightNode?.InnerText ?? string.Empty);

                    // Extract weight from the HTML and decode special characters
                    var weightNode = doc.DocumentNode.SelectSingleNode("//th[contains(text(), 'Weight')]/following-sibling::td");
                    string weight = WebUtility.HtmlDecode(weightNode?.InnerText ?? string.Empty);



                    // Create an embed response with the extracted information
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



                    // Add image URL to the embed, if available
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        embed.ImageUrl = imageUrl;
                    }

                    // Send the embed response to the channel
                    await message.Channel.SendMessageAsync(embed: embed.Build());
                }
                else
                {
                    // Handle the case where the request is not successful
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

            // Check if the author has the "Administrator" and your Defined Moderator permission (In CFG File)

            
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
                    embedLinks.AddField("\u200B", "\u200B", inline: true); // Zero-width space to create a column
                    embedLinks.AddField("YouTube", $"[YouTube Channel]({youtubelink})", inline: true);
                    embedLinks.AddField("Twitch", $"[Twitch Channel]({twitch})", inline: true);
                    embedLinks.AddField("\u200B", "\u200B", inline: true); // Zero-width space to create a column
                    embedLinks.AddField("Steam", $"[Steam Profile]({steam})", inline: true);
                    embedLinks.AddField("Facebook", $"[Facebook Page]({facebook})", inline: true);
                    embedLinks.AddField("\u200B", "\u200B", inline: true); // Zero-width space to create a column
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

                    // Log to console
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
                // Delete the original command message
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
            // Check if the user invoking the command has the "Administrator" permission
            var isAdmin = (message.Author as IGuildUser)?.GuildPermissions.Administrator ?? false;

            if (isAdmin)
            {
                // Extract the mentioned user
                var mentionedUser = message.MentionedUsers.FirstOrDefault();

                // Check if a user is mentioned
                if (mentionedUser != null)
                {
                    // Extract the text after the command
                    string messageContent = message.Content.Substring("/pm".Length).Trim();

                    try
                    {
                        // Delete the original command message
                        await message.DeleteAsync();

                        // Send a private message to the mentioned user with the specified content
                        await mentionedUser.SendMessageAsync(messageContent);
                        Console.WriteLine("PM Command successfully sent");
                     
                    }
                    catch (Exception ex)
                    {
                        // Exceptions, for example, when the mentioned user has direct messages disabled
                        // await message.Channel.SendMessageAsync($"Failed to send a private message to {mentionedUser.Mention}. Error: {ex.Message}");
                        Console.WriteLine("PM Command Error: " + ex.Message);
                    }
                }
                else
                {
                    // If no user is mentioned, inform the user to mention someone
                    await message.Channel.SendMessageAsync("Please mention a user to send a private message.");
                }
            }
            else
            {
                // If the user doesn't have the "Administrator" permission, inform them
                await message.Channel.SendMessageAsync("You don't have permission to use this command.");
            }
        } 
        if (message.Content.ToLower().StartsWith("/live") && message.Author is SocketGuildUser authlive)
{
    // Check if the author has the "Administrator" permission
       
    string userfile2 = @"\UserCFG.ini";
    string roleString = UserSettings(startupPath + userfile2, "ModeratorRole");
    string role2String = UserSettings(startupPath + userfile2, "StreamerRole");

    if (ulong.TryParse(roleString, out ulong modrole) && ulong.TryParse(role2String, out ulong streamerrole))
    {
        if (authlive.GuildPermissions.Administrator || (authlive.Roles.Any(r => r.Id == modrole) || (authlive.Roles.Any(r => r.Id == streamerrole))))
        {
            // Split the command to get the Twitch username and game as arguments
            string[] commandParts = message.Content.Split(' ');

            // Check if the command has at least two parts ("/live" and the Twitch username)
            if (commandParts.Length >= 3)
            {
                // Get the Twitch username and game from the command
                string twitchName = commandParts[1];
                string gameName = string.Join(" ", commandParts.Skip(2));

                // Get the current date and time
                DateTime now = DateTime.Now;

                // Get the display name of the user
                string displayName = (message.Author as SocketGuildUser)?.DisplayName ?? message.Author.Username;

                // Format the date and time as MonthName/day/year Time: hour:minute AM/PM
                string formattedDateTime = now.ToString("MMMM dd yyyy" + Environment.NewLine + "'Time:' h:mm tt");

                // Delete the original command message
                await message.DeleteAsync();

                // Send the echoed message to the channel
                await message.Channel.SendMessageAsync($"@everyone {displayName} is now LIVE on Twitch TV playing {gameName}!" + Environment.NewLine +
                                                      $"Stream Started At: {formattedDateTime}" + Environment.NewLine +
                                                      $"https://www.twitch.tv/{twitchName}");

                // Log to console
                Console.WriteLine("Twitch Update sent");
            }
            else
            {
                // Inform the user that they need to provide a Twitch username and game name
                await message.Channel.SendMessageAsync("Please provide a Twitch username and game name. Usage: `/live twitchname gamename`");
            }
        }
        else
        {
            // Inform the user that they don't have the necessary permission
            await message.Channel.SendMessageAsync("You don't have permission to use this command.");
        }

    }
    else
    {
        Console.WriteLine("Error: Could not convert role ID from string to Ulong!");
    }
}


        // Add this block of code inside your HandleMessageAsync method

        // Add this block of code inside your HandleMessageAsync method

        if (message.Content.ToLower().StartsWith("/purge") && message.Author is SocketGuildUser authPurge)
        {
            // Check if the author has the "Administrator" permission
            string userfile2 = @"\UserCFG.ini";
            string roleString = UserSettings(startupPath + userfile2, "ModeratorRole");
            if (ulong.TryParse(roleString, out ulong xForceRole))
            {
                var isAdmin = (message.Author as SocketGuildUser)?.GuildPermissions.Administrator ?? false;
                var hasSpecificRole = (message.Author as SocketGuildUser)?.Roles.Any(r => r.Id == xForceRole) ?? false;
                if (isAdmin || hasSpecificRole)
                {
                    // Get the number of messages to purge (replace 100 with the desired number)
                    int messagesToPurge = 100;

                    // Delete the original command message
                    await message.DeleteAsync();

                    // Get the channel
                    var channel = message.Channel as SocketTextChannel;

                    // Check if the channel is not null
                    if (channel != null)
                    {
                        // Fetch messages and filter out those older than two weeks
                        var messages = await channel.GetMessagesAsync(messagesToPurge).FlattenAsync();
                        var messagesToDelete = messages.Where(m => (DateTimeOffset.Now - m.CreatedAt).TotalDays < 14);

                        // Delete the filtered messages
                        await channel.DeleteMessagesAsync(messagesToDelete);

                        // Inform about the purge
                        await channel.SendMessageAsync($"Purged {messagesToDelete.Count()} messages.");
                        Console.WriteLine("Successfully " + $"Purged {messagesToDelete.Count()} messages.");
                    }
                }
                else
                {
                    // Inform the user that they don't have the necessary permission
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
            // Check if the author is an administrator or has a specific role with role ID
           
            if (ulong.TryParse(roleString, out ulong xForceRole))
            {
                var isAdmin = (message.Author as SocketGuildUser)?.GuildPermissions.Administrator ?? false;
                var hasSpecificRole = (message.Author as SocketGuildUser)?.Roles.Any(r => r.Id == xForceRole) ?? false;
                if (isAdmin || hasSpecificRole)
                {
                    // Extract the text after the command
                    string messageContent = message.Content.Substring("/updatemsg".Length).Trim();
                    string formattedDateTime = DateTime.Now.ToString("MMMM/dd/yyyy" + Environment.NewLine + "'Time:' h:mm tt");
                    // Create an embed response with a custom color
                    var embed = new EmbedBuilder
                    {
                        Title = "📰 Server Update Information 📰" + Environment.NewLine + Environment.NewLine + "Date: " + formattedDateTime,
                        Description = messageContent,
                        Color = Color.DarkRed // You can choose a different color if desired
                    };
                    await message.DeleteAsync();
                    // Send the embed response to the channel
                    await message.Channel.SendMessageAsync(embed: embed.Build());

                    // Log to console
                    Console.WriteLine("Global Message sent");
                }
                else
                {
                    // Inform the user that they don't have the necessary permission
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
            // Extract the poll question and options from the message content
            var match = Regex.Match(message.Content, @"/poll\s+(.*)");

            if (match.Success)
            {
                string pollContent = match.Groups[1].Value.Trim();
                await message.DeleteAsync();
                // Create a poll message
                var pollMessage = await message.Channel.SendMessageAsync($"@everyone **Poll:** {pollContent}\n\nReact with 👍 for Yes, and 👎 for No.");

                // Add reactions for Yes and No
                await pollMessage.AddReactionAsync(new Emoji("👍"));
                await pollMessage.AddReactionAsync(new Emoji("👎"));
            }
            else
            {
                // Handle the case where the command format is incorrect
                await message.Channel.SendMessageAsync("Invalid command format. Please use /poll <question>");
            }
        }



    }
    private async Task RegisterSlashCommands()
    {
        var commands = new List<SlashCommandBuilder>
    {
        // Your existing commands here

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

        // Add more commands as needed
    };
        ulong GuildID = 745824494035271750;
        // Register each command with the guild
        foreach (var command in commands)
        {
            var builtCommand = command.Build();
            await _client.Rest.CreateGuildCommand(builtCommand, GuildID); // Replace YourGuildId with your actual guild ID
        }

        Console.WriteLine("Slash commands registered.");
    }


    private async Task UserJoined(SocketGuildUser user)
    {
        // Get all channels in the server
        var channels = user.Guild.Channels;

        // Select the first text channel (you can modify this logic based on your requirements)
        var targetChannel = channels.FirstOrDefault(c => c is ITextChannel) as ITextChannel;
        var rules = channels.FirstOrDefault(c => c is ITextChannel && c.Name.Equals("welcome_channel", StringComparison.OrdinalIgnoreCase)) as ITextChannel;
        // Get the main channel by name (replace "main-channel" with your actual main channel name)
        var mainChannel = channels.FirstOrDefault(c => c is ITextChannel && c.Name.Equals("main-channel", StringComparison.OrdinalIgnoreCase)) as ITextChannel;
        string userfile2 = @"\UserCFG.ini";
        string roleString = UserSettings(startupPath + userfile2, "AutoRole");

        if (ulong.TryParse(roleString, out ulong xForceRole))
        {
            // Add the X-Force role to the user
            await user.AddRoleAsync(xForceRole);
            // Successfully converted to ulong, now you can use xForceRole
            Console.WriteLine("AutoRole Successful, user given new role.");
        }
        else
        {
            // Handle the case where the conversion failed
            Console.WriteLine("Error: Could not convert AutoRole to ulong from string, AutoRole Could Not Be Assigned!");
        }




        await mainChannel.SendMessageAsync($"HEYO! Welcome to the server {user.Mention}! Be sure to read the Rules in the " + rules.Mention + " !");
        Console.WriteLine("Welcome message sent");
    }




    // Check if the user's ID matches the one you want to kick, just for the lawls
    //ulong userIdToKick = USERID;

    //if (user.Id == userIdToKick)
    //{
    //    // Get all channels in the server
    //    var channels = user.Guild.Channels;

    //    // Select the first text channel (you can modify this logic based on your requirements)
    //    var targetChannel = channels.FirstOrDefault(c => c is ITextChannel) as ITextChannel;

    //    if (targetChannel != null)
    //    {
    //        // Kick the user
    //        await user.KickAsync("Auto-kicked based on user ID.");

    //        // Send a message to the selected channel
    //        await targetChannel.SendMessageAsync($"User {user.Username}#{user.Discriminator} ({user.Id}) was auto-kicked. BE GONE VILE MAN, BE GONE WITH YOU! :)");

    //        Console.WriteLine($"User {user.Username}#{user.Discriminator} ({user.Id}) auto-kicked based on user ID.");
    //    }
    //}
    //}


    private int RollDice()
    {
        // Generate a random number between 1 and 6 for a 6-sided die
        return new Random().Next(1, 7);
    }
    public async Task<string> GetOpenAiResponse(string query)
    {

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {GptApiKey}");

            string prompt = $"User: {query}\nChatGPT:";
            // Create message objects for the system and user messages
            var messages = new List<object>
        {
            new { role = "system", content = $"\"{contentstr}\"" },
            new { role = "user", content = query }
        };
            // Create a JSON object for the request payload
            var requestBody = new
            {

                model = "gpt-3.5-turbo",
                messages = messages,
                max_tokens = 200
            };

            // Serialize the JSON object to a string
            var jsonContent = JsonConvert.SerializeObject(requestBody);

            // Use StringContent with the correct content type
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);


            if (response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();

                // Parsing JSON to extract the response text
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
