<html>
   <body>
      <h1 align="center" style="position: relative;">
         <img src="LogoBanner.png?raw=true" width="200" style="border-radius: 50%;" align="center">
         <br>
         Dexter MASZ
      </h1>
      <h3 align="center">The official USFurries Discord bot!</h3>
      <h4 align="center">Used to help moderate the server and provides useful tools. Made with love <3</h4>
      <p align="center">
         <a href="https://discord.gg/DBS664yjWN">
         <img alt="Discord" src="https://img.shields.io/discord/613441321751019550?color=%237289DA&label=Discord&style=for-the-badge">
         </a>
         <a href="https://github.com/FeroxFoxxo/DexterMASZ/blob/master/LICENSE">
         <img alt="GitHub" src="https://img.shields.io/github/license/feroxfoxxo/dextermasz?label=License&style=for-the-badge">
         </a>
      </p>
      <h2>Built With</h2>
      <p>
        <ul>
          <li><a href="https://dotnet.microsoft.com/download/dotnet/8.0">.NET 8.0</a></li>
          <li><a href="https://github.com/discord-net/Discord.Net">Discord.NET</a></li>
          <li><a href="https://github.com/angelobreuer/Lavalink4NET">Lavalink4Net</a> - Music Plugin</li>
        </ul>
      </p>
      <h2>Commemorations</h2>
      <p>
         This bot is an adaption from the version of Dexter, written in JavaScript, by <a href="https://github.com/Jakey-F">Jakey Floofle</a>. As such, some commands have been adaptions from their original sources.
      </p>
      <p>
         This bot was originally built off of <a href="https://github.com/zaanposni/discord-masz">MASZ</a>, but extends upon it by offering much more, general features, and a refreshed, plugin-based, abstracted backend!
      </p>
   </body>
</html>

## Features

⭐ **A website and a discord bot** - to use Dexter\
⭐ **Localization** - timezones and languages are fully customizable\
⭐ **Userscan** - quickly spot relations between users with an included visualization

### Plugins

⭐ **Levelling** - including a leaderboard and rank system\
⭐ **Infractions and managed (temporary) punishments** - to moderate your server\
⭐ **Quicksearch** - to reliably search for any infractions or notes a user has\
⭐ **Automoderation** - to give trolls no chance\
⭐ **Ban appeals and webhook notifications** - to moderate your server transparently\
⭐ **Music Player** - for playing/pausing/resuming etc music from spotify and youtube, curtosy of <a href="https://github.com/Swyreee/Lilia/tree/master/Lilia">Lilia</a>

### 👀 Preview

![dashboard preview](https://raw.githubusercontent.com/FeroxFoxxo/DexterMASZ/master/.github/dashboard.png)

## 🛠 Hosting

You can **host your own instance of Dexter** by using the instructions below.\
However! These instructions are only meant for developing.
You should be using the official Dexter instance on your server.

#### TL;DR;

- Create a discord application at [https://discord.com/developers/applications](https://discord.com/developers/applications)
- Set redirect urls on your discord application [as defined](https://github.com/FeroxFoxxo/DexterMASZ#discord-oauth).
- Enable **Server Members** and **Message Content Intent** in your bot settings.
- App will be hosted on `127.0.0.1:5565`.
- Read further for more information on different deployment methods and further steps.

#### If you want to deploy on a domain:

- a (sub)domain to host the application on
- a reverse proxy on your host

#### Request logging and ratelimit

Dexter uses the `X-Forwarded-For` http header for logging and ratelimit.\
Ensure that this header is set in your reverse proxy for best experience.

#### MySQL Errors

Entity Framework implores some new features of MySQL for sake of optimising calls to the database.\
As such, it is recommended you install MySQL 8+ to use this new syntax. Otherwise, you will encounter
a `MySQLException` stating you need to check your MySQL version corresponds correctly with the version in your manual.

### 💻 Local hosting

- Start your bot from the `Launch` project and enter in the variables as required. These are set environmentally.
- To launch the frontend, run `npm start` from the terminal in the directory of frontend/dexter. Make sure to install dependencies prior using `npm install`.

### ↪ After Deployment


- You can visit your application at `yourdomain.com` (or `127.0.0.1:5565`). You will see a login screen that will ask you to authenticate yourself using Discord OAuth2.
- After authorizing your service to use your Discord account you will see your profile picture in the toolbar (this is hosted on `127.0.0.1:4200` when developing).
- If you are logged in as a siteadmin, you can use the "register guild" (+) button to register your guilds and to get started.
- Based on wanted features and functionalities you might have to grant your bot advanced permissions, read above under `Enabling Restricted Features`.

## 🚀 Discord Permissions

### Discord OAuth

Create your own OAuth application [here](https://discord.com/developers/applications).
Also set the redirect paths in the tab `OAuth2`.\
Be sure to set the following (choose localhost or domain depending on your deployment):

![redirect example](https://raw.githubusercontent.com/zaanposni/discord-masz/master/docs/redirects.png)

### Bot Intents

Enable **Server Members** and **Message Content Intent** in your bot settings.

![intents example](https://raw.githubusercontent.com/zaanposni/discord-masz/master/docs/intents.png)

### Enabling Restricted Features For Inbuilt Plugins

#### ⭐ Unban request feature

If you want banned users to see their cases, grant your bot the `ban people` permission.\
This way they can see the reason for their ban and comment or send an unban request.\
Furthermore, make sure the bot is high enough in the role hierarchy to ban people below him.

#### ⭐ Punishment feature

If you want the application to execute punishments like mutes and bans and manage them automatically (like unban after defined time on tempban), grant your bot the following permissions based on your needs:

```md
Manage roles - for muted role
Kick people
Ban people
```

#### ⭐ Automoderation feature

To avoid any issue for message deletion or read permissions it is recommended to grant your bot a very high and strong or even the `administrator` role.

#### ⭐ Invite tracking

Allows Dexter to track the invites new members are using. Grant your bot the `manage guild` permission to use this feature.

### 🤝 Contribute

Contributions are welcome.\
You can find our contributions guidelines [here](CONTRIBUTING.md).\
If you are new to open source, checkout [this tutorial](https://github.com/firstcontributions/first-contributions).\
Feel free to get in touch with me via our support server [https://discord.gg/DBS664yjWN](https://discord.gg/DBS664yjWN).

### 💁🏻 Further Help

Feel free to join our discord at [https://discord.gg/DBS664yjWN](https://discord.gg/DBS664yjWN) if you have further questions about your dev environment.
