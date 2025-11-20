using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using ExtensionsMethods;

namespace Kontur.GameStats.Server
{
    /// <summary>
    /// Обрабатывает входящие HTTP-запросы статистики, выполняет парсинг URL и маршрутизацию запросов.
    /// </summary>
    public class QueryProcessor : IStatServerRequestHandler
    {
        private static readonly Regex PutRequestRegex =
            new Regex("/(servers)/" +
                      "([\\.a-zA-Z0-9]+-[0-9]{1,4})/" +
                      "(info|matches)/?" +
                      "([0-9]{4}-[0-9]{2}-[0-9]{2}T" +
                      "[0-9]{2}:[0-9]{2}:[0-9]{2}Z)?", RegexOptions.Compiled);

        private static readonly Regex GetRequestRegex =
            new Regex("/(servers|players|reports)/", RegexOptions.Compiled);

        private static readonly Regex ServerInfoRegex =
            new Regex("([\\.a-zA-Z0-9]+-?[0-9]{1,4}|info)/?" +
                      "(info|matches|stats)?/?" +
                      "([0-9]{4}-[0-9]{2}-[0-9]{2}T" +
                      "[0-9]{2}:[0-9]{2}:[0-9]{2}Z)?", RegexOptions.Compiled);

        private static readonly Regex ReportRegex =
            new Regex("^(recent-matches|best-players|popular-servers)/?", RegexOptions.Compiled);

        private readonly ServerDataBase dataBase;
        private readonly JsonSerializer jsonSerializer;
        private readonly GameStatistic statistic;

        public ConcurrentDictionary<string, AdvertiseQueryServer> AdvertiseServers { get; }

        public BlockingCollection<GameServer> GameServers { get; }

        /// <summary>
        /// Инициализирует новый экземпляр класса QueryProcessor. Создает экземпляры ServerDataBase, GameStatistic и JsonSerializer, затем загружает данные из базы данных. Использует ReadAdvertServers для загрузки рекламируемых серверов и ReadGameServers для загрузки игровых серверов.
        /// </summary>
        public QueryProcessor()
        {
            dataBase = new ServerDataBase();
            statistic = new GameStatistic();
            jsonSerializer = new JsonSerializer();
            AdvertiseServers = dataBase.ReadAdvertServers();
            GameServers = dataBase.ReadGameServers();
        }

        /// <summary>
        /// Обрабатывает GET-запрос. Использует ProcessGetRequest для парсинга URL и маршрутизации запроса.
        /// </summary>
        /// <param name="uri">URI запроса.</param>
        /// <returns>Результат обработки GET-запроса от ProcessGetRequest.</returns>
        public RequestHandlingResult HandleGet(Uri uri)
        {
            return ProcessGetRequest(uri.LocalPath);
        }

        /// <summary>
        /// Обрабатывает PUT-запрос. Использует ProcessPutRequest для парсинга URL и маршрутизации запроса.
        /// </summary>
        /// <param name="uri">URI запроса.</param>
        /// <param name="body">Тело запроса в формате JSON.</param>
        /// <returns>Результат обработки PUT-запроса от ProcessPutRequest.</returns>
        public RequestHandlingResult HandlePut(Uri uri, string body)
        {
            return ProcessPutRequest(uri.LocalPath, body);
        }

        /// <summary>
        /// Обрабатывает PUT-запрос, парсит URL и маршрутизирует к соответствующему обработчику. Использует PutInfo для обработки запросов информации о сервере, PutMatches для обработки запросов информации о матчах и Fail для создания результата с ошибкой.
        /// </summary>
        /// <param name="requestString">Строка URL запроса.</param>
        /// <param name="body">Тело запроса в формате JSON.</param>
        /// <returns>Результат обработки запроса от соответствующего обработчика или Fail с BadRequest при некорректном запросе.</returns>
        private RequestHandlingResult ProcessPutRequest(string requestString, string body)
        {
            var splitedString = PutRequestRegex.Split(requestString);

            if (splitedString.Length <= 3 || body.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);

            switch (splitedString[3])
            {
                case "info":
                    return PutInfo(splitedString[2], body);
                case "matches":
                    return PutMatches(splitedString[2], body, splitedString[4]);
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Обрабатывает PUT-запрос для регистрации или обновления информации о сервере. Использует TryDeserialize для десериализации тела запроса, CheckInfoField для проверки обязательных полей, UpdateAdvertServer или AddAdvertServer для сохранения в базу данных и Successfull для создания успешного результата.
        /// </summary>
        /// <param name="endpoint">Endpoint сервера для регистрации или обновления.</param>
        /// <param name="body">Тело запроса в формате JSON с информацией о сервере.</param>
        /// <returns>Результат обработки запроса: Successfull при успехе, Fail с BadRequest при ошибке валидации.</returns>
        private RequestHandlingResult PutInfo(string endpoint, string body)
        {
            Information info;
            if (!jsonSerializer.TryDeserialize(body, out info) || !CheckInfoField(body))
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            var advertRequest = new AdvertiseQueryServer(endpoint, info);
            if (AdvertiseServers.ContainsKey(endpoint))
                dataBase.UpdateAdvertServer(advertRequest);
            else
                dataBase.AddAdvertServer(advertRequest);
            AdvertiseServers.AddOrUpdate(endpoint, advertRequest, ((s, server) => advertRequest));
            return RequestHandlingResult.Successfull(new byte[0]);
        }

        /// <summary>
        /// Обрабатывает PUT-запрос для добавления информации о матче. Использует CheckMatchField для проверки обязательных полей, TryDeserialize для десериализации тела запроса, CheckGameMode для проверки поддерживаемого игрового режима, AddGameServer для сохранения в базу данных и Successfull для создания успешного результата.
        /// </summary>
        /// <param name="endpoint">Endpoint сервера, на котором прошел матч.</param>
        /// <param name="body">Тело запроса в формате JSON с информацией о матче.</param>
        /// <param name="date">Дата и время матча в формате ISO 8601.</param>
        /// <returns>Результат обработки запроса: Successfull при успехе, Fail с BadRequest при ошибке валидации.</returns>
        private RequestHandlingResult PutMatches(string endpoint, string body, string date)
        {
            GameServer gameServer;
            if (!AdvertiseServers.ContainsKey(endpoint) ||
                !CheckMatchField(body) ||
                !jsonSerializer.TryDeserialize(body, out gameServer) ||
                !CheckGameMode(endpoint, gameServer.GameMode))
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            gameServer.Endpoint = endpoint;
            gameServer.DateAndTime = DateTime.Parse(date, null, DateTimeStyles.RoundtripKind);
            GameServers.Add(gameServer);
            dataBase.AddGameServer(gameServer);
            return RequestHandlingResult.Successfull(new byte[0]);
        }

        /// <summary>
        /// Проверяет наличие обязательных полей "name" и "gameModes" в теле запроса информации о сервере.
        /// </summary>
        /// <param name="infoBody">Тело запроса в формате JSON.</param>
        /// <returns>true, если оба обязательных поля присутствуют; иначе false.</returns>
        private bool CheckInfoField(string infoBody)
        {
            return infoBody.Contains("\"name\":") &&
                   infoBody.Contains("\"gameModes\":");
        }

        /// <summary>
        /// Проверяет наличие всех обязательных полей в теле запроса информации о матче.
        /// </summary>
        /// <param name="matchBody">Тело запроса в формате JSON.</param>
        /// <returns>true, если все обязательные поля (map, gameMode, fragLimit, timeLimit, timeElapsed, scoreboard) присутствуют; иначе false.</returns>
        private bool CheckMatchField(string matchBody)
        {
            return matchBody.Contains("\"map\":") &&
                   matchBody.Contains("\"gameMode\":") &&
                   matchBody.Contains("\"fragLimit\":") &&
                   matchBody.Contains("\"timeLimit\":") &&
                   matchBody.Contains("\"timeElapsed\":") &&
                   matchBody.Contains("\"scoreboard\":");
        }

        /// <summary>
        /// Проверяет, поддерживает ли сервер с указанным endpoint указанный игровой режим.
        /// </summary>
        /// <param name="endpoint">Endpoint сервера для проверки.</param>
        /// <param name="gameMode">Игровой режим для проверки.</param>
        /// <returns>true, если сервер поддерживает указанный игровой режим; иначе false.</returns>
        private bool CheckGameMode(string endpoint, string gameMode)
        {
            return AdvertiseServers.Where(x => x.Key == endpoint)
                .Select(x => x.Value.Info.GameModes).Any(x => x.Contains(gameMode));
        }

        /// <summary>
        /// Обрабатывает GET-запрос, парсит URL и маршрутизирует к соответствующему обработчику. Использует GetServerInformation для обработки запросов информации о серверах, GetPlayersStatistic для обработки запросов статистики игроков, GetReport для обработки запросов отчетов и Fail для создания результата с ошибкой.
        /// </summary>
        /// <param name="requestString">Строка URL запроса.</param>
        /// <returns>Результат обработки запроса от соответствующего обработчика или Fail с BadRequest при некорректном запросе.</returns>
        private RequestHandlingResult ProcessGetRequest(string requestString)
        {
            var splitedRequest = GetRequestRegex.Split(requestString);

            if (splitedRequest.Length < 2) return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);

            switch (splitedRequest[1])
            {
                case "servers":
                    return GetServerInformation(splitedRequest[2]);
                case "players":
                    return GetPlayersStatistic(splitedRequest[2]);
                case "reports":
                    return GetReport(splitedRequest[2]);
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// Получает статистику игрока по имени. Использует GetPlayerStatistic для вычисления статистики, Serialize для сериализации результата в JSON, GetBytesInAscii для преобразования в байты и Successfull для создания успешного результата.
        /// </summary>
        /// <param name="request">Строка запроса, содержащая имя игрока.</param>
        /// <returns>Результат обработки запроса: Successfull с сериализованной статистикой игрока или Fail с NotFound, если игрок не найден.</returns>
        private RequestHandlingResult GetPlayersStatistic(string request)
        {
            var splitRequest = request.Split('/');
            var name = splitRequest[0];
            var games = GameServers
                .Where(x => x.Scoreboard.Any(y => y.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
                .ToArray();
            if (games.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            var serializStatistic = jsonSerializer.Serialize(statistic.GetPlayerStatistic(name, games));
            return RequestHandlingResult.Successfull(serializStatistic.GetBytesInAscii());
        }

        /// <summary>
        /// Получает отчет в зависимости от типа запроса. Использует GetRightCountOfItems для нормализации количества элементов, GetRecentMatches для получения недавних матчей, GetBestPlayers для получения лучших игроков, GetPopularServers для получения популярных серверов, Serialize для сериализации результата в JSON, GetBytesInAscii для преобразования в байты и Successfull для создания успешного результата.
        /// </summary>
        /// <param name="request">Строка запроса, содержащая тип отчета и опционально количество элементов.</param>
        /// <returns>Результат обработки запроса: Successfull с сериализованным отчетом или Fail с BadRequest при некорректном типе отчета.</returns>
        private RequestHandlingResult GetReport(string request)
        {
            var splitRequest = ReportRegex.Split(request)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
            var n = 5;
            if (splitRequest.Length > 1)
                n = GetRightCountOfItems(int.Parse(splitRequest[1]));
            if (n == 0 || GameServers.Count == 0)
            {
                var emptyAnswer = jsonSerializer.Serialize(new string[0]);
                return RequestHandlingResult.Successfull(emptyAnswer.GetBytesInAscii());
            }
            object reportResult;
            switch (splitRequest[0])
            {
                case "recent-matches":
                    reportResult = statistic.GetRecentMatches(n, GameServers);
                    break;
                case "best-players":
                    reportResult = statistic.GetBestPlayers(n, GameServers);
                    break;
                case "popular-servers":
                    reportResult = statistic.GetPopularServers(n, AdvertiseServers, GameServers);
                    break;
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
            var serializeReport = jsonSerializer.Serialize(reportResult);
            return RequestHandlingResult.Successfull(serializeReport.GetBytesInAscii());
        }

        /// <summary>
        /// Нормализует количество элементов: возвращает 0 для отрицательных значений, ограничивает максимум до 50.
        /// </summary>
        /// <param name="countOfItems">Исходное количество элементов.</param>
        /// <returns>0, если значение меньше или равно 0; 50, если значение больше или равно 50; иначе исходное значение.</returns>
        private static int GetRightCountOfItems(int countOfItems)
        {
            if (countOfItems <= 0) return 0;
            return countOfItems >= 50 ? 50 : countOfItems;
        }

        /// <summary>
        /// Получает информацию о сервере или статистику сервера. Использует GetServerStatistic для вычисления статистики сервера, GetAdvertMatch для получения информации о конкретном матче, Serialize для сериализации результата в JSON, GetBytesInAscii для преобразования в байты, Successfull для создания успешного результата и Fail для создания результата с ошибкой.
        /// </summary>
        /// <param name="request">Строка запроса, содержащая endpoint сервера и тип запрашиваемой информации.</param>
        /// <returns>Результат обработки запроса: Successfull с сериализованной информацией или Fail с NotFound/BadRequest при ошибке.</returns>
        private RequestHandlingResult GetServerInformation(string request)
        {
            var splitedRequest = ServerInfoRegex.Split(request);
            if (splitedRequest[1] == "info")
            {
                var serializeServers = jsonSerializer.Serialize(AdvertiseServers.Values.ToArray());
                return RequestHandlingResult.Successfull(serializeServers.GetBytesInAscii());
            }
            var endpoint = splitedRequest[1];
            var neededGames = GameServers.Where(x => x.Endpoint == endpoint).ToArray();
            var neededAdvertServer = AdvertiseServers.Where(x => x.Key == endpoint).ToArray();
            if (!neededAdvertServer.Any() && neededGames.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            object informations;
            switch (splitedRequest[2])
            {
                case "info":
                    informations = neededAdvertServer[0].Value.Info;
                    break;
                case "matches":
                    return GetAdvertMatch(splitedRequest[3], neededGames);
                case "stats":
                    informations = statistic.GetServerStatistic(neededGames);
                    break;
                default:
                    return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
            var serializeInformation = jsonSerializer.Serialize(informations);
            return RequestHandlingResult.Successfull(serializeInformation.GetBytesInAscii());
        }

        /// <summary>
        /// Получает информацию о конкретном матче по дате. Использует Serialize для сериализации результата в JSON, GetBytesInAscii для преобразования в байты, Successfull для создания успешного результата и Fail для создания результата с ошибкой.
        /// </summary>
        /// <param name="date">Дата и время матча в формате ISO 8601.</param>
        /// <param name="games">Массив игровых серверов для поиска.</param>
        /// <returns>Результат обработки запроса: Successfull с сериализованной информацией о матче или Fail с BadRequest/NotFound при ошибке.</returns>
        private RequestHandlingResult GetAdvertMatch(string date, GameServer[] games)
        {
            DateTime dateTime;
            try
            {
                dateTime = DateTime.Parse(date, null, DateTimeStyles.RoundtripKind);
            }
            catch (Exception)
            {
                return RequestHandlingResult.Fail(HttpStatusCode.BadRequest);
            }
            var match = games.Where(x => x.DateAndTime == dateTime).ToArray();
            if (match.Length == 0)
                return RequestHandlingResult.Fail(HttpStatusCode.NotFound);
            var serializeMatch = jsonSerializer.Serialize(match[0]);
            return RequestHandlingResult.Successfull(serializeMatch.GetBytesInAscii());
        }
    }
}