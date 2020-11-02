--CREATE NONCLUSTERED INDEX IX_Rating_MovieId ON dbo.Rating
--	(
--	MovieId
--	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
--GO

CREATE NONCLUSTERED INDEX IX_Rating_UserId ON dbo.Rating
	(
	UserId
	) WITH( STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

SELECT COUNT(*) FROM Rating
--25000095

SELECT * FROM Movie WHERE Genres LIKE '%Action%' AND Genres NOT LIKE '%Comedy%' AND Genres NOT LIKE '%Animation%'

SELECT COUNT(*) FROM Rating WHERE MovieID IN 
(
SELECT Id FROM Movie WHERE Genres LIKE '%Action%' AND Genres NOT LIKE '%Comedy%' AND Genres NOT LIKE '%Animation%'
)
--5915669


SELECT MovieId, COUNT(*) FROM Rating WHERE MovieID IN 
(
SELECT Id FROM Movie WHERE Genres LIKE '%Action%' AND Genres NOT LIKE '%Comedy%' AND Genres NOT LIKE '%Animation%'
)
GROUP BY MovieId --5340
--HAVING COUNT(*) > 10 --2462
HAVING COUNT(*) > 1000 --1341

SELECT COUNT(*) FROM Rating WHERE MovieID IN 
(
	SELECT TOP 100 MovieId FROM Rating WHERE MovieID IN 
	(
		SELECT Id FROM Movie WHERE Genres LIKE '%Action%' AND Genres NOT LIKE '%Comedy%' AND Genres NOT LIKE '%Animation%'
	)
	GROUP BY MovieId --5340
	HAVING COUNT(*) > 1000 --2462
)
--5915669

--5906009  > 10
--5869195 > 100


SELECT m.Title, Temp1.RatingsCount FROM Movie m INNER JOIN
(
	SELECT MovieId, COUNT(*) as RatingsCount FROM Rating WHERE MovieID IN 
	(
		SELECT Id FROM Movie WHERE Genres LIKE '%Action%' AND Genres NOT LIKE '%Comedy%' AND Genres NOT LIKE '%Animation%'
	)
	GROUP BY MovieId --5340
	HAVING COUNT(*) > 1000 --2462
) Temp1
ON m.Id = Temp1.MovieId
ORDER BY Temp1.RatingsCount DESC

SELECT * FROM Movie
WHERE title like '%(%'  

UPDATE Movie SET Movie.MovieYear = TRY_CONVERT(int, Year)
FROM Movie INNER JOIN
(
SELECT id as MovieId,
	title,
    SUBSTRING(title,
              CHARINDEX('(', title) + 1,
              CHARINDEX(')', title) - CHARINDEX('(', title) - 1) AS Year
FROM Movie WHERE title like '%(%' AND Id NOT IN (203453)
) Temp1
ON Movie.Id = Temp1.MovieId


--60119

Select SUBSTRING(title, 
                 CHARINDEX('[', title, CHARINDEX('[', title) + 1 ) +1, 
                 CHARINDEX(']', title, CHARINDEX('[', title, CHARINDEX('[', title) + 1 ) ) -
                    CHARINDEX('[', title, CHARINDEX('[', title) + 1 ) - 1 )
FROM Movie 




--================

SELECT MovieId, COUNT(*) FROM Rating WHERE MovieID IN 
(
SELECT Id FROM Movie WHERE Genres LIKE '%Action%' AND Genres NOT LIKE '%Comedy%' AND Genres NOT LIKE '%Animation%'
)
GROUP BY MovieId --5340
--HAVING COUNT(*) > 10 --2462
HAVING COUNT(*) > 1000 --1341

SELECT COUNT(*) FROM Movie WHERE MovieYear > 2015
--7866

SELECT COUNT(*) FROM Rating WHERE MovieID IN 
(
	SELECT MovieId FROM Rating
	GROUP BY MovieId --5340
	HAVING COUNT(*) > 100 --2462
)
AND MovieId IN (SELECT Id FROM Movie WHERE MovieYear > 2015)
--451037

DROP TABLE #TopMovie
SELECT m.Id as MovieId, m.Title, m.Genres, Temp1.RatingsCount, Temp1.RatingsAvg
INTO #TopMovie
FROM Movie m INNER JOIN
(
	SELECT TOP 500 MovieId, COUNT(*) as RatingsCount, AVG(Rating) as RatingsAvg FROM Rating WHERE MovieID IN 
	(
		SELECT Id FROM Movie WHERE MovieYear > 2015
		AND Genres NOT LIKE '%Documentary%' AND Genres NOT LIKE '%Animation%'
	)
	GROUP BY MovieId --5340
	--HAVING COUNT(*) > 100 --2462
	ORDER BY COUNT(*) DESC
) Temp1
ON m.Id = Temp1.MovieId
--ORDER BY Temp1.RatingsCount DESC
ORDER BY Temp1.RatingsAvg DESC

--500 movies with 100+ ratings
-- 600 movies

SELECT * FROM #TopMovie ORDER BY RatingsCount DESC
SELECT * FROM #TopMovie ORDER BY RatingsAvg DESC

DROP TABLE #TopUser

SELECT UserId, COUNT(*) as RatingsCount, AVG(Rating) as RatingsAvg
INTO #TopUser
FROM Rating 
WHERE MovieId IN (SELECT MovieId FROM #TopMovie)
Group BY UserId 
HAVING COUNT(*) > 20
ORDER BY COUNT(*) DESC
--5327

SELECT * FROM #TopUser ORDER BY RatingsCount DESC 


DROP Table #TopRating 

SELECT *
--TOP 100000 * 
INTO #TopRating
FROM Rating WHERE 
MovieId IN (SELECT MovieId FROM #TopMovie)
AND UserId IN (SELECT UserId FROM #TopUser)
ORDER BY Rating.Timestamp DESC


SELECT UserId, COUNT(*) as NumRatings 
FROM #TopRating 
WHERE MovieId IN (SELECT MovieId FROM #TopMovie)
Group BY UserId 
--HAVING COUNT(*) > 10
ORDER BY COUNT(*) DESC


SELECT Max(UserID) From Rating
-- NumRatings: 269539
-- NumMovies: 500
-- NumUsers: 5327


SELECT *  FROM #TopMovie 
ORDER BY RatingsCount DESC

SELECT * FROM Rating WHERE 
MovieId IN (SELECT MovieId FROM #TopMovie)
AND UserId IN (SELECT UserId FROM #TopUser);


--Update stats

UPDATE #TopMovie SET #TopMovie.RatingsCount = Temp1.RatingsCount, #TopMovie.RatingsAvg=Temp1.RatingsAvg
--SELECT *
FROM #TopMovie m INNER JOIN
(
	SELECT MovieId, COUNT(*) as RatingsCount, AVG(Rating) as RatingsAvg FROM #TopRating 
	GROUP BY MovieId --5340
	--ORDER BY COUNT(*) DESC
) Temp1
ON m.MovieId = Temp1.MovieId
--ORDER BY m.MovieId

SELECT * FROM #TopMovie ORDER BY RatingsAvg DESC

UPDATE #TopUser SET #TopUser.RatingsCount =  Temp1.RatingsCount,  #TopUser.RatingsAvg =  Temp1.RatingsAvg
FROM #TopUser u INNER JOIN
(
	SELECT UserId, COUNT(*) as RatingsCount, AVG(Rating) as RatingsAvg FROM #TopRating 
	GROUP BY UserId 
) Temp1
ON u.UserId = Temp1.UserId

SELECT * FROM #TopUser ORDER BY RatingsCount DESC

--Get data into CSV
SELECT * FROM #TopMovie ORDER BY RatingsCount DESC
SELECT * FROM #TopRating ORDER BY Timestamp

SELECT TOP 50 * FROM #TopMovie ORDER BY RatingsCount DESC
