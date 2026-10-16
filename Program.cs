using System;
using System.Collections.Generic;
using System.IO;

// ---------- Tasks 3 and 4 + Going further : Program.cs ----------
// (Top-level statements must come BEFORE type declarations in C#)

const string LogFile = "failed_operations.log";

var rnd = new Random();
string[] names = { "Ada", "Linus", "Grace", "Alan", "Margaret" };
var accounts = new List<BankAccount>();

// Task 3: 5 accounts, Id = 1..5, random balance in [5000, 100000]
for (int i = 0; i < names.Length; i++)
{
    accounts.Add(new BankAccount
    {
        Id = i + 1,
        CustomerName = names[i],
        Balance = rnd.Next(5000, 100001) // upper bound is exclusive
    });
}

// Task 4 + loop until the user types q
while (true)
{
    ShowAccounts();
    Console.WriteLine();
    Console.Write("Choose: (w)ithdraw, (d)eposit, (t)ransfer, (q)uit: ");
    string choice = (Console.ReadLine() ?? "").Trim().ToLower();

    if (choice == "q")
        break;

    switch (choice)
    {
        case "w":
        {
            // The account is validated BEFORE asking for the amount
            var acc = ReadAccount("Account id");
            if (acc == null) break;
            var amount = ReadAmount();
            if (amount == null) break;
            Execute($"Withdraw {amount} from #{acc.Id}", () =>
            {
                acc.Withdraw(amount.Value);
                Console.WriteLine($"Withdrawal OK. New balance = {acc.Balance}");
            });
            break;
        }
        case "d":
        {
            var acc = ReadAccount("Account id");
            if (acc == null) break;
            var amount = ReadAmount();
            if (amount == null) break;
            Execute($"Deposit {amount} to #{acc.Id}", () =>
            {
                acc.Deposit(amount.Value);
                Console.WriteLine($"Deposit OK. New balance = {acc.Balance}");
            });
            break;
        }
        case "t":
        {
            var from = ReadAccount("Source account id");
            if (from == null) break;
            var to = ReadAccount("Destination account id");
            if (to == null) break;
            if (from.Id == to.Id)
            {
                Console.WriteLine("Source and destination must be different.");
                Log($"Transfer #{from.Id} -> #{to.Id}", "Same account");
                break;
            }
            var amount = ReadAmount();
            if (amount == null) break;
            Execute($"Transfer {amount} from #{from.Id} to #{to.Id}", () =>
            {
                BankAccount.Transfer(from, to, amount.Value);
                Console.WriteLine($"Transfer OK. #{from.Id} = {from.Balance}, #{to.Id} = {to.Balance}");
            });
            break;
        }
        default:
            Console.WriteLine("Unknown choice.");
            break;
    }

    Console.WriteLine();
}

Console.WriteLine("Goodbye.");

// ---------- Local helpers ----------

void ShowAccounts()
{
    Console.WriteLine("Id\tCustomer\tBalance");
    foreach (var a in accounts)
        Console.WriteLine($"{a.Id}\t{a.CustomerName,-10}\t{a.Balance}");
}

// Returns the account, or null (with an immediate error message) if the input is bad
BankAccount? ReadAccount(string label)
{
    Console.Write($"{label}: ");
    if (!int.TryParse(Console.ReadLine(), out int id))
    {
        Console.WriteLine("Invalid input");
        Log("Read account id", "Invalid input (not a number)");
        return null;
    }

    var acc = accounts.Find(a => a.Id == id);
    if (acc == null)
    {
        Console.WriteLine("Account not found");
        Log("Read account id", $"Account {id} not found");
        return null;
    }
    return acc;
}

decimal? ReadAmount()
{
    Console.Write("Amount: ");
    if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
    {
        Console.WriteLine("Invalid input");
        Log("Read amount", "Invalid input (not a number)");
        return null;
    }
    return amount;
}

// try / catch / finally shared by every operation
void Execute(string operation, Action action)
{
    try
    {
        action();
    }
    catch (InsufficientFundsException ex)
    {
        Console.WriteLine($"ERROR: {ex.Message}");
        Console.WriteLine($"       Balance = {ex.Balance}, requested = {ex.Requested}");
        Log(operation, $"{ex.Message} (balance = {ex.Balance}, requested = {ex.Requested})");
    }
    catch (ArgumentOutOfRangeException)
    {
        Console.WriteLine("Invalid amount: it must be greater than 0.");
        Log(operation, "Invalid amount (<= 0)");
    }
    finally
    {
        Console.WriteLine("Operation finished.");
    }
}

// Write failed operations to a file (using + StreamWriter, append mode)
void Log(string operation, string reason)
{
    using (var writer = new StreamWriter(LogFile, append: true))
    {
        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {operation} | {reason}");
    }
}

// ---------- Task 1 ----------
public class InsufficientFundsException : Exception
{
    public decimal Balance { get; }
    public decimal Requested { get; }

    // The three standard constructors
    public InsufficientFundsException() { }

    public InsufficientFundsException(string message) : base(message) { }

    public InsufficientFundsException(string message, Exception inner)
        : base(message, inner) { }

    // Business constructor
    public InsufficientFundsException(decimal balance, decimal requested)
        : base("Insufficient bank account balance")
    {
        Balance = balance;
        Requested = requested;
    }
}

// ---------- Task 2 (+ Deposit and Transfer) ----------
public class BankAccount
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Balance { get; set; }

    public void Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than 0.");

        if (amount > Balance)
            throw new InsufficientFundsException(Balance, amount);

        Balance -= amount;
    }

    public void Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than 0.");

        Balance += amount;
    }

    // Reuses Withdraw: if it throws, nothing is deposited
    public static void Transfer(BankAccount from, BankAccount to, decimal amount)
    {
        from.Withdraw(amount);
        to.Deposit(amount);
    }
}