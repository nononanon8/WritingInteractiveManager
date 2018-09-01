using System;
using System.Threading.Tasks;
using System.Threading;
using WIMCore;

namespace WIMConsole
{
    class Program
    {
        private static Interactive loadedStory = null;

        static void Main(string[] args)
        {
            bool reprintMenu = true;
            while(true)
            {
                if(reprintMenu)
                {
                    Console.WriteLine("**Menu**");
                    Console.WriteLine("1) Login");
                    Console.WriteLine("2) Load Story");
                    Console.WriteLine("3) View Story Info");
                    Console.WriteLine("4) Exit");
                }
                reprintMenu = true;
                Console.Write("Enter action number: ");
                string actionNum = Console.ReadLine();
                switch (actionNum)
                {
                    case "1":
                        Login();
                        break;
                    case "2":
                        LoadStory();
                        break;
                    case "3":
                        ShowStoryInfo();
                        break;
                    case "4":
                        Console.WriteLine("Goodbye!");
                        Thread.Sleep(1000);
                        return;
                    default:
                        Console.WriteLine("Invalid input");
                        reprintMenu = false;
                        break;
                }
            }           
        }
        
        private static void WaitForEnter()
        {
            Console.Write("Press enter to continue");
            Console.ReadLine();
        }

        private static void LoadStory()
        {
            bool haveValidInput = false;
            uint storyId = 0;
            while (!haveValidInput)
            {
                Console.Write("Enter story ID: ");
                string idStr = Console.ReadLine();
                try
                {
                    storyId = uint.Parse(idStr);
                    haveValidInput = true;
                }
                catch
                {
                    Console.WriteLine("Invalid input");
                }
            }
            try
            {
                Task<Interactive> storyTask = Interactive.LoadWebSkeleton(storyId);
                VisualWaitForTask(storyTask, "Downloading story info");
                loadedStory = storyTask.Result;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to download story info: " + e.Message);
                WaitForEnter();
            }
            Console.WriteLine("Info for " + loadedStory.Title + " downloaded");
        }

        private static void Login()
        {
            Console.Write("Username: ");
            string username = Console.ReadLine();
            Console.Write("Password: ");
            string password = Console.ReadLine();
            try
            {
                Task loginTask = WebUtilities.LoginAsync(username, password);
                VisualWaitForTask(loginTask, "Logging in");
                Console.WriteLine("Logged in as " + username);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to login: " + e.Message);
                WaitForEnter();
                return;
            }
        }

        private static void ShowStoryInfo()
        {
            if(loadedStory == null)
            {
                Console.WriteLine("No story loaded");
                return;
            }
            Console.WriteLine(loadedStory.ToString());
            Console.WriteLine("Info & Guidance: " + loadedStory.InfoText);
        }

        private static void VisualWaitForTask(Task task, string message)
        {
            string[] workingDots = new string[3] { ".   ", "..  ", "... " };
            int i = 0;
            while (task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Faulted)
            {
                Console.Write("\r" + message + " " + workingDots[i]);
                if (i < 2)
                    i++;
                else
                    i = 0;
                Thread.Sleep(500);
            }
            Console.WriteLine("\r" + message + " " + workingDots[2]);
            if (task.Status == TaskStatus.Faulted)
                throw task.Exception;
        }
    }
}
