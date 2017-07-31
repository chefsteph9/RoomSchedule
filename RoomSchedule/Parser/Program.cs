using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace Parser
{
    class Program
    {
        private class Parser
        {
            #region Members

            private int m_totalCount = 0;
            private string m_semester = "F17";

            #endregion

            #region Constructors

            public Parser() {  }

            public Parser(string semester)
            {
                m_totalCount = 0;
                m_semester = semester;
            }

            #endregion

            #region Properties

            public string Semester
            {
                get
                {
                    return m_semester;
                }
                set
                {
                    m_semester = value;
                }
            }

            #endregion

            #region Methods

            public int DoTheWork()
            {
                m_totalCount = 0;
                // Get schedule page url
                string urlBase = $"https://www.southern.edu/apps/CourseSchedule/default.aspx?Term=";
                string url = urlBase + m_semester;

                // Get class schedule html
                HtmlWeb webClient = new HtmlWeb();
                HtmlDocument classesHomePage = webClient.Load(url);

                // Get 2nd table, which contains links to all pages, to get number of pages
                var tables = classesHomePage.DocumentNode.Descendants("table").ToArray();
                if (tables.Length > 1)
                {
                    var pageLinksTable = classesHomePage.DocumentNode.Descendants("table").ElementAt(1);

                    foreach (var page in pageLinksTable.FirstChild.ChildNodes)
                    {
                        string pageUrl = url + "&page=" + page.FirstChild.InnerHtml;
                        HtmlDocument classPage = webClient.Load(pageUrl);

                        parsePageForClasses(pageUrl);
                    }
                }

                return m_totalCount;
            }


            public void parsePageForClasses(string url)
            {
                HtmlWeb webClient = new HtmlWeb();
                HtmlDocument doc = webClient.Load(url);

                var classesTable = doc.DocumentNode.Descendants("table").ElementAt(0);

                using (var dbcontext = new RoomScheduleContext())
                {
                    TimeSpan begin;
                    TimeSpan end;
                    string days;
                    string building;
                    string room;
                    string professor;
                    string section;
                    string title;
                    int syn;

                    List<string> alsoMeetsNotes = new List<string>();

                    int rows = 0;
                    int validRows = 0;
                    foreach (var tableRow in classesTable.ChildNodes)
                    {
                        rows++;
                        bool alsoMeets = false;
                        if (tableRow.ChildNodes.Count == 13 && tableRow.ChildNodes.ElementAt(1).Name != "th")
                        {
                            validRows++;
                            HtmlNode[] tableCells = tableRow.ChildNodes.ToArray();

                            string tempSyn = tableCells[1].Descendants("span").FirstOrDefault().InnerText;
                            int.TryParse(tempSyn, out syn);

                            section = tableCells[2].Descendants("div").FirstOrDefault().InnerText;

                            title = tableCells[3].Descendants("a").FirstOrDefault().InnerText;

                            if (tableCells[3].Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("note")).ToList().Count > 0)
                            {
                                alsoMeetsNotes = tableCells[3].Descendants("div").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("note")).FirstOrDefault().Descendants("span").FirstOrDefault().InnerText.Split(new char[] { '\n', '\r' }).ToList();
                                alsoMeetsNotes = alsoMeetsNotes.Where(note => note.ToLower().StartsWith("lecture") || note.ToLower().StartsWith("laboratory")).ToList();
                                alsoMeets = alsoMeetsNotes.Count > 0 ? true : false;
                            }

                            professor = tableCells[5].Descendants("span").FirstOrDefault().InnerText;

                            string place = tableCells[6].Descendants("span").FirstOrDefault().InnerText;
                            string[] buildingAndRoom = GetBuildingAndRoom(place);
                            building = buildingAndRoom[0];
                            room = buildingAndRoom.Length > 1 ? buildingAndRoom[1] : "";

                            days = tableCells[7].InnerText.Trim();

                            string beginTemp = tableCells[8].InnerText.Trim();
                            begin = beginTemp == "" ? new TimeSpan(0) : DateTime.Parse(beginTemp).TimeOfDay;

                            string endTemp = tableCells[9].InnerText.Trim();
                            end = endTemp == "" ? new TimeSpan(0) : DateTime.Parse(endTemp).TimeOfDay;

                            Class newClass = new Class()
                            {
                                Syn = syn,
                                Section = section,
                                Title = title,
                                Professor = professor,
                                Building = building,
                                Room = room,
                                Begin = begin,
                                End = end,
                                Days = days,
                                Semester = m_semester
                            };
                            dbcontext.Class.Add(newClass);

                            if (alsoMeets)
                                AddAlsoMeets(Class.Clone(newClass), alsoMeetsNotes);
                        }
                    }
                    try
                    {
                        m_totalCount += dbcontext.SaveChanges();
                    }

                    catch(Exception e)
                    {
                        Console.WriteLine("Error saving page of classes to database: " + e.Message);
                    }
                }
            }

            public void AddAlsoMeets(Class theClass, List<string> notes)
            {
                using (var dbContext = new RoomScheduleContext())
                {
                    string extraClass;
                    for (int i = 0; i < notes.Count; i++)
                    {
                        Class newClass = Class.Clone(theClass);

                        try
                        {
                            extraClass = notes.ElementAt(i);
                            List<string> temp = extraClass.Split(new char[] { ' ', '-', }).ToList();
                            temp.RemoveAll(s => s.Length == 0);
                            string[] parts = temp.ToArray();

                            if (parts.Length >= 8)
                            {
                                string type = parts[0].Trim(':');
                                string building = parts[1];
                                string room = parts[2];
                                string days = parts[3];

                                string beginString = parts[5];
                                string beginTimeOfDay = parts.Length == 9 ? parts[6] : "";
                                string endString = parts[parts.Length - 2];
                                string endTimeOfDay = parts[parts.Length - 1];

                                TimeSpan begin = beginString.Contains(":") ? TimeSpan.Parse(beginString) : TimeSpan.FromHours(int.Parse(beginString));
                                TimeSpan end = endString.Contains(":") ? TimeSpan.Parse(endString) : TimeSpan.FromHours(int.Parse(endString));

                                begin = begin.Add(beginTimeOfDay == "" && endTimeOfDay.ToLower() == "pm" && !beginString.StartsWith("12")? TimeSpan.FromHours(12) : TimeSpan.FromTicks(0));
                                end = end.Add(endTimeOfDay.ToLower() == "pm" && !(endString.StartsWith("12")) ? TimeSpan.FromHours(12) : TimeSpan.FromTicks(0));

                                // Change class parameters and add to database
                                newClass.Syn = i == 0 ? -theClass.Syn : i == 1 ? -(int.MaxValue - theClass.Syn) : i == 2 ? (int.MaxValue - theClass.Syn) : throw new Exception("Too many also meets!");
                                newClass.Title = theClass.Title + ' ' + type;
                                newClass.Building = building;
                                newClass.Room = room;
                                newClass.Days = days;
                                newClass.Begin = begin;
                                newClass.End = end;

                                dbContext.Class.Add(newClass);
                                dbContext.SaveChanges();
                            }

                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Unable to add extra class for {theClass.Syn}, {theClass.Title} because of exception: {e.Message}");
                            Console.WriteLine($"Inner exception: {e.InnerException.Message}");
                        }
                    }
                }
                
            }

            public string[] GetBuildingAndRoom(string place)
            {
                return place.Split(' ');
            }

            public void Test()
            {
                using (var db = new RoomScheduleContext())
                {
                    db.Class.Add(new Class()
                    {
                        Begin = new TimeSpan(9, 0, 0),
                        End = new TimeSpan(9, 50, 0),
                        Days = "MWF",
                        Building = "HSC",
                        Room = "3113",
                        Professor = "Richard Halterman",
                        Section = "A",
                        Semester = "F14",
                        Title = "CPTR124 Intro to Programming",
                        Syn = 1234,
                    });
                    var count = db.SaveChanges();

                }
            }
            
            #endregion

        }

        static void Main(string[] args)
        {
            ControlConsole();
        }

        static void ControlConsole()
        {
            ConsoleKeyInfo command;

            Console.WriteLine("What do you desire?");
            command = Console.ReadKey();
            Console.WriteLine();

            while(command.KeyChar != 'q')
            {
                switch(command.KeyChar)
                {
                    case 's':
                            SemesterStatus();
                            break;

                    case 'c':
                            AddAllClasses();
                            break;

                    case 'r':
                            RemoveAllClasses();
                            break;

                    case 'd':
                            ChangeDefaultSemester(GetSemester("be default", true));
                            break;

                    case 'i':
                            HideSemester(GetSemester("hide", true));
                            break;

                    case 'w':
                            ShowSemester(GetSemester("show", true));
                            break;

                    case 'a':
                            AddSemester(GetSemester("add", false));
                            break;

                    case 'x':
                            RemoveSemester(GetSemester("remove", true));
                            break;

                    case 'h':
                        ShowHelp();
                        break;

                    case 'q':
                            break;

                    default:
                            Console.WriteLine("Unknown command");
                            break;
                }

                Console.WriteLine("Enter next command:");
                command = Console.ReadKey();
                Console.WriteLine();
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Press 's' to show all semesters and their status");
            Console.WriteLine("Press 'c' to scrape website for classes for all semesters and add them to the database");
            Console.WriteLine("Press 'r' to remove all classes from database");
            Console.WriteLine("Press 'd' to change the defaule semester");
            Console.WriteLine("Press 'i' to hide a semester");
            Console.WriteLine("Press 'w' to show a semester");
            Console.WriteLine("Press 'a' to add a semester");
            Console.WriteLine("Press 'x' to remove a semester");
            Console.WriteLine("Press 'h' to show help messae");
            Console.WriteLine("Press 'q' to quit");

        }

        static string GetSemester(string type, bool showOptions)
        {
            string semester;
            Console.WriteLine($"Enter semester to {type}");

            if (showOptions)
            {
                Console.WriteLine("Options:");
                using (var dbContext = new RoomScheduleContext())
                {
                    foreach (string sem in dbContext.Semester.Select(row => row.Name).ToList())
                    {
                        Console.WriteLine(sem);
                    }
                }
            }
            semester = Console.ReadLine();

            return semester;
        }

        static void SemesterStatus()
        {
            using (var dbContext = new RoomScheduleContext())
            {
                Console.WriteLine("Semester table status:");
                foreach(Semester semester in dbContext.Semester)
                {
                    Console.WriteLine($"Semester Name: {semester.Name}, Display: {semester.Display}, Default: {semester.Default}");
                }
            }
        }

        static void AddAllClasses()
        {
            try
            {
                int totalCount = 0;
                using (var dbcontext = new RoomScheduleContext())
                {
                    Parser parser = new Parser();
                    foreach (string semester in dbcontext.Semester.Select(semester => semester.Name))
                    {
                        parser.Semester = semester;
                        int count = parser.DoTheWork();
                        totalCount += count;
                        Console.WriteLine($"{count} classes added for the {semester} term");
                    }
                }

                Console.WriteLine($"Finished. Added {totalCount} classes for all semesters");
            }
            catch(Exception e)
            {
                Console.WriteLine($"Operation failed: {e.Message}");
            }

        }

        static void RemoveAllClasses()
        {
            using (var dbContext = new RoomScheduleContext())
            {
                try
                {
                    Console.WriteLine("Attempting to remove all classes");

                    dbContext.Class.RemoveRange(dbContext.Class);
                    int classesRemoved = dbContext.SaveChanges();

                    Console.WriteLine($"{classesRemoved} classes removed");
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Operation failed: {e.Message}");
                }

            }
        }

        static void ChangeDefaultSemester(string semester)
        {
            using (var dbContext = new RoomScheduleContext())
            {
                try
                {
                    List<string> availableSemesters = dbContext.Semester.Select(row => row.Name).ToList();

                    if (availableSemesters.Contains(semester))
                    {
                        dbContext.Semester.UpdateRange(dbContext.Semester);
                        foreach (Semester sem in dbContext.Semester)
                        {
                            sem.Default = false;
                        }
                        dbContext.Semester.FirstOrDefault(row => row.Name == semester).Default = true;

                        dbContext.SaveChanges();
                    }

                    Semester defaultSemester = dbContext.Semester.Where(sem => sem.Default == true).FirstOrDefault();

                    if (defaultSemester != null)
                        Console.WriteLine($"The default semester is {defaultSemester.Name}");
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Operation failed: {e.Message}");
                }

            }
        }

        static void AddSemester(string semester)
        {
            using (var dbContext = new RoomScheduleContext())
            {
                try
                {
                    dbContext.Semester.Add(new Semester { Name = semester, Default = false, Display = true });
                    dbContext.SaveChanges();
                    SemesterStatus();
                }

                catch(Exception e)
                {
                    Console.WriteLine($"Operation failed: {e.Message}");
                }
            }
        }

        static void RemoveSemester(string semester)
        {
            using (var dbContext = new RoomScheduleContext())
            {
                try
                {
                    dbContext.Semester.Remove(dbContext.Semester.Where(row => row.Name == semester).FirstOrDefault());
                    dbContext.SaveChanges();
                    SemesterStatus();
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Operation failed: {e.Message}");
                }
            }
        }

        static void ShowSemester(string semester)
        {
            using (var dbContext = new RoomScheduleContext())
            {
                try
                {
                    Semester sem = dbContext.Semester.Where(row => row.Name == semester).FirstOrDefault();
                    if (sem != null)
                    {
                        dbContext.Semester.Update(sem);
                        sem.Display = true;
                        dbContext.SaveChanges();
                    }
                    SemesterStatus();
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Operation failed: {e.Message}");
                }
            }
        }

        static void HideSemester(string semester)
        {
            using (var dbContext = new RoomScheduleContext())
            {
                try
                {
                    Semester sem = dbContext.Semester.Where(row => row.Name == semester).FirstOrDefault();
                    if (sem != null)
                    {
                        dbContext.Semester.Update(sem);
                        sem.Display = false;
                        dbContext.SaveChanges();
                    }
                    SemesterStatus();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Operation failed: {e.Message}");
                }
            }
        }

    }
}
