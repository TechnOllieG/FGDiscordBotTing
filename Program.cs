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

	class TimerUpdater
	{
		public string msgPrefix;
		public string msgSuffix;
		public Task<RestUserMessage> msgTask;
		public DateTime endTime;
		public Thread thread;

		public TimerUpdater(string msgPrefix, string msgSuffix, Task<RestUserMessage> msgTask, DateTime endTime)
		{
			this.msgPrefix = msgPrefix;
			this.msgSuffix = msgSuffix;
			this.msgTask = msgTask;
			this.endTime = endTime;
			
			thread = new Thread(TimerMain);
			thread.Start();
		}

		public void TimerMain()
		{
			msgTask.Wait();
			RestUserMessage msg = msgTask.Result;
			TimeSpan current = endTime - DateTime.Now;

			while (current.Seconds > 0)
			{
				int ms = (int) Math.Round(((current.TotalSeconds - 0.05f) % 15f) * 1000f);
				Console.WriteLine($"Timer Updater: Start sleep for {ms} milliseconds");
				Thread.Sleep(ms);
				current = endTime - DateTime.Now;

				Console.WriteLine($"Timer Updater: Modify discord message to \"{msgPrefix}`{current.ToString(@"mm\:ss")}`{msgSuffix}\"");
				msg.ModifyAsync(a =>
				{
					a.Content = $"{msgPrefix}`{current.ToString(@"mm\:ss")}`{msgSuffix}";
				});
			}
		}
	}
    
	class TimeAnnouncer
	{
		private const bool deleteMessages = true;
		private const bool countdown = false;

		public ISocketMessageChannel channel;

		public TimeAnnouncer(ISocketMessageChannel channel)
		{
			this.channel = channel;
		}
        
		public void AnnounceMain()
		{
			int currentEventIndex = 0;

			List<Task<RestUserMessage>> currentMessages = new List<Task<RestUserMessage>>();
			TimerUpdater updater = null;

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
				int ms = (int) Math.Round(spanToStart.TotalMilliseconds);
				if(ms > 1)
					Thread.Sleep(ms - 50);

				if (deleteMessages)
				{
					foreach (Task<RestUserMessage> msg in currentMessages)
					{
						msg.Wait();
						msg.Result.DeleteAsync();
					}
				}
				currentMessages.Clear();

				switch (currentEvent.type)
				{
					case EventType.Meeting:
					{
						Task<RestUserMessage> msgTask = channel.SendMessageAsync($"Meeting start! (ends in `{TimeToEventEnd().ToString(@"mm\:ss")}`)");
						
						if(countdown)
							updater = new TimerUpdater("Meeting start! (ends in ", ")", msgTask, eventEndTime);
						
						currentMessages.Add(msgTask);

						Thread.Sleep((int) Math.Round(TimeToEventEnd().TotalMilliseconds * 0.5));
						currentMessages.Add(channel.SendMessageAsync($"Halfway point! ({(int) Math.Round(TimeToEventEnd().TotalMinutes)} minutes left)"));
						Thread.Sleep((int) Math.Round((TimeToEventEnd().TotalMinutes - 2.0) * 60.0 * 1000.0));
						currentMessages.Add(channel.SendMessageAsync($"2 minutes left"));
						Thread.Sleep(1 * 60 * 1000);
						currentMessages.Add(channel.SendMessageAsync($"1 minute left"));
						break;
					}
					case EventType.Break:
					{
						string prefix = $"Break for {(int) Math.Round(TimeToEventEnd().TotalMinutes)} minutes! (ends in ";
						Task<RestUserMessage> msg = channel.SendMessageAsync($"{prefix}`{TimeToEventEnd().ToString(@"mm\:ss")}`)");
						
						if(countdown)
							updater = new TimerUpdater(prefix, ")", msg, eventEndTime);
						
						currentMessages.Add(msg);
						break;
					}
					default: break;
				}
				
				++currentEventIndex;

				TimeSpan TimeToEventEnd() => (eventEndTime - DateTime.Now);
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
					throw new ArgumentOutOfRangeException("substrings", "Substring was above 3, which means the file has the wrong format on one or more lines");

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
				case "help":
				{
					message.Channel.SendMessageAsync("Possible commands:\n" +
					                                 "!test - used to test the bot that it is correctly receiving commands.\n" +
					                                 "!setup - Starts the time keeping in the channel where this command is written.");
					break;
				}
				default: break;
			}

			return Task.CompletedTask;
		}
	}
}