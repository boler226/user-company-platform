# Сервіси платформи

Код попередніх лабораторних залишається у вихідних папках кореня, без перенесення чи копіювання.
Platform.slnx включає всі їхні проєкти.

| Контекст | API | Бізнес-логіка | Доступ до даних | Домен |
|---|---|---|---|---|
| Користувачі та заявки | ../UserService.API | ../UserService.Application/Memberships | ../UserService.Infrastructure/Memberships | ../UserService.Domain |
| Компанії | ../CompanyService.API | ../CompanyService.Application/Catalog | ../CompanyService.Infrastructure | ../CompanyService.Domain |
| Сповіщення | ../NotificationService.API | ../NotificationService.Application | ../NotificationService.Infrastructure | ../NotificationService.Domain |

Назви проєктів попередніх робіт збережено. Нові проєкти мають точні назви Platform.*.
