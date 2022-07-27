using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

var botClient = new TelegramBotClient("5455845096:AAG8yz8oaMt0Ef2Dgc7EQO9UbNNLKKAwri0");

using var cts = new CancellationTokenSource();

string DocumentationPath = @"d:\Test_Bot";
string SendMessage = "Оберіть пункт меню";

string CurrentPath = DocumentationPath;
// StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
};
botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    pollingErrorHandler: HandlePollingErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMeAsync();

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

// Send cancellation request to stop bot
cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // Only process Message updates: https://core.telegram.org/bots/api#message
    if (update.Message is not { } message)
        return;
    // Only process text messages
    if (message.Text is not { } messageText)
        return;

    var chatId = message.Chat.Id;

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");
    List<string> Dirs = null;
    if (messageText == "👆 назад")
    {
        CurrentPath = Directory.GetParent(CurrentPath).FullName;
        if (!CurrentPath.Contains(DocumentationPath))
        {
            CurrentPath = DocumentationPath;
        }
        Dirs = ReadCatalog(CurrentPath);
    }
    else
    {
        Dirs = ReadCatalog($"{CurrentPath}\\{messageText}");
        if (Dirs == null)
            try
            {
                SendMessage = System.IO.File.ReadAllText($"{CurrentPath}\\{messageText}");
                Dirs = ReadCatalog(CurrentPath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Dirs = ReadCatalog(DocumentationPath);
                CurrentPath = DocumentationPath;
            }
        else
        {
            SendMessage = "Оберіть пункт меню";
            if (Dirs != null)
            {
                CurrentPath = $"{CurrentPath}\\{messageText}";
            }
        }
    }

    //Створення кнопок
    var dirs = new KeyboardButton[Dirs != null ? Dirs.Count : 0];
    for (int i = 0; i < dirs.Length; i++)
    {
        String[] words = Dirs[i].Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
        dirs[i] = words.Last();
    }

    ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
        {
        dirs,
        new KeyboardButton[]{ "👆 назад" }
    })
    {
        ResizeKeyboard = true
    };
    //string Temp = SendMessage == "" ? SendMessage = "Оберіть пункт меню" : SendMessage = SendMessage+".";
    Message sentMessage = await botClient.SendTextMessageAsync(
        chatId: chatId,
        text: SendMessage,
        replyMarkup: replyKeyboardMarkup,
        cancellationToken: cancellationToken);
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    System.IO.File.AppendAllText($"{DocumentationPath}\\data.log", $"{ErrorMessage}\n");
    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}
List<string> ReadCatalog(string path)
{

    List<string> dirs = null;
    if (Directory.Exists(path))
    {

        dirs = Directory.GetFileSystemEntries(path).ToList();
    }
    return dirs;
}