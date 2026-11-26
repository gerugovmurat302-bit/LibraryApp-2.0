# Кейс-задача №6 — качество ПО и автоматизация

Проект: информационная система «Библиотека вуза» (C#, Windows Forms, .NET 8, MySQL).

## Автоматическая сборка и тестирование

В каталоге `.github/workflows/build.yml` расположен GitHub Actions workflow. При push и pull request в ветки main/master он выполняет восстановление пакетов, сборку приложения в Release и автоматические тесты. Тесты проверяют PBKDF2-хеширование и проверку пароля, генерацию соли, политику сложности пароля и поведение пользовательской сессии.

## Локальный запуск тестов

Требуется .NET 8 SDK. Из корня репозитория выполните:

```powershell
dotnet test tests/LibraryApp.Tests/LibraryApp.Tests.csproj --configuration Release
```

Сборка Windows Forms приложения:

```powershell
dotnet build "LibraryApp_V4 Доработка/LibraryApp_V3 Доработка/LibraryApp__V3/LibraryApp_V3/LibraryAppV3/LibraryApp.csproj" --configuration Release
```

## Размещение на GitHub

1. Создайте новый пустой репозиторий GitHub.
2. Распакуйте архив и откройте PowerShell в корне проекта.
3. Выполните `git init`, `git add .`, `git commit -m "Initial LibraryApp quality and CI setup"`.
4. Добавьте адрес своего репозитория: `git remote add origin https://github.com/ВАШ_ЛОГИН/ИМЯ_РЕПОЗИТОРИЯ.git`.
5. Выполните `git branch -M main` и `git push -u origin main`.
6. Откройте вкладку Actions и дождитесь завершения workflow.

Не публикуйте реальные пароли, рабочие дампы БД и персональные данные. Перед публикацией проверьте App.config и замените секреты тестовыми значениями.
