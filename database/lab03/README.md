# Лабораторна №3 — EF Core у CompanyService

## Відповідність 14 критеріям

Шляхи нижче наведені від кореня проєкту.

| № | Критерій | Реалізація |
|---|---|---|
| 1 | Інкапсуляція домену | `CompanyService.Domain/Entities`: приватні setter-и, перевірки в конструкторах, `Rename`, `Archive`, `ChangeAddress`, `ChangeBudget`. |
| 2 | Fluent API | `CompanyService.Infrastructure/Configurations`: окремий конфіг для кожної сутності; `ApplyConfigurationsFromAssembly` у контексті; довжини, унікальність, CHECK, `HasPrecision(18, 2)`. PostgreSQL показує цей тип як `numeric(18,2)`. |
| 3 | Зв'язки | Company–CompanyDetails 1:1; Company–Department 1:N; Company–Tag M:N через CompanyTag з ключем `(CompanyId, TagId)` та `AddedAt`. |
| 4 | Міграція | `CompanyService.Infrastructure/Migrations/Lab03`: InitialCreate, designer і snapshot; `database/lab03/initial-create.sql`. |
| 5 | Repository та UoW | `GenericRepository<T>` працює через DbSet; Add/Get асинхронні; Update/Delete лише змінюють стан. Збереження бізнес-операції — один `UnitOfWork.SaveChangesAsync`. |
| 6 | Три спеціальні репозиторії | CompanyRepository: Include/ThenInclude; DepartmentRepository: Entry.Collection.LoadAsync; TagRepository: LINQ через CompanyTag з AddedAt. |
| 7 | BLL, DTO, AutoMapper | `CompanyService.Application/Catalog/CompanyCatalogService.cs`, DTO та `Mappings/CompanyMappingProfile.cs`. BLL використовує UoW, перевіряє email, назву підрозділу, повторне призначення тегу та версію. |
| 8 | API і статуси | CompaniesController та CompanyStructureController: async, 200/201 з Location/204; ProblemDetails 400/404/409; Swagger на порту 5001. DbContext у контролерах відсутній. |
| 9 | Сідінг | TagConfiguration.HasData з фіксованими ID; DbInitializer після MigrateAsync перевіряє кожен запис перед додаванням. Демодані можна вимкнути через SEED_DEMO_DATA=false. |
| 10 | SQL-пагінація | CompanySearchSpecification: фільтр назви/тегу, дозволене сортування, Skip/Take до ToListAsync; окремий CountAsync. PagedResult містить totalCount, totalPages, hasNext, hasPrevious. |
| 11 | Валідація | DataAnnotations у DTO плюс три окремі FluentValidation-валідатори. AddValidatorsFromAssembly і явний ValidateAndThrowAsync у BLL, без AddFluentValidation. Обидва шляхи повертають ValidationProblemDetails. |
| 12 | Помилки | CompanyExceptionHandler: NotFound → 404, Validation → 400, Conflict/Domain → 409, решта → 500. Detail лише в Development; обробник не дублює лог винятку middleware. |
| 13 | Serilog | Program.cs: Console sink, Enrich.FromLogContext, UseSerilogRequestLogging, TraceId; appsettings.json вмикає SQL-категорію EF Core на Information. |
| 14 | Specification | CompanySearchSpecification і CompanyEmailSpecification не залежать від EF. Обидві застосовуються через один WithSpecification у репозиторії. |

## Запуск та міграції

З кореня проєкту:

```powershell
docker compose up --build -d
docker compose logs database-setup
```

Або запустіть Aspire, попередньо зупинивши Compose, щоб звільнити порти:

```powershell
dotnet run --project UserCompanyPlatform.AppHost/UserCompanyPlatform.AppHost.csproj
```

DatabaseSetup створює `lab03_companies`, викликає `MigrateAsync`, програмний сідінг
та видає права ролі `lab01_companies_app`. API стартує після завершення setup.
При повторному запуску застосовані міграції й наявні демозаписи пропускаються.
У PostgreSQL можна переглянути таблицю `__EFMigrationsHistory`.

`lab01_companies` та її дані не видаляються і не переносяться автоматично.
Старі SQL-файли ЛР №1 залишені для попередньої роботи; DatabaseSetup більше
не застосовує їх до бази компаній. UserService та MongoDB зберігають попередні бази.
Створені вручну в ЛР №1 компанії потрібно відтворити у новій базі, якщо вони потрібні для заявок.

Для середовища без демоданих задайте `SEED_DEMO_DATA=false` у `.env` Compose
або змінну середовища процесу DatabaseSetup. Довідник тегів з HasData залишається:
він є частиною міграції. Налаштування стосується демоданих компаній ЛР №3.

Для окремого локального запуску DatabaseSetup потрібні PostgreSQL і MongoDB;
налаштування POSTGRES_ADMIN / MONGO_ADMIN_URI описані в кореневому README.
API не застосовує міграції самостійно, оскільки його роль не має DDL-прав.
SQL-файл міграції наведено для перегляду й ручного застосування адміністратором;
він не містить програмних демоданих і видачі прав, які виконує DatabaseSetup.

## Ручна перевірка через Swagger

Swagger: http://localhost:5001. Для прикладів companyId Contoso:
`20000000-0000-0000-0000-000000000001`.

1. `GET /api/companies?page=1&pageSize=1&sortBy=name&sortDir=asc` — 200,
   один елемент; на чистих демоданих totalCount=2, totalPages=2, hasNext=true.
   Друга сторінка має totalCount=2, hasPrevious=true. Лічильник враховує фільтри,
   але не залежить від розміру сторінки.
2. Додайте `search=Cont` або `tagId=50000000-0000-0000-0000-000000000002`.
   Дозволені sortBy: `name`, `createdAt`; sortDir: `asc`, `desc`.
   Пошук назви чутливий до регістру. pageSize від 1 до 100, page від 1 до 100000.
3. `GET /api/companies/{companyId}` — eager loading: компанія, реквізити,
   підрозділи й теги завантажуються одним SQL-запитом з JOIN.
4. `GET /api/company-structure/{companyId}/departments` — explicit loading:
   SQL для компанії з реквізитами, потім окремий SQL для підрозділів.
5. `GET /api/company-structure/{companyId}/tags` — проєкція M:N:
   companyId, companyName, tagId, tagName та addedAt.
6. `POST /api/companies` з тілом нижче — 201. Перейдіть за Location — має бути 200.
   Повторіть POST з тим самим email — 409.

```json
{
  "name": "Demo Company",
  "email": "demo@company.example",
  "address": "Київ, вул. Прикладна, 10"
}
```

7. У `PUT /api/companies/{id}` передайте ті самі поля та актуальне `version`
   з GET — 204. Повторіть зі старою версією — 409.
8. POST з name="" — 400 з `errors.Name`. Для перевірки саме FluentValidation
   використайте GET з `sortBy=unknown` — 400 з `errors.SortBy`.
   `pageSize=101` та від'ємний бюджет теж мають повертати 400.
9. GET з невідомим GUID — 404 ProblemDetails. DELETE створеної демокомпанії — 204,
   наступний GET — 404. Видалення м'яке, email архівованої компанії залишається зайнятим.
10. `POST /api/company-structure/{companyId}/departments`:
    `{"name":"Support","budget":1500.50}` — 201 з Location конкретного підрозділу.
    Повторне ім'я в цій компанії — 409; budget=1.234 — 400.
11. Для Fabrikam (`20000000-0000-0000-0000-000000000002`) виконайте
    `PUT /api/company-structure/{companyId}/tags/50000000-0000-0000-0000-000000000002`:
    204, повтор — 409. GET тегів має містити addedAt нового зв'язку.
12. Повторно запустіть setup через `docker compose run --rm database-setup`.
    Кількість компаній, підрозділів і зв'язків не повинна збільшитися.
