# BangGameBot
This is a Telegram Bot to play Bang!, a game by Emiliano Sciarra.
Info about the game:
  - Wikipedia: https://en.wikipedia.org/wiki/Bang!_(card_game)
  - Official rules: http://www.dvgiochi.net/bang/bang_rules.pdf
  - Official publisher's site: https://www.dvgiochi.com/



## Installing

  - Clone this repo wherever you want.
  - Put your bot's token in a .txt file.
     - In Telegram, use [@BotFather](http://t.me/BotFather) to create a new bot.
     - Copy the bot's token.
     - Create a new blank .txt file, and paste the token, then save and close.
  - In Program.cs, change settings according to your preferences
     - Change `renyhp` to your Telegram ID. That account is the only one authorised to use the /photoid command, and it receives error logs in PM.
     - Change `TokenPath` to make it point to wherever you put the .txt file that you created in the previous step. Currently, it will search for token.txt in the same directory as the build.
  - Build the solution with Visual Studio.
  - Play!
