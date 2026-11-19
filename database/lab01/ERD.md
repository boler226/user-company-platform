# ERD лабораторної №1

Діаграми відповідають DDL у `users/01-schema.sql` та `companies/01-schema.sql`.
`o|` означає необов'язковий залежний запис (не більше одного). Аудитні колонки
скорочено; повний перелік і обмеження містяться в SQL.

## Проєкт №1: користувачі та заявки

```mermaid
erDiagram
    users ||--o| user_profiles : "1:1 profile"
    users ||--o{ membership_applications : "1:N applications"
    company_directory ||--o{ membership_applications : "M:N via applications"
    membership_applications ||--o{ application_status_history : "1:N history"
    users {
        uuid id PK
        varchar email UK
        varchar first_name
        varchar last_name
        bigint version
    }
    user_profiles {
        uuid user_id PK,FK
        varchar bio
        varchar phone
    }
    company_directory {
        uuid id PK "CompanyService logical ID"
        varchar name "event-updated copy"
        bigint source_version
    }
    membership_applications {
        uuid id PK
        uuid user_id FK
        uuid company_id FK "local directory only"
        varchar company_name_snapshot
        varchar requested_role
        varchar status
        bigint version
    }
    application_status_history {
        uuid id PK
        uuid application_id FK
        varchar old_status
        varchar new_status
        timestamptz created_at
        text created_by
    }
```

Зв'язок M:N між користувачами й локальним довідником компаній реалізовано через
`membership_applications`, що має власні поля ролі, статусу й знімка назви.
`company_directory.id` логічно відповідає `companies.id` в іншій БД; FK між ними немає.

## Проєкт №2: компанії

```mermaid
erDiagram
    companies ||--o| company_details : "1:1 details"
    companies ||--o{ departments : "1:N departments"
    companies ||--o{ company_tags : "M:N links"
    tags ||--o{ company_tags : "M:N links"
    companies {
        uuid id PK
        varchar name
        varchar email UK
        bigint version
    }
    company_details {
        uuid company_id PK,FK
        varchar address
        varchar website
        int founded_year
    }
    departments {
        uuid id PK
        uuid company_id FK
        varchar name "unique within company"
        numeric budget "greater than zero"
    }
    tags {
        uuid id PK
        varchar name UK
    }
    company_tags {
        uuid company_id PK,FK
        uuid tag_id PK,FK
        timestamptz added_at
    }
```

## MongoDB: агрегат сповіщення

```mermaid
flowchart LR
    T[templates] -->|templateId logical reference| N[notifications]
    N --> E["embedded recipient, company, payload, recentAttempts max 3"]
    A[deliveryAttempts] -->|notificationId logical reference| N
    E -.->|attemptId identifies canonical attempt| A
```

Повна історія доставлення зростає в `deliveryAttempts`. Короткий список останніх
спроб вбудовано в сповіщення для читання без додаткового запиту: це гібридна модель.
