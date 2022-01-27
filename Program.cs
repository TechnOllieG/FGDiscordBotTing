using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace FGDiscordBotTing
{
	public enum EventType
	{
		Meeting,
		Break
	}
    
	struct Event
	{
		public EventType type;
		public string startTime;
		public string endTime;
	}
    
	class TimeAnnouncer
	{
		public ISocketMessageChannel channel;

		public TimeAnnouncer(ISocketMessageChannel channel)
		{
			this.channel = channel;
		}
        
		public void AnnounceMain()
		{
			int currentEventIndex = 0;

			while (currentEventIndex < Program.events.Count)
			{
				DateTime currentTime = DateTime.Now;
				Event currentEvent = Program.events[currentEventIndex];
				DateTime eventStartTime = DateTime.Parse(currentEvent.startTime);
				DateTime eventEndTime = DateTime.Parse(currentEvent.endTime);

				if (DateTime.Compare(currentTime, eventStartTime) > 0 && (currentTime - eventStartTime).Seconds > 15)
				{
					Console.WriteLine("Event start time is later than current time, skipping this event");
					++currentEventIndex;
					continue;
				}
                
				TimeSpan spanToStart = eventStartTime - currentTime;
				if(spanToStart.Milliseconds > 0)
					Thread.Sleep(spanToStart);
				
				switch (currentEvent.type)
				{
					case EventType.Meeting:
					{
						channel.SendMessageAsync($"Meeting start! (ends in {(int) Math.Round((eventEndTime - DateTime.Now).TotalMinutes)} minutes)");
						Thread.Sleep((int) Math.Round((eventEndTime - DateTime.Now).TotalMilliseconds * 0.5));
						channel.SendMessageAsync($"Halfway point! ({(int) Math.Round((eventEndTime - DateTime.Now).TotalMinutes)} minutes left)");
						Thread.Sleep((int) Math.Round(((eventEndTime - DateTime.Now).TotalMinutes - 2) * 60 * 1000));
						channel.SendMessageAsync($"2 minutes left");
						Thread.Sleep(1 * 60 * 1000);
						channel.SendMessageAsync($"1 minute left");
						++currentEventIndex;
						break;
					}
					case EventType.Break:
					{
						channel.SendMessageAsync($"Break for {(int) Math.Round((eventEndTime - DateTime.Now).TotalMinutes)} minutes!");
						++currentEventIndex;
						break;
					}
					default: break;
				}
			}

			channel.SendMessageAsync("Day end!");
		}
	}
    
	class Program
	{
		public static List<Event> events = new List<Event>();
		private static Dictionary<ISocketMessageChannel, Thread> _currentThreads = new Dictionary<ISocketMessageChannel, Thread>();
        
		private static void ParseScheduleFile()
		{
			string[] file = File.ReadAllLines("schedule.txt");

			foreach (string line in file)
			{
				if (line.StartsWith("//"))
					continue;
                
				string[] substrings = line.Split(' ');
				if (substrings.Length != 3)
					throw new ArgumentOutOfRangeException("Substring was above 3, which means the file has the wrong format on one or more lines");

				Event current = new Event();
				current.startTime = substrings[0];
				current.endTime = substrings[1];
				current.type = (EventType) Enum.Parse(typeof(EventType), substrings[2]);
				events.Add(current);
			}
		}
        
		public static void Main(string[] args)
		{
			ParseScheduleFile();
            
			new Program().MainAsync().GetAwaiter().GetResult();
		}

		private DiscordSocketClient _client;

		public async Task MainAsync()
		{
			_client = new DiscordSocketClient();
			_client.MessageReceived += CommandHandler;
			_client.Log += Log;
            
			var token = await File.ReadAllTextAsync("token.txt");

			await _client.LoginAsync(TokenType.Bot, token);
			await _client.StartAsync();
            
			await Task.Delay(-1);
		}

		private Task Log(LogMessage msg)
		{
			Console.WriteLine(msg.ToString());
			return Task.CompletedTask;
		}

		private Task CommandHandler(SocketMessage message)
		{
			if (!message.Content.StartsWith('!') || message.Author.IsBot)
				return Task.CompletedTask;

			int lengthOfCommand = message.Content.Length;
			string command = message.Content.Substring(1, lengthOfCommand - 1).ToLower();
            
			switch (command)
			{
				case "test":
				{
					message.Channel.SendMessageAsync($@"Receiving you loud and clear! {message.Author.Mention}");
					break;
				}
				case "setup":
				{
					if (_currentThreads.ContainsKey(message.Channel))
					{
						message.Channel.SendMessageAsync("Time keeper already started in this channel");
						break;
					}

					message.Channel.SendMessageAsync("Starting time keeper in this channel!");
					TimeAnnouncer announcer = new TimeAnnouncer(message.Channel);
					Thread current = new Thread(announcer.AnnounceMain);
					_currentThreads.Add(message.Channel, current);
					current.Start();
					break;
				}
				default: break;
			}

			return Task.CompletedTask;
		}
	}
}