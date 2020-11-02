// Read about this and other fiddles on .NET Fiddle Newsletter https://dotnetfiddle.substack.com/
//
// This fiddle demonstrates how to use ML technique matrix factorization, to predict what other movies you will like based on your ratings.
//
// Before running the program please specify your movie preferences by entering ratings for up to 50 movies in CurrentUserMovieRating dicitonary on top
// Use scale 0.5 - 5. For movies you didn't see enter NULL.
//
// This program consists of 5 steps
//
// 1. Load Movies - loads 500 movies from CSV file stored on azure.  The movies were picked from 25M MovielLens DataSet from https://grouplens.org/datasets/movielens.
// 2. Load Training and Test data - loads 260K+ ratings from CSV File.   The ratings also come from 25M MovielLens DataSet. All ratings are split into test and training data using TraingTestDataSplitFraction. 
//    By default 80% of data is used for training and 20% of data is uses for testing.
// 3. Train model. Run training 30 times trying to improve the model to give better predications.
// 4. Evaluate model with test data, to see how well is prediction model working.
// 5. Make predictions on  based on ratings you provided in CurrentUserMovieRatings and show you the top recommended movies and the bottom movies that you probably wouldn't like.

using System;
using System.Net;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using System.IO;
using Microsoft.ML;
using Microsoft.ML.Trainers;

using CsvHelper;


namespace DnfNewsletter.No4.WarGames.Console_
{
	public class Program
	{
		const int CurrentUserId = 1000001;
		const int NumRecommendations = 20;  //There are total of 500 movies so you can change this up to 250
		const double TraingTestDataSplitFraction = 0.2; //20% of data is used for testing and 80% for training
		const int TraingTestDataSplitRandomSeed = 42;

		private static List<Movie> _movies;
		private static Dictionary<string, Movie> _moviesTitleToMovie;

		//Your movie ratings
		static Dictionary<string, double?> CurrentUserMovieRatings
			= new Dictionary<string, double?>()
		{
		//Add your rating for any movie you saw.  Scale 0.5 - 5.  Null means you haven't seen the movie.
		{"Deadpool (2016)",                                5},
		{"Arrival (2016)",                                 null},
		{"Doctor Strange (2016)",                          null},
		{"Guardians of the Galaxy 2 (2017)",               3.5},
		{"Logan (2017)",                                   3},
		{"Rogue One: A Star Wars Story (2016)",            null},
		{"Captain America: Civil War (2016)",              null},
		{"Thor: Ragnarok (2017)",                          null},
		{"Avengers: Infinity War - Part I (2018)",         null},
		{"Get Out (2017)",                                 5},
		{"Wonder Woman (2017)",                            null},
		{"Blade Runner 2049 (2017)",                       null},
		{"Baby Driver (2017)",                             2},
		{"Spider-Man: Homecoming (2017)",                  null},
		{"Black Panther (2017)",                           3.5},
		{"Dunkirk (2017)",                                 null},
		{"Fantastic Beasts and Where to Find Them (2016)", null},
		{"Deadpool 2 (2018)",                              null},
		{"Star Wars: The Last Jedi (2017)",                null},
		{"Three Billboards Outside Ebbing, Missouri (2017)", null},
		{"La La Land (2016)",                              4},
		{"Suicide Squad (2016)",                           1},
		{"The Nice Guys (2016)",                           4.5},
		{"10 Cloverfield Lane (2016)",                     4},
		{"John Wick: Chapter Two (2017)",                  3},
		{"Passengers (2016)",                              null},
		{"Split (2017)",                                   null},
		{"X-Men: Apocalypse (2016)",                       null},
		{"Star Trek Beyond (2016)",                        null},
		{"Batman v Superman: Dawn of Justice (2016)",      2},
		{"A Quiet Place (2018)",                           null},
		{"The Shape of Water (2017)",                      2},
		{"Avengers: Infinity War - Part II (2019)",        null},
		{"Hacksaw Ridge (2016)",                           null},
		{"It (2017)",                                      3},
		{"Annihilation (2018)",                            null},
		{"The Accountant (2016)",                          null},
		{"Hidden Figures (2016)",                          null},
		{"Now You See Me 2 (2016)",                        null},
		{"Ant-Man and the Wasp (2018)",                    3},
		{"Hell or High Water (2016)",                      null},
		{"Lady Bird (2017)",                               null},
		{"Kingsman: The Golden Circle (2017)",             3.5},
		{"Jumanji: Welcome to the Jungle (2017)",          null},
		{"The Jungle Book (2016)",                         null},
		{"Bohemian Rhapsody (2018)",                       null},
		{"Ghost in the Shell (2017)",                      null},
		{"Manchester by the Sea (2016)",                   null},
		{"Solo: A Star Wars Story (2018)",                 null},
		{"Mission: Impossible - Fallout (2018)",           3.5},
		};

		public static void Main()
		{
			MLContext mlContext = new MLContext();

			if (!ValidateCurrentUserMovieRatings())
				return;

			Console.WriteLine("\nLoad movies");
			Console.WriteLine("=========================================\n");
			LoadMovies();


			Console.WriteLine("\nLoad training and test data");
			Console.WriteLine("=========================================\n");
			(IDataView trainDataView, IDataView testDataView) = LoadData1(mlContext);


			Console.WriteLine("\nTrain the model using training data set");
			Console.WriteLine("=========================================\n");
			ITransformer model = BuildAndTrainModel(mlContext, trainDataView);


			Console.WriteLine("\nEvaluate the model using test data set");
			Console.WriteLine("=========================================\n");
			EvaluateModel(mlContext, testDataView, model);


			Console.WriteLine("\nMake predictions");
			Console.WriteLine("=========================================\n");
			MakePredictions(mlContext, model);
		}


		public static (IDataView trainDataView, IDataView testDataView) LoadData1(MLContext mlContext)
		{
			var ratingsCsvFileUrl = @"https://dnfnewsletter.blob.core.windows.net/files/WarGames/Ratings.csv";
			var ratingsDataList = LoadListFromUrlCsv<MovieRating>(ratingsCsvFileUrl);
			Console.WriteLine($"RatingsList size : {ratingsDataList.Count}\n");

			var currentUserMovieRatings = GetCurrentUserMovieRatings();
			ratingsDataList.AddRange(currentUserMovieRatings);

			IDataView ratingsDataView = mlContext.Data.LoadFromEnumerable<MovieRating>(ratingsDataList);

			//split data
			DataOperationsCatalog.TrainTestData dataSplit = mlContext.Data.TrainTestSplit(ratingsDataView,
																						  testFraction: TraingTestDataSplitFraction,
																						  seed: TraingTestDataSplitRandomSeed);
			IDataView trainDataView = dataSplit.TrainSet;
			IDataView testDataView = dataSplit.TestSet;

			return (trainDataView, testDataView);
		}


		public static void LoadMovies()
		{
			var moviesCsvFileUrl = @"https://dnfnewsletter.blob.core.windows.net/files/WarGames/Movies.csv";

			var moviesList = LoadListFromUrlCsv<Movie>(moviesCsvFileUrl);
			Console.WriteLine($"MoviesList size : {moviesList.Count}\n");

			_movies = moviesList;
			_moviesTitleToMovie = moviesList.ToDictionary(ml => ml.Title, ml => ml);
		}



		public static List<MovieRating> GetCurrentUserMovieRatings()
		{
			var ratings = new List<MovieRating>();
			foreach (var pair in CurrentUserMovieRatings)
			{
				if (pair.Value == null)
					continue;

				Movie movie;
				if (!_moviesTitleToMovie.TryGetValue(pair.Key, out movie))
				{
					Console.WriteLine($"Title {pair.Key} does not match movie. Skipping.");
					continue;
				}

				ratings.Add(new MovieRating
				{
					UserId = CurrentUserId,
					MovieId = movie.Id,
					Rating = Convert.ToSingle(pair.Value.Value)
				});
			}

			return ratings;
		}


		public static ITransformer BuildAndTrainModel(MLContext mlContext, IDataView trainingDataView)
		{

			// Since userId and movieId represent users and movie titles, not real values,
			// you use the MapValueToKey() method to transform each userId and each movieId into a numeric key type Feature column (a format accepted by recommendation algorithms) and add them as new dataset columns:
			IEstimator<ITransformer> estimator = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "UserIdEncoded", inputColumnName: "UserId")
				.Append(mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "MovieIdEncoded", inputColumnName: "MovieId"));
			var options = new MatrixFactorizationTrainer.Options
			{
				MatrixColumnIndexColumnName = "UserIdEncoded",
				MatrixRowIndexColumnName = "MovieIdEncoded",
				LabelColumnName = "Rating",
				NumberOfIterations = 30,   //original value was 20
				ApproximationRank = 100
			};

			// Matrix Factorization is a common approach to recommendation when you have data on how users have rated products in the past
			var trainerEstimator = estimator.Append(mlContext.Recommendation().Trainers.MatrixFactorization(options));
			ITransformer model = trainerEstimator.Fit(trainingDataView);

			Console.WriteLine("\nIn this output, there are 30 iterations. In each iteration, the measure of error decreases and converges closer and closer to 0.");
			return model;
		}


		public static void EvaluateModel(MLContext mlContext, IDataView testDataView, ITransformer model)
		{
			var prediction = model.Transform(testDataView);

			var metrics = mlContext.Regression.Evaluate(prediction, labelColumnName: "Rating", scoreColumnName: "Score");

			Console.WriteLine("Root Mean Squared Error: " + metrics.RootMeanSquaredError.ToString());
			Console.WriteLine("- used to measure the differences between the model predicted values and the test dataset observed values. The lower it is, the better the model is.");
			Console.WriteLine("\nRSquared: " + metrics.RSquared.ToString());
			Console.WriteLine("- indicates how well data fits a model. A value of 0 means data is random, value of 1 means that model matches data exactly.");
		}


		public static void MakePredictions(MLContext mlContext, ITransformer model)
		{
			var moviePredictions = new List<MoviePrediction>();
			foreach (Movie movie in _movies)
			{
				if (CurrentUserMovieRatings.ContainsKey(movie.Title))
					continue;

				var predictedRating = MakePrediction(mlContext, model, CurrentUserId, movie.Id);
				moviePredictions.Add(new MoviePrediction { Movie = movie, Rating = predictedRating });
			}

			var topMovieRecommendations = moviePredictions.OrderByDescending(mp => mp.Rating).Take(NumRecommendations);
			Console.WriteLine($"Top Recommendations:\n");
			WriteRecommendations(topMovieRecommendations);

			var bottomMovieRecommendations = moviePredictions.OrderBy(mp => mp.Rating).Take(NumRecommendations);
			Console.WriteLine($"\nBottom Un-Recommendations:\n");
			WriteRecommendations(bottomMovieRecommendations);
		}

		private static void WriteRecommendations(IEnumerable<MoviePrediction> movieRecommendations)
		{
			Console.WriteLine($"{"Title",-40}  {"Predicted rating",-20} {"Total Ratings",-20} {"Avg rating",-20}");


			foreach (var movieRecommendation in movieRecommendations)
				Console.WriteLine($"{movieRecommendation.Movie.Title,-40}  {movieRecommendation.Rating,-20:0.00} {movieRecommendation.Movie.RatingsCount,-20} {movieRecommendation.Movie.RatingsAvg,-20:0.00}");
		}

		public static float MakePrediction(MLContext mlContext, ITransformer model, int userId, int movieId)
		{
			var predictionEngine = mlContext.Model.CreatePredictionEngine<MovieRating, MovieRatingPrediction>(model);

			var testInput = new MovieRating { UserId = userId, MovieId = movieId };
			var movieRatingPrediction = predictionEngine.Predict(testInput);

			return movieRatingPrediction.Score;
		}


		public static List<T> LoadListFromUrlCsv<T>(string cvsFileUrl)
		{
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create(cvsFileUrl);
			req.KeepAlive = false;
			req.ProtocolVersion = HttpVersion.Version10;
			HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

			using (var streamReader = new StreamReader(resp.GetResponseStream()))
			{
				using (var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture))
				{
					csvReader.Configuration.HasHeaderRecord = false;
					var records = csvReader.GetRecords<T>();
					return new List<T>(records);
				}
			}
		}


		public static bool ValidateCurrentUserMovieRatings()
		{
			if (CurrentUserMovieRatings.Values.Count(v => v != null) < 10)
			{
				Console.WriteLine("Please rate at least 10 movies");
				return false;
			}

			if (CurrentUserMovieRatings.Values.Any(v => v != null && (v.Value < 0.5 || v.Value > 5)))
			{
				Console.WriteLine("Please make sure your ratings are between 1 and 5");
				return false;
			}

			if (CurrentUserMovieRatings.Values.Any(v => v != null && (v.Value % .5 != 0)))
			{
				Console.WriteLine("Please make sure your rating is divisible by .5");
				return false;
			}

			return true;
		}

	}

	public class Movie
	{
		public int Id { get; set; }

		public string Title { get; set; }

		public string Genres { get; set; }

		public int RatingsCount { get; set; }

		public float RatingsAvg { get; set; }
	}


	public class MovieRating
	{
		public int UserId { get; set; }

		public int MovieId { get; set; }

		public float Rating { get; set; }
	}


	public class MovieRatingPrediction
	{
		public float Label;

		public float Score;
	}


	public class MoviePrediction
	{
		public Movie Movie;

		public float Rating;
	}
}
