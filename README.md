# FGDiscordBotTing
A discord bot for "blowing the whistle" on TING so that people don't go overtime.

# How it works
This bot will read a schedule from a schedule.txt file and then announce when meetings/breaks start in a  with a live updating countdown how long is left for that particular event. If the event is a meeting, it will also send a reminder message when half of the meeting time is remaining, when 2 minutes remain and when 1 minute remains. See demo below.

![Bot](https://user-images.githubusercontent.com/56596714/158211860-64da146f-5925-4b38-901a-e377b535ea3e.gif)

# Setup
1. Download the zip file in the latest release (or compile it yourself).
2. Unzip the zip file and put it in a folder.
3. Create a discord bot via discords developer portal (see [Creating the discord bot](#creating-the-discord-bot) below for more info om how to do this).
4. Write the schedule the time keeper bot should follow in the schedule.txt file (see [Schedule Formatting](#schedule-formatting) for guide on how to write this file).
5. Start the discord bot by opening the exe file in the folder where you unzipped. If it doesn't crash it should work! Try typing !test on the discord server to make sure that everything works.
6. To actually make the bot start keeping time, type !setup in a discord channel of choice (the bot will type quite a lot of messages so having a dedicated discord channel just for time keeping is recommended!).

# Creating the discord bot
If you haven't created a discord bot before, this is how you do it: 
1. Go to https://discord.com/developers/applications. 
2. Log in to your discord account if you aren't already.
3. Then press create application, name it to something that makes sense (maybe "TimeKeeper"?).
4. Go into your newly created application, here you can find your application id which you will need later to create your invite link.
5. Click on the bot list item to the left and click "Add Bot". Name it to something that makes sense (I prefer to name my application the same as my bot).
6. Pretty much done! Now you just click to reveal the bot token and save it in your token.txt file.
7. Generate a new token for your discord bot and save the token in token.txt (in the folder where you unzipped), this file should only contain the token, anything else including spaces and new lines will crash the program.
8. Create an invite link for your bot using this page: https://discordapi.com/permissions.html#3072, paste in your discord application id. The required permissions are already pre-selected if you use the link above (view channels, send messages).
9. Click the invite link to invite the discord bot to a discord server you are admin for.

# Schedule Formatting
The schedule.txt file contains all the events of the day. The zip file in releases will hold an example of how this file would look.
Lines that start with "//" are treated as comments and are simply skipped over.

Each line that aren't a comment should follow this formatting standard [START_TIME] [END_TIME] [EVENT_TYPE].

This is an example of how that line would look "09:00 09:10 Meeting".

Possible event types are: "Meeting" and "Break".

There should also not be any dead time between events, so for instance if there is a 09:00 09:10 Meeting and 09:20 09:30 Meeting, there should also be a explicit break in between so no time is unaccounted for. Like this: 09:10 09:20 Break.

Please note that any additional spaces or something that doesn't follow this precise format will result in a crash of the program.
