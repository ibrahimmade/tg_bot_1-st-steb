# 🤖 Telegram Bot for Task

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![MySQL](https://img.shields.io/badge/MySQL-4479A1?style=for-the-badge&logo=mysql&logoColor=white)
![Telegram](https://img.shields.io/badge/Telegram-26A5E4?style=for-the-badge&logo=telegram&logoColor=white)
![GitHub](https://img.shields.io/badge/GitHub-100000?style=for-the-badge&logo=github&logoColor=white)

![Status](https://img.shields.io/badge/status-active-success.svg)

Телеграм бот для управления задачами с интеграцией MySQL базы данных.

## ✨ Возможности
- ✅ Регистрация пользователей
- 📋 Добавление новых задач
- 📊 Просмотр списка задач
- ✔️ Отметка задач как выполненных
- ⚙️ Удвление задач
- 🔍 Информация о пользователе

## 🚀 Команды бота

|Команда|Описание|Пример|
|-------|--------|------|
|`/start`	|Регистрация пользователя и приветствие|	`/start`|
|`/me`	|Информация о пользователе|	`/me`|
|`/add {текст}`	|Добавить новую задачу|	`/add Купить молоко`|
|`/list`	|Показать все задачи|	`/list`|
|`/done {id}`	|Отметить задачу как выполненную|	`/done 1`|
|`/help`|	Вывести информационное сообщение|	`/help `|

## 📍Кнопки "Удаление" и "Выполнение" задач
## Есть кнопки для ⬇️
|Описание|команды|кнопка|
|-------|--------|------| 
|Удаление|  `/delete`| или по кнопке  Удалить🗑️|
|Выполненные| `/done`  | или по кнопке  Выполнить✅|



## 🗄️ Структура базы данных
Таблица `users`
```sql
tg\_user\_id      BIGINT PRIMARY KEY   -- Telegram ID пользователя
tg\_username      VARCHAR(255)         -- Username пользователя
time\_registered  DATETIME             -- Дата регистрации
```
Таблица `tasks`
```sql
id                INT AUTO\_INCREMENT PRIMARY KEY       -- ID задачи
tg\_user\_id      BIGINT                                -- Внешний ключ к users
task\_description TEXT                                  -- Описание задачи
time\_created     DATETIME                              -- Время создания
status            VARCHAR(50) DEFAULT 'надо выполнить'  -- Статус задачи

```

## 🛠️ Технологии
- .NET - платформа разработки
- Telegram.Bot - библиотека для Telegram Bot API
- MySQL - база данных
- MySql.Data - MySQL коннектор для .NET
- DotNetEnv - загрузка переменных окружения

# 📦 Установка и запуск
### 1. Клонирование репозитория
```bash
git clone https://github.com/ibrahimmade/tg_bot_1-st-steb
cd tg_bot_1-st-steb
```
### 2. Установка зависимостей
```bash
dotnet add package Telegram.Bot
dotnet add package MySql.Data
dotnet add package DotNetEnv
```
### 3. Настройка окружения
Создайте файл `.env` в корне проекта:
```env
API\_KEY=your\_telegram\_bot\_token
DB\_HOST=localhost
DB\_USER=your\_mysql\_user
DB\_NAME=your\_database\_name
```
### 4. Запуск бота
```bash 
dotnet run
