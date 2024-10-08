using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OmegaImportPusher
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            // URLs для запросов
            string authUrl = "https://internal.omp-system.ru/ompserver/daily/pxm/login.getAuthToken";
            string importUrl = "https://internal.omp-system.ru/ompserver/daily/pxm/imports/universalXmlImport.doImport";

            // Данные для авторизации
            var authData = new
            {
                login = "WORK",
                password = "WORK"
            };

            string authToken = string.Empty;
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    // Спрашиваем у пользователя, нужно ли получать новый токен
                    Console.WriteLine("Получить новый токен? (да/нет): ");
                    string getTokenResponse = Console.ReadLine()?.ToLower();

                    if (getTokenResponse == "да")
                    {
                        // Получение нового токена
                        var authContent = new StringContent(JsonConvert.SerializeObject(authData), Encoding.UTF8, "application/json");

                        HttpResponseMessage authResponse = await client.PostAsync(authUrl, authContent);
                        authResponse.EnsureSuccessStatusCode();

                        string authResponseBody = await authResponse.Content.ReadAsStringAsync();
                        dynamic authResponseJson = JsonConvert.DeserializeObject(authResponseBody);
                        authToken = authResponseJson.authToken;

                        if (string.IsNullOrEmpty(authToken))
                        {
                            Console.WriteLine("authToken не получен!");
                            return;
                        }

                        // Отображение полученного токена
                        Console.WriteLine($"Получен authToken: {authToken}");
                    }
                    else
                    {
                        // Если не нужен новый токен, программа завершает выполнение
                        Console.WriteLine("Использование токена пропущено.");
                        return;
                    }

                    // Спрашиваем, сколько сообщений сгенерировать
                    Console.WriteLine("Сколько сообщений сгенерировать?");
                    if (!int.TryParse(Console.ReadLine(), out int messageCount) || messageCount <= 0)
                    {
                        Console.WriteLine("Некорректное число. Попробуйте снова.");
                        return;
                    }

                    // Добавление authToken в заголовок
                    client.DefaultRequestHeaders.Add("authToken", authToken);

                    // Цикл для генерации сообщений
                    for (int i = 0; i < messageCount; i++)
                    {
                        string randomGuid = Guid.NewGuid().ToString();
                        string xmlData = $@"
<OmegaProduction>
    <Деталь>
        <внешний_код знач=""{randomGuid}"" />
        <БО>
            <поле наим=""BOState"" знач=""Проектирование"" />
            <поле наим=""Владелец"" знач=""ТЕСТ"" />
            <CO></CO>
        </БО>
        <поле наим=""Обозначение элемента"" знач=""{randomGuid}"" />
        <поле наим=""Заводской код"" знач="""" />
        <поле наим=""ОКП код"" знач="""" />
        <поле наим=""Наименование"" знач="""" />
        <поле наим=""Формат"" знач="""" />
        <поле наим=""Масса"" знач=""0"" />
        <поле наим=""Единица измерения"" знач=""Штука"" Наименование=""Штука"" КраткоеНаименование=""шт"" КодБМН=""796"" код_омеги=""106"" />
        <поле наим=""Литера документации"" знач=""О"" />
        <поле наим=""Конечный объект"" знач=""Нет"" />
        <поле наим=""Признак изготовления"" знач=""Собственное"" />
    </Деталь>
</OmegaProduction>";

                        var importData = new
                        {
                            xmlStr = xmlData,
                            @params = new
                            {
                                logParams = new
                                {
                                    AppendErrors = true,
                                    AppendInfo = false,
                                    AppendResults = true,
                                    AppendWarnings = false
                                },
                                oneTransaction = false
                            }
                        };

                        var importContent = new StringContent(JsonConvert.SerializeObject(importData), Encoding.UTF8, "application/json");

                        Console.WriteLine($"Отправка сообщения {i + 1} с GUID: {randomGuid}");

                        HttpResponseMessage importResponse = await client.PostAsync(importUrl, importContent);
                        importResponse.EnsureSuccessStatusCode();

                        string importResponseBody = await importResponse.Content.ReadAsStringAsync();
                        Console.WriteLine(importResponseBody);
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    Console.WriteLine($"HTTP ошибка: {httpEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }
        }
    }
}
