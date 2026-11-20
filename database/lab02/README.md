# Лабораторна №2: ADO.NET, Dapper і тришарова архітектура

## Розподіл відповідальності

```text
UserService.API/Controllers/MembershipsController
    ↓ IMembershipService
UserService.Application/Memberships/MembershipService
    ↓ IMembershipUnitOfWork
UserService.Infrastructure/Memberships
    ├── DapperMembershipRepository
    ├── CompanyDirectoryRepository
    └── AdoMembershipUserRepository
    ↓ одне NpgsqlConnection + одна NpgsqlTransaction
lab01_users
```

Моделі Membership, MembershipHistory і DirectoryCompany, а також доменні винятки
розташовані в UserService.Domain/Memberships. DTO та профіль AutoMapper — у BLL.
Контролер працює лише з IMembershipService: він не містить SQL, правил переходу
статусів або try/catch для бази. Складення залежностей виконується через DI;
Unit of Work зареєстрований як Scoped.

| Вимога | Реалізація |
|---|---|
| Чистий ADO.NET | AdoMembershipUserRepository: параметри NpgsqlDbType, reader, UPDATE; без Generic Repository |
| Два Dapper-репозиторії | DapperMembershipRepository і CompanyDirectoryRepository |
| Спільний CRUD | IGenericRepository<T>, BaseDapperRepository<T>; конкретні класи задають сталі SQL-команди |
| Multi-mapping | GetWithHistoryAsync: LEFT JOIN заявки та історії, splitOn Id, складання агрегату |
| Процедури ЛР №1 | Створення, редагування, soft delete і зміна статусу через CALL |
| DTO та AutoMapper | MembershipDto, MembershipHistoryDto, MembershipProfile |
| Валідація | DataAnnotations на запитах + перевірки стану в MembershipService |
| Транзакції | MembershipUnitOfWork: одне з'єднання для всіх трьох репозиторіїв |
| Помилки | Перетворення SQLSTATE у DAL, DomainExceptionHandler у API, ProblemDetails |
| Aspire | UserCompanyPlatform.AppHost: PostgreSQL, MongoDB, RabbitMQ, Redis, setup, API та worker |

Спільний CRUD довідника існує для повторного використання DAL, але API не дозволяє
редагувати назви чужих компаній незалежно: вони надходять лише від CompanyService.
Generic Repository отримує тільки SQL-константи розробника, а всі значення користувача
передаються параметрами. Методи DAL не повертають reader, DataTable або JSON-рядки.

## Транзакції та конкурентність

Створення заявки: HTTP-запит до CompanyService → початок транзакції → оновлення
локального довідника → процедура створення заявки та історії → commit.
Помилка процедури відкочує також зміну довідника. HTTP-запит виконується до
початку транзакції, щоб не тримати блокування під час очікування іншого сервісу.

Активація: початок транзакції → процедура Approved → Active і запис історії →
ADO.NET-оновлення users.company_id → commit. Користувач не може одночасно належати
двом різним компаніям. Якщо останній UPDATE не дозволений, статус, версія та
історія заявки відкочуються разом із ним.

Рівень ізоляції — ReadCommitted. Процедура блокує рядок заявки FOR UPDATE і
перевіряє version; прив'язка користувача виконується одним умовним UPDATE.
Це забезпечує потрібні інваріанти без Serializable для всього запиту.
Вищий рівень ізоляції дав би стабільніші повторні читання, але збільшив би
кількість блокувань/serialization failures. Deadlock і serialization failure
повертають 409; автоматичного повторення бізнес-операції немає.

Команди DAL мають timeout 30 секунд, HTTP CompanyService — 10 секунд.
CancellationToken проходить через API/BLL/DAL; rollback використовує
CancellationToken.None, щоб від'єднання клієнта не скасувало прибирання.
DisposeAsync відкочує незавершену транзакцію та звільняє з'єднання.
Npgsql використовує стандартний пул з'єднань.

## Запуск через Aspire

Потрібні .NET 8 SDK, Docker Desktop з Linux containers та доступ до NuGet
для першого відновлення пакетів. AppHost використовує Aspire 9.0.0 із net8.0.
Додатковий глобальний Aspire workload не потрібен: SDK підключений у csproj.
Зупиніть попередній Compose, якщо він займає порти 5000/5001:

```powershell
docker compose stop
dotnet run --project UserCompanyPlatform.AppHost/UserCompanyPlatform.AppHost.csproj
```

AppHost стартує бази та інфраструктуру, запускає C# DatabaseSetup і чекає
його успішного завершення перед API/worker. PostgreSQL та MongoDB використовують
окремі Aspire-томи, а не дані попереднього Compose. Порти інфраструктури
призначаються Aspire й передаються процесам через змінні оточення.
Сервісні SQL/Mongo-логіни створює DatabaseSetup; API не отримують права адміністратора.
Demo-паролі призначені лише для локального навчального запуску.

Відкрийте URL Dashboard з консолі (разом із login token). Очікуваний стан:
database-setup — Exited з кодом 0, бази й сервіси — Running.
Swagger: http://localhost:5000/swagger і http://localhost:5001.
Не запускайте Compose та AppHost одночасно на тих самих API-портах.
SMTP, як і в ЛР №1, потрібно налаштувати окремо для реального надсилання.

Для звичайного Docker-запуску також залишається `docker compose up --build -d`.
AppHost додано в solution, але не включено в публікацію Docker-образів сервісів.

## Контракти API

| Метод | Маршрут | Успіх |
|---|---|---|
| POST | /api/memberships | 201 + Location + створена заявка |
| GET | /api/memberships/{id} | 200, заявка з history |
| GET | /api/memberships?userId={id}&page=1 | 200, до 20 записів |
| PUT | /api/memberships/{id} | 204, редагування ролі Submitted-заявки |
| POST | /api/memberships/{id}/status | 204, зміна статусу |
| DELETE | /api/memberships/{id}?version={version} | 204, soft delete Cancelled-заявки |

400 — неправильний DTO/невідомий статус; 404 — відсутній запис;
409 — застаріла версія, дубль або недопустима бізнес-операція.
503 — недоступний CompanyService; 500 — непередбачена помилка з безпечним описом.
Помилки використовують ProblemDetails з traceId, без SQL чи внутрішніх подробиць.
Приклади нижче — для PowerShell; замініть USER_ID та APPLICATION_ID реальними UUID.

```powershell
curl.exe -i http://localhost:5000/api/memberships/ffffffff-ffff-ffff-ffff-ffffffffffff

$body = '{"userId":"USER_ID","companyId":"20000000-0000-0000-0000-000000000001","role":"Developer"}'
Invoke-RestMethod http://localhost:5000/api/memberships -Method Post -ContentType application/json -Body $body

Invoke-RestMethod http://localhost:5000/api/memberships/APPLICATION_ID

$change = '{"status":"AwaitingValidation","version":1}'
Invoke-RestMethod http://localhost:5000/api/memberships/APPLICATION_ID/status -Method Post -ContentType application/json -Body $change
```

Зареєструйте окремого користувача через наявний Users/Register/register або
візьміть seed-користувача без інших заявок. Перевірте роль `O'Brien`: вона має
зберегтися без SQL-помилки. Після кожного оновлення читайте актуальну version.
Повторіть успішний запит зі старою version — очікується 409.

## Демонстрація rollback без штучних помилок у коді

1. Для одного користувача створіть дві заявки: до Contoso (`...0001`)
   і Fabrikam (`...0002`), використовуючи повні UUID компаній із seed ЛР №1.
2. Першу переведіть Submitted → AwaitingValidation → Approved → Active.
3. Другу переведіть до Approved. Через GET збережіть version та history.
4. Спробуйте перевести другу в Active з актуальною version. Отримаєте 409:
   користувач уже належить першій компанії.
5. Повторіть GET другої заявки: статус залишається Approved, version та history
   не змінилися. Це відкат процедури після помилки в іншому репозиторії.
6. Повторна активація вже активної першої заявки теж повертає 409 ProblemDetails.
7. Для перевірки DELETE створіть іншу заявку, скасуйте її та видаліть з актуальною
   version. Наступний GET має повернути 404.