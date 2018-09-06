using System;
using System.Threading.Tasks;
using System.Threading;
using WIMCore;
using System.Collections.Generic;
using System.IO;

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
                    Console.WriteLine("5) Download All Chapters");
                    Console.WriteLine("6) Save Story to File");
                    Console.WriteLine("7) Load Story from File");
                    Console.WriteLine("8) Exit");
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
                        DownloadAllChapters();
                        break;
                    case "6":
                        SaveStory();
                        break;
                    case "7":
                        LoadLocalStory();
                        break;
                    case "8":
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
                WaitForEnter();
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

        private static void SaveStory()
        {
            if(loadedStory == null)
            {
                Console.WriteLine("No story loaded.");
                WaitForEnter();
                return;
            }
            Console.Write("Enter file name: ");
            string filename = Console.ReadLine() + ".wia";
            try
            {
                using (FileStream fStream = new FileStream(filename, FileMode.Create))
                {
                    loadedStory.Write(fStream);
                    fStream.Flush();
                    Console.WriteLine("Successfully wrote story to file " + filename);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Unable to write file " + filename + ": " + e.Message);
            }
        }

        private static void LoadLocalStory()
        {
            Console.Write("Enter filename: ");
            string filename = Console.ReadLine();
            filename += ".wia";
            try
            {
                using (FileStream fStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    loadedStory = Interactive.LoadLocal(fStream);
                }
                Console.WriteLine("Successfully loaded story " + loadedStory.Title + " from file " + filename);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to load story from file " + filename + ": " + e.Message);
            }
        }

        private static void ExploreStory(ushort chapterIndex)
        {
            // Called from BeginStory().
            while (true)
            {
                Chapter chapter = loadedStory.Chapters[chapterIndex];
                DisplayChapter(chapter);
                Dictionary<string, ushort> chapterChoiceMap = new Dictionary<string, ushort>();
                for (int i = 0; i < chapter.ChildChapters.Count; i++)
                {
                    if (chapter.ChildChapters[i] != 0xFFFF)
                        chapterChoiceMap.Add(i.ToString(), chapter.ChildChapters[i]);
                }
                string[] extraChoices;
                if (chapter.ParentChapter != 0xFFFF)
                {
                    Console.WriteLine("a) Go back to previous chapter");
                    chapterChoiceMap.Add("a", chapter.ParentChapter);
                    extraChoices = new string[] { "b", "c", "d" };
                }
                else
                    extraChoices = new string[] { "a", "b", "c" };
                Console.WriteLine(extraChoices[0] + ") Download chapter data");
                Console.WriteLine(extraChoices[1] + ") Download entire branch");
                Console.WriteLine(extraChoices[2] + ") Return to menu");
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
                    else if(choice == extraChoices[0])
                    {
                        inputValid = true;
                        DownloadChapter(chapterIndex);
                    }
                    else if(choice == extraChoices[1])
                    {
                        inputValid = true;
                        DownloadStoryBranch(chapterIndex);
                    }
                    else if(choice == extraChoices[2])
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
            // Called from ExploreStory().
            Console.WriteLine();
            Console.Write("Choice Path: ");
            foreach (byte c in loadedStory.GetChoicePath(chapter))
                Console.Write(c);
            Console.WriteLine();
            Console.WriteLine("Chapter Title: " + chapter.Title);
            Console.WriteLine("Author: " + chapter.Author);
            Console.WriteLine(chapter.Text);
            Console.WriteLine(Environment.NewLine + "You have the following choices:");
            for(int i = 0; i < chapter.ChildChapters.Count; i++)
            {
                if(chapter.ChildChapters[i] != 0xFFFF)
                {
                    Console.Write(i + ") ");
                    if (chapter.ChoiceDescriptions.Count > i && chapter.ChoiceDescriptions[i] != "")
                        Console.Write(chapter.ChoiceDescriptions[i]);
                    else
                        Console.Write(loadedStory.Chapters[chapter.ChildChapters[i]].Title);
                    Console.WriteLine(" (" + loadedStory.GetBranchSize(chapter.ChildChapters[i]) + ")");
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
                return;
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
                WaitForEnter();
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

        private static void DownloadChapter(ushort chapterIndex)
        {
            List<ushort> indices = new List<ushort>();
            indices.Add(chapterIndex);
            DownloadChapters(indices, true);
        }

        private static void DownloadAllChapters()
        {
            if(loadedStory == null)
            {
                Console.WriteLine("No story loaded.");
                WaitForEnter();
                return;
            }
            List<ushort> allChapters = new List<ushort>();
            while (allChapters.Count < loadedStory.Chapters.Count)
                allChapters.Add((ushort)allChapters.Count);
            DownloadChapters(allChapters);
        }

        private static void DownloadStoryBranch(ushort rootChapterIndex)
        {
            List<ushort> branchChapterIndices = loadedStory.GetSubBranchChapters(rootChapterIndex);
            DownloadChapters(branchChapterIndices);
        }

        private static void DownloadChapters(List<ushort> chapterIndices, bool forceRedownload = false)
        {
            string[] workingDots = new string[3] { ".   ", "..  ", "... " };
            if (!forceRedownload)
            {
                for(int j = 0; j < chapterIndices.Count; j++)
                {
                    if(loadedStory.Chapters[chapterIndices[j]].Text != "")
                    {
                        chapterIndices.RemoveAt(j);
                        j--;
                    }
                }
            }
            int i = 0;
            Console.WriteLine("Press any key to cancel.");
            int chapterIdxIdx = 0;
            int successCount = 0;
            int failureCount = 0;
            List<string> failMessages = new List<string>();
            while (chapterIdxIdx < chapterIndices.Count && !Console.KeyAvailable)
            {
                Task dlTask = loadedStory.DownloadChapterData(chapterIndices[chapterIdxIdx]);
                while (dlTask.Status != TaskStatus.RanToCompletion && dlTask.Status != TaskStatus.Faulted && !Console.KeyAvailable)
                {
                    Console.Write("\r" + "Downloading chapters (attempt " + Chapter.DownloadAttempts + ") ");
                    Console.Write("(" + successCount + "/" + chapterIndices.Count + " completed) ");
                    Console.Write("(" + failureCount + " failures) " + workingDots[i]);
                    if (i < 2)
                        i++;
                    else
                        i = 0;
                    Thread.Sleep(500);
                }
                if (dlTask.Status == TaskStatus.RanToCompletion)
                    successCount++;
                else if (dlTask.Status == TaskStatus.Faulted)
                {
                    failureCount++;
                    string message = dlTask.Exception.Message;
                    if (!failMessages.Contains(message))
                        failMessages.Add(message);
                }
                else if (Console.KeyAvailable)
                    Chapter.CancelDownloads = true;

                chapterIdxIdx++;
            }
            Console.Write("\r" + "Downloading chapters (attempt " + Chapter.DownloadAttempts + ") ");
            Console.Write("(" + successCount + "/" + chapterIndices.Count + " completed) ");
            Console.WriteLine("(" + failureCount + " failures) " + workingDots[2]);
            if (Console.KeyAvailable)
            {
                Console.WriteLine("Canceled");
                while (Console.KeyAvailable)
                    Console.ReadKey(false);
            }
            if(failureCount > 0)
            {
                Console.WriteLine("Failure messages: ");
                foreach (string msg in failMessages)
                    Console.WriteLine(msg);
            }
            WaitForEnter();
        }
    }
}
