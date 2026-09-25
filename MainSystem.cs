using Microsoft.VisualBasic.ApplicationServices;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Media;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ARSTConfig;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace atcmdHost
{
    internal class MainSystem
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0X0002;
        private const uint MOUSEEVENTF_LEFTUP = 0X0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0X0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0X0010;

        public static string help = "/start - запуск бота и инициализация соединения;\n/autoScreenShot - включение/выключение автоскриншота после ввода команды;\n/version - вывод версии системы;\n/help - вывод справки системных команд;\n/makeScreenShot - создание скриншота рабочего стола хоста;\n/msctrl - запуск клавиатуры удалённого управления мышью на хосте;\n/start=*путь к файлу или имя ссылки*>*аргументы командной строки* - запускает программу по пути или ссылке;\n/scr=*имя нового скрипта* - быстрое создание .bat скрипта, записанного в вводной строке(строки разделяются символом ';');\n/getf=*путь к файлу* - скачивает файл с хоста и отправляет в чат;\n/setMouseK=*новое число коэфицента* - изменение текущего коэффицента изменения позиции курсора(число пикселей, прибавляемое или отнимаемое от текущего положение курсора для его перемещения по экрану);\n/exit - выход из текущего сеанса;\n/stop - искусственный вызов System panic для полной остановки бота.";

        public static int mouseRepositionK = 15;
        public static bool initialized = false, isAutoScreenEnabled = false, isEthernetConnectionAvailable = false, enterLoginMode = false, userInitialized = false;
        public static string initializedToken = "", remoteMousePath = "", ver = "0.2.9", targetInitialDirectory = @"", loginedUsrs = "", permissionCode = "", usrPass = "", usrLogin = "", userName = "", sysRoot = "";
        public static long TargetUserID = 0, expectID = 0;

        static TelegramBotClient telegramBotClient;

        public static System.Windows.Forms.Timer errorTimeoutTimer = new System.Windows.Forms.Timer();

        public static void init(string token)
        {
            try
            {
                errorTimeoutTimer.Interval = 100;
                errorTimeoutTimer.Tick += ErrorTimeoutTimer_Tick;

                telegramBotClient = new TelegramBotClient(token);
                initializedToken = token;
                initialized = true;
            }
            catch(Exception ex)
            {
                throwErr(ex.Message, "CONNECTION_INIT_ERROR!", true);
            }
        }

        public static bool isNetworkAvailable()
        {
            try { return NetworkInterface.GetIsNetworkAvailable(); }
            catch { return false; }
        }

        public static void addToBotConsole(string text)
        {
            sendMessageToBot(text, TargetUserID);
        }

        public static void controlOnlineUsers(bool removeThisUser, long id)
        {
            //loginedUsrs = File.ReadAllText("LoginedUsers.txt", Encoding.UTF8);
            if (removeThisUser)
            {
                string[] lgndusrsarray = loginedUsrs.Split(",");
                for(int i = 0; i < lgndusrsarray.Length; i++)
                {
                    if (lgndusrsarray[i] == id.ToString())
                    {
                        lgndusrsarray[i] = lgndusrsarray[i].Replace(id.ToString(), "");
                        break;
                    }
                }

                loginedUsrs = "";
                for (int i = 0; i < lgndusrsarray.Length; i++)
                {
                    if (i > lgndusrsarray.Length) loginedUsrs += lgndusrsarray[i] + ",";
                    else loginedUsrs += lgndusrsarray[i];
                }
            }
            else
            {
                if (loginedUsrs.Length > 0) loginedUsrs += id.ToString() + ",";
                else loginedUsrs += id.ToString();
            }

            File.WriteAllText("LoginedUsers.txt", loginedUsrs);
        }

        public async static void sendMessageToBot(string message, long userID)
        {
            try
            {
                await telegramBotClient.SendMessage(userID, message, ParseMode.None);
            }
            catch(Exception ex)
            {
                throwErr(ex.Message, "SEND_MESSAGE_FAILURE!", false);
            }
        }

        public static bool checkToBanCommand(string commandString)
        {
            if (permissionCode != "ADMIN")
            {
                string[] usrs = File.ReadAllLines("BanedCommands.txt", Encoding.UTF8);

                if (permissionCode == "ROOT")
                {
                    string[] commands = usrs[0].Replace("ROOT=", "").Split(",");
                    for (int i = 0; i < commands.Length; i++)
                    {
                        //addToBotConsole("Check: " + commands[i]);
                        if (commandString.Contains(commands[i])) return true;
                    }
                }
                else if (permissionCode == "USR")
                {
                    string[] commands = usrs[1].Replace("USR=", "").Split(",");
                    for (int i = 0; i < commands.Length; i++)
                    {
                        if (commandString.Contains(commands[i])) return true;
                    }
                }
            }

            return false;
        }


        public static void sendToUsrChatMessage(string text)
        {
            if (TargetUserID != 0)
            {
                if (text.Contains("/mtu=")) addToBotConsole("Message from LOCAL SERVER: " + text.Replace("/mtu=", ""));
                else msgrecivied(text);
            }
            else MessageBox.Show("Нет текущего пользователя.", "START_ON_HOST_COMMAND_ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        public static async void sendImage(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                var inputFile = new InputFileStream(stream);
                await telegramBotClient.SendPhoto(TargetUserID, inputFile);
            }
        }

        public static void login(long userID, string usrName)
        {
            try
            {
                string path = sysRoot + $@"\Users\{userID.ToString()}.txt";
                if (File.Exists(path))
                {
                    /*string[] usrcfg = File.ReadAllLines(path, Encoding.UTF8);

                    permissionCode = usrcfg[0].Replace("permissionCode=", "");
                    userName = usrcfg[1].Replace("userName=", "");
                    usrPass = usrcfg[2].Replace("password=", "");
                    usrLogin = usrcfg[3].Replace("login=", "");
                    targetInitialDirectory = usrcfg[4].Replace("initialTargetDirectory=", "");*/
                    aConfg userConfig = new aConfg();
                    userConfig.init(path);
                    permissionCode = userConfig.read("permissionCode");
                    userName = userConfig.read("userName");
                    usrPass = userConfig.read("password");
                    usrLogin = userConfig.read("login");
                    targetInitialDirectory = userConfig.read("initialTargetDirectory");
                    isAutoScreenEnabled = Convert.ToBoolean(userConfig.read("autoScreenEnabled"));

                    if (usrPass != "" && usrLogin != "")
                    {
                        if(!loginCorrect)
                        {
                            enterLoginMode = true;
                            addToBotConsole("Введите логин: ");
                        }
                    }
                    else
                    {
                        usrIsLogined = true;
                        welcome();
                    }
                }
                else
                {
                    //throw new Exception("Конфигурация пользователя отсутствует в директории конфигурации пользователей.");
                    addToBotConsole("Creating USR account ...");
                    permissionCode = "USR";
                    userName = usrName;
                    targetInitialDirectory = sysRoot + @"\Users\UsersDirectoryes\" + userID;
                    Directory.CreateDirectory(targetInitialDirectory);
                    usrIsLogined = true;
                    welcome();
                }
            }
            catch(Exception ex)
            {
                throwErr(ex.Message, "USER_LOGIN_FAILED!", false);
            }
        }

        public static void msgInit(string message, long UserID, string username)
        {
            if (UserID == TargetUserID || TargetUserID == 0)
            {
                TargetUserID = UserID;

                if (message == "/start") started = true;

                if (started)
                {
                    loginedUsrs = File.ReadAllText("LoginedUsers.txt", Encoding.UTF8);

                    if (loginedUsrs != "")
                    {
                        string[] logined = loginedUsrs.Split(",");
                        for (int i = 0; i < logined.Length; i++) if (TargetUserID.ToString() == logined[i]) usrIsLogined = true;
                    }

                    if (usrIsLogined) msgrecivied(message);
                    else
                    {
                        if (enterLoginMode)
                        {
                            if (!loginCorrect)
                            {
                                if (message == usrLogin)
                                {
                                    message = "";
                                    addToBotConsole("Логин верный!");
                                    loginCorrect = true;
                                    addToBotConsole("Введите пароль: ");
                                }
                                else addToBotConsole("Логин не верный!\n\nПопробуйте ещё раз.");
                            }
                            if (loginCorrect)
                            {
                                if (message.Length > 1)
                                {
                                    if (message == usrPass)
                                    {
                                        message = "";
                                        addToBotConsole("Пароль верный!");
                                        enterLoginMode = false;
                                        welcome();
                                    }
                                    else addToBotConsole("Пароль не верный!\n\nПопробуйте ещё раз.");
                                }
                            }
                        }
                        else login(UserID, username);
                    }
                }
            }
            else sendMessageToBot("SYSTEM_ACCESS_EXCEPTION: Невозможно получить доступ к хосту, пока он занят.", UserID);
        }

        public static void welcome()
        {
            Thread.Sleep(100);
            addToBotConsole("--- Connection setup ---");
            Thread.Sleep(150);
            addToBotConsole("Preparing protocol host ...");
            controlOnlineUsers(false, TargetUserID);
            addToBotConsole("Preparing system console ...");
            addToBotConsole($"User data:\n\nUser permission: {permissionCode}\nUser name: {userName}\nTarget user ID: {TargetUserID}\n\nTarget initial directory: '{targetInitialDirectory}'\n\n\nInitialized!");
            addToBotConsole("Введите /help для справки.");
            msgrecivied("");
        }

        public static async void createNewBotKeyboard(bool visible, string text, string[] buttonsNamesArray)
        {
            try
            {
                var keyboard = new ReplyKeyboardMarkup { };
                if (visible)
                {
                    if (buttonsNamesArray.Length > 0 && buttonsNamesArray[0] != "")
                    {
                        for (int i = 0; i < buttonsNamesArray.Length; i++) keyboard.Keyboard = keyboard.Keyboard.Append(new[] { new KeyboardButton(buttonsNamesArray[i]) });
                    }
                    else throw new Exception("Массив имён кнопок новой клавиатуры не указан.");
                }
                await telegramBotClient.SendMessage(TargetUserID, text, replyMarkup: keyboard);
            }
            catch (Exception ex)
            {
                throwErr(ex.Message, "INTERNAL_BOT_KEYBOARD_EXCEPTION!", false);
            }
        }

        public static bool loginCorrect = false, started = false, usrIsLogined = false;
        public static void messageRecivied(string message, long UserID, string userName)
        {
            if (expectID != 0)
            {
                if (expectID == UserID) msgInit(message, expectID, userName);
                else started = false;
            }
            else msgInit(message, UserID, userName);
        }

        public static void makeScreenShot()
        {
            addToBotConsole("Creating screenshot ...");
            sendImage(CaptureScreenshot());
            addToBotConsole("All done!");
        }

        public static bool msctrlIsActive = false;
        public static int currentCurosPositionX = 0, currentCurosPositionY = 0;
        public static async void msgrecivied(string message)
        {
            if (message != "")
            {
                try
                {
                    addToBotConsole("--- Enterpretating target command ---");

                    if(checkToBanCommand(message))
                    {
                        throwErr($"У вас нет прав на вызов данной процедуры! [permissionCode={permissionCode}, message={message}]", "PROCESS_PERMISSION_REQUIRED!", false);
                        return;
                    }

                    string[] comand = new string[message.Split(" ").Length];
                    string controlComand = "";

                    string[] controlComandsArray = { "/help", "/start", "/stop", "/exit", "/autoScreenShot", "/makeScreenShot", "/mts=", "/msctrl", "/version", "Курсор влево", "Курсор вправо", "Курсор вниз", "Курсор вверх", "Правая кнопка мыши", "Сделать скриншот", "Левая кнопка мыши", "Выход" };

                    for (int i = 0; i < controlComandsArray.Length; i++)
                    {
                        if (message.Contains(controlComandsArray[i]))
                        {
                            controlComand = message;
                            break;
                        }
                        else comand = message.Split(" ");
                    }

                    if (initialized)
                    {
                        switch (controlComand)
                        {
                            case "/help":
                                addToBotConsole($"Справка контрольных команд бота:\n{help}");
                                break;
                            case "/stop":
                                throwErr("No data", "HOST_ARTIFICAL_STOP!", true);
                                break;
                            case "/msctrl":
                                msctrlIsActive = true;
                                remoteMousePath = File.ReadAllLines("sysCfg.txt", Encoding.UTF8)[0].Replace("remoteMouseSymbolProgram=", "");
                                if(remoteMousePath != "") startProcess(remoteMousePath, "", false, true);
                                createNewBotKeyboard(true, "Удалённое управление курсором активно!", new string[] { "Курсор вправо", "Курсор влево", "Курсор вниз", "Курсор вверх", "Правая кнопка мыши", "Левая кнопка мыши", "Сделать скриншот", "Выход" });
                                break;
                            case "/exit":
                                addToBotConsole("Exit of system ...");
                                /*if (permissionCode == "USR")
                                {
                                    Directory.Delete(targetInitialDirectory);
                                }*/
                                started = false;
                                userInitialized = false;
                                usrIsLogined = false;
                                controlOnlineUsers(true, TargetUserID);
                                //TargetUserID = 0;
                                //Application.Exit();
                                break;
                            case "/makeScreenShot":
                                makeScreenShot();
                                break;
                            case "/autoScreenShot":
                                if(!isAutoScreenEnabled)
                                {
                                    isAutoScreenEnabled = true;
                                    addToBotConsole("Автоскриншот включён.");
                                }
                                else
                                {
                                    isAutoScreenEnabled = false;
                                    addToBotConsole("Автоскриншот выключен.");
                                }

                                if (permissionCode != "USR")
                                {
                                    aConfg userConfig = new aConfg();
                                    userConfig.init(sysRoot + $@"\Users\{TargetUserID.ToString()}.txt");
                                    userConfig.write("autoScreenEnabled", isAutoScreenEnabled.ToString());
                                }
                                break;
                            case "/version":
                                addToBotConsole($"ATelegram-Console\nSystem version: {ver}\n\nВведите /help для справки контрольных команд.\n\n\nSystem by ARST mechanic studio Inc, (C) 2025.\nAnd Kriping");
                                break;
                            default:
                                if (msctrlIsActive && (controlComand.Contains("Сделать скриншот") || controlComand.Contains("Курсор влево") || controlComand.Contains("Курсор вправо") || controlComand.Contains("Курсор вниз") || controlComand.Contains("Курсор вверх") || controlComand.Contains("Правая кнопка мыши") || controlComand.Contains("Левая кнопка мыши") || controlComand.Contains("Выход")) && (permissionCode == "ADMIN" || permissionCode == "ROOT"))
                                {
                                    resetCurrenCorsorPosition();

                                    int k = mouseRepositionK;

                                    try
                                    {
                                        switch (controlComand)
                                        {
                                            case "Выход":
                                                createNewBotKeyboard(false, " ", new string[] { " " });
                                                Process.Start("taskkill.exe", "/im DesktopSolver.exe /f");
                                                msctrlIsActive = false;
                                                addToBotConsole("Выход из режима удалённого управления курсором выполнен успешно!");
                                                break;
                                            case "Курсор вниз":
                                                currentCurosPositionY = currentCurosPositionY + k;
                                                cursorControl(currentCurosPositionX, currentCurosPositionY, "");
                                                break;
                                            case "Курсор вверх":
                                                currentCurosPositionY = currentCurosPositionY - k;
                                                cursorControl(currentCurosPositionX, currentCurosPositionY, "");
                                                break;
                                            case "Курсор влево":
                                                currentCurosPositionX = currentCurosPositionX - k;
                                                cursorControl(currentCurosPositionX, currentCurosPositionY, "");
                                                break;
                                            case "Сделать скриншот":
                                                makeScreenShot();
                                                break;
                                            case "Курсор вправо":
                                                currentCurosPositionX = currentCurosPositionX + k;
                                                cursorControl(currentCurosPositionX, currentCurosPositionY, "");
                                                break;
                                            case "Правая кнопка мыши":
                                                cursorControl(currentCurosPositionX, currentCurosPositionY, "right");
                                                break;
                                            case "Левая кнопка мыши":
                                                cursorControl(currentCurosPositionX, currentCurosPositionY, "left");
                                                break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        throwErr(ex.Message, "MOUSE_CONTROL_EXCEPION!", false);
                                    }
                                }
                                else
                                {
                                    if (comand[0].Contains("/scr="))
                                    {
                                        addToBotConsole("Creating .bat script ...");
                                        string s1 = message.Replace("/scr=", "");
                                        addToBotConsole("Parsing string: " + s1);
                                        string[] scr = s1.Split(";");

                                        File.WriteAllLines("new_bat_scr.bat", scr);
                                        addToBotConsole("Starting script ...");
                                        startProcess("cmd.exe", "/c new_bat_scr.bat", false, true);
                                    }
                                    else if (comand[0].Contains("/setMouseK="))
                                    {
                                        addToBotConsole("Прошлый коэффицент репозиции курсора: " + mouseRepositionK);
                                        mouseRepositionK = Int32.Parse(message.Replace("/setMouseK=", ""));
                                        addToBotConsole("Новый коэффицент репозиции курсора: " + mouseRepositionK);
                                    }
                                    else if (comand[0].Contains("/mts="))
                                    {
                                        addToBotConsole("Отправка сообщения на сервер...");
                                        if (MessageBox.Show(message.Replace("/mts=", ""), $"Message from: {TargetUserID}({userName})", MessageBoxButtons.OK, MessageBoxIcon.Information) == DialogResult.OK) addToBotConsole("Пользователь прочел ваше сообщение!");
                                    }
                                    else if (comand[0].Contains("/getf="))
                                    {
                                        string mainRootForFiles = "", pathToFile = "";

                                        string[] files = message.Replace("/getf=", "").Split(';');

                                        if (files.Length > 0) mainRootForFiles = Path.GetDirectoryName(files[0]);

                                        for (int i = 0; i < files.Length; i++)
                                        {
                                            if (files[i] == Path.GetFileName(files[i])) pathToFile = mainRootForFiles + @"\" + files[i];
                                            else pathToFile = files[i];

                                            using var stream = System.IO.File.OpenRead(pathToFile);
                                            addToBotConsole("Downloading file: " + pathToFile);
                                            var fileName = Path.GetFileName(pathToFile);
                                            addToBotConsole("Please wait.");

                                            await telegramBotClient.SendDocument(chatId: TargetUserID, document: new InputFileStream(stream, fileName), caption: "Downloaded file: " + pathToFile);
                                        }

                                        addToBotConsole("Done!");
                                    }
                                    else if (comand[0].Contains("/start="))
                                    {
                                        string[] startData = message.Replace("/start=", "").Split('>');
                                        addToBotConsole("Starting program on server: " + startData[0]);

                                        string arg = "";
                                        for (int i = 1; i < startData.Length; i++) arg = arg + " " + startData[i];

                                        addToBotConsole($"Check for link[data={startData[0]}] ...");

                                        if (File.Exists(sysRoot + @"\Users\ProgramLinks\" + startData[0] + ".txt"))
                                        {
                                            string path = File.ReadAllText(sysRoot + @"\Users\ProgramLinks\" + startData[0] + ".txt", Encoding.UTF8).Replace("path=", "");
                                            addToBotConsole($"Link for '{startData[0]}' exists!\n\nStarting process with file '{Path.GetFileName(path)}' and arguments '{arg}'");
                                            startProcess(path, arg, false, false);
                                        }
                                        else
                                        {
                                            addToBotConsole($"Link not found\n\nStarting process with file '{Path.GetFileName(startData[0])}' and arguments '{arg}'");
                                            startProcess(startData[0], arg, false, false);
                                        }
                                    }
                                    else if (comand[0].Contains("/kenter="))
                                    {
                                        string text = message.Replace("/kenter=", "");
                                        resetCurrenCorsorPosition();

                                        if (text.Contains("del:"))
                                        {
                                            int count = Convert.ToInt32(text.Replace("del:", ""));
                                            addToBotConsole("Delating elements: " + count);
                                            for (int i = 0; i < count; i++) SendKeys.SendWait("{BACKSPACE}");
                                        }
                                        else
                                        {
                                            string[] addSteps = text.Split(";");
                                            addToBotConsole("All keyboard commands: " + text);

                                            for (int i = 0; i < addSteps.Length; i++)
                                            {
                                                string data = addSteps[i];
                                                addToBotConsole("Entering elements: " + data);
                                                SendKeys.SendWait(data);
                                                Thread.Sleep(200);
                                                if (!data.EndsWith("}")) SendKeys.SendWait("{ENTER}");
                                            }
                                        }
                                    }
                                    else
                                    {
                                        addToBotConsole("Starting command on server: " + message);

                                        string[] args = new string[comand.Length];
                                        string arg = "";
                                        int k = 1;

                                        if (controlComand != "") k = 2;

                                        for (int i = 0; i < comand.Length - k; i++) args[i] = comand[i + k];
                                        for (int i = 0; i < args.Length; i++) arg = arg + " " + args[i];

                                        startProcess(comand[0], arg, false, true);
                                    }

                                    if (isAutoScreenEnabled)
                                    {
                                        addToBotConsole("Ожидание перед созданием скриншота...");
                                        Thread.Sleep(600);
                                        makeScreenShot();
                                    }
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throwErr(ex.Message, "RECIVIED_MESSAGE_ENTERPRETATING_FAILURE!", false);
                }
            }
        }

        public static string CaptureScreenshot()
        {
            string filePath = Path.Combine(sysRoot + @"\localtemp\", $"screenshot_{TargetUserID}.jpg");
            using (Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(0, 0, 0, 0, bitmap.Size);
                }
                bitmap.Save(filePath, ImageFormat.Jpeg);
            }

            return filePath;
        }

        public static void resetCurrenCorsorPosition()
        {
            currentCurosPositionX = Cursor.Position.X;
            currentCurosPositionY = Cursor.Position.Y;

            addToBotConsole($"Текущая позиция курсора на экране:\n\nX={currentCurosPositionX}\nY={currentCurosPositionY}");
        }

        public static void cursorControl(int xPos, int yPos, string buttonNameToClick) // контроль курсора
        {
            Cursor.Position = new Point(xPos, yPos);

            switch (buttonNameToClick)
            {
                case "right":
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                    mouse_event(MOUSEEVENTF_RIGHTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                    break;
                case "left":
                    mouse_event(MOUSEEVENTF_LEFTDOWN, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, Cursor.Position.X, Cursor.Position.Y, 0, 0);
                    break;
            }

            resetCurrenCorsorPosition();
        }

        public static async void startProcess(string processName, string arg, bool isSudo, bool isReadToEndData) // универсальный запуск процессов
        {
            try
            {
                Process process = new Process();

                process.StartInfo.FileName = processName;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(processName);
                process.StartInfo.Arguments = arg;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.StandardErrorEncoding = Encoding.ASCII;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();

                if (isReadToEndData)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit();

                    int chunkSize = 4000;
                    string part = "";
                    for (int i = 0; i < output.Length; i += chunkSize) part = output.Substring(i, Math.Min(chunkSize, output.Length - i));

                    if (!File.Exists(processName)) addToBotConsole($"Data from {processName}: {part}");
                    else addToBotConsole($"Data from {Path.GetFileName(processName)}: {part}");
                }
                else addToBotConsole("Wait for end data chanceled for: " + Path.GetFileName(processName));
            }
            catch(Exception ex)
            {
                throwErr(ex.Message, "START_PROCESS_ERROR!", false);
            }
        }

        public static void SystemClose()
        {
            try
            {
                errorTimeoutTimer.Stop();
                Process.Start("taskkill", "/im atcmdHost.exe /f");
            }
            catch(Exception ex)
            {
                messageBox(ex.Message, "STOP_SYSTEM_ERROR");
            }
        }

        // Обработка системных ошибок

        public static void messageBox(string text, string code)
        {
            MessageBox.Show(text + "\n\n\nНажмите ОК для продолжения. . .", code, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static int errorTimeout = 0;
        private static void ErrorTimeoutTimer_Tick(object? sender, EventArgs e)
        {
            errorTimeout++;

            if (errorTimeout > 16)
            {
                errorTimeout = 0;
                SystemClose();
            }
        }

        public static void throwErr(string text, string code, bool isCritic)
        {
            if(isCritic)
            {
                controlOnlineUsers(true, TargetUserID);

                if (initialized && TargetUserID != 0)
                {
                    addToBotConsole($"--- System panic! ---");
                    addToBotConsole($"Internal critic exception detected.\n\n\nException code: {code}\n\nException text: {text}");
                    addToBotConsole("Bot server host process stoped.");
                }
                else
                {
                    errorTimeoutTimer.Start();
                    messageBox(text, code);
                }

                SystemClose();
            }
            else
            {
                addToBotConsole($"--- Exception detected ---");
                if (code == "") addToBotConsole($"Exception text: {text}");
                else addToBotConsole($"Exception code: {code}\n\nException text: {text}");
            }
        }
    }
}