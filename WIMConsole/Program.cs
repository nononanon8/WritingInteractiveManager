using System;
using System.Threading.Tasks;
using System.Threading;
using WIMCore;
using System.Collections.Generic;

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
                    Console.WriteLine("4) Begin Story");
                    Console.WriteLine("5) Exit");
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
                        BeginStory();
                        break;
                    case "5":
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
        
        private static void BeginStory()
        {
            if(loadedStory == null)
            {
                Console.WriteLine("No story loaded.");
                return;
            }
            int usedRootChapterCount = loadedStory.GetUsedRootChapterCount();

            if(usedRootChapterCount == 0)
            {
                Console.WriteLine("No root chapters founds found.");
                WaitForEnter();
                return;
            }
            else if(usedRootChapterCount > 1)
            {
                Console.WriteLine("Choose a beginning:");
                Dictionary<string, ushort> validChoiceMap = new Dictionary<string, ushort>();
                for(int i = 0; i < loadedStory.RootChapters.Count; i++)
                {
                    if(loadedStory.RootChapters[i] != 0xFFFF)
                    {
                        Console.WriteLine(i + ") " + loadedStory.Chapters[loadedStory.RootChapters[i]].Title);
                        validChoiceMap.Add(i.ToString(), loadedStory.RootChapters[i]);
                    }
                }
                bool inputValid = false;
                while(!inputValid)
                {
                    Console.Write("Enter choice: ");
                    string choice = Console.ReadLine();
                    if (validChoiceMap.ContainsKey(choice))
                    {
                        inputValid = true;
                        ExploreStory(validChoiceMap[choice]);
                    }
                    else
                        Console.WriteLine("Invalid input");
                }
            }
            else
            {
                foreach(ushort rc in loadedStory.RootChapters)
                {
                    if(rc != 0xFFFF)
                    {
                        ExploreStory(rc);
                        return;
                    }
                }
            }
        }

        private static void ExploreStory(ushort chapterIndex)
        {
            while (true)
            {
                Chapter chapter = loadedStory.Chapters[chapterIndex];
                DisplayChapter(chapter);
                Dictionary<string, ushort> chapterChoiceMap = new Dictionary<string, ushort>();
                int i = 0;
                for (i = 0; i < chapter.ChildChapters.Count; i++)
                {
                    if (chapter.ChildChapters[i] != 0xFFFF)
                        chapterChoiceMap.Add(i.ToString(), chapter.ChildChapters[i]);
                }
                if (chapter.ParentChapter != 0xFFFF)
                {
                    Console.WriteLine(i + ") Go back to previous chapter");
                    chapterChoiceMap.Add(i.ToString(), chapter.ParentChapter);
                    i++;
                }
                Console.WriteLine(i + ") Return to menu");
                bool inputValid = false;
                while (!inputValid)
                {
                    Console.Write("Enter choice: ");
                    string choice = Console.ReadLine();
                    if(chapterChoiceMap.ContainsKey(choice))
                    {
                        inputValid = true;
                        chapterIndex = chapterChoiceMap[choice];
                    }
                    else if(choice == i.ToString())
                    {
                        inputValid = true;
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Input invalid");
                    }
                }
            }
            
        }

        private static void DisplayChapter(Chapter chapter)
        {
            Console.WriteLine();
            Console.Write("Choice Path: ");
            foreach (byte c in loadedStory.GetChoicePath(chapter))
                Console.Write(c + " ");
            Console.WriteLine();
            Console.WriteLine("Chapter Title: " + chapter.Title);
            Console.WriteLine("Author: " + chapter.Author);
            Console.WriteLine(chapter.Text);
            Console.WriteLine("You have the following choices:");
            for(int i = 0; i < chapter.ChildChapters.Count; i++)
            {
                if(chapter.ChildChapters[i] != 0xFFFF)
                {
                    Console.Write(i + ") ");
                    if (chapter.ChoiceDescriptions.Count > i && chapter.ChoiceDescriptions[i] != "")
                        Console.WriteLine(chapter.ChoiceDescriptions[i]);
                    else
                        Console.WriteLine(loadedStory.Chapters[chapter.ChildChapters[i]].Title);
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
            Console.WriteLine("Info for " + loadedStory.Title + " downloaded.");
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
                Console.WriteLine("Logged in as " + username + ".");
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
