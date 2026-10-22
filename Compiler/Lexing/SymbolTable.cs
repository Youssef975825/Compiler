namespace Compiler.Lexing;

using System.Collections.Generic;
public class SymbolEntry
{
    public string Name { get; set; } = "";
    public int FirstLine {get; set;}
    public int Count {get; set;}
}

public class SymbolTable
{
    private readonly Dictionary<string, SymbolEntry> entries = new Dictionary<string, SymbolEntry>();
    public IEnumerable<SymbolEntry> Entries => entries.Values;
    public int Count => entries.Count;

    public void Add(string name, int line)
    {
        if(entries.TryGetValue(name, out SymbolEntry? entry)) entry.Count++;
        else entries[name] = new SymbolEntry { Name = name, FirstLine = line, Count = 1 };
    }

    public bool Contains(string name)
    {
        return entries.ContainsKey(name);
    }
    
    public void Clear()
    {
        entries.Clear();
    }
}


