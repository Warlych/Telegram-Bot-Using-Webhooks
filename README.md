## About the project
<p>
    This project is a telegram bot to simplify the work processes of administrators.
</p>
<p>
    This bot is designed with a focus on optimizing task and notification management, providing a convenient and efficient interface for interacting with projects.
</p>

## Technology stack
<p>
    The project is written in ASP.NET using the following libraries:
    <ul>
        <li>EF Core</li>
        <li>Telegram.Bot</li>
        <li>Serilog and Serilog.Sinks.Seq</li>
    </ul>
</p>

## Story about experience
<p>
     As the author of the project, I was interested in using webhook technology and making a telegram bot. This is a good experience in working with the ASP.NET platform and the ability to embed tools into it.
</p>

## Function review
<p>
    Features are divided into chat types:
    <ul>
        <li>Private chat</li>
        <li>Group chat</li>
        <li>Channel chat</li>
    </ul>
</p>

### Private chat functions
<p>
    This category contains functions that allow you to contact the public administration:
    <ul>
        <li>/ask - allows you to ask a question</li>
        <li>/advt - allows you to send a proposal for cooperation</li>
        <li>/news - allows you to send news of interest</li>
    </ul>
</p>

### Group chat functions
<p>
    This category contains functions that allow the administration to respond to a user’s message and interact with the channel:
    <ul>
        <li>/set_group and /unset_group - allow you to set the main group where interaction with the administration will take place</li>
        <li>/send - allows you to send a proposal for cooperation</li>
        <li>/close_topic - allows you to reply to a user's message</li>
        <li>/topic_statistics and /topic_statistics_date "dd-MM-yyyy" - allow you to find out statistics on activity in topics</li>
        <li>/channel_members - allows you to find out the number of subscribers at a given time</li>
        <li>/channel_subscribes - allows you to find out statistics on channel subscriptions</li>
        <li>/channel_posts - allows you to find out statistics on channel posts</li>
        <li>/ban and /unban - allows you to block violators</li>
    </ul>
</p>

### Channel chat functions
<p>
    This category contains functions that allow the administration to respond to a user’s message and interact with the channel:
    <ul>
        <li>/set_channel and /unset_channel - allows you to set a monitored channel</li>
    </ul>
</p>

## About the launch

First you need to set the Telegram Bot Token to the user's secrets.

```shell
dotnet user-secrets set "TelegramBotToken" "YourToken" 
```

If you, like me, do not have a domain, then you need to download <a href="https://ngrok.com/">ngrok</a>, it will allow you to tunnel requests for the bot to work correctly.

To start working with it, you need to enter `ngrok http 80`, it is to this port in the local machine that redirection will occur:
    
```shell
ngrok http 80 
```

You will receive the following window:
    
```shell
ngrok (Ctrl + C to quit)
Introducing Pay-as-you-go pricing: https://ngrok.com/r/payg

Session Status                online
Account                       xxx@xxx.xxx (Plan: Free)
Update                        update available (version 3.4.0, Ctrl-U to update)
Version                       3.3.5
Region                        Europe (eu)
Latency                       41ms
Web Interface                 http://
Forwarding                    https://url_for_bot -> http://localhost:80
Connections                   ttl     opn     rt1     rt5     p50     p90
                              0      0       0.00    0.00    0   0         
```

We need https://url_for_bot, this is the address we need to specify in docker-compose.yml as the address for webhooks.

<strong>That's all, good job</strong>

## Future
<p>
    <p>
        This telegram bot does not use all kinds of tools for working with telegram servers.
    </p>
    At the moment, only the Telegram Bot API is used, but there is a Telegram Client API that allows you to get broader statistics.
    One unofficial implementation is WTelegramClient -> <a href="https://github.com/wiz0u/WTelegramClient">WTelegramClient</a>
</p>

<p> 
    <img width="181" src="https://img.shields.io/badge/BUILT%20WITH%20LOVE-ef4041?style=for-the-badge&logo=&logoColor=white">
    <img src="https://forthebadge.com/images/badges/made-with-c-sharp.svg">
</p>