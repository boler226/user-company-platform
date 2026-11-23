# Лабораторна № 4: Clean Architecture та MongoDB

Предметна область — сповіщення і шаблони платформи користувачів та компаній.
Назви `Review` / `IReviewQueries` з методички адаптовано до `Notification` / `INotificationQueries`.

```text
NotificationService.API → Application → Domain
NotificationService.API → Infrastructure → Application / Domain
```

Domain має лише MongoDB.Bson для ObjectId, без MongoDB.Driver і посилань на інші проєкти.
Старі worker, споживачі RabbitMQ та їхні класи залишаються для лабораторної № 1.
Infrastructure також використовує Contracts для цих споживачів; новий Application залежить лише від Domain.

## Сумісність даних

API працює з **lab04_notifications**. Структуру `notifications`, `templates`,
`deliveryAttempts` взято з лабораторної № 1, але ідентифікатори документів,
templateId, notificationId та attemptId тепер мають BSON-тип ObjectId.
Посилання на користувачів і компанії залишаються рядковими UUID.
Нові JSON Schema вбудовані в Infrastructure; колекції створюються з валідацією.
Додавання полів до вже створених схем потребує окремої міграції.

Старі документи з рядковими ID автоматично не переносяться.
Worker продовжує працювати з **lab01_notifications**; новий API не надсилає листів.
У новій базі зберігаються керовані через API сповіщення, шаблони та демонстраційна історія доставки.
Окремого API для реєстрації доставки немає; recentAttempts показує останні три спроби агрегату.

## Реалізація критеріїв

| Критерій | Реалізація |
|---|---|
| Чотири шари | Domain, Application, Infrastructure, API |
| Сутності | Notification, NotificationTemplate; BaseEntity з ObjectId, аудитом, Version; приватні setter-и |
| Value Objects | Recipient, CompanySnapshot, RecentAttempt; ValueObject порівнює компоненти |
| Репозиторії | INotificationRepository, ITemplateRepository у Domain |
| CQRS | Create/Update/Delete і GetById/GetList з окремими MediatR-обробниками для обох ресурсів |
| Читання | INotificationQueries / ITemplateQueries, MongoDB-проєкція одразу в DTO, sort/skip/limit, text-пошук |
| DI | AddApplication, AddInfrastructure, AddPresentation; сканування assembly |
| Валідація | FluentValidation для всіх команд і запитів, ObjectId, UUID, асинхронні перевірки шаблону та дубля |
| Behaviors | Validation → Logging → Performance; попередження після 500 мс |
| BSON | IOptions, одноразові BsonClassMap, camelCase, MapCreator для VO, приватні поля payload/recentAttempts |
| Конкурентність | ReplaceOne з фільтром Id + expectedVersion + isDeleted; нуль збігів дає 409 |
| API | ISender, AutoMapper для результатів створення, вкладені DTO, Swagger, 201 з Location |
| Помилки | ProblemDetails: 400, 404, 409, 503, 500; DuplicateKey дає 409 |
| Ініціалізація | JSON Schema, складені/text/unique індекси, IDataSeeder з InsertManyAsync та перевіркою ID |

Видалення м'яке: isDeleted=true, Version збільшується, запис зникає з GET.
Шаблон можна архівувати, зберігши посилання з історичних повідомлень.
Code + Channel зарезервовані унікальним індексом навіть після архівування.
Дані seed мають фіксовані ID; повторний старт не перезаписує редагування й не відновлює видалені записи.
Вбудовані отримувач і компанія — історичні знімки; API не виконує міжбазових запитів для їх оновлення.

## Запуск

Потрібні .NET 8 SDK і MongoDB 8 або Docker Desktop.
Із кореня solution:

```powershell
docker compose up --build -d
```

Якщо Compose вже запускався до лабораторної № 4, спочатку оновіть права сервісного логіна:

```powershell
docker compose up --build --force-recreate database-setup
docker compose up --build -d notifications-api
```

Альтернатива — Aspire, який запускає DatabaseSetup перед API:

```powershell
dotnet run --project UserCompanyPlatform.AppHost/UserCompanyPlatform.AppHost.csproj
```

Для запуску API поза Docker після підготовки бази:

```powershell
dotnet run --project NotificationService.API/NotificationService.API.csproj
```

Swagger: http://localhost:5002/swagger. Перевірка MongoDB: http://localhost:5002/health.
appsettings.json налаштовано на порт MongoDB із Compose — 57017.
Для власної MongoDB перевизначте MongoDb__ConnectionString і MongoDb__DatabaseName.
DatabaseSetup видає наявному lab01_notifications_app роль readWrite також для lab04_notifications.
Staging/Production вимагають власний MongoDb__ConnectionString; демодані там вимкнено.

## Ручна перевірка

Збірка, тести й запуск під час внесення змін не виконувалися на прохання користувача.

1. GET `/api/templates` і `/api/notifications`: 200, items та totalCount.
2. POST `/api/templates` із тілом нижче: 201, рядковий id та Location.
   Повторіть запит: 400 від асинхронного валідатора; гонку одночасних запитів захищає unique-індекс із 409.
3. POST `/api/notifications` із тілом нижче: 201. Збережіть id і version.
4. Змініть priority на 10: 400, обробник не запускається, повідомлення Handling для нього немає.
5. GET `/api/notifications/not-an-id`: 400; GET із валідним неіснуючим ObjectId: 404.
6. PUT створеного сповіщення з expectedVersion=1: 204. Повторіть PUT з тією ж версією: 409.
7. DELETE `/api/notifications/{id}?expectedVersion=2`: 204; наступний GET: 404.
8. GET `/api/notifications?page=1&pageSize=1&status=Pending` перевіряє фільтр і пагінацію.
   Параметр search використовує text-індекс; page=0 або pageSize=101 дають 400.
9. Перезапустіть API: seed не дублюється. У Compass перевірте BSON ObjectId, вкладені об'єкти й recentAttempts.
10. Зупиніть MongoDB після старту API: запит повертає 503 після таймауту, /health стає unhealthy.

Створення шаблону:

```json
{
  "code": "manual-welcome",
  "channel": "inApp",
  "body": "Вітаємо нового користувача!"
}
```

Створення сповіщення з seed-шаблоном:

```json
{
  "templateId": "690000000000000000000001",
  "userId": "10000000-0000-0000-0000-000000000001",
  "email": "olena@example.test",
  "displayName": "Олена Коваль",
  "companyId": "20000000-0000-0000-0000-000000000001",
  "companyName": "Contoso",
  "priority": 3,
  "kind": "manual",
  "message": "Вітаємо на платформі!",
  "scheduledAt": "2026-11-24T10:00:00Z"
}
```

Оновлення сповіщення:

```json
{
  "expectedVersion": 1,
  "priority": 4,
  "kind": "manual",
  "message": "Оновлене повідомлення",
  "scheduledAt": "2026-11-24T11:00:00Z"
}
```

GET за ID повертає ETag. Для запису очікувана версія передається явно:
expectedVersion у JSON для PUT або query для DELETE; If-Match не реалізовано.
Пагінація використовує skip/limit; додаткова курсорна пагінація не реалізована.

Перевірка індексів у mongosh:

```javascript
use lab04_notifications
db.notifications.getIndexes()
db.templates.getIndexes()
db.deliveryAttempts.getIndexes()
db.notifications.find()
```

Для здачі зробіть скриншоти Swagger, документа в Compass та списку індексів.
BSON-мапінг: https://www.mongodb.com/docs/drivers/csharp/current/serialization/class-mapping/
