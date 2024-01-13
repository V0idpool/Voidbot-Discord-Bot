[!Voidbot Discord Bot README!]

If you have questions reach out to me directly via Discord, my username is: voidpool1


You will need multiple API's, they are listed below and links are included to make accounts to get your keys.

ChatGPT API: https://platform.openai.com/api-keys 
Create an account, Choose API Keys menu option on the far left. 
Click "+ Create new secret key" Name it anything, it will then give you a secret key (Your API Key).
Paste that key into the UserCFG.ini config file where it's required.

Youtube API Key: https://console.cloud.google.com/apis/dashboard
Create an account, Choose Credentials menu option on the far left. 
Click "+ Create Credentials" Name it anything youd like, it will then give you a secret key (Your API Key). (COPY THIS)
Paste that key into the UserCFG.ini config file where it's required.
Paste the Name you made for the api key into the UserCFG.ini config file where it's required.

Discord API: https://discord.com/developers/applications
Create an account, Choose Applications option on the far left. 
Click the "New Application" Button at the top right. Name it anything youd like, set up its description,
and click save if there is a save button.
Click on Bot, on the left of the page, under your bot username it will display a token key,
or it will display a "Reset Token" Button, click that. it will then give you a token key (Your Bot Token Key). (COPY THIS)
Paste that key into the UserCFG.ini config file where it's required.

BOT SETUP ON DISCORD DEVELOPER SITE: REQUIRED
On the Bot Tab turn OFF PUBLIC BOT, and turn OFF REQUIRES OAUTH2 CODE GRANT.
Now, staying on the Bot tab, you MUST have the Privileged Gateway Intents ALL enabled below.
PRESENCE INTENT Tick this ON
Required for your bot to receive Presence Update events.
SERVER MEMBERS INTENT Tick this ON
Required for your bot to receive events listed under GUILD_MEMBERS.
MESSAGE CONTENT INTENT Tick this ON
Required for your bot to receive message content in most messages.

Now, click the "OAuth2" Tab on the left. This will expand two more options, "General" and "URL Generator" Ignore General
and click on "URL Generator", this will give you alot of options. ONLY click the boxes "applications.commands" AND "bot".
Scroll down a bit, and give the bot permissions. It's generally a good idea to give it Administrator, so that it can
run all the commands needed.

Scroll to the bottom of this same page, and you will see a GENERATED URL at the bottom. COPY this and paste 
into your browser and it will invite the bot to your server to join. 

Command Documentation is located here: https://github.com/V0idpool/Voidbot-Discord-Bot/blob/main/COMMANDS.txt

If you like what I do, and would like to support me please consider donating.
BuyMeACoffee: https://www.buymeacoffee.com/voidpool or CashApp Donations: https://cash.app/$KenM1337/
