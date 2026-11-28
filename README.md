# User Company Platform

## Лабораторна № 6

Aggregator читає заявки, компанії та сповіщення через gRPC. Redis зберігає огляд користувача
на 30 секунд, а каталог компаній використовує кеш у пам’яті та Redis.
Запуск через Platform.AppHost, Gateway доступний на http://localhost:8080.
[Налаштування й ручна перевірка](docs/lab06.md).


## Лабораторна №5: мікросервісна платформа

Основне рішення — **Platform.slnx**. Запуск — **Platform.AppHost**, вхідний HTTP-порт Gateway — **8080**.
Збережено погоджену PostgreSQL замість SQL Server. Npgsql трасує запити Dapper, ADO.NET та EF Core;
SqlClient не підключається, оскільки ці сервіси його не використовують.

```text
Gateway :8080 → Aggregator → Users / Companies / Notifications
            ↘ Users / Companies / Notifications
Users → PostgreSQL lab01_users
Companies → PostgreSQL lab03_companies
Notifications → MongoDB lab04_notifications
```

### Структура та залежності

| Компонент | Призначення |
|---|---|
| Platform.AppHost/AppHost.cs | PostgreSQL, MongoDB, RabbitMQ, Redis, DatabaseSetup, три API, worker, Aggregator і Gateway |
| Platform.ServiceDefaults/Extensions.cs | Serilog, OTLP, HTTP service discovery, resilience, health checks |
| Platform.ServiceDefaults/CorrelationIdMiddleware.cs | Перевірка/створення X-Correlation-Id, LogContext, відповідь клієнту |
| Platform.ServiceDefaults/CorrelationIdHandler.cs | Передавання CorrelationId у вихідні HTTP-запити |
| Platform.ApiGateway/ | YARP, JWT, логування маршруту й кластера; Dockerfile |
| Platform.Aggregator/ | Typed HttpClients, DTO, паралельні запити, часткові відповіді; Dockerfile |

AppHost посилається на виконувані проєкти; API, Aggregator і Gateway підключають ServiceDefaults.
Gateway та Aggregator не посилаються на DLL мікросервісів. Жоден сервіс не посилається на AppHost.
Власні шари Domain/Application/Infrastructure та їхні залежності попередніх лабораторних збережено.
Старий UserCompanyPlatform.AppHost і .sln залишені для попередніх робіт; для №5 використовується Platform.slnx.
Код попередніх лаб не переміщено й не продубльовано: [відповідність папок](Services/README.md),
[джерела схем БД](db/README.md), [контексти й модель агрегування](docs/platform-contexts.md).

### Перший запуск

Потрібні Docker Desktop із Linux containers, .NET 8 Runtime і SDK **9.0.200 або новіший** для Platform.slnx.
Проєкти сервісів залишаються net8.0. Відкрийте корінь репозиторію в терміналі.
Якщо запущено старий Compose чи старий AppHost, зупиніть їх перед запуском цієї платформи.

Один раз задайте секрети AppHost. Значення нижче — приклади для локальної лабораторної:

```powershell
dotnet user-secrets set "Parameters:postgres-password" "Lab05PostgresLocal2026" --project Platform.AppHost
dotnet user-secrets set "Parameters:jwt-key" "Lab05-Local-Jwt-Key-Change-This-32-Bytes-Minimum" --project Platform.AppHost
dotnet dev-certs https --trust
dotnet run --project Platform.AppHost/Platform.AppHost.csproj
```

Адреса Dashboard та посилання з токеном доступу з'являться в консолі AppHost.
Gateway: **http://localhost:8080**. Прямі адреси трьох API та Aggregator дивіться у Dashboard:
Aspire призначає їм порти автоматично. `/alive` перевіряє процес, `/health` — готовність;
SQL API перевіряють PostgreSQL, Notification API — MongoDB.

PostgreSQL і MongoDB мають постійні volumes `platform-lab05-postgres` та `platform-lab05-mongo`.
Вони окремі від старого Compose: дані звідти автоматично не переносяться.
DatabaseSetup створює бази, ролі, SQL-процедури, міграції EF Core та демодані;
Notification API створює свої MongoDB-колекції, індекси та seed.
Порядок задають WaitFor/WaitForCompletion: БД → DatabaseSetup → API → Aggregator → Gateway.
DatabaseSetup завершується з кодом 0 — це очікувано, він не повинен постійно залишатись Running.

WithReference передає service discovery і рядки підключення.
SQL-ресурси users-db/companies-db передаються відповідному сервісу з alias `PostgreSQLConnection`;
MongoDB-ресурс — `NotificationsDb`. Для застосунків передаються рядки з обмеженими сервісними логінами;
адміністратор PostgreSQL використовується DatabaseSetup. Демопаролі сервісних ролей залишено з №1.
JWT-секрет однаковий для UserService і Gateway та передається з параметра AppHost.

### Запити через Gateway

Проксі-маршрути: `/api/users/**`, `/api/memberships/**`, `/api/companies/**`,
`/api/company-structure/**`, `/api/notifications/**`, `/api/templates/**`, `/api/aggregator/**`.
Маршрут Aggregator має Order=-10, звичайні — 10, вхід/реєстрація — -100.
YARP використовує адреси `http://users-api` тощо та resolver service discovery.

Усі проксі-маршрути, крім реєстрації та входу, вимагають JWT. Це перевірка підпису й строку дії
наявного токена UserService; токени цього сервісу поки не містять issuer/audience.
Прямі внутрішні API залишаються навчальними endpoint-ами попередніх лабораторних;
перевірка JWT реалізована на Gateway, а не як повна система прав доступу до ресурсів.

Приклад для PowerShell: спочатку зареєструйте окремого користувача для демонстрації.
Якщо він уже існує, пропустіть реєстрацію й виконайте вхід.

```powershell
$base = "http://localhost:8080"
$registration = @{
    firstName = "Demo"
    lastName = "User"
    email = "lab05@example.test"
    password = "Lab05Demo2026!"
} | ConvertTo-Json
Invoke-RestMethod "$base/api/users/Register/register" -Method Post -ContentType "application/json" -Body $registration
$login = @{ email = "lab05@example.test"; password = "Lab05Demo2026!" } | ConvertTo-Json
$session = Invoke-RestMethod "$base/api/users/Login/login" -Method Post -ContentType "application/json" -Body $login
$headers = @{ Authorization = "Bearer $($session.token)"; "X-Correlation-Id" = [guid]::NewGuid().ToString() }
Invoke-RestMethod "$base/api/companies?page=1&pageSize=10" -Headers $headers
Invoke-RestMethod "$base/api/notifications?page=1&pageSize=10" -Headers $headers
Invoke-RestMethod "$base/api/aggregator/users/10000000-0000-0000-0000-000000000001/overview" -Headers $headers
```

Огляд користувача повертає `userId`, `memberships`, `companies`, `notifications`, `warnings`, `membershipPage`.
Заявки та сповіщення читаються через Task.WhenAll; компанії зі сторінки заявок також читаються паралельно.
Параметр `page` за замовчуванням 1; сповіщення містять власні totalCount/page/pageSize.
API не вдає, що одна сторінка є повною історією користувача.
JSON має camelCase; null-поля не серіалізуються.

Для прямого виклику Aggregator скопіюйте його URL із Dashboard:

```powershell
$aggregatorUrl = "http://localhost:ПОРТ_ІЗ_DASHBOARD"
Invoke-RestMethod "$aggregatorUrl/api/aggregator/users/10000000-0000-0000-0000-000000000001/overview" -Headers $headers
```

### Часткові відмови та телеметрія

1. Виконайте огляд seed-користувача з прикладу: є заявка Contoso та сповіщення.
2. Зупиніть notifications-api у Dashboard, не зупиняючи Gateway і Aggregator.
3. Повторіть запит: HTTP 200, заявки та компанії доступні, у warnings — notifications-api.
4. Запустіть сервіс знову. Після відновлення discovery/circuit breaker повна відповідь повертається.
5. Повторіть сценарій із companies-api: заявки й повідомлення залишаються у відповіді.
6. За зупиненого users-api сповіщення залишаються; компанії не запитуються, бо ID беруться із заявок.

Клієнти мають timeout, retry та circuit breaker. Повтори вимкнено для POST/PUT/PATCH/DELETE/CONNECT.
Resilience обмежує HTTP-спроби, окремий 15-секундний deadline обмежує також читання JSON.
Кожен виклик логує назву сервісу, тривалість і тип збою; тіло відповіді й Authorization не логуються.
Скасування клієнтом передається downstream та не перетворюється на успішну часткову відповідь.

X-Correlation-Id нормалізується як GUID; відсутній або невалідний замінюється новим.
Він є в заголовку відповіді, LogContext і вихідних HTTP-запитах. YARP передає той самий заголовок.
TraceId/SpanId створюються OpenTelemetry, а CorrelationId використовується для пошуку бізнес-звернення.
Serilog додає ServiceName, Environment, MachineName, ThreadId і пише Compact JSON у консоль;
`writeToProviders: true` передає події також в OpenTelemetry й Structured logs Dashboard.

У Traces знайдіть запит `/overview`: Gateway → Aggregator → Users/Notifications → PostgreSQL/MongoDB,
а після отримання ID компаній — Companies → PostgreSQL.
Власні spans: `users.overview`, `memberships.list`, `companies.get` та MediatR-операції Notifications.
У Structured logs відфільтруйте CorrelationId з `$headers`.
SQL-текст і текст команд MongoDB не експортуються; повні запити, email і повідомлення не логуються новим кодом.
У Development sampling 100%; в інших середовищах `Telemetry__SamplingRatio` за замовчуванням 0.1.

### Таблиця дублювання

| Дані | Власник | Копія / політика |
|---|---|---|
| Назва компанії | Companies | Users.company_directory — локальна проєкція з source_version |
| Назва в заявці | Companies на момент події | companyNameSnapshot — історичний знімок, не переписується |
| Отримувач і company у сповіщенні | Users / Companies на момент створення | Незмінні вкладені знімки MongoDB |
| Останні спроби доставки | Notifications.deliveryAttempts | recentAttempts — останні три спроби |
| DTO Aggregator | Відповідні сервіси | Тимчасова композиція на один HTTP-запит, не окрема БД і не кеш |

### Перевірка та здача

Збірка, запуск контейнерів і інтеграційні тести під час внесення змін не виконувалися.
Для власної перевірки: `dotnet build Platform.slnx`, потім запуск AppHost командою вище.
Скриншоти: ресурси Dashboard, trace агрегування, логи з одним CorrelationId, відповідь із warnings при відмові.
Dockerfile Gateway/Aggregator збираються з кореня репозиторію; оркестрація №5 виконується AppHost.
Старий docker-compose.yml запускає попередній набір сервісів без нового Gateway/Aggregator.

Документація: [service discovery YARP](https://www.nuget.org/packages/Microsoft.Extensions.ServiceDiscovery.Yarp/9.3.0),
[MongoDB DiagnosticSources](https://github.com/jbogard/MongoDB.Driver.Core.Extensions.DiagnosticSources),
[Npgsql tracing](https://www.npgsql.org/doc/diagnostics/tracing.html),
[підтримка SLNX](https://devblogs.microsoft.com/dotnet/introducing-slnx-support-dotnet-cli/).


## Лабораторна №4

NotificationService доповнено Application та API для Clean Architecture, CQRS/MediatR,
FluentValidation, MongoDB-проєкцій, Value Objects, контролю версій та ProblemDetails.
Swagger сповіщень: http://localhost:5002/swagger.
[Архітектура, запуск і ручна перевірка](database/lab04/README.md).

API використовує нову **lab04_notifications** з ObjectId та структурою колекцій із ЛР №1.
Попередня **lab01_notifications** і worker продовжують працювати окремо;
старі записи не переносяться автоматично. Для наявного Compose повторно запустіть
DatabaseSetup, щоб видати сервісному логіну права на нову базу.

## Лабораторна №3

CompanyService використовує EF Core Code First, Generic Repository, Unit of Work,
BLL, DTO, AutoMapper та специфікації. Додано пагінацію, валідацію, ProblemDetails
і Serilog. [Усі 14 критеріїв та ручна перевірка](database/lab03/README.md).

DatabaseSetup застосовує міграцію до **lab03_companies** і додає демодані.
База **lab01_companies** залишається без змін; її власні записи автоматично не переносяться.
Компанії Contoso/Fabrikam мають ті самі фіксовані ID, що й у попередніх лабораторних.
Compose та Aspire вже налаштовані на нову базу. Команди запуску нижче залишаються актуальними.

## Лабораторна №2

Заявки тепер проходять через BLL-сервіс, два Dapper-репозиторії та ADO.NET-репозиторій
зі спільним Unit of Work. Контролер не містить SQL. Помилки повертаються як ProblemDetails;
недопустимий перехід статусу тепер має код **409**, а не 400.

Запуск усіх компонентів через Aspire:

```powershell
dotnet run --project UserCompanyPlatform.AppHost/UserCompanyPlatform.AppHost.csproj
```

[Структура ЛР №2, приклади запитів і перевірка rollback](database/lab02/README.md).
Якщо вже запущений Compose, спочатку зупиніть його через `docker compose stop`.
Нижче збережено інструкції базового Docker/локального запуску з ЛР №1.

Лабораторна №1 інтегрована в наявний .NET solution. Python для запуску не потрібен.

## Як працює

- **UserService.API**: реєстрація/вхід/користувачі через EF Core у `lab01_users`;
  `api/memberships` використовує BLL, Dapper/ADO.NET та процедури через спільний Unit of Work.
  Роль JWT береться з `User.Role`, пароль перевіряється BCrypt, як і раніше.
- **CompanyService.API**: компанії, реквізити 1:1, підрозділи 1:N та теги M:N
  відображені на `lab03_companies` через EF Core Fluent API.
- **NotificationService.Worker**: події реєстрації надходять через RabbitMQ;
  документи й історія доставлення зберігаються у MongoDB `lab01_notifications`.
- **DatabaseSetup**: C#-консольний проєкт створює бази, логіни, таблиці,
  процедури, колекції, індекси та seed-дані. Повторний запуск передбачено.

Нові бази не переносять автоматично дані зі старої `UserService`. Старі EF-міграції
залишені як історія попередньої схеми. Для CompanyService активна нова міграція
`Migrations/Lab03/InitialCreate`; старі файли в корені `Migrations` виключені з компіляції.
DatabaseSetup застосовує нові міграції та видає ролям права; API працює з обмеженими правами.

## Найпростіший запуск: Docker

Потрібен Docker Desktop із Linux containers. Із кореня репозиторію:

```powershell
docker compose up --build -d
docker compose logs database-setup
docker compose logs users-service companies-service notifications-worker --tail 50
```

Compose запускає PostgreSQL, MongoDB, RabbitMQ, Redis, потім C# DatabaseSetup;
API та worker стартують лише після успішного завершення підготовки баз.
Контейнер database-setup зі статусом Exited (0) — це очікувана поведінка.

- Swagger користувачів: http://localhost:5000
- Swagger компаній: http://localhost:5001
- PostgreSQL: localhost:55432; MongoDB: localhost:57017.
- RabbitMQ management: http://localhost:15672 (guest / guest).

Використовуються нові томи `lab-sql`/`lab-mongo`, старі томи не видаляються.
Якщо попередні контейнери займають 5000/5001, спочатку зупиніть їх.
RabbitMQ AMQP (5672) та Redis (6379) доступні лише всередині Docker-мережі:
сервіси звертаються до rabbitmq:5672 і redis:6379, без прив'язки цих портів Windows.
Локальні demo-паролі задані в Compose; можна перевизначити POSTGRES_PASSWORD,
MONGO_ROOT_PASSWORD, LAB01_USERS_PASSWORD, LAB01_COMPANIES_PASSWORD,
LAB01_NOTIFICATIONS_PASSWORD через кореневий `.env`. Для пароля в Mongo URI
використовуйте URL-безпечне значення або правильне URL-кодування.
Старий `database/lab01/.env` автоматично не читається новим C#-проєктом.

## Запуск C# з IDE / dotnet

Потрібні локальні PostgreSQL, MongoDB, RabbitMQ та Redis.
DatabaseSetup за замовчуванням використовує PostgreSQL localhost:5432 із попередніми
локальними налаштуваннями адміністратора, MongoDB localhost:27017. Для іншого
оточення задайте змінні перед запуском:

```powershell
$env:POSTGRES_ADMIN = 'Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD'
$env:MONGO_ADMIN_URI = 'mongodb://localhost:27017'
dotnet run --project DatabaseSetup
```

Для MongoDB з авторизацією вкажіть URI адміністратора з authSource=admin.
Програма створює окремого MongoDB-користувача; на сервері має бути ввімкнена
авторизація, щоб обмеження його доступу застосовувались.
Якщо використовуєте окремий MongoDB на 27019 із попереднього етапу, задайте його
адміністративний URI в MONGO_ADMIN_URI та оновіть MongoDB connection string worker.

Налаштування API у appsettings.json відповідають стандартним локальним портам і
demo-паролям DatabaseSetup. Якщо змінюєте паролі/порти, змініть також
ConnectionStrings__PostgreSQLConnection / ConnectionStrings__MongoDB у сервісах.
Кожен процес запускається в окремому терміналі:

```powershell
dotnet run --project CompanyService.API
dotnet run --project UserService.API
dotnet run --project NotificationService.Worker
```

## Що перевірити у Swagger

1. Створіть компанію через `Companies/Create` або використайте seed Contoso:
   `20000000-0000-0000-0000-000000000001`.
2. Зареєструйте користувача через `Users/Register/register`, виконайте login.
   Seed-користувачі не мають паролів і призначені лише для прикладів заявок.
3. `POST /api/memberships`: передайте userId, companyId та role (`Developer`).
4. `GET /api/memberships/{id}`: скопіюйте поточне `version`.
5. `POST /api/memberships/{id}/status`: передайте status=AwaitingValidation і version.
   Потім Approved та Active, щоразу читаючи нову версію. Submitted → Active дає 409,
   застаріла версія — 409, невідомий ID — 404. Cancelled дозволений до Active;
   DELETE архівує лише Cancelled-заявку.
6. У `api/company-structure` перегляньте теги, додайте компанії тег та підрозділ.
7. У MongoDB перевірте notifications: після реєстрації там буде документ Pending
   з recipient, payload і scheduledAt через два дні. Повторна доставка події
   не створює дублікат повідомлення.

Worker перевіряє реєстраційні повідомлення раз на добу, надсилає через два дні.
Для швидкої ручної перевірки встановіть scheduledAt у минуле й перезапустіть worker.
SMTP-пароль прибрано з конфігурації: для реального надсилання налаштуйте EmailSettings
і передайте SMTP_PASSWORD у Docker або EmailSettings__Password локально.
Без SMTP можна перевіряти створення документів; стан Sent потребує успішного надсилання.
Поточний worker розрахований на один екземпляр; гарантії доставки без повторного
листа при аварії між SMTP та записом MongoDB ще не реалізовані.

## Межі перевірки

Після інтеграції виконується лише компіляція; запуск Docker, API, баз та SMTP
і перевірка сценаріїв залишені користувачеві за домовленістю.
Гілку й коміт автоматично не створено.

[Модель і вимоги лабораторної](database/lab01/README.md) · [ERD](database/lab01/ERD.md)
