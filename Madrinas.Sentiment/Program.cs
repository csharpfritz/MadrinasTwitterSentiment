using LinqToTwitter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Madrinas_SentimentML.Model;
using System.IO;
using CsvHelper;
using System.Globalization;

namespace Madrinas.Sentiment
{
	class Program
	{

		static List<Status> Statuses = new List<Status>();
		static int MaxStatusCount = 500; // 500


		static async Task Main(string[] args)
		{

			var auth = await SetAuthentication();
			var context = new TwitterContext(auth);

			Search(context, "#coffee4fuel");

			var model = new ConsumeModel();
			var outSentiment = new List<Sentiment>();

			foreach (var item in Statuses)
			{

				var input = new ModelInput() { TextToAnalyze = item.FullText };
				var response = model.Predict(input);
				outSentiment.Add(new Sentiment
				{
					CreatedAt = item.CreatedAt,
					Text = item.FullText,
					Location = item.User.Location,
					SentimentPct = response.Probability,
					ImageUrl = item.Entities.MediaEntities.Any() ? item.Entities.MediaEntities[0].MediaUrlHttps : ""
				});
				Console.WriteLine($"Sentiment %: {response.Probability:0.0%} + {item.FullText}");

			}

			using (var writer = new StreamWriter(".\\coffeeForFuel.csv"))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(outSentiment);
			}

			Console.ReadLine();

		}

		private static void Search(TwitterContext context, string searchTerm, ulong sinceId = 1, ulong maxId = ulong.MaxValue)
		{

			var response = context.Search.Where(s => s.Type == SearchType.Search &&
				s.Query == searchTerm &&
				s.TweetMode == TweetMode.Extended &&
				s.IncludeEntities == true &&
				s.SinceID == sinceId &&
				s.MaxID == maxId
			).ToList();

			if (response[0].Statuses.Any())
			{
				maxId = response[0].Statuses.Min(s => s.StatusID) - 1;
				Statuses.AddRange(response[0].Statuses.Where(s => !s.FullText.StartsWith("RT")));

				if (response[0].Statuses.Any() && Statuses.Count() < MaxStatusCount)
					Search(context, searchTerm, sinceId, maxId);
			}

		}


		private static async Task<ApplicationOnlyAuthorizer> SetAuthentication()
		{
			var auth = new ApplicationOnlyAuthorizer()
			{
				CredentialStore = new InMemoryCredentialStore
				{

					ConsumerKey = "",
					ConsumerSecret = ""
				}
			};
			await auth.AuthorizeAsync();
			return auth;
		}


	}
}
