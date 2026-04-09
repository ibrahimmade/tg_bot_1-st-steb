using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using DotNetEnv;                
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using Org.BouncyCastle.Asn1.Nist;
using Sprache;
using Telegram.Bot;             
using Telegram.Bot.Polling;     
using Telegram.Bot.Types;       
using Telegram.Bot.Types.Enums; 
using Telegram.Bot.Types.ReplyMarkups; 


Env.Load();


var _userState = new Dictionary<long, string>();


string token = Env.GetString("API_KEY");
string host = Env.GetString("DB_HOST");
string user = Env.GetString("DB_USER");
string password = Env.GetString("DB_PASSWORD");
string database = Env.GetString("DB_NAME");


MySqlConnectionStringBuilder builder = new()
{
    Server = host,
    Database = database,
    UserID = user
};


string connectionString = builder.ConnectionString;

string createTableQuery = @"CREATE TABLE IF NOT EXISTS users (
    tg_user_id BIGINT PRIMARY KEY,           
    tg_username VARCHAR(255),                
    time_registered DATETIME                 
);";


using var connectionForUsers = new MySqlConnection(connectionString);
connectionForUsers.Open(); 

using var createUsersCommand = new MySqlCommand(createTableQuery, connectionForUsers);
createUsersCommand.ExecuteNonQuery();
Console.WriteLine(" Таблица users проверена/создана");


string createTableQuery2 = @"CREATE TABLE IF NOT EXISTS tasks(
    id INT AUTO_INCREMENT PRIMARY KEY,       
    tg_user_id BIGINT,                       
    task_description TEXT,                   
    time_created DATETIME,                   
    status VARCHAR(50) DEFAULT 'надо выполнить', 

    FOREIGN KEY (tg_user_id) REFERENCES users(tg_user_id) ON DELETE CASCADE
);";

using var connectionForTasks = new MySqlConnection(connectionString);
connectionForTasks.Open();
using var createTasksCommand = new MySqlCommand(createTableQuery2, connectionForTasks);
createTasksCommand.ExecuteNonQuery();
Console.WriteLine(" Таблица tasks проверена/создана");




MySqlConnection GetConnection() 
{
    return new MySqlConnection(connectionString);
}


string seeUserInfo(long tgUserId)
{
    try
    {
       
        string selectQuery = "SELECT tg_user_id, tg_username, time_registered FROM users WHERE tg_user_id = @UserId";
        
       
        using var connection = GetConnection();
        connection.Open();
        
       
        using var command = new MySqlCommand(selectQuery, connection);
        command.Parameters.AddWithValue("@UserId", tgUserId);
        
       
        using var reader = command.ExecuteReader();
        
       
        if (reader.Read())
        {
            
            string username = reader["tg_username"].ToString();
            long userId = Convert.ToInt64(reader["tg_user_id"]);
            DateTime timeRegistered = Convert.ToDateTime(reader["time_registered"]);
            
            Console.WriteLine($"❕Информация о пользователе: {username} (ID: {userId})");
            
            return $"ℹ️Пользователь: {username}\n ID: {userId}\n Зарегистрирован: {timeRegistered:dd.MM.yyyy HH:mm}";
        }
        else
        {
            Console.WriteLine($" Пользователь с ID {tgUserId} не найден.");
            return "Пользователь не найден";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка получения информации: {ex.Message}");
        return $"Ошибка: {ex.Message}";
    }
}

string AddNewUser(long tgUserId, string tgUsername)
{
    try
    {
       
        string userInfo = seeUserInfo(tgUserId);
        if (userInfo != "Пользователь не найден")
        {
            Console.WriteLine($"‼️Пользователь уже существует: {tgUsername} (ID: {tgUserId})");
            return " Вы уже зарегистрированы в системе!";
        }
        
        
        string insertQuery = "INSERT INTO users (tg_user_id, tg_username, time_registered) VALUES (@UserId, @Username, @TimeRegistered)";
        
        
        using var connection = GetConnection();
        connection.Open();
        
        using var command = new MySqlCommand(insertQuery, connection);

        command.Parameters.AddWithValue("@UserId", tgUserId);

        command.Parameters.AddWithValue("@Username", tgUsername ?? "unknown");
        command.Parameters.AddWithValue("@TimeRegistered", DateTime.UtcNow);
        

        int rowsAffected = command.ExecuteNonQuery();
        

        if (rowsAffected > 0)
        {
            Console.WriteLine($"Новый пользователь добавлен: {tgUsername} (ID: {tgUserId})");
            return "Вы успешно зарегистрированы! Теперь вы можете создавать задачи.";
        }
        else
        {
            return " Ошибка при добавлении пользователя";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка добавления пользователя: {ex.Message}");
        return $" Ошибка: {ex.Message}";
    }
}


string AddTask(long tgUserId, string taskDescription)
{
    try
    {
        
        if (string.IsNullOrWhiteSpace(taskDescription))
        {
            return "‼️Описание задачи не может быть пустым!";
        }
        
        
        string userInfo = seeUserInfo(tgUserId);
        if (userInfo == "Пользователь не найден")
        {
            return " Сначала зарегистрируйтесь через команду /start";
        }
        
        
        if (taskDescription.Length > 500)
        {
            return " ‼️Описание задачи слишком длинное (максимум 500 символов)";
        }
        
        
        string insertQuery = "INSERT INTO tasks (tg_user_id, task_description, time_created) VALUES (@UserId, @TaskDescription, @TimeCreated)";
        
        using var connection = GetConnection();
        connection.Open();
        
        using var command = new MySqlCommand(insertQuery, connection);
        command.Parameters.AddWithValue("@UserId", tgUserId);
        command.Parameters.AddWithValue("@TaskDescription", taskDescription);
        command.Parameters.AddWithValue("@TimeCreated", DateTime.UtcNow);
        
        int rowsAffected = command.ExecuteNonQuery();
        
        if (rowsAffected > 0)
        {
            Console.WriteLine($"Новая задача добавлена для пользователя ID: {tgUserId}");
            return $"✅Задача успешно добавлена!\n Описание: {taskDescription}";
        }
        else
        {
            return " ❌Ошибка при добавлении задачи";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка добавления задачи: {ex.Message}");
        return $" Ошибка: {ex.Message}";
    }
}


string UserTasks(long tgUserID)
{
    try
    {
       
        string selectQuery = "SELECT id, task_description, time_created, status FROM tasks WHERE tg_user_id = @UserId ORDER BY id DESC";
        
        using var connection = GetConnection();
        connection.Open();
        
        using var command = new MySqlCommand(selectQuery, connection);
        command.Parameters.AddWithValue("@UserId", tgUserID);
        
        using var reader = command.ExecuteReader();
        
       
        if (!reader.HasRows)
        {
            Console.WriteLine($"Нет задач для пользователя ID: {tgUserID}");
            return " ‼️У вас пока нет задач. Создайте первую через `/add`";
            
        }
        
       
        var tasksList = new List<string>();
        int e=0,r=0;
       
        while (reader.Read())
        {
       
            string taskId = reader["id"].ToString();
            string description = reader["task_description"].ToString();
            DateTime timeCreated = Convert.ToDateTime(reader["time_created"]);
            string status = reader["status"].ToString();
            
       
            string statusEmoji = status == "выполнено" ? " ID  " : " ID";
            
            if(status != "выполнено")
            {
                if(e==0){tasksList.Add("\n ‼️Надо выполнить: \n");e=1;}
                tasksList.Add($"{statusEmoji} **#{taskId}**:\n {description}\n  \n Создано: {timeCreated:dd.MM.yyyy HH:mm}\n");
            }
            else 
            {
                if(r==0){tasksList.Add("\n✅Выполненные\n");r=1;}
                tasksList.Add($"{statusEmoji} **#{taskId}**:\n {description}\n   \nСоздано: {timeCreated:dd.MM.yyyy HH:mm}\n");
            }
        }
        
        Console.WriteLine($"Получены задачи для пользователя ID: {tgUserID}");
        
        
        string result = $"**Ваши задачи:**\n\n{string.Join("\n\n", tasksList)}";
        
        
        if (result.Length > 4000)
        {
            return " У вас слишком много задач! Пожалуйста, выполните часть из них командой /done";
        }
        
        return result;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка получения задач: {ex.Message}");
        return $"Ошибка: {ex.Message}";
    }
}

{
    

}


string ChangeTaskStatus(long tgUserId, int taskId)
{
    try
    {
        string updateQuery = "UPDATE tasks SET status = 'выполнено' WHERE id = @TaskId AND tg_user_id = @UserId AND status != 'выполнено'";
        
        using var connection = GetConnection();
        connection.Open();
        
        using var command = new MySqlCommand(updateQuery, connection);
        command.Parameters.AddWithValue("@TaskId", taskId);
        command.Parameters.AddWithValue("@UserId", tgUserId);
        
        int rowsAffected = command.ExecuteNonQuery();
        
        if (rowsAffected > 0)
        {
            Console.WriteLine($" Статус задачи ID: {taskId} для пользователя ID: {tgUserId} изменен на 'выполнено'");
            return $" ✅Задача #{taskId} отмечена как выполненная! ";
        }
        else
        {
            return $" ❌Ошибка ,Не удалось отметить задачу #{taskId}. Проверьте:\n" +
                   "• Существует ли задача с таким ID\n" +
                   "• Не выполнена ли она уже\n" +
                   "• Ваша ли это задача";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка изменения статуса задачи: {ex.Message}");
        return $"Ошибка: {ex.Message}";
    }
}


   string StatsTask(long tgUserID)
{
    
    try
    {
        using var connection = GetConnection();
        connection.Open();

        string sts = @"SELECT 
        (SELECT COUNT(*) FROM tasks WHERE tg_user_id = @UserId) AS Vse,
        (SELECT COUNT(*) FROM tasks WHERE tg_user_id = @UserId AND STATUS = 'выполнено') AS neVse,
        (SELECT COUNT(*) FROM tasks WHERE tg_user_id = @UserId AND STATUS !='выполнено') AS neVse2;
        ";

        using var command = new MySqlCommand(sts, connection);
        command.Parameters.AddWithValue("@UserId", tgUserID);

        using var reader = command.ExecuteReader();

        if (reader.Read())
        {
            int Vse = Convert.ToInt32(reader["Vse"]);
            int neVse = Convert.ToInt32(reader["neVse"]);
            int neVse2 = Convert.ToInt32(reader["neVse2"]);
                        
            if (Vse != 0)
            {
                return $"ℹ️Статистика \n Все {Vse}, \n Bыполненные {neVse}, \n Не Выполненные {neVse2}";
            }
            else return $"ℹ️У вас нет задач для статистики";
        }
        else 
        {
            return "No Data";
        }
    }
    catch (Exception ex)
    {
        return $"Eror {ex.Message}";
    }
}


    string DeleteTasks(long tgUserID, int taskId)
{
    try{
    using var connection = GetConnection();
    connection.Open();

    string del = "DELETE FROM tasks WHERE id = @taskId AND tg_user_id  = @UserId"; 

    using var deleteCommand = new MySqlCommand(del, connection);
        deleteCommand.Parameters.AddWithValue("@TaskId", taskId);
        deleteCommand.Parameters.AddWithValue("@UserId", tgUserID);
        
        int rowsAffected = deleteCommand.ExecuteNonQuery();
        
        if (rowsAffected > 0)
        {
            Console.WriteLine($"Задача #{taskId} удалена для пользователя ID: {tgUserID}");
            return $" ✅Задача #{taskId} успешно удалена!";
        }
        else
        {
            return $" ❌Ошибка ,Не удалось удалить задачу #{taskId}";
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($" Ошибка удаления задачи: {ex.Message}");
        return $" Ошибка: {ex.Message}";
    }
    
    
}

InlineKeyboardMarkup CreateTaskKeyboard()
{
    return new InlineKeyboardMarkup( new[]
    {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Удалить 🗑️", "CallBack"),
            InlineKeyboardButton.WithCallbackData("Выполнить✅", "CallBack2")
        }
    });
}

async Task<InlineKeyboardMarkup?> Buttfrdell(long tgUserID, string ? status = null)
{
    
 try
    {
        using var connection = GetConnection();
        connection.Open();

        string sts = @"SELECT id FROM tasks WHERE tg_user_id = @UserId";
        if(!string.IsNullOrEmpty(status))
        {
            sts += $" AND status = @Status";
        }
        using var command = new MySqlCommand(sts, connection);
        command.Parameters.AddWithValue("@UserId", tgUserID);
        command.Parameters.AddWithValue("@Status", status);

        using var reader = command.ExecuteReader();
        List<string > taskIds = new List<string>();
        while (reader.Read())
        {
            taskIds.Add(reader["id"].ToString());
        }
        if(taskIds.Count == 0)
        {
            return null;
        }
        var buttons = new List<List<InlineKeyboardButton>>();
        for(int i=0; i< taskIds.Count; i+=2)
        {
            var row = new List<InlineKeyboardButton>();
            
            row.Add(InlineKeyboardButton.WithCallbackData($"ID {taskIds[i]}", taskIds[i].ToString()));
            if(i + 1 < taskIds.Count)
            {
                row.Add(InlineKeyboardButton.WithCallbackData($"ID {taskIds[i+1]}", taskIds[i+1].ToString()));
            }
            buttons.Add(row);
        }
        return new InlineKeyboardMarkup(buttons);

    }
    catch (Exception ex)
    {
        Console.WriteLine($" Ошибка создания клавиатуры: {ex.Message}");
        return null;
    }




}



//-------------------------------    tgbot





async Task HandleUpdateAsync(ITelegramBotClient bot, Telegram.Bot.Types.Update update, CancellationToken ct)
{
       
  

if(update.CallbackQuery is {} CallbackQuery)
    {
        
        
        long chadId = CallbackQuery.Message.Chat.Id;
            string Data = CallbackQuery.Data;
        if(CallbackQuery.Data == "CallBack")
        {
            _userState[CallbackQuery.From.Id] = "1";
            await bot.SendMessage(chadId, " ❕Выберите ID задачи для удаления: ",
            replyMarkup: await Buttfrdell(CallbackQuery.From.Id), cancellationToken: ct);
            return;
        }
        else if(CallbackQuery.Data == "CallBack2")
        {
            _userState[CallbackQuery.From.Id]= "2";
            string strts= "надо выполнить";
            await bot.SendMessage(chadId, " ❕Выберите ID задачи для отметки как выполненной: ",
            replyMarkup: await Buttfrdell(CallbackQuery.From.Id, strts), cancellationToken: ct);
            return;
        }
        

        if(int.TryParse(Data, out int taskId))
        {
            if(_userState.TryGetValue(CallbackQuery.From.Id, out string ? state) && state == "1"){
            string result = DeleteTasks(CallbackQuery.From.Id, taskId);
            await bot.SendMessage(CallbackQuery.Message!.Chat.Id, result, cancellationToken: ct);
            _userState.Remove(CallbackQuery.From.Id);
            }

            else if(_userState.TryGetValue(CallbackQuery.From.Id, out string ? state2) && state2 == "2")
            {
                string result2 = ChangeTaskStatus(CallbackQuery.From.Id, taskId);
                await bot.SendMessage(CallbackQuery.Message!.Chat.Id, result2, cancellationToken: ct);
                _userState.Remove(CallbackQuery.From.Id);
            }
            else {
                await bot.SendMessage(CallbackQuery.Message!.Chat.Id, 
                " ❌Неизвестное действие. Пожалуйста, используйте команды  | Удалить | или | Выполнено | для взаимодействия с задачами.", cancellationToken: ct);
            }
        }
            return;
    }
    



        
      if (update.Message is not { } message) return;
       if (message.Text is not { } messageText) return;
        


 
    var chatId = message.Chat.Id;           
    var userId = message.From.Id;           
    var username = message.From.Username ?? message.From.FirstName;
    
    Console.WriteLine($" {username}: {messageText}");
            

    string command2;
    if(messageText.StartsWith("/add") || messageText.StartsWith("/done") || messageText.StartsWith("/delete")){
     command2 = messageText.Split(' ')[0].ToLower();
    }
    else {command2 = messageText.ToLower();}

    switch (command2)
    {
        case "/start":
           
            await bot.SendMessage(
                chatId: chatId,
                text: " **Добро пожаловать в бот-менеджер задач!**\n\n" +
                      "Я помогу вам организовать свои дела.\n\n" +
                      "**Доступные команды:**\n\n" +
                      "▪ /me - Мой профиль\n\n"+
                      "▪ `/add ` - Добавить задачу\n\n"+
                      "▪ /list - Список задач\n\n" +
                      "▪ `/done` <ID> - Отметить задачу выполненной\n\n" +
                      "▪ `/delete` - Удалить задачу\n\n" +
                      "▪ /stats - Показать statistiku\n\n"+
                      "▪ /help - Показать это сообщение",
                      parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
            
            AddNewUser(userId, username);
            break;
        
        case "/help":
            await bot.SendMessage(
                chatId: chatId,
                text: " ** ℹ️Справка по командам:**\n\n" +
                      "- `/add Купить молоко` - создаст новую задачу\n" +
                      "- `/list` - покажет все ваши задачи с ID\n" +
                      "- `/done 1` - отметит задачу с ID 1 как выполненную\n" +
                      "- `/me` - покажет информацию о вас\n\n" +
                      " **Совет:** ID задачи можно узнать через `/list`",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct
            );
            break;
        
        case "/me":
            string userInfo = seeUserInfo(userId);
            await bot.SendMessage(chatId, userInfo, cancellationToken: ct);
            break;
        
        case "/add":
            
            
            string taskDescription = messageText.Length > 4 ? messageText.Substring(4).Trim() : "";
            
            if (string.IsNullOrEmpty(taskDescription))
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "** ❌Ошибка:** Вы не указали описание задачи\n\n" +
                          "**Пример использования:**\n`/add Купить продукты`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            else
            {
                string result = AddTask(userId, taskDescription);
                await bot.SendMessage(chatId, result, cancellationToken: ct);
            }
            break;
        
        case "/list":

            string tasks = UserTasks(userId);

            if(tasks.Contains("Ваши")){
            var kb = CreateTaskKeyboard();
            await bot.SendMessage(chatId, tasks, parseMode: ParseMode.Markdown, replyMarkup:kb, cancellationToken: ct);
            }
            else {await bot.SendMessage(chatId, tasks, parseMode: ParseMode.Markdown, cancellationToken: ct);}
            break;
        
        case "/done":
            
            string taskIdStr = messageText.Length > 5 ? messageText.Substring(5).Trim() : "";
            
            if (int.TryParse(taskIdStr, out int taskId))
            {
                string result = ChangeTaskStatus(userId, taskId);
                await bot.SendMessage(chatId, result, cancellationToken: ct);
            }
            else
            {
                await bot.SendMessage(
                    chatId: chatId,
                    text: "** ❌Ошибка:** Укажите корректный ID задачи\n\n" +
                          "**Пример:** `/done 1`\n" +
                          "Узнать ID можно через `/list`",
                    parseMode: ParseMode.Markdown,
                    cancellationToken: ct
                );
            }
            break;

       case "/delete":
    
    string deleteIdStr = messageText.Length > 7 ? messageText.Substring(7).Trim() : "";
    
    if (int.TryParse(deleteIdStr, out int taskIdForDelete))
    {
        
        string result = DeleteTasks(userId, taskIdForDelete);
        
        
        Console.WriteLine($" Результат удаления для пользователя {userId}: {result}");
        await bot.SendMessage(chatId, result, cancellationToken: ct);
    }
    else
    {
        await bot.SendMessage(chatId, 
            " ❌Ошибка: Укажите корректный ID задачи!\n\n" +
            "Пример: `/delete 1`\n" +
            "Узнать ID можно через команду `/list`",
            parseMode: ParseMode.Markdown,
            cancellationToken: ct);
    }
    break;

    case "/stats":
    string answer = StatsTask(message.From.Id);
    await bot.SendMessage(message.Chat.Id, answer, cancellationToken: ct);
       break;
        
        default:
            
            await bot.SendMessage(
                chatId: chatId,
                text: " ❌ Я не понимаю эту команду.\n" +
                      "Используйте `/help` для просмотра всех команд.",
                parseMode: ParseMode.Markdown,
                cancellationToken: ct   
            );
            break;
    }
}


Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
{
   
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"Ошибка бота: {exception.Message}");
    Console.ResetColor();
    
    
    return Task.CompletedTask;
}


async Task Main()
{
    var botClient = new TelegramBotClient(token);
    
    var receiverOptions = new ReceiverOptions 
    { 
        AllowedUpdates = Array.Empty<UpdateType>() 
    };
    
 
    using CancellationTokenSource cts = new();
    
 
    botClient.StartReceiving(
        updateHandler: HandleUpdateAsync,      
        errorHandler: HandleErrorAsync,        
        receiverOptions: receiverOptions,      
        cancellationToken: cts.Token           
    );
    
    Console.WriteLine("Бот успешно запущен!");
    Console.WriteLine("Нажмите Enter для остановки...");
    
    
    Console.ReadLine();
    
    Console.WriteLine("Останавливаем бота...");
    cts.Cancel();
    
    
    await Task.Delay(1000);
    Console.WriteLine(" Бот остановлен. До свидания!");
}

await Main();