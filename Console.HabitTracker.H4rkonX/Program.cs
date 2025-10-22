using Microsoft.Data.Sqlite;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace Program;

class HabitTracker
{
    public static void Main(string[] args)
    {
        CreateAndSeed();
        Logic();
    }

    public static void CreateAndSeed()
    {
        string connectionString = "Data Source=habits.db";

        try
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string createIfNotExists = "CREATE TABLE IF NOT EXISTS habits(id INTEGER PRIMARY KEY, habit TEXT, occurrences INTEGER, unit TEXT, habit_date TEXT)";
                using var createCommand = new SqliteCommand(createIfNotExists, connection);
                createCommand.ExecuteNonQuery();

                var (anyRecords, _) = ViewAllRecords();
                Console.Clear();
                if (anyRecords == 0)
                {
                    string seedRecords = "INSERT INTO habits(habit, occurrences, unit, habit_date) VALUES ($habit, $occurrence, $unit, $date);";
                    using var seedCommand = new SqliteCommand(seedRecords, connection);
                    List<string> habits = new List<string> { "Ran", "Ate", "Showered", "Drank", "Stretched" };
                    List<string> habitsUnit = new List<string> { "kilometers", "calories", "times", "glasses", "times" };
                    List<int> habitsOccurrences = new List<int> { 5, 450, 2, 4, 4 };
                    for (int i = 0; i < habits.Count; i++)
                    {
                        seedCommand.Parameters.Clear();
                        seedCommand.Parameters.AddWithValue("$habit", habits[i]);
                        seedCommand.Parameters.AddWithValue("$occurrence", habitsOccurrences[i]);
                        seedCommand.Parameters.AddWithValue("$unit", habitsUnit[i]);
                        seedCommand.Parameters.AddWithValue("$date", DateOnly.FromDateTime(DateTime.Now));
                        seedCommand.ExecuteNonQuery();
                    }
                }
            }
        }
        catch (SqliteException ex)
        {
            AppFailed(ex);
        }
    }

    public static void InsertRecord(string habit, int occurrence, string unit, string date)
    {
        string connectionString = "Data Source=habits.db";

        try
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string insert = "INSERT INTO habits(habit, occurrences, unit, habit_date) VALUES ($habit, $occurrence, $unit, $date)";
                using var command = new SqliteCommand(insert, connection);
                command.Parameters.AddWithValue("$habit", habit);
                command.Parameters.AddWithValue("$occurrence", occurrence);
                command.Parameters.AddWithValue("$unit", unit);
                command.Parameters.AddWithValue("$date", date);
                command.ExecuteNonQuery();
            }
        }
        catch (SqliteException ex)
        {
            AppFailed(ex);
        }
    }

    public static void UpdateRecord(int id, string habit, int occurrence, string unit, string date)
    {
        string connectionString = "Data Source=habits.db";

        try
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string insert = "UPDATE habits SET habit=$habit, occurrences=$occurrence, unit=$unit, habit_date=$date WHERE id=$id";
                using var command = new SqliteCommand(insert, connection);
                command.Parameters.AddWithValue("$id", id);
                command.Parameters.AddWithValue("$habit", habit);
                command.Parameters.AddWithValue("$occurrence", occurrence);
                command.Parameters.AddWithValue("$unit", unit);
                command.Parameters.AddWithValue("$date", date);
                command.ExecuteNonQuery();
            }
        }
        catch (SqliteException ex)
        {
            AppFailed(ex);
        }
    }

    public static void DeleteRecord(int id)
    {
        string connectionString = "Data Source=habits.db";

        try
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string delete = "DELETE FROM habits WHERE id=$id;";
                using var command = new SqliteCommand(delete, connection);
                command.Parameters.AddWithValue("$id", id);
                command.ExecuteNonQuery();
            }
        }
        catch (SqliteException ex)
        {
            AppFailed(ex);
        }
    }

    public static (int, List<int>) ViewAllRecords()
    {
        int recordsAmount = 0;
        List<int> ids = new List<int>();
        string connectionString = "Data Source=habits.db";
        var table = new Table();
        table.Title("[red]Recorded Habits[/]");

        try
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string selectAll = "SELECT * FROM habits";
                using var command = new SqliteCommand(selectAll, connection);

                SqliteDataReader reader = command.ExecuteReader();

                if (!reader.HasRows)
                {
                    Console.WriteLine("No records found!");
                    return (0, ids);
                }

                while (reader.HasRows)
                {

                    table.AddColumn(reader.GetName(0));
                    table.AddColumn(reader.GetName(1));
                    table.AddColumn(reader.GetName(2));
                    table.AddColumn(reader.GetName(3));
                    table.AddColumn(reader.GetName(4));

                    while (reader.Read())
                    {
                        table.AddRow(
                            Convert.ToString(reader.GetInt32(0)),
                            $"[green]{reader.GetString(1)}[/]",
                            $"{reader.GetString(2)}",
                            $"{reader.GetString(3)}",
                            $"{reader.GetString(4)}");

                        recordsAmount++;
                        ids.Add(reader.GetInt32(0));
                    }
                    reader.NextResult();
                }
            }
        }
        catch (SqliteException ex)
        {
            AppFailed(ex);
        }
        table.Expand();
        AnsiConsole.Write(table);
        return (recordsAmount, ids);
    }

    public static void ReportRecord(int id)
    {
        string connectionString = "Data Source=habits.db";

        try
        {
            using (SqliteConnection connection = new SqliteConnection(connectionString))
            {
                connection.Open();
                string select = "SELECT * FROM habits WHERE id=$id;";
                using var command = new SqliteCommand(select, connection);
                command.Parameters.AddWithValue("$id", id);

                SqliteDataReader reader = command.ExecuteReader();

                while (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        AnsiConsole.MarkupLine($@"You [red]{reader.GetString(1)}[/] [yellow]{reader.GetString(2)}[/] {reader.GetString(3)} on [blue]{reader.GetString(4)}[/]");
                    }
                    reader.NextResult();
                }
            }
        }
        catch (SqliteException ex)
        {
            AppFailed(ex);
        }
    }

    public static void AppFailed(SqliteException ex)
    {
        Console.Clear();
        Console.WriteLine(ex.Message);
        Console.WriteLine("The application will close...");
        Thread.Sleep(1500);
        Environment.Exit(1);
    }

    public static void WaitAndClear(int milliseconds)
    {
        Thread.Sleep(milliseconds);
        Console.Clear();
    }

    public static bool NoNullValues(params string[] values)
    {
        foreach (string value in values)
        {
            if (value == null || value == "")
            {
                Console.WriteLine("Values cannot be NULL/Empty, going back to main menu...");
                WaitAndClear(2000);
                return true;
            }
        }
        return false;
    }

    public static void Menu()
    {
        var table = new Table();

        table.Title("[red]Habits Tracker[/]");
        table.AddColumn(new TableColumn("[bold]MAIN MENU[/]"));
        table.AddEmptyRow();
        table.AddRow("1. Insert record");
        table.AddRow("2. Update record");
        table.AddRow("3. Delete record");
        table.AddRow("4. View all records");
        table.AddRow("5. Report habit information");
        table.AddRow("6. Close application");
        table.AddEmptyRow();
        table.Expand();

        AnsiConsole.Write(table);
    }

    public static void Logic()
    {
        int id = 0;
        bool updating = false;
        int userInput;
        string datePattern = @"^((\d{4}-\d{2}-\d{2})|(?i)today)$";
        Regex regex = new Regex(datePattern);

        while (true)
        {
            if (updating)
            {
                userInput = 1;
            }
            else
            {
                Menu();
                int.TryParse(Console.ReadLine(), out userInput);
            }

            switch (userInput)
            {
                case 1:
                    if (updating)
                    {
                        ViewAllRecords();
                    }
                    Console.WriteLine("What did you do?");
                    string habit = Console.ReadLine();
                    Console.WriteLine("How do you want to quantify it?");
                    string unitOfMeasurement = Console.ReadLine();
                    Console.WriteLine($"How many {unitOfMeasurement}?");
                    int occurrence;
                    while (!int.TryParse(Console.ReadLine(), out occurrence))
                    {
                        Console.WriteLine($"Enter a valid number of {unitOfMeasurement}.");
                        Thread.Sleep(1500);
                    }
                    Console.WriteLine("When does this happened? Please use 'today' or 'YYYY-MM-DD'.");

                    while (true)
                    {
                        string date = Console.ReadLine()?.Trim() ?? "";
                        if (regex.IsMatch(date))
                        {
                            date = DateOnly.FromDateTime(DateTime.Now).ToString("yyyy-MM-dd");
                            if (NoNullValues(habit, unitOfMeasurement, date))
                            {
                                updating = false;
                                break;
                            }
                            if (updating)
                            {
                                UpdateRecord(id, habit, occurrence, unitOfMeasurement, date);
                                Console.WriteLine("Record updated!");
                                WaitAndClear(1000);
                                updating = false;
                                break;
                            }
                            else
                            {
                                InsertRecord(habit, occurrence, unitOfMeasurement, date);
                                Console.WriteLine("Record logged!");
                                WaitAndClear(1000);
                                break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid date format. Please use 'today' or 'YYYY-MM-DD'.");
                        }
                    }
                    continue;
                case 2:
                    while (true)
                    {
                        Console.Clear();
                        var (_, ids) = ViewAllRecords();
                        Console.WriteLine();
                        Console.WriteLine("Which record do you want to update?");

                        if ((int.TryParse(Console.ReadLine(), out id)) && ids.Contains(id))
                        {
                            updating = true;
                            WaitAndClear(500);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Select one of the available records!");
                            Thread.Sleep(1500);
                            continue;
                        }
                    }
                    continue;
                case 3:
                    while (true)
                    {
                        Console.Clear();
                        var (_, ids) = ViewAllRecords();
                        Console.WriteLine();
                        Console.WriteLine("Which record do you want to delete?");

                        if ((int.TryParse(Console.ReadLine(), out id)) && ids.Contains(id))
                        {
                            DeleteRecord(id);
                            Console.WriteLine("Record deleted!");
                            WaitAndClear(1500);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Select one of the available records!");
                            Thread.Sleep(1500);
                            continue;
                        }
                    }
                    continue;
                case 4:
                    Console.Clear();
                    ViewAllRecords();
                    Console.WriteLine();
                    Console.Write("Press any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                    continue;
                case 5:
                    while (true)
                    {
                        Console.Clear();
                        var (_, ids) = ViewAllRecords();
                        Console.WriteLine();
                        Console.WriteLine("Select your desired record: ");

                        if ((int.TryParse(Console.ReadLine(), out id)) && ids.Contains(id))
                        {
                            ReportRecord(id);
                            WaitAndClear(5000);
                            break;
                        }
                        else
                        {
                            Console.WriteLine("Select one of the available records!");
                            Thread.Sleep(1500);
                            continue;
                        }
                    }
                    continue;
                case 6:
                    Environment.Exit(0);
                    break;
                default:
                    Console.WriteLine("Please choose an available option!");
                    WaitAndClear(1000);
                    continue;
            }
            break;
        }
    }
}