# User Company Platform

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
